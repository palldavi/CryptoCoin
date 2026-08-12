' ===============================================================================
' CryptoCoin.Contracts - VirtualMachine.vb
' Stack-based virtual machine for executing smart contracts with gas metering.
' ===============================================================================

Imports System
Imports System.Collections.Generic
Imports System.Numerics
Imports CryptoCoin.Cryptography

Namespace CryptoCoin.Contracts

    ''' <summary>
    ''' Stack-based virtual machine for executing CryptoCoin smart contracts.
    ''' Supports arithmetic, logic, cryptographic, storage, and control flow operations
    ''' with gas metering to prevent infinite loops and resource abuse.
    ''' </summary>
    Public Class VirtualMachine

        Private ReadOnly _stack As Stack(Of Byte())
        Private ReadOnly _memory As Dictionary(Of Integer, Byte())
        Private ReadOnly _storage As ContractStorage
        Private ReadOnly _gasCalculator As GasCalculator
        Private _programCounter As Integer
        Private _gasUsed As Long
        Private _gasLimit As Long
        Private _isRunning As Boolean
        Private _returnData As Byte()
        Private _reverted As Boolean

        Private Const MaxStackSize As Integer = 1024
        Private Const MaxMemorySize As Integer = 1024 * 1024 ' 1MB

        ''' <summary>Gets the current program counter position.</summary>
        Public ReadOnly Property ProgramCounter As Integer
            Get
                Return _programCounter
            End Get
        End Property

        ''' <summary>Gets the total gas consumed during execution.</summary>
        Public ReadOnly Property GasUsed As Long
            Get
                Return _gasUsed
            End Get
        End Property

        ''' <summary>Gets the remaining gas available.</summary>
        Public ReadOnly Property GasRemaining As Long
            Get
                Return _gasLimit - _gasUsed
            End Get
        End Property

        ''' <summary>Gets the current stack depth.</summary>
        Public ReadOnly Property StackDepth As Integer
            Get
                Return _stack.Count
            End Get
        End Property

        ''' <summary>Gets the return data from the last execution.</summary>
        Public ReadOnly Property ReturnData As Byte()
            Get
                Return _returnData
            End Get
        End Property

        ''' <summary>Gets whether the last execution was reverted.</summary>
        Public ReadOnly Property WasReverted As Boolean
            Get
                Return _reverted
            End Get
        End Property

        ''' <summary>Gets the execution context for environment opcodes.</summary>
        Public Property Context As ExecutionContext

        ''' <summary>
        ''' Initializes a new VirtualMachine with the specified storage and gas limit.
        ''' </summary>
        ''' <param name="storage">The contract storage instance.</param>
        ''' <param name="gasLimit">The maximum gas allowed for execution.</param>
        Public Sub New(storage As ContractStorage, gasLimit As Long)
            _stack = New Stack(Of Byte())()
            _memory = New Dictionary(Of Integer, Byte())()
            _storage = storage
            _gasCalculator = New GasCalculator()
            _gasLimit = gasLimit
            _gasUsed = 0
            _programCounter = 0
            _isRunning = False
            _reverted = False
            Context = New ExecutionContext()
        End Sub

        ''' <summary>
        ''' Executes the given bytecode program.
        ''' </summary>
        ''' <param name="bytecode">The compiled contract bytecode to execute.</param>
        ''' <returns>An ExecutionResult containing the outcome of execution.</returns>
        Public Function Execute(bytecode As Byte()) As ExecutionResult
            If bytecode Is Nothing OrElse bytecode.Length = 0 Then
                Return New ExecutionResult(False, Nothing, 0, "Empty bytecode")
            End If

            _programCounter = 0
            _isRunning = True
            _reverted = False
            _returnData = Nothing

            Try
                While _isRunning AndAlso _programCounter < bytecode.Length
                    Dim opCode As ContractOpCode = CType(bytecode(_programCounter), ContractOpCode)

                    ' Consume gas for this operation
                    Dim gasCost As Long = _gasCalculator.GetCost(opCode)
                    ConsumeGas(gasCost)

                    ' Execute the opcode
                    ExecuteOpCode(opCode, bytecode)

                    ' Advance program counter (unless jump occurred)
                    _programCounter += 1
                End While

                If _reverted Then
                    Return New ExecutionResult(False, _returnData, _gasUsed, "Execution reverted")
                End If

                Return New ExecutionResult(True, _returnData, _gasUsed, Nothing)

            Catch ex As OutOfGasException
                Return New ExecutionResult(False, Nothing, _gasUsed, "Out of gas")
            Catch ex As StackOverflowException
                Return New ExecutionResult(False, Nothing, _gasUsed, "Stack overflow")
            Catch ex As InvalidOperationException
                Return New ExecutionResult(False, Nothing, _gasUsed, ex.Message)
            Catch ex As Exception
                Return New ExecutionResult(False, Nothing, _gasUsed, $"VM error: {ex.Message}")
            End Try
        End Function

        ''' <summary>
        ''' Executes a single opcode instruction.
        ''' </summary>
        Private Sub ExecuteOpCode(opCode As ContractOpCode, bytecode As Byte())
            Select Case opCode
                Case ContractOpCode.NOP
                    ' Do nothing

                Case ContractOpCode.PUSH1
                    _programCounter += 1
                    Push(New Byte() {bytecode(_programCounter)})

                Case ContractOpCode.PUSH4
                    Dim data(3) As Byte
                    Array.Copy(bytecode, _programCounter + 1, data, 0, 4)
                    _programCounter += 4
                    Push(data)

                Case ContractOpCode.PUSH8
                    Dim data(7) As Byte
                    Array.Copy(bytecode, _programCounter + 1, data, 0, 8)
                    _programCounter += 8
                    Push(data)

                Case ContractOpCode.PUSH32
                    Dim data(31) As Byte
                    Array.Copy(bytecode, _programCounter + 1, data, 0, 32)
                    _programCounter += 32
                    Push(data)

                Case ContractOpCode.POP
                    Pop()

                Case ContractOpCode.DUP
                    Dim top As Byte() = Peek()
                    Push(CType(top.Clone(), Byte()))

                Case ContractOpCode.SWAP
                    Dim a As Byte() = Pop()
                    Dim b As Byte() = Pop()
                    Push(a)
                    Push(b)

                Case ContractOpCode.ROT
                    Dim a As Byte() = Pop()
                    Dim b As Byte() = Pop()
                    Dim c As Byte() = Pop()
                    Push(b)
                    Push(a)
                    Push(c)

                ' Arithmetic
                Case ContractOpCode.ADD
                    Dim a As BigInteger = PopBigInteger()
                    Dim b As BigInteger = PopBigInteger()
                    PushBigInteger(b + a)

                Case ContractOpCode.[SUB]
                    Dim a As BigInteger = PopBigInteger()
                    Dim b As BigInteger = PopBigInteger()
                    PushBigInteger(b - a)

                Case ContractOpCode.MUL
                    Dim a As BigInteger = PopBigInteger()
                    Dim b As BigInteger = PopBigInteger()
                    PushBigInteger(b * a)

                Case ContractOpCode.DIV
                    Dim a As BigInteger = PopBigInteger()
                    Dim b As BigInteger = PopBigInteger()
                    If a = BigInteger.Zero Then
                        PushBigInteger(BigInteger.Zero)
                    Else
                        PushBigInteger(b / a)
                    End If

                Case ContractOpCode.[MOD]
                    Dim a As BigInteger = PopBigInteger()
                    Dim b As BigInteger = PopBigInteger()
                    If a = BigInteger.Zero Then
                        PushBigInteger(BigInteger.Zero)
                    Else
                        PushBigInteger(b Mod a)
                    End If

                Case ContractOpCode.NEG
                    PushBigInteger(-PopBigInteger())

                Case ContractOpCode.ABS
                    PushBigInteger(BigInteger.Abs(PopBigInteger()))

                Case ContractOpCode.INC
                    PushBigInteger(PopBigInteger() + BigInteger.One)

                Case ContractOpCode.DEC
                    PushBigInteger(PopBigInteger() - BigInteger.One)

                Case ContractOpCode.EXP
                    Dim exponent As BigInteger = PopBigInteger()
                    Dim base As BigInteger = PopBigInteger()
                    PushBigInteger(BigInteger.Pow(base, CInt(exponent)))

                ' Comparison
                Case ContractOpCode.EQ
                    Dim a As Byte() = Pop()
                    Dim b As Byte() = Pop()
                    PushBool(ByteArraysEqual(a, b))

                Case ContractOpCode.NEQ
                    Dim a As Byte() = Pop()
                    Dim b As Byte() = Pop()
                    PushBool(Not ByteArraysEqual(a, b))

                Case ContractOpCode.LT
                    Dim a As BigInteger = PopBigInteger()
                    Dim b As BigInteger = PopBigInteger()
                    PushBool(b < a)

                Case ContractOpCode.GT
                    Dim a As BigInteger = PopBigInteger()
                    Dim b As BigInteger = PopBigInteger()
                    PushBool(b > a)

                Case ContractOpCode.LTE
                    Dim a As BigInteger = PopBigInteger()
                    Dim b As BigInteger = PopBigInteger()
                    PushBool(b <= a)

                Case ContractOpCode.GTE
                    Dim a As BigInteger = PopBigInteger()
                    Dim b As BigInteger = PopBigInteger()
                    PushBool(b >= a)

                Case ContractOpCode.ISZERO
                    PushBool(PopBigInteger() = BigInteger.Zero)

                ' Logic
                Case ContractOpCode.[AND]
                    Dim a As BigInteger = PopBigInteger()
                    Dim b As BigInteger = PopBigInteger()
                    PushBigInteger(b And a)

                Case ContractOpCode.[OR]
                    Dim a As BigInteger = PopBigInteger()
                    Dim b As BigInteger = PopBigInteger()
                    PushBigInteger(b Or a)

                Case ContractOpCode.[XOR]
                    Dim a As BigInteger = PopBigInteger()
                    Dim b As BigInteger = PopBigInteger()
                    PushBigInteger(b Xor a)

                Case ContractOpCode.[NOT]
                    PushBigInteger(Not PopBigInteger())

                Case ContractOpCode.SHL
                    Dim shift As Integer = CInt(PopBigInteger())
                    Dim value As BigInteger = PopBigInteger()
                    PushBigInteger(value << shift)

                Case ContractOpCode.SHR
                    Dim shift As Integer = CInt(PopBigInteger())
                    Dim value As BigInteger = PopBigInteger()
                    PushBigInteger(value >> shift)

                ' Cryptographic
                Case ContractOpCode.SHA256
                    Dim data As Byte() = Pop()
                    Push(HashUtil.Sha256(data))

                Case ContractOpCode.HASH256
                    Dim data As Byte() = Pop()
                    Push(HashUtil.DoubleSha256(data))

                Case ContractOpCode.RIPEMD160
                    Dim data As Byte() = Pop()
                    Push(HashUtil.Ripemd160(data))

                Case ContractOpCode.HASH160
                    Dim data As Byte() = Pop()
                    Push(HashUtil.Hash160(data))

                ' Storage
                Case ContractOpCode.SLOAD
                    Dim key As Byte() = Pop()
                    Dim value As Byte() = _storage.Get(key)
                    Push(If(value, New Byte() {}))

                Case ContractOpCode.SSTORE
                    Dim value As Byte() = Pop()
                    Dim key As Byte() = Pop()
                    ConsumeGas(_gasCalculator.GetStorageCost(value))
                    _storage.Put(key, value)

                ' Memory
                Case ContractOpCode.MLOAD
                    Dim offset As Integer = CInt(PopBigInteger())
                    Dim value As Byte() = Nothing
                    If _memory.TryGetValue(offset, value) Then
                        Push(value)
                    Else
                        Push(New Byte(31) {})
                    End If

                Case ContractOpCode.MSTORE
                    Dim value As Byte() = Pop()
                    Dim offset As Integer = CInt(PopBigInteger())
                    _memory(offset) = value

                Case ContractOpCode.MSIZE
                    PushBigInteger(New BigInteger(_memory.Count * 32))

                ' Control Flow
                Case ContractOpCode.JUMP
                    Dim dest As Integer = CInt(PopBigInteger())
                    ValidateJumpDest(dest, bytecode)
                    _programCounter = dest - 1 ' -1 because loop increments

                Case ContractOpCode.JUMPI
                    Dim condition As BigInteger = PopBigInteger()
                    Dim dest As Integer = CInt(PopBigInteger())
                    If condition <> BigInteger.Zero Then
                        ValidateJumpDest(dest, bytecode)
                        _programCounter = dest - 1
                    End If

                Case ContractOpCode.JUMPDEST
                    ' Valid jump destination marker

                Case ContractOpCode.[RETURN]
                    _returnData = Pop()
                    _isRunning = False

                Case ContractOpCode.REVERT
                    _returnData = If(_stack.Count > 0, Pop(), Nothing)
                    _reverted = True
                    _isRunning = False

                Case ContractOpCode.HALT
                    _isRunning = False

                ' Environment
                Case ContractOpCode.CALLER
                    Push(If(Context.Caller, New Byte(19) {}))

                Case ContractOpCode.CALLVALUE
                    PushBigInteger(New BigInteger(Context.Value))

                Case ContractOpCode.ADDRESS
                    Push(If(Context.ContractAddress, New Byte(19) {}))

                Case ContractOpCode.BLOCKNUMBER
                    PushBigInteger(New BigInteger(Context.BlockNumber))

                Case ContractOpCode.TIMESTAMP
                    PushBigInteger(New BigInteger(Context.Timestamp))

                Case ContractOpCode.GAS
                    PushBigInteger(New BigInteger(GasRemaining))

                Case ContractOpCode.CALLDATASIZE
                    Dim size As Integer = If(Context.CallData IsNot Nothing, Context.CallData.Length, 0)
                    PushBigInteger(New BigInteger(size))

                Case ContractOpCode.CALLDATALOAD
                    Dim offset As Integer = CInt(PopBigInteger())
                    If Context.CallData IsNot Nothing AndAlso offset < Context.CallData.Length Then
                        Dim chunk(31) As Byte
                        Dim length As Integer = Math.Min(32, Context.CallData.Length - offset)
                        Array.Copy(Context.CallData, offset, chunk, 0, length)
                        Push(chunk)
                    Else
                        Push(New Byte(31) {})
                    End If

                Case Else
                    Throw New InvalidOperationException($"Unknown opcode: {CByte(opCode):X2}")
            End Select
        End Sub

        ' --- Stack helpers ---

        Private Sub Push(data As Byte())
            If _stack.Count >= MaxStackSize Then
                Throw New StackOverflowException("VM stack overflow")
            End If
            _stack.Push(data)
        End Sub

        Private Function Pop() As Byte()
            If _stack.Count = 0 Then
                Throw New InvalidOperationException("Stack underflow")
            End If
            Return _stack.Pop()
        End Function

        Private Function Peek() As Byte()
            If _stack.Count = 0 Then
                Throw New InvalidOperationException("Stack underflow")
            End If
            Return _stack.Peek()
        End Function

        Private Function PopBigInteger() As BigInteger
            Dim data As Byte() = Pop()
            If data.Length = 0 Then Return BigInteger.Zero
            Return New BigInteger(data)
        End Function

        Private Sub PushBigInteger(value As BigInteger)
            Push(value.ToByteArray())
        End Sub

        Private Sub PushBool(value As Boolean)
            Push(If(value, New Byte() {1}, New Byte() {0}))
        End Sub

        Private Sub ConsumeGas(amount As Long)
            _gasUsed += amount
            If _gasUsed > _gasLimit Then
                Throw New OutOfGasException($"Gas limit exceeded: used {_gasUsed}, limit {_gasLimit}")
            End If
        End Sub

        Private Sub ValidateJumpDest(dest As Integer, bytecode As Byte())
            If dest < 0 OrElse dest >= bytecode.Length Then
                Throw New InvalidOperationException($"Invalid jump destination: {dest}")
            End If
            If CType(bytecode(dest), ContractOpCode) <> ContractOpCode.JUMPDEST Then
                Throw New InvalidOperationException($"Jump to non-JUMPDEST: {dest}")
            End If
        End Sub

        Private Function ByteArraysEqual(a As Byte(), b As Byte()) As Boolean
            If a.Length <> b.Length Then Return False
            For i As Integer = 0 To a.Length - 1
                If a(i) <> b(i) Then Return False
            Next
            Return True
        End Function

    End Class

    ''' <summary>
    ''' Represents the result of a VM execution.
    ''' </summary>
    Public Class ExecutionResult
        ''' <summary>Gets whether execution completed successfully.</summary>
        Public ReadOnly Property Success As Boolean

        ''' <summary>Gets the return data from execution.</summary>
        Public ReadOnly Property ReturnData As Byte()

        ''' <summary>Gets the total gas consumed.</summary>
        Public ReadOnly Property GasUsed As Long

        ''' <summary>Gets the error message if execution failed.</summary>
        Public ReadOnly Property ErrorMessage As String

        Public Sub New(success As Boolean, returnData As Byte(), gasUsed As Long, errorMessage As String)
            Me.Success = success
            Me.ReturnData = returnData
            Me.GasUsed = gasUsed
            Me.ErrorMessage = errorMessage
        End Sub
    End Class

    ''' <summary>
    ''' Provides execution context (environment) for the VM.
    ''' </summary>
    Public Class ExecutionContext
        ''' <summary>Gets or sets the caller's address.</summary>
        Public Property Caller As Byte()

        ''' <summary>Gets or sets the value sent with the call (in satoshis).</summary>
        Public Property Value As Long

        ''' <summary>Gets or sets the contract's own address.</summary>
        Public Property ContractAddress As Byte()

        ''' <summary>Gets or sets the current block number.</summary>
        Public Property BlockNumber As Long

        ''' <summary>Gets or sets the current block timestamp.</summary>
        Public Property Timestamp As Long

        ''' <summary>Gets or sets the call data (function selector + arguments).</summary>
        Public Property CallData As Byte()
    End Class

    ''' <summary>
    ''' Exception thrown when the VM runs out of gas.
    ''' </summary>
    Public Class OutOfGasException
        Inherits Exception

        Public Sub New(message As String)
            MyBase.New(message)
        End Sub
    End Class

End Namespace
