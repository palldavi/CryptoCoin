Namespace CryptoCoin.Transactions

    ''' <summary>
    ''' Estimates appropriate transaction fees based on mempool state and recent blocks.
    ''' </summary>
    Public Class FeeEstimator

        Private ReadOnly _recentFeeRates As New Queue(Of Double)()
        Private ReadOnly _maxSamples As Integer = 1000
        Private ReadOnly _syncLock As New Object()

        ''' <summary>
        ''' Default fee rate in satoshis per byte.
        ''' </summary>
        Public Const DefaultFeeRate As Long = 10

        ''' <summary>
        ''' Minimum fee rate in satoshis per byte.
        ''' </summary>
        Public Const MinFeeRate As Long = 1

        ''' <summary>
        ''' Maximum fee rate in satoshis per byte.
        ''' </summary>
        Public Const MaxFeeRate As Long = 1000

        ''' <summary>
        ''' Records a fee rate observation from a confirmed transaction.
        ''' </summary>
        Public Sub RecordFeeRate(feeRate As Double)
            SyncLock _syncLock
                _recentFeeRates.Enqueue(feeRate)
                While _recentFeeRates.Count > _maxSamples
                    _recentFeeRates.Dequeue()
                End While
            End SyncLock
        End Sub

        ''' <summary>
        ''' Estimates the fee rate for a given confirmation target (in blocks).
        ''' </summary>
        ''' <param name="targetBlocks">Desired number of blocks until confirmation.</param>
        ''' <returns>Estimated fee rate in satoshis per byte.</returns>
        Public Function EstimateFeeRate(targetBlocks As Integer) As Long
            SyncLock _syncLock
                If _recentFeeRates.Count = 0 Then Return DefaultFeeRate

                Dim sorted As List(Of Double) = _recentFeeRates.ToList()
                sorted.Sort()

                ' Use percentile based on target blocks
                ' Faster confirmation = higher percentile
                Dim percentile As Double
                Select Case targetBlocks
                    Case 1 : percentile = 0.9
                    Case 2 To 3 : percentile = 0.75
                    Case 4 To 6 : percentile = 0.5
                    Case 7 To 12 : percentile = 0.25
                    Case Else : percentile = 0.1
                End Select

                Dim index As Integer = CInt(Math.Floor(sorted.Count * percentile))
                index = Math.Min(index, sorted.Count - 1)

                Dim estimate As Long = CLng(sorted(index))
                Return Math.Max(MinFeeRate, Math.Min(MaxFeeRate, estimate))
            End SyncLock
        End Function

        ''' <summary>
        ''' Estimates the total fee for a transaction of the given size.
        ''' </summary>
        Public Function EstimateFee(transactionSizeBytes As Integer, Optional targetBlocks As Integer = 3) As Long
            Dim rate As Long = EstimateFeeRate(targetBlocks)
            Return CLng(transactionSizeBytes) * rate
        End Function

        ''' <summary>
        ''' Gets fee estimates for multiple confirmation targets.
        ''' </summary>
        Public Function GetFeeEstimates() As Dictionary(Of Integer, Long)
            Dim estimates As New Dictionary(Of Integer, Long)()
            estimates(1) = EstimateFeeRate(1)
            estimates(3) = EstimateFeeRate(3)
            estimates(6) = EstimateFeeRate(6)
            estimates(12) = EstimateFeeRate(12)
            estimates(24) = EstimateFeeRate(24)
            Return estimates
        End Function

    End Class

End Namespace
