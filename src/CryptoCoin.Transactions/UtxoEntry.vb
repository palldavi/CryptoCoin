Namespace CryptoCoin.Transactions

    ''' <summary>
    ''' Represents an entry in the UTXO (Unspent Transaction Output) set.
    ''' Tracks unspent outputs available for spending.
    ''' </summary>
    Public Class UtxoEntry

        ''' <summary>
        ''' The transaction output.
        ''' </summary>
        Public Property Output As TransactionOutput

        ''' <summary>
        ''' The block height at which this UTXO was created.
        ''' </summary>
        Public Property BlockHeight As Integer

        ''' <summary>
        ''' Whether this UTXO is from a coinbase transaction.
        ''' </summary>
        Public Property IsCoinbase As Boolean

        ''' <summary>
        ''' The transaction ID that created this output.
        ''' </summary>
        Public Property TxHash As String

        ''' <summary>
        ''' The output index within the transaction.
        ''' </summary>
        Public Property OutputIndex As Integer

        ''' <summary>
        ''' Gets the outpoint reference for this UTXO.
        ''' </summary>
        Public ReadOnly Property OutPoint As OutPoint
            Get
                Return New OutPoint(TxHash, CUInt(OutputIndex))
            End Get
        End Property

        ''' <summary>
        ''' Gets the value of this UTXO in satoshis.
        ''' </summary>
        Public ReadOnly Property Value As Long
            Get
                If Output Is Nothing Then Return 0
                Return Output.Value
            End Get
        End Property

        ''' <summary>
        ''' Gets the locking script of this UTXO.
        ''' </summary>
        Public ReadOnly Property ScriptPubKey As Byte()
            Get
                If Output Is Nothing Then Return New Byte() {}
                Return Output.ScriptPubKey
            End Get
        End Property

        ''' <summary>
        ''' Checks if this coinbase UTXO is mature enough to spend.
        ''' </summary>
        Public Function IsMature(currentHeight As Integer, maturityDepth As Integer) As Boolean
            If Not IsCoinbase Then Return True
            Return (currentHeight - BlockHeight) >= maturityDepth
        End Function

        Public Sub New()
        End Sub

        Public Sub New(output As TransactionOutput, blockHeight As Integer, isCoinbase As Boolean, txHash As String, outputIndex As Integer)
            Me.Output = output
            Me.BlockHeight = blockHeight
            Me.IsCoinbase = isCoinbase
            Me.TxHash = txHash
            Me.OutputIndex = outputIndex
        End Sub

        Public Overrides Function ToString() As String
            Return $"UTXO({TxHash?.Substring(0, 8)}...:{OutputIndex}, Value={Output?.ValueInCrc} CRC, Height={BlockHeight})"
        End Function

    End Class

End Namespace
