Imports CryptoCoin.Core
Imports CryptoCoin.Transactions

Namespace CryptoCoin.Node

    ''' <summary>
    ''' Processes incoming blocks and updates the mempool.
    ''' </summary>
    Public Class BlockProcessor

        Private ReadOnly _blockchain As Blockchain
        Private ReadOnly _mempool As Mempool

        Public Sub New(blockchain As Blockchain, mempool As Mempool)
            _blockchain = blockchain
            _mempool = mempool
        End Sub

        ''' <summary>
        ''' Processes a new block received from a peer or mined locally.
        ''' Returns True if the block was accepted.
        ''' </summary>
        Public Function ProcessBlock(block As Block) As Boolean
            ' Attempt to add the block to the chain
            Dim result As BlockValidationResult = _blockchain.AddBlock(block)

            If result.IsValid Then
                ' Remove confirmed transactions from mempool
                RemoveConfirmedTransactions(block)
                Return True
            End If

            Return False
        End Function

        ''' <summary>
        ''' Removes transactions that were included in a block from the mempool.
        ''' </summary>
        Private Sub RemoveConfirmedTransactions(block As Block)
            If block.TransactionIds Is Nothing Then Return
            _mempool.RemoveAll(block.TransactionIds)
        End Sub

        ''' <summary>
        ''' Validates and adds a transaction to the mempool.
        ''' </summary>
        Public Function AcceptTransaction(tx As Transaction, fee As Long) As Boolean
            If tx Is Nothing Then Return False
            If tx.IsCoinbase Then Return False
            If _mempool.Contains(tx.TxId) Then Return False

            Return _mempool.Add(tx, fee)
        End Function

        ''' <summary>
        ''' Gets the current chain tip information.
        ''' </summary>
        Public Function GetChainInfo() As String
            Dim tip As BlockIndex = _blockchain.Tip
            Return $"Height={tip.Height}, Hash={tip.Hash.Substring(0, 16)}..., Difficulty={tip.Bits}"
        End Function

    End Class

End Namespace
