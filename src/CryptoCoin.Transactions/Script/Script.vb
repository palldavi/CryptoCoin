Imports CryptoCoin.Cryptography

Namespace CryptoCoin.Transactions.Script

    ''' <summary>
    ''' Represents a parsed script with its operations.
    ''' </summary>
    Public Class Script

        ''' <summary>
        ''' The raw script bytes.
        ''' </summary>
        Public ReadOnly Property RawBytes As Byte()

        ''' <summary>
        ''' The parsed operations in the script.
        ''' </summary>
        Public ReadOnly Property Operations As List(Of ScriptOp)

        Public Sub New(rawBytes As Byte())
            If rawBytes Is Nothing Then rawBytes = New Byte() {}
            _RawBytes = rawBytes
            _Operations = Parse(rawBytes)
        End Sub

        ''' <summary>
        ''' Parses raw script bytes into a list of operations.
        ''' </summary>
        Private Shared Function Parse(data As Byte()) As List(Of ScriptOp)
            Dim ops As New List(Of ScriptOp)()
            Dim i As Integer = 0

            While i < data.Length
                Dim opcode As Byte = data(i)
                i += 1

                If opcode >= 1 AndAlso opcode <= 75 Then
                    ' Direct data push
                    Dim pushData(opcode - 1) As Byte
                    If i + opcode <= data.Length Then
                        Array.Copy(data, i, pushData, 0, opcode)
                    End If
                    i += opcode
                    ops.Add(New ScriptOp(opcode, pushData))
                ElseIf opcode = OpCodes.OP_PUSHDATA1 Then
                    If i < data.Length Then
                        Dim length As Integer = data(i) : i += 1
                        Dim pushData(length - 1) As Byte
                        If i + length <= data.Length Then
                            Array.Copy(data, i, pushData, 0, length)
                        End If
                        i += length
                        ops.Add(New ScriptOp(opcode, pushData))
                    End If
                ElseIf opcode = OpCodes.OP_PUSHDATA2 Then
                    If i + 2 <= data.Length Then
                        Dim length As Integer = BitConverter.ToUInt16(data, i) : i += 2
                        Dim pushData(length - 1) As Byte
                        If i + length <= data.Length Then
                            Array.Copy(data, i, pushData, 0, length)
                        End If
                        i += length
                        ops.Add(New ScriptOp(opcode, pushData))
                    End If
                ElseIf opcode = OpCodes.OP_PUSHDATA4 Then
                    If i + 4 <= data.Length Then
                        Dim length As Integer = CInt(BitConverter.ToUInt32(data, i)) : i += 4
                        Dim pushData(length - 1) As Byte
                        If i + length <= data.Length Then
                            Array.Copy(data, i, pushData, 0, length)
                        End If
                        i += length
                        ops.Add(New ScriptOp(opcode, pushData))
                    End If
                Else
                    ops.Add(New ScriptOp(opcode, Nothing))
                End If
            End While

            Return ops
        End Function

        ''' <summary>
        ''' Gets a human-readable representation of the script.
        ''' </summary>
        Public Function ToAsm() As String
            Dim parts As New List(Of String)()
            For Each op As ScriptOp In Operations
                If op.Data IsNot Nothing Then
                    parts.Add(HashUtil.ToHex(op.Data))
                Else
                    parts.Add(OpCodes.GetName(op.OpCode))
                End If
            Next
            Return String.Join(" ", parts)
        End Function

        ''' <summary>
        ''' Gets the script length.
        ''' </summary>
        Public ReadOnly Property Length As Integer
            Get
                Return RawBytes.Length
            End Get
        End Property

        Public Overrides Function ToString() As String
            Return ToAsm()
        End Function

    End Class

    ''' <summary>
    ''' Represents a single operation in a script.
    ''' </summary>
    Public Class ScriptOp

        ''' <summary>
        ''' The opcode byte.
        ''' </summary>
        Public ReadOnly Property OpCode As Byte

        ''' <summary>
        ''' The data associated with this operation (for push operations).
        ''' </summary>
        Public ReadOnly Property Data As Byte()

        ''' <summary>
        ''' Whether this operation pushes data onto the stack.
        ''' </summary>
        Public ReadOnly Property IsPushData As Boolean
            Get
                Return Data IsNot Nothing
            End Get
        End Property

        Public Sub New(opCode As Byte, data As Byte())
            Me.OpCode = opCode
            Me.Data = data
        End Sub

        Public Overrides Function ToString() As String
            If Data IsNot Nothing Then
                Return $"PUSH({Data.Length} bytes)"
            End If
            Return OpCodes.GetName(OpCode)
        End Function

    End Class

End Namespace
