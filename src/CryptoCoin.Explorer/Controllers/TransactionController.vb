Imports CryptoCoin.Core
Imports CryptoCoin.Transactions

Namespace CryptoCoin.Explorer.Controllers

    ''' <summary>
    ''' Handles transaction-related API requests.
    ''' </summary>
    Public Class TransactionController

        Private ReadOnly _blockchain As Blockchain
        Private ReadOnly _mempool As Mempool

        Public Sub New(blockchain As Blockchain, mempool As Mempool)
            _blockchain = blockchain
            _mempool = mempool
        End Sub

        ''' <summary>
        ''' Gets a transaction by its ID. Checks mempool first, then blockchain.
        ''' </summary>
        Public Function GetTransaction(txId As String) As String
            ' Check mempool first
            Dim mempoolTx As Transaction = _mempool.Get(txId)
            If mempoolTx IsNot Nothing Then
                Return SerializeTransaction(mempoolTx, -1, True)
            End If

            ' Search in blockchain blocks
            For h As Integer = _blockchain.Height To 0 Step -1
                Dim block As Block = _blockchain.GetBlockByHeight(h)
                If block IsNot Nothing AndAlso block.TransactionIds.Contains(txId) Then
                    Return SerializeTransaction(Nothing, h, False)
                End If
            Next

            Return "{""error"":""Transaction not found""}"
        End Function

        ''' <summary>
        ''' Gets mempool information.
        ''' </summary>
        Public Function GetMempoolInfo() As String
            Dim props As New List(Of String)()
            props.Add(JsonSerializer.PropInt("size", _mempool.Count))
            props.Add(JsonSerializer.PropLong("bytes", _mempool.TotalBytes))
            props.Add(JsonSerializer.PropLong("fees", _mempool.TotalFees))

            ' Get top transactions by fee rate
            Dim topEntries As List(Of MempoolEntry) = _mempool.GetByFeeRate(10)
            Dim txList As New List(Of String)()
            For Each entry As MempoolEntry In topEntries
                Dim txProps As New List(Of String)()
                txProps.Add(JsonSerializer.PropStr("txid", entry.Transaction.TxId))
                txProps.Add(JsonSerializer.PropLong("fee", entry.Fee))
                txProps.Add(JsonSerializer.PropInt("size", entry.Size))
                txProps.Add(JsonSerializer.PropDbl("feeRate", entry.FeeRate))
                txList.Add(JsonSerializer.CreateObject(txProps.ToArray()))
            Next
            props.Add(JsonSerializer.Prop("transactions", JsonSerializer.CreateArray(txList)))

            Return JsonSerializer.CreateObject(props.ToArray())
        End Function

        Private Function SerializeTransaction(tx As Transaction, blockHeight As Integer, inMempool As Boolean) As String
            Dim props As New List(Of String)()

            If tx IsNot Nothing Then
                props.Add(JsonSerializer.PropStr("txid", tx.TxId))
                props.Add(JsonSerializer.PropBool("coinbase", tx.IsCoinbase))
                props.Add(JsonSerializer.PropInt("inputCount", tx.Inputs.Count))
                props.Add(JsonSerializer.PropInt("outputCount", tx.Outputs.Count))
                props.Add(JsonSerializer.PropLong("totalOutput", tx.TotalOutputValue))
                props.Add(JsonSerializer.PropInt("size", tx.Size))
            End If

            props.Add(JsonSerializer.PropBool("inMempool", inMempool))
            props.Add(JsonSerializer.PropInt("blockHeight", blockHeight))

            If blockHeight >= 0 Then
                Dim confirmations As Integer = _blockchain.Height - blockHeight + 1
                props.Add(JsonSerializer.PropInt("confirmations", confirmations))
            Else
                props.Add(JsonSerializer.PropInt("confirmations", 0))
            End If

            Return JsonSerializer.CreateObject(props.ToArray())
        End Function

    End Class

End Namespace
