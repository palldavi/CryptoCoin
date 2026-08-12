Namespace CryptoCoin.Core.Serialization

    ''' <summary>
    ''' Variable-length integer encoding used in the CryptoCoin protocol.
    ''' Encodes integers using 1-9 bytes depending on the value.
    ''' </summary>
    Public NotInheritable Class VarInt

        Private Sub New()
        End Sub

        ''' <summary>
        ''' Encodes a value as a variable-length integer.
        ''' </summary>
        Public Shared Function Encode(value As Long) As Byte()
            If value < 0 Then Throw New ArgumentOutOfRangeException(NameOf(value))

            If value < &HFD Then
                Return New Byte() {CByte(value)}
            ElseIf value <= &HFFFF Then
                Dim result(2) As Byte
                result(0) = &HFD
                result(1) = CByte(value And &HFF)
                result(2) = CByte((value >> 8) And &HFF)
                Return result
            ElseIf value <= &HFFFFFFFFL Then
                Dim result(4) As Byte
                result(0) = &HFE
                Dim bytes As Byte() = BitConverter.GetBytes(CUInt(value))
                Array.Copy(bytes, 0, result, 1, 4)
                Return result
            Else
                Dim result(8) As Byte
                result(0) = &HFF
                Dim bytes As Byte() = BitConverter.GetBytes(CLng(value))
                Array.Copy(bytes, 0, result, 1, 8)
                Return result
            End If
        End Function

        ''' <summary>
        ''' Decodes a variable-length integer from a byte array at the given offset.
        ''' Returns the value and advances the offset.
        ''' </summary>
        Public Shared Function Decode(data As Byte(), ByRef offset As Integer) As Long
            If data Is Nothing Then Throw New ArgumentNullException(NameOf(data))
            If offset >= data.Length Then Throw New ArgumentOutOfRangeException(NameOf(offset))

            Dim first As Byte = data(offset)
            offset += 1

            If first < &HFD Then
                Return CLng(first)
            ElseIf first = &HFD Then
                If offset + 2 > data.Length Then Throw New FormatException("Insufficient data for VarInt.")
                Dim value As Long = CLng(data(offset)) Or (CLng(data(offset + 1)) << 8)
                offset += 2
                Return value
            ElseIf first = &HFE Then
                If offset + 4 > data.Length Then Throw New FormatException("Insufficient data for VarInt.")
                Dim value As Long = BitConverter.ToUInt32(data, offset)
                offset += 4
                Return value
            Else
                If offset + 8 > data.Length Then Throw New FormatException("Insufficient data for VarInt.")
                Dim value As Long = BitConverter.ToInt64(data, offset)
                offset += 8
                Return value
            End If
        End Function

        ''' <summary>
        ''' Gets the encoded size of a value without actually encoding it.
        ''' </summary>
        Public Shared Function GetEncodedSize(value As Long) As Integer
            If value < &HFD Then Return 1
            If value <= &HFFFF Then Return 3
            If value <= &HFFFFFFFFL Then Return 5
            Return 9
        End Function

    End Class

End Namespace
