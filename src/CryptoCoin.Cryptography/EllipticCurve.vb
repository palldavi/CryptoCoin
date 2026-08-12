Imports System
Imports System.Numerics
Imports System.Security.Cryptography

Namespace CryptoCoin.Cryptography

    ''' <summary>
    ''' Represents a point on an elliptic curve with X and Y coordinates.
    ''' Supports the point at infinity (identity element) for group operations.
    ''' </summary>
    Public NotInheritable Class ECPoint
        Implements IEquatable(Of ECPoint)

        ''' <summary>
        ''' Gets the X coordinate of the point.
        ''' </summary>
        Public ReadOnly Property X As BigInteger

        ''' <summary>
        ''' Gets the Y coordinate of the point.
        ''' </summary>
        Public ReadOnly Property Y As BigInteger

        ''' <summary>
        ''' Gets whether this point represents the point at infinity (identity element).
        ''' </summary>
        Public ReadOnly Property IsInfinity As Boolean

        ''' <summary>
        ''' The point at infinity (additive identity on the curve).
        ''' </summary>
        Public Shared ReadOnly Infinity As New ECPoint()

        ''' <summary>
        ''' Creates the point at infinity.
        ''' </summary>
        Private Sub New()
            IsInfinity = True
            X = BigInteger.Zero
            Y = BigInteger.Zero
        End Sub

        ''' <summary>
        ''' Creates a new point with the specified coordinates.
        ''' </summary>
        ''' <param name="x">The X coordinate.</param>
        ''' <param name="y">The Y coordinate.</param>
        Public Sub New(x As BigInteger, y As BigInteger)
            Me.X = x
            Me.Y = y
            Me.IsInfinity = False
        End Sub

        ''' <summary>
        ''' Creates a point from byte arrays representing X and Y coordinates.
        ''' </summary>
        ''' <param name="xBytes">Big-endian byte array for X coordinate.</param>
        ''' <param name="yBytes">Big-endian byte array for Y coordinate.</param>
        Public Sub New(xBytes() As Byte, yBytes() As Byte)
            If xBytes Is Nothing Then Throw New ArgumentNullException(NameOf(xBytes))
            If yBytes Is Nothing Then Throw New ArgumentNullException(NameOf(yBytes))

            ' Convert from big-endian unsigned
            Dim xPadded(xBytes.Length) As Byte
            Dim yPadded(yBytes.Length) As Byte
            Array.Copy(xBytes, 0, xPadded, 0, xBytes.Length)
            Array.Copy(yBytes, 0, yPadded, 0, yBytes.Length)
            Array.Reverse(xPadded)
            Array.Reverse(yPadded)

            Me.X = New BigInteger(xPadded)
            Me.Y = New BigInteger(yPadded)
            Me.IsInfinity = False
        End Sub

        ''' <summary>
        ''' Returns the compressed public key encoding (33 bytes).
        ''' Format: 0x02 + X if Y is even, 0x03 + X if Y is odd.
        ''' </summary>
        ''' <returns>33-byte compressed point encoding.</returns>
        Public Function ToCompressedBytes() As Byte()
            If IsInfinity Then
                Return New Byte() {&H0}
            End If

            Dim xBytes() As Byte = BigIntegerToBytes(X, 32)
            Dim result(32) As Byte

            If Y.IsEven Then
                result(0) = &H2
            Else
                result(0) = &H3
            End If

            Array.Copy(xBytes, 0, result, 1, 32)
            Return result
        End Function

        ''' <summary>
        ''' Returns the uncompressed public key encoding (65 bytes).
        ''' Format: 0x04 + X (32 bytes) + Y (32 bytes).
        ''' </summary>
        ''' <returns>65-byte uncompressed point encoding.</returns>
        Public Function ToUncompressedBytes() As Byte()
            If IsInfinity Then
                Return New Byte() {&H0}
            End If

            Dim xBytes() As Byte = BigIntegerToBytes(X, 32)
            Dim yBytes() As Byte = BigIntegerToBytes(Y, 32)
            Dim result(64) As Byte

            result(0) = &H4
            Array.Copy(xBytes, 0, result, 1, 32)
            Array.Copy(yBytes, 0, result, 33, 32)
            Return result
        End Function

        ''' <summary>
        ''' Determines whether this point equals another point.
        ''' </summary>
        Public Overloads Function Equals(other As ECPoint) As Boolean Implements IEquatable(Of ECPoint).Equals
            If other Is Nothing Then Return False
            If IsInfinity AndAlso other.IsInfinity Then Return True
            If IsInfinity OrElse other.IsInfinity Then Return False
            Return X.Equals(other.X) AndAlso Y.Equals(other.Y)
        End Function

        ''' <summary>
        ''' Determines whether this point equals another object.
        ''' </summary>
        Public Overrides Function Equals(obj As Object) As Boolean
            Return Equals(TryCast(obj, ECPoint))
        End Function

        ''' <summary>
        ''' Returns a hash code for this point.
        ''' </summary>
        Public Overrides Function GetHashCode() As Integer
            If IsInfinity Then Return 0
            Return X.GetHashCode() Xor Y.GetHashCode()
        End Function

        ''' <summary>
        ''' Returns a string representation of this point.
        ''' </summary>
        Public Overrides Function ToString() As String
            If IsInfinity Then Return "(Infinity)"
            Return $"({X}, {Y})"
        End Function

        ''' <summary>
        ''' Returns the negation of this point (same X, negated Y mod p).
        ''' </summary>
        ''' <param name="p">The field prime.</param>
        ''' <returns>The negated point.</returns>
        Public Function Negate(p As BigInteger) As ECPoint
            If IsInfinity Then Return Infinity
            Dim negY As BigInteger = (p - Y) Mod p
            Return New ECPoint(X, negY)
        End Function

        ''' <summary>
        ''' Converts a BigInteger to a fixed-length big-endian byte array.
        ''' </summary>
        Friend Shared Function BigIntegerToBytes(value As BigInteger, length As Integer) As Byte()
            Dim bytes() As Byte = value.ToByteArray() ' Little-endian with possible sign byte
            Dim result(length - 1) As Byte

            ' Copy bytes (skip sign byte if present)
            Dim copyLen As Integer = Math.Min(bytes.Length, length)
            If bytes.Length > length AndAlso bytes(bytes.Length - 1) = 0 Then
                copyLen = length
            End If

            Array.Copy(bytes, 0, result, 0, Math.Min(copyLen, length))
            Array.Reverse(result) ' Convert to big-endian
            Return result
        End Function

        Public Shared Operator =(left As ECPoint, right As ECPoint) As Boolean
            If left Is Nothing Then Return right Is Nothing
            Return left.Equals(right)
        End Operator

        Public Shared Operator <>(left As ECPoint, right As ECPoint) As Boolean
            Return Not (left = right)
        End Operator
    End Class

    ''' <summary>
    ''' Implements the secp256k1 elliptic curve used in Bitcoin and CryptoCoin.
    ''' Provides point arithmetic operations including addition, doubling, and scalar multiplication.
    ''' 
    ''' The curve equation is: y² = x³ + 7 (mod p)
    ''' Where p = 2²⁵⁶ - 2³² - 977
    ''' </summary>
    Public NotInheritable Class Secp256k1

        Private Sub New()
        End Sub

        ''' <summary>
        ''' The prime field modulus p = 2^256 - 2^32 - 977.
        ''' </summary>
        Public Shared ReadOnly P As BigInteger = BigInteger.Parse(
            "115792089237316195423570985008687907853269984665640564039457584007908834671663")

        ''' <summary>
        ''' The curve parameter a = 0.
        ''' </summary>
        Public Shared ReadOnly A As BigInteger = BigInteger.Zero

        ''' <summary>
        ''' The curve parameter b = 7.
        ''' </summary>
        Public Shared ReadOnly B As New BigInteger(7)

        ''' <summary>
        ''' The order of the generator point (number of points on the curve).
        ''' </summary>
        Public Shared ReadOnly N As BigInteger = BigInteger.Parse(
            "115792089237316195423570985008687907852837564279074904382605163141518161494337")

        ''' <summary>
        ''' The cofactor h = 1.
        ''' </summary>
        Public Shared ReadOnly H As BigInteger = BigInteger.One

        ''' <summary>
        ''' The X coordinate of the generator point G.
        ''' </summary>
        Private Shared ReadOnly Gx As BigInteger = BigInteger.Parse(
            "55066263022277343669578718895168534326250603453777594175500187360389116729240")

        ''' <summary>
        ''' The Y coordinate of the generator point G.
        ''' </summary>
        Private Shared ReadOnly Gy As BigInteger = BigInteger.Parse(
            "32670510020758816978083085130507043184471273380659243275938904335757245426176")

        ''' <summary>
        ''' The generator point G of the secp256k1 curve.
        ''' </summary>
        Public Shared ReadOnly G As New ECPoint(Gx, Gy)

        ''' <summary>
        ''' Half of the curve order N, used for low-S normalization (BIP 62).
        ''' </summary>
        Public Shared ReadOnly HalfN As BigInteger = N >> 1

        ''' <summary>
        ''' Adds two points on the secp256k1 curve.
        ''' </summary>
        ''' <param name="p1">The first point.</param>
        ''' <param name="p2">The second point.</param>
        ''' <returns>The sum of the two points.</returns>
        ''' <exception cref="ArgumentNullException">Thrown when either point is Nothing.</exception>
        Public Shared Function PointAdd(p1 As ECPoint, p2 As ECPoint) As ECPoint
            If p1 Is Nothing Then Throw New ArgumentNullException(NameOf(p1))
            If p2 Is Nothing Then Throw New ArgumentNullException(NameOf(p2))

            ' Handle identity element
            If p1.IsInfinity Then Return p2
            If p2.IsInfinity Then Return p1

            ' If points are inverses, return infinity
            If p1.X = p2.X Then
                If (p1.Y + p2.Y) Mod P = BigInteger.Zero Then
                    Return ECPoint.Infinity
                End If
                ' Points are the same - use doubling
                Return PointDouble(p1)
            End If

            ' Standard point addition
            ' slope = (y2 - y1) / (x2 - x1) mod p
            Dim deltaY As BigInteger = (p2.Y - p1.Y) Mod P
            If deltaY < BigInteger.Zero Then deltaY += P

            Dim deltaX As BigInteger = (p2.X - p1.X) Mod P
            If deltaX < BigInteger.Zero Then deltaX += P

            Dim slope As BigInteger = (deltaY * ModInverse(deltaX, P)) Mod P

            ' x3 = slope² - x1 - x2 mod p
            Dim x3 As BigInteger = (slope * slope - p1.X - p2.X) Mod P
            If x3 < BigInteger.Zero Then x3 += P

            ' y3 = slope * (x1 - x3) - y1 mod p
            Dim y3 As BigInteger = (slope * (p1.X - x3) - p1.Y) Mod P
            If y3 < BigInteger.Zero Then y3 += P

            Return New ECPoint(x3, y3)
        End Function

        ''' <summary>
        ''' Doubles a point on the secp256k1 curve.
        ''' </summary>
        ''' <param name="point">The point to double.</param>
        ''' <returns>The doubled point (2P).</returns>
        ''' <exception cref="ArgumentNullException">Thrown when point is Nothing.</exception>
        Public Shared Function PointDouble(point As ECPoint) As ECPoint
            If point Is Nothing Then Throw New ArgumentNullException(NameOf(point))
            If point.IsInfinity Then Return ECPoint.Infinity

            ' If Y = 0, the tangent is vertical
            If point.Y = BigInteger.Zero Then
                Return ECPoint.Infinity
            End If

            ' slope = (3x² + a) / (2y) mod p
            ' For secp256k1, a = 0, so slope = 3x² / (2y) mod p
            Dim numerator As BigInteger = (3 * point.X * point.X + A) Mod P
            Dim denominator As BigInteger = (2 * point.Y) Mod P
            If denominator < BigInteger.Zero Then denominator += P

            Dim slope As BigInteger = (numerator * ModInverse(denominator, P)) Mod P

            ' x3 = slope² - 2x mod p
            Dim x3 As BigInteger = (slope * slope - 2 * point.X) Mod P
            If x3 < BigInteger.Zero Then x3 += P

            ' y3 = slope * (x - x3) - y mod p
            Dim y3 As BigInteger = (slope * (point.X - x3) - point.Y) Mod P
            If y3 < BigInteger.Zero Then y3 += P

            Return New ECPoint(x3, y3)
        End Function

        ''' <summary>
        ''' Performs scalar multiplication of a point by a scalar value using the double-and-add algorithm.
        ''' Computes k * P where k is the scalar and P is the point.
        ''' </summary>
        ''' <param name="k">The scalar multiplier (typically a private key).</param>
        ''' <param name="point">The point to multiply (typically the generator G).</param>
        ''' <returns>The resulting point k*P.</returns>
        ''' <exception cref="ArgumentNullException">Thrown when point is Nothing.</exception>
        ''' <exception cref="ArgumentOutOfRangeException">Thrown when k is not in valid range.</exception>
        Public Shared Function ScalarMultiply(k As BigInteger, point As ECPoint) As ECPoint
            If point Is Nothing Then Throw New ArgumentNullException(NameOf(point))
            If k <= BigInteger.Zero Then
                Throw New ArgumentOutOfRangeException(NameOf(k), "Scalar must be positive.")
            End If

            ' Reduce k modulo N
            k = k Mod N
            If k = BigInteger.Zero Then Return ECPoint.Infinity

            ' Double-and-add algorithm (constant-time variant would be preferred in production)
            Dim result As ECPoint = ECPoint.Infinity
            Dim addend As ECPoint = point

            While k > BigInteger.Zero
                If Not k.IsEven Then
                    result = PointAdd(result, addend)
                End If
                addend = PointDouble(addend)
                k >>= 1
            End While

            Return result
        End Function

        ''' <summary>
        ''' Multiplies the generator point G by a scalar.
        ''' This is the primary operation for deriving a public key from a private key.
        ''' </summary>
        ''' <param name="k">The scalar (private key).</param>
        ''' <returns>The public key point k*G.</returns>
        Public Shared Function GeneratorMultiply(k As BigInteger) As ECPoint
            Return ScalarMultiply(k, G)
        End Function

        ''' <summary>
        ''' Validates that a point lies on the secp256k1 curve.
        ''' Checks that y² ≡ x³ + 7 (mod p).
        ''' </summary>
        ''' <param name="point">The point to validate.</param>
        ''' <returns>True if the point is on the curve; otherwise, False.</returns>
        Public Shared Function IsOnCurve(point As ECPoint) As Boolean
            If point Is Nothing Then Return False
            If point.IsInfinity Then Return True

            ' Check: y² mod p == (x³ + 7) mod p
            Dim left As BigInteger = BigInteger.ModPow(point.Y, 2, P)
            Dim right As BigInteger = (BigInteger.ModPow(point.X, 3, P) + B) Mod P

            Return left = right
        End Function

        ''' <summary>
        ''' Validates that a point is a valid public key on secp256k1.
        ''' Checks that the point is on the curve, not infinity, and has the correct order.
        ''' </summary>
        ''' <param name="point">The point to validate.</param>
        ''' <returns>True if the point is a valid public key.</returns>
        Public Shared Function IsValidPublicKey(point As ECPoint) As Boolean
            If point Is Nothing Then Return False
            If point.IsInfinity Then Return False

            ' Check coordinates are in valid range
            If point.X < BigInteger.Zero OrElse point.X >= P Then Return False
            If point.Y < BigInteger.Zero OrElse point.Y >= P Then Return False

            ' Check point is on curve
            If Not IsOnCurve(point) Then Return False

            ' Check point has correct order (n * P = infinity)
            Dim nP As ECPoint = ScalarMultiply(N, point)
            If Not nP.IsInfinity Then Return False

            Return True
        End Function

        ''' <summary>
        ''' Decompresses a compressed public key (33 bytes) to a full point.
        ''' </summary>
        ''' <param name="compressed">The 33-byte compressed public key.</param>
        ''' <returns>The decompressed point.</returns>
        ''' <exception cref="ArgumentException">Thrown when the compressed key is invalid.</exception>
        Public Shared Function DecompressPoint(compressed() As Byte) As ECPoint
            If compressed Is Nothing Then
                Throw New ArgumentNullException(NameOf(compressed))
            End If
            If compressed.Length <> 33 Then
                Throw New ArgumentException("Compressed public key must be 33 bytes.", NameOf(compressed))
            End If

            Dim prefix As Byte = compressed(0)
            If prefix <> &H2 AndAlso prefix <> &H3 Then
                Throw New ArgumentException("Invalid compression prefix. Must be 0x02 or 0x03.", NameOf(compressed))
            End If

            ' Extract X coordinate (big-endian)
            Dim xBytes(31) As Byte
            Array.Copy(compressed, 1, xBytes, 0, 32)

            ' Convert from big-endian to BigInteger
            Dim xPadded(32) As Byte
            Array.Copy(xBytes, xPadded, 32)
            Array.Reverse(xPadded)
            Dim x As New BigInteger(xPadded)

            ' Compute y² = x³ + 7 mod p
            Dim ySquared As BigInteger = (BigInteger.ModPow(x, 3, P) + B) Mod P

            ' Compute square root using Tonelli-Shanks (for p ≡ 3 mod 4, use y = ySquared^((p+1)/4) mod p)
            Dim y As BigInteger = ModSqrt(ySquared, P)

            ' Choose the correct Y based on the prefix (even/odd)
            Dim isYEven As Boolean = y.IsEven
            Dim wantEven As Boolean = (prefix = &H2)

            If isYEven <> wantEven Then
                y = P - y
            End If

            Dim result As New ECPoint(x, y)

            ' Verify the point is on the curve
            If Not IsOnCurve(result) Then
                Throw New CryptographicException("Decompressed point is not on the curve.")
            End If

            Return result
        End Function

        ''' <summary>
        ''' Parses a public key from its byte representation (compressed or uncompressed).
        ''' </summary>
        ''' <param name="publicKeyBytes">The public key bytes (33 or 65 bytes).</param>
        ''' <returns>The parsed ECPoint.</returns>
        Public Shared Function ParsePublicKey(publicKeyBytes() As Byte) As ECPoint
            If publicKeyBytes Is Nothing Then
                Throw New ArgumentNullException(NameOf(publicKeyBytes))
            End If

            If publicKeyBytes.Length = 33 Then
                ' Compressed format
                Return DecompressPoint(publicKeyBytes)
            ElseIf publicKeyBytes.Length = 65 Then
                ' Uncompressed format
                If publicKeyBytes(0) <> &H4 Then
                    Throw New ArgumentException("Uncompressed public key must start with 0x04.", NameOf(publicKeyBytes))
                End If

                Dim xBytes(31) As Byte
                Dim yBytes(31) As Byte
                Array.Copy(publicKeyBytes, 1, xBytes, 0, 32)
                Array.Copy(publicKeyBytes, 33, yBytes, 0, 32)

                ' Convert from big-endian
                Dim xPadded(32) As Byte
                Dim yPadded(32) As Byte
                Array.Copy(xBytes, xPadded, 32)
                Array.Copy(yBytes, yPadded, 32)
                Array.Reverse(xPadded)
                Array.Reverse(yPadded)

                Dim x As New BigInteger(xPadded)
                Dim y As New BigInteger(yPadded)

                Dim point As New ECPoint(x, y)
                If Not IsOnCurve(point) Then
                    Throw New CryptographicException("Point is not on the secp256k1 curve.")
                End If

                Return point
            Else
                Throw New ArgumentException("Public key must be 33 (compressed) or 65 (uncompressed) bytes.", NameOf(publicKeyBytes))
            End If
        End Function

        ''' <summary>
        ''' Validates that a private key scalar is in the valid range [1, N-1].
        ''' </summary>
        ''' <param name="privateKey">The private key to validate.</param>
        ''' <returns>True if the private key is valid.</returns>
        Public Shared Function IsValidPrivateKey(privateKey As BigInteger) As Boolean
            Return privateKey > BigInteger.Zero AndAlso privateKey < N
        End Function

        ''' <summary>
        ''' Computes the modular multiplicative inverse using the extended Euclidean algorithm.
        ''' Finds x such that (a * x) mod m = 1.
        ''' </summary>
        ''' <param name="a">The value to invert.</param>
        ''' <param name="m">The modulus.</param>
        ''' <returns>The modular inverse of a mod m.</returns>
        Public Shared Function ModInverse(a As BigInteger, m As BigInteger) As BigInteger
            If a < BigInteger.Zero Then a = ((a Mod m) + m) Mod m

            Dim g As BigInteger = BigInteger.Zero
            Dim x As BigInteger = BigInteger.Zero
            Dim y As BigInteger = BigInteger.Zero

            ExtendedGcd(a, m, g, x, y)

            If g <> BigInteger.One Then
                Throw New ArithmeticException("Modular inverse does not exist.")
            End If

            Return ((x Mod m) + m) Mod m
        End Function

        ''' <summary>
        ''' Computes the modular square root using the Tonelli-Shanks algorithm.
        ''' For secp256k1, p ≡ 3 (mod 4), so we can use the simpler formula: sqrt(a) = a^((p+1)/4) mod p.
        ''' </summary>
        ''' <param name="a">The value to find the square root of.</param>
        ''' <param name="p">The prime modulus.</param>
        ''' <returns>A square root of a mod p.</returns>
        Public Shared Function ModSqrt(a As BigInteger, p As BigInteger) As BigInteger
            ' For p ≡ 3 (mod 4): sqrt(a) = a^((p+1)/4) mod p
            Dim exponent As BigInteger = (p + 1) / 4
            Dim result As BigInteger = BigInteger.ModPow(a, exponent, p)

            ' Verify
            If BigInteger.ModPow(result, 2, p) <> a Mod p Then
                Throw New ArithmeticException("No square root exists for the given value.")
            End If

            Return result
        End Function

        ''' <summary>
        ''' Extended Euclidean algorithm to find gcd and Bezout coefficients.
        ''' </summary>
        Private Shared Sub ExtendedGcd(a As BigInteger, b As BigInteger, ByRef gcd As BigInteger, ByRef x As BigInteger, ByRef y As BigInteger)
            If a = BigInteger.Zero Then
                gcd = b
                x = BigInteger.Zero
                y = BigInteger.One
                Return
            End If

            Dim g1 As BigInteger = BigInteger.Zero
            Dim x1 As BigInteger = BigInteger.Zero
            Dim y1 As BigInteger = BigInteger.Zero

            ExtendedGcd(b Mod a, a, g1, x1, y1)

            gcd = g1
            x = y1 - (b / a) * x1
            y = x1
        End Sub

        ''' <summary>
        ''' Performs simultaneous scalar multiplication (Shamir's trick).
        ''' Computes k1*P1 + k2*P2 more efficiently than separate multiplications.
        ''' Used in ECDSA verification.
        ''' </summary>
        ''' <param name="k1">First scalar.</param>
        ''' <param name="p1">First point.</param>
        ''' <param name="k2">Second scalar.</param>
        ''' <param name="p2">Second point.</param>
        ''' <returns>The result k1*P1 + k2*P2.</returns>
        Public Shared Function ShamirMultiply(k1 As BigInteger, p1 As ECPoint, k2 As BigInteger, p2 As ECPoint) As ECPoint
            If p1 Is Nothing Then Throw New ArgumentNullException(NameOf(p1))
            If p2 Is Nothing Then Throw New ArgumentNullException(NameOf(p2))

            ' Precompute P1 + P2
            Dim p1PlusP2 As ECPoint = PointAdd(p1, p2)

            Dim result As ECPoint = ECPoint.Infinity

            ' Get the maximum bit length
            Dim maxBits As Integer = Math.Max(GetBitLength(k1), GetBitLength(k2))

            For i As Integer = maxBits - 1 To 0 Step -1
                result = PointDouble(result)

                Dim bit1 As Boolean = TestBit(k1, i)
                Dim bit2 As Boolean = TestBit(k2, i)

                If bit1 AndAlso bit2 Then
                    result = PointAdd(result, p1PlusP2)
                ElseIf bit1 Then
                    result = PointAdd(result, p1)
                ElseIf bit2 Then
                    result = PointAdd(result, p2)
                End If
            Next

            Return result
        End Function

        ''' <summary>
        ''' Gets the bit length of a BigInteger.
        ''' </summary>
        Private Shared Function GetBitLength(value As BigInteger) As Integer
            Dim bytes() As Byte = value.ToByteArray()
            Dim lastByte As Byte = bytes(bytes.Length - 1)
            Dim bits As Integer = (bytes.Length - 1) * 8

            While lastByte > 0
                bits += 1
                lastByte >>= 1
            End While

            Return bits
        End Function

        ''' <summary>
        ''' Tests whether a specific bit is set in a BigInteger.
        ''' </summary>
        Private Shared Function TestBit(value As BigInteger, bit As Integer) As Boolean
            Return (value >> bit And BigInteger.One) = BigInteger.One
        End Function
    End Class

End Namespace
