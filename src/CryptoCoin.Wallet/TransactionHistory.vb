Imports System.Collections.Generic
Imports System.Linq

Namespace CryptoCoin.Wallet

    ''' <summary>
    ''' Tracks the complete transaction history for a wallet.
    ''' Provides filtering, balance calculation, and confirmation tracking.
    ''' </summary>
    Public Class TransactionHistory

        Private ReadOnly _transactions As Dictionary(Of String, WalletTransaction)
        Private ReadOnly _syncLock As New Object()

        ''' <summary>
        ''' Gets the total number of transactions in the history.
        ''' </summary>
        Public ReadOnly Property Count As Integer
            Get
                SyncLock _syncLock
                    Return _transactions.Count
                End SyncLock
            End Get
        End Property

        ''' <summary>
        ''' Creates a new empty transaction history.
        ''' </summary>
        Public Sub New()
            _transactions = New Dictionary(Of String, WalletTransaction)(StringComparer.OrdinalIgnoreCase)
        End Sub

        ''' <summary>
        ''' Adds a transaction to the history.
        ''' If a transaction with the same TxId already exists, it is updated.
        ''' </summary>
        ''' <param name="transaction">The wallet transaction to add.</param>
        Public Sub AddTransaction(transaction As WalletTransaction)
            If transaction Is Nothing Then Throw New ArgumentNullException(NameOf(transaction))
            If String.IsNullOrEmpty(transaction.TxId) Then
                Throw New ArgumentException("Transaction must have a valid TxId.", NameOf(transaction))
            End If

            SyncLock _syncLock
                _transactions(transaction.TxId) = transaction
            End SyncLock
        End Sub

        ''' <summary>
        ''' Removes a transaction from the history.
        ''' </summary>
        ''' <param name="txId">The transaction ID to remove.</param>
        ''' <returns>True if the transaction was removed.</returns>
        Public Function RemoveTransaction(txId As String) As Boolean
            If String.IsNullOrEmpty(txId) Then Return False

            SyncLock _syncLock
                Return _transactions.Remove(txId)
            End SyncLock
        End Function

        ''' <summary>
        ''' Gets a transaction by its ID.
        ''' </summary>
        ''' <param name="txId">The transaction hash.</param>
        ''' <returns>The wallet transaction, or Nothing if not found.</returns>
        Public Function GetTransaction(txId As String) As WalletTransaction
            If String.IsNullOrEmpty(txId) Then Return Nothing

            SyncLock _syncLock
                Dim tx As WalletTransaction = Nothing
                _transactions.TryGetValue(txId, tx)
                Return tx
            End SyncLock
        End Function

        ''' <summary>
        ''' Checks whether a transaction exists in the history.
        ''' </summary>
        ''' <param name="txId">The transaction ID to check.</param>
        ''' <returns>True if the transaction exists.</returns>
        Public Function Contains(txId As String) As Boolean
            If String.IsNullOrEmpty(txId) Then Return False

            SyncLock _syncLock
                Return _transactions.ContainsKey(txId)
            End SyncLock
        End Function

        ''' <summary>
        ''' Gets all transactions ordered by timestamp (most recent first).
        ''' </summary>
        ''' <returns>A list of all transactions sorted by date descending.</returns>
        Public Function GetAllTransactions() As List(Of WalletTransaction)
            SyncLock _syncLock
                Return _transactions.Values.
                    OrderByDescending(Function(t) t.Timestamp).
                    ToList()
            End SyncLock
        End Function

        ''' <summary>
        ''' Gets transactions filtered by direction (sent/received).
        ''' </summary>
        ''' <param name="direction">The transaction direction to filter by.</param>
        ''' <returns>Filtered list of transactions.</returns>
        Public Function GetByDirection(direction As TransactionDirection) As List(Of WalletTransaction)
            SyncLock _syncLock
                Return _transactions.Values.
                    Where(Function(t) t.Direction = direction).
                    OrderByDescending(Function(t) t.Timestamp).
                    ToList()
            End SyncLock
        End Function

        ''' <summary>
        ''' Gets transactions within a date range.
        ''' </summary>
        ''' <param name="startDate">The start date (inclusive).</param>
        ''' <param name="endDate">The end date (inclusive).</param>
        ''' <returns>Transactions within the specified date range.</returns>
        Public Function GetByDateRange(startDate As DateTime, endDate As DateTime) As List(Of WalletTransaction)
            If endDate < startDate Then
                Throw New ArgumentException("End date must be after start date.")
            End If

            SyncLock _syncLock
                Return _transactions.Values.
                    Where(Function(t) t.Timestamp >= startDate AndAlso t.Timestamp <= endDate).
                    OrderByDescending(Function(t) t.Timestamp).
                    ToList()
            End SyncLock
        End Function

        ''' <summary>
        ''' Gets transactions involving a specific address.
        ''' </summary>
        ''' <param name="address">The address to filter by.</param>
        ''' <returns>Transactions involving the specified address.</returns>
        Public Function GetByAddress(address As String) As List(Of WalletTransaction)
            If String.IsNullOrEmpty(address) Then Return New List(Of WalletTransaction)()

            SyncLock _syncLock
                Return _transactions.Values.
                    Where(Function(t) String.Equals(t.Address, address, StringComparison.OrdinalIgnoreCase)).
                    OrderByDescending(Function(t) t.Timestamp).
                    ToList()
            End SyncLock
        End Function

        ''' <summary>
        ''' Gets transactions for a specific account.
        ''' </summary>
        ''' <param name="accountIndex">The account index to filter by.</param>
        ''' <returns>Transactions for the specified account.</returns>
        Public Function GetByAccount(accountIndex As Integer) As List(Of WalletTransaction)
            SyncLock _syncLock
                Return _transactions.Values.
                    Where(Function(t) t.AccountIndex = accountIndex).
                    OrderByDescending(Function(t) t.Timestamp).
                    ToList()
            End SyncLock
        End Function

        ''' <summary>
        ''' Gets only unconfirmed (pending) transactions.
        ''' </summary>
        ''' <returns>List of unconfirmed transactions.</returns>
        Public Function GetUnconfirmed() As List(Of WalletTransaction)
            SyncLock _syncLock
                Return _transactions.Values.
                    Where(Function(t) Not t.IsConfirmed AndAlso Not t.IsAbandoned).
                    OrderByDescending(Function(t) t.Timestamp).
                    ToList()
            End SyncLock
        End Function

        ''' <summary>
        ''' Gets the most recent N transactions.
        ''' </summary>
        ''' <param name="count">The maximum number of transactions to return.</param>
        ''' <returns>The most recent transactions.</returns>
        Public Function GetRecent(count As Integer) As List(Of WalletTransaction)
            If count <= 0 Then Return New List(Of WalletTransaction)()

            SyncLock _syncLock
                Return _transactions.Values.
                    OrderByDescending(Function(t) t.Timestamp).
                    Take(count).
                    ToList()
            End SyncLock
        End Function

        ''' <summary>
        ''' Updates confirmation counts for all transactions based on the current chain height.
        ''' </summary>
        ''' <param name="currentHeight">The current blockchain height.</param>
        Public Sub UpdateConfirmations(currentHeight As Integer)
            If currentHeight < 0 Then Throw New ArgumentOutOfRangeException(NameOf(currentHeight))

            SyncLock _syncLock
                For Each tx As Object In _transactions.Values
                    tx.UpdateConfirmations(currentHeight)
                Next
            End SyncLock
        End Sub

        ''' <summary>
        ''' Calculates the total balance from transaction history.
        ''' Only includes confirmed transactions.
        ''' </summary>
        ''' <param name="requiredConfirmations">Minimum confirmations for a transaction to count.</param>
        ''' <returns>The calculated balance in satoshis.</returns>
        Public Function CalculateBalance(Optional requiredConfirmations As Integer = 1) As Long
            SyncLock _syncLock
                Dim balance As Long = 0
                For Each tx As Object In _transactions.Values
                    If tx.IsAbandoned Then Continue For
                    If tx.Confirmations >= requiredConfirmations Then
                        balance += tx.NetAmount
                    End If
                Next
                Return balance
            End SyncLock
        End Function

        ''' <summary>
        ''' Calculates the total amount sent (confirmed transactions only).
        ''' </summary>
        ''' <returns>Total sent amount in satoshis.</returns>
        Public Function GetTotalSent() As Long
            SyncLock _syncLock
                Dim total As Long = 0
                For Each tx As Object In _transactions.Values
                    If tx.Direction = TransactionDirection.Sent AndAlso tx.IsConfirmed AndAlso Not tx.IsAbandoned Then
                        total += tx.Amount + tx.Fee
                    End If
                Next
                Return total
            End SyncLock
        End Function

        ''' <summary>
        ''' Calculates the total amount received (confirmed transactions only).
        ''' </summary>
        ''' <returns>Total received amount in satoshis.</returns>
        Public Function GetTotalReceived() As Long
            SyncLock _syncLock
                Dim total As Long = 0
                For Each tx As Object In _transactions.Values
                    If tx.Direction = TransactionDirection.Received AndAlso tx.IsConfirmed AndAlso Not tx.IsAbandoned Then
                        total += tx.Amount
                    End If
                Next
                Return total
            End SyncLock
        End Function

        ''' <summary>
        ''' Gets the total fees paid across all sent transactions.
        ''' </summary>
        ''' <returns>Total fees in satoshis.</returns>
        Public Function GetTotalFees() As Long
            SyncLock _syncLock
                Dim total As Long = 0
                For Each tx As Object In _transactions.Values
                    If tx.Direction = TransactionDirection.Sent AndAlso Not tx.IsAbandoned Then
                        total += tx.Fee
                    End If
                Next
                Return total
            End SyncLock
        End Function

        ''' <summary>
        ''' Marks a transaction as abandoned (e.g., dropped from mempool).
        ''' </summary>
        ''' <param name="txId">The transaction ID to abandon.</param>
        ''' <returns>True if the transaction was found and marked.</returns>
        Public Function AbandonTransaction(txId As String) As Boolean
            If String.IsNullOrEmpty(txId) Then Return False

            SyncLock _syncLock
                Dim tx As WalletTransaction = Nothing
                If _transactions.TryGetValue(txId, tx) Then
                    If tx.IsConfirmed Then Return False ' Cannot abandon confirmed tx
                    tx.IsAbandoned = True
                    Return True
                End If
            End SyncLock

            Return False
        End Function

        ''' <summary>
        ''' Clears all transaction history.
        ''' </summary>
        Public Sub Clear()
            SyncLock _syncLock
                _transactions.Clear()
            End SyncLock
        End Sub

    End Class

End Namespace
