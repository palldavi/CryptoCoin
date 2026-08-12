Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Numerics
Imports System.Security.Cryptography
Imports System.Text

Namespace CryptoCoin.Cryptography

    ''' <summary>
    ''' Represents an extended key (public or private) in the BIP32 hierarchical deterministic wallet scheme.
    ''' Contains the key material, chain code, depth, parent fingerprint, and child index.
    ''' </summary>
    Public NotInheritable Class ExtendedKey
        Implements IDisposable

        Private _keyData() As Byte
        Private _disposed As Boolean

        ''' <summary>
        ''' Gets the chain code (32 bytes) used for child key derivation.
        ''' </summary>
        Public ReadOnly Property ChainCode As Byte()

        ''' <summary>
        ''' Gets the depth of this key in the derivation hierarchy (0 for master).
        ''' </summary>
        Public ReadOnly Property Depth As Byte

        ''' <summary>
        ''' Gets the fingerprint of the parent key (first 4 bytes of Hash160 of parent public key).
        ''' </summary>
        Public ReadOnly Property ParentFingerprint As Byte()

        ''' <summary>
        ''' Gets the child index used to derive this key from its parent.
        ''' </summary>
        Public ReadOnly Property ChildIndex As UInteger

        ''' <summary>
        ''' Gets whether this is a private extended key (xprv) or public extended key (xpub).
        ''' </summary>
        Public ReadOnly Property IsPrivate As Boolean

        ''' <summary>
        ''' Gets the network this key belongs to.
        ''' </summary>
        Public ReadOnly Property Network As NetworkType

        ''' <summary>
        ''' Gets the key data bytes.
        ''' For private keys: 32-byte private key.
        ''' For public keys: 33-byte compressed public key.
        ''' </summary>
        Public ReadOnly Property KeyData As Byte()
            Get
                ThrowIfDisposed()
                Dim copy(_keyData.Length - 1) As Byte
                Array.Copy(_keyData, copy, _keyData.Length)
                Return copy
            End Get
        End Property

        ''' <summary>
        ''' Gets the private key as a BigInteger (only valid for private extended keys).
        ''' </summary>
        ''' <exception cref="InvalidOperationException">Thrown when this is a public extended key.</exception>
        Public ReadOnly Property PrivateKey As BigInteger
            Get
                ThrowIfDisposed()
                If Not IsPrivate Then
                    Throw New InvalidOperationException("Cannot get private key from a public extended key.")
                End If
                Dim padded(_keyData.Length) As Byte
                Array.Copy(_keyData, padded, _keyData.Length)
                Array.Reverse(padded)
                Return New BigInteger(padded)
            End Get
        End Property

        ''' <summary>
        ''' Gets the compressed public key bytes (derived from private key if this is a private extended key).
        ''' </summary>
        Public ReadOnly Property PublicKeyBytes As Byte()
            Get
                ThrowIfDisposed()
                If Not IsPrivate Then
                    Return KeyData
                End If
                ' Derive public key from private key
                Dim pubPoint As ECPoint = Secp256k1.GeneratorMultiply(PrivateKey)
                Return pubPoint.ToCompressedBytes()
            End Get
        End Property

        ''' <summary>
        ''' Gets the key fingerprint (first 4 bytes of Hash160 of the public key).
        ''' </summary>
        Public ReadOnly Property Fingerprint As Byte()
            Get
                Dim hash160() As Byte = HashAlgorithms.Hash160(PublicKeyBytes).Bytes
                Dim fp(3) As Byte
                Array.Copy(hash160, fp, 4)
                Return fp
            End Get
        End Property

        ' Version bytes for serialization
        Private Const MainnetPrivateVersion As UInteger = &H0488ADE4UI ' xprv
        Private Const MainnetPublicVersion As UInteger = &H0488B21EUI  ' xpub
        Private Const TestnetPrivateVersion As UInteger = &H04358394UI ' tprv
        Private Const TestnetPublicVersion As UInteger = &H043587CFUI  ' tpub

        ''' <summary>
        ''' Creates a new ExtendedKey instance.
        ''' </summary>
        Public Sub New(keyData() As Byte, chainCode() As Byte, depth As Byte,
                       parentFingerprint() As Byte, childIndex As UInteger,
                       isPrivate As Boolean, Optional network As NetworkType = NetworkType.Mainnet)
            If keyData Is Nothing Then Throw New ArgumentNullException(NameOf(keyData))
            If chainCode Is Nothing Then Throw New ArgumentNullException(NameOf(chainCode))
            If chainCode.Length <> 32 Then Throw New ArgumentException("Chain code must be 32 bytes.", NameOf(chainCode))
            If parentFingerprint Is Nothing Then Throw New ArgumentNullException(NameOf(parentFingerprint))
            If parentFingerprint.Length <> 4 Then Throw New ArgumentException("Parent fingerprint must be 4 bytes.", NameOf(parentFingerprint))

            _keyData = New Byte(keyData.Length - 1) {}
            Array.Copy(keyData, _keyData, keyData.Length)

            Me.ChainCode = New Byte(31) {}
            Array.Copy(chainCode, Me.ChainCode, 32)

            Me.Depth = depth
            Me.ParentFingerprint = New Byte(3) {}
            Array.Copy(parentFingerprint, Me.ParentFingerprint, 4)
            Me.ChildIndex = childIndex
            Me.IsPrivate = isPrivate
            Me.Network = network
        End Sub

        ''' <summary>
        ''' Converts this private extended key to its corresponding public extended key.
        ''' </summary>
        ''' <returns>The public extended key (xpub).</returns>
        ''' <exception cref="InvalidOperationException">Thrown when this is already a public key.</exception>
        Public Function ToPublic() As ExtendedKey
            ThrowIfDisposed()
            If Not IsPrivate Then
                Throw New InvalidOperationException("Key is already public.")
            End If

            Return New ExtendedKey(PublicKeyBytes, ChainCode, Depth, ParentFingerprint, ChildIndex, False, Network)
        End Function

        ''' <summary>
        ''' Serializes the extended key to Base58Check format (xprv/xpub/tprv/tpub).
        ''' </summary>
        ''' <returns>The serialized extended key string.</returns>
        Public Function Serialize() As String
            ThrowIfDisposed()

            Dim version As UInteger
            If IsPrivate Then
                version = If(Network = NetworkType.Mainnet, MainnetPrivateVersion, TestnetPrivateVersion)
            Else
                version = If(Network = NetworkType.Mainnet, MainnetPublicVersion, TestnetPublicVersion)
            End If

            Using ms As New MemoryStream()
                ' Version (4 bytes, big-endian)
                Dim versionBytes() As Byte = BitConverter.GetBytes(version)
                Array.Reverse(versionBytes)
                ms.Write(versionBytes, 0, 4)

                ' Depth (1 byte)
                ms.WriteByte(Depth)

                ' Parent fingerprint (4 bytes)
                ms.Write(ParentFingerprint, 0, 4)

                ' Child index (4 bytes, big-endian)
                Dim indexBytes() As Byte = BitConverter.GetBytes(ChildIndex)
                Array.Reverse(indexBytes)
                ms.Write(indexBytes, 0, 4)

                ' Chain code (32 bytes)
                ms.Write(ChainCode, 0, 32)

                ' Key data (33 bytes)
                If IsPrivate Then
                    ms.WriteByte(0) ' Private key prefix
                    ms.Write(_keyData, 0, 32)
                Else
                    ms.Write(_keyData, 0, 33)
                End If

                Return Base58Encoder.EncodeWithChecksum(ms.ToArray())
            End Using
        End Function

        ''' <summary>
        ''' Deserializes an extended key from Base58Check format.
        ''' </summary>
        ''' <param name="serialized">The serialized extended key string (xprv/xpub/tprv/tpub).</param>
        ''' <returns>The deserialized extended key.</returns>
        Public Shared Function Deserialize(serialized As String) As ExtendedKey
            If String.IsNullOrEmpty(serialized) Then
                Throw New ArgumentException("Serialized key cannot be null or empty.", NameOf(serialized))
            End If

            Dim data() As Byte = Base58Encoder.DecodeWithChecksum(serialized)
            If data.Length <> 78 Then
                Throw New ArgumentException("Invalid extended key length.", NameOf(serialized))
            End If

            ' Parse version (4 bytes, big-endian)
            Dim versionBytes(3) As Byte
            Array.Copy(data, versionBytes, 4)
            Array.Reverse(versionBytes)
            Dim version As UInteger = BitConverter.ToUInt32(versionBytes, 0)

            Dim isPrivate As Boolean
            Dim network As NetworkType

            Select Case version
                Case MainnetPrivateVersion
                    isPrivate = True
                    network = NetworkType.Mainnet
                Case MainnetPublicVersion
                    isPrivate = False
                    network = NetworkType.Mainnet
                Case TestnetPrivateVersion
                    isPrivate = True
                    network = NetworkType.Testnet
                Case TestnetPublicVersion
                    isPrivate = False
                    network = NetworkType.Testnet
                Case Else
                    Throw New ArgumentException($"Unknown extended key version: 0x{version:X8}.", NameOf(serialized))
            End Select

            ' Parse depth
            Dim depth As Byte = data(4)

            ' Parse parent fingerprint
            Dim parentFingerprint(3) As Byte
            Array.Copy(data, 5, parentFingerprint, 0, 4)

            ' Parse child index (big-endian)
            Dim indexBytes(3) As Byte
            Array.Copy(data, 9, indexBytes, 0, 4)
            Array.Reverse(indexBytes)
            Dim childIndex As UInteger = BitConverter.ToUInt32(indexBytes, 0)

            ' Parse chain code
            Dim chainCode(31) As Byte
            Array.Copy(data, 13, chainCode, 0, 32)

            ' Parse key data
            Dim keyData() As Byte
            If isPrivate Then
                If data(45) <> 0 Then
                    Throw New ArgumentException("Invalid private key prefix.", NameOf(serialized))
                End If
                keyData = New Byte(31) {}
                Array.Copy(data, 46, keyData, 0, 32)
            Else
                keyData = New Byte(32) {}
                Array.Copy(data, 45, keyData, 0, 33)
            End If

            Return New ExtendedKey(keyData, chainCode, depth, parentFingerprint, childIndex, isPrivate, network)
        End Function

        ''' <summary>
        ''' Returns a string representation of the extended key.
        ''' </summary>
        Public Overrides Function ToString() As String
            If _disposed Then Return "(disposed)"
            Return Serialize()
        End Function

        Private Sub ThrowIfDisposed()
            If _disposed Then
                Throw New ObjectDisposedException(NameOf(ExtendedKey))
            End If
        End Sub

        ''' <summary>
        ''' Securely disposes of the key material.
        ''' </summary>
        Public Sub Dispose() Implements IDisposable.Dispose
            If Not _disposed Then
                If _keyData IsNot Nothing Then
                    Array.Clear(_keyData, 0, _keyData.Length)
                    _keyData = Nothing
                End If
                _disposed = True
            End If
        End Sub
    End Class

    ''' <summary>
    ''' Represents a parsed BIP32 derivation path (e.g., m/44'/0'/0'/0/0).
    ''' </summary>
    Public NotInheritable Class DerivationPath

        ''' <summary>
        ''' Gets the individual path components.
        ''' </summary>
        Public ReadOnly Property Components As IReadOnlyList(Of DerivationPathComponent)

        ''' <summary>
        ''' Gets the string representation of the path.
        ''' </summary>
        Public ReadOnly Property Path As String

        ''' <summary>
        ''' Gets the depth (number of components) in the path.
        ''' </summary>
        Public ReadOnly Property Depth As Integer
            Get
                Return Components.Count
            End Get
        End Property

        ''' <summary>
        ''' Creates a derivation path from components.
        ''' </summary>
        Public Sub New(components As IList(Of DerivationPathComponent))
            Me.Components = New List(Of DerivationPathComponent)(components).AsReadOnly()
            Me.Path = BuildPathString()
        End Sub

        ''' <summary>
        ''' Parses a derivation path string (e.g., "m/44'/0'/0'/0/0").
        ''' </summary>
        ''' <param name="path">The path string to parse.</param>
        ''' <returns>The parsed derivation path.</returns>
        ''' <exception cref="ArgumentException">Thrown when the path is invalid.</exception>
        Public Shared Function Parse(path As String) As DerivationPath
            If String.IsNullOrEmpty(path) Then
                Throw New ArgumentException("Path cannot be null or empty.", NameOf(path))
            End If

            ' Remove leading "m/" or "M/"
            Dim normalizedPath As String = path.Trim()
            If normalizedPath.StartsWith("m/", StringComparison.OrdinalIgnoreCase) Then
                normalizedPath = normalizedPath.Substring(2)
            ElseIf normalizedPath = "m" OrElse normalizedPath = "M" Then
                Return New DerivationPath(New List(Of DerivationPathComponent)())
            End If

            Dim parts() As String = normalizedPath.Split("/"c)
            Dim components As New List(Of DerivationPathComponent)()

            For Each part In parts
                If String.IsNullOrWhiteSpace(part) Then Continue For

                Dim hardened As Boolean = part.EndsWith("'") OrElse part.EndsWith("h", StringComparison.OrdinalIgnoreCase)
                Dim indexStr As String = part.TrimEnd("'"c, "h"c, "H"c)

                Dim index As UInteger
                If Not UInteger.TryParse(indexStr, index) Then
                    Throw New ArgumentException($"Invalid path component: '{part}'.", NameOf(path))
                End If

                If index >= &H80000000UI Then
                    Throw New ArgumentException($"Index too large: {index}.", NameOf(path))
                End If

                components.Add(New DerivationPathComponent(index, hardened))
            Next

            Return New DerivationPath(components)
        End Function

        ''' <summary>
        ''' Creates the standard BIP44 path for CryptoCoin: m/44'/0'/account'/change/index.
        ''' </summary>
        Public Shared Function Bip44(account As UInteger, change As UInteger, index As UInteger) As DerivationPath
            Return Parse($"m/44'/0'/{account}'/{change}/{index}")
        End Function

        ''' <summary>
        ''' Creates the standard BIP84 path for native SegWit: m/84'/0'/account'/change/index.
        ''' </summary>
        Public Shared Function Bip84(account As UInteger, change As UInteger, index As UInteger) As DerivationPath
            Return Parse($"m/84'/0'/{account}'/{change}/{index}")
        End Function

        Private Function BuildPathString() As String
            If Components.Count = 0 Then Return "m"
            Dim sb As New StringBuilder("m")
            For Each component In Components
                sb.Append("/")
                sb.Append(component.ToString())
            Next
            Return sb.ToString()
        End Function

        Public Overrides Function ToString() As String
            Return Path
        End Function
    End Class

    ''' <summary>
    ''' Represents a single component in a BIP32 derivation path.
    ''' </summary>
    Public NotInheritable Class DerivationPathComponent

        ''' <summary>
        ''' Gets the child index (without the hardened flag).
        ''' </summary>
        Public ReadOnly Property Index As UInteger

        ''' <summary>
        ''' Gets whether this is a hardened derivation.
        ''' </summary>
        Public ReadOnly Property IsHardened As Boolean

        ''' <summary>
        ''' Gets the full child index including the hardened flag bit.
        ''' </summary>
        Public ReadOnly Property FullIndex As UInteger
            Get
                If IsHardened Then
                    Return Index Or &H80000000UI
                End If
                Return Index
            End Get
        End Property

        ''' <summary>
        ''' The hardened offset value (2^31).
        ''' </summary>
        Public Const HardenedOffset As UInteger = &H80000000UI

        Public Sub New(index As UInteger, isHardened As Boolean)
            Me.Index = index
            Me.IsHardened = isHardened
        End Sub

        Public Overrides Function ToString() As String
            If IsHardened Then
                Return $"{Index}'"
            End If
            Return Index.ToString()
        End Function
    End Class

    ''' <summary>
    ''' Implements BIP32 hierarchical deterministic key derivation for the CryptoCoin wallet system.
    ''' Supports master key generation from seed, child key derivation (hardened and normal),
    ''' and derivation path navigation.
    ''' </summary>
    Public NotInheritable Class KeyDerivation

        Private Sub New()
        End Sub

        ''' <summary>
        ''' The HMAC key used for master key generation ("CryptoCoin seed").
        ''' </summary>
        Private Shared ReadOnly MasterKeyHmacKey() As Byte = Encoding.UTF8.GetBytes("CryptoCoin seed")

        ''' <summary>
        ''' Generates a master extended private key from a seed.
        ''' </summary>
        ''' <param name="seed">The seed bytes (typically 16-64 bytes, commonly 64 from BIP39).</param>
        ''' <param name="network">The target network.</param>
        ''' <returns>The master extended private key.</returns>
        ''' <exception cref="ArgumentException">Thrown when the seed is invalid.</exception>
        Public Shared Function GenerateMasterKey(seed() As Byte, Optional network As NetworkType = NetworkType.Mainnet) As ExtendedKey
            If seed Is Nothing Then
                Throw New ArgumentNullException(NameOf(seed))
            End If
            If seed.Length < 16 OrElse seed.Length > 64 Then
                Throw New ArgumentException("Seed must be between 16 and 64 bytes.", NameOf(seed))
            End If

            ' HMAC-SHA512 with key "CryptoCoin seed"
            Dim hmacResult() As Byte = HashAlgorithms.HmacSha512(MasterKeyHmacKey, seed).Bytes

            ' Split into private key (left 32 bytes) and chain code (right 32 bytes)
            Dim privateKey(31) As Byte
            Dim chainCode(31) As Byte
            Array.Copy(hmacResult, 0, privateKey, 0, 32)
            Array.Copy(hmacResult, 32, chainCode, 0, 32)

            ' Validate the private key
            Dim padded(32) As Byte
            Array.Copy(privateKey, padded, 32)
            Array.Reverse(padded)
            Dim keyValue As New BigInteger(padded)

            If Not Secp256k1.IsValidPrivateKey(keyValue) Then
                Throw New CryptographicException("Generated master key is invalid. Try a different seed.")
            End If

            Return New ExtendedKey(
                privateKey,
                chainCode,
                depth:=0,
                parentFingerprint:=New Byte(3) {},
                childIndex:=0,
                isPrivate:=True,
                network:=network
            )
        End Function

        ''' <summary>
        ''' Derives a child key from a parent extended key.
        ''' </summary>
        ''' <param name="parent">The parent extended key.</param>
        ''' <param name="index">The child index (use values >= 0x80000000 for hardened).</param>
        ''' <returns>The derived child extended key.</returns>
        ''' <exception cref="InvalidOperationException">Thrown when hardened derivation is attempted on a public key.</exception>
        Public Shared Function DeriveChild(parent As ExtendedKey, index As UInteger) As ExtendedKey
            If parent Is Nothing Then
                Throw New ArgumentNullException(NameOf(parent))
            End If

            Dim isHardened As Boolean = (index >= &H80000000UI)

            If isHardened AndAlso Not parent.IsPrivate Then
                Throw New InvalidOperationException("Cannot perform hardened derivation from a public extended key.")
            End If

            ' Prepare HMAC data
            Dim data() As Byte
            If isHardened Then
                ' Hardened: HMAC-SHA512(Key = chainCode, Data = 0x00 || privateKey || index)
                data = New Byte(36) {}
                data(0) = 0
                Array.Copy(parent.KeyData, 0, data, 1, 32)
                Dim indexBytes() As Byte = BitConverter.GetBytes(index)
                Array.Reverse(indexBytes)
                Array.Copy(indexBytes, 0, data, 33, 4)
            Else
                ' Normal: HMAC-SHA512(Key = chainCode, Data = publicKey || index)
                Dim pubKey() As Byte = parent.PublicKeyBytes
                data = New Byte(36) {}
                Array.Copy(pubKey, 0, data, 0, 33)
                Dim indexBytes() As Byte = BitConverter.GetBytes(index)
                Array.Reverse(indexBytes)
                Array.Copy(indexBytes, 0, data, 33, 4)
            End If

            ' Compute HMAC-SHA512
            Dim hmacResult() As Byte = HashAlgorithms.HmacSha512(parent.ChainCode, data).Bytes

            ' Split result
            Dim il(31) As Byte
            Dim ir(31) As Byte
            Array.Copy(hmacResult, 0, il, 0, 32)
            Array.Copy(hmacResult, 32, ir, 0, 32)

            ' Convert IL to BigInteger
            Dim ilPadded(32) As Byte
            Array.Copy(il, ilPadded, 32)
            Array.Reverse(ilPadded)
            Dim ilValue As New BigInteger(ilPadded)

            ' Check IL is valid
            If ilValue >= Secp256k1.N Then
                Throw New CryptographicException("Derived key is invalid (IL >= N). Try next index.")
            End If

            Dim childKeyData() As Byte
            Dim childIsPrivate As Boolean = parent.IsPrivate

            If parent.IsPrivate Then
                ' Child private key = (IL + parent private key) mod N
                Dim parentKeyValue As BigInteger = parent.PrivateKey
                Dim childKeyValue As BigInteger = (ilValue + parentKeyValue) Mod Secp256k1.N

                If childKeyValue = BigInteger.Zero Then
                    Throw New CryptographicException("Derived key is zero. Try next index.")
                End If

                childKeyData = BigIntegerToFixedBytes(childKeyValue, 32)
            Else
                ' Child public key = point(IL) + parent public key
                Dim ilPoint As ECPoint = Secp256k1.GeneratorMultiply(ilValue)
                Dim parentPoint As ECPoint = Secp256k1.ParsePublicKey(parent.KeyData)
                Dim childPoint As ECPoint = Secp256k1.PointAdd(ilPoint, parentPoint)

                If childPoint.IsInfinity Then
                    Throw New CryptographicException("Derived public key is at infinity. Try next index.")
                End If

                childKeyData = childPoint.ToCompressedBytes()
            End If

            Return New ExtendedKey(
                childKeyData,
                ir,
                depth:=CByte(parent.Depth + 1),
                parentFingerprint:=parent.Fingerprint,
                childIndex:=index,
                isPrivate:=childIsPrivate,
                network:=parent.Network
            )
        End Function

        ''' <summary>
        ''' Derives a child key using a derivation path.
        ''' </summary>
        ''' <param name="masterKey">The master (or parent) extended key.</param>
        ''' <param name="path">The derivation path.</param>
        ''' <returns>The derived extended key.</returns>
        Public Shared Function DerivePath(masterKey As ExtendedKey, path As DerivationPath) As ExtendedKey
            If masterKey Is Nothing Then
                Throw New ArgumentNullException(NameOf(masterKey))
            End If
            If path Is Nothing Then
                Throw New ArgumentNullException(NameOf(path))
            End If

            Dim current As ExtendedKey = masterKey
            For Each component In path.Components
                current = DeriveChild(current, component.FullIndex)
            Next

            Return current
        End Function

        ''' <summary>
        ''' Derives a child key using a path string (e.g., "m/44'/0'/0'/0/0").
        ''' </summary>
        ''' <param name="masterKey">The master extended key.</param>
        ''' <param name="pathString">The derivation path string.</param>
        ''' <returns>The derived extended key.</returns>
        Public Shared Function DerivePath(masterKey As ExtendedKey, pathString As String) As ExtendedKey
            Dim path As DerivationPath = DerivationPath.Parse(pathString)
            Return DerivePath(masterKey, path)
        End Function

        ''' <summary>
        ''' Derives multiple child keys from a parent at sequential indices.
        ''' Useful for generating a batch of receiving addresses.
        ''' </summary>
        ''' <param name="parent">The parent extended key.</param>
        ''' <param name="startIndex">The starting child index.</param>
        ''' <param name="count">The number of keys to derive.</param>
        ''' <returns>A list of derived extended keys.</returns>
        Public Shared Function DeriveRange(parent As ExtendedKey, startIndex As UInteger, count As Integer) As IList(Of ExtendedKey)
            If parent Is Nothing Then
                Throw New ArgumentNullException(NameOf(parent))
            End If
            If count < 0 Then
                Throw New ArgumentOutOfRangeException(NameOf(count), "Count must be non-negative.")
            End If

            Dim results As New List(Of ExtendedKey)(count)
            For i As Integer = 0 To count - 1
                results.Add(DeriveChild(parent, CUInt(CLng(startIndex) + i)))
            Next
            Return results
        End Function

        ''' <summary>
        ''' Converts a BigInteger to a fixed-length big-endian byte array.
        ''' </summary>
        Private Shared Function BigIntegerToFixedBytes(value As BigInteger, length As Integer) As Byte()
            Dim bytes() As Byte = value.ToByteArray()
            Dim result(length - 1) As Byte
            Dim copyLen As Integer = Math.Min(bytes.Length, length)
            If bytes.Length > length AndAlso bytes(bytes.Length - 1) = 0 Then
                copyLen = length
            End If
            Array.Copy(bytes, 0, result, 0, Math.Min(copyLen, length))
            Array.Reverse(result)
            Return result
        End Function
    End Class

End Namespace
