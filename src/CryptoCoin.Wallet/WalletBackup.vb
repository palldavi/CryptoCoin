Imports System.IO
Imports System.Security.Cryptography
Imports System.Text
Imports CryptoCoin.Cryptography

Namespace CryptoCoin.Wallet

    ''' <summary>
    ''' Provides wallet backup and restore functionality.
    ''' Supports mnemonic phrase backup, encrypted file export, and import operations.
    ''' </summary>
    Public Class WalletBackup

        Private Const BackupMagic As String = "CCWB" ' CryptoCoin Wallet Backup
        Private Const BackupVersion As Integer = 1

        ''' <summary>
        ''' The mnemonic phrase for this wallet backup.
        ''' </summary>
        Public Property MnemonicPhrase As String

        ''' <summary>
        ''' The wallet creation timestamp.
        ''' </summary>
        Public Property CreationDate As DateTime

        ''' <summary>
        ''' The number of accounts in the wallet.
        ''' </summary>
        Public Property AccountCount As Integer

        ''' <summary>
        ''' Account names indexed by account number.
        ''' </summary>
        Public ReadOnly Property AccountNames As Dictionary(Of Integer, String)

        ''' <summary>
        ''' The wallet configuration at time of backup.
        ''' </summary>
        Public Property Config As WalletConfig

        ''' <summary>
        ''' Creates a new wallet backup instance.
        ''' </summary>
        Public Sub New()
            _AccountNames = New Dictionary(Of Integer, String)()
            CreationDate = DateTime.UtcNow
        End Sub

        ''' <summary>
        ''' Creates a wallet backup from a mnemonic phrase.
        ''' </summary>
        ''' <param name="mnemonicPhrase">The BIP39 mnemonic phrase.</param>
        ''' <param name="accountCount">The number of accounts to back up.</param>
        Public Sub New(mnemonicPhrase As String, accountCount As Integer)
            If String.IsNullOrEmpty(mnemonicPhrase) Then Throw New ArgumentNullException(NameOf(mnemonicPhrase))
            If accountCount <= 0 Then Throw New ArgumentOutOfRangeException(NameOf(accountCount))

            Me.MnemonicPhrase = mnemonicPhrase
            Me.AccountCount = accountCount
            Me.CreationDate = DateTime.UtcNow
            _AccountNames = New Dictionary(Of Integer, String)()
        End Sub

        ''' <summary>
        ''' Exports the wallet backup to an encrypted file.
        ''' Uses AES-256-CBC with PBKDF2-derived key from the backup password.
        ''' </summary>
        ''' <param name="filePath">The file path to write the backup to.</param>
        ''' <param name="password">The password to encrypt the backup with.</param>
        Public Sub ExportEncrypted(filePath As String, password As String)
            If String.IsNullOrEmpty(filePath) Then Throw New ArgumentNullException(NameOf(filePath))
            If String.IsNullOrEmpty(password) Then Throw New ArgumentNullException(NameOf(password))

            ' Serialize backup data
            Dim backupData As Byte() = SerializeBackupData()

            ' Generate salt and derive key
            Dim salt As Byte() = New Byte(31) {}
            Using rng As New RNGCryptoServiceProvider()
                rng.GetBytes(salt)
            End Using

            Dim key As Byte() = DeriveKey(password, salt)
            Dim iv As Byte() = New Byte(15) {}
            Using rng As New RNGCryptoServiceProvider()
                rng.GetBytes(iv)
            End Using

            ' Encrypt the backup data
            Dim encryptedData As Byte()
            Using aes As Aes = Aes.Create()
                aes.Key = key
                aes.IV = iv
                aes.Mode = CipherMode.CBC
                aes.Padding = PaddingMode.PKCS7

                Using encryptor As ICryptoTransform = aes.CreateEncryptor()
                    encryptedData = encryptor.TransformFinalBlock(backupData, 0, backupData.Length)
                End Using
            End Using

            ' Write backup file: magic + version + salt + iv + encrypted data
            Using fs As New FileStream(filePath, FileMode.Create, FileAccess.Write)
                Using writer As New BinaryWriter(fs)
                    writer.Write(Encoding.ASCII.GetBytes(BackupMagic))
                    writer.Write(BackupVersion)
                    writer.Write(salt.Length)
                    writer.Write(salt)
                    writer.Write(iv.Length)
                    writer.Write(iv)
                    writer.Write(encryptedData.Length)
                    writer.Write(encryptedData)
                End Using
            End Using

            ' Clear sensitive data from memory
            Array.Clear(key, 0, key.Length)
            Array.Clear(backupData, 0, backupData.Length)
        End Sub

        ''' <summary>
        ''' Imports a wallet backup from an encrypted file.
        ''' </summary>
        ''' <param name="filePath">The file path to read the backup from.</param>
        ''' <param name="password">The password to decrypt the backup.</param>
        ''' <returns>The restored WalletBackup instance.</returns>
        Public Shared Function ImportEncrypted(filePath As String, password As String) As WalletBackup
            If String.IsNullOrEmpty(filePath) Then Throw New ArgumentNullException(NameOf(filePath))
            If String.IsNullOrEmpty(password) Then Throw New ArgumentNullException(NameOf(password))
            If Not File.Exists(filePath) Then Throw New FileNotFoundException("Backup file not found.", filePath)

            Using fs As New FileStream(filePath, FileMode.Open, FileAccess.Read)
                Using reader As New BinaryReader(fs)
                    ' Read and verify magic
                    Dim magic As String = Encoding.ASCII.GetString(reader.ReadBytes(4))
                    If magic <> BackupMagic Then
                        Throw New FormatException("Invalid backup file format.")
                    End If

                    ' Read version
                    Dim version As Integer = reader.ReadInt32()
                    If version > BackupVersion Then
                        Throw New FormatException($"Unsupported backup version: {version}.")
                    End If

                    ' Read salt
                    Dim saltLength As Integer = reader.ReadInt32()
                    Dim salt As Byte() = reader.ReadBytes(saltLength)

                    ' Read IV
                    Dim ivLength As Integer = reader.ReadInt32()
                    Dim iv As Byte() = reader.ReadBytes(ivLength)

                    ' Read encrypted data
                    Dim dataLength As Integer = reader.ReadInt32()
                    Dim encryptedData As Byte() = reader.ReadBytes(dataLength)

                    ' Derive key and decrypt
                    Dim key As Byte() = DeriveKey(password, salt)
                    Dim decryptedData As Byte()

                    Try
                        Using aes As Aes = Aes.Create()
                            aes.Key = key
                            aes.IV = iv
                            aes.Mode = CipherMode.CBC
                            aes.Padding = PaddingMode.PKCS7

                            Using decryptor As ICryptoTransform = aes.CreateDecryptor()
                                decryptedData = decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length)
                            End Using
                        End Using
                    Catch ex As CryptographicException
                        Throw New InvalidOperationException("Invalid password or corrupted backup file.", ex)
                    Finally
                        Array.Clear(key, 0, key.Length)
                    End Try

                    ' Deserialize backup data
                    Dim backup As WalletBackup = DeserializeBackupData(decryptedData)
                    Array.Clear(decryptedData, 0, decryptedData.Length)
                    Return backup
                End Using
            End Using
        End Function

        ''' <summary>
        ''' Verifies that a mnemonic phrase matches the expected backup phrase.
        ''' Used for backup verification during wallet setup.
        ''' </summary>
        ''' <param name="inputPhrase">The phrase entered by the user for verification.</param>
        ''' <returns>True if the phrase matches the backup mnemonic.</returns>
        Public Function VerifyMnemonic(inputPhrase As String) As Boolean
            If String.IsNullOrEmpty(inputPhrase) Then Return False
            If String.IsNullOrEmpty(MnemonicPhrase) Then Return False

            Dim normalized As String = inputPhrase.Trim().ToLowerInvariant()
            Dim expected As String = MnemonicPhrase.Trim().ToLowerInvariant()

            Return String.Equals(normalized, expected, StringComparison.Ordinal)
        End Function

        ''' <summary>
        ''' Verifies specific words from the mnemonic (partial verification).
        ''' </summary>
        ''' <param name="wordIndices">The 0-based indices of words to verify.</param>
        ''' <param name="words">The words provided by the user.</param>
        ''' <returns>True if all specified words match.</returns>
        Public Function VerifyMnemonicWords(wordIndices As Integer(), words As String()) As Boolean
            If wordIndices Is Nothing OrElse words Is Nothing Then Return False
            If wordIndices.Length <> words.Length Then Return False
            If String.IsNullOrEmpty(MnemonicPhrase) Then Return False

            Dim mnemonicWords As String() = MnemonicPhrase.Split(" "c)

            For i As Integer = 0 To wordIndices.Length - 1
                Dim idx As Integer = wordIndices(i)
                If idx < 0 OrElse idx >= mnemonicWords.Length Then Return False
                If Not String.Equals(mnemonicWords(idx), words(i).Trim().ToLowerInvariant(), StringComparison.Ordinal) Then
                    Return False
                End If
            Next

            Return True
        End Function

        ''' <summary>
        ''' Validates that the mnemonic phrase is a valid BIP39 mnemonic.
        ''' </summary>
        ''' <returns>True if the mnemonic is valid.</returns>
        Public Function ValidateMnemonic() As Boolean
            If String.IsNullOrEmpty(MnemonicPhrase) Then Return False
            Return Mnemonic.IsValid(MnemonicPhrase)
        End Function

        Private Function SerializeBackupData() As Byte()
            Using ms As New MemoryStream()
                Using writer As New BinaryWriter(ms, Encoding.UTF8)
                    writer.Write(If(MnemonicPhrase, String.Empty))
                    writer.Write(CreationDate.ToBinary())
                    writer.Write(AccountCount)

                    writer.Write(AccountNames.Count)
                    For Each kvp As Object In AccountNames
                        writer.Write(kvp.Key)
                        writer.Write(kvp.Value)
                    Next
                End Using
                Return ms.ToArray()
            End Using
        End Function

        Private Shared Function DeserializeBackupData(data As Byte()) As WalletBackup
            Dim backup As New WalletBackup()

            Using ms As New MemoryStream(data)
                Using reader As New BinaryReader(ms, Encoding.UTF8)
                    backup.MnemonicPhrase = reader.ReadString()
                    backup.CreationDate = DateTime.FromBinary(reader.ReadInt64())
                    backup.AccountCount = reader.ReadInt32()

                    Dim nameCount As Integer = reader.ReadInt32()
                    For i As Integer = 0 To nameCount - 1
                        Dim index As Integer = reader.ReadInt32()
                        Dim name As String = reader.ReadString()
                        backup.AccountNames(index) = name
                    Next
                End Using
            End Using

            Return backup
        End Function

        Private Shared Function DeriveKey(password As String, salt As Byte()) As Byte()
            Dim passwordBytes As Byte() = Encoding.UTF8.GetBytes(password)
            Using pbkdf2 As New Rfc2898DeriveBytes(passwordBytes, salt, 100000)
                Return pbkdf2.GetBytes(32)
            End Using
        End Function

    End Class

End Namespace
