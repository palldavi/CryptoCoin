Imports CryptoCoin.Core
Imports CryptoCoin.Cryptography
Imports CryptoCoin.Transactions

Namespace CryptoCoin.Mining

    ''' <summary>
    ''' Assembles candidate blocks for mining by selecting transactions from the mempool
    ''' and creating the coinbase transaction.
    ''' </summary>
    Public Class BlockAssembler

        Private ReadOnly _mempool As Mempool
        Private ReadOnly _params As ChainParameters

        Public Sub New(mempool As Mempool, params As ChainParameters)
            If mempool Is Nothing Then Throw New ArgumentNullException(NameOf(mempool))
            If params Is Nothing Then Throw New ArgumentNullException(NameOf(params))
            _mempool = mempool
            _params = params
        End Sub

        ''' <summary>
        ''' Creates a new mining job with a candidate block.
        ''' </summary>
        Public Function CreateJob(minerAddress As String, blockchain As Blockchain) As MiningJob
            If String.IsNullOrEmpty(minerAddress) Then Throw New ArgumentNullException(NameOf(minerAddress))
            If blockchain Is Nothing Then Throw New ArgumentNullException(NameOf(blockchain))

            Dim tip = blockchain.Tip
            Dim newHeight As Integer = tip.Height + 1

            ' Calculate reward
            Dim subsidy As Long = _params.GetBlockReward(newHeight)

            ' Select transactions from mempool
            Dim selectedTxs As List(Of Transaction) = _mempool.SelectForBlock(_params.MaxBlockSize - 1000) ' Reserve space for coinbase
            Dim totalFees As Long = CalculateTotalFees(selectedTxs)
            Dim totalReward As Long = subsidy + totalFees

            ' Create coinbase transaction
            Dim coinbaseTx As Transaction = Transaction.CreateCoinbase(newHeight, totalReward, minerAddress)

            ' Build transaction list (coinbase first)
            Dim allTxIds As New List(Of String)()
            allTxIds.Add(coinbaseTx.TxId)
            For Each tx As Object In selectedTxs
                allTxIds.Add(tx.TxId)
            Next

            ' Create block header
            Dim header As New BlockHeader()
            header.Version = _params.ProtocolVersion
            header.PreviousBlockHash = tip.Hash
            header.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            header.Bits = blockchain.GetNextDifficulty(tip.Hash)
            header.Height = newHeight
            header.Nonce = 0

            ' Create block
            Dim block As New Block(header, allTxIds)
            block.Header.MerkleRoot = block.ComputeMerkleRoot()

            ' Create job
            Dim job As New MiningJob()
            job.Block = block
            job.TargetBits = header.Bits
            job.TotalFees = totalFees
            job.TotalReward = totalReward

            Return job
        End Function

        ''' <summary>
        ''' Calculates total fees from selected transactions.
        ''' In a full implementation, this would look up input values from the UTXO set.
        ''' </summary>
        Private Function CalculateTotalFees(transactions As List(Of Transaction)) As Long
            ' Simplified: use mempool entry fees
            Dim total As Long = 0
            For Each tx As Object In transactions
                ' Estimate fee as 1 sat/byte * size
                total += CLng(tx.Size) * _params.MinFeePerByte
            Next
            Return total
        End Function

        ''' <summary>
        ''' Updates an existing job with new transactions (for long-running mining).
        ''' </summary>
        Public Function RefreshJob(existingJob As MiningJob, minerAddress As String, blockchain As Blockchain) As MiningJob
            ' Check if tip has changed
            If blockchain.Tip.Hash <> existingJob.Block.Header.PreviousBlockHash Then
                ' New block arrived, create fresh job
                existingJob.IsValid = False
                Return CreateJob(minerAddress, blockchain)
            End If

            ' Update timestamp and transactions
            Dim newJob As MiningJob = CreateJob(minerAddress, blockchain)
            existingJob.IsValid = False
            Return newJob
        End Function

    End Class

End Namespace
