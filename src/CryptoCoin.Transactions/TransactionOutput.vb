Namespace CryptoCoin.Transactions

    ''' <summary>
    ''' Represents an output of a transaction.
    ''' Each output specifies an amount and a locking script (conditions to spend).
    ''' </summary>
    Public Class TransactionOutput

        ''' <summary>
        ''' The value in satoshis (1 CRC = 100,000,000 satoshis).
        ''' </summary>
        Public Property Value As Long

        ''' <summary>
        ''' The locking script (scriptPubKey) that defines spending conditions.
        ''' </summary>
        Public Property ScriptPubKey As Byte()

        ''' <summary>
        ''' The index of this output within its transaction.
        ''' </summary>
        Public Property Index As Integer

        Public Sub New()
            ScriptPubKey = New Byte() {}
        End Sub

        Public Sub New(value As Long, scriptPubKey As Byte())
            Me.Value = value
            Me.ScriptPubKey = If(scriptPubKey, New Byte() {})
        End Sub

        ''' <summary>
        ''' Gets the value in CRC (floating point for display).
        ''' </summary>
        Public ReadOnly Property ValueInCrc As Decimal
            Get
                Return CDec(Value) / 100000000D
            End Get
        End Property

        ''' <summary>
        ''' Gets whether this output is provably unspendable (OP_RETURN).
        ''' </summary>
        Public ReadOnly Property IsUnspendable As Boolean
            Get
                Return ScriptPubKey IsNot Nothing AndAlso ScriptPubKey.Length > 0 AndAlso ScriptPubKey(0) = Script.OpCodes.OP_RETURN
            End Get
        End Property

        ''' <summary>
        ''' Gets the serialized size of this output.
        ''' </summary>
        Public ReadOnly Property Size As Integer
            Get
                ' Value (8) + VarInt(scriptLen) + script
                Return 8 + Core.Serialization.VarInt.GetEncodedSize(ScriptPubKey.Length) + ScriptPubKey.Length
            End Get
        End Property

        ''' <summary>
        ''' Gets the type of script (P2PKH, P2SH, etc.).
        ''' </summary>
        Public ReadOnly Property ScriptType As ScriptOutputType
            Get
                Return Script.StandardScripts.GetOutputType(ScriptPubKey)
            End Get
        End Property

        Public Overrides Function ToString() As String
            Return $"TxOutput(Value={ValueInCrc} CRC, ScriptLen={ScriptPubKey.Length})"
        End Function

    End Class

    ''' <summary>
    ''' Types of standard output scripts.
    ''' </summary>
    Public Enum ScriptOutputType
        ''' <summary>Pay to Public Key Hash (most common).</summary>
        P2PKH
        ''' <summary>Pay to Script Hash.</summary>
        P2SH
        ''' <summary>Pay to Public Key (legacy).</summary>
        P2PK
        ''' <summary>Multi-signature.</summary>
        MultiSig
        ''' <summary>Data carrier (OP_RETURN).</summary>
        NullData
        ''' <summary>Non-standard script.</summary>
        NonStandard
    End Enum

End Namespace
