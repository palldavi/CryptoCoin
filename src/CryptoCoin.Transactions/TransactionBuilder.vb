Imports CryptoCoin.Cryptography

Namespace CryptoCoin.Transactions

    ''' <summary>
    ''' Fluent builder for constructing and signing CryptoCoin transactions.
    ''' </summary>
    Public Class TransactionBuilder

        Private ReadOnly _inputs As New List(Of BuilderInput)()
        Private ReadOnly _outputs As New List(Of TransactionOutput)()
        Private _changeAddress As String
        Private _feePerByte As Long = 1
        Private _lockTime As UInteger = 0

        ''' <summary>
        ''' Adds an input to spend from a UTXO.
        ''' </summary>
        Public Function AddInput(txHash As String, outputIndex As Integer, value As Long, scriptPubKey As Byte(), privateKey As KeyPair) As TransactionBuilder
            Dim input As New BuilderInput()
            input.TxHash = txHash
            input.OutputIndex = outputIndex
            input.Value = value
            input.ScriptPubKey = scriptPubKey
            input.PrivateKey = privateKey
            _inputs.Add(input)
            Return Me
        End Function

        ''' <summary>
        ''' Adds an input from a UTXO entry.
        ''' </summary>
        Public Function AddInput(utxo As UtxoEntry, privateKey As KeyPair) As TransactionBuilder
            Return AddInput(utxo.TxHash, utxo.OutputIndex, utxo.Value, utxo.ScriptPubKey, privateKey)
        End Function

        ''' <summary>
        ''' Adds an output (recipient).
        ''' </summary>
        Public Function AddOutput(address As String, value As Long) As TransactionBuilder
            Dim output As New TransactionOutput()
            output.Value = value
            output.ScriptPubKey = Script.StandardScripts.CreateP2PKHOutput(address)
            _outputs.Add(output)
            Return Me
        End Function

        ''' <summary>
        ''' Adds a data output (OP_RETURN).
        ''' </summary>
        Public Function AddDataOutput(data As Byte()) As TransactionBuilder
            Dim output As New TransactionOutput()
            output.Value = 0
            output.ScriptPubKey = Script.StandardScripts.CreateNullDataOutput(data)
            _outputs.Add(output)
            Return Me
        End Function

        ''' <summary>
        ''' Sets the change address for leftover funds.
        ''' </summary>
        Public Function SetChangeAddress(address As String) As TransactionBuilder
            _changeAddress = address
            Return Me
        End Function

        ''' <summary>
        ''' Sets the fee rate in satoshis per byte.
        ''' </summary>
        Public Function SetFeePerByte(feePerByte As Long) As TransactionBuilder
            _feePerByte = feePerByte
            Return Me
        End Function

        ''' <summary>
        ''' Sets the lock time.
        ''' </summary>
        Public Function SetLockTime(lockTime As UInteger) As TransactionBuilder
            _lockTime = lockTime
            Return Me
        End Function

        ''' <summary>
        ''' Builds and signs the transaction.
        ''' </summary>
        Public Function Build() As Transaction
            ' Calculate totals
            Dim totalInput As Long = 0
            For Each input As BuilderInput In _inputs
                totalInput += input.Value
            Next

            Dim totalOutput As Long = 0
            For Each output As TransactionOutput In _outputs
                totalOutput += output.Value
            Next

            ' Estimate transaction size for fee calculation
            Dim estimatedSize As Integer = EstimateSize()
            Dim fee As Long = estimatedSize * _feePerByte

            ' Calculate change
            Dim change As Long = totalInput - totalOutput - fee
            If change < 0 Then
                Throw New InvalidOperationException(
                    $"Insufficient funds. Need {totalOutput + fee} satoshis, have {totalInput}.")
            End If

            ' Create transaction
            Dim tx As New Transaction()
            tx.Version = 1
            tx.LockTime = _lockTime

            ' Add inputs (unsigned initially)
            For Each builderInput As BuilderInput In _inputs
                Dim txInput As New TransactionInput()
                txInput.PreviousOutput = New OutPoint(builderInput.TxHash, CUInt(builderInput.OutputIndex))
                txInput.ScriptSig = New Byte() {}
                txInput.Sequence = &HFFFFFFFFUI
                tx.Inputs.Add(txInput)
            Next

            ' Add outputs
            For Each output As TransactionOutput In _outputs
                tx.Outputs.Add(output)
            Next

            ' Add change output if significant
            If change > 546 Then ' Dust threshold
                If String.IsNullOrEmpty(_changeAddress) Then
                    Throw New InvalidOperationException("Change address required when change exceeds dust threshold.")
                End If
                Dim changeOutput As New TransactionOutput()
                changeOutput.Value = change
                changeOutput.ScriptPubKey = Script.StandardScripts.CreateP2PKHOutput(_changeAddress)
                tx.Outputs.Add(changeOutput)
            End If

            ' Sign inputs
            For i As Integer = 0 To _inputs.Count - 1
                Dim builderInput As BuilderInput = _inputs(i)
                Dim sigHash As Byte() = tx.GetSignatureHash(i, builderInput.ScriptPubKey, 1) ' SIGHASH_ALL

                ' Sign
                Dim signature As EcdsaSignature = EcdsaSigner.Sign(sigHash, builderInput.PrivateKey)
                Dim derSig As Byte() = signature.ToDer()

                ' Append hash type byte
                Dim sigWithHashType(derSig.Length) As Byte
                Array.Copy(derSig, sigWithHashType, derSig.Length)
                sigWithHashType(derSig.Length) = 1 ' SIGHASH_ALL

                ' Create scriptSig: <sig> <pubkey>
                tx.Inputs(i).ScriptSig = Script.StandardScripts.CreateP2PKHInput(
                    sigWithHashType, builderInput.PrivateKey.CompressedPublicKey)
            Next

            Return tx
        End Function

        ''' <summary>
        ''' Estimates the transaction size in bytes.
        ''' </summary>
        Private Function EstimateSize() As Integer
            ' Version (4) + input count varint (1-3) + outputs count varint (1-3) + locktime (4)
            Dim size As Integer = 10

            ' Each input: outpoint (36) + scriptSig (~107 for P2PKH) + sequence (4)
            size += _inputs.Count * 148

            ' Each output: value (8) + scriptPubKey (~25 for P2PKH)
            size += (_outputs.Count + 1) * 34 ' +1 for potential change output

            Return size
        End Function

        ''' <summary>
        ''' Gets the estimated fee for this transaction.
        ''' </summary>
        Public Function EstimateFee() As Long
            Return CLng(EstimateSize()) * _feePerByte
        End Function

        Private Class BuilderInput
            Public Property TxHash As String
            Public Property OutputIndex As Integer
            Public Property Value As Long
            Public Property ScriptPubKey As Byte()
            Public Property PrivateKey As KeyPair
        End Class

    End Class

End Namespace
