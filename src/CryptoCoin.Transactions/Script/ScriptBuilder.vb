Imports System.IO

Namespace CryptoCoin.Transactions.Script

    ''' <summary>
    ''' Fluent builder for constructing transaction scripts.
    ''' </summary>
    Public Class ScriptBuilder

        Private ReadOnly _stream As New MemoryStream()

        ''' <summary>
        ''' Adds an opcode to the script.
        ''' </summary>
        Public Function AddOp(opcode As Byte) As ScriptBuilder
            _stream.WriteByte(opcode)
            Return Me
        End Function

        ''' <summary>
        ''' Pushes data onto the stack.
        ''' Automatically selects the correct push opcode based on data length.
        ''' </summary>
        Public Function PushData(data As Byte()) As ScriptBuilder
            If data Is Nothing Then Throw New ArgumentNullException(NameOf(data))

            If data.Length = 0 Then
                _stream.WriteByte(OpCodes.OP_0)
            ElseIf data.Length <= 75 Then
                ' Direct push (length byte followed by data)
                _stream.WriteByte(CByte(data.Length))
                _stream.Write(data, 0, data.Length)
            ElseIf data.Length <= 255 Then
                ' OP_PUSHDATA1
                _stream.WriteByte(OpCodes.OP_PUSHDATA1)
                _stream.WriteByte(CByte(data.Length))
                _stream.Write(data, 0, data.Length)
            ElseIf data.Length <= 65535 Then
                ' OP_PUSHDATA2
                _stream.WriteByte(OpCodes.OP_PUSHDATA2)
                Dim lenBytes As Byte() = BitConverter.GetBytes(CUShort(data.Length))
                _stream.Write(lenBytes, 0, 2)
                _stream.Write(data, 0, data.Length)
            Else
                ' OP_PUSHDATA4
                _stream.WriteByte(OpCodes.OP_PUSHDATA4)
                Dim lenBytes As Byte() = BitConverter.GetBytes(CUInt(data.Length))
                _stream.Write(lenBytes, 0, 4)
                _stream.Write(data, 0, data.Length)
            End If

            Return Me
        End Function

        ''' <summary>
        ''' Pushes a small integer (0-16) using the appropriate opcode.
        ''' </summary>
        Public Function PushNumber(value As Integer) As ScriptBuilder
            If value = 0 Then
                _stream.WriteByte(OpCodes.OP_0)
            ElseIf value = -1 Then
                _stream.WriteByte(OpCodes.OP_1NEGATE)
            ElseIf value >= 1 AndAlso value <= 16 Then
                _stream.WriteByte(CByte(OpCodes.OP_1 + value - 1))
            Else
                ' Encode as data push
                Dim bytes As Byte() = EncodeScriptNumber(value)
                PushData(bytes)
            End If
            Return Me
        End Function

        ''' <summary>
        ''' Pushes a hex-encoded value.
        ''' </summary>
        Public Function PushHex(hex As String) As ScriptBuilder
            Dim data As Byte() = Cryptography.HashUtil.FromHex(hex)
            Return PushData(data)
        End Function

        ''' <summary>
        ''' Adds OP_DUP.
        ''' </summary>
        Public Function Dup() As ScriptBuilder
            Return AddOp(OpCodes.OP_DUP)
        End Function

        ''' <summary>
        ''' Adds OP_HASH160.
        ''' </summary>
        Public Function Hash160() As ScriptBuilder
            Return AddOp(OpCodes.OP_HASH160)
        End Function

        ''' <summary>
        ''' Adds OP_EQUALVERIFY.
        ''' </summary>
        Public Function EqualVerify() As ScriptBuilder
            Return AddOp(OpCodes.OP_EQUALVERIFY)
        End Function

        ''' <summary>
        ''' Adds OP_CHECKSIG.
        ''' </summary>
        Public Function CheckSig() As ScriptBuilder
            Return AddOp(OpCodes.OP_CHECKSIG)
        End Function

        ''' <summary>
        ''' Adds OP_EQUAL.
        ''' </summary>
        Public Function Equal() As ScriptBuilder
            Return AddOp(OpCodes.OP_EQUAL)
        End Function

        ''' <summary>
        ''' Adds OP_RETURN.
        ''' </summary>
        Public Function OpReturn() As ScriptBuilder
            Return AddOp(OpCodes.OP_RETURN)
        End Function

        ''' <summary>
        ''' Gets the built script as a byte array.
        ''' </summary>
        Public Function ToBytes() As Byte()
            Return _stream.ToArray()
        End Function

        ''' <summary>
        ''' Gets the script length.
        ''' </summary>
        Public ReadOnly Property Length As Integer
            Get
                Return CInt(_stream.Length)
            End Get
        End Property

        ''' <summary>
        ''' Encodes an integer for use in script.
        ''' </summary>
        Private Shared Function EncodeScriptNumber(value As Integer) As Byte()
            If value = 0 Then Return New Byte() {}

            Dim negative As Boolean = value < 0
            Dim absValue As Long = Math.Abs(CLng(value))
            Dim result As New List(Of Byte)()

            While absValue > 0
                result.Add(CByte(absValue And &HFF))
                absValue >>= 8
            End While

            ' If the most significant byte has the high bit set, add a sign byte
            If (result(result.Count - 1) And &H80) <> 0 Then
                result.Add(If(negative, CByte(&H80), CByte(0)))
            ElseIf negative Then
                result(result.Count - 1) = CByte(result(result.Count - 1) Or &H80)
            End If

            Return result.ToArray()
        End Function

    End Class

End Namespace
