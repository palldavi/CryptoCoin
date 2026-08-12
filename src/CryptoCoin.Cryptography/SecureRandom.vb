Imports System.Security.Cryptography

Namespace CryptoCoin.Cryptography

    ''' <summary>
    ''' Cryptographically secure random number generator.
    ''' Used for key generation, nonces, and other security-critical randomness.
    ''' </summary>
    Public NotInheritable Class SecureRandom

        Private Shared ReadOnly Rng As RNGCryptoServiceProvider = New RNGCryptoServiceProvider()

        Private Sub New()
        End Sub

        ''' <summary>
        ''' Generates cryptographically secure random bytes.
        ''' </summary>
        Public Shared Function GetBytes(count As Integer) As Byte()
            If count <= 0 Then Throw New ArgumentOutOfRangeException(NameOf(count))
            Dim buffer(count - 1) As Byte
            Rng.GetBytes(buffer)
            Return buffer
        End Function

        ''' <summary>
        ''' Fills the provided buffer with cryptographically secure random bytes.
        ''' </summary>
        Public Shared Sub FillBytes(buffer As Byte())
            If buffer Is Nothing Then Throw New ArgumentNullException(NameOf(buffer))
            Rng.GetBytes(buffer)
        End Sub

        ''' <summary>
        ''' Generates a random 32-byte (256-bit) value suitable for use as a private key.
        ''' </summary>
        Public Shared Function Generate256Bits() As Byte()
            Return GetBytes(32)
        End Function

        ''' <summary>
        ''' Generates a random 64-byte (512-bit) value.
        ''' </summary>
        Public Shared Function Generate512Bits() As Byte()
            Return GetBytes(64)
        End Function

        ''' <summary>
        ''' Generates a random integer in the range [0, maxExclusive).
        ''' </summary>
        Public Shared Function NextInteger(maxExclusive As Integer) As Integer
            If maxExclusive <= 0 Then Throw New ArgumentOutOfRangeException(NameOf(maxExclusive))

            ' Use rejection sampling to avoid modulo bias
            Dim maxValid As Integer = Integer.MaxValue - (Integer.MaxValue Mod maxExclusive)
            Dim buffer(3) As Byte
            Dim result As Integer

            Do
                Rng.GetBytes(buffer)
                result = BitConverter.ToInt32(buffer, 0) And Integer.MaxValue
            Loop While result >= maxValid

            Return result Mod maxExclusive
        End Function

        ''' <summary>
        ''' Generates a random UInt64 value.
        ''' </summary>
        Public Shared Function NextUInt64() As ULong
            Dim buffer(7) As Byte
            Rng.GetBytes(buffer)
            Return BitConverter.ToUInt64(buffer, 0)
        End Function

    End Class

End Namespace
