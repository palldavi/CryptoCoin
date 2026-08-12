Imports System
Imports System.IO
Imports System.Numerics
Imports System.Security.Cryptography
Imports System.Text

Namespace CryptoCoin.Cryptography

    ''' <summary>
    ''' Represents an ECDSA signature with R and S components.
    ''' </summary>
    Public NotInheritable Class ECDSASignature
        Implements IEquatable(Of ECDSASignature)

        ''' <summary>
        ''' Gets the R component of the signature.
        ''' </summary>
        Public ReadOnly Property R As BigInteger

        ''' <summary>
        ''' Gets the S component of the signature.
        ''' </summary>
        Public ReadOnly Property S As BigInteger

        ''' <summary>
        ''' Gets the recovery ID (0 or 1) used for public key recovery.
        ''' </summary>
        Public Property RecoveryId As Integer

        ''' <summary>
        ''' Creates a new ECDSA signature with the specified R and S values.
        ''' </summary>
        ''' <param name="r">The R component.</param>
        ''' <param name="s">The S component.</param>
        ''' <exception cref="ArgumentOutOfRangeException">Thrown when R or S is not in valid range.</exception>
        Public Sub New(r As BigInteger, s As BigInteger)
            If r <= BigInteger.Zero OrElse r >= Secp256k1.N Then
                Throw New ArgumentOutOfRangeException(NameOf(r), "R must be in range (0, N).")
            End If
            If s <= BigInteger.Zero OrElse s >= Secp256k1.N Then
                Throw New ArgumentOutOfRangeException(NameOf(s), "S must be in range (0, N).")
            End If

            Me.R = r
            Me.S = s
            Me.RecoveryId = 0
        End Sub

        ''' <summary>
        ''' Creates a new ECDSA signature with R, S, and recovery ID.
        ''' </summary>
        Public Sub New(r As BigInteger, s As BigInteger, recoveryId As Integer)
            Me.New(r, s)
            Me.RecoveryId = recoveryId
        End Sub

        ''' <summary>
        ''' Returns a normalized signature with low-S value (BIP 62).
        ''' If S > N/2, replace S with N - S.
        ''' </summary>
        ''' <returns>A signature with S in the lower half of the range.</returns>
        Public Function ToLowS() As ECDSASignature
            If S > Secp256k1.HalfN Then
                Return New ECDSASignature(R, Secp256k1.N - S, RecoveryId Xor 1)
            End If
            Return Me
        End Function

        ''' <summary>
        ''' Gets whether this signature has a low-S value (BIP 62 compliant).
        ''' </summary>
        Public ReadOnly Property IsLowS As Boolean
            Get
                Return S <= Secp256k1.HalfN
            End Get
        End Property

        ''' <summary>
        ''' Encodes the signature in DER format.
        ''' </summary>
        ''' <returns>The DER-encoded signature bytes.</returns>
        Public Function ToDer() As Byte()
            Dim rBytes() As Byte = BigIntegerToSignedBytes(R)
            Dim sBytes() As Byte = BigIntegerToSignedBytes(S)

            ' DER format: 0x30 [total-length] 0x02 [r-length] [r] 0x02 [s-length] [s]
            Dim totalLength As Integer = 2 + rBytes.Length + 2 + sBytes.Length

            Using ms As New MemoryStream()
                ms.WriteByte(&H30) ' SEQUENCE tag
                ms.WriteByte(CByte(totalLength))
                ms.WriteByte(&H2) ' INTEGER tag for R
                ms.WriteByte(CByte(rBytes.Length))
                ms.Write(rBytes, 0, rBytes.Length)
                ms.WriteByte(&H2) ' INTEGER tag for S
                ms.WriteByte(CByte(sBytes.Length))
                ms.Write(sBytes, 0, sBytes.Length)
                Return ms.ToArray()
            End Using
        End Function

        ''' <summary>
        ''' Decodes a DER-encoded signature.
        ''' </summary>
        ''' <param name="der">The DER-encoded signature bytes.</param>
        ''' <returns>The decoded signature.</returns>
        ''' <exception cref="ArgumentException">Thrown when the DER encoding is invalid.</exception>
        Public Shared Function FromDer(der() As Byte) As ECDSASignature
            If der Is Nothing Then
                Throw New ArgumentNullException(NameOf(der))
            End If
            If der.Length < 8 Then
                Throw New ArgumentException("DER signature too short.", NameOf(der))
            End If

            Dim offset As Integer = 0

            ' Check SEQUENCE tag
            If der(offset) <> &H30 Then
                Throw New ArgumentException("Invalid DER: missing SEQUENCE tag.", NameOf(der))
            End If
            offset += 1

            ' Total length
            Dim totalLength As Integer = CInt(der(offset))
            offset += 1

            If offset + totalLength > der.Length Then
                Throw New ArgumentException("Invalid DER: length exceeds data.", NameOf(der))
            End If

            ' Parse R
            If der(offset) <> &H2 Then
                Throw New ArgumentException("Invalid DER: missing INTEGER tag for R.", NameOf(der))
            End If
            offset += 1

            Dim rLength As Integer = CInt(der(offset))
            offset += 1

            If offset + rLength > der.Length Then
                Throw New ArgumentException("Invalid DER: R length exceeds data.", NameOf(der))
            End If

            Dim rBytes(rLength - 1) As Byte
            Array.Copy(der, offset, rBytes, 0, rLength)
            offset += rLength

            ' Parse S
            If der(offset) <> &H2 Then
                Throw New ArgumentException("Invalid DER: missing INTEGER tag for S.", NameOf(der))
            End If
            offset += 1

            Dim sLength As Integer = CInt(der(offset))
            offset += 1

            If offset + sLength > der.Length Then
                Throw New ArgumentException("Invalid DER: S length exceeds data.", NameOf(der))
            End If

            Dim sBytes(sLength - 1) As Byte
            Array.Copy(der, offset, sBytes, 0, sLength)

            ' Convert from big-endian signed to BigInteger
            Dim r As BigInteger = BytesToBigInteger(rBytes)
            Dim s As BigInteger = BytesToBigInteger(sBytes)

            Return New ECDSASignature(r, s)
        End Function

        ''' <summary>
        ''' Encodes the signature as a compact 64-byte array (R || S).
        ''' </summary>
        ''' <returns>A 64-byte array containing R (32 bytes) and S (32 bytes).</returns>
        Public Function ToCompact() As Byte()
            Dim result(63) As Byte
            Dim rBytes() As Byte = ECPoint.BigIntegerToBytes(R, 32)
            Dim sBytes() As Byte = ECPoint.BigIntegerToBytes(S, 32)
            Array.Copy(rBytes, 0, result, 0, 32)
            Array.Copy(sBytes, 0, result, 32, 32)
            Return result
        End Function

        ''' <summary>
        ''' Decodes a compact 64-byte signature (R || S).
        ''' </summary>
        ''' <param name="compact">The 64-byte compact signature.</param>
        ''' <returns>The decoded signature.</returns>
        Public Shared Function FromCompact(compact() As Byte) As ECDSASignature
            If compact Is Nothing Then
                Throw New ArgumentNullException(NameOf(compact))
            End If
            If compact.Length <> 64 Then
                Throw New ArgumentException("Compact signature must be 64 bytes.", NameOf(compact))
            End If

            Dim rBytes(31) As Byte
            Dim sBytes(31) As Byte
            Array.Copy(compact, 0, rBytes, 0, 32)
            Array.Copy(compact, 32, sBytes, 0, 32)

            ' Convert from big-endian unsigned
            Dim rPadded(32) As Byte
            Dim sPadded(32) As Byte
            Array.Copy(rBytes, rPadded, 32)
            Array.Copy(sBytes, sPadded, 32)
            Array.Reverse(rPadded)
            Array.Reverse(sPadded)

            Dim r As New BigInteger(rPadded)
            Dim s As New BigInteger(sPadded)

            Return New ECDSASignature(r, s)
        End Function

        ''' <summary>
        ''' Determines whether this signature equals another signature.
        ''' </summary>
        Public Overloads Function Equals(other As ECDSASignature) As Boolean Implements IEquatable(Of ECDSASignature).Equals
            If other Is Nothing Then Return False
            Return R.Equals(other.R) AndAlso S.Equals(other.S)
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            Return Equals(TryCast(obj, ECDSASignature))
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return R.GetHashCode() Xor S.GetHashCode()
        End Function

        Public Overrides Function ToString() As String
            Return $"Signature(R={R}, S={S})"
        End Function

        ''' <summary>
        ''' Converts a BigInteger to a DER-compatible signed big-endian byte array.
        ''' </summary>
        Private Shared Function BigIntegerToSignedBytes(value As BigInteger) As Byte()
            Dim bytes() As Byte = value.ToByteArray() ' Little-endian, signed
            Array.Reverse(bytes) ' Convert to big-endian

            ' Remove leading zeros but keep one if the high bit is set
            Dim startIndex As Integer = 0
            While startIndex < bytes.Length - 1 AndAlso bytes(startIndex) = 0 AndAlso (bytes(startIndex + 1) And &H80) = 0
                startIndex += 1
            End While

            If startIndex > 0 Then
                Dim trimmed(bytes.Length - startIndex - 1) As Byte
                Array.Copy(bytes, startIndex, trimmed, 0, trimmed.Length)
                Return trimmed
            End If

            Return bytes
        End Function

        ''' <summary>
        ''' Converts a big-endian signed byte array to a BigInteger.
        ''' </summary>
        Private Shared Function BytesToBigInteger(bytes() As Byte) As BigInteger
            ' Ensure positive by adding a zero byte if high bit is set
            Dim padded() As Byte
            If (bytes(0) And &H80) <> 0 Then
                padded = New Byte(bytes.Length) {}
                Array.Copy(bytes, 0, padded, 1, bytes.Length)
            Else
                padded = New Byte(bytes.Length - 1) {}
                Array.Copy(bytes, padded, bytes.Length)
            End If
            Array.Reverse(padded) ' Convert to little-endian for BigInteger
            Return New BigInteger(padded)
        End Function
    End Class

    ''' <summary>
    ''' Provides ECDSA digital signature operations for the secp256k1 curve.
    ''' Supports signing, verification, deterministic k generation (RFC 6979),
    ''' and signature malleability protection (BIP 62).
    ''' </summary>
    Public NotInheritable Class DigitalSignature

        Private Sub New()
        End Sub

        ''' <summary>
        ''' Signs a 32-byte message hash using the specified private key.
        ''' Uses deterministic k generation per RFC 6979 for security.
        ''' The resulting signature is normalized to low-S form (BIP 62).
        ''' </summary>
        ''' <param name="messageHash">The 32-byte hash of the message to sign.</param>
        ''' <param name="privateKey">The private key to sign with.</param>
        ''' <returns>The ECDSA signature with low-S normalization.</returns>
        ''' <exception cref="ArgumentException">Thrown when inputs are invalid.</exception>
        Public Shared Function Sign(messageHash() As Byte, privateKey As BigInteger) As ECDSASignature
            If messageHash Is Nothing Then
                Throw New ArgumentNullException(NameOf(messageHash))
            End If
            If messageHash.Length <> 32 Then
                Throw New ArgumentException("Message hash must be exactly 32 bytes.", NameOf(messageHash))
            End If
            If Not Secp256k1.IsValidPrivateKey(privateKey) Then
                Throw New ArgumentOutOfRangeException(NameOf(privateKey), "Invalid private key.")
            End If

            ' Convert message hash to BigInteger
            Dim z As BigInteger = HashToBigInteger(messageHash)

            ' Generate deterministic k using RFC 6979
            Dim k As BigInteger = GenerateDeterministicK(messageHash, privateKey)

            ' Compute R = k * G
            Dim rPoint As ECPoint = Secp256k1.GeneratorMultiply(k)
            Dim r As BigInteger = rPoint.X Mod Secp256k1.N

            If r = BigInteger.Zero Then
                Throw New CryptographicException("Signature generation failed: R = 0.")
            End If

            ' Compute S = k^(-1) * (z + r * privateKey) mod N
            Dim kInverse As BigInteger = Secp256k1.ModInverse(k, Secp256k1.N)
            Dim s As BigInteger = (kInverse * (z + r * privateKey)) Mod Secp256k1.N

            If s = BigInteger.Zero Then
                Throw New CryptographicException("Signature generation failed: S = 0.")
            End If

            ' Determine recovery ID
            Dim recoveryId As Integer = 0
            If rPoint.Y.IsEven Then recoveryId = 0 Else recoveryId = 1
            If r <> rPoint.X Then recoveryId += 2

            Dim signature As New ECDSASignature(r, s, recoveryId)

            ' Normalize to low-S (BIP 62)
            Return signature.ToLowS()
        End Function

        ''' <summary>
        ''' Signs a message hash using a KeyPair instance.
        ''' </summary>
        ''' <param name="messageHash">The 32-byte hash of the message to sign.</param>
        ''' <param name="keyPair">The key pair containing the private key.</param>
        ''' <returns>The ECDSA signature.</returns>
        Public Shared Function Sign(messageHash() As Byte, keyPair As KeyPair) As ECDSASignature
            If keyPair Is Nothing Then
                Throw New ArgumentNullException(NameOf(keyPair))
            End If
            Return Sign(messageHash, keyPair.PrivateKey)
        End Function

        ''' <summary>
        ''' Verifies an ECDSA signature against a message hash and public key.
        ''' </summary>
        ''' <param name="messageHash">The 32-byte hash of the signed message.</param>
        ''' <param name="signature">The signature to verify.</param>
        ''' <param name="publicKey">The public key point to verify against.</param>
        ''' <returns>True if the signature is valid; otherwise, False.</returns>
        Public Shared Function Verify(messageHash() As Byte, signature As ECDSASignature, publicKey As ECPoint) As Boolean
            If messageHash Is Nothing OrElse messageHash.Length <> 32 Then Return False
            If signature Is Nothing Then Return False
            If publicKey Is Nothing OrElse publicKey.IsInfinity Then Return False

            Try
                ' Verify R and S are in valid range
                If signature.R <= BigInteger.Zero OrElse signature.R >= Secp256k1.N Then Return False
                If signature.S <= BigInteger.Zero OrElse signature.S >= Secp256k1.N Then Return False

                ' Convert message hash to BigInteger
                Dim z As BigInteger = HashToBigInteger(messageHash)

                ' Compute w = s^(-1) mod N
                Dim w As BigInteger = Secp256k1.ModInverse(signature.S, Secp256k1.N)

                ' Compute u1 = z * w mod N
                Dim u1 As BigInteger = (z * w) Mod Secp256k1.N

                ' Compute u2 = r * w mod N
                Dim u2 As BigInteger = (signature.R * w) Mod Secp256k1.N

                ' Compute point = u1*G + u2*Q using Shamir's trick
                Dim point As ECPoint = Secp256k1.ShamirMultiply(u1, Secp256k1.G, u2, publicKey)

                If point.IsInfinity Then Return False

                ' Verify: point.X mod N == R
                Dim v As BigInteger = point.X Mod Secp256k1.N
                Return v = signature.R

            Catch
                Return False
            End Try
        End Function

        ''' <summary>
        ''' Verifies an ECDSA signature using public key bytes.
        ''' </summary>
        ''' <param name="messageHash">The 32-byte message hash.</param>
        ''' <param name="signature">The signature to verify.</param>
        ''' <param name="publicKeyBytes">The public key bytes (33 or 65 bytes).</param>
        ''' <returns>True if the signature is valid.</returns>
        Public Shared Function Verify(messageHash() As Byte, signature As ECDSASignature, publicKeyBytes() As Byte) As Boolean
            Try
                Dim publicKey As ECPoint = Secp256k1.ParsePublicKey(publicKeyBytes)
                Return Verify(messageHash, signature, publicKey)
            Catch
                Return False
            End Try
        End Function

        ''' <summary>
        ''' Verifies a DER-encoded signature against a message hash and public key bytes.
        ''' </summary>
        ''' <param name="messageHash">The 32-byte message hash.</param>
        ''' <param name="derSignature">The DER-encoded signature.</param>
        ''' <param name="publicKeyBytes">The public key bytes.</param>
        ''' <returns>True if the signature is valid.</returns>
        Public Shared Function VerifyDer(messageHash() As Byte, derSignature() As Byte, publicKeyBytes() As Byte) As Boolean
            Try
                Dim sig As ECDSASignature = ECDSASignature.FromDer(derSignature)
                Return Verify(messageHash, sig, publicKeyBytes)
            Catch
                Return False
            End Try
        End Function

        ''' <summary>
        ''' Performs batch verification of multiple signatures.
        ''' Returns True only if ALL signatures are valid.
        ''' </summary>
        ''' <param name="items">Collection of (messageHash, signature, publicKey) tuples to verify.</param>
        ''' <returns>True if all signatures are valid; False if any signature is invalid.</returns>
        Public Shared Function BatchVerify(items As IEnumerable(Of Tuple(Of Byte(), ECDSASignature, ECPoint))) As Boolean
            If items Is Nothing Then
                Throw New ArgumentNullException(NameOf(items))
            End If

            For Each item In items
                If item Is Nothing Then Return False
                If Not Verify(item.Item1, item.Item2, item.Item3) Then
                    Return False
                End If
            Next

            Return True
        End Function

        ''' <summary>
        ''' Recovers the public key from a signature and message hash.
        ''' Requires the recovery ID to determine which of the possible public keys is correct.
        ''' </summary>
        ''' <param name="messageHash">The 32-byte message hash that was signed.</param>
        ''' <param name="signature">The signature with recovery ID set.</param>
        ''' <returns>The recovered public key point, or Nothing if recovery fails.</returns>
        Public Shared Function RecoverPublicKey(messageHash() As Byte, signature As ECDSASignature) As ECPoint
            If messageHash Is Nothing OrElse messageHash.Length <> 32 Then Return Nothing
            If signature Is Nothing Then Return Nothing

            Try
                Dim r As BigInteger = signature.R
                Dim s As BigInteger = signature.S
                Dim recoveryId As Integer = signature.RecoveryId

                ' Determine the X coordinate of R point
                Dim x As BigInteger = r
                If recoveryId >= 2 Then
                    x = x + Secp256k1.N
                End If

                If x >= Secp256k1.P Then Return Nothing

                ' Recover the Y coordinate
                Dim ySquared As BigInteger = (BigInteger.ModPow(x, 3, Secp256k1.P) + Secp256k1.B) Mod Secp256k1.P
                Dim y As BigInteger = Secp256k1.ModSqrt(ySquared, Secp256k1.P)

                ' Choose correct Y parity
                Dim isYEven As Boolean = y.IsEven
                Dim wantEven As Boolean = ((recoveryId And 1) = 0)
                If isYEven <> wantEven Then
                    y = Secp256k1.P - y
                End If

                Dim rPoint As New ECPoint(x, y)

                ' Verify point is on curve
                If Not Secp256k1.IsOnCurve(rPoint) Then Return Nothing

                ' Compute public key: Q = r^(-1) * (s*R - z*G)
                Dim z As BigInteger = HashToBigInteger(messageHash)
                Dim rInverse As BigInteger = Secp256k1.ModInverse(r, Secp256k1.N)

                Dim sR As ECPoint = Secp256k1.ScalarMultiply(s, rPoint)
                Dim zG As ECPoint = Secp256k1.ScalarMultiply(z, Secp256k1.G)
                Dim negZG As ECPoint = zG.Negate(Secp256k1.P)
                Dim diff As ECPoint = Secp256k1.PointAdd(sR, negZG)
                Dim publicKey As ECPoint = Secp256k1.ScalarMultiply(rInverse, diff)

                ' Verify the recovered key
                If Verify(messageHash, signature, publicKey) Then
                    Return publicKey
                End If

                Return Nothing
            Catch
                Return Nothing
            End Try
        End Function

        ''' <summary>
        ''' Generates a deterministic k value per RFC 6979.
        ''' This ensures that the same message and key always produce the same signature
        ''' without requiring a random number generator.
        ''' </summary>
        ''' <param name="messageHash">The 32-byte message hash.</param>
        ''' <param name="privateKey">The private key.</param>
        ''' <returns>A deterministic k value suitable for ECDSA signing.</returns>
        Public Shared Function GenerateDeterministicK(messageHash() As Byte, privateKey As BigInteger) As BigInteger
            ' RFC 6979 Section 3.2
            Dim privKeyBytes() As Byte = ECPoint.BigIntegerToBytes(privateKey, 32)

            ' Step a: h1 = H(m) - already provided as messageHash
            ' Step b: V = 0x01 0x01 ... 0x01 (32 bytes)
            Dim v(31) As Byte
            For i As Integer = 0 To 31
                v(i) = &H1
            Next

            ' Step c: K = 0x00 0x00 ... 0x00 (32 bytes)
            Dim k(31) As Byte

            ' Step d: K = HMAC_K(V || 0x00 || int2octets(x) || bits2octets(h1))
            Dim combined() As Byte = ConcatBytes(v, New Byte() {&H0}, privKeyBytes, messageHash)
            Using hmac As New HMACSHA256(k)
                k = hmac.ComputeHash(combined)
            End Using

            ' Step e: V = HMAC_K(V)
            Using hmac As New HMACSHA256(k)
                v = hmac.ComputeHash(v)
            End Using

            ' Step f: K = HMAC_K(V || 0x01 || int2octets(x) || bits2octets(h1))
            combined = ConcatBytes(v, New Byte() {&H1}, privKeyBytes, messageHash)
            Using hmac As New HMACSHA256(k)
                k = hmac.ComputeHash(combined)
            End Using

            ' Step g: V = HMAC_K(V)
            Using hmac As New HMACSHA256(k)
                v = hmac.ComputeHash(v)
            End Using

            ' Step h: Generate k
            Dim attempts As Integer = 0
            While True
                ' V = HMAC_K(V)
                Using hmac As New HMACSHA256(k)
                    v = hmac.ComputeHash(v)
                End Using

                ' Convert V to integer
                Dim padded(32) As Byte
                Array.Copy(v, padded, 32)
                Array.Reverse(padded)
                Dim candidate As New BigInteger(padded)

                ' Check if k is valid
                If candidate > BigInteger.Zero AndAlso candidate < Secp256k1.N Then
                    Return candidate
                End If

                ' Update K and V for next iteration
                combined = ConcatBytes(v, New Byte() {&H0})
                Using hmac As New HMACSHA256(k)
                    k = hmac.ComputeHash(combined)
                End Using
                Using hmac As New HMACSHA256(k)
                    v = hmac.ComputeHash(v)
                End Using

                attempts += 1
                If attempts > 1000 Then
                    Throw New CryptographicException("Failed to generate deterministic k after maximum attempts.")
                End If
            End While

            ' Should never reach here
            Throw New CryptographicException("Unreachable code in deterministic k generation.")
        End Function

        ''' <summary>
        ''' Validates that a signature is strictly DER-encoded (BIP 66).
        ''' </summary>
        ''' <param name="der">The DER-encoded signature bytes.</param>
        ''' <returns>True if the encoding is strictly valid DER.</returns>
        Public Shared Function IsStrictDer(der() As Byte) As Boolean
            If der Is Nothing OrElse der.Length < 8 OrElse der.Length > 72 Then
                Return False
            End If

            ' Check SEQUENCE tag
            If der(0) <> &H30 Then Return False

            ' Check total length
            If der(1) <> der.Length - 2 Then Return False

            ' Check R INTEGER tag
            If der(2) <> &H2 Then Return False

            Dim rLen As Integer = CInt(der(3))
            If rLen = 0 Then Return False
            If 4 + rLen >= der.Length Then Return False

            ' Check R is not negative (no unnecessary leading zero)
            If (der(4) And &H80) <> 0 Then Return False
            If rLen > 1 AndAlso der(4) = 0 AndAlso (der(5) And &H80) = 0 Then Return False

            ' Check S INTEGER tag
            Dim sOffset As Integer = 4 + rLen
            If der(sOffset) <> &H2 Then Return False

            Dim sLen As Integer = CInt(der(sOffset + 1))
            If sLen = 0 Then Return False
            If sOffset + 2 + sLen <> der.Length Then Return False

            ' Check S is not negative
            If (der(sOffset + 2) And &H80) <> 0 Then Return False
            If sLen > 1 AndAlso der(sOffset + 2) = 0 AndAlso (der(sOffset + 3) And &H80) = 0 Then Return False

            Return True
        End Function

        ''' <summary>
        ''' Converts a 32-byte hash to a BigInteger for use in ECDSA operations.
        ''' </summary>
        Private Shared Function HashToBigInteger(hash() As Byte) As BigInteger
            ' Add zero byte to ensure positive interpretation
            Dim padded(32) As Byte
            Array.Copy(hash, padded, 32)
            Array.Reverse(padded) ' Convert from big-endian to little-endian
            Return New BigInteger(padded)
        End Function

        ''' <summary>
        ''' Concatenates multiple byte arrays into one.
        ''' </summary>
        Private Shared Function ConcatBytes(ParamArray arrays() As Byte()) As Byte()
            Dim totalLength As Integer = 0
            For Each arr In arrays
                totalLength += arr.Length
            Next

            Dim result(totalLength - 1) As Byte
            Dim offset As Integer = 0
            For Each arr In arrays
                Array.Copy(arr, 0, result, offset, arr.Length)
                offset += arr.Length
            Next

            Return result
        End Function
    End Class

End Namespace
