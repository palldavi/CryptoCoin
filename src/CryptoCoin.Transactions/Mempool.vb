Namespace CryptoCoin.Transactions

    ''' <summary>
    ''' The memory pool (mempool) holds unconfirmed transactions waiting to be included in a block.
    ''' Transactions are prioritized by fee rate for block assembly.
    ''' </summary>
    Public Class Mempool

        Private ReadOnly _transactions As New Dictionary(Of String, MempoolEntry)(StringComparer.OrdinalIgnoreCase)
        Private ReadOnly _syncLock As New Object()
        Private Const MaxMempoolSize As Integer = 50000
        Private Const MaxMempoolBytes As Long = 300 * 1024 * 1024 ' 300 MB

        ''' <summary>
        ''' Gets the number of transactions in the mempool.
        ''' </summary>
        Public ReadOnly Property Count As Integer
            Get
                SyncLock _syncLock
                    Return _transactions.Count
                End SyncLock
            End Get
        End Property

        ''' <summary>
        ''' Gets the total size of all transactions in bytes.
        ''' </summary>
        Public ReadOnly Property TotalBytes As Long
            Get
                SyncLock _syncLock
                    Dim total As Long = 0
                    For Each entry As MempoolEntry In _transactions.Values
                        total += entry.Size
                    Next
                    Return total
                End SyncLock
            End Get
        End Property

        ''' <summary>
        ''' Gets the total fees of all transactions in the mempool.
        ''' </summary>
        Public ReadOnly Property TotalFees As Long
            Get
                SyncLock _syncLock
                    Dim total As Long = 0
                    For Each entry As MempoolEntry In _transactions.Values
                        total += entry.Fee
                    Next
                    Return total
                End SyncLock
            End Get
        End Property

        ''' <summary>
        ''' Adds a transaction to the mempool.
        ''' </summary>
        Public Function Add(tx As Transaction, fee As Long) As Boolean
            If tx Is Nothing Then Return False
            If tx.IsCoinbase Then Return False ' Coinbase cannot be in mempool

            SyncLock _syncLock
                ' Check capacity
                If _transactions.Count >= MaxMempoolSize Then
                    EvictLowestFeeRate()
                End If

                If _transactions.ContainsKey(tx.TxId) Then Return False

                Dim entry As New MempoolEntry()
                entry.Transaction = tx
                entry.Fee = fee
                entry.Size = tx.Size
                entry.FeeRate = CDbl(fee) / tx.Size
                entry.TimeAdded = DateTimeOffset.UtcNow.ToUnixTimeSeconds()

                _transactions(tx.TxId) = entry
                Return True
            End SyncLock
        End Function

        ''' <summary>
        ''' Removes a transaction from the mempool.
        ''' </summary>
        Public Function Remove(txId As String) As Boolean
            SyncLock _syncLock
                Return _transactions.Remove(txId)
            End SyncLock
        End Function

        ''' <summary>
        ''' Removes multiple transactions (e.g., after a block is mined).
        ''' </summary>
        Public Sub RemoveAll(txIds As IEnumerable(Of String))
            SyncLock _syncLock
                For Each txId As String In txIds
                    _transactions.Remove(txId)
                Next
            End SyncLock
        End Sub

        ''' <summary>
        ''' Gets a transaction from the mempool.
        ''' </summary>
        Public Function [Get](txId As String) As Transaction
            SyncLock _syncLock
                Dim entry As MempoolEntry = Nothing
                If _transactions.TryGetValue(txId, entry) Then
                    Return entry.Transaction
                End If
                Return Nothing
            End SyncLock
        End Function

        ''' <summary>
        ''' Checks if a transaction exists in the mempool.
        ''' </summary>
        Public Function Contains(txId As String) As Boolean
            SyncLock _syncLock
                Return _transactions.ContainsKey(txId)
            End SyncLock
        End Function

        ''' <summary>
        ''' Gets transactions sorted by fee rate (highest first) for block assembly.
        ''' </summary>
        Public Function GetByFeeRate(Optional maxCount As Integer = Integer.MaxValue) As List(Of MempoolEntry)
            SyncLock _syncLock
                Dim sorted As List(Of MempoolEntry) = _transactions.Values.ToList()
                sorted.Sort(Function(a, b) b.FeeRate.CompareTo(a.FeeRate))
                If sorted.Count > maxCount Then
                    Return sorted.GetRange(0, maxCount)
                End If
                Return sorted
            End SyncLock
        End Function

        ''' <summary>
        ''' Gets transactions for block assembly up to the given size limit.
        ''' </summary>
        Public Function SelectForBlock(maxBlockSize As Integer) As List(Of Transaction)
            Dim selected As New List(Of Transaction)()
            Dim currentSize As Integer = 80 ' Block header

            Dim entries As List(Of MempoolEntry) = GetByFeeRate()
            For Each entry As MempoolEntry In entries
                If currentSize + entry.Size > maxBlockSize Then Continue For
                selected.Add(entry.Transaction)
                currentSize += entry.Size
            Next

            Return selected
        End Function

        ''' <summary>
        ''' Checks if any input of a transaction conflicts with mempool transactions.
        ''' </summary>
        Public Function HasConflict(tx As Transaction) As Boolean
            SyncLock _syncLock
                For Each input As TransactionInput In tx.Inputs
                    For Each entry As MempoolEntry In _transactions.Values
                        For Each existingInput As TransactionInput In entry.Transaction.Inputs
                            If input.PreviousOutput.Equals(existingInput.PreviousOutput) Then
                                Return True
                            End If
                        Next
                    Next
                Next
                Return False
            End SyncLock
        End Function

        ''' <summary>
        ''' Gets the minimum fee rate in the mempool.
        ''' </summary>
        Public Function GetMinFeeRate() As Double
            SyncLock _syncLock
                If _transactions.Count = 0 Then Return 0
                Dim minRate As Double = Double.MaxValue
                For Each entry As MempoolEntry In _transactions.Values
                    If entry.FeeRate < minRate Then
                        minRate = entry.FeeRate
                    End If
                Next
                Return minRate
            End SyncLock
        End Function

        Private Sub EvictLowestFeeRate()
            Dim lowestKey As String = Nothing
            Dim lowestRate As Double = Double.MaxValue

            For Each kvp As KeyValuePair(Of String, MempoolEntry) In _transactions
                If kvp.Value.FeeRate < lowestRate Then
                    lowestRate = kvp.Value.FeeRate
                    lowestKey = kvp.Key
                End If
            Next

            If lowestKey IsNot Nothing Then
                _transactions.Remove(lowestKey)
            End If
        End Sub

        ''' <summary>
        ''' Clears all transactions from the mempool.
        ''' </summary>
        Public Sub Clear()
            SyncLock _syncLock
                _transactions.Clear()
            End SyncLock
        End Sub

    End Class

    ''' <summary>
    ''' Represents a transaction entry in the mempool with metadata.
    ''' </summary>
    Public Class MempoolEntry

        Public Property Transaction As Transaction
        Public Property Fee As Long
        Public Property Size As Integer
        Public Property FeeRate As Double
        Public Property TimeAdded As Long

        Public ReadOnly Property AgeSeconds As Long
            Get
                Return DateTimeOffset.UtcNow.ToUnixTimeSeconds() - TimeAdded
            End Get
        End Property

        Public Overrides Function ToString() As String
            Return $"MempoolEntry(TxId={Transaction?.TxId?.Substring(0, 8)}..., Fee={Fee}, Rate={FeeRate:F2})"
        End Function

    End Class

End Namespace
