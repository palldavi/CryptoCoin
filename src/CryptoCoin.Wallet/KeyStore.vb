Imports System.IO
Imports System.Security.Cryptography
Imports System.Text
Imports CryptoCoin.Cryptography

Namespace CryptoCoin.Wallet

    ''' <summary>
    ''' Provides secure storage for private keys with AES-256 encryption.
    ''' Keys are encrypted using a password-derived key via PBKDF2.
    ''' </summary>
    Public Class KeyStore

        Private ReadOnly _encryptedKeys As Dictionary(Of String, EncryptedKeyEntry)
        Private ReadOnly _syncLock As New Object()
        Private _derivedKey As Byte()
        Private _isUnlocked As Boolean
        Private _salt As Byte()
        Private _iterations As Integer

        ''' <summary>
        ''' Gets whether the key store is currently unlocked (decryption key is in memory).
        ''' </summary>
        Public ReadOnly Property IsUnlocked As Boolean
            Get
                Return _isUnlocked
            End Get
        End Property

        ''' <summary>
        ''' Gets the number of keys stored.
        ''' </summary>
        Public ReadOnly Property KeyCount As Integer
            Get
                SyncLock _syncLock
                    Return _encryptedKeys.Count
                End SyncLock
            End Get
        End Property

        ''' <summary>
        ''' Gets the salt used for key derivation.
        ''' </summary>
        Public ReadOnly Property Salt As Byte()
            Get
                Return _salt
            End Get
        End Property

        ''' <summary>
        ''' Creates a new key store with a fresh salt.
        ''' </summary>
        ''' <param name="iterations">PBKDF2 iteration count for password-based key derivation.</param>
        Public Sub New(Optional iterations As Integer = 100000)
            If iterations < 10000 Then
                Throw New ArgumentOutOfRangeException(NameOf(iterations), "Iterations must be at least 10000.")
            End If

            _encryptedKeys = New Dictionary(Of String, EncryptedKeyEntry)(StringComparer.OrdinalIgnoreCase)
            _iterations = iterations
            _isUnlocked = False

            ' Generate a random 32-byte salt
            _salt = New Byte(31) {}
            Using rng As New RNGCryptoServiceProvider()
                rng.GetBytes(_salt)
            End Using
        End Sub

        ''' <summary>
        ''' Creates a key store with an existing salt (for loading from file).
        ''' </summary>
        ''' <param name="salt">The existing salt bytes.</param>
        ''' <param name="iterations">The PBKDF2 iteration count.</param>
        Public Sub New(salt As Byte(), iterations As Integer)
            If salt Is Nothing Then Throw New ArgumentNullException(NameOf(salt))
            If salt.Length < 16 Then Throw New ArgumentException("Salt must be at least 16 bytes.", NameOf(salt))
            If iterations < 10000 Then
                Throw New ArgumentOutOfRangeException(NameOf(iterations), "Iterations must be at least 10000.")
            End If

            _encryptedKeys = New Dictionary(Of String, EncryptedKeyEntry)(StringComparer.OrdinalIgnoreCase)
            _salt = CType(salt.Clone(), Byte())
            _iterations = iterations
            _isUnlocked = False
        End Sub

        ''' <summary>
        ''' Unlocks the key store by deriving the encryption key from the password.
        ''' </summary>
        ''' <param name="password">The user's password.</param>
        Public Sub Unlock(password As String)
            If String.IsNullOrEmpty(password) Then Throw New ArgumentNullException(NameOf(password))

            _derivedKey = DeriveKeyFromPassword(password)
            _isUnlocked = True
        End Sub

        ''' <summary>
        ''' Locks the key store by clearing the derived key from memory.
        ''' </summary>
        Public Sub Lock()
            If _derivedKey IsNot Nothing Then
                Array.Clear(_derivedKey, 0, _derivedKey.Length)
                _derivedKey = Nothing
            End If
            _isUnlocked = False
        End Sub

        ''' <summary>
        ''' Stores a private key in the key store (encrypted).
        ''' The key store must be unlocked.
        ''' </summary>
        ''' <param name="identifier">A unique identifier for the key (e.g., address or derivation path).</param>
        ''' <param name="privateKey">The private key bytes to store.</param>
        Public Sub StoreKey(identifier As String, privateKey As Byte())
            If String.IsNullOrEmpty(identifier) Then Throw New ArgumentNullException(NameOf(identifier))
            If privateKey Is Nothing Then Throw New ArgumentNullException(NameOf(privateKey))
            If Not _isUnlocked Then Throw New InvalidOperationException("Key store must be unlocked to store keys.")

            Dim encrypted As EncryptedKeyEntry = EncryptKey(privateKey)

            SyncLock _syncLock
                _encryptedKeys(identifier) = encrypted
            End SyncLock
        End Sub

        ''' <summary>
        ''' Stores a KeyPair in the key store.
        ''' </summary>
        ''' <param name="identifier">A unique identifier for the key.</param>
        ''' <param name="keyPair">The key pair to store.</param>
        Public Sub StoreKeyPair(identifier As String, keyPair As KeyPair)
            If keyPair Is Nothing Then Throw New ArgumentNullException(NameOf(keyPair))
            StoreKey(identifier, keyPair.PrivateKeyBytes)
        End Sub

        ''' <summary>
        ''' Retrieves and decrypts a private key from the store.
        ''' The key store must be unlocked.
        ''' </summary>
        ''' <param name="identifier">The key identifier.</param>
        ''' <returns>The decrypted private key bytes, or Nothing if not found.</returns>
        Public Function RetrieveKey(identifier As String) As Byte()
            If String.IsNullOrEmpty(identifier) Then Return Nothing
            If Not _isUnlocked Then Throw New InvalidOperationException("Key store must be unlocked to retrieve keys.")

            Dim entry As EncryptedKeyEntry = Nothing

            SyncLock _syncLock
                If Not _encryptedKeys.TryGetValue(identifier, entry) Then
                    Return Nothing
                End If
            End SyncLock

            Return DecryptKey(entry)
        End Function

        ''' <summary>
        ''' Retrieves a key pair from the store.
        ''' </summary>
        ''' <param name="identifier">The key identifier.</param>
        ''' <returns>The decrypted KeyPair, or Nothing if not found.</returns>
        Public Function RetrieveKeyPair(identifier As String) As KeyPair
            Dim keyBytes As Byte() = RetrieveKey(identifier)
            If keyBytes Is Nothing Then Return Nothing

            Try
                Return New KeyPair(keyBytes)
            Finally
                Array.Clear(keyBytes, 0, keyBytes.Length)
            End Try
        End Function

        ''' <summary>
        ''' Checks whether a key with the given identifier exists.
        ''' </summary>
        ''' <param name="identifier">The key identifier to check.</param>
        ''' <returns>True if the key exists in the store.</returns>
        Public Function ContainsKey(identifier As String) As Boolean
            If String.IsNullOrEmpty(identifier) Then Return False

            SyncLock _syncLock
                Return _encryptedKeys.ContainsKey(identifier)
            End SyncLock
        End Function

        ''' <summary>
        ''' Removes a key from the store.
        ''' </summary>
        ''' <param name="identifier">The key identifier to remove.</param>
        ''' <returns>True if the key was removed.</returns>
        Public Function RemoveKey(identifier As String) As Boolean
            If String.IsNullOrEmpty(identifier) Then Return False

            SyncLock _syncLock
                Return _encryptedKeys.Remove(identifier)
            End SyncLock
        End Function

        ''' <summary>
        ''' Changes the encryption password. Re-encrypts all stored keys with the new password.
        ''' The key store must be unlocked.
        ''' </summary>
        ''' <param name="newPassword">The new password.</param>
        Public Sub ChangePassword(newPassword As String)
            If String.IsNullOrEmpty(newPassword) Then Throw New ArgumentNullException(NameOf(newPassword))
            If Not _isUnlocked Then Throw New InvalidOperationException("Key store must be unlocked to change password.")

            ' Decrypt all keys with current password
            Dim decryptedKeys As New Dictionary(Of String, Byte())()

            SyncLock _syncLock
                For Each kvp As Object In _encryptedKeys
                    decryptedKeys(kvp.Key) = DecryptKey(kvp.Value)
                Next

                ' Generate new salt and derive new key
                Using rng As New RNGCryptoServiceProvider()
                    rng.GetBytes(_salt)
                End Using

                _derivedKey = DeriveKeyFromPassword(newPassword)

                ' Re-encrypt all keys with new password
                _encryptedKeys.Clear()
                For Each kvp As Object In decryptedKeys
                    _encryptedKeys(kvp.Key) = EncryptKey(kvp.Value)
                    Array.Clear(kvp.Value, 0, kvp.Value.Length)
                Next
            End SyncLock
        End Sub

        ''' <summary>
        ''' Verifies that the given password can unlock this key store.
        ''' </summary>
        ''' <param name="password">The password to verify.</param>
        ''' <returns>True if the password is correct.</returns>
        Public Function VerifyPassword(password As String) As Boolean
            If String.IsNullOrEmpty(password) Then Return False

            Try
                Dim testKey As Byte() = DeriveKeyFromPassword(password)

                ' Try to decrypt the first key as a verification
                SyncLock _syncLock
                    If _encryptedKeys.Count = 0 Then Return True ' No keys to verify against

                    Dim firstEntry As EncryptedKeyEntry = _encryptedKeys.Values.First()
                    Try
                        DecryptKeyWithDerivedKey(firstEntry, testKey)
                        Return True
                    Catch ex As CryptographicException
                        Return False
                    Finally
                        Array.Clear(testKey, 0, testKey.Length)
                    End Try
                End SyncLock
            Catch
                Return False
            End Try
        End Function

        ''' <summary>
        ''' Gets all key identifiers stored in this key store.
        ''' </summary>
        ''' <returns>List of key identifiers.</returns>
        Public Function GetAllIdentifiers() As List(Of String)
            SyncLock _syncLock
                Return New List(Of String)(_encryptedKeys.Keys)
            End SyncLock
        End Function

        Private Function DeriveKeyFromPassword(password As String) As Byte()
            Dim passwordBytes As Byte() = Encoding.UTF8.GetBytes(password)
            Using pbkdf2 As New Rfc2898DeriveBytes(passwordBytes, _salt, _iterations)
                Return pbkdf2.GetBytes(32) ' 256-bit key for AES-256
            End Using
        End Function

        Private Function EncryptKey(plainKey As Byte()) As EncryptedKeyEntry
            Dim entry As New EncryptedKeyEntry()
            entry.IV = New Byte(15) {}

            Using rng As New RNGCryptoServiceProvider()
                rng.GetBytes(entry.IV)
            End Using

            Using aes As Aes = Aes.Create()
                aes.Key = _derivedKey
                aes.IV = entry.IV
                aes.Mode = CipherMode.CBC
                aes.Padding = PaddingMode.PKCS7

                Using encryptor As ICryptoTransform = aes.CreateEncryptor()
                    entry.EncryptedData = encryptor.TransformFinalBlock(plainKey, 0, plainKey.Length)
                End Using
            End Using

            Return entry
        End Function

        Private Function DecryptKey(entry As EncryptedKeyEntry) As Byte()
            Return DecryptKeyWithDerivedKey(entry, _derivedKey)
        End Function

        Private Shared Function DecryptKeyWithDerivedKey(entry As EncryptedKeyEntry, key As Byte()) As Byte()
            Using aes As Aes = Aes.Create()
                aes.Key = key
                aes.IV = entry.IV
                aes.Mode = CipherMode.CBC
                aes.Padding = PaddingMode.PKCS7

                Using decryptor As ICryptoTransform = aes.CreateDecryptor()
                    Return decryptor.TransformFinalBlock(entry.EncryptedData, 0, entry.EncryptedData.Length)
                End Using
            End Using
        End Function

    End Class

    ''' <summary>
    ''' Represents an encrypted key entry in the key store.
    ''' </summary>
    Friend Class EncryptedKeyEntry

        ''' <summary>The AES initialization vector.</summary>
        Public Property IV As Byte()

        ''' <summary>The encrypted key data.</summary>
        Public Property EncryptedData As Byte()

    End Class

End Namespace
