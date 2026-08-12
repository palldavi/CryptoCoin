Imports System.IO
Imports System.Net
Imports System.Net.Sockets
Imports System.Threading
Imports CryptoCoin.Cryptography

Namespace CryptoCoin.Networking

    ''' <summary>
    ''' Represents the state of a TCP connection's read operation.
    ''' </summary>
    Public Enum ReadState
        ''' <summary>Waiting to read the message header.</summary>
        ReadingHeader
        ''' <summary>Reading the message payload.</summary>
        ReadingPayload
        ''' <summary>A complete message is ready for processing.</summary>
        MessageReady
        ''' <summary>The connection has been closed or errored.</summary>
        Closed
    End Enum

    ''' <summary>
    ''' Manages a single TCP connection to a peer with read/write buffering
    ''' and message framing. Handles the low-level byte stream and assembles
    ''' complete NetworkMessage instances from the wire protocol.
    ''' </summary>
    Public Class TcpConnection
        Implements IDisposable

        Private _client As TcpClient
        Private _stream As NetworkStream
        Private _readBuffer() As Byte
        Private _readOffset As Integer
        Private _writeQueue As New Queue(Of Byte())()
        Private _readState As ReadState
        Private _currentHeader As NetworkMessage
        Private _payloadBuffer() As Byte
        Private _payloadOffset As Integer
        Private _disposed As Boolean
        Private ReadOnly _syncLock As New Object()

        ''' <summary>
        ''' Size of the read buffer in bytes.
        ''' </summary>
        Public Const ReadBufferSize As Integer = 65536

        ''' <summary>
        ''' Maximum write queue depth before dropping messages.
        ''' </summary>
        Public Const MaxWriteQueueDepth As Integer = 1000

        ''' <summary>
        ''' The peer associated with this connection.
        ''' </summary>
        Public Property Peer As Peer

        ''' <summary>
        ''' The expected network magic bytes for message validation.
        ''' </summary>
        Public Property ExpectedMagic As Byte()

        ''' <summary>
        ''' Whether the connection is currently open and usable.
        ''' </summary>
        Public ReadOnly Property IsConnected As Boolean
            Get
                If _client Is Nothing Then Return False
                Return _client.Connected AndAlso _readState <> ReadState.Closed
            End Get
        End Property

        ''' <summary>
        ''' The current read state of the connection.
        ''' </summary>
        Public ReadOnly Property CurrentReadState As ReadState
            Get
                Return _readState
            End Get
        End Property

        ''' <summary>
        ''' Number of messages waiting to be sent.
        ''' </summary>
        Public ReadOnly Property WriteQueueCount As Integer
            Get
                SyncLock _syncLock
                    Return _writeQueue.Count
                End SyncLock
            End Get
        End Property

        ''' <summary>
        ''' Total bytes read from this connection.
        ''' </summary>
        Public Property TotalBytesRead As Long

        ''' <summary>
        ''' Total bytes written to this connection.
        ''' </summary>
        Public Property TotalBytesWritten As Long

        ''' <summary>
        ''' Creates a new TcpConnection wrapping an existing TcpClient.
        ''' </summary>
        ''' <param name="client">The connected TcpClient.</param>
        ''' <param name="peer">The peer associated with this connection.</param>
        Public Sub New(client As TcpClient, peer As Peer)
            If client Is Nothing Then Throw New ArgumentNullException(NameOf(client))
            If peer Is Nothing Then Throw New ArgumentNullException(NameOf(peer))

            _client = client
            _stream = client.GetStream()
            Me.Peer = peer
            ExpectedMagic = NetworkMessage.MainNetMagic

            _readBuffer = New Byte(ReadBufferSize - 1) {}
            _readOffset = 0
            _readState = ReadState.ReadingHeader
            _disposed = False
            TotalBytesRead = 0
            TotalBytesWritten = 0
        End Sub

        ''' <summary>
        ''' Creates a new outbound TcpConnection to the specified endpoint.
        ''' </summary>
        ''' <param name="address">The remote IP address.</param>
        ''' <param name="port">The remote port.</param>
        ''' <param name="timeoutMs">Connection timeout in milliseconds.</param>
        ''' <returns>A connected TcpConnection, or Nothing if connection failed.</returns>
        Public Shared Function ConnectTo(address As IPAddress, port As Integer,
                                         timeoutMs As Integer) As TcpConnection
            Dim client As New TcpClient()
            client.NoDelay = True
            client.ReceiveBufferSize = ReadBufferSize
            client.SendBufferSize = ReadBufferSize

            Try
                Dim connectTask = client.ConnectAsync(address, port)
                If Not connectTask.Wait(timeoutMs) Then
                    client.Close()
                    Return Nothing
                End If

                Dim peer As New Peer(address, port)
                peer.IsInbound = False
                peer.State = PeerState.Connecting
                peer.ConnectedAt = DateTime.UtcNow

                Return New TcpConnection(client, peer)
            Catch ex As Exception
                client.Close()
                Return Nothing
            End Try
        End Function

        ''' <summary>
        ''' Attempts to read data from the socket and assemble messages.
        ''' Returns a complete NetworkMessage if one is ready, or Nothing.
        ''' </summary>
        ''' <returns>A complete NetworkMessage, or Nothing if more data is needed.</returns>
        Public Function TryReadMessage() As NetworkMessage
            If Not IsConnected Then Return Nothing

            Try
                ' Check if data is available
                If Not _stream.DataAvailable Then Return Nothing

                ' Read available data into buffer
                Dim bytesRead As Integer = _stream.Read(_readBuffer, _readOffset,
                                                        ReadBufferSize - _readOffset)
                If bytesRead = 0 Then
                    _readState = ReadState.Closed
                    Return Nothing
                End If

                _readOffset += bytesRead
                TotalBytesRead += bytesRead
                If Peer IsNot Nothing Then Peer.BytesReceived += bytesRead

                ' Try to parse based on current state
                Select Case _readState
                    Case ReadState.ReadingHeader
                        Return TryParseHeader()
                    Case ReadState.ReadingPayload
                        Return TryParsePayload()
                    Case Else
                        Return Nothing
                End Select

            Catch ex As IOException
                _readState = ReadState.Closed
                Return Nothing
            Catch ex As SocketException
                _readState = ReadState.Closed
                Return Nothing
            End Try
        End Function

        ''' <summary>
        ''' Enqueues a message for sending.
        ''' </summary>
        ''' <param name="message">The network message to send.</param>
        ''' <returns>True if the message was queued successfully.</returns>
        Public Function EnqueueMessage(message As NetworkMessage) As Boolean
            If message Is Nothing Then Return False
            If Not IsConnected Then Return False

            Dim serialized As Byte() = message.Serialize()

            SyncLock _syncLock
                If _writeQueue.Count >= MaxWriteQueueDepth Then
                    Return False
                End If
                _writeQueue.Enqueue(serialized)
            End SyncLock

            Return True
        End Function

        ''' <summary>
        ''' Sends all queued messages to the peer.
        ''' </summary>
        ''' <returns>The number of messages sent.</returns>
        Public Function FlushWriteQueue() As Integer
            If Not IsConnected Then Return 0

            Dim sent As Integer = 0

            Try
                SyncLock _syncLock
                    While _writeQueue.Count > 0
                        Dim data As Byte() = _writeQueue.Dequeue()
                        _stream.Write(data, 0, data.Length)
                        TotalBytesWritten += data.Length
                        If Peer IsNot Nothing Then Peer.BytesSent += data.Length
                        sent += 1
                    End While
                End SyncLock

                _stream.Flush()

            Catch ex As IOException
                _readState = ReadState.Closed
            Catch ex As SocketException
                _readState = ReadState.Closed
            End Try

            Return sent
        End Function

        ''' <summary>
        ''' Sends a single message immediately without queuing.
        ''' </summary>
        ''' <param name="message">The message to send.</param>
        ''' <returns>True if the message was sent successfully.</returns>
        Public Function SendImmediate(message As NetworkMessage) As Boolean
            If message Is Nothing OrElse Not IsConnected Then Return False

            Try
                Dim data As Byte() = message.Serialize()
                _stream.Write(data, 0, data.Length)
                _stream.Flush()
                TotalBytesWritten += data.Length
                If Peer IsNot Nothing Then
                    Peer.BytesSent += data.Length
                    Peer.LastSentAt = DateTime.UtcNow
                End If
                Return True
            Catch ex As Exception
                _readState = ReadState.Closed
                Return False
            End Try
        End Function

        ''' <summary>
        ''' Closes the connection and releases resources.
        ''' </summary>
        Public Sub Close()
            _readState = ReadState.Closed
            If Peer IsNot Nothing Then
                Peer.State = PeerState.Disconnected
            End If

            Try
                If _stream IsNot Nothing Then _stream.Close()
                If _client IsNot Nothing Then _client.Close()
            Catch ex As Exception
                ' Ignore errors during close
            End Try
        End Sub

        ''' <summary>
        ''' Attempts to parse a message header from the read buffer.
        ''' </summary>
        Private Function TryParseHeader() As NetworkMessage
            If _readOffset < NetworkMessage.HeaderSize Then Return Nothing

            ' Parse the header
            Dim headerData(NetworkMessage.HeaderSize - 1) As Byte
            Array.Copy(_readBuffer, 0, headerData, 0, NetworkMessage.HeaderSize)
            _currentHeader = NetworkMessage.DeserializeHeader(headerData)

            ' Validate magic bytes
            If Not _currentHeader.ValidateMagic(ExpectedMagic) Then
                _readState = ReadState.Closed
                Return Nothing
            End If

            ' Validate payload size
            If Not _currentHeader.IsPayloadSizeValid() Then
                _readState = ReadState.Closed
                Return Nothing
            End If

            ' Shift remaining data in buffer
            Dim remaining As Integer = _readOffset - NetworkMessage.HeaderSize
            If remaining > 0 Then
                Array.Copy(_readBuffer, NetworkMessage.HeaderSize, _readBuffer, 0, remaining)
            End If
            _readOffset = remaining

            ' If no payload, message is complete
            If _currentHeader.PayloadLength = 0 Then
                _currentHeader.Payload = Array.Empty(Of Byte)()
                _readState = ReadState.ReadingHeader
                Return _currentHeader
            End If

            ' Prepare for payload reading
            _payloadBuffer = New Byte(_currentHeader.PayloadLength - 1) {}
            _payloadOffset = 0
            _readState = ReadState.ReadingPayload

            ' Check if we already have payload data in the buffer
            Return TryParsePayload()
        End Function

        ''' <summary>
        ''' Attempts to complete reading the message payload.
        ''' </summary>
        Private Function TryParsePayload() As NetworkMessage
            Dim needed As Integer = _currentHeader.PayloadLength - _payloadOffset
            Dim available As Integer = Math.Min(needed, _readOffset)

            If available > 0 Then
                Array.Copy(_readBuffer, 0, _payloadBuffer, _payloadOffset, available)
                _payloadOffset += available

                ' Shift remaining data
                Dim remaining As Integer = _readOffset - available
                If remaining > 0 Then
                    Array.Copy(_readBuffer, available, _readBuffer, 0, remaining)
                End If
                _readOffset = remaining
            End If

            ' Check if payload is complete
            If _payloadOffset >= _currentHeader.PayloadLength Then
                _currentHeader.Payload = _payloadBuffer

                ' Validate checksum
                If Not _currentHeader.ValidateChecksum() Then
                    _readState = ReadState.Closed
                    Return Nothing
                End If

                _readState = ReadState.ReadingHeader
                Return _currentHeader
            End If

            Return Nothing
        End Function

        Protected Overridable Sub Dispose(disposing As Boolean)
            If Not _disposed Then
                If disposing Then
                    Close()
                End If
                _disposed = True
            End If
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub

    End Class

End Namespace
