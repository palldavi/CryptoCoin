Namespace CryptoCoin.Wallet

    ''' <summary>
    ''' Represents a transaction as viewed from the wallet's perspective.
    ''' Tracks direction (sent/received), amount, fee, confirmations, and metadata.
    ''' </summary>
    Public Class WalletTransaction

        ''' <summary>
        ''' The unique transaction hash (txid) as a hex string.
        ''' </summary>
        Public Property TxId As String

        ''' <summary>
        ''' The direction of the transaction relative to this wallet.
        ''' </summary>
        Public Property Direction As TransactionDirection

        ''' <summary>
        ''' The net amount transferred in satoshis (positive for received, positive for sent amount).
        ''' </summary>
        Public Property Amount As Long

        ''' <summary>
        ''' The transaction fee in satoshis (only meaningful for sent transactions).
        ''' </summary>
        Public Property Fee As Long

        ''' <summary>
        ''' The number of confirmations this transaction has received.
        ''' </summary>
        Public Property Confirmations As Integer

        ''' <summary>
        ''' The block hash where this transaction was included, or Nothing if unconfirmed.
        ''' </summary>
        Public Property BlockHash As String

        ''' <summary>
        ''' The block height where this transaction was included, or -1 if unconfirmed.
        ''' </summary>
        Public Property BlockHeight As Integer = -1

        ''' <summary>
        ''' The timestamp when this transaction was first seen or confirmed.
        ''' </summary>
        Public Property Timestamp As DateTime

        ''' <summary>
        ''' The destination address (for sent) or receiving address (for received).
        ''' </summary>
        Public Property Address As String

        ''' <summary>
        ''' An optional user-provided memo or label for this transaction.
        ''' </summary>
        Public Property Memo As String = String.Empty

        ''' <summary>
        ''' The account index that owns this transaction.
        ''' </summary>
        Public Property AccountIndex As Integer

        ''' <summary>
        ''' Whether this transaction has been abandoned (removed from mempool without confirmation).
        ''' </summary>
        Public Property IsAbandoned As Boolean = False

        ''' <summary>
        ''' Whether this is a coinbase (mining reward) transaction.
        ''' </summary>
        Public Property IsCoinbase As Boolean = False

        ''' <summary>
        ''' The raw transaction bytes for reference.
        ''' </summary>
        Public Property RawTransaction As Byte()

        ''' <summary>
        ''' Creates a new wallet transaction record.
        ''' </summary>
        Public Sub New()
            Timestamp = DateTime.UtcNow
        End Sub

        ''' <summary>
        ''' Creates a wallet transaction with the specified parameters.
        ''' </summary>
        ''' <param name="txId">The transaction hash.</param>
        ''' <param name="direction">Whether this was sent or received.</param>
        ''' <param name="amount">The amount in satoshis.</param>
        ''' <param name="fee">The fee in satoshis.</param>
        Public Sub New(txId As String, direction As TransactionDirection, amount As Long, fee As Long)
            If String.IsNullOrEmpty(txId) Then Throw New ArgumentNullException(NameOf(txId))
            If amount < 0 Then Throw New ArgumentOutOfRangeException(NameOf(amount), "Amount cannot be negative.")
            If fee < 0 Then Throw New ArgumentOutOfRangeException(NameOf(fee), "Fee cannot be negative.")

            Me.TxId = txId
            Me.Direction = direction
            Me.Amount = amount
            Me.Fee = fee
            Me.Timestamp = DateTime.UtcNow
        End Sub

        ''' <summary>
        ''' Gets whether this transaction is confirmed (has at least one confirmation).
        ''' </summary>
        Public ReadOnly Property IsConfirmed As Boolean
            Get
                Return Confirmations > 0
            End Get
        End Property

        ''' <summary>
        ''' Gets whether this transaction is considered mature (enough confirmations).
        ''' For coinbase transactions, requires coinbase maturity (100 blocks).
        ''' </summary>
        ''' <param name="requiredConfirmations">Standard confirmation requirement.</param>
        ''' <param name="coinbaseMaturity">Coinbase maturity requirement.</param>
        Public Function IsMature(requiredConfirmations As Integer, coinbaseMaturity As Integer) As Boolean
            If IsCoinbase Then
                Return Confirmations >= coinbaseMaturity
            End If
            Return Confirmations >= requiredConfirmations
        End Function

        ''' <summary>
        ''' Updates the confirmation count based on the current chain height.
        ''' </summary>
        ''' <param name="currentHeight">The current blockchain height.</param>
        Public Sub UpdateConfirmations(currentHeight As Integer)
            If BlockHeight < 0 Then
                Confirmations = 0
            Else
                Confirmations = Math.Max(0, currentHeight - BlockHeight + 1)
            End If
        End Sub

        ''' <summary>
        ''' Gets the effective amount considering direction and fee.
        ''' For sent transactions, returns -(amount + fee).
        ''' For received transactions, returns +amount.
        ''' </summary>
        Public ReadOnly Property NetAmount As Long
            Get
                If Direction = TransactionDirection.Sent Then
                    Return -(Amount + Fee)
                Else
                    Return Amount
                End If
            End Get
        End Property

        ''' <summary>
        ''' Returns a human-readable description of this transaction.
        ''' </summary>
        Public Overrides Function ToString() As String
            Dim dirStr As String = If(Direction = TransactionDirection.Sent, "Sent", "Received")
            Dim confStr As String = If(IsConfirmed, $"{Confirmations} conf", "unconfirmed")
            Return $"{dirStr} {Amount} satoshis ({confStr}) - {TxId}"
        End Function

    End Class

    ''' <summary>
    ''' Indicates the direction of a wallet transaction.
    ''' </summary>
    Public Enum TransactionDirection
        ''' <summary>Transaction was sent from this wallet.</summary>
        Sent = 0
        ''' <summary>Transaction was received by this wallet.</summary>
        Received = 1
        ''' <summary>Transaction is internal (sent to self, e.g., change).</summary>
        Internal = 2
    End Enum

End Namespace
