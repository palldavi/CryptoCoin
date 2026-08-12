Namespace CryptoCoin.Cryptography

    ''' <summary>
    ''' Encodes and decodes CryptoCoin addresses.
    ''' Address format: Base58Check(version_byte + Hash160(public_key))
    ''' </summary>
    Public NotInheritable Class AddressEncoder

        ''' <summary>
        ''' Version byte for mainnet Pay-to-Public-Key-Hash addresses.
        ''' </summary>
        Public Const MainnetP2PKH As Byte = &H1C ' "C" prefix in Base58

        ''' <summary>
        ''' Version byte for testnet Pay-to-Public-Key-Hash addresses.
        ''' </summary>
        Public Const TestnetP2PKH As Byte = &H6F ' "m" or "n" prefix

        ''' <summary>
        ''' Version byte for mainnet Pay-to-Script-Hash addresses.
        ''' </summary>
        Public Const MainnetP2SH As Byte = &H1D

        ''' <summary>
        ''' Version byte for testnet Pay-to-Script-Hash addresses.
        ''' </summary>
        Public Const TestnetP2SH As Byte = &HC4

        Private Sub New()
        End Sub

        ''' <summary>
        ''' Generates a CryptoCoin address from a public key.
        ''' </summary>
        Public Shared Function FromPublicKey(publicKey As Byte(), Optional version As Byte = MainnetP2PKH) As String
            If publicKey Is Nothing Then Throw New ArgumentNullException(NameOf(publicKey))

            ' Hash160 = RIPEMD160(SHA256(publicKey))
            Dim hash As Byte() = HashUtil.Hash160(publicKey)
            Return FromHash160(hash, version)
        End Function

        ''' <summary>
        ''' Generates a CryptoCoin address from a Hash160 value.
        ''' </summary>
        Public Shared Function FromHash160(hash160 As Byte(), Optional version As Byte = MainnetP2PKH) As String
            If hash160 Is Nothing Then Throw New ArgumentNullException(NameOf(hash160))
            If hash160.Length <> 20 Then Throw New ArgumentException("Hash160 must be 20 bytes.", NameOf(hash160))

            ' Prepend version byte
            Dim versionedPayload(20) As Byte
            versionedPayload(0) = version
            Array.Copy(hash160, 0, versionedPayload, 1, 20)

            Return Base58Encoder.EncodeCheck(versionedPayload)
        End Function

        ''' <summary>
        ''' Generates a CryptoCoin address from a KeyPair (uses compressed public key).
        ''' </summary>
        Public Shared Function FromKeyPair(keyPair As KeyPair, Optional version As Byte = MainnetP2PKH) As String
            If keyPair Is Nothing Then Throw New ArgumentNullException(NameOf(keyPair))
            Return FromPublicKey(keyPair.CompressedPublicKey, version)
        End Function

        ''' <summary>
        ''' Validates a CryptoCoin address format and checksum.
        ''' </summary>
        Public Shared Function IsValid(address As String) As Boolean
            If String.IsNullOrEmpty(address) Then Return False

            Try
                Dim decoded As Byte() = Base58Encoder.DecodeCheck(address)
                ' Must be version byte + 20-byte hash
                Return decoded.Length = 21
            Catch
                Return False
            End Try
        End Function

        ''' <summary>
        ''' Extracts the Hash160 from an address.
        ''' </summary>
        Public Shared Function GetHash160(address As String) As Byte()
            If Not IsValid(address) Then
                Throw New FormatException("Invalid CryptoCoin address.")
            End If

            Dim decoded As Byte() = Base58Encoder.DecodeCheck(address)
            Dim hash(19) As Byte
            Array.Copy(decoded, 1, hash, 0, 20)
            Return hash
        End Function

        ''' <summary>
        ''' Gets the version byte from an address.
        ''' </summary>
        Public Shared Function GetVersion(address As String) As Byte
            If Not IsValid(address) Then
                Throw New FormatException("Invalid CryptoCoin address.")
            End If

            Dim decoded As Byte() = Base58Encoder.DecodeCheck(address)
            Return decoded(0)
        End Function

        ''' <summary>
        ''' Determines if an address is a mainnet address.
        ''' </summary>
        Public Shared Function IsMainnet(address As String) As Boolean
            Dim version As Byte = GetVersion(address)
            Return version = MainnetP2PKH OrElse version = MainnetP2SH
        End Function

        ''' <summary>
        ''' Determines if an address is a P2SH (script hash) address.
        ''' </summary>
        Public Shared Function IsP2SH(address As String) As Boolean
            Dim version As Byte = GetVersion(address)
            Return version = MainnetP2SH OrElse version = TestnetP2SH
        End Function

    End Class

End Namespace
