Imports System.ServiceModel
Imports CryptoCoin.Core
Imports CryptoCoin.Transactions
Imports CryptoCoin.Services.Contracts
Imports CryptoCoin.Services.DataContracts

Namespace CryptoCoin.Services.Implementations

    ''' <summary>
    ''' WCF service implementation for blockchain query operations.
    ''' Backed directly by the live Blockchain and Mempool instances
    ''' running in the Node process — no HTTP round-trip needed.
    ''' </summary>
    <ServiceBehavior(InstanceContextMode:=InstanceContextMode.Single,
                     ConcurrencyMode:=ConcurrencyMode.Multiple)>
    Public Class BlockchainServiceImpl
        Implements IBlockchainService

        Private ReadOnly _blockchain As Blockchain
        Private ReadOnly _mempool As Mempool
        Private ReadOnly _params As ChainParameters

        Public Sub New(blockchain As Blockchain, mempool As Mempool, params As ChainParameters)
            _blockchain = blockchain
            _mempool = mempool
            _params = params
        End Sub

        Public Function GetBlockCount() As Integer _
               Implements IBlockchainService.GetBlockCount
            Return _blockchain.Height
        End Function

        Public Function GetBestBlockHash() As String _
               Implements IBlockchainService.GetBestBlockHash
            Return _blockchain.Tip?.Hash
        End Function

        Public Function GetBlock(hash As String) As BlockData _
               Implements IBlockchainService.GetBlock
            Dim block As Block = _blockchain.GetBlock(hash)
            If block Is Nothing Then Return Nothing
            Return MapBlock(block)
        End Function

        Public Function GetBlockByHeight(height As Integer) As BlockData _
               Implements IBlockchainService.GetBlockByHeight
            Dim block As Block = _blockchain.GetBlockByHeight(height)
            If block Is Nothing Then Return Nothing
            Return MapBlock(block)
        End Function

        Public Function GetLatestBlocks() As BlockListData _
               Implements IBlockchainService.GetLatestBlocks
            Dim result As New BlockListData()
            Dim startHeight As Integer = _blockchain.Height
            Dim endHeight As Integer = Math.Max(0, startHeight - 9)
            For h As Integer = startHeight To endHeight Step -1
                Dim block As Block = _blockchain.GetBlockByHeight(h)
                If block IsNot Nothing Then
                    result.Blocks.Add(MapBlock(block))
                End If
            Next
            result.TotalCount = result.Blocks.Count
            Return result
        End Function

        Public Function GetNetworkStatus() As NetworkStatusData _
               Implements IBlockchainService.GetNetworkStatus
            Dim tip As BlockIndex = _blockchain.Tip
            Dim difficulty As Double = DifficultyCalculator.GetDifficultyRatio(_blockchain.CurrentDifficulty)
            Dim hashRate As Double = DifficultyCalculator.EstimateHashRate(
                _blockchain.CurrentDifficulty, _params.TargetBlockTimeSeconds)

            Return New NetworkStatusData() With {
                .Height = _blockchain.Height,
                .BestBlockHash = tip?.Hash,
                .BestBlockTime = If(tip IsNot Nothing, tip.Timestamp, 0),
                .BlockCount = _blockchain.BlockCount,
                .MempoolCount = _mempool.Count,
                .MempoolBytes = _mempool.TotalBytes,
                .CoinName = _params.CoinName,
                .CoinSymbol = _params.CoinSymbol,
                .Difficulty = difficulty,
                .HashRate = hashRate
            }
        End Function

        Public Function GetMempool() As MempoolData _
               Implements IBlockchainService.GetMempool
            Dim result As New MempoolData() With {
                .TransactionCount = _mempool.Count,
                .TotalBytes = _mempool.TotalBytes,
                .TotalFees = _mempool.TotalFees
            }
            For Each entry As MempoolEntry In _mempool.GetByFeeRate(10)
                result.Transactions.Add(New MempoolEntryData() With {
                    .TxId = entry.Transaction.TxId,
                    .Fee = entry.Fee,
                    .Size = entry.Size,
                    .FeeRate = entry.FeeRate
                })
            Next
            Return result
        End Function

        ' ── Mapping helper ───────────────────────────────────────────────────

        Private Shared Function MapBlock(block As Block) As BlockData
            Dim data As New BlockData() With {
                .Hash = block.Hash,
                .Height = block.Height,
                .PreviousHash = block.Header.PreviousBlockHash,
                .MerkleRoot = block.Header.MerkleRoot,
                .Timestamp = block.Header.Timestamp,
                .Bits = CLng(block.Header.Bits),
                .Nonce = CLng(block.Header.Nonce),
                .TransactionCount = block.TransactionCount,
                .Size = block.Size
            }
            data.TransactionIds.AddRange(block.TransactionIds)
            Return data
        End Function

    End Class

End Namespace
