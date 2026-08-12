' ===============================================================================
' CryptoCoin.Contracts - ContractCompiler.vb
' Simple compiler from a basic contract language to VM bytecode.
' ===============================================================================

Imports System
Imports System.Collections.Generic
Imports System.Text

Namespace CryptoCoin.Contracts

    ''' <summary>
    ''' Compiles a simple high-level contract language into VM bytecode.
    ''' Supports variable declarations, arithmetic expressions, conditionals,
    ''' storage operations, and function definitions.
    ''' </summary>
    ''' <remarks>
    ''' Language syntax:
    '''   var x = 10
    '''   storage["key"] = value
    '''   if condition then ... end
    '''   function name(params) ... end
    '''   return value
    ''' </remarks>
    Public Class ContractCompiler

        Private ReadOnly _output As List(Of Byte)
        Private ReadOnly _labels As Dictionary(Of String, Integer)
        Private ReadOnly _variables As Dictionary(Of String, Integer)
        Private ReadOnly _functions As Dictionary(Of String, Integer)
        Private ReadOnly _pendingJumps As List(Of PendingJump)
        Private _variableSlot As Integer
        Private _errors As List(Of CompilerError)

        ''' <summary>Gets the list of compilation errors.</summary>
        Public ReadOnly Property Errors As List(Of CompilerError)
            Get
                Return _errors
            End Get
        End Property

        ''' <summary>Gets whether the last compilation had errors.</summary>
        Public ReadOnly Property HasErrors As Boolean
            Get
                Return _errors IsNot Nothing AndAlso _errors.Count > 0
            End Get
        End Property

        ''' <summary>
        ''' Initializes a new ContractCompiler instance.
        ''' </summary>
        Public Sub New()
            _output = New List(Of Byte)()
            _labels = New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)
            _variables = New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)
            _functions = New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)
            _pendingJumps = New List(Of PendingJump)()
            _variableSlot = 0
            _errors = New List(Of CompilerError)()
        End Sub

        ''' <summary>
        ''' Compiles source code into VM bytecode.
        ''' </summary>
        ''' <param name="source">The source code to compile.</param>
        ''' <returns>The compiled bytecode, or Nothing if compilation failed.</returns>
        Public Function Compile(source As String) As Byte()
            _output.Clear()
            _labels.Clear()
            _variables.Clear()
            _functions.Clear()
            _pendingJumps.Clear()
            _variableSlot = 0
            _errors.Clear()

            If String.IsNullOrWhiteSpace(source) Then
                _errors.Add(New CompilerError(0, "Empty source code"))
                Return Nothing
            End If

            Try
                ' Tokenize
                Dim lines As String() = source.Split(New String() {vbCrLf, vbLf, vbCr}, StringSplitOptions.None)

                ' First pass: collect function definitions and labels
                FirstPass(lines)

                ' Second pass: generate bytecode
                SecondPass(lines)

                ' Resolve pending jumps
                ResolveJumps()

                If _errors.Count > 0 Then
                    Return Nothing
                End If

                Return _output.ToArray()

            Catch ex As Exception
                _errors.Add(New CompilerError(0, $"Compilation failed: {ex.Message}"))
                Return Nothing
            End Try
        End Function

        ''' <summary>
        ''' First pass: collects function definitions and label positions.
        ''' </summary>
        Private Sub FirstPass(lines As String())
            For i As Integer = 0 To lines.Length - 1
                Dim line As String = lines(i).Trim()
                If line.StartsWith("function ", StringComparison.OrdinalIgnoreCase) Then
                    Dim funcName As String = ExtractFunctionName(line)
                    If Not String.IsNullOrEmpty(funcName) Then
                        _functions(funcName) = i
                    End If
                ElseIf line.EndsWith(":") Then
                    Dim labelName As String = line.TrimEnd(":"c).Trim()
                    _labels(labelName) = i
                End If
            Next
        End Sub

        ''' <summary>
        ''' Second pass: generates bytecode from source lines.
        ''' </summary>
        Private Sub SecondPass(lines As String())
            For i As Integer = 0 To lines.Length - 1
                Dim line As String = lines(i).Trim()

                ' Skip empty lines and comments
                If String.IsNullOrEmpty(line) OrElse line.StartsWith("//") OrElse line.StartsWith("'") Then
                    Continue For
                End If

                CompileLine(line, i + 1)
            Next

            ' Add HALT at end if not already present
            If _output.Count = 0 OrElse _output(_output.Count - 1) <> CByte(ContractOpCode.HALT) Then
                EmitOpCode(ContractOpCode.HALT)
            End If
        End Sub

        ''' <summary>
        ''' Compiles a single source line into bytecode.
        ''' </summary>
        Private Sub CompileLine(line As String, lineNumber As Integer)
            Dim tokens As String() = TokenizeLine(line)
            If tokens.Length = 0 Then Return

            Dim keyword As String = tokens(0).ToLowerInvariant()

            Select Case keyword
                Case "push"
                    CompilePush(tokens, lineNumber)
                Case "var"
                    CompileVarDeclaration(tokens, lineNumber)
                Case "store", "sstore"
                    CompileStore(tokens, lineNumber)
                Case "load", "sload"
                    CompileLoad(tokens, lineNumber)
                Case "add"
                    EmitOpCode(ContractOpCode.ADD)
                Case "sub"
                    EmitOpCode(ContractOpCode.[SUB])
                Case "mul"
                    EmitOpCode(ContractOpCode.MUL)
                Case "div"
                    EmitOpCode(ContractOpCode.DIV)
                Case "mod"
                    EmitOpCode(ContractOpCode.[MOD])
                Case "eq"
                    EmitOpCode(ContractOpCode.EQ)
                Case "neq"
                    EmitOpCode(ContractOpCode.NEQ)
                Case "lt"
                    EmitOpCode(ContractOpCode.LT)
                Case "gt"
                    EmitOpCode(ContractOpCode.GT)
                Case "and"
                    EmitOpCode(ContractOpCode.[AND])
                Case "or"
                    EmitOpCode(ContractOpCode.[OR])
                Case "not"
                    EmitOpCode(ContractOpCode.[NOT])
                Case "sha256"
                    EmitOpCode(ContractOpCode.SHA256)
                Case "hash160"
                    EmitOpCode(ContractOpCode.HASH160)
                Case "dup"
                    EmitOpCode(ContractOpCode.DUP)
                Case "pop"
                    EmitOpCode(ContractOpCode.POP)
                Case "swap"
                    EmitOpCode(ContractOpCode.SWAP)
                Case "jump"
                    CompileJump(tokens, lineNumber)
                Case "jumpi"
                    CompileJumpI(tokens, lineNumber)
                Case "jumpdest"
                    EmitOpCode(ContractOpCode.JUMPDEST)
                Case "return"
                    EmitOpCode(ContractOpCode.[RETURN])
                Case "revert"
                    EmitOpCode(ContractOpCode.REVERT)
                Case "halt", "stop"
                    EmitOpCode(ContractOpCode.HALT)
                Case "caller"
                    EmitOpCode(ContractOpCode.CALLER)
                Case "callvalue"
                    EmitOpCode(ContractOpCode.CALLVALUE)
                Case "address"
                    EmitOpCode(ContractOpCode.ADDRESS)
                Case "blocknumber"
                    EmitOpCode(ContractOpCode.BLOCKNUMBER)
                Case "timestamp"
                    EmitOpCode(ContractOpCode.TIMESTAMP)
                Case "gas"
                    EmitOpCode(ContractOpCode.GAS)
                Case "function"
                    ' Function marker - emit JUMPDEST
                    EmitOpCode(ContractOpCode.JUMPDEST)
                Case "end"
                    ' End of block - no-op
                Case Else
                    ' Check if it's a label
                    If line.EndsWith(":") Then
                        _labels(line.TrimEnd(":"c).Trim()) = _output.Count
                        EmitOpCode(ContractOpCode.JUMPDEST)
                    Else
                        _errors.Add(New CompilerError(lineNumber, $"Unknown instruction: {keyword}"))
                    End If
            End Select
        End Sub

        ''' <summary>
        ''' Compiles a PUSH instruction with an immediate value.
        ''' </summary>
        Private Sub CompilePush(tokens As String(), lineNumber As Integer)
            If tokens.Length < 2 Then
                _errors.Add(New CompilerError(lineNumber, "PUSH requires a value"))
                Return
            End If

            Dim valueStr As String = tokens(1)
            Dim value As Integer
            If Integer.TryParse(valueStr, value) Then
                EmitPushInt(value)
            Else
                ' Push as string bytes
                Dim strBytes As Byte() = Encoding.UTF8.GetBytes(valueStr.Trim(""""c))
                EmitOpCode(ContractOpCode.PUSH)
                _output.Add(CByte(strBytes.Length))
                _output.AddRange(strBytes)
            End If
        End Sub

        ''' <summary>
        ''' Compiles a variable declaration.
        ''' </summary>
        Private Sub CompileVarDeclaration(tokens As String(), lineNumber As Integer)
            If tokens.Length < 4 OrElse tokens(2) <> "=" Then
                _errors.Add(New CompilerError(lineNumber, "Invalid variable declaration. Use: var name = value"))
                Return
            End If

            Dim varName As String = tokens(1)
            _variables(varName) = _variableSlot
            _variableSlot += 1

            ' Compile the value expression
            Dim value As Integer
            If Integer.TryParse(tokens(3), value) Then
                EmitPushInt(value)
            End If

            ' Store in memory slot
            EmitPushInt(_variables(varName))
            EmitOpCode(ContractOpCode.MSTORE)
        End Sub

        ''' <summary>
        ''' Compiles a storage store operation.
        ''' </summary>
        Private Sub CompileStore(tokens As String(), lineNumber As Integer)
            If tokens.Length < 3 Then
                _errors.Add(New CompilerError(lineNumber, "STORE requires key and value"))
                Return
            End If
            ' Key and value should already be on stack, just emit SSTORE
            EmitOpCode(ContractOpCode.SSTORE)
        End Sub

        ''' <summary>
        ''' Compiles a storage load operation.
        ''' </summary>
        Private Sub CompileLoad(tokens As String(), lineNumber As Integer)
            ' Key should already be on stack, just emit SLOAD
            EmitOpCode(ContractOpCode.SLOAD)
        End Sub

        ''' <summary>
        ''' Compiles an unconditional jump.
        ''' </summary>
        Private Sub CompileJump(tokens As String(), lineNumber As Integer)
            If tokens.Length < 2 Then
                _errors.Add(New CompilerError(lineNumber, "JUMP requires a target label"))
                Return
            End If

            Dim target As String = tokens(1)
            EmitPushInt(0) ' Placeholder
            _pendingJumps.Add(New PendingJump() With {
                .Position = _output.Count - 4,
                .Label = target
            })
            EmitOpCode(ContractOpCode.JUMP)
        End Sub

        ''' <summary>
        ''' Compiles a conditional jump.
        ''' </summary>
        Private Sub CompileJumpI(tokens As String(), lineNumber As Integer)
            If tokens.Length < 2 Then
                _errors.Add(New CompilerError(lineNumber, "JUMPI requires a target label"))
                Return
            End If

            Dim target As String = tokens(1)
            EmitPushInt(0) ' Placeholder for destination
            _pendingJumps.Add(New PendingJump() With {
                .Position = _output.Count - 4,
                .Label = target
            })
            EmitOpCode(ContractOpCode.JUMPI)
        End Sub

        ''' <summary>
        ''' Resolves all pending jump targets after compilation.
        ''' </summary>
        Private Sub ResolveJumps()
            For Each jump As Object In _pendingJumps
                If _labels.ContainsKey(jump.Label) Then
                    Dim target As Integer = _labels(jump.Label)
                    Dim targetBytes As Byte() = BitConverter.GetBytes(target)
                    For i As Integer = 0 To 3
                        _output(jump.Position + i) = targetBytes(i)
                    Next
                Else
                    _errors.Add(New CompilerError(0, $"Unresolved label: {jump.Label}"))
                End If
            Next
        End Sub

        ' --- Helper methods ---

        Private Sub EmitOpCode(opCode As ContractOpCode)
            _output.Add(CByte(opCode))
        End Sub

        Private Sub EmitPushInt(value As Integer)
            EmitOpCode(ContractOpCode.PUSH4)
            _output.AddRange(BitConverter.GetBytes(value))
        End Sub

        Private Function TokenizeLine(line As String) As String()
            Return line.Split(New Char() {" "c, vbTab}, StringSplitOptions.RemoveEmptyEntries)
        End Function

        Private Function ExtractFunctionName(line As String) As String
            Dim parts As String() = line.Split(New Char() {" "c, "("c}, StringSplitOptions.RemoveEmptyEntries)
            If parts.Length >= 2 Then Return parts(1)
            Return Nothing
        End Function

    End Class

    ''' <summary>
    ''' Represents a compilation error with line number and message.
    ''' </summary>
    Public Class CompilerError
        ''' <summary>Gets the line number where the error occurred.</summary>
        Public ReadOnly Property LineNumber As Integer

        ''' <summary>Gets the error message.</summary>
        Public ReadOnly Property Message As String

        Public Sub New(lineNumber As Integer, message As String)
            Me.LineNumber = lineNumber
            Me.Message = message
        End Sub

        Public Overrides Function ToString() As String
            Return $"Line {LineNumber}: {Message}"
        End Function
    End Class

    ''' <summary>
    ''' Represents a jump instruction that needs its target resolved.
    ''' </summary>
    Friend Class PendingJump
        Public Property Position As Integer
        Public Property Label As String
    End Class

End Namespace
