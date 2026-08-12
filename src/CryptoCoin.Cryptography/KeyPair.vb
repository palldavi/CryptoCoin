Imports System
Imports System.Numerics

Namespace CryptoCoin.Cryptography

    ''' <summary>
    ''' Represents an ECDSA key pair on the secp256k1 curve.
    ''' Contains a private key (scalar) and the corresponding public key (point).
    ''' </summary>
    Public Class KeyPair

        ''' <summary>
        ''' The private key as a 32-byte array.
        ''' </summary>
        Public ReadOnly Property PrivateKeyBytes As Byte()

        ''' <summary>
        ''' The private key as a BigInteger.
        ''' </summary>
        Public ReadOnly Property PrivateKey As BigInteger

        ''' <summary>
        ''' The public key point on the curve.
        ''' </summary>
        Public ReadOnly Property PublicKey As EcPoint

        ''' <summary>
        ''' The compressed public key (33 bytes).
        ''' </summary>
        Public ReadOnly Property CompressedPublicKey As Byte()
            Get
                Return PublicKey.ToCompressedBytes()
            End Get
        End Property

        ''' <summary>
        ''' The uncompressed public key (65 bytes).
        ''' </summary>
        Public ReadOnly Property UncompressedPublicKey As Byte()
            Get
                Return PublicKey.ToUncompressedBytes()
            End Get
        End Property

        ''' <summary>
        ''' Creates a new random key pair.
        ''' </summary>
        Public Sub New()
            ' Generate random private key in valid range
            Do
                _PrivateKeyBytes = SecureRandom.Generate256Bits()
                _PrivateKey = FromByteArrayUnsigned(_PrivateKeyBytes)
            Loop While Not Secp256k1Curve.IsValidPrivateKey(_PrivateKey)

            ' Compute public key = privateKey * G
            _PublicKey = EcPoint.Multiply(Secp256k1Curve.G, _PrivateKey)
        End Sub

        ''' <summary>
        ''' Creates a key pair from an existing private key.
        ''' </summary>
        Public Sub New(privateKeyBytes As Byte())
            If privateKeyBytes Is Nothing Then Throw New ArgumentNullException(NameOf(privateKeyBytes))
            If privateKeyBytes.Length <> 32 Then Throw New ArgumentException("Private key must be 32 bytes.", NameOf(privateKeyBytes))

            _PrivateKeyBytes = CType(privateKeyBytes.Clone(), Byte())
            _PrivateKey = FromByteArrayUnsigned(_PrivateKeyBytes)

            If Not Secp256k1Curve.IsValidPrivateKey(_PrivateKey) Then
                Throw New ArgumentException("Private key is not in valid range [1, n-1].", NameOf(privateKeyBytes))
            End If

            _PublicKey = EcPoint.Multiply(Secp256k1Curve.G, _PrivateKey)
        End Sub

        ''' <summary>
        ''' Creates a key pair from a BigInteger private key.
        ''' </summary>
        Public Sub New(privateKey As BigInteger)
            If Not Secp256k1Curve.IsValidPrivateKey(privateKey) Then
                Throw New ArgumentException("Private key is not in valid range [1, n-1].", NameOf(privateKey))
            End If

            _PrivateKey = privateKey
            _PrivateKeyBytes = privateKey.ToByteArrayFixed(32)
            _PublicKey = EcPoint.Multiply(Secp256k1Curve.G, _PrivateKey)
        End Sub

        ''' <summary>
        ''' Creates a key pair from a hex-encoded private key string.
        ''' </summary>
        Public Shared Function FromHex(hexPrivateKey As String) As KeyPair
            If String.IsNullOrEmpty(hexPrivateKey) Then Throw New ArgumentNullException(NameOf(hexPrivateKey))
            Dim bytes As Byte() = HashUtil.FromHex(hexPrivateKey)
            Return New KeyPair(bytes)
        End Function

        ''' <summary>
        ''' Gets the private key as a hex string.
        ''' </summary>
        Public Function ToHex() As String
            Return HashUtil.ToHex(_PrivateKeyBytes)
        End Function

        ''' <summary>
        ''' Exports the private key in Wallet Import Format (WIF).
        ''' </summary>
        Public Function ToWif(Optional compressed As Boolean = True, Optional testnet As Boolean = False) As String
            Dim prefix As Byte = If(testnet, CByte(&HEF), CByte(&H80))
            Dim data As Byte()

            If compressed Then
                ReDim data(33)
                data(0) = prefix
                Array.Copy(_PrivateKeyBytes, 0, data, 1, 32)
                data(33) = 1 ' Compression flag
            Else
                ReDim data(32)
                data(0) = prefix
                Array.Copy(_PrivateKeyBytes, 0, data, 1, 32)
            End If

            Return Base58Encoder.EncodeCheck(data)
        End Function

        ''' <summary>
        ''' Imports a private key from Wallet Import Format (WIF).
        ''' </summary>
        Public Shared Function FromWif(wif As String) As KeyPair
            If String.IsNullOrEmpty(wif) Then Throw New ArgumentNullException(NameOf(wif))

            Dim decoded As Byte() = Base58Encoder.DecodeCheck(wif)
            If decoded.Length = 34 AndAlso decoded(33) = 1 Then
                ' Compressed
                Dim keyBytes(31) As Byte
                Array.Copy(decoded, 1, keyBytes, 0, 32)
                Return New KeyPair(keyBytes)
            ElseIf decoded.Length = 33 Then
                ' Uncompressed
                Dim keyBytes(31) As Byte
                Array.Copy(decoded, 1, keyBytes, 0, 32)
                Return New KeyPair(keyBytes)
            Else
                Throw New FormatException("Invalid WIF format.")
            End If
        End Function

        Public Overrides Function ToString() As String
            Return $"KeyPair(PublicKey={HashUtil.ToHex(CompressedPublicKey)})"
        End Function

    End Class

End Namespace
