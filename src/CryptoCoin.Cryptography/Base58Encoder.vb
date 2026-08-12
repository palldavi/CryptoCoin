Imports System
Imports System.Collections.Generic
Imports System.Numerics
Imports System.Text

Namespace CryptoCoin.Cryptography

    ''' <summary>
    ''' Base58 encoding/decoding used for CryptoCoin addresses.
    ''' Uses the Bitcoin alphabet (no 0, O, I, l to avoid ambiguity).
    ''' </summary>
    Public NotInheritable Class Base58Encoder

        Private Const Alphabet As String = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz"
        Private Shared ReadOnly AlphabetMap As Dictionary(Of Char, Integer)

        Shared Sub New()
            AlphabetMap = New Dictionary(Of Char, Integer)()
            For i As Integer = 0 To Alphabet.Length - 1
                AlphabetMap(Alphabet(i)) = i
            Next
        End Sub

        Private Sub New()
        End Sub

        ''' <summary>
        ''' Encodes a byte array to a Base58 string.
        ''' </summary>
        Public Shared Function Encode(data As Byte()) As String
            If data Is Nothing Then Throw New ArgumentNullException(NameOf(data))
            If data.Length = 0 Then Return String.Empty

            ' Count leading zeros
            Dim leadingZeros As Integer = 0
            For Each b As Byte In data
                If b = 0 Then
                    leadingZeros += 1
                Else
                    Exit For
                End If
            Next

            ' Convert to BigInteger (prepend 0 to ensure positive)
            Dim temp(data.Length) As Byte
            Array.Copy(data, 0, temp, 1, data.Length)
            ' Reverse for BigInteger (little-endian)
            Array.Reverse(temp)
            Dim num As New BigInteger(temp)

            Dim result As New StringBuilder()
            Dim base58 As New BigInteger(58)

            While num > BigInteger.Zero
                Dim remainder As BigInteger = Nothing
                num = BigInteger.DivRem(num, base58, remainder)
                result.Insert(0, Alphabet(CInt(remainder)))
            End While

            ' Add '1' for each leading zero byte
            For i As Integer = 0 To leadingZeros - 1
                result.Insert(0, "1"c)
            Next

            Return result.ToString()
        End Function

        ''' <summary>
        ''' Decodes a Base58 string to a byte array.
        ''' </summary>
        Public Shared Function Decode(encoded As String) As Byte()
            If encoded Is Nothing Then Throw New ArgumentNullException(NameOf(encoded))
            If encoded.Length = 0 Then Return New Byte() {}

            ' Count leading '1's (representing zero bytes)
            Dim leadingOnes As Integer = 0
            For Each c As Char In encoded
                If c = "1"c Then
                    leadingOnes += 1
                Else
                    Exit For
                End If
            Next

            ' Convert from Base58 to BigInteger
            Dim num As BigInteger = BigInteger.Zero
            Dim base58 As New BigInteger(58)

            For Each c As Char In encoded
                If Not AlphabetMap.ContainsKey(c) Then
                    Throw New FormatException($"Invalid Base58 character: '{c}'")
                End If
                num = BigInteger.Multiply(num, base58) + New BigInteger(AlphabetMap(c))
            Next

            ' Convert BigInteger to byte array
            Dim bytes As Byte() = num.ToByteArray()
            ' Remove trailing zero if present (sign byte)
            If bytes.Length > 1 AndAlso bytes(bytes.Length - 1) = 0 Then
                Dim trimmed(bytes.Length - 2) As Byte
                Array.Copy(bytes, trimmed, trimmed.Length)
                bytes = trimmed
            End If
            ' Reverse to big-endian
            Array.Reverse(bytes)

            ' Prepend leading zero bytes
            Dim result(leadingOnes + bytes.Length - 1) As Byte
            Array.Copy(bytes, 0, result, leadingOnes, bytes.Length)

            Return result
        End Function

        ''' <summary>
        ''' Encodes data with a 4-byte checksum appended (Base58Check).
        ''' </summary>
        Public Shared Function EncodeCheck(data As Byte()) As String
            If data Is Nothing Then Throw New ArgumentNullException(NameOf(data))

            Dim checksum As Byte() = HashUtil.Checksum(data)
            Dim dataWithChecksum(data.Length + 3) As Byte
            Array.Copy(data, dataWithChecksum, data.Length)
            Array.Copy(checksum, 0, dataWithChecksum, data.Length, 4)

            Return Encode(dataWithChecksum)
        End Function

        ''' <summary>
        ''' Decodes a Base58Check string and verifies the checksum.
        ''' </summary>
        Public Shared Function DecodeCheck(encoded As String) As Byte()
            Dim decoded As Byte() = Decode(encoded)
            If decoded.Length < 4 Then
                Throw New FormatException("Base58Check string too short.")
            End If

            Dim dataLength As Integer = decoded.Length - 4
            Dim data(dataLength - 1) As Byte
            Dim checksum(3) As Byte
            Array.Copy(decoded, data, dataLength)
            Array.Copy(decoded, dataLength, checksum, 0, 4)

            Dim expectedChecksum As Byte() = HashUtil.Checksum(data)
            If Not HashUtil.ConstantTimeEquals(checksum, expectedChecksum) Then
                Throw New FormatException("Base58Check checksum mismatch.")
            End If

            Return data
        End Function

    End Class

End Namespace
