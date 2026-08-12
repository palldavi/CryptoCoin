Imports System
Imports System.Numerics

Namespace CryptoCoin.Cryptography

    ''' <summary>
    ''' Extension methods for BigInteger to support elliptic curve operations.
    ''' </summary>
    Public Module BigIntegerExtensions

        ''' <summary>
        ''' Computes the modular inverse of a mod m using the extended Euclidean algorithm.
        ''' </summary>
        <System.Runtime.CompilerServices.Extension>
        Public Function ModInverse(a As BigInteger, m As BigInteger) As BigInteger
            If m = BigInteger.One Then Return BigInteger.Zero

            Dim m0 As BigInteger = m
            Dim x0 As BigInteger = BigInteger.Zero
            Dim x1 As BigInteger = BigInteger.One
            Dim aVal As BigInteger = a

            If aVal < BigInteger.Zero Then
                aVal = ((aVal Mod m) + m) Mod m
            End If

            While aVal > BigInteger.One
                Dim q As BigInteger = BigInteger.Divide(aVal, m)
                Dim t As BigInteger = m
                m = BigInteger.Remainder(aVal, m)
                aVal = t
                t = x0
                x0 = x1 - q * x0
                x1 = t
            End While

            If x1 < BigInteger.Zero Then
                x1 += m0
            End If

            Return x1
        End Function

        ''' <summary>
        ''' Performs modular exponentiation: (base ^ exponent) mod modulus.
        ''' </summary>
        <System.Runtime.CompilerServices.Extension>
        Public Function ModPow(base1 As BigInteger, exponent As BigInteger, modulus As BigInteger) As BigInteger
            Return BigInteger.ModPow(base1, exponent, modulus)
        End Function

        ''' <summary>
        ''' Converts a BigInteger to a fixed-size byte array (big-endian, unsigned).
        ''' </summary>
        <System.Runtime.CompilerServices.Extension>
        Public Function ToByteArrayFixed(value As BigInteger, size As Integer) As Byte()
            Dim bytes As Byte() = value.ToByteArray()
            ' BigInteger is little-endian, reverse to big-endian
            System.Array.Reverse(bytes)

            ' Remove leading sign byte if present
            Dim startIndex As Integer = 0
            While startIndex < bytes.Length - 1 AndAlso bytes(startIndex) = 0
                startIndex += 1
            End While

            Dim trimmed(bytes.Length - startIndex - 1) As Byte
            System.Array.Copy(bytes, startIndex, trimmed, 0, trimmed.Length)

            ' Pad or trim to desired size
            If trimmed.Length >= size Then
                Dim result(size - 1) As Byte
                System.Array.Copy(trimmed, trimmed.Length - size, result, 0, size)
                Return result
            Else
                Dim result(size - 1) As Byte
                System.Array.Copy(trimmed, 0, result, size - trimmed.Length, trimmed.Length)
                Return result
            End If
        End Function

        ''' <summary>
        ''' Creates a BigInteger from a big-endian unsigned byte array.
        ''' </summary>
        Public Function FromByteArrayUnsigned(data As Byte()) As BigInteger
            If data Is Nothing Then Throw New ArgumentNullException(NameOf(data))
            ' Append zero byte to ensure positive interpretation, then reverse for little-endian
            Dim temp(data.Length) As Byte
            System.Array.Copy(data, 0, temp, 1, data.Length)
            System.Array.Reverse(temp)
            Return New BigInteger(temp)
        End Function

        ''' <summary>
        ''' Performs positive modulo operation (always returns non-negative result).
        ''' </summary>
        <System.Runtime.CompilerServices.Extension>
        Public Function PositiveMod(value As BigInteger, modulus As BigInteger) As BigInteger
            Dim result As BigInteger = BigInteger.Remainder(value, modulus)
            If result < BigInteger.Zero Then
                result += modulus
            End If
            Return result
        End Function

    End Module

End Namespace
