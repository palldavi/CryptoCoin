Imports System
Imports System.Security.Cryptography

Namespace CryptoCoin.Cryptography

    ''' <summary>
    ''' HMAC-SHA512 implementation used for HD key derivation (BIP32).
    ''' </summary>
    Public NotInheritable Class HmacSha512

        Private Sub New()
        End Sub

        ''' <summary>
        ''' Computes HMAC-SHA512 with the given key and data.
        ''' </summary>
        Public Shared Function Compute(key As Byte(), data As Byte()) As Byte()
            If key Is Nothing Then Throw New ArgumentNullException(NameOf(key))
            If data Is Nothing Then Throw New ArgumentNullException(NameOf(data))

            Using hmac As New System.Security.Cryptography.HMACSHA512(key)
                Return hmac.ComputeHash(data)
            End Using
        End Function

        ''' <summary>
        ''' Computes HMAC-SHA512 with a string key (UTF-8 encoded).
        ''' </summary>
        Public Shared Function Compute(key As String, data As Byte()) As Byte()
            If key Is Nothing Then Throw New ArgumentNullException(NameOf(key))
            Return Compute(System.Text.Encoding.UTF8.GetBytes(key), data)
        End Function

        ''' <summary>
        ''' Splits the HMAC-SHA512 result into left (32 bytes) and right (32 bytes) halves.
        ''' Used in BIP32 key derivation where left = key material, right = chain code.
        ''' </summary>
        Public Shared Function ComputeAndSplit(key As Byte(), data As Byte()) As Tuple(Of Byte(), Byte())
            Dim hash As Byte() = Compute(key, data)
            Dim left(31) As Byte
            Dim right(31) As Byte
            Array.Copy(hash, 0, left, 0, 32)
            Array.Copy(hash, 32, right, 0, 32)
            Return Tuple.Create(left, right)
        End Function

    End Class

End Namespace
