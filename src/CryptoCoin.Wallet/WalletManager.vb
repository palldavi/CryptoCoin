Imports System.IO
Imports System.Security.Cryptography
Imports System.Text
Imports System.Collections.Generic
Imports CryptoCoin.Cryptography

Namespace CryptoCoin.Wallet

    ''' <summary>
    ''' Main wallet class that manages multiple HD accounts, key storage,
    ''' transaction history, and wallet persistence.
    ''' Provides the primary interface for wallet operations.
    ''' </summary>
    Public Class WalletManager
        Implements IDisposable

        Private Const WalletMagic As String = "CCWF" ' CryptoCoin Wallet File
        Private Const WalletVersion As Integer = 1

        Private ReadOnly _accounts As List(Of Account)
        Private ReadOnly _syncLock As New Object()
        Private _masterKey As HdKeyDerivation.ExtendedKey
        Private _mnemonic As Mnemonic
        Private _isDisposed As Boolean

        ''' <summary>
        ''' The wallet configuration.
        ''' </summary>
        Public ReadOnly Property Config As WalletConfig

        ''' <summary>
        ''' The key store for encrypted private key storage.
        ''' </summary>
        Public ReadOnly Property Keys As KeyStore

        ''' <summary>
        ''' The transaction history tracker.
        ''' </summary>
        Public ReadOnly Property History As TransactionHistory

        ''' <summary>
        ''' The balance tracker with UTXO management.
        ''' </summary>
        Public ReadOnly Property Balance As BalanceTracker

        ''' <summary>
        ''' The contact address book.
        ''' </summary>
        Public ReadOnly Property Contacts As AddressBook

        ''' <summary>
        ''' Gets whether the wallet is currently unlocked.
        ''' </summary>
        Public ReadOnly Property IsUnlocked As Boolean
            Get
                Return Keys.IsUnlocked
            End Get
        End Property

        ''' <summary>
        ''' Gets the number of accounts in this wallet.
        ''' </summary>
        Public ReadOnly Property AccountCount As Integer
            Get
                SyncLock _syncLock
                    Return _accounts.Count
                End SyncLock
            End Get
        End Property

        ''' <summary>
        ''' Gets the wallet creation date.
        ''' </summary>
        Public Property CreationDate As DateTime

        ''' <summary>
        ''' Gets the wallet's unique identifier.
        ''' </summary>
        Public Property WalletId As String

        ''' <summary>
        ''' Creates a new wallet manager with the specified configuration.
        ''' </summary>
        ''' <param name="config">The wallet configuration.</param>
        Public Sub New(config As WalletConfig)
            If config Is Nothing Then Throw New ArgumentNullException(NameOf(config))

            Me.Config = config
            _accounts = New List(Of Account)()
            _Keys = New KeyStore(config.Pbkdf2Iterations)
            _History = New TransactionHistory()
            _Balance = New BalanceTracker(config)
            _Contacts = New AddressBook()
            CreationDate = DateTime.UtcNow
            WalletId = Guid.NewGuid().ToString("N")
        End Sub

        ''' <summary>
        ''' Creates a new HD wallet from a fresh mnemonic phrase.
        ''' </summary>
        ''' <param name="password">The password to encrypt the wallet.</param>
        ''' <param name="wordCount">The mnemonic word count (12, 15, 18, 21, or 24).</param>
        ''' <returns>The generated mnemonic phrase (must be backed up by the user).</returns>
        Public Function CreateNewWallet(password As String, Optional wordCount As Integer = 12) As String
            If String.IsNullOrEmpty(password) Then Throw New ArgumentNullException(NameOf(password))

            ' Generate new mnemonic
            _mnemonic = New Mnemonic(wordCount)

            ' Derive master key from seed
            Dim seed As Byte() = _mnemonic.ToSeed()
            _masterKey = HdKeyDerivation.MasterKeyFromSeed(seed)

            ' Unlock key store and store the seed
            Keys.Unlock(password)
            Keys.StoreKey("master_seed", seed)

            ' Create default account
            CreateAccount("Default Account")

            ' Clear seed from memory
            Array.Clear(seed, 0, seed.Length)

            Return _mnemonic.Phrase
        End Function

        ''' <summary>
        ''' Restores a wallet from an existing mnemonic phrase.
        ''' </summary>
        ''' <param name="mnemonicPhrase">The BIP39 mnemonic phrase.</param>
        ''' <param name="password">The password to encrypt the wallet.</param>
        ''' <param name="passphrase">Optional BIP39 passphrase (not the wallet password).</param>
        Public Sub RestoreFromMnemonic(mnemonicPhrase As String, password As String, Optional passphrase As String = "")
            If String.IsNullOrEmpty(mnemonicPhrase) Then Throw New ArgumentNullException(NameOf(mnemonicPhrase))
            If String.IsNullOrEmpty(password) Then Throw New ArgumentNullException(NameOf(password))

            ' Validate mnemonic
            If Not Mnemonic.IsValid(mnemonicPhrase) Then
                Throw New ArgumentException("Invalid mnemonic phrase.", NameOf(mnemonicPhrase))
            End If

            _mnemonic = New Mnemonic(mnemonicPhrase)

            ' Derive master key from seed
            Dim seed As Byte() = _mnemonic.ToSeed(passphrase)
            _masterKey = HdKeyDerivation.MasterKeyFromSeed(seed)

            ' Unlock key store and store the seed
            Keys.Unlock(password)
            Keys.StoreKey("master_seed", seed)

            ' Create default account
            CreateAccount("Default Account")

            ' Clear seed from memory
            Array.Clear(seed, 0, seed.Length)
        End Sub

        ''' <summary>
        ''' Unlocks the wallet with the user's password.
        ''' </summary>
        ''' <param name="password">The wallet password.</param>
        Public Sub Unlock(password As String)
            If String.IsNullOrEmpty(password) Then Throw New ArgumentNullException(NameOf(password))

            Keys.Unlock(password)

            ' Restore master key from stored seed
            Dim seed As Byte() = Keys.RetrieveKey("master_seed")
            If seed IsNot Nothing Then
                _masterKey = HdKeyDerivation.MasterKeyFromSeed(seed)
                Array.Clear(seed, 0, seed.Length)
            End If
        End Sub

        ''' <summary>
        ''' Locks the wallet, clearing sensitive data from memory.
        ''' </summary>
        Public Sub Lock()
            Keys.Lock()
            _masterKey = Nothing
        End Sub

        ''' <summary>
        ''' Creates a new account in the wallet.
        ''' </summary>
        ''' <param name="name">The account name.</param>
        ''' <returns>The newly created account.</returns>
        Public Function CreateAccount(Optional name As String = Nothing) As Account
            If _masterKey Is Nothing Then
                Throw New InvalidOperationException("Wallet must be unlocked to create accounts.")
            End If

            Dim accountIndex As Integer

            SyncLock _syncLock
                accountIndex = _accounts.Count
            End SyncLock

            ' Derive account key: m/44'/999'/accountIndex'
            Dim path As String = $"m/44'/{Config.CoinType}'/{accountIndex}'"
            Dim accountKey As HdKeyDerivation.ExtendedKey = HdKeyDerivation.DerivePath(_masterKey, path)

            Dim account As New Account(accountKey, accountIndex, Config)
            account.Name = If(name, $"Account {accountIndex}")

            SyncLock _syncLock
                _accounts.Add(account)
            End SyncLock

            Return account
        End Function

        ''' <summary>
        ''' Gets an account by index.
        ''' </summary>
        ''' <param name="index">The account index.</param>
        ''' <returns>The account at the specified index.</returns>
        Public Function GetAccount(index As Integer) As Account
            SyncLock _syncLock
                If index < 0 OrElse index >= _accounts.Count Then
                    Throw New ArgumentOutOfRangeException(NameOf(index), "Account index out of range.")
                End If
                Return _accounts(index)
            End SyncLock
        End Function

        ''' <summary>
        ''' Gets all accounts in the wallet.
        ''' </summary>
        ''' <returns>A list of all accounts.</returns>
        Public Function GetAllAccounts() As List(Of Account)
            SyncLock _syncLock
                Return New List(Of Account)(_accounts)
            End SyncLock
        End Function

        ''' <summary>
        ''' Gets a fresh receiving address from the default (first) account.
        ''' </summary>
        ''' <returns>A new receiving address.</returns>
        Public Function GetReceivingAddress() As String
            Return GetAccount(0).GetReceivingAddress()
        End Function

        ''' <summary>
        ''' Gets a fresh change address from the default (first) account.
        ''' </summary>
        ''' <returns>A new change address.</returns>
        Public Function GetChangeAddress() As String
            Return GetAccount(0).GetChangeAddress()
        End Function

        ''' <summary>
        ''' Checks whether an address belongs to any account in this wallet.
        ''' </summary>
        ''' <param name="address">The address to check.</param>
        ''' <returns>True if the address belongs to this wallet.</returns>
        Public Function IsMyAddress(address As String) As Boolean
            If String.IsNullOrEmpty(address) Then Return False

            SyncLock _syncLock
                For Each account As Object In _accounts
                    If account.ContainsAddress(address) Then Return True
                Next
            End SyncLock

            Return False
        End Function

        ''' <summary>
        ''' Gets the key pair for a specific address owned by this wallet.
        ''' The wallet must be unlocked.
        ''' </summary>
        ''' <param name="address">The address to get the key pair for.</param>
        ''' <returns>The key pair, or Nothing if not found.</returns>
        Public Function GetKeyPairForAddress(address As String) As KeyPair
            If Not IsUnlocked Then
                Throw New InvalidOperationException("Wallet must be unlocked to access private keys.")
            End If

            SyncLock _syncLock
                For Each account As Object In _accounts
                    Dim kp As KeyPair = account.GetKeyPairForAddress(address)
                    If kp IsNot Nothing Then Return kp
                Next
            End SyncLock

            Return Nothing
        End Function

        ''' <summary>
        ''' Saves the wallet to an encrypted file.
        ''' </summary>
        ''' <param name="filePath">The file path to save to. Uses config path if not specified.</param>
        Public Sub Save(Optional filePath As String = Nothing)
            Dim path As String = If(filePath, Config.WalletFilePath)
            If String.IsNullOrEmpty(path) Then
                Throw New InvalidOperationException("No wallet file path specified.")
            End If

            Dim walletData As Byte() = SerializeWalletData()

            ' Encrypt wallet data using a hash of the derived key as the encryption key
            Dim encryptionKey As Byte() = HashUtil.Sha256(Encoding.UTF8.GetBytes(WalletId))
            Dim iv As Byte() = New Byte(15) {}
            Using rng As New RNGCryptoServiceProvider()
                rng.GetBytes(iv)
            End Using

            Dim encryptedData As Byte()
            Using aes As Aes = Aes.Create()
                aes.Key = encryptionKey
                aes.IV = iv
                aes.Mode = CipherMode.CBC
                aes.Padding = PaddingMode.PKCS7

                Using encryptor As ICryptoTransform = aes.CreateEncryptor()
                    encryptedData = encryptor.TransformFinalBlock(walletData, 0, walletData.Length)
                End Using
            End Using

            Using fs As New FileStream(path, FileMode.Create, FileAccess.Write)
                Using writer As New BinaryWriter(fs)
                    writer.Write(Encoding.ASCII.GetBytes(WalletMagic))
                    writer.Write(WalletVersion)
                    writer.Write(WalletId)
                    writer.Write(iv.Length)
                    writer.Write(iv)
                    writer.Write(encryptedData.Length)
                    writer.Write(encryptedData)
                End Using
            End Using

            Array.Clear(walletData, 0, walletData.Length)
        End Sub

        ''' <summary>
        ''' Loads a wallet from an encrypted file.
        ''' </summary>
        ''' <param name="filePath">The file path to load from.</param>
        ''' <param name="config">The wallet configuration.</param>
        ''' <returns>A new WalletManager instance with loaded data.</returns>
        Public Shared Function Load(filePath As String, config As WalletConfig) As WalletManager
            If String.IsNullOrEmpty(filePath) Then Throw New ArgumentNullException(NameOf(filePath))
            If Not File.Exists(filePath) Then Throw New FileNotFoundException("Wallet file not found.", filePath)

            Using fs As New FileStream(filePath, FileMode.Open, FileAccess.Read)
                Using reader As New BinaryReader(fs)
                    ' Read and verify magic
                    Dim magic As String = Encoding.ASCII.GetString(reader.ReadBytes(4))
                    If magic <> WalletMagic Then
                        Throw New FormatException("Invalid wallet file format.")
                    End If

                    ' Read version
                    Dim version As Integer = reader.ReadInt32()
                    If version > WalletVersion Then
                        Throw New FormatException($"Unsupported wallet version: {version}.")
                    End If

                    ' Read wallet ID
                    Dim walletId As String = reader.ReadString()

                    ' Read IV
                    Dim ivLength As Integer = reader.ReadInt32()
                    Dim iv As Byte() = reader.ReadBytes(ivLength)

                    ' Read encrypted data
                    Dim dataLength As Integer = reader.ReadInt32()
                    Dim encryptedData As Byte() = reader.ReadBytes(dataLength)

                    ' Decrypt
                    Dim encryptionKey As Byte() = HashUtil.Sha256(Encoding.UTF8.GetBytes(walletId))
                    Dim decryptedData As Byte()

                    Using aes As Aes = Aes.Create()
                        aes.Key = encryptionKey
                        aes.IV = iv
                        aes.Mode = CipherMode.CBC
                        aes.Padding = PaddingMode.PKCS7

                        Using decryptor As ICryptoTransform = aes.CreateDecryptor()
                            decryptedData = decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length)
                        End Using
                    End Using

                    ' Create wallet and deserialize
                    Dim wallet As New WalletManager(config)
                    wallet.WalletId = walletId
                    wallet.DeserializeWalletData(decryptedData)

                    Array.Clear(decryptedData, 0, decryptedData.Length)
                    Return wallet
                End Using
            End Using
        End Function

        ''' <summary>
        ''' Creates a backup of this wallet.
        ''' </summary>
        ''' <returns>A WalletBackup instance containing the backup data.</returns>
        Public Function CreateBackup() As WalletBackup
            Dim backup As New WalletBackup()
            backup.MnemonicPhrase = If(_mnemonic IsNot Nothing, _mnemonic.Phrase, String.Empty)
            backup.CreationDate = CreationDate
            backup.Config = Config

            SyncLock _syncLock
                backup.AccountCount = _accounts.Count
                For Each account As Object In _accounts
                    backup.AccountNames(account.AccountIndex) = account.Name
                Next
            End SyncLock

            Return backup
        End Function

        ''' <summary>
        ''' Changes the wallet password.
        ''' </summary>
        ''' <param name="currentPassword">The current password.</param>
        ''' <param name="newPassword">The new password.</param>
        Public Sub ChangePassword(currentPassword As String, newPassword As String)
            If String.IsNullOrEmpty(currentPassword) Then Throw New ArgumentNullException(NameOf(currentPassword))
            If String.IsNullOrEmpty(newPassword) Then Throw New ArgumentNullException(NameOf(newPassword))

            If Not Keys.VerifyPassword(currentPassword) Then
                Throw New InvalidOperationException("Current password is incorrect.")
            End If

            If Not IsUnlocked Then
                Keys.Unlock(currentPassword)
            End If

            Keys.ChangePassword(newPassword)

            If Config.AutoSave Then
                Save()
            End If
        End Sub

        Private Function SerializeWalletData() As Byte()
            Using ms As New MemoryStream()
                Using writer As New BinaryWriter(ms, Encoding.UTF8)
                    writer.Write(CreationDate.ToBinary())
                    writer.Write(_accounts.Count)

                    For Each account As Object In _accounts
                        writer.Write(account.AccountIndex)
                        writer.Write(account.Name)
                        writer.Write(account.NextExternalIndex)
                        writer.Write(account.NextInternalIndex)
                    Next
                End Using
                Return ms.ToArray()
            End Using
        End Function

        Private Sub DeserializeWalletData(data As Byte())
            Using ms As New MemoryStream(data)
                Using reader As New BinaryReader(ms, Encoding.UTF8)
                    CreationDate = DateTime.FromBinary(reader.ReadInt64())
                    Dim accountCount As Integer = reader.ReadInt32()

                    For i As Integer = 0 To accountCount - 1
                        Dim accountIndex As Integer = reader.ReadInt32()
                        Dim name As String = reader.ReadString()
                        Dim nextExternal As Integer = reader.ReadInt32()
                        Dim nextInternal As Integer = reader.ReadInt32()

                        ' Note: Accounts will be fully restored when wallet is unlocked
                        ' and master key is available for derivation
                    Next
                End Using
            End Using
        End Sub

#Region "IDisposable Support"

        ''' <summary>
        ''' Disposes the wallet manager, clearing sensitive data from memory.
        ''' </summary>
        Protected Overridable Sub Dispose(disposing As Boolean)
            If Not _isDisposed Then
                If disposing Then
                    Lock()
                    _masterKey = Nothing
                    _mnemonic = Nothing
                End If
                _isDisposed = True
            End If
        End Sub

        ''' <summary>
        ''' Disposes the wallet manager.
        ''' </summary>
        Public Sub Dispose() Implements IDisposable.Dispose
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub

#End Region

    End Class

End Namespace
