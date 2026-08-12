Namespace CryptoCoin.Transactions

    ''' <summary>
    ''' Manages the set of all unspent transaction outputs (UTXOs) in the blockchain.
    ''' This is the core data structure for validating transactions.
    ''' </summary>
    Public Class UtxoSet

        Private ReadOnly _utxos As New Dictionary(Of String, UtxoEntry)(StringComparer.OrdinalIgnoreCase)
        Private ReadOnly _syncLock As New Object()

        ''' <summary>
        ''' Gets the number of UTXOs in the set.
        ''' </summary>
        Public ReadOnly Property Count As Integer
            Get
                SyncLock _syncLock
                    Return _utxos.Count
                End SyncLock
            End Get
        End Property

        ''' <summary>
        ''' Gets the total value of all UTXOs in satoshis.
        ''' </summary>
        Public ReadOnly Property TotalValue As Long
            Get
                SyncLock _syncLock
                    Dim total As Long = 0
                    For Each entry As UtxoEntry In _utxos.Values
                        total += entry.Value
                    Next
                    Return total
                End SyncLock
            End Get
        End Property

        ''' <summary>
        ''' Adds a UTXO to the set.
        ''' </summary>
        Public Sub Add(entry As UtxoEntry)
            If entry Is Nothing Then Throw New ArgumentNullException(NameOf(entry))
            Dim key As String = GetKey(entry.TxHash, entry.OutputIndex)
            SyncLock _syncLock
                _utxos(key) = entry
            End SyncLock
        End Sub

        ''' <summary>
        ''' Adds a transaction output as a UTXO.
        ''' </summary>
        Public Sub Add(txHash As String, outputIndex As Integer, output As TransactionOutput, blockHeight As Integer, isCoinbase As Boolean)
            Dim entry As New UtxoEntry(output, blockHeight, isCoinbase, txHash, outputIndex)
            Add(entry)
        End Sub

        ''' <summary>
        ''' Removes (spends) a UTXO from the set.
        ''' </summary>
        Public Function Spend(txHash As String, outputIndex As Integer) As UtxoEntry
            Dim key As String = GetKey(txHash, outputIndex)
            SyncLock _syncLock
                Dim entry As UtxoEntry = Nothing
                If _utxos.TryGetValue(key, entry) Then
                    _utxos.Remove(key)
                    Return entry
                End If
                Return Nothing
            End SyncLock
        End Function

        ''' <summary>
        ''' Removes a UTXO by outpoint.
        ''' </summary>
        Public Function Spend(outpoint As OutPoint) As UtxoEntry
            If outpoint Is Nothing Then Return Nothing
            Return Spend(outpoint.TxHash, CInt(outpoint.OutputIndex))
        End Function

        ''' <summary>
        ''' Gets a UTXO entry without removing it.
        ''' </summary>
        Public Function [Get](txHash As String, outputIndex As Integer) As UtxoEntry
            Dim key As String = GetKey(txHash, outputIndex)
            SyncLock _syncLock
                Dim entry As UtxoEntry = Nothing
                _utxos.TryGetValue(key, entry)
                Return entry
            End SyncLock
        End Function

        ''' <summary>
        ''' Gets a UTXO entry by outpoint.
        ''' </summary>
        Public Function [Get](outpoint As OutPoint) As UtxoEntry
            If outpoint Is Nothing Then Return Nothing
            Return [Get](outpoint.TxHash, CInt(outpoint.OutputIndex))
        End Function

        ''' <summary>
        ''' Checks if a UTXO exists in the set.
        ''' </summary>
        Public Function Contains(txHash As String, outputIndex As Integer) As Boolean
            Dim key As String = GetKey(txHash, outputIndex)
            SyncLock _syncLock
                Return _utxos.ContainsKey(key)
            End SyncLock
        End Function

        ''' <summary>
        ''' Checks if a UTXO exists by outpoint.
        ''' </summary>
        Public Function Contains(outpoint As OutPoint) As Boolean
            If outpoint Is Nothing Then Return False
            Return Contains(outpoint.TxHash, CInt(outpoint.OutputIndex))
        End Function

        ''' <summary>
        ''' Gets all UTXOs belonging to a specific address (by matching scriptPubKey).
        ''' </summary>
        Public Function GetByAddress(address As String) As List(Of UtxoEntry)
            Dim addressHash As Byte() = Cryptography.AddressEncoder.GetHash160(address)
            Dim results As New List(Of UtxoEntry)()

            SyncLock _syncLock
                For Each entry As UtxoEntry In _utxos.Values
                    If MatchesAddress(entry.ScriptPubKey, addressHash) Then
                        results.Add(entry)
                    End If
                Next
            End SyncLock

            Return results
        End Function

        ''' <summary>
        ''' Gets the balance for a specific address.
        ''' </summary>
        Public Function GetBalance(address As String) As Long
            Dim utxos As List(Of UtxoEntry) = GetByAddress(address)
            Dim total As Long = 0
            For Each entry As UtxoEntry In utxos
                total += entry.Value
            Next
            Return total
        End Function

        ''' <summary>
        ''' Applies a transaction to the UTXO set (spend inputs, add outputs).
        ''' </summary>
        Public Sub ApplyTransaction(tx As Transaction, blockHeight As Integer)
            If tx Is Nothing Then Return

            ' Spend inputs (skip coinbase)
            If Not tx.IsCoinbase Then
                For Each input As TransactionInput In tx.Inputs
                    Spend(input.PreviousOutput)
                Next
            End If

            ' Add outputs
            For i As Integer = 0 To tx.Outputs.Count - 1
                If Not tx.Outputs(i).IsUnspendable Then
                    Add(tx.TxId, i, tx.Outputs(i), blockHeight, tx.IsCoinbase)
                End If
            Next
        End Sub

        ''' <summary>
        ''' Reverts a transaction from the UTXO set (used during chain reorganization).
        ''' </summary>
        Public Sub RevertTransaction(tx As Transaction, spentOutputs As List(Of UtxoEntry))
            If tx Is Nothing Then Return

            ' Remove outputs that were added
            For i As Integer = 0 To tx.Outputs.Count - 1
                Spend(tx.TxId, i)
            Next

            ' Restore spent inputs
            If spentOutputs IsNot Nothing Then
                For Each spent As UtxoEntry In spentOutputs
                    Add(spent)
                Next
            End If
        End Sub

        Private Shared Function GetKey(txHash As String, outputIndex As Integer) As String
            Return $"{txHash}:{outputIndex}"
        End Function

        Private Shared Function MatchesAddress(scriptPubKey As Byte(), addressHash As Byte()) As Boolean
            ' Simple P2PKH matching: OP_DUP OP_HASH160 <20 bytes> OP_EQUALVERIFY OP_CHECKSIG
            If scriptPubKey Is Nothing OrElse scriptPubKey.Length < 25 Then Return False
            If scriptPubKey(0) <> Script.OpCodes.OP_DUP Then Return False
            If scriptPubKey(1) <> Script.OpCodes.OP_HASH160 Then Return False
            If scriptPubKey(2) <> 20 Then Return False

            ' Compare hash bytes
            For i As Integer = 0 To 19
                If scriptPubKey(3 + i) <> addressHash(i) Then Return False
            Next
            Return True
        End Function

        ''' <summary>
        ''' Clears all UTXOs.
        ''' </summary>
        Public Sub Clear()
            SyncLock _syncLock
                _utxos.Clear()
            End SyncLock
        End Sub

    End Class

End Namespace
