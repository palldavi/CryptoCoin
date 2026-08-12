Imports CryptoCoin.Cryptography
Imports CryptoCoin.Core.Serialization

Namespace CryptoCoin.Transactions

    ''' <summary>
    ''' Serializes and deserializes transactions for network transmission and storage.
    ''' </summary>
    Public NotInheritable Class TransactionSerializer

        Private Sub New()
        End Sub

        ''' <summary>
        ''' Serializes a transaction to bytes.
        ''' </summary>
        Public Shared Function Serialize(tx As Transaction) As Byte()
            If tx Is Nothing Then Throw New ArgumentNullException(NameOf(tx))
            Return tx.Serialize()
        End Function

        ''' <summary>
        ''' Deserializes a transaction from bytes.
        ''' </summary>
        Public Shared Function Deserialize(data As Byte()) As Transaction
            Return Transaction.Deserialize(data)
        End Function

        ''' <summary>
        ''' Serializes a transaction to a hex string.
        ''' </summary>
        Public Shared Function SerializeToHex(tx As Transaction) As String
            Return HashUtil.ToHex(Serialize(tx))
        End Function

        ''' <summary>
        ''' Deserializes a transaction from a hex string.
        ''' </summary>
        Public Shared Function DeserializeFromHex(hex As String) As Transaction
            Dim data As Byte() = HashUtil.FromHex(hex)
            Return Deserialize(data)
        End Function

        ''' <summary>
        ''' Computes the transaction ID (hash).
        ''' </summary>
        Public Shared Function ComputeTxId(tx As Transaction) As String
            Dim data As Byte() = Serialize(tx)
            Dim hash As Byte() = HashUtil.DoubleSha256(data)
            Return HashUtil.ToHex(hash)
        End Function

        ''' <summary>
        ''' Serializes a list of transactions.
        ''' </summary>
        Public Shared Function SerializeList(transactions As List(Of Transaction)) As Byte()
            Dim writer As New BufferWriter()
            writer.WriteVarInt(transactions.Count)
            For Each tx As Transaction In transactions
                Dim txBytes As Byte() = Serialize(tx)
                writer.WriteVarBytes(txBytes)
            Next
            Return writer.ToArray()
        End Function

        ''' <summary>
        ''' Deserializes a list of transactions.
        ''' </summary>
        Public Shared Function DeserializeList(data As Byte()) As List(Of Transaction)
            Dim reader As New BufferReader(data)
            Dim count As Integer = CInt(reader.ReadVarInt())
            Dim transactions As New List(Of Transaction)(count)
            For i As Integer = 0 To count - 1
                Dim txBytes As Byte() = reader.ReadVarBytes()
                transactions.Add(Deserialize(txBytes))
            Next
            Return transactions
        End Function

    End Class

End Namespace
