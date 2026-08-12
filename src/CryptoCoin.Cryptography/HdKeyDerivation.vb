Imports System
Imports System.Numerics
Imports System.Text

Namespace CryptoCoin.Cryptography

    ''' <summary>
    ''' Hierarchical Deterministic (HD) key derivation per BIP32.
    ''' Allows deriving child keys from a master key using a path like m/44'/0'/0'/0/0.
    ''' </summary>
    Public Class HdKeyDerivation

        Private Const HardenedOffset As UInteger = &H80000000UI

        ''' <summary>
        ''' Represents an extended key (private or public) with chain code.
        ''' </summary>
        Public Class ExtendedKey
            Public Property KeyData As Byte()
            Public Property ChainCode As Byte()
            Public Property Depth As Byte
            Public Property ParentFingerprint As Byte()
            Public Property ChildIndex As UInteger
            Public Property IsPrivate As Boolean

            Public Sub New()
                ParentFingerprint = New Byte(3) {}
            End Sub

            ''' <summary>
            ''' Gets the key pair if this is a private extended key.
            ''' </summary>
            Public Function GetKeyPair() As KeyPair
                If Not IsPrivate Then
                    Throw New InvalidOperationException("Cannot get private key from public extended key.")
                End If
                Return New KeyPair(KeyData)
            End Function

            ''' <summary>
            ''' Gets the public key point.
            ''' </summary>
            Public Function GetPublicKey() As EcPoint
                If IsPrivate Then
                    Dim kp As New KeyPair(KeyData)
                    Return kp.PublicKey
                Else
                    Return EcPoint.FromBytes(KeyData)
                End If
            End Function

            ''' <summary>
            ''' Computes the fingerprint (first 4 bytes of Hash160 of the public key).
            ''' </summary>
            Public Function GetFingerprint() As Byte()
                Dim pubKeyBytes As Byte()
                If IsPrivate Then
                    Dim kp As New KeyPair(KeyData)
                    pubKeyBytes = kp.CompressedPublicKey
                Else
                    pubKeyBytes = KeyData
                End If

                Dim hash As Byte() = HashUtil.Hash160(pubKeyBytes)
                Dim fp(3) As Byte
                Array.Copy(hash, 0, fp, 0, 4)
                Return fp
            End Function

            ''' <summary>
            ''' Serializes the extended key to Base58Check format (xprv/xpub).
            ''' </summary>
            Public Function Serialize() As String
                Dim data(77) As Byte
                Dim version As Byte()

                If IsPrivate Then
                    version = New Byte() {&H4, &H88, &HAD, &HE4} ' xprv
                Else
                    version = New Byte() {&H4, &H88, &HB2, &H1E} ' xpub
                End If

                Array.Copy(version, 0, data, 0, 4)
                data(4) = Depth
                Array.Copy(ParentFingerprint, 0, data, 5, 4)

                ' Child index (big-endian)
                data(9) = CByte((ChildIndex >> 24) And &HFFUI)
                data(10) = CByte((ChildIndex >> 16) And &HFFUI)
                data(11) = CByte((ChildIndex >> 8) And &HFFUI)
                data(12) = CByte(ChildIndex And &HFFUI)

                Array.Copy(ChainCode, 0, data, 13, 32)

                If IsPrivate Then
                    data(45) = 0 ' Private key prefix
                    Array.Copy(KeyData, 0, data, 46, 32)
                Else
                    Array.Copy(KeyData, 0, data, 45, 33)
                End If

                Return Base58Encoder.EncodeCheck(data)
            End Function
        End Class

        ''' <summary>
        ''' Generates a master key from a seed (typically 64 bytes from BIP39).
        ''' </summary>
        Public Shared Function MasterKeyFromSeed(seed As Byte()) As ExtendedKey
            If seed Is Nothing Then Throw New ArgumentNullException(NameOf(seed))
            If seed.Length < 16 OrElse seed.Length > 64 Then
                Throw New ArgumentException("Seed must be between 16 and 64 bytes.", NameOf(seed))
            End If

            ' HMAC-SHA512 with key "Bitcoin seed" (we use same standard)
            Dim hmacKey As Byte() = Encoding.UTF8.GetBytes("CryptoCoin seed")
            Dim result As Tuple(Of Byte(), Byte()) = HmacSha512.ComputeAndSplit(hmacKey, seed)

            Dim privateKey As BigInteger = FromByteArrayUnsigned(result.Item1)
            If Not Secp256k1Curve.IsValidPrivateKey(privateKey) Then
                Throw New InvalidOperationException("Generated master key is invalid. Try different seed.")
            End If

            Dim key As New ExtendedKey()
            key.KeyData = result.Item1
            key.ChainCode = result.Item2
            key.Depth = 0
            key.ParentFingerprint = New Byte(3) {}
            key.ChildIndex = 0
            key.IsPrivate = True

            Return key
        End Function

        ''' <summary>
        ''' Derives a child key from a parent extended key.
        ''' </summary>
        Public Shared Function DeriveChild(parent As ExtendedKey, index As UInteger) As ExtendedKey
            If parent Is Nothing Then Throw New ArgumentNullException(NameOf(parent))

            Dim isHardened As Boolean = (index >= HardenedOffset)
            Dim data As Byte()

            If isHardened Then
                If Not parent.IsPrivate Then
                    Throw New InvalidOperationException("Cannot derive hardened child from public key.")
                End If
                ' Hardened: 0x00 || private_key || index
                data = New Byte(36) {}
                data(0) = 0
                Array.Copy(parent.KeyData, 0, data, 1, 32)
            Else
                ' Normal: public_key || index
                Dim pubKey As Byte()
                If parent.IsPrivate Then
                    Dim kp As New KeyPair(parent.KeyData)
                    pubKey = kp.CompressedPublicKey
                Else
                    pubKey = parent.KeyData
                End If
                data = New Byte(36) {}
                Array.Copy(pubKey, 0, data, 0, 33)
            End If

            ' Append index (big-endian)
            data(data.Length - 4) = CByte((index >> 24) And &HFFUI)
            data(data.Length - 3) = CByte((index >> 16) And &HFFUI)
            data(data.Length - 2) = CByte((index >> 8) And &HFFUI)
            data(data.Length - 1) = CByte(index And &HFFUI)

            Dim hmacResult As Tuple(Of Byte(), Byte()) = HmacSha512.ComputeAndSplit(parent.ChainCode, data)
            Dim il As BigInteger = FromByteArrayUnsigned(hmacResult.Item1)

            If il >= Secp256k1Curve.N Then
                Throw New InvalidOperationException("Derived key is invalid. Try next index.")
            End If

            Dim child As New ExtendedKey()
            child.ChainCode = hmacResult.Item2
            child.Depth = CByte(parent.Depth + 1)
            child.ParentFingerprint = parent.GetFingerprint()
            child.ChildIndex = index

            If parent.IsPrivate Then
                Dim parentKey As BigInteger = FromByteArrayUnsigned(parent.KeyData)
                Dim childKey As BigInteger = (il + parentKey).PositiveMod(Secp256k1Curve.N)
                If childKey = BigInteger.Zero Then
                    Throw New InvalidOperationException("Derived key is zero. Try next index.")
                End If
                child.KeyData = childKey.ToByteArrayFixed(32)
                child.IsPrivate = True
            Else
                Dim parentPoint As EcPoint = EcPoint.FromBytes(parent.KeyData)
                Dim ilPoint As EcPoint = EcPoint.Multiply(Secp256k1Curve.G, il)
                Dim childPoint As EcPoint = EcPoint.Add(parentPoint, ilPoint)
                If childPoint.IsInfinity Then
                    Throw New InvalidOperationException("Derived public key is infinity. Try next index.")
                End If
                child.KeyData = childPoint.ToCompressedBytes()
                child.IsPrivate = False
            End If

            Return child
        End Function

        ''' <summary>
        ''' Derives a key using a BIP32 path string (e.g., "m/44'/0'/0'/0/0").
        ''' </summary>
        Public Shared Function DerivePath(masterKey As ExtendedKey, path As String) As ExtendedKey
            If masterKey Is Nothing Then Throw New ArgumentNullException(NameOf(masterKey))
            If String.IsNullOrEmpty(path) Then Throw New ArgumentNullException(NameOf(path))

            Dim parts As String() = path.Split("/"c)
            If parts(0) <> "m" AndAlso parts(0) <> "M" Then
                Throw New FormatException("Path must start with 'm' or 'M'.")
            End If

            Dim current As ExtendedKey = masterKey

            For i As Integer = 1 To parts.Length - 1
                Dim part As String = parts(i).Trim()
                Dim hardened As Boolean = part.EndsWith("'") OrElse part.EndsWith("h")

                If hardened Then
                    part = part.Substring(0, part.Length - 1)
                End If

                Dim index As UInteger = UInteger.Parse(part)
                If hardened Then
                    index += HardenedOffset
                End If

                current = DeriveChild(current, index)
            Next

            Return current
        End Function

        ''' <summary>
        ''' Gets the CryptoCoin BIP44 derivation path.
        ''' CryptoCoin uses coin type 999 (imaginary).
        ''' </summary>
        Public Shared Function GetCryptoCoinPath(Optional account As Integer = 0, Optional change As Integer = 0, Optional addressIndex As Integer = 0) As String
            Return $"m/44'/999'/{account}'/{change}/{addressIndex}"
        End Function

    End Class

End Namespace
