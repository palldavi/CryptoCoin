Imports System
Imports System.IO
Imports System.Numerics
Imports System.Runtime.CompilerServices
Imports System.Security.Cryptography
Imports System.Text

Namespace CryptoCoin.Cryptography

    ''' <summary>
    ''' Represents an immutable cryptographic hash value with equality comparison,
    ''' hex encoding, and constant-time comparison support.
    ''' </summary>
    Public NotInheritable Class Hash
        Implements IEquatable(Of Hash), IComparable(Of Hash)

        Private ReadOnly _bytes() As Byte

        ''' <summary>
        ''' Gets the length of the hash in bytes.
        ''' </summary>
        Public ReadOnly Property Length As Integer
            Get
                Return _bytes.Length
            End Get
        End Property

        ''' <summary>
        ''' Gets the raw bytes of the hash value.
        ''' </summary>
        ''' <returns>A copy of the internal byte array.</returns>
        Public ReadOnly Property Bytes As Byte()
            Get
                Dim copy(_bytes.Length - 1) As Byte
                Array.Copy(_bytes, copy, _bytes.Length)
                Return copy
            End Get
        End Property

        ''' <summary>
        ''' Creates a new Hash instance from the specified byte array.
        ''' </summary>
        ''' <param name="data">The hash bytes.</param>
        ''' <exception cref="ArgumentNullException">Thrown when data is Nothing.</exception>
        Public Sub New(data() As Byte)
            If data Is Nothing Then
                Throw New ArgumentNullException(NameOf(data), "Hash data cannot be Nothing.")
            End If
            _bytes = New Byte(data.Length - 1) {}
            Array.Copy(data, _bytes, data.Length)
        End Sub

        ''' <summary>
        ''' Creates a Hash instance from a hexadecimal string.
        ''' </summary>
        ''' <param name="hex">The hexadecimal string representation of the hash.</param>
        ''' <returns>A new Hash instance.</returns>
        ''' <exception cref="ArgumentException">Thrown when hex is invalid.</exception>
        Public Shared Function FromHex(hex As String) As Hash
            If String.IsNullOrEmpty(hex) Then
                Throw New ArgumentException("Hex string cannot be null or empty.", NameOf(hex))
            End If
            Return New Hash(HexEncoder.Decode(hex))
        End Function

        ''' <summary>
        ''' Returns the hash as a hexadecimal string.
        ''' </summary>
        Public Overrides Function ToString() As String
            Return HexEncoder.Encode(_bytes)
        End Function

        ''' <summary>
        ''' Returns the hash as a reversed hexadecimal string (common in block explorers).
        ''' </summary>
        Public Function ToReversedString() As String
            Dim reversed(_bytes.Length - 1) As Byte
            For i As Integer = 0 To _bytes.Length - 1
                reversed(i) = _bytes(_bytes.Length - 1 - i)
            Next
            Return HexEncoder.Encode(reversed)
        End Function

        ''' <summary>
        ''' Determines whether this hash equals another hash using constant-time comparison.
        ''' </summary>
        Public Overloads Function Equals(other As Hash) As Boolean Implements IEquatable(Of Hash).Equals
            If other Is Nothing Then Return False
            Return HashComparer.ConstantTimeEquals(_bytes, other._bytes)
        End Function

        ''' <summary>
        ''' Determines whether this hash equals another object.
        ''' </summary>
        Public Overrides Function Equals(obj As Object) As Boolean
            Return Equals(TryCast(obj, Hash))
        End Function

        ''' <summary>
        ''' Returns a hash code for this instance.
        ''' </summary>
        Public Overrides Function GetHashCode() As Integer
            If _bytes.Length < 4 Then Return 0
            Return BitConverter.ToInt32(_bytes, 0)
        End Function

        ''' <summary>
        ''' Compares this hash to another hash lexicographically.
        ''' </summary>
        Public Function CompareTo(other As Hash) As Integer Implements IComparable(Of Hash).CompareTo
            If other Is Nothing Then Return 1
            Dim minLen As Integer = Math.Min(_bytes.Length, other._bytes.Length)
            For i As Integer = 0 To minLen - 1
                Dim cmp As Integer = _bytes(i).CompareTo(other._bytes(i))
                If cmp <> 0 Then Return cmp
            Next
            Return _bytes.Length.CompareTo(other._bytes.Length)
        End Function

        ''' <summary>
        ''' Gets a zero-filled hash of the specified length.
        ''' </summary>
        Public Shared Function Zero(length As Integer) As Hash
            Return New Hash(New Byte(length - 1) {})
        End Function

        Public Shared Operator =(left As Hash, right As Hash) As Boolean
            If left Is Nothing Then Return right Is Nothing
            Return left.Equals(right)
        End Operator

        Public Shared Operator <>(left As Hash, right As Hash) As Boolean
            Return Not (left = right)
        End Operator

        Public Shared Operator <(left As Hash, right As Hash) As Boolean
            If left Is Nothing Then Return right IsNot Nothing
            Return left.CompareTo(right) < 0
        End Operator

        Public Shared Operator >(left As Hash, right As Hash) As Boolean
            If left Is Nothing Then Return False
            Return left.CompareTo(right) > 0
        End Operator
    End Class

    ''' <summary>
    ''' Provides cryptographic hashing utilities for the CryptoCoin cryptocurrency.
    ''' Includes SHA-256 double hash, RIPEMD-160, Hash160, SHA-512, and HMAC-SHA512.
    ''' </summary>
    Public NotInheritable Class HashAlgorithms

        Private Sub New()
            ' Static class - prevent instantiation
        End Sub

        ''' <summary>
        ''' Computes a single SHA-256 hash of the input data.
        ''' </summary>
        ''' <param name="data">The data to hash.</param>
        ''' <returns>A 32-byte SHA-256 hash.</returns>
        ''' <exception cref="ArgumentNullException">Thrown when data is Nothing.</exception>
        Public Shared Function Sha256(data() As Byte) As Hash
            If data Is Nothing Then
                Throw New ArgumentNullException(NameOf(data), "Input data cannot be Nothing.")
            End If

            Try
                Using hasher As SHA256 = SHA256.Create()
                    Return New Hash(hasher.ComputeHash(data))
                End Using
            Catch ex As CryptographicException
                Throw New InvalidOperationException("SHA-256 computation failed.", ex)
            End Try
        End Function

        ''' <summary>
        ''' Computes a single SHA-256 hash of the input stream.
        ''' </summary>
        ''' <param name="stream">The stream to hash.</param>
        ''' <returns>A 32-byte SHA-256 hash.</returns>
        Public Shared Function Sha256(stream As Stream) As Hash
            If stream Is Nothing Then
                Throw New ArgumentNullException(NameOf(stream), "Input stream cannot be Nothing.")
            End If

            Try
                Using hasher As SHA256 = SHA256.Create()
                    Return New Hash(hasher.ComputeHash(stream))
                End Using
            Catch ex As CryptographicException
                Throw New InvalidOperationException("SHA-256 stream computation failed.", ex)
            End Try
        End Function

        ''' <summary>
        ''' Computes a double SHA-256 hash (SHA256d) of the input data.
        ''' This is the primary hash function used in Bitcoin-style cryptocurrencies
        ''' for block headers, transaction IDs, and proof-of-work.
        ''' </summary>
        ''' <param name="data">The data to hash.</param>
        ''' <returns>A 32-byte double SHA-256 hash.</returns>
        ''' <exception cref="ArgumentNullException">Thrown when data is Nothing.</exception>
        Public Shared Function DoubleSha256(data() As Byte) As Hash
            If data Is Nothing Then
                Throw New ArgumentNullException(NameOf(data), "Input data cannot be Nothing.")
            End If

            Try
                Using hasher As SHA256 = SHA256.Create()
                    Dim firstHash() As Byte = hasher.ComputeHash(data)
                    Return New Hash(hasher.ComputeHash(firstHash))
                End Using
            Catch ex As CryptographicException
                Throw New InvalidOperationException("Double SHA-256 computation failed.", ex)
            End Try
        End Function

        ''' <summary>
        ''' Computes a double SHA-256 hash of two concatenated byte arrays.
        ''' Useful for Merkle tree computation without allocating a combined array.
        ''' </summary>
        ''' <param name="first">The first byte array.</param>
        ''' <param name="second">The second byte array.</param>
        ''' <returns>A 32-byte double SHA-256 hash of the concatenation.</returns>
        Public Shared Function DoubleSha256(first() As Byte, second() As Byte) As Hash
            If first Is Nothing Then
                Throw New ArgumentNullException(NameOf(first))
            End If
            If second Is Nothing Then
                Throw New ArgumentNullException(NameOf(second))
            End If

            Dim combined(first.Length + second.Length - 1) As Byte
            Array.Copy(first, 0, combined, 0, first.Length)
            Array.Copy(second, 0, combined, first.Length, second.Length)
            Return DoubleSha256(combined)
        End Function

        ''' <summary>
        ''' Computes a RIPEMD-160 hash of the input data.
        ''' Used in address generation for shorter hash output.
        ''' </summary>
        ''' <param name="data">The data to hash.</param>
        ''' <returns>A 20-byte RIPEMD-160 hash.</returns>
        ''' <exception cref="ArgumentNullException">Thrown when data is Nothing.</exception>
        Public Shared Function Ripemd160(data() As Byte) As Hash
            If data Is Nothing Then
                Throw New ArgumentNullException(NameOf(data), "Input data cannot be Nothing.")
            End If

            Try
                Using hasher As RIPEMD160 = RIPEMD160.Create()
                    Return New Hash(hasher.ComputeHash(data))
                End Using
            Catch ex As CryptographicException
                Throw New InvalidOperationException("RIPEMD-160 computation failed.", ex)
            End Try
        End Function

        ''' <summary>
        ''' Computes Hash160 (SHA-256 followed by RIPEMD-160) of the input data.
        ''' This is the standard hash used for generating CryptoCoin addresses from public keys.
        ''' </summary>
        ''' <param name="data">The data to hash (typically a public key).</param>
        ''' <returns>A 20-byte Hash160 result.</returns>
        ''' <exception cref="ArgumentNullException">Thrown when data is Nothing.</exception>
        Public Shared Function Hash160(data() As Byte) As Hash
            If data Is Nothing Then
                Throw New ArgumentNullException(NameOf(data), "Input data cannot be Nothing.")
            End If

            Try
                Dim sha256Result() As Byte
                Using sha As SHA256 = SHA256.Create()
                    sha256Result = sha.ComputeHash(data)
                End Using

                Using ripemd As RIPEMD160 = RIPEMD160.Create()
                    Return New Hash(ripemd.ComputeHash(sha256Result))
                End Using
            Catch ex As CryptographicException
                Throw New InvalidOperationException("Hash160 computation failed.", ex)
            End Try
        End Function

        ''' <summary>
        ''' Computes a SHA-512 hash of the input data.
        ''' Used in key derivation and HMAC operations.
        ''' </summary>
        ''' <param name="data">The data to hash.</param>
        ''' <returns>A 64-byte SHA-512 hash.</returns>
        ''' <exception cref="ArgumentNullException">Thrown when data is Nothing.</exception>
        Public Shared Function Sha512(data() As Byte) As Hash
            If data Is Nothing Then
                Throw New ArgumentNullException(NameOf(data), "Input data cannot be Nothing.")
            End If

            Try
                Using hasher As SHA512 = SHA512.Create()
                    Return New Hash(hasher.ComputeHash(data))
                End Using
            Catch ex As CryptographicException
                Throw New InvalidOperationException("SHA-512 computation failed.", ex)
            End Try
        End Function

        ''' <summary>
        ''' Computes HMAC-SHA512 of the input data with the specified key.
        ''' Used in BIP32 hierarchical deterministic key derivation.
        ''' </summary>
        ''' <param name="key">The HMAC key.</param>
        ''' <param name="data">The data to authenticate.</param>
        ''' <returns>A 64-byte HMAC-SHA512 result.</returns>
        ''' <exception cref="ArgumentNullException">Thrown when key or data is Nothing.</exception>
        Public Shared Function HmacSha512(key() As Byte, data() As Byte) As Hash
            If key Is Nothing Then
                Throw New ArgumentNullException(NameOf(key), "HMAC key cannot be Nothing.")
            End If
            If data Is Nothing Then
                Throw New ArgumentNullException(NameOf(data), "Input data cannot be Nothing.")
            End If

            Try
                Using hmac As New HMACSHA512(key)
                    Return New Hash(hmac.ComputeHash(data))
                End Using
            Catch ex As CryptographicException
                Throw New InvalidOperationException("HMAC-SHA512 computation failed.", ex)
            End Try
        End Function

        ''' <summary>
        ''' Computes HMAC-SHA256 of the input data with the specified key.
        ''' Used in various authentication and derivation schemes.
        ''' </summary>
        ''' <param name="key">The HMAC key.</param>
        ''' <param name="data">The data to authenticate.</param>
        ''' <returns>A 32-byte HMAC-SHA256 result.</returns>
        Public Shared Function HmacSha256(key() As Byte, data() As Byte) As Hash
            If key Is Nothing Then
                Throw New ArgumentNullException(NameOf(key), "HMAC key cannot be Nothing.")
            End If
            If data Is Nothing Then
                Throw New ArgumentNullException(NameOf(data), "Input data cannot be Nothing.")
            End If

            Try
                Using hmac As New HMACSHA256(key)
                    Return New Hash(hmac.ComputeHash(data))
                End Using
            Catch ex As CryptographicException
                Throw New InvalidOperationException("HMAC-SHA256 computation failed.", ex)
            End Try
        End Function

        ''' <summary>
        ''' Computes the Merkle root from a list of transaction hashes.
        ''' Uses double SHA-256 for combining pairs, duplicating the last element if odd count.
        ''' </summary>
        ''' <param name="hashes">The list of transaction hashes.</param>
        ''' <returns>The Merkle root hash.</returns>
        Public Shared Function ComputeMerkleRoot(hashes As IList(Of Hash)) As Hash
            If hashes Is Nothing Then
                Throw New ArgumentNullException(NameOf(hashes))
            End If
            If hashes.Count = 0 Then
                Return Hash.Zero(32)
            End If
            If hashes.Count = 1 Then
                Return hashes(0)
            End If

            Dim currentLevel As New List(Of Hash)(hashes)

            While currentLevel.Count > 1
                Dim nextLevel As New List(Of Hash)()

                ' If odd number of hashes, duplicate the last one
                If currentLevel.Count Mod 2 <> 0 Then
                    currentLevel.Add(currentLevel(currentLevel.Count - 1))
                End If

                For i As Integer = 0 To currentLevel.Count - 1 Step 2
                    Dim combined = DoubleSha256(currentLevel(i).Bytes, currentLevel(i + 1).Bytes)
                    nextLevel.Add(combined)
                Next

                currentLevel = nextLevel
            End While

            Return currentLevel(0)
        End Function

        ''' <summary>
        ''' Computes a tagged hash as defined in BIP340 (Schnorr signatures).
        ''' TaggedHash(tag, msg) = SHA256(SHA256(tag) || SHA256(tag) || msg)
        ''' </summary>
        ''' <param name="tag">The tag string.</param>
        ''' <param name="message">The message to hash.</param>
        ''' <returns>A 32-byte tagged hash.</returns>
        Public Shared Function TaggedHash(tag As String, message() As Byte) As Hash
            If String.IsNullOrEmpty(tag) Then
                Throw New ArgumentException("Tag cannot be null or empty.", NameOf(tag))
            End If
            If message Is Nothing Then
                Throw New ArgumentNullException(NameOf(message))
            End If

            Dim tagBytes() As Byte = Encoding.UTF8.GetBytes(tag)
            Dim tagHash() As Byte = Sha256(tagBytes).Bytes

            Dim combined(tagHash.Length + tagHash.Length + message.Length - 1) As Byte
            Array.Copy(tagHash, 0, combined, 0, tagHash.Length)
            Array.Copy(tagHash, 0, combined, tagHash.Length, tagHash.Length)
            Array.Copy(message, 0, combined, tagHash.Length * 2, message.Length)

            Return Sha256(combined)
        End Function
    End Class

    ''' <summary>
    ''' Provides constant-time hash comparison utilities to prevent timing attacks.
    ''' </summary>
    Public NotInheritable Class HashComparer

        Private Sub New()
        End Sub

        ''' <summary>
        ''' Compares two byte arrays in constant time to prevent timing attacks.
        ''' The comparison always examines all bytes regardless of where differences occur.
        ''' </summary>
        ''' <param name="a">The first byte array.</param>
        ''' <param name="b">The second byte array.</param>
        ''' <returns>True if the arrays are equal; otherwise, False.</returns>
        <MethodImpl(MethodImplOptions.NoInlining Or MethodImplOptions.NoOptimization)>
        Public Shared Function ConstantTimeEquals(a() As Byte, b() As Byte) As Boolean
            If a Is Nothing AndAlso b Is Nothing Then Return True
            If a Is Nothing OrElse b Is Nothing Then Return False
            If a.Length <> b.Length Then Return False

            Dim result As Integer = 0
            For i As Integer = 0 To a.Length - 1
                result = result Or (CInt(a(i)) Xor CInt(b(i)))
            Next

            Return result = 0
        End Function

        ''' <summary>
        ''' Compares two Hash instances in constant time.
        ''' </summary>
        ''' <param name="a">The first hash.</param>
        ''' <param name="b">The second hash.</param>
        ''' <returns>True if the hashes are equal; otherwise, False.</returns>
        Public Shared Function ConstantTimeEquals(a As Hash, b As Hash) As Boolean
            If a Is Nothing AndAlso b Is Nothing Then Return True
            If a Is Nothing OrElse b Is Nothing Then Return False
            Return ConstantTimeEquals(a.Bytes, b.Bytes)
        End Function

        ''' <summary>
        ''' Verifies that a hash meets a difficulty target (hash &lt;= target).
        ''' Used in proof-of-work validation.
        ''' </summary>
        ''' <param name="hash">The hash to check.</param>
        ''' <param name="target">The difficulty target.</param>
        ''' <returns>True if the hash meets the target difficulty.</returns>
        Public Shared Function MeetsDifficulty(hash As Hash, target As Hash) As Boolean
            If hash Is Nothing Then
                Throw New ArgumentNullException(NameOf(hash))
            End If
            If target Is Nothing Then
                Throw New ArgumentNullException(NameOf(target))
            End If

            Return hash.CompareTo(target) <= 0
        End Function

        ''' <summary>
        ''' Counts the number of leading zero bits in a hash.
        ''' Useful for difficulty estimation.
        ''' </summary>
        ''' <param name="hash">The hash to examine.</param>
        ''' <returns>The number of leading zero bits.</returns>
        Public Shared Function CountLeadingZeroBits(hash As Hash) As Integer
            If hash Is Nothing Then
                Throw New ArgumentNullException(NameOf(hash))
            End If

            Dim bits As Integer = 0
            Dim hashBytes() As Byte = hash.Bytes

            For Each b As Byte In hashBytes
                If b = 0 Then
                    bits += 8
                Else
                    ' Count leading zeros in this byte
                    Dim mask As Integer = &H80
                    While (CInt(b) And mask) = 0
                        bits += 1
                        mask >>= 1
                    End While
                    Exit For
                End If
            Next

            Return bits
        End Function
    End Class

    ''' <summary>
    ''' Provides hexadecimal encoding and decoding utilities.
    ''' </summary>
    Public NotInheritable Class HexEncoder

        Private Shared ReadOnly HexChars() As Char = "0123456789abcdef".ToCharArray()
        Private Shared ReadOnly HexLookup(255) As Integer

        Shared Sub New()
            ' Initialize lookup table
            For i As Integer = 0 To 255
                HexLookup(i) = -1
            Next
            For i As Integer = 0 To 9
                HexLookup(Asc("0"c) + i) = i
            Next
            For i As Integer = 0 To 5
                HexLookup(Asc("a"c) + i) = 10 + i
                HexLookup(Asc("A"c) + i) = 10 + i
            Next
        End Sub

        Private Sub New()
        End Sub

        ''' <summary>
        ''' Encodes a byte array to a lowercase hexadecimal string.
        ''' </summary>
        ''' <param name="data">The bytes to encode.</param>
        ''' <returns>A lowercase hex string.</returns>
        ''' <exception cref="ArgumentNullException">Thrown when data is Nothing.</exception>
        Public Shared Function Encode(data() As Byte) As String
            If data Is Nothing Then
                Throw New ArgumentNullException(NameOf(data), "Data cannot be Nothing.")
            End If

            Dim result As New StringBuilder(data.Length * 2)
            For Each b As Byte In data
                result.Append(HexChars(b >> 4))
                result.Append(HexChars(b And &HF))
            Next
            Return result.ToString()
        End Function

        ''' <summary>
        ''' Encodes a byte array to an uppercase hexadecimal string.
        ''' </summary>
        ''' <param name="data">The bytes to encode.</param>
        ''' <returns>An uppercase hex string.</returns>
        Public Shared Function EncodeUpper(data() As Byte) As String
            If data Is Nothing Then
                Throw New ArgumentNullException(NameOf(data))
            End If
            Return Encode(data).ToUpperInvariant()
        End Function

        ''' <summary>
        ''' Decodes a hexadecimal string to a byte array.
        ''' </summary>
        ''' <param name="hex">The hex string to decode.</param>
        ''' <returns>The decoded byte array.</returns>
        ''' <exception cref="ArgumentException">Thrown when hex is invalid.</exception>
        Public Shared Function Decode(hex As String) As Byte()
            If hex Is Nothing Then
                Throw New ArgumentNullException(NameOf(hex), "Hex string cannot be Nothing.")
            End If

            ' Remove optional 0x prefix
            If hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase) Then
                hex = hex.Substring(2)
            End If

            If hex.Length Mod 2 <> 0 Then
                Throw New ArgumentException("Hex string must have an even number of characters.", NameOf(hex))
            End If

            Dim result(hex.Length \ 2 - 1) As Byte
            For i As Integer = 0 To result.Length - 1
                Dim highNibble As Integer = GetHexValue(hex(i * 2))
                Dim lowNibble As Integer = GetHexValue(hex(i * 2 + 1))

                If highNibble < 0 OrElse lowNibble < 0 Then
                    Throw New ArgumentException($"Invalid hex character at position {i * 2}.", NameOf(hex))
                End If

                result(i) = CByte((highNibble << 4) Or lowNibble)
            Next

            Return result
        End Function

        ''' <summary>
        ''' Attempts to decode a hexadecimal string, returning success or failure.
        ''' </summary>
        ''' <param name="hex">The hex string to decode.</param>
        ''' <param name="result">The decoded bytes if successful.</param>
        ''' <returns>True if decoding succeeded; otherwise, False.</returns>
        Public Shared Function TryDecode(hex As String, ByRef result() As Byte) As Boolean
            Try
                result = Decode(hex)
                Return True
            Catch ex As Exception
                result = Nothing
                Return False
            End Try
        End Function

        ''' <summary>
        ''' Validates whether a string is a valid hexadecimal string.
        ''' </summary>
        ''' <param name="hex">The string to validate.</param>
        ''' <returns>True if the string is valid hex; otherwise, False.</returns>
        Public Shared Function IsValidHex(hex As String) As Boolean
            If String.IsNullOrEmpty(hex) Then Return False

            Dim startIndex As Integer = 0
            If hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase) Then
                startIndex = 2
            End If

            If (hex.Length - startIndex) Mod 2 <> 0 Then Return False

            For i As Integer = startIndex To hex.Length - 1
                If GetHexValue(hex(i)) < 0 Then Return False
            Next

            Return True
        End Function

        ''' <summary>
        ''' Reverses the byte order of a hex string (for display purposes).
        ''' </summary>
        ''' <param name="hex">The hex string to reverse.</param>
        ''' <returns>The reversed hex string.</returns>
        Public Shared Function ReverseHex(hex As String) As String
            Dim bytes() As Byte = Decode(hex)
            Array.Reverse(bytes)
            Return Encode(bytes)
        End Function

        Private Shared Function GetHexValue(c As Char) As Integer
            Dim index As Integer = AscW(c)
            If index > 255 Then Return -1
            Return HexLookup(index)
        End Function
    End Class

End Namespace
