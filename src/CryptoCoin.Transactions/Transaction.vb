Imports CryptoCoin.Cryptography
Imports CryptoCoin.Core.Serialization

Namespace CryptoCoin.Transactions

    ''' <summary>
    ''' Represents a CryptoCoin transaction that transfers value between addresses.
    ''' Uses the UTXO (Unspent Transaction Output) model.
    ''' </summary>
    Public Class Transaction

        ''' <summary>
        ''' Transaction version number.
        ''' </summary>
        Public Property Version As Integer = 1

        ''' <summary>
        ''' List of transaction inputs (references to previous outputs being spent).
        ''' </summary>
        Public Property Inputs As List(Of TransactionInput)

        ''' <summary>
        ''' List of transaction outputs (new UTXOs being created).
        ''' </summary>
        Public Property Outputs As List(Of TransactionOutput)

        ''' <summary>
        ''' Lock time - earliest time/block when this transaction can be included in a block.
        ''' 0 = no lock time.
        ''' </summary>
        Public Property LockTime As UInteger = 0

        ''' <summary>
        ''' Gets the transaction ID (double SHA-256 of the serialized transaction).
        ''' </summary>
        Public ReadOnly Property TxId As String
            Get
                Dim data As Byte() = Serialize()
                Dim hash As Byte() = HashUtil.DoubleSha256(data)
                Return HashUtil.ToHex(hash)
            End Get
        End Property

        ''' <summary>
        ''' Gets whether this is a coinbase transaction.
        ''' </summary>
        Public ReadOnly Property IsCoinbase As Boolean
            Get
                Return Inputs.Count = 1 AndAlso Inputs(0).PreviousOutput.IsNull
            End Get
        End Property

        ''' <summary>
        ''' Gets the total output value in satoshis.
        ''' </summary>
        Public ReadOnly Property TotalOutputValue As Long
            Get
                Dim total As Long = 0
                For Each output As TransactionOutput In Outputs
                    total += output.Value
                Next
                Return total
            End Get
        End Property

        ''' <summary>
        ''' Gets the serialized size of this transaction in bytes.
        ''' </summary>
        Public ReadOnly Property Size As Integer
            Get
                Return Serialize().Length
            End Get
        End Property

        Public Sub New()
            Inputs = New List(Of TransactionInput)()
            Outputs = New List(Of TransactionOutput)()
        End Sub

        ''' <summary>
        ''' Creates a coinbase transaction for a given block height and reward.
        ''' </summary>
        Public Shared Function CreateCoinbase(height As Integer, reward As Long, minerAddress As String) As Transaction
            Dim tx As New Transaction()
            tx.Version = 1

            ' Coinbase input (no previous output)
            Dim input As New TransactionInput()
            input.PreviousOutput = OutPoint.Null
            ' Coinbase script contains the block height
            input.ScriptSig = System.Text.Encoding.UTF8.GetBytes($"Height:{height}")
            input.Sequence = &HFFFFFFFFUI
            tx.Inputs.Add(input)

            ' Output to miner
            Dim output As New TransactionOutput()
            output.Value = reward
            output.ScriptPubKey = Script.StandardScripts.CreateP2PKHOutput(minerAddress)
            tx.Outputs.Add(output)

            Return tx
        End Function

        ''' <summary>
        ''' Serializes the transaction to bytes.
        ''' </summary>
        Public Function Serialize() As Byte()
            Dim writer As New BufferWriter()

            ' Version
            writer.WriteInt32(Version)

            ' Input count and inputs
            writer.WriteVarInt(Inputs.Count)
            For Each input As TransactionInput In Inputs
                writer.WriteBytes(input.PreviousOutput.Serialize())
                writer.WriteVarBytes(input.ScriptSig)
                writer.WriteUInt32(input.Sequence)
            Next

            ' Output count and outputs
            writer.WriteVarInt(Outputs.Count)
            For Each output As TransactionOutput In Outputs
                writer.WriteInt64(output.Value)
                writer.WriteVarBytes(output.ScriptPubKey)
            Next

            ' Lock time
            writer.WriteUInt32(LockTime)

            Return writer.ToArray()
        End Function

        ''' <summary>
        ''' Deserializes a transaction from bytes.
        ''' </summary>
        Public Shared Function Deserialize(data As Byte()) As Transaction
            If data Is Nothing Then Throw New ArgumentNullException(NameOf(data))

            Dim reader As New BufferReader(data)
            Dim tx As New Transaction()

            tx.Version = reader.ReadInt32()

            ' Inputs
            Dim inputCount As Integer = CInt(reader.ReadVarInt())
            For i As Integer = 0 To inputCount - 1
                Dim input As New TransactionInput()
                Dim outpointBytes As Byte() = reader.ReadBytes(36)
                input.PreviousOutput = OutPoint.Deserialize(outpointBytes)
                input.ScriptSig = reader.ReadVarBytes()
                input.Sequence = reader.ReadUInt32()
                tx.Inputs.Add(input)
            Next

            ' Outputs
            Dim outputCount As Integer = CInt(reader.ReadVarInt())
            For i As Integer = 0 To outputCount - 1
                Dim output As New TransactionOutput()
                output.Value = reader.ReadInt64()
                output.ScriptPubKey = reader.ReadVarBytes()
                tx.Outputs.Add(output)
            Next

            tx.LockTime = reader.ReadUInt32()

            Return tx
        End Function

        ''' <summary>
        ''' Gets the hash of the transaction for signing a specific input.
        ''' </summary>
        Public Function GetSignatureHash(inputIndex As Integer, subscript As Byte(), hashType As Integer) As Byte()
            If inputIndex < 0 OrElse inputIndex >= Inputs.Count Then
                Throw New ArgumentOutOfRangeException(NameOf(inputIndex))
            End If

            ' Create a copy with modified scripts for signing
            Dim txCopy As New Transaction()
            txCopy.Version = Version
            txCopy.LockTime = LockTime

            For i As Integer = 0 To Inputs.Count - 1
                Dim inputCopy As New TransactionInput()
                inputCopy.PreviousOutput = Inputs(i).PreviousOutput
                inputCopy.Sequence = Inputs(i).Sequence
                If i = inputIndex Then
                    inputCopy.ScriptSig = subscript
                Else
                    inputCopy.ScriptSig = New Byte() {}
                End If
                txCopy.Inputs.Add(inputCopy)
            Next

            For Each output As TransactionOutput In Outputs
                Dim outputCopy As New TransactionOutput()
                outputCopy.Value = output.Value
                outputCopy.ScriptPubKey = CType(output.ScriptPubKey.Clone(), Byte())
                txCopy.Outputs.Add(outputCopy)
            Next

            ' Append hash type
            Dim txData As Byte() = txCopy.Serialize()
            Dim hashTypeBytes As Byte() = BitConverter.GetBytes(hashType)
            Dim combined(txData.Length + 3) As Byte
            Array.Copy(txData, combined, txData.Length)
            Array.Copy(hashTypeBytes, 0, combined, txData.Length, 4)

            Return HashUtil.DoubleSha256(combined)
        End Function

        Public Overrides Function ToString() As String
            Return $"Transaction(TxId={TxId.Substring(0, 16)}..., Inputs={Inputs.Count}, Outputs={Outputs.Count})"
        End Function

    End Class

End Namespace
