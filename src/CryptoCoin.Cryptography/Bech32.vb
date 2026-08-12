Imports System
Imports System.Collections.Generic
Imports System.Text

Namespace CryptoCoin.Cryptography

    ''' <summary>
    ''' Specifies the Bech32 encoding variant.
    ''' </summary>
    Public Enum Bech32Encoding
        ''' <summary>
        ''' Original Bech32 encoding (BIP173) for witness version 0.
        ''' </summary>
        Bech32 = 1

        ''' <summary>
        ''' Bech32m encoding (BIP350) for witness versions 1+.
        ''' </summary>
        Bech32m = 2
    End Enum

    ''' <summary>
    ''' Represents a decoded Bech32 address with its components.
    ''' </summary>
    Public NotInheritable Class Bech32Data

        ''' <summary>
        ''' Gets the human-readable part (HRP) of the address.
        ''' </summary>
        Public ReadOnly Property Hrp As String

        ''' <summary>
        ''' Gets the witness version (0-16).
        ''' </summary>
        Public ReadOnly Property WitnessVersion As Integer

        ''' <summary>
        ''' Gets the witness program data bytes.
        ''' </summary>
        Public ReadOnly Property Data As Byte()

        ''' <summary>
        ''' Gets the encoding variant used.
        ''' </summary>
        Public ReadOnly Property Encoding As Bech32Encoding

        ''' <summary>
        ''' Creates a new Bech32Data instance.
        ''' </summary>
        Public Sub New(hrp As String, witnessVersion As Integer, data() As Byte, encoding As Bech32Encoding)
            Me.Hrp = hrp
            Me.WitnessVersion = witnessVersion
            Me.Data = data
            Me.Encoding = encoding
        End Sub
    End Class

    ''' <summary>
    ''' Provides Bech32 and Bech32m encoding and decoding for SegWit-style addresses.
    ''' Implements BIP173 (Bech32) and BIP350 (Bech32m) specifications.
    ''' 
    ''' Bech32 addresses consist of:
    ''' - A human-readable part (HRP) identifying the network
    ''' - A separator character '1'
    ''' - A data part encoded in base-32 using a specific character set
    ''' - A 6-character checksum
    ''' </summary>
    Public NotInheritable Class Bech32Encoder

        ''' <summary>
        ''' The Bech32 character set for encoding 5-bit values.
        ''' </summary>
        Public Const Charset As String = "qpzry9x8gf2tvdw0s3jn54khce6mua7l"

        Private Shared ReadOnly CharsetReverse(127) As Integer

        ''' <summary>
        ''' Bech32 checksum constant.
        ''' </summary>
        Private Const Bech32Constant As UInteger = 1UI

        ''' <summary>
        ''' Bech32m checksum constant (BIP350).
        ''' </summary>
        Private Const Bech32mConstant As UInteger = &H2BC830A3UI

        ''' <summary>
        ''' Generator polynomial values for checksum computation.
        ''' </summary>
        Private Shared ReadOnly Generator() As UInteger = {
            &H3B6A57B2UI, &H26508E6DUI, &H1EA119FAUI, &H3D4233DDUI, &H2A1462B3UI
        }

        Shared Sub New()
            ' Initialize reverse lookup
            For i As Integer = 0 To 127
                CharsetReverse(i) = -1
            Next
            For i As Integer = 0 To Charset.Length - 1
                CharsetReverse(AscW(Charset(i))) = i
            Next
        End Sub

        Private Sub New()
        End Sub

        ''' <summary>
        ''' Encodes a witness program as a Bech32/Bech32m address.
        ''' </summary>
        ''' <param name="hrp">The human-readable part (e.g., "cc" for CryptoCoin mainnet).</param>
        ''' <param name="witnessVersion">The witness version (0-16).</param>
        ''' <param name="witnessProgram">The witness program bytes (20 or 32 bytes typically).</param>
        ''' <returns>The encoded Bech32/Bech32m address string.</returns>
        ''' <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
        Public Shared Function Encode(hrp As String, witnessVersion As Integer, witnessProgram() As Byte) As String
            If String.IsNullOrEmpty(hrp) Then
                Throw New ArgumentException("HRP cannot be null or empty.", NameOf(hrp))
            End If
            If witnessVersion < 0 OrElse witnessVersion > 16 Then
                Throw New ArgumentOutOfRangeException(NameOf(witnessVersion), "Witness version must be 0-16.")
            End If
            If witnessProgram Is Nothing OrElse witnessProgram.Length < 2 OrElse witnessProgram.Length > 40 Then
                Throw New ArgumentException("Witness program must be 2-40 bytes.", NameOf(witnessProgram))
            End If

            ' Validate witness program length for specific versions
            If witnessVersion = 0 AndAlso witnessProgram.Length <> 20 AndAlso witnessProgram.Length <> 32 Then
                Throw New ArgumentException("Witness version 0 programs must be 20 or 32 bytes.", NameOf(witnessProgram))
            End If

            ' Convert 8-bit data to 5-bit groups
            Dim converted() As Byte = ConvertBits(witnessProgram, 8, 5, True)

            ' Prepend witness version
            Dim data(converted.Length) As Byte
            data(0) = CByte(witnessVersion)
            Array.Copy(converted, 0, data, 1, converted.Length)

            ' Determine encoding variant
            Dim enc As Bech32Encoding = If(witnessVersion = 0, Bech32Encoding.Bech32, Bech32Encoding.Bech32m)

            Return EncodeBech32(hrp, data, enc)
        End Function

        ''' <summary>
        ''' Decodes a Bech32/Bech32m address to its components.
        ''' </summary>
        ''' <param name="address">The Bech32/Bech32m encoded address.</param>
        ''' <returns>The decoded address components.</returns>
        ''' <exception cref="ArgumentException">Thrown when the address is invalid.</exception>
        Public Shared Function Decode(address As String) As Bech32Data
            If String.IsNullOrEmpty(address) Then
                Throw New ArgumentException("Address cannot be null or empty.", NameOf(address))
            End If

            ' Bech32 addresses must be lowercase or uppercase, not mixed
            If address <> address.ToLowerInvariant() AndAlso address <> address.ToUpperInvariant() Then
                Throw New ArgumentException("Mixed case in Bech32 address.", NameOf(address))
            End If

            address = address.ToLowerInvariant()

            ' Find the separator
            Dim separatorIndex As Integer = address.LastIndexOf("1"c)
            If separatorIndex < 1 Then
                Throw New ArgumentException("Missing separator in Bech32 address.", NameOf(address))
            End If
            If separatorIndex + 7 > address.Length Then
                Throw New ArgumentException("Data part too short in Bech32 address.", NameOf(address))
            End If

            Dim hrp As String = address.Substring(0, separatorIndex)
            Dim dataPart As String = address.Substring(separatorIndex + 1)

            ' Validate HRP
            If hrp.Length < 1 OrElse hrp.Length > 83 Then
                Throw New ArgumentException("Invalid HRP length.", NameOf(address))
            End If
            For Each c As Char In hrp
                If AscW(c) < 33 OrElse AscW(c) > 126 Then
                    Throw New ArgumentException("Invalid character in HRP.", NameOf(address))
                End If
            Next

            ' Decode data characters to 5-bit values
            Dim data(dataPart.Length - 1) As Byte
            For i As Integer = 0 To dataPart.Length - 1
                Dim c As Char = dataPart(i)
                Dim code As Integer = AscW(c)
                If code > 127 OrElse CharsetReverse(code) = -1 Then
                    Throw New ArgumentException($"Invalid Bech32 character: '{c}'.", NameOf(address))
                End If
                data(i) = CByte(CharsetReverse(code))
            Next

            ' Verify checksum and determine encoding
            Dim enc As Bech32Encoding = VerifyChecksum(hrp, data)
            If enc = 0 Then
                Throw New ArgumentException("Invalid Bech32 checksum.", NameOf(address))
            End If

            ' Extract witness version and program
            Dim witnessVersion As Integer = CInt(data(0))
            If witnessVersion > 16 Then
                Throw New ArgumentException("Invalid witness version.", NameOf(address))
            End If

            ' Verify encoding matches witness version
            If witnessVersion = 0 AndAlso enc <> Bech32Encoding.Bech32 Then
                Throw New ArgumentException("Witness version 0 must use Bech32 encoding.", NameOf(address))
            End If
            If witnessVersion > 0 AndAlso enc <> Bech32Encoding.Bech32m Then
                Throw New ArgumentException("Witness version 1+ must use Bech32m encoding.", NameOf(address))
            End If

            ' Convert 5-bit data (excluding version and checksum) to 8-bit
            Dim fiveBitData(data.Length - 7) As Byte ' Exclude version (1) and checksum (6)
            Array.Copy(data, 1, fiveBitData, 0, fiveBitData.Length)

            Dim witnessProgram() As Byte = ConvertBits(fiveBitData, 5, 8, False)

            ' Validate witness program length
            If witnessProgram.Length < 2 OrElse witnessProgram.Length > 40 Then
                Throw New ArgumentException("Invalid witness program length.", NameOf(address))
            End If
            If witnessVersion = 0 AndAlso witnessProgram.Length <> 20 AndAlso witnessProgram.Length <> 32 Then
                Throw New ArgumentException("Witness version 0 program must be 20 or 32 bytes.", NameOf(address))
            End If

            Return New Bech32Data(hrp, witnessVersion, witnessProgram, enc)
        End Function

        ''' <summary>
        ''' Validates a Bech32/Bech32m address without throwing exceptions.
        ''' </summary>
        ''' <param name="address">The address to validate.</param>
        ''' <returns>True if the address is valid.</returns>
        Public Shared Function IsValid(address As String) As Boolean
            Try
                Decode(address)
                Return True
            Catch
                Return False
            End Try
        End Function

        ''' <summary>
        ''' Validates a Bech32/Bech32m address for a specific HRP.
        ''' </summary>
        ''' <param name="address">The address to validate.</param>
        ''' <param name="expectedHrp">The expected human-readable part.</param>
        ''' <returns>True if the address is valid and has the expected HRP.</returns>
        Public Shared Function IsValid(address As String, expectedHrp As String) As Boolean
            Try
                Dim result As Bech32Data = Decode(address)
                Return String.Equals(result.Hrp, expectedHrp, StringComparison.OrdinalIgnoreCase)
            Catch
                Return False
            End Try
        End Function

        ''' <summary>
        ''' Encodes raw data with Bech32/Bech32m encoding (low-level).
        ''' </summary>
        ''' <param name="hrp">The human-readable part.</param>
        ''' <param name="data">The 5-bit data values.</param>
        ''' <param name="enc">The encoding variant to use.</param>
        ''' <returns>The encoded string.</returns>
        Public Shared Function EncodeBech32(hrp As String, data() As Byte, enc As Bech32Encoding) As String
            ' Compute checksum
            Dim checksum() As Byte = CreateChecksum(hrp, data, enc)

            ' Build result string
            Dim result As New StringBuilder(hrp.Length + 1 + data.Length + 6)
            result.Append(hrp)
            result.Append("1"c)

            For Each b As Byte In data
                result.Append(Charset(CInt(b)))
            Next
            For Each b As Byte In checksum
                result.Append(Charset(CInt(b)))
            Next

            Return result.ToString()
        End Function

        ''' <summary>
        ''' Converts data between different bit group sizes.
        ''' Used to convert between 8-bit bytes and 5-bit Bech32 values.
        ''' </summary>
        ''' <param name="data">The input data.</param>
        ''' <param name="fromBits">The source bit group size.</param>
        ''' <param name="toBits">The target bit group size.</param>
        ''' <param name="pad">Whether to pad the output.</param>
        ''' <returns>The converted data.</returns>
        ''' <exception cref="ArgumentException">Thrown when conversion fails.</exception>
        Public Shared Function ConvertBits(data() As Byte, fromBits As Integer, toBits As Integer, pad As Boolean) As Byte()
            Dim acc As Integer = 0
            Dim bits As Integer = 0
            Dim maxValue As Integer = (1 << toBits) - 1
            Dim result As New List(Of Byte)()

            For Each value As Byte In data
                If (CInt(value) >> fromBits) <> 0 Then
                    Throw New ArgumentException("Invalid data value for bit conversion.")
                End If

                acc = (acc << fromBits) Or CInt(value)
                bits += fromBits

                While bits >= toBits
                    bits -= toBits
                    result.Add(CByte((acc >> bits) And maxValue))
                End While
            Next

            If pad Then
                If bits > 0 Then
                    result.Add(CByte((acc << (toBits - bits)) And maxValue))
                End If
            Else
                If bits >= fromBits Then
                    Throw New ArgumentException("Invalid padding in bit conversion.")
                End If
                If ((acc << (toBits - bits)) And maxValue) <> 0 Then
                    Throw New ArgumentException("Non-zero padding in bit conversion.")
                End If
            End If

            Return result.ToArray()
        End Function

        ''' <summary>
        ''' Computes the Bech32 polymod checksum value.
        ''' </summary>
        Private Shared Function Polymod(values() As Byte) As UInteger
            Dim chk As UInteger = 1UI
            For Each v As Byte In values
                Dim top As UInteger = chk >> 25
                chk = ((chk And &H1FFFFFFUI) << 5) Xor CUInt(v)
                For i As Integer = 0 To 4
                    If ((top >> i) And 1UI) <> 0 Then
                        chk = chk Xor Generator(i)
                    End If
                Next
            Next
            Return chk
        End Function

        ''' <summary>
        ''' Expands the HRP for checksum computation.
        ''' </summary>
        Private Shared Function HrpExpand(hrp As String) As Byte()
            Dim result(hrp.Length * 2) As Byte
            For i As Integer = 0 To hrp.Length - 1
                result(i) = CByte(AscW(hrp(i)) >> 5)
            Next
            result(hrp.Length) = 0
            For i As Integer = 0 To hrp.Length - 1
                result(hrp.Length + 1 + i) = CByte(AscW(hrp(i)) And 31)
            Next
            Return result
        End Function

        ''' <summary>
        ''' Creates the 6-byte checksum for Bech32/Bech32m encoding.
        ''' </summary>
        Private Shared Function CreateChecksum(hrp As String, data() As Byte, enc As Bech32Encoding) As Byte()
            Dim hrpExpanded() As Byte = HrpExpand(hrp)
            Dim values(hrpExpanded.Length + data.Length + 5) As Byte
            Array.Copy(hrpExpanded, values, hrpExpanded.Length)
            Array.Copy(data, 0, values, hrpExpanded.Length, data.Length)
            ' Last 6 bytes are zeros for checksum computation

            Dim constant As UInteger = If(enc = Bech32Encoding.Bech32, Bech32Constant, Bech32mConstant)
            Dim polymodValue As UInteger = Polymod(values) Xor constant

            Dim checksum(5) As Byte
            For i As Integer = 0 To 5
                checksum(i) = CByte((polymodValue >> (5 * (5 - i))) And 31)
            Next
            Return checksum
        End Function

        ''' <summary>
        ''' Verifies the checksum and returns the encoding variant, or 0 if invalid.
        ''' </summary>
        Private Shared Function VerifyChecksum(hrp As String, data() As Byte) As Bech32Encoding
            Dim hrpExpanded() As Byte = HrpExpand(hrp)
            Dim values(hrpExpanded.Length + data.Length - 1) As Byte
            Array.Copy(hrpExpanded, values, hrpExpanded.Length)
            Array.Copy(data, 0, values, hrpExpanded.Length, data.Length)

            Dim polymodValue As UInteger = Polymod(values)

            If polymodValue = Bech32Constant Then
                Return Bech32Encoding.Bech32
            ElseIf polymodValue = Bech32mConstant Then
                Return Bech32Encoding.Bech32m
            Else
                Return CType(0, Bech32Encoding)
            End If
        End Function
    End Class

End Namespace
