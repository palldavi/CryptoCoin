Imports CryptoCoin.Core
Imports CryptoCoin.Cryptography

Namespace CryptoCoin.Networking

    ''' <summary>
    ''' Network message containing a full block with header and all transactions.
    ''' Sent in response to a GetData request for a block hash.
    ''' </summary>
    Public Class BlockMessage

        ''' <summary>
        ''' The block header.
        ''' </summary>
        Public Property Header As BlockHeader

        ''' <summary>
        ''' The number of transactions in the block.
        ''' </summary>
        Public Property TransactionCount As Integer

        ''' <summary>
        ''' The serialized transaction data For Each transaction As Object In the block.
        ''' </summary>
        Public Property Transactions As List(Of Byte())

        ''' <summary>
        ''' The block hash (computed from header).
        ''' </summary>
        Public ReadOnly Property BlockHash As String
            Get
                If Header Is Nothing Then Return String.Empty
                Return Header.ComputeHash()
            End Get
        End Property

        ''' <summary>
        ''' The total size of the block message payload in bytes.
        ''' </summary>
        Public ReadOnly Property PayloadSize As Integer
            Get
                Dim size As Integer = 80 ' header
                size += 4 ' transaction count varint (simplified)
                If Transactions IsNot Nothing Then
                    For Each tx As Object In Transactions
                        size += tx.Length
                    Next
                End If
                Return size
            End Get
        End Property

        ''' <summary>
        ''' Creates a new empty BlockMessage.
        ''' </summary>
        Public Sub New()
            Header = New BlockHeader()
            TransactionCount = 0
            Transactions = New List(Of Byte())()
        End Sub

        ''' <summary>
        ''' Creates a BlockMessage from an existing Block object.
        ''' </summary>
        ''' <param name="block">The block to wrap in a message.</param>
        Public Sub New(block As Block)
            If block Is Nothing Then
                Throw New ArgumentNullException(NameOf(block))
            End If
            Header = block.Header
            TransactionCount = block.TransactionCount
            Transactions = New List(Of Byte())(block.TransactionData)
        End Sub

        ''' <summary>
        ''' Serializes the block message to a byte array payload.
        ''' </summary>
        ''' <returns>The serialized payload bytes.</returns>
        Public Function Serialize() As Byte()
            Dim parts As New List(Of Byte())()

            ' Block header (80 bytes)
            parts.Add(Header.Serialize())

            ' Transaction count (4 bytes for simplicity)
            parts.Add(BitConverter.GetBytes(TransactionCount))

            ' Transaction data
            For Each tx As Object In Transactions
                ' Transaction length prefix (4 bytes)
                parts.Add(BitConverter.GetBytes(tx.Length))
                parts.Add(tx)
            Next

            ' Combine all parts
            Dim totalSize As Integer = 0
            For Each p As Object In parts
                totalSize += p.Length
            Next

            Dim result(totalSize - 1) As Byte
            Dim offset As Integer = 0
            For Each p As Object In parts
                Array.Copy(p, 0, result, offset, p.Length)
                offset += p.Length
            Next

            Return result
        End Function

        ''' <summary>
        ''' Deserializes a block message from a byte array payload.
        ''' </summary>
        ''' <param name="data">The payload bytes to deserialize.</param>
        ''' <returns>A populated BlockMessage instance.</returns>
        Public Shared Function Deserialize(data As Byte()) As BlockMessage
            If data Is Nothing OrElse data.Length < 84 Then
                Throw New ArgumentException("Block message payload too short.")
            End If

            Dim msg As New BlockMessage()
            Dim offset As Integer = 0

            ' Block header (80 bytes)
            Dim headerBytes(79) As Byte
            Array.Copy(data, offset, headerBytes, 0, 80)
            msg.Header = BlockHeader.Deserialize(headerBytes)
            offset += 80

            ' Transaction count
            msg.TransactionCount = BitConverter.ToInt32(data, offset)
            offset += 4

            ' Transaction data
            msg.Transactions = New List(Of Byte())()
            For i As Integer = 0 To msg.TransactionCount - 1
                If offset + 4 > data.Length Then Exit For

                Dim txLen As Integer = BitConverter.ToInt32(data, offset)
                offset += 4

                If txLen <= 0 OrElse offset + txLen > data.Length Then Exit For

                Dim txData(txLen - 1) As Byte
                Array.Copy(data, offset, txData, 0, txLen)
                msg.Transactions.Add(txData)
                offset += txLen
            Next

            Return msg
        End Function

        ''' <summary>
        ''' Converts this block message to a Block object.
        ''' </summary>
        ''' <returns>A Block instance populated from this message.</returns>
        Public Function ToBlock() As Block
            Dim block As New Block()
            block.Header = Header
            block.TransactionData = New List(Of Byte())(Transactions)

            ' Compute transaction IDs from data
            block.TransactionIds = New List(Of String)()
            For Each txData As Object In Transactions
                Dim txHash As Byte() = HashUtil.DoubleSha256(txData)
                block.TransactionIds.Add(HashUtil.ToHex(txHash))
            Next

            Return block
        End Function

        ''' <summary>
        ''' Validates the basic structure of the block message.
        ''' </summary>
        ''' <returns>True if the message structure is valid.</returns>
        Public Function ValidateStructure() As Boolean
            If Header Is Nothing Then Return False
            If TransactionCount < 0 Then Return False
            If Transactions Is Nothing Then Return False
            If Transactions.Count <> TransactionCount Then Return False
            Return True
        End Function

        ''' <summary>
        ''' Wraps this block message in a NetworkMessage for transmission.
        ''' </summary>
        ''' <returns>A NetworkMessage with the "block" command.</returns>
        Public Function ToNetworkMessage() As NetworkMessage
            Return New NetworkMessage(NetworkCommands.Block, Serialize())
        End Function

        Public Overrides Function ToString() As String
            Return $"BlockMessage(Hash={BlockHash.Substring(0, Math.Min(16, BlockHash.Length))}..., Txs={TransactionCount})"
        End Function

    End Class

End Namespace
