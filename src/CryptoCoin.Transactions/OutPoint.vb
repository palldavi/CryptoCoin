Imports CryptoCoin.Cryptography

Namespace CryptoCoin.Transactions

    ''' <summary>
    ''' References a specific output of a previous transaction.
    ''' Used in transaction inputs to identify which UTXO is being spent.
    ''' </summary>
    Public Class OutPoint

        ''' <summary>
        ''' The transaction ID containing the output being referenced.
        ''' </summary>
        Public Property TxHash As String

        ''' <summary>
        ''' The index of the output within the referenced transaction.
        ''' </summary>
        Public Property OutputIndex As UInteger

        ''' <summary>
        ''' A null outpoint used for coinbase transactions.
        ''' </summary>
        Public Shared ReadOnly Null As New OutPoint(New String("0"c, 64), UInteger.MaxValue)

        Public Sub New()
            TxHash = New String("0"c, 64)
            OutputIndex = 0
        End Sub

        Public Sub New(txHash As String, outputIndex As UInteger)
            Me.TxHash = If(txHash, New String("0"c, 64))
            Me.OutputIndex = outputIndex
        End Sub

        ''' <summary>
        ''' Gets whether this is a null outpoint (coinbase).
        ''' </summary>
        Public ReadOnly Property IsNull As Boolean
            Get
                Return TxHash = New String("0"c, 64) AndAlso OutputIndex = UInteger.MaxValue
            End Get
        End Property

        ''' <summary>
        ''' Serializes the outpoint to 36 bytes (32 hash + 4 index).
        ''' </summary>
        Public Function Serialize() As Byte()
            Dim result(35) As Byte
            Dim hashBytes As Byte() = HashUtil.FromHex(TxHash.PadLeft(64, "0"c))
            Array.Copy(hashBytes, 0, result, 0, 32)
            Dim indexBytes As Byte() = BitConverter.GetBytes(OutputIndex)
            Array.Copy(indexBytes, 0, result, 32, 4)
            Return result
        End Function

        ''' <summary>
        ''' Deserializes an outpoint from 36 bytes.
        ''' </summary>
        Public Shared Function Deserialize(data As Byte()) As OutPoint
            If data Is Nothing OrElse data.Length < 36 Then
                Throw New ArgumentException("OutPoint data must be at least 36 bytes.")
            End If

            Dim hashBytes(31) As Byte
            Array.Copy(data, 0, hashBytes, 0, 32)
            Dim txHash As String = HashUtil.ToHex(hashBytes)
            Dim outputIndex As UInteger = BitConverter.ToUInt32(data, 32)

            Return New OutPoint(txHash, outputIndex)
        End Function

        ''' <summary>
        ''' Gets a unique key for this outpoint (used in UTXO set lookups).
        ''' </summary>
        Public Function ToKey() As String
            Return $"{TxHash}:{OutputIndex}"
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            Dim other As OutPoint = TryCast(obj, OutPoint)
            If other Is Nothing Then Return False
            Return String.Equals(TxHash, other.TxHash, StringComparison.OrdinalIgnoreCase) AndAlso OutputIndex = other.OutputIndex
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return TxHash.GetHashCode() Xor CInt(OutputIndex)
        End Function

        Public Overrides Function ToString() As String
            If IsNull Then Return "OutPoint(Null/Coinbase)"
            Return $"OutPoint({TxHash.Substring(0, 8)}...:{OutputIndex})"
        End Function

    End Class

End Namespace
