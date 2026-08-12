Imports System.Security.Cryptography
Imports System.Text

Namespace CryptoCoin.Cryptography

    ''' <summary>
    ''' PBKDF2 key derivation used for converting mnemonic phrases to seeds.
    ''' Uses HMAC-SHA512 as the PRF per BIP39 specification.
    ''' </summary>
    Public NotInheritable Class Pbkdf2Deriver

        Private Sub New()
        End Sub

        ''' <summary>
        ''' Derives a key using PBKDF2-HMAC-SHA512.
        ''' </summary>
        ''' <param name="password">The password (mnemonic phrase).</param>
        ''' <param name="salt">The salt (typically "mnemonic" + passphrase).</param>
        ''' <param name="iterations">Number of iterations (2048 for BIP39).</param>
        ''' <param name="keyLength">Desired key length in bytes (64 for BIP39).</param>
        Public Shared Function Derive(password As String, salt As String, iterations As Integer, keyLength As Integer) As Byte()
            If password Is Nothing Then Throw New ArgumentNullException(NameOf(password))
            If salt Is Nothing Then Throw New ArgumentNullException(NameOf(salt))
            If iterations <= 0 Then Throw New ArgumentOutOfRangeException(NameOf(iterations))
            If keyLength <= 0 Then Throw New ArgumentOutOfRangeException(NameOf(keyLength))

            Dim passwordBytes As Byte() = Encoding.UTF8.GetBytes(password)
            Dim saltBytes As Byte() = Encoding.UTF8.GetBytes(salt)

            Return Derive(passwordBytes, saltBytes, iterations, keyLength)
        End Function

        ''' <summary>
        ''' Derives a key using PBKDF2-HMAC-SHA512 with byte array inputs.
        ''' </summary>
        Public Shared Function Derive(password As Byte(), salt As Byte(), iterations As Integer, keyLength As Integer) As Byte()
            If password Is Nothing Then Throw New ArgumentNullException(NameOf(password))
            If salt Is Nothing Then Throw New ArgumentNullException(NameOf(salt))

            Const HashLength As Integer = 64 ' SHA-512 output
            Dim blockCount As Integer = CInt(Math.Ceiling(keyLength / CDbl(HashLength)))
            Dim result(keyLength - 1) As Byte
            Dim offset As Integer = 0

            For blockIndex As Integer = 1 To blockCount
                Dim block As Byte() = ComputeBlock(password, salt, iterations, blockIndex)
                Dim copyLength As Integer = Math.Min(HashLength, keyLength - offset)
                Array.Copy(block, 0, result, offset, copyLength)
                offset += copyLength
            Next

            Return result
        End Function

        Private Shared Function ComputeBlock(password As Byte(), salt As Byte(), iterations As Integer, blockIndex As Integer) As Byte()
            ' U1 = PRF(password, salt || INT(blockIndex))
            Dim saltWithIndex(salt.Length + 3) As Byte
            Array.Copy(salt, saltWithIndex, salt.Length)
            saltWithIndex(salt.Length) = CByte((blockIndex >> 24) And &HFF)
            saltWithIndex(salt.Length + 1) = CByte((blockIndex >> 16) And &HFF)
            saltWithIndex(salt.Length + 2) = CByte((blockIndex >> 8) And &HFF)
            saltWithIndex(salt.Length + 3) = CByte(blockIndex And &HFF)

            Using hmac As New System.Security.Cryptography.HMACSHA512(password)
                Dim u As Byte() = hmac.ComputeHash(saltWithIndex)
                Dim result As Byte() = CType(u.Clone(), Byte())

                ' U2..Uc
                For i As Integer = 2 To iterations
                    u = hmac.ComputeHash(u)
                    For j As Integer = 0 To result.Length - 1
                        result(j) = result(j) Xor u(j)
                    Next
                Next

                Return result
            End Using
        End Function

        ''' <summary>
        ''' Derives a BIP39 seed from a mnemonic phrase and optional passphrase.
        ''' </summary>
        Public Shared Function DeriveBip39Seed(mnemonic As String, Optional passphrase As String = "") As Byte()
            Return Derive(mnemonic, "mnemonic" & passphrase, 2048, 64)
        End Function

    End Class

End Namespace
