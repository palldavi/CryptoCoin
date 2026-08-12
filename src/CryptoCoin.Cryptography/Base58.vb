Imports System
Imports System.Numerics
Imports System.Security.Cryptography
Imports System.Text

Namespace CryptoCoin.Cryptography

    ''' <summary>
    ''' Provides Base58 and Base58Check encoding and decoding.
    ''' Base58 is a binary-to-text encoding used in Bitcoin and CryptoCoin addresses.
    ''' It uses an alphabet that avoids visually ambiguous characters (0, O, I, l).
    ''' </summary>
    Public NotInheritable Class Base58Encoder

        ''' <summary>
        ''' The Base58 alphabet used for encoding.
        ''' Excludes 0 (zero), O (uppercase o), I (uppercase i), and l (lowercase L).
        ''' </summary>
        Public Const Alphabet As String = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz"

        Private Shared ReadOnly AlphabetChars() As Char = Alphabet.ToCharArray()
        Private Shared ReadOnly Base As New BigInteger(58)
        Private Shared ReadOnly CharToIndex(127) As Integer

        Shared Sub New()
            ' Initialize reverse lookup table
            For i As Integer = 0 To 127
                CharToIndex(i) = -1
            Next
            For i As Integer = 0 To Alphabet.Length - 1
                CharToIndex(AscW(Alphabet(i))) = i
            Next
        End Sub

        Private Sub New()
            ' Static class - prevent instantiation
        End Sub

        ''' <summary>
        ''' Encodes a byte array to a Base58 string.
        ''' Leading zero bytes are encoded as '1' characters.
        ''' </summary>
        ''' <param name="data">The bytes to encode.</param>
        ''' <returns>The Base58-encoded string.</returns>
        ''' <exception cref="ArgumentNullException">Thrown when data is Nothing.</exception>
        Public Shared Function Encode(data() As Byte) As String
            If data Is Nothing Then
                Throw New ArgumentNullException(NameOf(data), "Data cannot be Nothing.")
            End If

            If data.Length = 0 Then
                Return String.Empty
            End If

            ' Count leading zeros
            Dim leadingZeros As Integer = 0
            For Each b As Byte In data
                If b <> 0 Then Exit For
                leadingZeros += 1
            Next

            ' Convert byte array to BigInteger (big-endian, unsigned)
            ' Add a zero byte at the end (becomes MSB after reverse) to ensure positive
            Dim padded(data.Length) As Byte
            Array.Copy(data, padded, data.Length)
            Array.Reverse(padded) ' Convert to little-endian for BigInteger
            Dim value As New BigInteger(padded)

            ' Convert to Base58
            Dim result As New StringBuilder()
            While value > BigInteger.Zero
                Dim remainder As BigInteger = BigInteger.Zero
                value = BigInteger.DivRem(value, Base, remainder)
                result.Insert(0, AlphabetChars(CInt(remainder)))
            End While

            ' Add leading '1' characters for each leading zero byte
            For i As Integer = 0 To leadingZeros - 1
                result.Insert(0, "1"c)
            Next

            Return result.ToString()
        End Function

        ''' <summary>
        ''' Decodes a Base58 string to a byte array.
        ''' Leading '1' characters are decoded as zero bytes.
        ''' </summary>
        ''' <param name="encoded">The Base58-encoded string.</param>
        ''' <returns>The decoded byte array.</returns>
        ''' <exception cref="ArgumentException">Thrown when the string contains invalid characters.</exception>
        Public Shared Function Decode(encoded As String) As Byte()
            If encoded Is Nothing Then
                Throw New ArgumentNullException(NameOf(encoded), "Encoded string cannot be Nothing.")
            End If

            If encoded.Length = 0 Then
                Return New Byte() {}
            End If

            ' Count leading '1' characters (represent zero bytes)
            Dim leadingOnes As Integer = 0
            For Each c As Char In encoded
                If c <> "1"c Then Exit For
                leadingOnes += 1
            Next

            ' Convert from Base58 to BigInteger
            Dim value As BigInteger = BigInteger.Zero
            For Each c As Char In encoded
                Dim charIndex As Integer = GetCharIndex(c)
                If charIndex < 0 Then
                    Throw New ArgumentException($"Invalid Base58 character: '{c}'.", NameOf(encoded))
                End If
                value = value * Base + New BigInteger(charIndex)
            Next

            ' Convert BigInteger to byte array (big-endian)
            Dim bytes() As Byte = value.ToByteArray() ' Little-endian
            Array.Reverse(bytes) ' Convert to big-endian

            ' Remove leading zero byte (sign byte from BigInteger)
            Dim startIndex As Integer = 0
            While startIndex < bytes.Length AndAlso bytes(startIndex) = 0
                startIndex += 1
            End While

            ' Construct result with leading zeros
            Dim result(leadingOnes + bytes.Length - startIndex - 1) As Byte
            ' Leading zeros are already zero in the array
            If bytes.Length - startIndex > 0 Then
                Array.Copy(bytes, startIndex, result, leadingOnes, bytes.Length - startIndex)
            End If

            Return result
        End Function

        ''' <summary>
        ''' Encodes data with a Base58Check checksum (4-byte double-SHA256 suffix).
        ''' </summary>
        ''' <param name="payload">The payload bytes (including version byte).</param>
        ''' <returns>The Base58Check-encoded string.</returns>
        ''' <exception cref="ArgumentNullException">Thrown when payload is Nothing.</exception>
        Public Shared Function EncodeWithChecksum(payload() As Byte) As String
            If payload Is Nothing Then
                Throw New ArgumentNullException(NameOf(payload), "Payload cannot be Nothing.")
            End If

            ' Compute checksum (first 4 bytes of double SHA-256)
            Dim checksum() As Byte = ComputeChecksum(payload)

            ' Append checksum to payload
            Dim dataWithChecksum(payload.Length + 3) As Byte
            Array.Copy(payload, dataWithChecksum, payload.Length)
            Array.Copy(checksum, 0, dataWithChecksum, payload.Length, 4)

            Return Encode(dataWithChecksum)
        End Function

        ''' <summary>
        ''' Encodes data with a version byte and Base58Check checksum.
        ''' </summary>
        ''' <param name="version">The version byte prefix.</param>
        ''' <param name="data">The data bytes.</param>
        ''' <returns>The Base58Check-encoded string.</returns>
        Public Shared Function EncodeWithVersionAndChecksum(version As Byte, data() As Byte) As String
            If data Is Nothing Then
                Throw New ArgumentNullException(NameOf(data))
            End If

            Dim payload(data.Length) As Byte
            payload(0) = version
            Array.Copy(data, 0, payload, 1, data.Length)

            Return EncodeWithChecksum(payload)
        End Function

        ''' <summary>
        ''' Decodes a Base58Check-encoded string, verifying the checksum.
        ''' Returns the payload (including version byte) without the checksum.
        ''' </summary>
        ''' <param name="encoded">The Base58Check-encoded string.</param>
        ''' <returns>The decoded payload bytes (including version byte).</returns>
        ''' <exception cref="ArgumentException">Thrown when the checksum is invalid.</exception>
        Public Shared Function DecodeWithChecksum(encoded As String) As Byte()
            If String.IsNullOrEmpty(encoded) Then
                Throw New ArgumentException("Encoded string cannot be null or empty.", NameOf(encoded))
            End If

            Dim decoded() As Byte = Decode(encoded)

            If decoded.Length < 5 Then
                Throw New ArgumentException("Base58Check data too short (minimum 5 bytes: 1 version + 4 checksum).", NameOf(encoded))
            End If

            ' Split into payload and checksum
            Dim payload(decoded.Length - 5) As Byte
            Dim checksum(3) As Byte
            Array.Copy(decoded, payload, payload.Length)
            Array.Copy(decoded, payload.Length, checksum, 0, 4)

            ' Verify checksum
            Dim expectedChecksum() As Byte = ComputeChecksum(payload)
            If Not ConstantTimeEquals(checksum, expectedChecksum) Then
                Throw New ArgumentException("Invalid Base58Check checksum.", NameOf(encoded))
            End If

            Return payload
        End Function

        ''' <summary>
        ''' Decodes a Base58Check-encoded string and separates the version byte from the data.
        ''' </summary>
        ''' <param name="encoded">The Base58Check-encoded string.</param>
        ''' <param name="version">Output: the version byte.</param>
        ''' <returns>The data bytes (without version byte or checksum).</returns>
        Public Shared Function DecodeWithVersionAndChecksum(encoded As String, ByRef version As Byte) As Byte()
            Dim payload() As Byte = DecodeWithChecksum(encoded)

            If payload.Length < 1 Then
                Throw New ArgumentException("Decoded payload is empty.", NameOf(encoded))
            End If

            version = payload(0)
            Dim data(payload.Length - 2) As Byte
            Array.Copy(payload, 1, data, 0, data.Length)
            Return data
        End Function

        ''' <summary>
        ''' Validates a Base58Check-encoded string without throwing exceptions.
        ''' </summary>
        ''' <param name="encoded">The string to validate.</param>
        ''' <returns>True if the string is valid Base58Check; otherwise, False.</returns>
        Public Shared Function IsValidBase58Check(encoded As String) As Boolean
            If String.IsNullOrEmpty(encoded) Then Return False

            Try
                DecodeWithChecksum(encoded)
                Return True
            Catch
                Return False
            End Try
        End Function

        ''' <summary>
        ''' Validates that a string contains only valid Base58 characters.
        ''' </summary>
        ''' <param name="value">The string to validate.</param>
        ''' <returns>True if all characters are valid Base58.</returns>
        Public Shared Function IsValidBase58(value As String) As Boolean
            If String.IsNullOrEmpty(value) Then Return False

            For Each c As Char In value
                If GetCharIndex(c) < 0 Then Return False
            Next

            Return True
        End Function

        ''' <summary>
        ''' Computes the 4-byte checksum for Base58Check encoding.
        ''' The checksum is the first 4 bytes of the double SHA-256 hash.
        ''' </summary>
        ''' <param name="data">The data to compute the checksum for.</param>
        ''' <returns>A 4-byte checksum.</returns>
        Public Shared Function ComputeChecksum(data() As Byte) As Byte()
            Dim hash As Hash = HashAlgorithms.DoubleSha256(data)
            Dim checksum(3) As Byte
            Array.Copy(hash.Bytes, checksum, 4)
            Return checksum
        End Function

        ''' <summary>
        ''' Gets the index of a character in the Base58 alphabet.
        ''' </summary>
        ''' <param name="c">The character to look up.</param>
        ''' <returns>The index (0-57) or -1 if invalid.</returns>
        Private Shared Function GetCharIndex(c As Char) As Integer
            Dim code As Integer = AscW(c)
            If code > 127 Then Return -1
            Return CharToIndex(code)
        End Function

        ''' <summary>
        ''' Constant-time comparison of two byte arrays.
        ''' </summary>
        Private Shared Function ConstantTimeEquals(a() As Byte, b() As Byte) As Boolean
            If a.Length <> b.Length Then Return False
            Dim result As Integer = 0
            For i As Integer = 0 To a.Length - 1
                result = result Or (CInt(a(i)) Xor CInt(b(i)))
            Next
            Return result = 0
        End Function
    End Class

End Namespace
