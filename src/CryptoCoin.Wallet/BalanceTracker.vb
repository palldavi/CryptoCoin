Imports System.Collections.Generic
Imports System.Linq

Namespace CryptoCoin.Wallet

    ''' <summary>
    ''' Tracks wallet balances including confirmed, unconfirmed, and immature amounts.
    ''' Manages the set of unspent transaction outputs (UTXOs) owned by the wallet.
    ''' </summary>
    Public Class BalanceTracker

        Private ReadOnly _utxos As Dictionary(Of String, WalletUtxo)
        Private ReadOnly _syncLock As New Object()
        Private ReadOnly _config As WalletConfig

        ''' <summary>
        ''' Gets the confirmed balance in satoshis.
        ''' Only includes UTXOs with sufficient confirmations.
        ''' </summary>
        Public ReadOnly Property ConfirmedBalance As Long
            Get
                SyncLock _syncLock
                    Dim total As Long = 0
                    For Each utxo As WalletUtxo In _utxos.Values
                        If Not utxo.IsSpent AndAlso utxo.Confirmations >= _config.RequiredConfirmations Then
                            If Not utxo.IsCoinbase OrElse utxo.Confirmations >= _config.CoinbaseMaturity Then
                                total += utxo.Amount
                            End If
                        End If
                    Next
                    Return total
                End SyncLock
            End Get
        End Property

        ''' <summary>
        ''' Gets the unconfirmed balance in satoshis.
        ''' Includes UTXOs with fewer than required confirmations (but at least 0).
        ''' </summary>
        Public ReadOnly Property UnconfirmedBalance As Long
            Get
                SyncLock _syncLock
                    Dim total As Long = 0
                    For Each utxo As WalletUtxo In _utxos.Values
                        If Not utxo.IsSpent AndAlso utxo.Confirmations < _config.RequiredConfirmations Then
                            If Not utxo.IsCoinbase Then
                                total += utxo.Amount
                            End If
                        End If
                    Next
                    Return total
                End SyncLock
            End Get
        End Property

        ''' <summary>
        ''' Gets the immature balance in satoshis.
        ''' Includes coinbase UTXOs that have not yet reached maturity.
        ''' </summary>
        Public ReadOnly Property ImmatureBalance As Long
            Get
                SyncLock _syncLock
                    Dim total As Long = 0
                    For Each utxo As WalletUtxo In _utxos.Values
                        If Not utxo.IsSpent AndAlso utxo.IsCoinbase AndAlso
                           utxo.Confirmations < _config.CoinbaseMaturity Then
                            total += utxo.Amount
                        End If
                    Next
                    Return total
                End SyncLock
            End Get
        End Property

        ''' <summary>
        ''' Gets the total balance (confirmed + unconfirmed, excluding immature).
        ''' </summary>
        Public ReadOnly Property TotalBalance As Long
            Get
                Return ConfirmedBalance + UnconfirmedBalance
            End Get
        End Property

        ''' <summary>
        ''' Gets the number of unspent outputs tracked.
        ''' </summary>
        Public ReadOnly Property UtxoCount As Integer
            Get
                SyncLock _syncLock
                    Return _utxos.Values.Where(Function(u) Not u.IsSpent).Count()
                End SyncLock
            End Get
        End Property

        ''' <summary>
        ''' Creates a new balance tracker with the specified configuration.
        ''' </summary>
        ''' <param name="config">The wallet configuration.</param>
        Public Sub New(config As WalletConfig)
            If config Is Nothing Then Throw New ArgumentNullException(NameOf(config))
            _config = config
            _utxos = New Dictionary(Of String, WalletUtxo)(StringComparer.OrdinalIgnoreCase)
        End Sub

        ''' <summary>
        ''' Adds a new UTXO to the tracker.
        ''' </summary>
        ''' <param name="utxo">The unspent output to track.</param>
        Public Sub AddUtxo(utxo As WalletUtxo)
            If utxo Is Nothing Then Throw New ArgumentNullException(NameOf(utxo))

            Dim key As String = GetUtxoKey(utxo.TxId, utxo.OutputIndex)

            SyncLock _syncLock
                _utxos(key) = utxo
            End SyncLock
        End Sub

        ''' <summary>
        ''' Marks a UTXO as spent.
        ''' </summary>
        ''' <param name="txId">The transaction ID containing the output.</param>
        ''' <param name="outputIndex">The output index within the transaction.</param>
        ''' <returns>True if the UTXO was found and marked as spent.</returns>
        Public Function SpendUtxo(txId As String, outputIndex As Integer) As Boolean
            Dim key As String = GetUtxoKey(txId, outputIndex)

            SyncLock _syncLock
                Dim utxo As WalletUtxo = Nothing
                If _utxos.TryGetValue(key, utxo) Then
                    utxo.IsSpent = True
                    utxo.SpentInTxId = txId
                    Return True
                End If
            End SyncLock

            Return False
        End Function

        ''' <summary>
        ''' Removes a UTXO from the tracker entirely (e.g., on reorg).
        ''' </summary>
        ''' <param name="txId">The transaction ID.</param>
        ''' <param name="outputIndex">The output index.</param>
        ''' <returns>True if the UTXO was removed.</returns>
        Public Function RemoveUtxo(txId As String, outputIndex As Integer) As Boolean
            Dim key As String = GetUtxoKey(txId, outputIndex)

            SyncLock _syncLock
                Return _utxos.Remove(key)
            End SyncLock
        End Function

        ''' <summary>
        ''' Gets all unspent UTXOs available for spending.
        ''' Excludes immature coinbase outputs and already-spent outputs.
        ''' </summary>
        ''' <returns>List of spendable UTXOs.</returns>
        Public Function GetSpendableUtxos() As List(Of WalletUtxo)
            SyncLock _syncLock
                Return _utxos.Values.
                    Where(Function(u) Not u.IsSpent AndAlso IsSpendable(u)).
                    OrderByDescending(Function(u) u.Amount).
                    ToList()
            End SyncLock
        End Function

        ''' <summary>
        ''' Gets all unspent UTXOs regardless of maturity.
        ''' </summary>
        ''' <returns>List of all unspent UTXOs.</returns>
        Public Function GetAllUnspent() As List(Of WalletUtxo)
            SyncLock _syncLock
                Return _utxos.Values.
                    Where(Function(u) Not u.IsSpent).
                    ToList()
            End SyncLock
        End Function

        ''' <summary>
        ''' Selects UTXOs to cover the specified amount using a simple largest-first strategy.
        ''' </summary>
        ''' <param name="targetAmount">The amount needed in satoshis.</param>
        ''' <returns>Selected UTXOs, or Nothing if insufficient funds.</returns>
        Public Function SelectUtxos(targetAmount As Long) As List(Of WalletUtxo)
            If targetAmount <= 0 Then Throw New ArgumentOutOfRangeException(NameOf(targetAmount))

            Dim spendable As List(Of WalletUtxo) = GetSpendableUtxos()
            Dim selected As New List(Of WalletUtxo)()
            Dim accumulated As Long = 0

            For Each utxo As WalletUtxo In spendable
                selected.Add(utxo)
                accumulated += utxo.Amount
                If accumulated >= targetAmount Then
                    Return selected
                End If
            Next

            ' Insufficient funds
            Return Nothing
        End Function

        ''' <summary>
        ''' Updates confirmation counts for all UTXOs based on current chain height.
        ''' </summary>
        ''' <param name="currentHeight">The current blockchain height.</param>
        Public Sub UpdateConfirmations(currentHeight As Integer)
            If currentHeight < 0 Then Throw New ArgumentOutOfRangeException(NameOf(currentHeight))

            SyncLock _syncLock
                For Each utxo As WalletUtxo In _utxos.Values
                    If utxo.BlockHeight >= 0 Then
                        utxo.Confirmations = Math.Max(0, currentHeight - utxo.BlockHeight + 1)
                    Else
                        utxo.Confirmations = 0
                    End If
                Next
            End SyncLock
        End Sub

        ''' <summary>
        ''' Gets a specific UTXO by transaction ID and output index.
        ''' </summary>
        ''' <param name="txId">The transaction ID.</param>
        ''' <param name="outputIndex">The output index.</param>
        ''' <returns>The UTXO, or Nothing if not found.</returns>
        Public Function GetUtxo(txId As String, outputIndex As Integer) As WalletUtxo
            Dim key As String = GetUtxoKey(txId, outputIndex)

            SyncLock _syncLock
                Dim utxo As WalletUtxo = Nothing
                _utxos.TryGetValue(key, utxo)
                Return utxo
            End SyncLock
        End Function

        ''' <summary>
        ''' Clears all tracked UTXOs.
        ''' </summary>
        Public Sub Clear()
            SyncLock _syncLock
                _utxos.Clear()
            End SyncLock
        End Sub

        Private Function IsSpendable(utxo As WalletUtxo) As Boolean
            If utxo.IsCoinbase Then
                Return utxo.Confirmations >= _config.CoinbaseMaturity
            End If
            Return utxo.Confirmations >= _config.RequiredConfirmations
        End Function

        Private Shared Function GetUtxoKey(txId As String, outputIndex As Integer) As String
            Return $"{txId}:{outputIndex}"
        End Function

    End Class

    ''' <summary>
    ''' Represents an unspent transaction output (UTXO) owned by the wallet.
    ''' </summary>
    Public Class WalletUtxo

        ''' <summary>The transaction ID that created this output.</summary>
        Public Property TxId As String

        ''' <summary>The output index within the transaction.</summary>
        Public Property OutputIndex As Integer

        ''' <summary>The amount in satoshis.</summary>
        Public Property Amount As Long

        ''' <summary>The address that owns this output.</summary>
        Public Property Address As String

        ''' <summary>The scriptPubKey for this output.</summary>
        Public Property ScriptPubKey As Byte()

        ''' <summary>The block height where this UTXO was confirmed, or -1 if unconfirmed.</summary>
        Public Property BlockHeight As Integer = -1

        ''' <summary>The number of confirmations.</summary>
        Public Property Confirmations As Integer = 0

        ''' <summary>Whether this is a coinbase output.</summary>
        Public Property IsCoinbase As Boolean = False

        ''' <summary>Whether this UTXO has been spent.</summary>
        Public Property IsSpent As Boolean = False

        ''' <summary>The transaction ID that spent this UTXO (if spent).</summary>
        Public Property SpentInTxId As String

        ''' <summary>The account index that owns this UTXO.</summary>
        Public Property AccountIndex As Integer

        Public Overrides Function ToString() As String
            Return $"{TxId}:{OutputIndex} = {Amount} sat ({Confirmations} conf)"
        End Function

    End Class

End Namespace
