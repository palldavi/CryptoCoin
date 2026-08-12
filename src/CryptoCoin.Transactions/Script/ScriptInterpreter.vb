Imports CryptoCoin.Cryptography

Namespace CryptoCoin.Transactions.Script

    ''' <summary>
    ''' Interprets and executes CryptoCoin transaction scripts.
    ''' Validates that spending conditions are met.
    ''' </summary>
    Public Class ScriptInterpreter

        Private ReadOnly _stack As New Stack(Of Byte())()
        Private ReadOnly _altStack As New Stack(Of Byte())()
        Private _error As String = ""

        ''' <summary>
        ''' Gets the last error message if execution failed.
        ''' </summary>
        Public ReadOnly Property ErrorMessage As String
            Get
                Return _error
            End Get
        End Property

        ''' <summary>
        ''' Gets the current stack depth.
        ''' </summary>
        Public ReadOnly Property StackDepth As Integer
            Get
                Return _stack.Count
            End Get
        End Property

        ''' <summary>
        ''' Verifies a transaction input by executing scriptSig + scriptPubKey.
        ''' </summary>
        Public Function Verify(scriptSig As Byte(), scriptPubKey As Byte(), tx As Transaction, inputIndex As Integer) As Boolean
            _stack.Clear()
            _altStack.Clear()
            _error = ""

            ' Execute scriptSig
            If Not Execute(scriptSig, tx, inputIndex) Then
                Return False
            End If

            ' Save stack for P2SH
            Dim savedStack As New Stack(Of Byte())(_stack.Reverse())

            ' Execute scriptPubKey
            If Not Execute(scriptPubKey, tx, inputIndex) Then
                Return False
            End If

            ' Check result
            If _stack.Count = 0 Then
                _error = "Stack empty after execution."
                Return False
            End If

            Dim result As Byte() = _stack.Pop()
            If Not IsTrue(result) Then
                _error = "Script evaluated to false."
                Return False
            End If

            ' P2SH validation
            If StandardScripts.GetOutputType(scriptPubKey) = ScriptOutputType.P2SH Then
                ' Restore stack from scriptSig execution
                _stack.Clear()
                For Each item As Byte() In savedStack
                    _stack.Push(item)
                Next

                If _stack.Count = 0 Then
                    _error = "P2SH: no serialized script on stack."
                    Return False
                End If

                Dim serializedScript As Byte() = _stack.Pop()
                If Not Execute(serializedScript, tx, inputIndex) Then
                    Return False
                End If

                If _stack.Count = 0 OrElse Not IsTrue(_stack.Pop()) Then
                    _error = "P2SH script evaluated to false."
                    Return False
                End If
            End If

            Return True
        End Function

        ''' <summary>
        ''' Executes a script.
        ''' </summary>
        Private Function Execute(scriptBytes As Byte(), tx As Transaction, inputIndex As Integer) As Boolean
            Dim script As New Script(scriptBytes)

            For Each op As ScriptOp In script.Operations
                If op.IsPushData Then
                    _stack.Push(op.Data)
                Else
                    If Not ExecuteOp(op.OpCode, tx, inputIndex, scriptBytes) Then
                        Return False
                    End If
                End If
            Next

            Return True
        End Function

        Private Function ExecuteOp(opcode As Byte, tx As Transaction, inputIndex As Integer, scriptBytes As Byte()) As Boolean
            Select Case opcode
                Case OpCodes.OP_0
                    _stack.Push(New Byte() {})

                Case OpCodes.OP_1 To OpCodes.OP_16
                    _stack.Push(New Byte() {CByte(opcode - OpCodes.OP_1 + 1)})

                Case OpCodes.OP_1NEGATE
                    _stack.Push(New Byte() {&H81})

                Case OpCodes.OP_NOP
                    ' Do nothing

                Case OpCodes.OP_DUP
                    If _stack.Count < 1 Then
                        _error = "OP_DUP: stack underflow." : Return False
                    End If
                    _stack.Push(CType(_stack.Peek().Clone(), Byte()))

                Case OpCodes.OP_DROP
                    If _stack.Count < 1 Then
                        _error = "OP_DROP: stack underflow." : Return False
                    End If
                    _stack.Pop()

                Case OpCodes.OP_SWAP
                    If _stack.Count < 2 Then
                        _error = "OP_SWAP: stack underflow." : Return False
                    End If
                    Dim a As Byte() = _stack.Pop()
                    Dim b As Byte() = _stack.Pop()
                    _stack.Push(a)
                    _stack.Push(b)

                Case OpCodes.OP_OVER
                    If _stack.Count < 2 Then
                        _error = "OP_OVER: stack underflow." : Return False
                    End If
                    Dim items As Byte()() = _stack.ToArray()
                    _stack.Push(CType(items(1).Clone(), Byte()))

                Case OpCodes.OP_EQUAL
                    If _stack.Count < 2 Then
                        _error = "OP_EQUAL: stack underflow." : Return False
                    End If
                    Dim eq1 As Byte() = _stack.Pop()
                    Dim eq2 As Byte() = _stack.Pop()
                    _stack.Push(If(BytesEqual(eq1, eq2), New Byte() {1}, New Byte() {}))

                Case OpCodes.OP_EQUALVERIFY
                    If _stack.Count < 2 Then
                        _error = "OP_EQUALVERIFY: stack underflow." : Return False
                    End If
                    Dim ev1 As Byte() = _stack.Pop()
                    Dim ev2 As Byte() = _stack.Pop()
                    If Not BytesEqual(ev1, ev2) Then
                        _error = "OP_EQUALVERIFY: values not equal." : Return False
                    End If

                Case OpCodes.OP_VERIFY
                    If _stack.Count < 1 Then
                        _error = "OP_VERIFY: stack underflow." : Return False
                    End If
                    If Not IsTrue(_stack.Pop()) Then
                        _error = "OP_VERIFY: false." : Return False
                    End If

                Case OpCodes.OP_RETURN
                    _error = "OP_RETURN: script terminated." : Return False

                Case OpCodes.OP_HASH160
                    If _stack.Count < 1 Then
                        _error = "OP_HASH160: stack underflow." : Return False
                    End If
                    Dim data As Byte() = _stack.Pop()
                    _stack.Push(HashUtil.Hash160(data))

                Case OpCodes.OP_HASH256
                    If _stack.Count < 1 Then
                        _error = "OP_HASH256: stack underflow." : Return False
                    End If
                    Dim h256Data As Byte() = _stack.Pop()
                    _stack.Push(HashUtil.DoubleSha256(h256Data))

                Case OpCodes.OP_SHA256
                    If _stack.Count < 1 Then
                        _error = "OP_SHA256: stack underflow." : Return False
                    End If
                    Dim shaData As Byte() = _stack.Pop()
                    _stack.Push(HashUtil.Sha256(shaData))

                Case OpCodes.OP_CHECKSIG
                    If _stack.Count < 2 Then
                        _error = "OP_CHECKSIG: stack underflow." : Return False
                    End If
                    Dim pubKey As Byte() = _stack.Pop()
                    Dim sig As Byte() = _stack.Pop()
                    Dim sigValid As Boolean = VerifySignature(sig, pubKey, tx, inputIndex, scriptBytes)
                    _stack.Push(If(sigValid, New Byte() {1}, New Byte() {}))

                Case OpCodes.OP_CHECKSIGVERIFY
                    If _stack.Count < 2 Then
                        _error = "OP_CHECKSIGVERIFY: stack underflow." : Return False
                    End If
                    Dim csvPubKey As Byte() = _stack.Pop()
                    Dim csvSig As Byte() = _stack.Pop()
                    If Not VerifySignature(csvSig, csvPubKey, tx, inputIndex, scriptBytes) Then
                        _error = "OP_CHECKSIGVERIFY: signature invalid." : Return False
                    End If

                Case OpCodes.OP_ADD
                    If _stack.Count < 2 Then
                        _error = "OP_ADD: stack underflow." : Return False
                    End If
                    Dim addB As Long = DecodeNumber(_stack.Pop())
                    Dim addA As Long = DecodeNumber(_stack.Pop())
                    _stack.Push(EncodeNumber(addA + addB))

                Case OpCodes.OP_SUB
                    If _stack.Count < 2 Then
                        _error = "OP_SUB: stack underflow." : Return False
                    End If
                    Dim subB As Long = DecodeNumber(_stack.Pop())
                    Dim subA As Long = DecodeNumber(_stack.Pop())
                    _stack.Push(EncodeNumber(subA - subB))

                Case OpCodes.OP_TOALTSTACK
                    If _stack.Count < 1 Then
                        _error = "OP_TOALTSTACK: stack underflow." : Return False
                    End If
                    _altStack.Push(_stack.Pop())

                Case OpCodes.OP_FROMALTSTACK
                    If _altStack.Count < 1 Then
                        _error = "OP_FROMALTSTACK: alt stack underflow." : Return False
                    End If
                    _stack.Push(_altStack.Pop())

                Case Else
                    ' Unknown opcode - treat as NOP for forward compatibility
            End Select

            Return True
        End Function

        Private Function VerifySignature(sig As Byte(), pubKey As Byte(), tx As Transaction, inputIndex As Integer, subscript As Byte()) As Boolean
            Try
                If sig.Length < 1 Then Return False

                ' Last byte is hash type
                Dim hashType As Integer = sig(sig.Length - 1)
                Dim sigWithoutHashType(sig.Length - 2) As Byte
                Array.Copy(sig, sigWithoutHashType, sig.Length - 1)

                ' Get signature hash
                Dim sigHash As Byte() = tx.GetSignatureHash(inputIndex, subscript, hashType)

                ' Parse DER signature
                Dim ecSig As EcdsaSignature = EcdsaSignature.FromDer(sigWithoutHashType)

                ' Parse public key
                Dim point As EcPoint = EcPoint.FromBytes(pubKey)

                ' Verify
                Return EcdsaSigner.Verify(sigHash, ecSig, point)
            Catch
                Return False
            End Try
        End Function

        Private Shared Function IsTrue(data As Byte()) As Boolean
            If data Is Nothing OrElse data.Length = 0 Then Return False
            For i As Integer = 0 To data.Length - 1
                If data(i) <> 0 Then
                    ' Negative zero
                    If i = data.Length - 1 AndAlso data(i) = &H80 Then Return False
                    Return True
                End If
            Next
            Return False
        End Function

        Private Shared Function BytesEqual(a As Byte(), b As Byte()) As Boolean
            If a Is Nothing AndAlso b Is Nothing Then Return True
            If a Is Nothing OrElse b Is Nothing Then Return False
            If a.Length <> b.Length Then Return False
            For i As Integer = 0 To a.Length - 1
                If a(i) <> b(i) Then Return False
            Next
            Return True
        End Function

        Private Shared Function DecodeNumber(data As Byte()) As Long
            If data Is Nothing OrElse data.Length = 0 Then Return 0
            ' Little-endian with sign bit in MSB of last byte
            Dim result As Long = 0
            For i As Integer = 0 To data.Length - 1
                result = result Or (CLng(data(i)) << (8 * i))
            Next
            If (data(data.Length - 1) And &H80) <> 0 Then
                result = result And Not (CLng(&H80) << (8 * (data.Length - 1)))
                result = -result
            End If
            Return result
        End Function

        Private Shared Function EncodeNumber(value As Long) As Byte()
            If value = 0 Then Return New Byte() {}
            Dim negative As Boolean = value < 0
            Dim absValue As Long = Math.Abs(value)
            Dim result As New List(Of Byte)()
            While absValue > 0
                result.Add(CByte(absValue And &HFF))
                absValue >>= 8
            End While
            If (result(result.Count - 1) And &H80) <> 0 Then
                result.Add(If(negative, CByte(&H80), CByte(0)))
            ElseIf negative Then
                result(result.Count - 1) = CByte(result(result.Count - 1) Or &H80)
            End If
            Return result.ToArray()
        End Function

    End Class

End Namespace
