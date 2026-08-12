Imports CryptoCoin.Core

Namespace CryptoCoin.Transactions

    ''' <summary>
    ''' Validates transactions against consensus rules and UTXO state.
    ''' </summary>
    Public Class TransactionValidator

        Private ReadOnly _utxoSet As UtxoSet
        Private ReadOnly _params As ChainParameters

        Public Sub New(utxoSet As UtxoSet, params As ChainParameters)
            If utxoSet Is Nothing Then Throw New ArgumentNullException(NameOf(utxoSet))
            If params Is Nothing Then Throw New ArgumentNullException(NameOf(params))
            _utxoSet = utxoSet
            _params = params
        End Sub

        ''' <summary>
        ''' Validates a transaction for inclusion in a block or mempool.
        ''' </summary>
        Public Function Validate(tx As Transaction, currentHeight As Integer) As TransactionValidationResult
            Dim result As New TransactionValidationResult()

            ' Basic structure checks
            ValidateStructure(tx, result)
            If Not result.IsValid Then Return result

            ' Skip UTXO checks for coinbase
            If tx.IsCoinbase Then
                ValidateCoinbase(tx, currentHeight, result)
                Return result
            End If

            ' UTXO and value checks
            ValidateInputs(tx, currentHeight, result)

            Return result
        End Function

        Private Sub ValidateStructure(tx As Transaction, result As TransactionValidationResult)
            ' Must have inputs and outputs
            If tx.Inputs Is Nothing OrElse tx.Inputs.Count = 0 Then
                result.AddError("Transaction has no inputs.")
                Return
            End If

            If tx.Outputs Is Nothing OrElse tx.Outputs.Count = 0 Then
                result.AddError("Transaction has no outputs.")
                Return
            End If

            ' Check for negative output values
            Dim totalOutput As Long = 0
            For Each output As TransactionOutput In tx.Outputs
                If output.Value < 0 Then
                    result.AddError("Output value cannot be negative.")
                End If
                If output.Value > _params.MaxSupply Then
                    result.AddError("Output value exceeds maximum supply.")
                End If
                totalOutput += output.Value
                If totalOutput > _params.MaxSupply Then
                    result.AddError("Total output value exceeds maximum supply.")
                End If
            Next

            ' Check transaction size
            If tx.Size > _params.MaxBlockSize Then
                result.AddError("Transaction too large.")
            End If

            ' Check for duplicate inputs
            Dim seen As New HashSet(Of String)()
            For Each input As TransactionInput In tx.Inputs
                Dim key As String = input.PreviousOutput.ToKey()
                If Not seen.Add(key) Then
                    result.AddError($"Duplicate input: {key}")
                End If
            Next
        End Sub

        Private Sub ValidateCoinbase(tx As Transaction, currentHeight As Integer, result As TransactionValidationResult)
            ' Coinbase must have exactly one input
            If tx.Inputs.Count <> 1 Then
                result.AddError("Coinbase must have exactly one input.")
            End If

            ' Coinbase scriptSig size limits (2-100 bytes)
            Dim scriptLen As Integer = tx.Inputs(0).ScriptSig.Length
            If scriptLen < 2 OrElse scriptLen > 100 Then
                result.AddError("Coinbase scriptSig must be 2-100 bytes.")
            End If

            ' Validate reward amount
            Dim maxReward As Long = _params.GetBlockReward(currentHeight)
            If tx.TotalOutputValue > maxReward Then
                ' Note: In full implementation, would add fees to allowed reward
                result.AddWarning($"Coinbase reward ({tx.TotalOutputValue}) may exceed allowed ({maxReward} + fees).")
            End If
        End Sub

        Private Sub ValidateInputs(tx As Transaction, currentHeight As Integer, result As TransactionValidationResult)
            Dim totalInput As Long = 0

            For Each input As TransactionInput In tx.Inputs
                ' Check UTXO exists
                Dim utxo As UtxoEntry = _utxoSet.Get(input.PreviousOutput)
                If utxo Is Nothing Then
                    result.AddError($"Input references non-existent UTXO: {input.PreviousOutput.ToKey()}")
                    Continue For
                End If

                ' Check coinbase maturity
                If utxo.IsCoinbase AndAlso Not utxo.IsMature(currentHeight, _params.CoinbaseMaturity) Then
                    result.AddError($"Coinbase UTXO not yet mature: {input.PreviousOutput.ToKey()}")
                End If

                totalInput += utxo.Value
            Next

            ' Check that inputs >= outputs (difference is fee)
            If totalInput < tx.TotalOutputValue Then
                result.AddError($"Input value ({totalInput}) less than output value ({tx.TotalOutputValue}).")
            Else
                result.Fee = totalInput - tx.TotalOutputValue
            End If

            ' Validate minimum fee
            Dim minFee As Long = CLng(tx.Size) * _params.MinFeePerByte
            If result.Fee < minFee Then
                result.AddWarning($"Fee ({result.Fee}) below minimum ({minFee}).")
            End If
        End Sub

        ''' <summary>
        ''' Validates transaction scripts (signature verification).
        ''' </summary>
        Public Function ValidateScripts(tx As Transaction) As TransactionValidationResult
            Dim result As New TransactionValidationResult()
            If tx.IsCoinbase Then Return result

            Dim interpreter As New Script.ScriptInterpreter()

            For i As Integer = 0 To tx.Inputs.Count - 1
                Dim input As TransactionInput = tx.Inputs(i)
                Dim utxo As UtxoEntry = _utxoSet.Get(input.PreviousOutput)
                If utxo Is Nothing Then
                    result.AddError($"Cannot validate script: UTXO not found for input {i}.")
                    Continue For
                End If

                Dim valid As Boolean = interpreter.Verify(input.ScriptSig, utxo.ScriptPubKey, tx, i)
                If Not valid Then
                    result.AddError($"Script validation failed for input {i}: {interpreter.ErrorMessage}")
                End If
            Next

            Return result
        End Function

    End Class

    ''' <summary>
    ''' Result of transaction validation.
    ''' </summary>
    Public Class TransactionValidationResult

        Public Property Errors As List(Of String)
        Public Property Warnings As List(Of String)
        Public Property Fee As Long

        Public ReadOnly Property IsValid As Boolean
            Get
                Return Errors.Count = 0
            End Get
        End Property

        Public Sub New()
            Errors = New List(Of String)()
            Warnings = New List(Of String)()
        End Sub

        Public Sub AddError(message As String)
            Errors.Add(message)
        End Sub

        Public Sub AddWarning(message As String)
            Warnings.Add(message)
        End Sub

        Public Overrides Function ToString() As String
            If IsValid Then Return $"Valid (Fee={Fee})"
            Return $"Invalid: {String.Join("; ", Errors)}"
        End Function

    End Class

End Namespace
