' ===============================================================================
' CryptoCoin.Contracts - StandardContracts\MultiSigContract.vb
' Multi-signature wallet contract requiring M-of-N approvals for transactions.
' ===============================================================================

Imports System
Imports System.Collections.Generic
Imports System.Numerics
Imports System.Text
Imports CryptoCoin.Cryptography

Namespace CryptoCoin.Contracts.StandardContracts

    ''' <summary>
    ''' Implements a multi-signature wallet contract that requires a minimum number
    ''' of owner approvals before executing transactions. Supports adding/removing owners,
    ''' submitting transactions, and changing the required approval threshold.
    ''' </summary>
    ''' <remarks>
    ''' Storage layout:
    '''   "requiredSigs"          -> Required signature count (integer)
    '''   "ownerCount"            -> Total number of owners (integer)
    '''   "owner:{index}"         -> Owner address at index
    '''   "isOwner:{address}"     -> Whether address is an owner (1/0)
    '''   "txCount"               -> Total submitted transaction count
    '''   "tx:{id}:to"            -> Transaction destination address
    '''   "tx:{id}:value"         -> Transaction value
    '''   "tx:{id}:data"          -> Transaction data
    '''   "tx:{id}:executed"      -> Whether transaction has been executed (1/0)
    '''   "tx:{id}:confirmations" -> Number of confirmations
    '''   "tx:{id}:confirmed:{addr}" -> Whether address confirmed this tx (1/0)
    ''' </remarks>
    Public Class MultiSigContract

        Private ReadOnly _storage As ContractStorage
        Private ReadOnly _contractAddress As Byte()

        ''' <summary>Gets the number of required signatures for execution.</summary>
        Public ReadOnly Property RequiredSignatures As Integer
            Get
                Return CInt(GetStorageInteger("requiredSigs"))
            End Get
        End Property

        ''' <summary>Gets the total number of owners.</summary>
        Public ReadOnly Property OwnerCount As Integer
            Get
                Return CInt(GetStorageInteger("ownerCount"))
            End Get
        End Property

        ''' <summary>Gets the total number of submitted transactions.</summary>
        Public ReadOnly Property TransactionCount As Integer
            Get
                Return CInt(GetStorageInteger("txCount"))
            End Get
        End Property

        ''' <summary>
        ''' Initializes a new MultiSigContract with existing storage.
        ''' </summary>
        ''' <param name="storage">The contract storage instance.</param>
        ''' <param name="contractAddress">The contract's address.</param>
        Public Sub New(storage As ContractStorage, contractAddress As Byte())
            _storage = storage
            _contractAddress = contractAddress
        End Sub

        ''' <summary>
        ''' Initializes the multi-sig contract with owners and required signature count.
        ''' </summary>
        ''' <param name="owners">The list of owner addresses.</param>
        ''' <param name="requiredSignatures">The minimum number of approvals required.</param>
        Public Sub Initialize(owners As List(Of Byte()), requiredSignatures As Integer)
            If owners Is Nothing OrElse owners.Count = 0 Then
                Throw New ArgumentException("At least one owner is required.")
            End If
            If requiredSignatures <= 0 OrElse requiredSignatures > owners.Count Then
                Throw New ArgumentException("Required signatures must be between 1 and owner count.")
            End If

            SetStorageInteger("requiredSigs", New BigInteger(requiredSignatures))
            SetStorageInteger("ownerCount", New BigInteger(owners.Count))
            SetStorageInteger("txCount", BigInteger.Zero)

            For i As Integer = 0 To owners.Count - 1
                Dim ownerHex As String = BytesToHex(owners(i))
                _storage.PutByString($"owner:{i}", owners(i))
                _storage.PutByString($"isOwner:{ownerHex}", New Byte() {1})
            Next
        End Sub

        ''' <summary>
        ''' Submits a new transaction for approval by the owners.
        ''' </summary>
        ''' <param name="caller">The submitter's address (must be an owner).</param>
        ''' <param name="destination">The transaction destination address.</param>
        ''' <param name="value">The CRC value to send.</param>
        ''' <param name="data">Optional transaction data.</param>
        ''' <returns>The transaction ID, or -1 if submission failed.</returns>
        Public Function SubmitTransaction(caller As Byte(), destination As Byte(),
                                          value As Long, Optional data As Byte() = Nothing) As Integer
            If Not IsOwner(caller) Then Return -1
            If destination Is Nothing Then Return -1

            Dim txId As Integer = TransactionCount
            Dim txIdStr As String = txId.ToString()

            ' Store transaction details
            _storage.PutByString($"tx:{txIdStr}:to", destination)
            SetStorageInteger($"tx:{txIdStr}:value", New BigInteger(value))
            If data IsNot Nothing Then
                _storage.PutByString($"tx:{txIdStr}:data", data)
            End If
            _storage.PutByString($"tx:{txIdStr}:executed", New Byte() {0})
            SetStorageInteger($"tx:{txIdStr}:confirmations", BigInteger.Zero)

            ' Increment transaction count
            SetStorageInteger("txCount", New BigInteger(txId + 1))

            ' Auto-confirm by submitter
            ConfirmTransaction(caller, txId)

            Return txId
        End Function

        ''' <summary>
        ''' Confirms (approves) a pending transaction.
        ''' </summary>
        ''' <param name="caller">The confirmer's address (must be an owner).</param>
        ''' <param name="txId">The transaction ID to confirm.</param>
        ''' <returns>True if confirmation was recorded; otherwise, False.</returns>
        Public Function ConfirmTransaction(caller As Byte(), txId As Integer) As Boolean
            If Not IsOwner(caller) Then Return False
            If txId < 0 OrElse txId >= TransactionCount Then Return False

            Dim txIdStr As String = txId.ToString()
            Dim callerHex As String = BytesToHex(caller)

            ' Check if already executed
            Dim executedData As Byte() = _storage.GetByString($"tx:{txIdStr}:executed")
            If executedData IsNot Nothing AndAlso executedData.Length > 0 AndAlso executedData(0) = 1 Then
                Return False ' Already executed
            End If

            ' Check if already confirmed by this owner
            Dim confirmKey As String = $"tx:{txIdStr}:confirmed:{callerHex}"
            Dim alreadyConfirmed As Byte() = _storage.GetByString(confirmKey)
            If alreadyConfirmed IsNot Nothing AndAlso alreadyConfirmed.Length > 0 AndAlso alreadyConfirmed(0) = 1 Then
                Return False ' Already confirmed
            End If

            ' Record confirmation
            _storage.PutByString(confirmKey, New Byte() {1})
            Dim confirmations As BigInteger = GetStorageInteger($"tx:{txIdStr}:confirmations")
            SetStorageInteger($"tx:{txIdStr}:confirmations", confirmations + BigInteger.One)

            Return True
        End Function

        ''' <summary>
        ''' Revokes a previous confirmation for a pending transaction.
        ''' </summary>
        ''' <param name="caller">The revoker's address (must be an owner who confirmed).</param>
        ''' <param name="txId">The transaction ID to revoke confirmation for.</param>
        ''' <returns>True if revocation was recorded; otherwise, False.</returns>
        Public Function RevokeConfirmation(caller As Byte(), txId As Integer) As Boolean
            If Not IsOwner(caller) Then Return False
            If txId < 0 OrElse txId >= TransactionCount Then Return False

            Dim txIdStr As String = txId.ToString()
            Dim callerHex As String = BytesToHex(caller)

            ' Check if executed
            Dim executedData As Byte() = _storage.GetByString($"tx:{txIdStr}:executed")
            If executedData IsNot Nothing AndAlso executedData.Length > 0 AndAlso executedData(0) = 1 Then
                Return False
            End If

            ' Check if confirmed
            Dim confirmKey As String = $"tx:{txIdStr}:confirmed:{callerHex}"
            Dim confirmed As Byte() = _storage.GetByString(confirmKey)
            If confirmed Is Nothing OrElse confirmed.Length = 0 OrElse confirmed(0) <> 1 Then
                Return False ' Not confirmed
            End If

            ' Revoke
            _storage.PutByString(confirmKey, New Byte() {0})
            Dim confirmations As BigInteger = GetStorageInteger($"tx:{txIdStr}:confirmations")
            If confirmations > BigInteger.Zero Then
                SetStorageInteger($"tx:{txIdStr}:confirmations", confirmations - BigInteger.One)
            End If

            Return True
        End Function

        ''' <summary>
        ''' Executes a transaction that has received enough confirmations.
        ''' </summary>
        ''' <param name="caller">The executor's address (must be an owner).</param>
        ''' <param name="txId">The transaction ID to execute.</param>
        ''' <returns>True if the transaction was executed; otherwise, False.</returns>
        Public Function ExecuteTransaction(caller As Byte(), txId As Integer) As Boolean
            If Not IsOwner(caller) Then Return False
            If txId < 0 OrElse txId >= TransactionCount Then Return False

            Dim txIdStr As String = txId.ToString()

            ' Check if already executed
            Dim executedData As Byte() = _storage.GetByString($"tx:{txIdStr}:executed")
            If executedData IsNot Nothing AndAlso executedData.Length > 0 AndAlso executedData(0) = 1 Then
                Return False
            End If

            ' Check if enough confirmations
            Dim confirmations As Integer = CInt(GetStorageInteger($"tx:{txIdStr}:confirmations"))
            If confirmations < RequiredSignatures Then
                Return False ' Not enough confirmations
            End If

            ' Mark as executed
            _storage.PutByString($"tx:{txIdStr}:executed", New Byte() {1})

            ' In a full implementation, this would transfer the value to the destination
            Return True
        End Function

        ''' <summary>
        ''' Gets the confirmation count for a transaction.
        ''' </summary>
        ''' <param name="txId">The transaction ID.</param>
        ''' <returns>The number of confirmations.</returns>
        Public Function GetConfirmationCount(txId As Integer) As Integer
            If txId < 0 OrElse txId >= TransactionCount Then Return 0
            Return CInt(GetStorageInteger($"tx:{txId}:confirmations"))
        End Function

        ''' <summary>
        ''' Checks if a transaction has been executed.
        ''' </summary>
        ''' <param name="txId">The transaction ID.</param>
        ''' <returns>True if executed; otherwise, False.</returns>
        Public Function IsTransactionExecuted(txId As Integer) As Boolean
            If txId < 0 OrElse txId >= TransactionCount Then Return False
            Dim data As Byte() = _storage.GetByString($"tx:{txId}:executed")
            Return data IsNot Nothing AndAlso data.Length > 0 AndAlso data(0) = 1
        End Function

        ''' <summary>
        ''' Checks if the specified address is an owner of this multi-sig wallet.
        ''' </summary>
        ''' <param name="address">The address to check.</param>
        ''' <returns>True if the address is an owner; otherwise, False.</returns>
        Public Function IsOwner(address As Byte()) As Boolean
            If address Is Nothing Then Return False
            Dim key As String = $"isOwner:{BytesToHex(address)}"
            Dim data As Byte() = _storage.GetByString(key)
            Return data IsNot Nothing AndAlso data.Length > 0 AndAlso data(0) = 1
        End Function

        ''' <summary>
        ''' Gets the list of all owner addresses.
        ''' </summary>
        ''' <returns>A list of owner addresses.</returns>
        Public Function GetOwners() As List(Of Byte())
            Dim owners As New List(Of Byte())()
            Dim count As Integer = OwnerCount

            For i As Integer = 0 To count - 1
                Dim ownerData As Byte() = _storage.GetByString($"owner:{i}")
                If ownerData IsNot Nothing Then
                    owners.Add(ownerData)
                End If
            Next

            Return owners
        End Function

        Private Function GetStorageInteger(key As String) As BigInteger
            Dim data As Byte() = _storage.GetByString(key)
            If data Is Nothing OrElse data.Length = 0 Then Return BigInteger.Zero
            Return New BigInteger(data)
        End Function

        Private Sub SetStorageInteger(key As String, value As BigInteger)
            _storage.PutByString(key, value.ToByteArray())
        End Sub

        Private Function BytesToHex(data As Byte()) As String
            If data Is Nothing Then Return String.Empty
            Return BitConverter.ToString(data).Replace("-", "").ToLowerInvariant()
        End Function

    End Class

End Namespace
