Namespace CryptoCoin.Transactions

    ''' <summary>
    ''' Represents an input to a transaction.
    ''' Each input references a previous unspent transaction output (UTXO) being spent.
    ''' </summary>
    Public Class TransactionInput

        ''' <summary>
        ''' Reference to the previous transaction output being spent.
        ''' </summary>
        Public Property PreviousOutput As OutPoint

        ''' <summary>
        ''' The unlocking script (signature + public key) that proves ownership.
        ''' </summary>
        Public Property ScriptSig As Byte()

        ''' <summary>
        ''' Sequence number. Used for relative lock-time (BIP 68).
        ''' Default is 0xFFFFFFFF (no relative lock-time).
        ''' </summary>
        Public Property Sequence As UInteger = &HFFFFFFFFUI

        Public Sub New()
            PreviousOutput = New OutPoint()
            ScriptSig = New Byte() {}
        End Sub

        Public Sub New(previousOutput As OutPoint, scriptSig As Byte())
            Me.PreviousOutput = previousOutput
            Me.ScriptSig = If(scriptSig, New Byte() {})
        End Sub

        ''' <summary>
        ''' Gets whether this input is a coinbase input (no previous output).
        ''' </summary>
        Public ReadOnly Property IsCoinbase As Boolean
            Get
                Return PreviousOutput.IsNull
            End Get
        End Property

        ''' <summary>
        ''' Gets whether this input has relative lock-time enabled.
        ''' </summary>
        Public ReadOnly Property HasRelativeLockTime As Boolean
            Get
                Return Sequence <> &HFFFFFFFFUI
            End Get
        End Property

        ''' <summary>
        ''' Gets the serialized size of this input.
        ''' </summary>
        Public ReadOnly Property Size As Integer
            Get
                ' OutPoint (36) + VarInt(scriptLen) + script + sequence (4)
                Return 36 + Core.Serialization.VarInt.GetEncodedSize(ScriptSig.Length) + ScriptSig.Length + 4
            End Get
        End Property

        Public Overrides Function ToString() As String
            If IsCoinbase Then
                Return "TxInput(Coinbase)"
            End If
            Return $"TxInput(PrevOut={PreviousOutput})"
        End Function

    End Class

End Namespace
