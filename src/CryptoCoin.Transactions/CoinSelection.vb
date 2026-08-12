Namespace CryptoCoin.Transactions

    ''' <summary>
    ''' Algorithms for selecting which UTXOs to use as inputs for a transaction.
    ''' Balances between minimizing fees and avoiding dust outputs.
    ''' </summary>
    Public Class CoinSelection

        ''' <summary>
        ''' Selects UTXOs to cover the target amount plus estimated fees.
        ''' Uses a combination of largest-first and exact-match strategies.
        ''' </summary>
        Public Shared Function SelectCoins(availableUtxos As List(Of UtxoEntry), targetAmount As Long, feePerByte As Long) As CoinSelectionResult
            If availableUtxos Is Nothing OrElse availableUtxos.Count = 0 Then
                Return CoinSelectionResult.Failure("No UTXOs available.")
            End If

            ' Estimate fee for a typical transaction
            Dim estimatedInputs As Integer = 1
            Dim estimatedSize As Integer = EstimateSize(estimatedInputs, 2) ' 2 outputs (recipient + change)
            Dim estimatedFee As Long = CLng(estimatedSize) * feePerByte
            Dim requiredAmount As Long = targetAmount + estimatedFee

            ' Try exact match first
            Dim exactResult As CoinSelectionResult = TryExactMatch(availableUtxos, requiredAmount)
            If exactResult.Success Then Return exactResult

            ' Try largest-first selection
            Dim largestFirst As CoinSelectionResult = SelectLargestFirst(availableUtxos, targetAmount, feePerByte)
            If largestFirst.Success Then Return largestFirst

            ' Try using all UTXOs
            Dim totalAvailable As Long = 0
            For Each utxo As UtxoEntry In availableUtxos
                totalAvailable += utxo.Value
            Next

            Dim allInputsFee As Long = CLng(EstimateSize(availableUtxos.Count, 2)) * feePerByte
            If totalAvailable >= targetAmount + allInputsFee Then
                Dim result As New CoinSelectionResult()
                result.SelectedUtxos = New List(Of UtxoEntry)(availableUtxos)
                result.TotalInput = totalAvailable
                result.Fee = allInputsFee
                result.Change = totalAvailable - targetAmount - allInputsFee
                Return result
            End If

            Return CoinSelectionResult.Failure(
                $"Insufficient funds. Need {targetAmount + estimatedFee}, have {totalAvailable}.")
        End Function

        ''' <summary>
        ''' Selects UTXOs using largest-first strategy.
        ''' </summary>
        Private Shared Function SelectLargestFirst(utxos As List(Of UtxoEntry), targetAmount As Long, feePerByte As Long) As CoinSelectionResult
            ' Sort by value descending
            Dim sorted As List(Of UtxoEntry) = utxos.ToList()
            sorted.Sort(Function(a, b) b.Value.CompareTo(a.Value))

            Dim selected As New List(Of UtxoEntry)()
            Dim totalInput As Long = 0

            For Each utxo As UtxoEntry In sorted
                selected.Add(utxo)
                totalInput += utxo.Value

                ' Recalculate fee with current input count
                Dim fee As Long = CLng(EstimateSize(selected.Count, 2)) * feePerByte
                Dim required As Long = targetAmount + fee

                If totalInput >= required Then
                    Dim result As New CoinSelectionResult()
                    result.SelectedUtxos = selected
                    result.TotalInput = totalInput
                    result.Fee = fee
                    result.Change = totalInput - targetAmount - fee
                    Return result
                End If
            Next

            Return CoinSelectionResult.Failure("Insufficient funds with largest-first strategy.")
        End Function

        ''' <summary>
        ''' Tries to find a single UTXO that exactly matches the required amount.
        ''' </summary>
        Private Shared Function TryExactMatch(utxos As List(Of UtxoEntry), requiredAmount As Long) As CoinSelectionResult
            ' Look for a single UTXO within 1% of the required amount (to avoid change)
            Dim tolerance As Long = requiredAmount \ 100

            For Each utxo As UtxoEntry In utxos
                If utxo.Value >= requiredAmount AndAlso utxo.Value <= requiredAmount + tolerance Then
                    Dim result As New CoinSelectionResult()
                    result.SelectedUtxos = New List(Of UtxoEntry)() From {utxo}
                    result.TotalInput = utxo.Value
                    Dim fee As Long = CLng(EstimateSize(1, 1)) * 10 ' Assume 10 sat/byte
                    result.Fee = fee
                    result.Change = utxo.Value - requiredAmount
                    Return result
                End If
            Next

            Return CoinSelectionResult.Failure("No exact match found.")
        End Function

        ''' <summary>
        ''' Estimates transaction size based on input and output count.
        ''' </summary>
        Public Shared Function EstimateSize(inputCount As Integer, outputCount As Integer) As Integer
            ' Version (4) + varint inputs + varint outputs + locktime (4)
            ' Each input: ~148 bytes (outpoint 36 + scriptSig ~107 + sequence 4 + varint 1)
            ' Each output: ~34 bytes (value 8 + scriptPubKey ~25 + varint 1)
            Return 10 + (inputCount * 148) + (outputCount * 34)
        End Function

    End Class

    ''' <summary>
    ''' Result of a coin selection operation.
    ''' </summary>
    Public Class CoinSelectionResult

        Public Property SelectedUtxos As List(Of UtxoEntry)
        Public Property TotalInput As Long
        Public Property Fee As Long
        Public Property Change As Long
        Public Property ErrorMessage As String

        Public ReadOnly Property Success As Boolean
            Get
                Return String.IsNullOrEmpty(ErrorMessage) AndAlso SelectedUtxos IsNot Nothing
            End Get
        End Property

        Public Sub New()
            SelectedUtxos = New List(Of UtxoEntry)()
        End Sub

        Public Shared Function Failure(message As String) As CoinSelectionResult
            Dim result As New CoinSelectionResult()
            result.ErrorMessage = message
            result.SelectedUtxos = Nothing
            Return result
        End Function

    End Class

End Namespace
