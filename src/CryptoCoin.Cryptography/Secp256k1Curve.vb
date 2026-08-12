Imports System
Imports System.Globalization
Imports System.Numerics

Namespace CryptoCoin.Cryptography

    ''' <summary>
    ''' Parameters for the secp256k1 elliptic curve used in CryptoCoin.
    ''' y^2 = x^3 + 7 (mod p)
    ''' </summary>
    Public NotInheritable Class Secp256k1Curve

        Private Sub New()
        End Sub

        ''' <summary>
        ''' The prime field modulus p.
        ''' </summary>
        Public Shared ReadOnly P As BigInteger = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFEFFFFFC2F", Globalization.NumberStyles.HexNumber)

        ''' <summary>
        ''' The order of the generator point n.
        ''' </summary>
        Public Shared ReadOnly N As BigInteger = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFEBAAEDCE6AF48A03BBFD25E8CD0364141", Globalization.NumberStyles.HexNumber)

        ''' <summary>
        ''' Curve coefficient a (= 0 for secp256k1).
        ''' </summary>
        Public Shared ReadOnly A As BigInteger = BigInteger.Zero

        ''' <summary>
        ''' Curve coefficient b (= 7 for secp256k1).
        ''' </summary>
        Public Shared ReadOnly B As New BigInteger(7)

        ''' <summary>
        ''' Generator point X coordinate.
        ''' </summary>
        Public Shared ReadOnly Gx As BigInteger = BigInteger.Parse("079BE667EF9DCBBAC55A06295CE870B07029BFCDB2DCE28D959F2815B16F81798", Globalization.NumberStyles.HexNumber)

        ''' <summary>
        ''' Generator point Y coordinate.
        ''' </summary>
        Public Shared ReadOnly Gy As BigInteger = BigInteger.Parse("0483ADA7726A3C4655DA4FBFC0E1108A8FD17B448A68554199C47D08FFB10D4B8", Globalization.NumberStyles.HexNumber)

        ''' <summary>
        ''' The generator point G.
        ''' </summary>
        Public Shared ReadOnly G As New EcPoint(Gx, Gy)

        ''' <summary>
        ''' The cofactor h (= 1 for secp256k1).
        ''' </summary>
        Public Shared ReadOnly H As BigInteger = BigInteger.One

        ''' <summary>
        ''' Validates that a private key scalar is in the valid range [1, n-1].
        ''' </summary>
        Public Shared Function IsValidPrivateKey(key As BigInteger) As Boolean
            Return key > BigInteger.Zero AndAlso key < N
        End Function

        ''' <summary>
        ''' Validates that a point lies on the curve.
        ''' </summary>
        Public Shared Function IsOnCurve(point As EcPoint) As Boolean
            If point.IsInfinity Then Return True

            ' Check y^2 = x^3 + 7 (mod p)
            Dim left As BigInteger = BigInteger.ModPow(point.Y, 2, P)
            Dim right As BigInteger = (BigInteger.ModPow(point.X, 3, P) + B).PositiveMod(P)
            Return left = right
        End Function

    End Class

End Namespace
