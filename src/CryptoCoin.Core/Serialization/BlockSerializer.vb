Imports CryptoCoin.Cryptography

Namespace CryptoCoin.Core.Serialization

    ''' <summary>
    ''' Serializes and deserializes blocks and block headers for network transmission and storage.
    ''' </summary>
    Public NotInheritable Class BlockSerializer

        Private Sub New()
        End Sub

        ''' <summary>
        ''' Serializes a block header to bytes.
        ''' </summary>
        Public Shared Function SerializeHeader(header As BlockHeader) As Byte()
            If header Is Nothing Then Throw New ArgumentNullException(NameOf(header))

            Dim writer As New BufferWriter(80)
            writer.WriteInt32(header.Version)
            writer.WriteHashFromHex(header.PreviousBlockHash.PadLeft(64, "0"c))
            writer.WriteHashFromHex(header.MerkleRoot.PadLeft(64, "0"c))
            writer.WriteUInt32(CUInt(header.Timestamp))
            writer.WriteUInt32(header.Bits)
            writer.WriteUInt32(header.Nonce)
            Return writer.ToArray()
        End Function

        ''' <summary>
        ''' Deserializes a block header from bytes.
        ''' </summary>
        Public Shared Function DeserializeHeader(data As Byte()) As BlockHeader
            If data Is Nothing OrElse data.Length < 80 Then
                Throw New ArgumentException("Insufficient data for block header.")
            End If

            Dim reader As New BufferReader(data)
            Dim header As New BlockHeader()
            header.Version = reader.ReadInt32()
            header.PreviousBlockHash = reader.ReadHashAsHex()
            header.MerkleRoot = reader.ReadHashAsHex()
            header.Timestamp = reader.ReadUInt32()
            header.Bits = reader.ReadUInt32()
            header.Nonce = reader.ReadUInt32()
            Return header
        End Function

        ''' <summary>
        ''' Serializes a complete block to bytes.
        ''' </summary>
        Public Shared Function SerializeBlock(block As Block) As Byte()
            If block Is Nothing Then Throw New ArgumentNullException(NameOf(block))

            Dim writer As New BufferWriter()

            ' Header
            Dim headerBytes As Byte() = SerializeHeader(block.Header)
            writer.WriteBytes(headerBytes)

            ' Transaction count
            writer.WriteVarInt(block.TransactionIds.Count)

            ' Transaction IDs
            For Each txId As String In block.TransactionIds
                writer.WriteHashFromHex(txId.PadLeft(64, "0"c))
            Next

            ' Transaction data (if available)
            writer.WriteVarInt(block.TransactionData.Count)
            For Each txData As Byte() In block.TransactionData
                writer.WriteVarBytes(txData)
            Next

            Return writer.ToArray()
        End Function

        ''' <summary>
        ''' Deserializes a complete block from bytes.
        ''' </summary>
        Public Shared Function DeserializeBlock(data As Byte()) As Block
            If data Is Nothing Then Throw New ArgumentNullException(NameOf(data))

            Dim reader As New BufferReader(data)
            Dim block As New Block()

            ' Header
            Dim headerBytes As Byte() = reader.ReadBytes(80)
            block.Header = DeserializeHeader(headerBytes)

            ' Transaction IDs
            Dim txCount As Integer = CInt(reader.ReadVarInt())
            block.TransactionIds = New List(Of String)(txCount)
            For i As Integer = 0 To txCount - 1
                block.TransactionIds.Add(reader.ReadHashAsHex())
            Next

            ' Transaction data
            If reader.Remaining > 0 Then
                Dim dataCount As Integer = CInt(reader.ReadVarInt())
                block.TransactionData = New List(Of Byte())(dataCount)
                For i As Integer = 0 To dataCount - 1
                    block.TransactionData.Add(reader.ReadVarBytes())
                Next
            End If

            Return block
        End Function

        ''' <summary>
        ''' Computes the hash of a serialized block header.
        ''' </summary>
        Public Shared Function ComputeBlockHash(headerBytes As Byte()) As String
            Dim hash As Byte() = HashUtil.DoubleSha256(headerBytes)
            Return HashUtil.ToHex(hash)
        End Function

    End Class

End Namespace
