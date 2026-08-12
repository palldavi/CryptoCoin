' ===============================================================================
' CryptoCoin.Contracts - ContractStorage.vb
' Key-value storage for smart contract persistent state.
' ===============================================================================

Imports System
Imports System.Collections.Generic
Imports System.Numerics
Imports CryptoCoin.Cryptography

Namespace CryptoCoin.Contracts

    ''' <summary>
    ''' Provides persistent key-value storage for smart contract state.
    ''' Supports byte array keys and values with snapshot/rollback capabilities
    ''' for transaction atomicity.
    ''' </summary>
    Public Class ContractStorage

        Private ReadOnly _store As Dictionary(Of String, Byte())
        Private _snapshot As Dictionary(Of String, Byte())
        Private _isDirty As Boolean

        ''' <summary>Gets the number of entries in storage.</summary>
        Public ReadOnly Property Count As Integer
            Get
                Return _store.Count
            End Get
        End Property

        ''' <summary>Gets whether the storage has been modified since the last snapshot.</summary>
        Public ReadOnly Property IsDirty As Boolean
            Get
                Return _isDirty
            End Get
        End Property

        ''' <summary>
        ''' Initializes a new empty ContractStorage instance.
        ''' </summary>
        Public Sub New()
            _store = New Dictionary(Of String, Byte())(StringComparer.Ordinal)
            _isDirty = False
        End Sub

        ''' <summary>
        ''' Gets a value from storage by key.
        ''' </summary>
        ''' <param name="key">The storage key (byte array).</param>
        ''' <returns>The stored value, or Nothing if the key does not exist.</returns>
        Public Function [Get](key As Byte()) As Byte()
            Dim keyHex As String = BytesToHex(key)
            Dim value As Byte() = Nothing
            If _store.TryGetValue(keyHex, value) Then
                Return value
            End If
            Return Nothing
        End Function

        ''' <summary>
        ''' Gets a value from storage by string key.
        ''' </summary>
        ''' <param name="key">The storage key as a string.</param>
        ''' <returns>The stored value, or Nothing if the key does not exist.</returns>
        Public Function GetByString(key As String) As Byte()
            Dim keyBytes As Byte() = System.Text.Encoding.UTF8.GetBytes(key)
            Return [Get](keyBytes)
        End Function

        ''' <summary>
        ''' Stores a value in storage by key.
        ''' </summary>
        ''' <param name="key">The storage key (byte array).</param>
        ''' <param name="value">The value to store.</param>
        Public Sub Put(key As Byte(), value As Byte())
            Dim keyHex As String = BytesToHex(key)
            _store(keyHex) = value
            _isDirty = True
        End Sub

        ''' <summary>
        ''' Stores a value in storage by string key.
        ''' </summary>
        ''' <param name="key">The storage key as a string.</param>
        ''' <param name="value">The value to store.</param>
        Public Sub PutByString(key As String, value As Byte())
            Dim keyBytes As Byte() = System.Text.Encoding.UTF8.GetBytes(key)
            Put(keyBytes, value)
        End Sub

        ''' <summary>
        ''' Stores a BigInteger value in storage.
        ''' </summary>
        ''' <param name="key">The storage key (byte array).</param>
        ''' <param name="value">The BigInteger value to store.</param>
        Public Sub PutInteger(key As Byte(), value As BigInteger)
            Put(key, value.ToByteArray())
        End Sub

        ''' <summary>
        ''' Gets a BigInteger value from storage.
        ''' </summary>
        ''' <param name="key">The storage key (byte array).</param>
        ''' <returns>The stored BigInteger value, or Zero if not found.</returns>
        Public Function GetInteger(key As Byte()) As BigInteger
            Dim data As Byte() = [Get](key)
            If data Is Nothing OrElse data.Length = 0 Then Return BigInteger.Zero
            Return New BigInteger(data)
        End Function

        ''' <summary>
        ''' Removes a key from storage.
        ''' </summary>
        ''' <param name="key">The storage key to remove.</param>
        ''' <returns>True if the key was found and removed; otherwise, False.</returns>
        Public Function Remove(key As Byte()) As Boolean
            Dim keyHex As String = BytesToHex(key)
            Dim removed As Boolean = _store.Remove(keyHex)
            If removed Then _isDirty = True
            Return removed
        End Function

        ''' <summary>
        ''' Checks if a key exists in storage.
        ''' </summary>
        ''' <param name="key">The storage key to check.</param>
        ''' <returns>True if the key exists; otherwise, False.</returns>
        Public Function ContainsKey(key As Byte()) As Boolean
            Dim keyHex As String = BytesToHex(key)
            Return _store.ContainsKey(keyHex)
        End Function

        ''' <summary>
        ''' Creates a snapshot of the current storage state for potential rollback.
        ''' </summary>
        Public Sub CreateSnapshot()
            _snapshot = New Dictionary(Of String, Byte())(_store, StringComparer.Ordinal)
        End Sub

        ''' <summary>
        ''' Rolls back storage to the last snapshot state.
        ''' </summary>
        Public Sub Rollback()
            If _snapshot IsNot Nothing Then
                _store.Clear()
                For Each kvp As KeyValuePair(Of String, Byte()) In _snapshot
                    _store(kvp.Key) = kvp.Value
                Next
                _isDirty = False
            End If
        End Sub

        ''' <summary>
        ''' Commits the current state and discards the snapshot.
        ''' </summary>
        Public Sub Commit()
            _snapshot = Nothing
            _isDirty = False
        End Sub

        ''' <summary>
        ''' Clears all entries from storage.
        ''' </summary>
        Public Sub Clear()
            _store.Clear()
            _isDirty = True
        End Sub

        ''' <summary>
        ''' Gets all storage keys as hex strings.
        ''' </summary>
        ''' <returns>A list of all storage keys.</returns>
        Public Function GetAllKeys() As List(Of String)
            Return New List(Of String)(_store.Keys)
        End Function

        ''' <summary>
        ''' Computes a hash of the entire storage state (for state root calculation).
        ''' </summary>
        ''' <returns>A 32-byte hash of the storage state.</returns>
        Public Function ComputeStateHash() As Byte()
            If _store.Count = 0 Then
                Return New Byte(31) {}
            End If

            ' Sort keys and hash all key-value pairs
            Dim sortedKeys As New List(Of String)(_store.Keys)
            sortedKeys.Sort(StringComparer.Ordinal)

            Dim combined As New List(Of Byte)()
            For Each key As String In sortedKeys
                combined.AddRange(System.Text.Encoding.UTF8.GetBytes(key))
                combined.AddRange(_store(key))
            Next

            Return HashUtil.Sha256(combined.ToArray())
        End Function

        ''' <summary>
        ''' Converts a byte array to a hex string for use as dictionary key.
        ''' </summary>
        Private Function BytesToHex(data As Byte()) As String
            If data Is Nothing Then Return String.Empty
            Return BitConverter.ToString(data).Replace("-", "").ToLowerInvariant()
        End Function

    End Class

End Namespace
