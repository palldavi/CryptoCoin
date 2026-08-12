Imports System.Security.Cryptography
Imports System.Text

Namespace CryptoCoin.Cryptography

    ''' <summary>
    ''' Provides hashing utilities for the CryptoCoin blockchain.
    ''' Supports SHA-256, double SHA-256, SHA-512, and RIPEMD-160.
    ''' </summary>
    Public NotInheritable Class HashUtil

        Private Sub New()
            ' Static utility class - prevent instantiation
        End Sub

        ''' <summary>
        ''' Computes SHA-256 hash of the input bytes.
        ''' </summary>
        Public Shared Function Sha256(data As Byte()) As Byte()
            If data Is Nothing Then Throw New ArgumentNullException(NameOf(data))
            Using hasher As System.Security.Cryptography.SHA256 = System.Security.Cryptography.SHA256.Create()
                Return hasher.ComputeHash(data)
            End Using
        End Function

        ''' <summary>
        ''' Computes SHA-256 hash of a string (UTF-8 encoded).
        ''' </summary>
        Public Shared Function Sha256(text As String) As Byte()
            If text Is Nothing Then Throw New ArgumentNullException(NameOf(text))
            Return Sha256(Encoding.UTF8.GetBytes(text))
        End Function

        ''' <summary>
        ''' Computes double SHA-256 (SHA-256 of SHA-256) as used in Bitcoin-style protocols.
        ''' </summary>
        Public Shared Function DoubleSha256(data As Byte()) As Byte()
            Return Sha256(Sha256(data))
        End Function

        ''' <summary>
        ''' Computes SHA-512 hash of the input bytes.
        ''' </summary>
        Public Shared Function Sha512(data As Byte()) As Byte()
            If data Is Nothing Then Throw New ArgumentNullException(NameOf(data))
            Using hasher As System.Security.Cryptography.SHA512 = System.Security.Cryptography.SHA512.Create()
                Return hasher.ComputeHash(data)
            End Using
        End Function

        ''' <summary>
        ''' Computes RIPEMD-160 hash using the managed implementation.
        ''' </summary>
        Public Shared Function Ripemd160(data As Byte()) As Byte()
            If data Is Nothing Then Throw New ArgumentNullException(NameOf(data))
            Dim hasher As New Ripemd160Hasher()
            Return hasher.ComputeHash(data)
        End Function

        ''' <summary>
        ''' Computes Hash160 (SHA-256 followed by RIPEMD-160) as used for address generation.
        ''' </summary>
        Public Shared Function Hash160(data As Byte()) As Byte()
            Return Ripemd160(Sha256(data))
        End Function

        ''' <summary>
        ''' Converts a byte array to a hexadecimal string.
        ''' </summary>
        Public Shared Function ToHex(data As Byte()) As String
            If data Is Nothing Then Throw New ArgumentNullException(NameOf(data))
            Dim sb As New StringBuilder(data.Length * 2)
            For Each b As Byte In data
                sb.Append(b.ToString("x2"))
            Next
            Return sb.ToString()
        End Function

        ''' <summary>
        ''' Converts a hexadecimal string to a byte array.
        ''' </summary>
        Public Shared Function FromHex(hex As String) As Byte()
            If hex Is Nothing Then Throw New ArgumentNullException(NameOf(hex))
            If hex.Length Mod 2 <> 0 Then
                Throw New ArgumentException("Hex string must have even length.", NameOf(hex))
            End If

            Dim bytes(hex.Length \ 2 - 1) As Byte
            For i As Integer = 0 To bytes.Length - 1
                bytes(i) = Convert.ToByte(hex.Substring(i * 2, 2), 16)
            Next
            Return bytes
        End Function

        ''' <summary>
        ''' Computes a checksum (first 4 bytes of double SHA-256).
        ''' </summary>
        Public Shared Function Checksum(data As Byte()) As Byte()
            Dim hash As Byte() = DoubleSha256(data)
            Dim result(3) As Byte
            Array.Copy(hash, 0, result, 0, 4)
            Return result
        End Function

        ''' <summary>
        ''' Verifies that two byte arrays are equal in constant time to prevent timing attacks.
        ''' </summary>
        Public Shared Function ConstantTimeEquals(a As Byte(), b As Byte()) As Boolean
            If a Is Nothing OrElse b Is Nothing Then Return False
            If a.Length <> b.Length Then Return False

            Dim result As Integer = 0
            For i As Integer = 0 To a.Length - 1
                result = result Or (CInt(a(i)) Xor CInt(b(i)))
            Next
            Return result = 0
        End Function

    End Class

End Namespace
