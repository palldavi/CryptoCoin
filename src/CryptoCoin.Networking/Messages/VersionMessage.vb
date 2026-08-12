Imports System.Net
Imports System.Text

Namespace CryptoCoin.Networking

    ''' <summary>
    ''' Version handshake message exchanged when two peers first connect.
    ''' Contains protocol version, services, timestamp, and user agent information.
    ''' This is always the first message sent after establishing a TCP connection.
    ''' </summary>
    Public Class VersionMessage

        ''' <summary>
        ''' The current protocol version supported by this node.
        ''' </summary>
        Public Const CurrentProtocolVersion As Integer = 70015

        ''' <summary>
        ''' The protocol version number of the sending node.
        ''' </summary>
        Public Property ProtocolVersion As Integer

        ''' <summary>
        ''' Bitfield of services offered by the sending node.
        ''' </summary>
        Public Property Services As PeerServices

        ''' <summary>
        ''' Unix timestamp at the time the message was generated.
        ''' </summary>
        Public Property Timestamp As Long

        ''' <summary>
        ''' The services expected from the receiving node.
        ''' </summary>
        Public Property ReceiverServices As PeerServices

        ''' <summary>
        ''' The IP address of the receiving node as seen by the sender.
        ''' </summary>
        Public Property ReceiverAddress As IPAddress

        ''' <summary>
        ''' The port of the receiving node.
        ''' </summary>
        Public Property ReceiverPort As Integer

        ''' <summary>
        ''' The IP address of the sending node.
        ''' </summary>
        Public Property SenderAddress As IPAddress

        ''' <summary>
        ''' The port of the sending node.
        ''' </summary>
        Public Property SenderPort As Integer

        ''' <summary>
        ''' A random nonce used to detect self-connections.
        ''' </summary>
        Public Property Nonce As ULong

        ''' <summary>
        ''' User agent string identifying the software (e.g., "/CryptoCoin:1.0.0/").
        ''' </summary>
        Public Property UserAgent As String

        ''' <summary>
        ''' The best block height known to the sending node.
        ''' </summary>
        Public Property StartHeight As Integer

        ''' <summary>
        ''' Whether the sender wants to receive relay transactions (BIP 37).
        ''' </summary>
        Public Property Relay As Boolean

        ''' <summary>
        ''' Creates a new VersionMessage with default values.
        ''' </summary>
        Public Sub New()
            ProtocolVersion = CurrentProtocolVersion
            Services = PeerServices.FullNode
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            ReceiverServices = PeerServices.None
            ReceiverAddress = IPAddress.Loopback
            ReceiverPort = 8333
            SenderAddress = IPAddress.Loopback
            SenderPort = 8333
            Nonce = 0UL
            UserAgent = "/CryptoCoin:1.0.0/"
            StartHeight = 0
            Relay = True
        End Sub

        ''' <summary>
        ''' Creates a VersionMessage for initiating a connection to a remote peer.
        ''' </summary>
        ''' <param name="localHeight">The local node's best block height.</param>
        ''' <param name="localServices">The services offered by the local node.</param>
        ''' <param name="remoteAddress">The remote peer's IP address.</param>
        ''' <param name="remotePort">The remote peer's port.</param>
        ''' <returns>A configured VersionMessage ready to send.</returns>
        Public Shared Function CreateOutgoing(localHeight As Integer, localServices As PeerServices,
                                              remoteAddress As IPAddress, remotePort As Integer) As VersionMessage
            Dim msg As New VersionMessage()
            msg.StartHeight = localHeight
            msg.Services = localServices
            msg.ReceiverAddress = remoteAddress
            msg.ReceiverPort = remotePort
            msg.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()

            ' Generate random nonce
            Dim rng As New Random()
            Dim nonceBytes(7) As Byte
            rng.NextBytes(nonceBytes)
            msg.Nonce = BitConverter.ToUInt64(nonceBytes, 0)

            Return msg
        End Function

        ''' <summary>
        ''' Serializes the version message to a byte array payload.
        ''' </summary>
        ''' <returns>The serialized payload bytes.</returns>
        Public Function Serialize() As Byte()
            Dim parts As New List(Of Byte())()

            ' Protocol version (4 bytes)
            parts.Add(BitConverter.GetBytes(ProtocolVersion))

            ' Services (8 bytes)
            parts.Add(BitConverter.GetBytes(CULng(Services)))

            ' Timestamp (8 bytes)
            parts.Add(BitConverter.GetBytes(Timestamp))

            ' Receiver services (8 bytes)
            parts.Add(BitConverter.GetBytes(CULng(ReceiverServices)))

            ' Receiver address (16 bytes, IPv6-mapped IPv4)
            parts.Add(SerializeAddress(ReceiverAddress))

            ' Receiver port (2 bytes, big-endian)
            parts.Add(SerializePort(ReceiverPort))

            ' Sender services (8 bytes) - same as Services
            parts.Add(BitConverter.GetBytes(CULng(Services)))

            ' Sender address (16 bytes)
            parts.Add(SerializeAddress(SenderAddress))

            ' Sender port (2 bytes, big-endian)
            parts.Add(SerializePort(SenderPort))

            ' Nonce (8 bytes)
            parts.Add(BitConverter.GetBytes(Nonce))

            ' User agent (varint length + string)
            Dim agentBytes As Byte() = Encoding.UTF8.GetBytes(If(UserAgent, ""))
            parts.Add(EncodeVarInt(agentBytes.Length))
            parts.Add(agentBytes)

            ' Start height (4 bytes)
            parts.Add(BitConverter.GetBytes(StartHeight))

            ' Relay (1 byte)
            parts.Add(New Byte() {If(Relay, CByte(1), CByte(0))})

            ' Combine all parts
            Dim totalSize As Integer = 0
            For Each p As Object In parts
                totalSize += p.Length
            Next

            Dim result(totalSize - 1) As Byte
            Dim offset As Integer = 0
            For Each p As Object In parts
                Array.Copy(p, 0, result, offset, p.Length)
                offset += p.Length
            Next

            Return result
        End Function

        ''' <summary>
        ''' Deserializes a version message from a byte array payload.
        ''' </summary>
        ''' <param name="data">The payload bytes to deserialize.</param>
        ''' <returns>A populated VersionMessage instance.</returns>
        Public Shared Function Deserialize(data As Byte()) As VersionMessage
            If data Is Nothing OrElse data.Length < 46 Then
                Throw New ArgumentException("Version message payload too short.")
            End If

            Dim msg As New VersionMessage()
            Dim offset As Integer = 0

            ' Protocol version
            msg.ProtocolVersion = BitConverter.ToInt32(data, offset) : offset += 4

            ' Services
            msg.Services = CType(BitConverter.ToUInt64(data, offset), PeerServices) : offset += 8

            ' Timestamp
            msg.Timestamp = BitConverter.ToInt64(data, offset) : offset += 8

            ' Receiver services
            msg.ReceiverServices = CType(BitConverter.ToUInt64(data, offset), PeerServices) : offset += 8

            ' Receiver address (16 bytes)
            msg.ReceiverAddress = DeserializeAddress(data, offset) : offset += 16

            ' Receiver port (2 bytes, big-endian)
            msg.ReceiverPort = DeserializePort(data, offset) : offset += 2

            ' Skip sender services (8 bytes)
            offset += 8

            ' Sender address (16 bytes)
            msg.SenderAddress = DeserializeAddress(data, offset) : offset += 16

            ' Sender port (2 bytes, big-endian)
            msg.SenderPort = DeserializePort(data, offset) : offset += 2

            ' Nonce
            msg.Nonce = BitConverter.ToUInt64(data, offset) : offset += 8

            ' User agent
            If offset < data.Length Then
                Dim agentLen As Integer = 0
                Dim varIntSize As Integer = DecodeVarInt(data, offset, agentLen)
                offset += varIntSize
                If agentLen > 0 AndAlso offset + agentLen <= data.Length Then
                    msg.UserAgent = Encoding.UTF8.GetString(data, offset, agentLen)
                    offset += agentLen
                End If
            End If

            ' Start height
            If offset + 4 <= data.Length Then
                msg.StartHeight = BitConverter.ToInt32(data, offset) : offset += 4
            End If

            ' Relay
            If offset < data.Length Then
                msg.Relay = data(offset) <> 0
            End If

            Return msg
        End Function

        ''' <summary>
        ''' Wraps this version message in a NetworkMessage for transmission.
        ''' </summary>
        ''' <returns>A NetworkMessage with the "version" command.</returns>
        Public Function ToNetworkMessage() As NetworkMessage
            Return New NetworkMessage(NetworkCommands.Version, Serialize())
        End Function

        Private Shared Function SerializeAddress(addr As IPAddress) As Byte()
            Dim result(15) As Byte
            If addr.AddressFamily = Sockets.AddressFamily.InterNetwork Then
                ' IPv4-mapped IPv6: ::ffff:x.x.x.x
                result(10) = &HFF
                result(11) = &HFF
                Dim ipv4Bytes As Byte() = addr.GetAddressBytes()
                Array.Copy(ipv4Bytes, 0, result, 12, 4)
            Else
                Dim ipv6Bytes As Byte() = addr.GetAddressBytes()
                Array.Copy(ipv6Bytes, 0, result, 0, Math.Min(16, ipv6Bytes.Length))
            End If
            Return result
        End Function

        Private Shared Function DeserializeAddress(data As Byte(), offset As Integer) As IPAddress
            ' Check if IPv4-mapped
            If data(offset + 10) = &HFF AndAlso data(offset + 11) = &HFF Then
                Dim ipv4(3) As Byte
                Array.Copy(data, offset + 12, ipv4, 0, 4)
                Return New IPAddress(ipv4)
            Else
                Dim ipv6(15) As Byte
                Array.Copy(data, offset, ipv6, 0, 16)
                Return New IPAddress(ipv6)
            End If
        End Function

        Private Shared Function SerializePort(port As Integer) As Byte()
            ' Big-endian port
            Return New Byte() {CByte((port >> 8) And &HFF), CByte(port And &HFF)}
        End Function

        Private Shared Function DeserializePort(data As Byte(), offset As Integer) As Integer
            Return (CInt(data(offset)) << 8) Or CInt(data(offset + 1))
        End Function

        Private Shared Function EncodeVarInt(value As Integer) As Byte()
            If value < 253 Then
                Return New Byte() {CByte(value)}
            ElseIf value <= &HFFFF Then
                Dim result(2) As Byte
                result(0) = 253
                Array.Copy(BitConverter.GetBytes(CUShort(value)), 0, result, 1, 2)
                Return result
            Else
                Dim result(4) As Byte
                result(0) = 254
                Array.Copy(BitConverter.GetBytes(CUInt(value)), 0, result, 1, 4)
                Return result
            End If
        End Function

        Private Shared Function DecodeVarInt(data As Byte(), offset As Integer, ByRef value As Integer) As Integer
            If data(offset) < 253 Then
                value = CInt(data(offset))
                Return 1
            ElseIf data(offset) = 253 Then
                value = CInt(BitConverter.ToUInt16(data, offset + 1))
                Return 3
            ElseIf data(offset) = 254 Then
                value = CInt(BitConverter.ToUInt32(data, offset + 1))
                Return 5
            Else
                value = CInt(BitConverter.ToUInt32(data, offset + 1))
                Return 9
            End If
        End Function

        Public Overrides Function ToString() As String
            Return $"VersionMessage(Version={ProtocolVersion}, Agent={UserAgent}, Height={StartHeight}, Services={Services})"
        End Function

    End Class

End Namespace
