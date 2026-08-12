Imports System
Imports System.Numerics

Namespace CryptoCoin.Cryptography

    ''' <summary>
    ''' ECDSA signature generation and verification on the secp256k1 curve.
    ''' Used for signing and verifying CryptoCoin transactions.
    ''' </summary>
    Public NotInheritable Class EcdsaSigner

        Private Sub New()
        End Sub

        ''' <summary>
        ''' Signs a message hash with the given private key.
        ''' Returns the signature as (r, s) components.
        ''' </summary>
        Public Shared Function Sign(messageHash As Byte(), privateKey As BigInteger) As EcdsaSignature
            If messageHash Is Nothing Then Throw New ArgumentNullException(NameOf(messageHash))
            If messageHash.Length <> 32 Then Throw New ArgumentException("Message hash must be 32 bytes.", NameOf(messageHash))
            If Not Secp256k1Curve.IsValidPrivateKey(privateKey) Then
                Throw New ArgumentException("Invalid private key.", NameOf(privateKey))
            End If

            Dim n As BigInteger = Secp256k1Curve.N
            Dim z As BigInteger = FromByteArrayUnsigned(messageHash)

            ' Truncate z if longer than n
            If z >= n Then
                z = z - n
            End If

            Dim r, s As BigInteger
            Dim k As BigInteger

            Do
                ' Generate random k
                Dim kBytes As Byte() = SecureRandom.Generate256Bits()
                k = FromByteArrayUnsigned(kBytes)

                If k <= BigInteger.Zero OrElse k >= n Then Continue Do

                ' R = k * G
                Dim point As EcPoint = EcPoint.Multiply(Secp256k1Curve.G, k)
                r = point.X.PositiveMod(n)

                If r = BigInteger.Zero Then Continue Do

                ' s = k^-1 * (z + r * privateKey) mod n
                Dim kInv As BigInteger = k.ModInverse(n)
                s = (kInv * (z + r * privateKey)).PositiveMod(n)

                If s = BigInteger.Zero Then Continue Do

                ' Enforce low-S (BIP 62)
                If s > n >> 1 Then
                    s = n - s
                End If

                Exit Do
            Loop

            Return New EcdsaSignature(r, s)
        End Function

        ''' <summary>
        ''' Signs a message hash using a KeyPair.
        ''' </summary>
        Public Shared Function Sign(messageHash As Byte(), keyPair As KeyPair) As EcdsaSignature
            If keyPair Is Nothing Then Throw New ArgumentNullException(NameOf(keyPair))
            Return Sign(messageHash, keyPair.PrivateKey)
        End Function

        ''' <summary>
        ''' Verifies an ECDSA signature against a message hash and public key.
        ''' </summary>
        Public Shared Function Verify(messageHash As Byte(), signature As EcdsaSignature, publicKey As EcPoint) As Boolean
            If messageHash Is Nothing OrElse messageHash.Length <> 32 Then Return False
            If signature Is Nothing Then Return False
            If publicKey Is Nothing OrElse publicKey.IsInfinity Then Return False

            Dim n As BigInteger = Secp256k1Curve.N
            Dim r As BigInteger = signature.R
            Dim s As BigInteger = signature.S

            ' Check r and s are in [1, n-1]
            If r <= BigInteger.Zero OrElse r >= n Then Return False
            If s <= BigInteger.Zero OrElse s >= n Then Return False

            Dim z As BigInteger = FromByteArrayUnsigned(messageHash)
            If z >= n Then z = z - n

            ' w = s^-1 mod n
            Dim w As BigInteger = s.ModInverse(n)

            ' u1 = z * w mod n
            Dim u1 As BigInteger = (z * w).PositiveMod(n)
            ' u2 = r * w mod n
            Dim u2 As BigInteger = (r * w).PositiveMod(n)

            ' (x1, y1) = u1 * G + u2 * publicKey
            Dim point1 As EcPoint = EcPoint.Multiply(Secp256k1Curve.G, u1)
            Dim point2 As EcPoint = EcPoint.Multiply(publicKey, u2)
            Dim point As EcPoint = EcPoint.Add(point1, point2)

            If point.IsInfinity Then Return False

            ' Verify r == x mod n
            Return point.X.PositiveMod(n) = r
        End Function

        ''' <summary>
        ''' Verifies a signature using a KeyPair's public key.
        ''' </summary>
        Public Shared Function Verify(messageHash As Byte(), signature As EcdsaSignature, keyPair As KeyPair) As Boolean
            If keyPair Is Nothing Then Return False
            Return Verify(messageHash, signature, keyPair.PublicKey)
        End Function

    End Class

    ''' <summary>
    ''' Represents an ECDSA signature with r and s components.
    ''' </summary>
    Public Class EcdsaSignature

        ''' <summary>
        ''' The r component of the signature.
        ''' </summary>
        Public ReadOnly Property R As BigInteger

        ''' <summary>
        ''' The s component of the signature.
        ''' </summary>
        Public ReadOnly Property S As BigInteger

        Public Sub New(r As BigInteger, s As BigInteger)
            Me.R = r
            Me.S = s
        End Sub

        ''' <summary>
        ''' Serializes the signature to DER format.
        ''' </summary>
        Public Function ToDer() As Byte()
            Dim rBytes As Byte() = R.ToByteArray()
            Array.Reverse(rBytes) ' To big-endian
            ' Remove leading zeros but keep one if high bit set
            rBytes = TrimLeadingZeros(rBytes)
            If rBytes(0) >= &H80 Then
                Dim padded(rBytes.Length) As Byte
                Array.Copy(rBytes, 0, padded, 1, rBytes.Length)
                rBytes = padded
            End If

            Dim sBytes As Byte() = S.ToByteArray()
            Array.Reverse(sBytes)
            sBytes = TrimLeadingZeros(sBytes)
            If sBytes(0) >= &H80 Then
                Dim padded(sBytes.Length) As Byte
                Array.Copy(sBytes, 0, padded, 1, sBytes.Length)
                sBytes = padded
            End If

            ' DER: 30 <len> 02 <rlen> <r> 02 <slen> <s>
            Dim totalLen As Integer = 2 + rBytes.Length + 2 + sBytes.Length
            Dim der(totalLen + 1) As Byte
            Dim pos As Integer = 0

            der(pos) = &H30 : pos += 1
            der(pos) = CByte(totalLen) : pos += 1
            der(pos) = &H2 : pos += 1
            der(pos) = CByte(rBytes.Length) : pos += 1
            Array.Copy(rBytes, 0, der, pos, rBytes.Length) : pos += rBytes.Length
            der(pos) = &H2 : pos += 1
            der(pos) = CByte(sBytes.Length) : pos += 1
            Array.Copy(sBytes, 0, der, pos, sBytes.Length) : pos += sBytes.Length

            Dim result(pos - 1) As Byte
            Array.Copy(der, result, pos)
            Return result
        End Function

        ''' <summary>
        ''' Deserializes a signature from DER format.
        ''' </summary>
        Public Shared Function FromDer(data As Byte()) As EcdsaSignature
            If data Is Nothing OrElse data.Length < 8 Then
                Throw New FormatException("Invalid DER signature.")
            End If
            If data(0) <> &H30 Then Throw New FormatException("Invalid DER signature prefix.")

            Dim pos As Integer = 2
            If data(pos) <> &H2 Then Throw New FormatException("Invalid DER R marker.")
            pos += 1
            Dim rLen As Integer = data(pos) : pos += 1
            Dim rBytes(rLen - 1) As Byte
            Array.Copy(data, pos, rBytes, 0, rLen) : pos += rLen

            If data(pos) <> &H2 Then Throw New FormatException("Invalid DER S marker.")
            pos += 1
            Dim sLen As Integer = data(pos) : pos += 1
            Dim sBytes(sLen - 1) As Byte
            Array.Copy(data, pos, sBytes, 0, sLen)

            ' Convert to BigInteger (big-endian unsigned)
            Dim r As BigInteger = FromByteArrayUnsigned(rBytes)
            Dim s As BigInteger = FromByteArrayUnsigned(sBytes)

            Return New EcdsaSignature(r, s)
        End Function

        Private Shared Function TrimLeadingZeros(data As Byte()) As Byte()
            Dim start As Integer = 0
            While start < data.Length - 1 AndAlso data(start) = 0
                start += 1
            End While
            If start = 0 Then Return data
            Dim result(data.Length - start - 1) As Byte
            Array.Copy(data, start, result, 0, result.Length)
            Return result
        End Function

        Public Overrides Function ToString() As String
            Return $"Signature(R={HashUtil.ToHex(R.ToByteArrayFixed(32))}, S={HashUtil.ToHex(S.ToByteArrayFixed(32))})"
        End Function

    End Class

End Namespace
