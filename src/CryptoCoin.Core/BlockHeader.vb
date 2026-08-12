Imports CryptoCoin.Cryptography

Namespace CryptoCoin.Core

    ''' <summary>
    ''' Represents the header of a block in the CryptoCoin blockchain.
    ''' The header contains metadata and is the target of proof-of-work mining.
    ''' </summary>
    Public Class BlockHeader

        ''' <summary>
        ''' Protocol version number.
        ''' </summary>
        Public Property Version As Integer = 1

        ''' <summary>
        ''' Hash of the previous block header (32 bytes, hex-encoded).
        ''' </summary>
        Public Property PreviousBlockHash As String = ""

        ''' <summary>
        ''' Merkle root of all transactions in the block (32 bytes, hex-encoded).
        ''' </summary>
        Public Property MerkleRoot As String = ""

        ''' <summary>
        ''' Block creation timestamp (Unix epoch seconds).
        ''' </summary>
        Public Property Timestamp As Long

        ''' <summary>
        ''' Difficulty target in compact format (nBits).
        ''' </summary>
        Public Property Bits As UInteger

        ''' <summary>
        ''' Nonce value found by the miner to satisfy proof-of-work.
        ''' </summary>
        Public Property Nonce As UInteger

        ''' <summary>
        ''' Block height (not part of the serialized header, but tracked for convenience).
        ''' </summary>
        Public Property Height As Integer

        ''' <summary>
        ''' Computes the hash of this block header (double SHA-256).
        ''' </summary>
        Public Function ComputeHash() As String
            Dim data As Byte() = Serialize()
            Dim hash As Byte() = HashUtil.DoubleSha256(data)
            Return HashUtil.ToHex(hash)
        End Function

        ''' <summary>
        ''' Serializes the block header to bytes for hashing.
        ''' </summary>
        Public Function Serialize() As Byte()
            Dim buffer(79) As Byte
            Dim offset As Integer = 0

            ' Version (4 bytes, little-endian)
            Dim versionBytes As Byte() = BitConverter.GetBytes(Version)
            Array.Copy(versionBytes, 0, buffer, offset, 4)
            offset += 4

            ' Previous block hash (32 bytes)
            Dim prevHash As Byte() = HashUtil.FromHex(PreviousBlockHash.PadLeft(64, "0"c))
            Array.Copy(prevHash, 0, buffer, offset, 32)
            offset += 32

            ' Merkle root (32 bytes)
            Dim merkle As Byte() = HashUtil.FromHex(MerkleRoot.PadLeft(64, "0"c))
            Array.Copy(merkle, 0, buffer, offset, 32)
            offset += 32

            ' Timestamp (4 bytes, little-endian)
            Dim tsBytes As Byte() = BitConverter.GetBytes(CUInt(Timestamp))
            Array.Copy(tsBytes, 0, buffer, offset, 4)
            offset += 4

            ' Bits (4 bytes, little-endian)
            Dim bitsBytes As Byte() = BitConverter.GetBytes(Bits)
            Array.Copy(bitsBytes, 0, buffer, offset, 4)
            offset += 4

            ' Nonce (4 bytes, little-endian)
            Dim nonceBytes As Byte() = BitConverter.GetBytes(Nonce)
            Array.Copy(nonceBytes, 0, buffer, offset, 4)

            Return buffer
        End Function

        ''' <summary>
        ''' Deserializes a block header from bytes.
        ''' </summary>
        Public Shared Function Deserialize(data As Byte()) As BlockHeader
            If data Is Nothing OrElse data.Length < 80 Then
                Throw New ArgumentException("Block header must be at least 80 bytes.")
            End If

            Dim header As New BlockHeader()
            Dim offset As Integer = 0

            header.Version = BitConverter.ToInt32(data, offset) : offset += 4

            Dim prevHashBytes(31) As Byte
            Array.Copy(data, offset, prevHashBytes, 0, 32) : offset += 32
            header.PreviousBlockHash = HashUtil.ToHex(prevHashBytes)

            Dim merkleBytes(31) As Byte
            Array.Copy(data, offset, merkleBytes, 0, 32) : offset += 32
            header.MerkleRoot = HashUtil.ToHex(merkleBytes)

            header.Timestamp = BitConverter.ToUInt32(data, offset) : offset += 4
            header.Bits = BitConverter.ToUInt32(data, offset) : offset += 4
            header.Nonce = BitConverter.ToUInt32(data, offset)

            Return header
        End Function

        ''' <summary>
        ''' Converts compact bits format to the full 256-bit target.
        ''' </summary>
        Public Function GetTarget() As Byte()
            Dim exponent As Integer = CInt(Bits >> 24)
            Dim coefficient As UInteger = Bits And &HFFFFFFUI

            Dim target(31) As Byte
            If exponent <= 3 Then
                coefficient = coefficient >> (8 * (3 - exponent))
                target(0) = CByte(coefficient And &HFFUI)
                If target.Length > 1 Then target(1) = CByte((coefficient >> 8) And &HFFUI)
                If target.Length > 2 Then target(2) = CByte((coefficient >> 16) And &HFFUI)
            Else
                Dim startPos As Integer = exponent - 3
                If startPos < 32 Then target(startPos) = CByte(coefficient And &HFFUI)
                If startPos + 1 < 32 Then target(startPos + 1) = CByte((coefficient >> 8) And &HFFUI)
                If startPos + 2 < 32 Then target(startPos + 2) = CByte((coefficient >> 16) And &HFFUI)
            End If

            ' Reverse to big-endian for comparison
            Array.Reverse(target)
            Return target
        End Function

        ''' <summary>
        ''' Checks if the block hash meets the difficulty target.
        ''' </summary>
        Public Function MeetsTarget() As Boolean
            Dim hash As String = ComputeHash()
            Dim hashBytes As Byte() = HashUtil.FromHex(hash)
            Dim target As Byte() = GetTarget()

            ' Compare hash <= target (both big-endian)
            For i As Integer = 0 To 31
                If hashBytes(i) < target(i) Then Return True
                If hashBytes(i) > target(i) Then Return False
            Next
            Return True ' Equal
        End Function

        Public Overrides Function ToString() As String
            Return $"BlockHeader(Height={Height}, Hash={ComputeHash().Substring(0, 16)}..., Timestamp={Timestamp})"
        End Function

    End Class

End Namespace
