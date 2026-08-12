Imports CryptoCoin.Cryptography

Namespace CryptoCoin.Core

    ''' <summary>
    ''' Creates the genesis (first) block of the CryptoCoin blockchain.
    ''' The genesis block is hardcoded and serves as the root of the chain.
    ''' </summary>
    Public NotInheritable Class GenesisBlock

        Private Sub New()
        End Sub

        ''' <summary>
        ''' The genesis block timestamp (January 1, 2025 00:00:00 UTC).
        ''' </summary>
        Public Const GenesisTimestamp As Long = 1735689600

        ''' <summary>
        ''' The genesis block message embedded in the coinbase transaction.
        ''' </summary>
        Public Const GenesisMessage As String = "CryptoCoin Genesis - A New Digital Currency Is Born"

        ''' <summary>
        ''' Creates the genesis block with the given chain parameters.
        ''' </summary>
        Public Shared Function Create(params As ChainParameters) As Block
            Dim header As New BlockHeader()
            header.Version = 1
            header.PreviousBlockHash = New String("0"c, 64)
            header.Timestamp = GenesisTimestamp
            header.Bits = DifficultyCalculator.MinDifficultyBits
            header.Nonce = FindGenesisNonce(header)
            header.Height = 0

            ' Create coinbase transaction hash
            Dim coinbaseTxHash As String = HashUtil.ToHex(
                HashUtil.DoubleSha256(
                    System.Text.Encoding.UTF8.GetBytes(GenesisMessage)
                )
            )

            Dim block As New Block()
            block.Header = header
            block.TransactionIds.Add(coinbaseTxHash)

            ' Set Merkle root
            block.Header.MerkleRoot = block.ComputeMerkleRoot()

            Return block
        End Function

        ''' <summary>
        ''' Finds a valid nonce for the genesis block header.
        ''' For the genesis block, we use a pre-computed nonce to avoid mining at startup.
        ''' </summary>
        Private Shared Function FindGenesisNonce(header As BlockHeader) As UInteger
            ' In a real implementation, this would be pre-computed.
            ' For demo purposes, we use a simple nonce that works with minimum difficulty.
            Return 42UI
        End Function

        ''' <summary>
        ''' Validates that a block is the expected genesis block.
        ''' </summary>
        Public Shared Function IsGenesisBlock(block As Block) As Boolean
            If block Is Nothing Then Return False
            If block.Header Is Nothing Then Return False
            Return block.Header.Height = 0 AndAlso
                   block.Header.PreviousBlockHash = New String("0"c, 64)
        End Function

    End Class

End Namespace
