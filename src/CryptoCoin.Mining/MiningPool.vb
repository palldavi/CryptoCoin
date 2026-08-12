Imports CryptoCoin.Core
Imports CryptoCoin.Cryptography

Namespace CryptoCoin.Mining

    ''' <summary>
    ''' Implements a mining pool that distributes work to multiple miners
    ''' and shares rewards proportionally based on submitted shares.
    ''' </summary>
    Public Class MiningPool

        Private ReadOnly _workers As New Dictionary(Of String, PoolWorker)()
        Private ReadOnly _shares As New List(Of PoolShare)()
        Private ReadOnly _syncLock As New Object()
        Private _currentJob As MiningJob
        Private _shareDifficulty As UInteger

        ''' <summary>
        ''' Pool name.
        ''' </summary>
        Public Property PoolName As String = "CryptoCoin Pool"

        ''' <summary>
        ''' Pool fee percentage (0-100).
        ''' </summary>
        Public Property FeePercent As Double = 2.0

        ''' <summary>
        ''' Pool payout address.
        ''' </summary>
        Public Property PoolAddress As String

        ''' <summary>
        ''' Minimum payout threshold in satoshis.
        ''' </summary>
        Public Property MinPayoutThreshold As Long = 100000000 ' 1 CRC

        ''' <summary>
        ''' Gets the number of connected workers.
        ''' </summary>
        Public ReadOnly Property WorkerCount As Integer
            Get
                SyncLock _syncLock
                    Return _workers.Count
                End SyncLock
            End Get
        End Property

        ''' <summary>
        ''' Gets the total pool hash rate.
        ''' </summary>
        Public ReadOnly Property TotalHashRate As Double
            Get
                SyncLock _syncLock
                    Dim total As Double = 0
                    For Each worker As Object In _workers.Values
                        total += worker.HashRate
                    Next
                    Return total
                End SyncLock
            End Get
        End Property

        ''' <summary>
        ''' Gets the current mining job.
        ''' </summary>
        Public ReadOnly Property CurrentJob As MiningJob
            Get
                Return _currentJob
            End Get
        End Property

        ''' <summary>
        ''' Event raised when a block is found by the pool.
        ''' </summary>
        Public Event BlockFound As EventHandler(Of PoolBlockFoundEventArgs)

        Public Sub New(shareDifficulty As UInteger)
            _shareDifficulty = shareDifficulty
        End Sub

        ''' <summary>
        ''' Registers a new worker with the pool.
        ''' </summary>
        Public Function RegisterWorker(workerName As String, payoutAddress As String) As PoolWorker
            SyncLock _syncLock
                Dim worker As New PoolWorker()
                worker.Name = workerName
                worker.PayoutAddress = payoutAddress
                worker.ConnectedAt = DateTimeOffset.UtcNow
                worker.IsActive = True
                _workers(workerName) = worker
                Return worker
            End SyncLock
        End Function

        ''' <summary>
        ''' Removes a worker from the pool.
        ''' </summary>
        Public Sub RemoveWorker(workerName As String)
            SyncLock _syncLock
                _workers.Remove(workerName)
            End SyncLock
        End Sub

        ''' <summary>
        ''' Submits a share from a worker.
        ''' </summary>
        Public Function SubmitShare(workerName As String, nonce As UInteger, jobId As String) As ShareResult
            SyncLock _syncLock
                ' Validate worker
                If Not _workers.ContainsKey(workerName) Then
                    Return New ShareResult(False, "Unknown worker.")
                End If

                ' Validate job
                If _currentJob Is Nothing OrElse _currentJob.JobId <> jobId Then
                    Return New ShareResult(False, "Stale job.")
                End If

                ' Check if share meets share difficulty
                Dim header As BlockHeader = _currentJob.Block.Header
                header.Nonce = nonce
                Dim hash As String = header.ComputeHash()
                Dim hashBytes As Byte() = HashUtil.FromHex(hash)

                If Not DifficultyCalculator.MeetsTarget(hashBytes, _shareDifficulty) Then
                    Return New ShareResult(False, "Share does not meet difficulty.")
                End If

                ' Record share
                Dim share As New PoolShare()
                share.WorkerName = workerName
                share.Nonce = nonce
                share.Timestamp = DateTimeOffset.UtcNow
                share.Difficulty = _shareDifficulty
                _shares.Add(share)

                ' Update worker stats
                _workers(workerName).SharesSubmitted += 1
                _workers(workerName).LastShareAt = DateTimeOffset.UtcNow

                ' Check if share also meets block difficulty
                If DifficultyCalculator.MeetsTarget(hashBytes, _currentJob.TargetBits) Then
                    ' Block found
                    _workers(workerName).BlocksFound += 1
                    RaiseEvent BlockFound(Me, New PoolBlockFoundEventArgs(_currentJob.Block, workerName))
                    Return New ShareResult(True, "Block found", True)
                End If

                Return New ShareResult(True, "Share accepted.")
            End SyncLock
        End Function

        ''' <summary>
        ''' Updates the current mining job.
        ''' </summary>
        Public Sub UpdateJob(job As MiningJob)
            SyncLock _syncLock
                If _currentJob IsNot Nothing Then
                    _currentJob.IsValid = False
                End If
                _currentJob = job
            End SyncLock
        End Sub

        ''' <summary>
        ''' Calculates payouts for all workers based on shares.
        ''' Uses proportional (PROP) payout scheme.
        ''' </summary>
        Public Function CalculatePayouts(blockReward As Long) As Dictionary(Of String, Long)
            SyncLock _syncLock
                Dim payouts As New Dictionary(Of String, Long)()

                ' Deduct pool fee
                Dim poolFee As Long = CLng(blockReward * FeePercent / 100)
                Dim distributable As Long = blockReward - poolFee

                ' Count total shares
                Dim totalShares As Integer = _shares.Count
                If totalShares = 0 Then Return payouts

                ' Calculate per-worker shares
                Dim workerShares As New Dictionary(Of String, Integer)()
                For Each share As Object In _shares
                    If Not workerShares.ContainsKey(share.WorkerName) Then
                        workerShares(share.WorkerName) = 0
                    End If
                    workerShares(share.WorkerName) += 1
                Next

                ' Distribute proportionally
                For Each kvp As Object In workerShares
                    Dim workerPayout As Long = CLng(distributable * kvp.Value / totalShares)
                    If _workers.ContainsKey(kvp.Key) Then
                        payouts(_workers(kvp.Key).PayoutAddress) = workerPayout
                    End If
                Next

                ' Clear shares for next round
                _shares.Clear()

                Return payouts
            End SyncLock
        End Function

        ''' <summary>
        ''' Gets statistics for all workers.
        ''' </summary>
        Public Function GetWorkerStats() As List(Of PoolWorker)
            SyncLock _syncLock
                Return _workers.Values.ToList()
            End SyncLock
        End Function

    End Class

    Public Class PoolWorker
        Public Property Name As String
        Public Property PayoutAddress As String
        Public Property ConnectedAt As DateTimeOffset
        Public Property IsActive As Boolean
        Public Property SharesSubmitted As Long
        Public Property BlocksFound As Integer
        Public Property LastShareAt As DateTimeOffset
        Public Property HashRate As Double
        Public Property PendingBalance As Long
    End Class

    Public Class PoolShare
        Public Property WorkerName As String
        Public Property Nonce As UInteger
        Public Property Timestamp As DateTimeOffset
        Public Property Difficulty As UInteger
    End Class

    Public Class ShareResult
        Public ReadOnly Property Accepted As Boolean
        Public ReadOnly Property Message As String
        Public ReadOnly Property IsBlock As Boolean

        Public Sub New(accepted As Boolean, message As String, Optional isBlock As Boolean = False)
            Me.Accepted = accepted
            Me.Message = message
            Me.IsBlock = isBlock
        End Sub
    End Class

    Public Class PoolBlockFoundEventArgs
        Inherits EventArgs
        Public ReadOnly Property Block As Block
        Public ReadOnly Property WorkerName As String

        Public Sub New(block As Block, workerName As String)
            Me.Block = block
            Me.WorkerName = workerName
        End Sub
    End Class

End Namespace
