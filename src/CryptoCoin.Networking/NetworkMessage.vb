Imports CryptoCoin.Cryptography

Namespace CryptoCoin.Networking

    ''' <summary>
    ''' Defines the known network message command types.
    ''' </summary>
    Public Module NetworkCommands
        Public Const Version As String = "version"
        Public Const VerAck As String = "verack"
        Public Const Ping As String = "ping"
        Public Const Pong As String = "pong"
        Public Const GetBlocks As String = "getblocks"
        Public Const GetData As String = "getdata"
        Public Const Inv As String = "inv"
        Public Const Block As String = "block"
        Public Const Tx As String = "tx"
        Public Const Addr As String = "addr"
        Public Const GetAddr As String = "getaddr"
        Public Const GetHeaders As String = "getheaders"
        Public Const Headers As String = "headers"
        Public Const Reject As String = "reject"
    End Module

    ''' <summary>
    ''' Base class for all network messages in the CryptoCoin P2P protocol.
    ''' Each message consists of a header with magic bytes, command name, payload length,
    ''' checksum, and the serialized payload data.
    ''' </summary>
    Public Class NetworkMessage

        ''' <summary>
        ''' Magic bytes identifying the network (mainnet, testnet, etc.).
        ''' Used to detect message boundaries and reject messages from other networks.
        ''' </summary>
        Public Shared ReadOnly MainNetMagic As Byte() = {&HF9, &HBE, &HB4, &HD9}

        ''' <summary>
        ''' Magic bytes for the test network.
        ''' </summary>
        Public Shared ReadOnly TestNetMagic As Byte() = {&H0B, &H11, &H09, &H07}

        ''' <summary>
        ''' Maximum allowed payload size (32 MB).
        ''' </summary>
        Public Const MaxPayloadSize As Integer = 33554432

        ''' <summary>
        ''' Size of the message header in bytes (4 magic + 12 command + 4 length + 4 checksum).
        ''' </summary>
        Public Const HeaderSize As Integer = 24

        ''' <summary>
        ''' The magic bytes for this message's network.
        ''' </summary>
        Public Property Magic As Byte()

        ''' <summary>
        ''' The command name identifying the message type (up to 12 ASCII characters).
        ''' </summary>
        Public Property Command As String

        ''' <summary>
        ''' The length of the payload in bytes.
        ''' </summary>
        Public Property PayloadLength As Integer

        ''' <summary>
        ''' First 4 bytes of the double-SHA256 hash of the payload (integrity check).
        ''' </summary>
        Public Property Checksum As Byte()

        ''' <summary>
        ''' The raw payload data.
        ''' </summary>
        Public Property Payload As Byte()

        ''' <summary>
        ''' Creates a new empty NetworkMessage.
        ''' </summary>
        Public Sub New()
            Magic = MainNetMagic
            Command = String.Empty
            PayloadLength = 0
            Checksum = New Byte(3) {}
            Payload = Array.Empty(Of Byte)()
        End Sub

        ''' <summary>
        ''' Creates a new NetworkMessage with the specified command and payload.
        ''' </summary>
        ''' <param name="command">The command name for this message.</param>
        ''' <param name="payload">The serialized payload data.</param>
        Public Sub New(command As String, payload As Byte())
            Me.Magic = MainNetMagic
            Me.Command = command
            Me.Payload = If(payload, Array.Empty(Of Byte)())
            Me.PayloadLength = Me.Payload.Length
            Me.Checksum = ComputeChecksum(Me.Payload)
        End Sub

        ''' <summary>
        ''' Creates a new NetworkMessage with specified magic, command, and payload.
        ''' </summary>
        ''' <param name="magic">The network magic bytes.</param>
        ''' <param name="command">The command name for this message.</param>
        ''' <param name="payload">The serialized payload data.</param>
        Public Sub New(magic As Byte(), command As String, payload As Byte())
            Me.Magic = magic
            Me.Command = command
            Me.Payload = If(payload, Array.Empty(Of Byte)())
            Me.PayloadLength = Me.Payload.Length
            Me.Checksum = ComputeChecksum(Me.Payload)
        End Sub

        ''' <summary>
        ''' Computes the 4-byte checksum for a payload (first 4 bytes of double-SHA256).
        ''' </summary>
        ''' <param name="data">The payload data to checksum.</param>
        ''' <returns>A 4-byte checksum array.</returns>
        Public Shared Function ComputeChecksum(data As Byte()) As Byte()
            Dim hash As Byte() = HashUtil.DoubleSha256(data)
            Dim result(3) As Byte
            Array.Copy(hash, 0, result, 0, 4)
            Return result
        End Function

        ''' <summary>
        ''' Validates that the checksum matches the payload.
        ''' </summary>
        ''' <returns>True if the checksum is valid.</returns>
        Public Function ValidateChecksum() As Boolean
            If Payload Is Nothing Then Return False
            Dim computed As Byte() = ComputeChecksum(Payload)
            If Checksum Is Nothing OrElse Checksum.Length <> 4 Then Return False
            For i As Integer = 0 To 3
                If computed(i) <> Checksum(i) Then Return False
            Next
            Return True
        End Function

        ''' <summary>
        ''' Validates the magic bytes match the expected network.
        ''' </summary>
        ''' <param name="expectedMagic">The expected magic bytes for the network.</param>
        ''' <returns>True if the magic bytes match.</returns>
        Public Function ValidateMagic(expectedMagic As Byte()) As Boolean
            If Magic Is Nothing OrElse Magic.Length <> 4 Then Return False
            If expectedMagic Is Nothing OrElse expectedMagic.Length <> 4 Then Return False
            For i As Integer = 0 To 3
                If Magic(i) <> expectedMagic(i) Then Return False
            Next
            Return True
        End Function

        ''' <summary>
        ''' Serializes the complete message (header + payload) to bytes for transmission.
        ''' </summary>
        ''' <returns>The serialized message bytes.</returns>
        Public Function Serialize() As Byte()
            Dim totalSize As Integer = HeaderSize + PayloadLength
            Dim buffer(totalSize - 1) As Byte
            Dim offset As Integer = 0

            ' Magic (4 bytes)
            Array.Copy(Magic, 0, buffer, offset, 4)
            offset += 4

            ' Command (12 bytes, null-padded ASCII)
            Dim cmdBytes As Byte() = System.Text.Encoding.ASCII.GetBytes(Command)
            Dim cmdLength As Integer = Math.Min(cmdBytes.Length, 12)
            Array.Copy(cmdBytes, 0, buffer, offset, cmdLength)
            offset += 12

            ' Payload length (4 bytes, little-endian)
            Dim lenBytes As Byte() = BitConverter.GetBytes(PayloadLength)
            Array.Copy(lenBytes, 0, buffer, offset, 4)
            offset += 4

            ' Checksum (4 bytes)
            Array.Copy(Checksum, 0, buffer, offset, 4)
            offset += 4

            ' Payload
            If Payload IsNot Nothing AndAlso Payload.Length > 0 Then
                Array.Copy(Payload, 0, buffer, offset, Payload.Length)
            End If

            Return buffer
        End Function

        ''' <summary>
        ''' Deserializes a message header from the given byte array.
        ''' Does not include the payload; use PayloadLength to read the remaining bytes.
        ''' </summary>
        ''' <param name="headerData">A byte array of at least 24 bytes.</param>
        ''' <returns>A NetworkMessage with header fields populated (payload is empty).</returns>
        Public Shared Function DeserializeHeader(headerData As Byte()) As NetworkMessage
            If headerData Is Nothing OrElse headerData.Length < HeaderSize Then
                Throw New ArgumentException("Header data must be at least 24 bytes.")
            End If

            Dim msg As New NetworkMessage()
            Dim offset As Integer = 0

            ' Magic (4 bytes)
            msg.Magic = New Byte(3) {}
            Array.Copy(headerData, offset, msg.Magic, 0, 4)
            offset += 4

            ' Command (12 bytes, null-terminated ASCII)
            Dim cmdBytes(11) As Byte
            Array.Copy(headerData, offset, cmdBytes, 0, 12)
            offset += 12
            Dim cmdStr As String = System.Text.Encoding.ASCII.GetString(cmdBytes)
            Dim nullIdx As Integer = cmdStr.IndexOf(ChrW(0))
            If nullIdx >= 0 Then cmdStr = cmdStr.Substring(0, nullIdx)
            msg.Command = cmdStr

            ' Payload length (4 bytes)
            msg.PayloadLength = BitConverter.ToInt32(headerData, offset)
            offset += 4

            ' Checksum (4 bytes)
            msg.Checksum = New Byte(3) {}
            Array.Copy(headerData, offset, msg.Checksum, 0, 4)

            Return msg
        End Function

        ''' <summary>
        ''' Checks whether the payload size is within acceptable limits.
        ''' </summary>
        ''' <returns>True if the payload size is valid.</returns>
        Public Function IsPayloadSizeValid() As Boolean
            Return PayloadLength >= 0 AndAlso PayloadLength <= MaxPayloadSize
        End Function

        Public Overrides Function ToString() As String
            Return $"NetworkMessage(Command={Command}, PayloadLength={PayloadLength})"
        End Function

    End Class

End Namespace
