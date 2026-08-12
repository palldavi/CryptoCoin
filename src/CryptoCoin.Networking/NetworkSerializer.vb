Imports CryptoCoin.Cryptography

Namespace CryptoCoin.Networking

    ''' <summary>
    ''' Serializes and deserializes network messages with magic bytes, checksums,
    ''' and proper framing for the CryptoCoin P2P wire protocol.
    ''' Provides factory methods for creating typed messages from raw payloads.
    ''' </summary>
    Public Class NetworkSerializer

        Private ReadOnly _magic As Byte()

        ''' <summary>
        ''' Gets the network magic bytes used by this serializer.
        ''' </summary>
        Public ReadOnly Property Magic As Byte()
            Get
                Return _magic
            End Get
        End Property

        ''' <summary>
        ''' Creates a new NetworkSerializer for the main network.
        ''' </summary>
        Public Sub New()
            _magic = NetworkMessage.MainNetMagic
        End Sub

        ''' <summary>
        ''' Creates a new NetworkSerializer with custom magic bytes.
        ''' </summary>
        ''' <param name="magic">The 4-byte network magic identifier.</param>
        Public Sub New(magic As Byte())
            If magic Is Nothing OrElse magic.Length <> 4 Then
                Throw New ArgumentException("Magic bytes must be exactly 4 bytes.")
            End If
            _magic = magic
        End Sub

        ''' <summary>
        ''' Serializes a typed message into a complete NetworkMessage with header.
        ''' </summary>
        ''' <param name="command">The command name.</param>
        ''' <param name="payload">The serialized payload data.</param>
        ''' <returns>A complete NetworkMessage ready for transmission.</returns>
        Public Function CreateMessage(command As String, payload As Byte()) As NetworkMessage
            Return New NetworkMessage(_magic, command, payload)
        End Function

        ''' <summary>
        ''' Serializes a VersionMessage into a complete NetworkMessage.
        ''' </summary>
        ''' <param name="versionMsg">The version message to serialize.</param>
        ''' <returns>A NetworkMessage with the serialized version payload.</returns>
        Public Function SerializeVersion(versionMsg As VersionMessage) As NetworkMessage
            If versionMsg Is Nothing Then Throw New ArgumentNullException(NameOf(versionMsg))
            Return New NetworkMessage(_magic, NetworkCommands.Version, versionMsg.Serialize())
        End Function

        ''' <summary>
        ''' Serializes a BlockMessage into a complete NetworkMessage.
        ''' </summary>
        ''' <param name="blockMsg">The block message to serialize.</param>
        ''' <returns>A NetworkMessage with the serialized block payload.</returns>
        Public Function SerializeBlock(blockMsg As BlockMessage) As NetworkMessage
            If blockMsg Is Nothing Then Throw New ArgumentNullException(NameOf(blockMsg))
            Return New NetworkMessage(_magic, NetworkCommands.Block, blockMsg.Serialize())
        End Function

        ''' <summary>
        ''' Serializes a TransactionMessage into a complete NetworkMessage.
        ''' </summary>
        ''' <param name="txMsg">The transaction message to serialize.</param>
        ''' <returns>A NetworkMessage with the serialized transaction payload.</returns>
        Public Function SerializeTransaction(txMsg As TransactionMessage) As NetworkMessage
            If txMsg Is Nothing Then Throw New ArgumentNullException(NameOf(txMsg))
            Return New NetworkMessage(_magic, NetworkCommands.Tx, txMsg.Serialize())
        End Function

        ''' <summary>
        ''' Serializes an InventoryMessage into a complete NetworkMessage.
        ''' </summary>
        ''' <param name="invMsg">The inventory message to serialize.</param>
        ''' <returns>A NetworkMessage with the serialized inventory payload.</returns>
        Public Function SerializeInventory(invMsg As InventoryMessage) As NetworkMessage
            If invMsg Is Nothing Then Throw New ArgumentNullException(NameOf(invMsg))
            Return New NetworkMessage(_magic, NetworkCommands.Inv, invMsg.Serialize())
        End Function

        ''' <summary>
        ''' Serializes a GetBlocksMessage into a complete NetworkMessage.
        ''' </summary>
        ''' <param name="getBlocksMsg">The getblocks message to serialize.</param>
        ''' <returns>A NetworkMessage with the serialized getblocks payload.</returns>
        Public Function SerializeGetBlocks(getBlocksMsg As GetBlocksMessage) As NetworkMessage
            If getBlocksMsg Is Nothing Then Throw New ArgumentNullException(NameOf(getBlocksMsg))
            Return New NetworkMessage(_magic, NetworkCommands.GetBlocks, getBlocksMsg.Serialize())
        End Function

        ''' <summary>
        ''' Serializes a GetDataMessage into a complete NetworkMessage.
        ''' </summary>
        ''' <param name="getDataMsg">The getdata message to serialize.</param>
        ''' <returns>A NetworkMessage with the serialized getdata payload.</returns>
        Public Function SerializeGetData(getDataMsg As GetDataMessage) As NetworkMessage
            If getDataMsg Is Nothing Then Throw New ArgumentNullException(NameOf(getDataMsg))
            Return New NetworkMessage(_magic, NetworkCommands.GetData, getDataMsg.Serialize())
        End Function

        ''' <summary>
        ''' Serializes a PingPongMessage into a complete NetworkMessage.
        ''' </summary>
        ''' <param name="pingPongMsg">The ping/pong message to serialize.</param>
        ''' <returns>A NetworkMessage with the serialized ping/pong payload.</returns>
        Public Function SerializePingPong(pingPongMsg As PingPongMessage) As NetworkMessage
            If pingPongMsg Is Nothing Then Throw New ArgumentNullException(NameOf(pingPongMsg))
            Dim command As String = If(pingPongMsg.IsPing, NetworkCommands.Ping, NetworkCommands.Pong)
            Return New NetworkMessage(_magic, command, pingPongMsg.Serialize())
        End Function

        ''' <summary>
        ''' Serializes an AddrMessage into a complete NetworkMessage.
        ''' </summary>
        ''' <param name="addrMsg">The addr message to serialize.</param>
        ''' <returns>A NetworkMessage with the serialized addr payload.</returns>
        Public Function SerializeAddr(addrMsg As AddrMessage) As NetworkMessage
            If addrMsg Is Nothing Then Throw New ArgumentNullException(NameOf(addrMsg))
            Return New NetworkMessage(_magic, NetworkCommands.Addr, addrMsg.Serialize())
        End Function

        ''' <summary>
        ''' Creates a verack message (empty payload).
        ''' </summary>
        ''' <returns>A NetworkMessage with the "verack" command and empty payload.</returns>
        Public Function CreateVerAck() As NetworkMessage
            Return New NetworkMessage(_magic, NetworkCommands.VerAck, Array.Empty(Of Byte)())
        End Function

        ''' <summary>
        ''' Creates a getaddr message (empty payload).
        ''' </summary>
        ''' <returns>A NetworkMessage with the "getaddr" command and empty payload.</returns>
        Public Function CreateGetAddr() As NetworkMessage
            Return New NetworkMessage(_magic, NetworkCommands.GetAddr, Array.Empty(Of Byte)())
        End Function

        ''' <summary>
        ''' Deserializes a typed message from a raw NetworkMessage based on its command.
        ''' </summary>
        ''' <param name="message">The raw network message to deserialize.</param>
        ''' <returns>The deserialized typed message object, or Nothing if unknown.</returns>
        Public Function DeserializePayload(message As NetworkMessage) As Object
            If message Is Nothing Then Return Nothing
            If message.Payload Is Nothing Then Return Nothing

            Select Case message.Command.ToLowerInvariant()
                Case NetworkCommands.Version
                    Return VersionMessage.Deserialize(message.Payload)
                Case NetworkCommands.Block
                    Return BlockMessage.Deserialize(message.Payload)
                Case NetworkCommands.Tx
                    Return TransactionMessage.Deserialize(message.Payload)
                Case NetworkCommands.Inv
                    Return InventoryMessage.Deserialize(message.Payload)
                Case NetworkCommands.GetBlocks
                    Return GetBlocksMessage.Deserialize(message.Payload)
                Case NetworkCommands.GetData
                    Return GetDataMessage.Deserialize(message.Payload)
                Case NetworkCommands.Ping
                    Return PingPongMessage.Deserialize(message.Payload, True)
                Case NetworkCommands.Pong
                    Return PingPongMessage.Deserialize(message.Payload, False)
                Case NetworkCommands.Addr
                    Return AddrMessage.Deserialize(message.Payload)
                Case Else
                    Return Nothing
            End Select
        End Function

        ''' <summary>
        ''' Validates a raw network message including magic bytes, payload size, and checksum.
        ''' </summary>
        ''' <param name="message">The network message to validate.</param>
        ''' <returns>A validation result with any errors found.</returns>
        Public Function ValidateMessage(message As NetworkMessage) As MessageValidationResult
            Dim result As New MessageValidationResult()

            If message Is Nothing Then
                result.AddError("Message is null.")
                Return result
            End If

            ' Validate magic bytes
            If Not message.ValidateMagic(_magic) Then
                result.AddError("Invalid magic bytes - message may be from a different network.")
            End If

            ' Validate command
            If String.IsNullOrEmpty(message.Command) Then
                result.AddError("Empty command name.")
            ElseIf message.Command.Length > 12 Then
                result.AddError("Command name exceeds 12 characters.")
            End If

            ' Validate payload size
            If Not message.IsPayloadSizeValid() Then
                result.AddError($"Payload size {message.PayloadLength} exceeds maximum {NetworkMessage.MaxPayloadSize}.")
            End If

            ' Validate checksum (only if payload is present)
            If message.Payload IsNot Nothing AndAlso message.Payload.Length > 0 Then
                If Not message.ValidateChecksum() Then
                    result.AddError("Checksum mismatch - payload may be corrupted.")
                End If
            End If

            ' Validate payload length matches actual payload
            If message.Payload IsNot Nothing Then
                If message.Payload.Length <> message.PayloadLength Then
                    result.AddError($"Payload length mismatch: header says {message.PayloadLength}, actual is {message.Payload.Length}.")
                End If
            End If

            Return result
        End Function

        ''' <summary>
        ''' Computes the checksum for a given payload.
        ''' </summary>
        ''' <param name="payload">The payload data.</param>
        ''' <returns>The 4-byte checksum.</returns>
        Public Shared Function ComputeChecksum(payload As Byte()) As Byte()
            Return NetworkMessage.ComputeChecksum(payload)
        End Function

        ''' <summary>
        ''' Checks if a command string is a known/supported command.
        ''' </summary>
        ''' <param name="command">The command to check.</param>
        ''' <returns>True if the command is recognized.</returns>
        Public Shared Function IsKnownCommand(command As String) As Boolean
            If String.IsNullOrEmpty(command) Then Return False
            Select Case command.ToLowerInvariant()
                Case NetworkCommands.Version, NetworkCommands.VerAck,
                     NetworkCommands.Ping, NetworkCommands.Pong,
                     NetworkCommands.GetBlocks, NetworkCommands.GetData,
                     NetworkCommands.Inv, NetworkCommands.Block,
                     NetworkCommands.Tx, NetworkCommands.Addr,
                     NetworkCommands.GetAddr, NetworkCommands.GetHeaders,
                     NetworkCommands.Headers, NetworkCommands.Reject
                    Return True
                Case Else
                    Return False
            End Select
        End Function

    End Class

    ''' <summary>
    ''' Result of network message validation.
    ''' </summary>
    Public Class MessageValidationResult

        ''' <summary>
        ''' List of validation errors found.
        ''' </summary>
        Public Property Errors As List(Of String)

        ''' <summary>
        ''' Whether the message passed all validation checks.
        ''' </summary>
        Public ReadOnly Property IsValid As Boolean
            Get
                Return Errors.Count = 0
            End Get
        End Property

        Public Sub New()
            Errors = New List(Of String)()
        End Sub

        ''' <summary>
        ''' Adds a validation error.
        ''' </summary>
        ''' <param name="message">The error description.</param>
        Public Sub AddError(message As String)
            Errors.Add(message)
        End Sub

        Public Overrides Function ToString() As String
            If IsValid Then Return "Valid"
            Return $"Invalid: {String.Join("; ", Errors)}"
        End Function

    End Class

End Namespace
