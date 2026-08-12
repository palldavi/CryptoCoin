Imports System.Net

Namespace CryptoCoin.Networking

    ''' <summary>
    ''' Represents a single network address entry with timestamp and services.
    ''' </summary>
    Public Class NetworkAddress

        ''' <summary>
        ''' Unix timestamp when this address was last seen active.
        ''' </summary>
        Public Property Timestamp As Long

        ''' <summary>
        ''' Services offered by the node at this address.
        ''' </summary>
        Public Property Services As PeerServices

        ''' <summary>
        ''' The IP address of the node.
        ''' </summary>
        Public Property Address As IPAddress

        ''' <summary>
        ''' The TCP port of the node.
        ''' </summary>
        Public Property Port As Integer

        ''' <summary>
        ''' Creates a new empty NetworkAddress.
        ''' </summary>
        Public Sub New()
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            Services = PeerServices.None
            Address = IPAddress.None
            Port = 8333
        End Sub

        ''' <summary>
        ''' Creates a NetworkAddress with the specified parameters.
        ''' </summary>
        ''' <param name="address">The IP address.</param>
        ''' <param name="port">The TCP port.</param>
        ''' <param name="services">The advertised services.</param>
        Public Sub New(address As IPAddress, port As Integer, services As PeerServices)
            Me.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            Me.Services = services
            Me.Address = address
            Me.Port = port
        End Sub

        ''' <summary>
        ''' The age of this address entry in seconds.
        ''' </summary>
        Public ReadOnly Property AgeSeconds As Long
            Get
                Return DateTimeOffset.UtcNow.ToUnixTimeSeconds() - Timestamp
            End Get
        End Property

        ''' <summary>
        ''' Whether this address is considered fresh (seen within the last 3 hours).
        ''' </summary>
        Public ReadOnly Property IsFresh As Boolean
            Get
                Return AgeSeconds < 10800 ' 3 hours
            End Get
        End Property

        ''' <summary>
        ''' Serializes this network address to bytes (30 bytes).
        ''' </summary>
        ''' <returns>The serialized bytes.</returns>
        Public Function Serialize() As Byte()
            Dim result(29) As Byte
            Dim offset As Integer = 0

            ' Timestamp (4 bytes)
            Dim tsBytes As Byte() = BitConverter.GetBytes(CUInt(Timestamp))
            Array.Copy(tsBytes, 0, result, offset, 4)
            offset += 4

            ' Services (8 bytes)
            Dim svcBytes As Byte() = BitConverter.GetBytes(CULng(Services))
            Array.Copy(svcBytes, 0, result, offset, 8)
            offset += 8

            ' IP address (16 bytes, IPv6-mapped)
            Dim addrBytes As Byte() = SerializeIpAddress(Address)
            Array.Copy(addrBytes, 0, result, offset, 16)
            offset += 16

            ' Port (2 bytes, big-endian)
            result(offset) = CByte((Port >> 8) And &HFF)
            result(offset + 1) = CByte(Port And &HFF)

            Return result
        End Function

        ''' <summary>
        ''' Deserializes a network address from bytes at the given offset.
        ''' </summary>
        ''' <param name="data">The source byte array.</param>
        ''' <param name="offset">The offset to start reading from.</param>
        ''' <returns>A populated NetworkAddress.</returns>
        Public Shared Function Deserialize(data As Byte(), offset As Integer) As NetworkAddress
            If data Is Nothing OrElse offset + 30 > data.Length Then
                Throw New ArgumentException("Insufficient data for network address.")
            End If

            Dim addr As New NetworkAddress()
            addr.Timestamp = CLng(BitConverter.ToUInt32(data, offset)) : offset += 4
            addr.Services = CType(BitConverter.ToUInt64(data, offset), PeerServices) : offset += 8
            addr.Address = DeserializeIpAddress(data, offset) : offset += 16
            addr.Port = (CInt(data(offset)) << 8) Or CInt(data(offset + 1))

            Return addr
        End Function

        Private Shared Function SerializeIpAddress(addr As IPAddress) As Byte()
            Dim result(15) As Byte
            If addr.AddressFamily = Sockets.AddressFamily.InterNetwork Then
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

        Private Shared Function DeserializeIpAddress(data As Byte(), offset As Integer) As IPAddress
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

        Public Overrides Function ToString() As String
            Return $"NetworkAddress({Address}:{Port}, Services={Services})"
        End Function

    End Class

    ''' <summary>
    ''' Address announcement message containing peer addresses.
    ''' Used to propagate knowledge of active nodes through the network.
    ''' Nodes periodically relay addresses they know about to help peers
    ''' discover new connections.
    ''' </summary>
    Public Class AddrMessage

        ''' <summary>
        ''' Maximum number of addresses in a single addr message.
        ''' </summary>
        Public Const MaxAddresses As Integer = 1000

        ''' <summary>
        ''' Maximum age of an address to be relayed (10 days in seconds).
        ''' </summary>
        Public Const MaxAddressAge As Long = 864000

        ''' <summary>
        ''' The list of network addresses being announced.
        ''' </summary>
        Public Property Addresses As List(Of NetworkAddress)

        ''' <summary>
        ''' The number of addresses in this message.
        ''' </summary>
        Public ReadOnly Property Count As Integer
            Get
                If Addresses Is Nothing Then Return 0
                Return Addresses.Count
            End Get
        End Property

        ''' <summary>
        ''' Creates a new empty AddrMessage.
        ''' </summary>
        Public Sub New()
            Addresses = New List(Of NetworkAddress)()
        End Sub

        ''' <summary>
        ''' Creates an AddrMessage with the specified addresses.
        ''' </summary>
        ''' <param name="addresses">The network addresses to include.</param>
        Public Sub New(addresses As IEnumerable(Of NetworkAddress))
            Me.Addresses = New List(Of NetworkAddress)(addresses)
        End Sub

        ''' <summary>
        ''' Adds an address to the message if the limit has not been reached.
        ''' </summary>
        ''' <param name="address">The network address to add.</param>
        ''' <returns>True if the address was added.</returns>
        Public Function AddAddress(address As NetworkAddress) As Boolean
            If Addresses.Count >= MaxAddresses Then Return False
            Addresses.Add(address)
            Return True
        End Function

        ''' <summary>
        ''' Gets only fresh addresses (seen within the last 3 hours).
        ''' </summary>
        ''' <returns>A list of fresh network addresses.</returns>
        Public Function GetFreshAddresses() As List(Of NetworkAddress)
            Dim result As New List(Of NetworkAddress)()
            For Each addr As Object In Addresses
                If addr.IsFresh Then
                    result.Add(addr)
                End If
            Next
            Return result
        End Function

        ''' <summary>
        ''' Filters out addresses that are too old to relay.
        ''' </summary>
        ''' <returns>A list of addresses suitable for relay.</returns>
        Public Function GetRelayableAddresses() As List(Of NetworkAddress)
            Dim result As New List(Of NetworkAddress)()
            For Each addr As Object In Addresses
                If addr.AgeSeconds <= MaxAddressAge Then
                    result.Add(addr)
                End If
            Next
            Return result
        End Function

        ''' <summary>
        ''' Serializes the addr message to a byte array payload.
        ''' </summary>
        ''' <returns>The serialized payload bytes.</returns>
        Public Function Serialize() As Byte()
            Dim parts As New List(Of Byte())()

            ' Address count (4 bytes)
            parts.Add(BitConverter.GetBytes(Addresses.Count))

            ' Network addresses (30 bytes each)
            For Each addr As Object In Addresses
                parts.Add(addr.Serialize())
            Next

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
        ''' Deserializes an addr message from a byte array payload.
        ''' </summary>
        ''' <param name="data">The payload bytes to deserialize.</param>
        ''' <returns>A populated AddrMessage instance.</returns>
        Public Shared Function Deserialize(data As Byte()) As AddrMessage
            If data Is Nothing OrElse data.Length < 4 Then
                Throw New ArgumentException("Addr message payload too short.")
            End If

            Dim msg As New AddrMessage()
            Dim offset As Integer = 0

            ' Address count
            Dim count As Integer = BitConverter.ToInt32(data, offset)
            offset += 4

            If count < 0 OrElse count > MaxAddresses Then
                Throw New ArgumentException($"Invalid address count: {count}")
            End If

            ' Read addresses
            For i As Integer = 0 To count - 1
                If offset + 30 > data.Length Then Exit For
                Dim addr As NetworkAddress = NetworkAddress.Deserialize(data, offset)
                msg.Addresses.Add(addr)
                offset += 30
            Next

            Return msg
        End Function

        ''' <summary>
        ''' Validates the message structure.
        ''' </summary>
        ''' <returns>True if the message is structurally valid.</returns>
        Public Function ValidateStructure() As Boolean
            If Addresses Is Nothing Then Return False
            If Addresses.Count > MaxAddresses Then Return False
            Return True
        End Function

        ''' <summary>
        ''' Wraps this message in a NetworkMessage for transmission.
        ''' </summary>
        ''' <returns>A NetworkMessage with the "addr" command.</returns>
        Public Function ToNetworkMessage() As NetworkMessage
            Return New NetworkMessage(NetworkCommands.Addr, Serialize())
        End Function

        Public Overrides Function ToString() As String
            Return $"AddrMessage(Count={Count})"
        End Function

    End Class

End Namespace
