Imports System
Imports System.Numerics

Namespace CryptoCoin.Cryptography

    ''' <summary>
    ''' Represents a point on the secp256k1 elliptic curve.
    ''' Supports point addition, doubling, and scalar multiplication.
    ''' </summary>
    Public Class EcPoint

        Private ReadOnly _x As BigInteger
        Private ReadOnly _y As BigInteger
        Private ReadOnly _isInfinity As Boolean

        ''' <summary>
        ''' The X coordinate of the point.
        ''' </summary>
        Public ReadOnly Property X As BigInteger
            Get
                Return _x
            End Get
        End Property

        ''' <summary>
        ''' The Y coordinate of the point.
        ''' </summary>
        Public ReadOnly Property Y As BigInteger
            Get
                Return _y
            End Get
        End Property

        ''' <summary>
        ''' Whether this point represents the point at infinity (identity element).
        ''' </summary>
        Public ReadOnly Property IsInfinity As Boolean
            Get
                Return _isInfinity
            End Get
        End Property

        ''' <summary>
        ''' The point at infinity (identity element for point addition).
        ''' </summary>
        Public Shared ReadOnly Infinity As New EcPoint()

        ''' <summary>
        ''' Creates a new point with the given coordinates.
        ''' </summary>
        Public Sub New(x As BigInteger, y As BigInteger)
            _x = x
            _y = y
            _isInfinity = False
        End Sub

        ''' <summary>
        ''' Creates the point at infinity.
        ''' </summary>
        Private Sub New()
            _x = BigInteger.Zero
            _y = BigInteger.Zero
            _isInfinity = True
        End Sub

        ''' <summary>
        ''' Adds two points on the curve.
        ''' </summary>
        Public Shared Function Add(p1 As EcPoint, p2 As EcPoint) As EcPoint
            If p1.IsInfinity Then Return p2
            If p2.IsInfinity Then Return p1

            Dim modP As BigInteger = Secp256k1Curve.P

            If p1.X = p2.X Then
                If p1.Y = p2.Y Then
                    Return PointDouble(p1)
                Else
                    ' Points are inverses of each other
                    Return Infinity
                End If
            End If

            ' slope = (y2 - y1) / (x2 - x1) mod p
            Dim dy As BigInteger = (p2.Y - p1.Y).PositiveMod(modP)
            Dim dx As BigInteger = (p2.X - p1.X).PositiveMod(modP)
            Dim slope As BigInteger = (dy * dx.ModInverse(modP)).PositiveMod(modP)

            ' x3 = slope^2 - x1 - x2 mod p
            Dim x3 As BigInteger = (slope * slope - p1.X - p2.X).PositiveMod(modP)
            ' y3 = slope * (x1 - x3) - y1 mod p
            Dim y3 As BigInteger = (slope * (p1.X - x3) - p1.Y).PositiveMod(modP)

            Return New EcPoint(x3, y3)
        End Function

        ''' <summary>
        ''' Doubles a point on the curve.
        ''' </summary>
        Public Shared Function PointDouble(p As EcPoint) As EcPoint
            If p.IsInfinity Then Return Infinity
            If p.Y = BigInteger.Zero Then Return Infinity

            Dim modP As BigInteger = Secp256k1Curve.P

            ' slope = (3 * x^2 + a) / (2 * y) mod p
            ' For secp256k1, a = 0
            Dim numerator As BigInteger = (3 * BigInteger.ModPow(p.X, 2, modP)).PositiveMod(modP)
            Dim denominator As BigInteger = (2 * p.Y).PositiveMod(modP)
            Dim slope As BigInteger = (numerator * denominator.ModInverse(modP)).PositiveMod(modP)

            ' x3 = slope^2 - 2*x mod p
            Dim x3 As BigInteger = (slope * slope - 2 * p.X).PositiveMod(modP)
            ' y3 = slope * (x - x3) - y mod p
            Dim y3 As BigInteger = (slope * (p.X - x3) - p.Y).PositiveMod(modP)

            Return New EcPoint(x3, y3)
        End Function

        ''' <summary>
        ''' Multiplies a point by a scalar using double-and-add algorithm.
        ''' </summary>
        Public Shared Function Multiply(point As EcPoint, scalar As BigInteger) As EcPoint
            If scalar = BigInteger.Zero OrElse point.IsInfinity Then Return Infinity
            If scalar < BigInteger.Zero Then
                scalar = -scalar
                point = Negate(point)
            End If

            Dim result As EcPoint = Infinity
            Dim addend As EcPoint = point

            While scalar > BigInteger.Zero
                If Not scalar.IsEven Then
                    result = Add(result, addend)
                End If
                addend = PointDouble(addend)
                scalar >>= 1
            End While

            Return result
        End Function

        ''' <summary>
        ''' Negates a point (reflects over x-axis).
        ''' </summary>
        Public Shared Function Negate(p As EcPoint) As EcPoint
            If p.IsInfinity Then Return Infinity
            Return New EcPoint(p.X, (Secp256k1Curve.P - p.Y).PositiveMod(Secp256k1Curve.P))
        End Function

        ''' <summary>
        ''' Serializes the point to compressed format (33 bytes).
        ''' </summary>
        Public Function ToCompressedBytes() As Byte()
            If IsInfinity Then Return New Byte() {0}

            Dim xBytes As Byte() = X.ToByteArrayFixed(32)
            Dim result(32) As Byte

            ' Prefix: 02 if Y is even, 03 if Y is odd
            If Y.IsEven Then
                result(0) = 2
            Else
                result(0) = 3
            End If

            Array.Copy(xBytes, 0, result, 1, 32)
            Return result
        End Function

        ''' <summary>
        ''' Serializes the point to uncompressed format (65 bytes).
        ''' </summary>
        Public Function ToUncompressedBytes() As Byte()
            If IsInfinity Then Return New Byte() {0}

            Dim xBytes As Byte() = X.ToByteArrayFixed(32)
            Dim yBytes As Byte() = Y.ToByteArrayFixed(32)
            Dim result(64) As Byte

            result(0) = 4 ' Uncompressed prefix
            Array.Copy(xBytes, 0, result, 1, 32)
            Array.Copy(yBytes, 0, result, 33, 32)
            Return result
        End Function

        ''' <summary>
        ''' Deserializes a point from compressed or uncompressed format.
        ''' </summary>
        Public Shared Function FromBytes(data As Byte()) As EcPoint
            If data Is Nothing OrElse data.Length = 0 Then
                Throw New ArgumentException("Invalid point data.")
            End If

            If data(0) = 4 AndAlso data.Length = 65 Then
                ' Uncompressed
                Dim xBytes(31) As Byte
                Dim yBytes(31) As Byte
                Array.Copy(data, 1, xBytes, 0, 32)
                Array.Copy(data, 33, yBytes, 0, 32)
                Dim px As BigInteger = FromByteArrayUnsigned(xBytes)
                Dim py As BigInteger = FromByteArrayUnsigned(yBytes)
                Return New EcPoint(px, py)
            ElseIf (data(0) = 2 OrElse data(0) = 3) AndAlso data.Length = 33 Then
                ' Compressed - recover Y from X
                Dim xBytes(31) As Byte
                Array.Copy(data, 1, xBytes, 0, 32)
                Dim px As BigInteger = FromByteArrayUnsigned(xBytes)

                ' y^2 = x^3 + 7 mod p
                Dim ySquared As BigInteger = (BigInteger.ModPow(px, 3, Secp256k1Curve.P) + Secp256k1Curve.B).PositiveMod(Secp256k1Curve.P)

                ' Compute square root using p = 3 (mod 4): y = ySquared^((p+1)/4) mod p
                Dim exponent As BigInteger = (Secp256k1Curve.P + BigInteger.One)
                exponent = BigInteger.Divide(exponent, New BigInteger(4))
                Dim py As BigInteger = BigInteger.ModPow(ySquared, exponent, Secp256k1Curve.P)

                ' Choose correct Y based on prefix
                Dim isOdd As Boolean = Not py.IsEven
                If (data(0) = 2 AndAlso isOdd) OrElse (data(0) = 3 AndAlso Not isOdd) Then
                    py = Secp256k1Curve.P - py
                End If

                Return New EcPoint(px, py)
            Else
                Throw New ArgumentException("Invalid point encoding.")
            End If
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            Dim other As EcPoint = TryCast(obj, EcPoint)
            If other Is Nothing Then Return False
            If IsInfinity AndAlso other.IsInfinity Then Return True
            If IsInfinity OrElse other.IsInfinity Then Return False
            Return X = other.X AndAlso Y = other.Y
        End Function

        Public Overrides Function GetHashCode() As Integer
            If IsInfinity Then Return 0
            Return X.GetHashCode() Xor Y.GetHashCode()
        End Function

        Public Overrides Function ToString() As String
            If IsInfinity Then Return "(Infinity)"
            Return $"({X}, {Y})"
        End Function

    End Class

End Namespace
