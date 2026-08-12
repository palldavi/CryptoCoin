Namespace CryptoCoin.Networking

    ''' <summary>
    ''' Request message for block hashes starting from a set of locator hashes.
    ''' The receiving peer responds with an INV message containing block hashes
    ''' that follow the locator in the best chain, up to the stop hash or 500 blocks.
    ''' </summary>
    Public Class GetBlocksMessage

        ''' <summary>
        ''' Maximum number of block locator hashes allowed.
        ''' </summary>
        Public Const MaxLocatorHashes As Integer = 101

        ''' <summary>
        ''' Maximum number of block hashes to return in response.
        ''' </summary>
        Public Const MaxResponseHashes As Integer = 500

        ''' <summary>
        ''' The protocol version of the requesting node.
        ''' </summary>
        Public Property ProtocolVersion As Integer

        ''' <summary>
        ''' Block locator hashes, ordered from highest to lowest height.
        ''' The first hash the receiver recognizes determines the starting point.
        ''' Typically includes exponentially spaced hashes for efficient fork detection.
        ''' </summary>
        Public Property BlockLocatorHashes As List(Of String)

        ''' <summary>
        ''' Hash of the last desired block. Set to all zeros to get as many as possible.
        ''' </summary>
        Public Property HashStop As String

        ''' <summary>
        ''' The number of locator hashes in this message.
        ''' </summary>
        Public ReadOnly Property LocatorCount As Integer
            Get
                If BlockLocatorHashes Is Nothing Then Return 0
                Return BlockLocatorHashes.Count
            End Get
        End Property

        ''' <summary>
        ''' Creates a new empty GetBlocksMessage.
        ''' </summary>
        Public Sub New()
            ProtocolVersion = 70015
            BlockLocatorHashes = New List(Of String)()
            HashStop = New String("0"c, 64)
        End Sub

        ''' <summary>
        ''' Creates a GetBlocksMessage with the specified locator hashes.
        ''' </summary>
        ''' <param name="locatorHashes">The block locator hashes from high to low.</param>
        ''' <param name="stopHash">The hash to stop at, or all zeros for maximum.</param>
        Public Sub New(locatorHashes As IEnumerable(Of String), Optional stopHash As String = Nothing)
            ProtocolVersion = 70015
            BlockLocatorHashes = New List(Of String)(locatorHashes)
            HashStop = If(stopHash, New String("0"c, 64))
        End Sub

        ''' <summary>
        ''' Builds a block locator from a chain of block hashes at various heights.
        ''' Uses exponential back-off: includes hashes at heights n, n-1, n-2, n-3,
        ''' n-4, n-5, n-6, n-7, n-8, n-10, n-14, n-22, n-38, ..., 0.
        ''' </summary>
        ''' <param name="tipHeight">The current tip height.</param>
        ''' <param name="getHashAtHeight">Function that returns the block hash at a given height.</param>
        ''' <returns>A configured GetBlocksMessage with the locator.</returns>
        Public Shared Function BuildLocator(tipHeight As Integer,
                                            getHashAtHeight As Func(Of Integer, String)) As GetBlocksMessage
            Dim msg As New GetBlocksMessage()
            Dim height As Integer = tipHeight
            Dim [step] As Integer = 1

            While height >= 0
                Dim hash As String = getHashAtHeight(height)
                If Not String.IsNullOrEmpty(hash) Then
                    msg.BlockLocatorHashes.Add(hash)
                End If

                If msg.BlockLocatorHashes.Count >= 10 Then
                    [step] *= 2
                End If

                height -= [step]

                If msg.BlockLocatorHashes.Count >= MaxLocatorHashes Then
                    Exit While
                End If
            End While

            ' Always include genesis
            If height <> 0 AndAlso msg.BlockLocatorHashes.Count < MaxLocatorHashes Then
                Dim genesisHash As String = getHashAtHeight(0)
                If Not String.IsNullOrEmpty(genesisHash) Then
                    msg.BlockLocatorHashes.Add(genesisHash)
                End If
            End If

            Return msg
        End Function

        ''' <summary>
        ''' Serializes the getblocks message to a byte array payload.
        ''' </summary>
        ''' <returns>The serialized payload bytes.</returns>
        Public Function Serialize() As Byte()
            Dim parts As New List(Of Byte())()

            ' Protocol version (4 bytes)
            parts.Add(BitConverter.GetBytes(ProtocolVersion))

            ' Locator hash count (4 bytes)
            parts.Add(BitConverter.GetBytes(BlockLocatorHashes.Count))

            ' Locator hashes (32 bytes each)
            For Each hash As Object In BlockLocatorHashes
                parts.Add(HexToBytes(hash.PadLeft(64, "0"c)))
            Next

            ' Stop hash (32 bytes)
            parts.Add(HexToBytes(HashStop.PadLeft(64, "0"c)))

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
        ''' Deserializes a getblocks message from a byte array payload.
        ''' </summary>
        ''' <param name="data">The payload bytes to deserialize.</param>
        ''' <returns>A populated GetBlocksMessage instance.</returns>
        Public Shared Function Deserialize(data As Byte()) As GetBlocksMessage
            If data Is Nothing OrElse data.Length < 40 Then
                Throw New ArgumentException("GetBlocks message payload too short.")
            End If

            Dim msg As New GetBlocksMessage()
            Dim offset As Integer = 0

            ' Protocol version
            msg.ProtocolVersion = BitConverter.ToInt32(data, offset)
            offset += 4

            ' Locator hash count
            Dim count As Integer = BitConverter.ToInt32(data, offset)
            offset += 4

            If count < 0 OrElse count > MaxLocatorHashes Then
                Throw New ArgumentException($"Invalid locator count: {count}")
            End If

            ' Locator hashes
            msg.BlockLocatorHashes = New List(Of String)()
            For i As Integer = 0 To count - 1
                If offset + 32 > data.Length Then Exit For
                Dim hashBytes(31) As Byte
                Array.Copy(data, offset, hashBytes, 0, 32)
                msg.BlockLocatorHashes.Add(BytesToHex(hashBytes))
                offset += 32
            Next

            ' Stop hash
            If offset + 32 <= data.Length Then
                Dim stopBytes(31) As Byte
                Array.Copy(data, offset, stopBytes, 0, 32)
                msg.HashStop = BytesToHex(stopBytes)
            End If

            Return msg
        End Function

        ''' <summary>
        ''' Validates the message structure.
        ''' </summary>
        ''' <returns>True if the message is structurally valid.</returns>
        Public Function ValidateStructure() As Boolean
            If BlockLocatorHashes Is Nothing Then Return False
            If BlockLocatorHashes.Count = 0 Then Return False
            If BlockLocatorHashes.Count > MaxLocatorHashes Then Return False
            If String.IsNullOrEmpty(HashStop) OrElse HashStop.Length <> 64 Then Return False
            Return True
        End Function

        ''' <summary>
        ''' Wraps this message in a NetworkMessage for transmission.
        ''' </summary>
        ''' <returns>A NetworkMessage with the "getblocks" command.</returns>
        Public Function ToNetworkMessage() As NetworkMessage
            Return New NetworkMessage(NetworkCommands.GetBlocks, Serialize())
        End Function

        Private Shared Function HexToBytes(hex As String) As Byte()
            Dim bytes(hex.Length \ 2 - 1) As Byte
            For i As Integer = 0 To bytes.Length - 1
                bytes(i) = Convert.ToByte(hex.Substring(i * 2, 2), 16)
            Next
            Return bytes
        End Function

        Private Shared Function BytesToHex(bytes As Byte()) As String
            Dim sb As New System.Text.StringBuilder(bytes.Length * 2)
            For Each b As Object In bytes
                sb.Append(b.ToString("x2"))
            Next
            Return sb.ToString()
        End Function

        Public Overrides Function ToString() As String
            Return $"GetBlocksMessage(Locators={LocatorCount}, Stop={HashStop.Substring(0, 16)}...)"
        End Function

    End Class

End Namespace
