Namespace CryptoCoin.Networking

    ''' <summary>
    ''' Request message for specific blocks or transactions by their hashes.
    ''' Sent in response to an INV message to request the full data for
    ''' inventory items the node does not yet have.
    ''' </summary>
    Public Class GetDataMessage

        ''' <summary>
        ''' Maximum number of items that can be requested in a single GetData message.
        ''' </summary>
        Public Const MaxRequestItems As Integer = 50000

        ''' <summary>
        ''' The list of inventory vectors identifying the requested items.
        ''' </summary>
        Public Property RequestedItems As List(Of InventoryVector)

        ''' <summary>
        ''' The number of items being requested.
        ''' </summary>
        Public ReadOnly Property Count As Integer
            Get
                If RequestedItems Is Nothing Then Return 0
                Return RequestedItems.Count
            End Get
        End Property

        ''' <summary>
        ''' Creates a new empty GetDataMessage.
        ''' </summary>
        Public Sub New()
            RequestedItems = New List(Of InventoryVector)()
        End Sub

        ''' <summary>
        ''' Creates a GetDataMessage requesting the specified inventory items.
        ''' </summary>
        ''' <param name="items">The inventory vectors to request.</param>
        Public Sub New(items As IEnumerable(Of InventoryVector))
            RequestedItems = New List(Of InventoryVector)(items)
        End Sub

        ''' <summary>
        ''' Adds a request for a specific block by hash.
        ''' </summary>
        ''' <param name="blockHash">The block hash to request (hex-encoded).</param>
        Public Sub RequestBlock(blockHash As String)
            If RequestedItems.Count >= MaxRequestItems Then Return
            RequestedItems.Add(New InventoryVector(InventoryType.Block, blockHash))
        End Sub

        ''' <summary>
        ''' Adds a request for a specific transaction by hash.
        ''' </summary>
        ''' <param name="txHash">The transaction hash to request (hex-encoded).</param>
        Public Sub RequestTransaction(txHash As String)
            If RequestedItems.Count >= MaxRequestItems Then Return
            RequestedItems.Add(New InventoryVector(InventoryType.Transaction, txHash))
        End Sub

        ''' <summary>
        ''' Adds multiple block requests at once.
        ''' </summary>
        ''' <param name="blockHashes">The block hashes to request.</param>
        Public Sub RequestBlocks(blockHashes As IEnumerable(Of String))
            For Each hash As Object In blockHashes
                If RequestedItems.Count >= MaxRequestItems Then Exit For
                RequestedItems.Add(New InventoryVector(InventoryType.Block, hash))
            Next
        End Sub

        ''' <summary>
        ''' Adds multiple transaction requests at once.
        ''' </summary>
        ''' <param name="txHashes">The transaction hashes to request.</param>
        Public Sub RequestTransactions(txHashes As IEnumerable(Of String))
            For Each hash As Object In txHashes
                If RequestedItems.Count >= MaxRequestItems Then Exit For
                RequestedItems.Add(New InventoryVector(InventoryType.Transaction, hash))
            Next
        End Sub

        ''' <summary>
        ''' Gets all block requests from this message.
        ''' </summary>
        ''' <returns>A list of block inventory vectors.</returns>
        Public Function GetBlockRequests() As List(Of InventoryVector)
            Dim result As New List(Of InventoryVector)()
            For Each item As Object In RequestedItems
                If item.Type = InventoryType.Block Then
                    result.Add(item)
                End If
            Next
            Return result
        End Function

        ''' <summary>
        ''' Gets all transaction requests from this message.
        ''' </summary>
        ''' <returns>A list of transaction inventory vectors.</returns>
        Public Function GetTransactionRequests() As List(Of InventoryVector)
            Dim result As New List(Of InventoryVector)()
            For Each item As Object In RequestedItems
                If item.Type = InventoryType.Transaction Then
                    result.Add(item)
                End If
            Next
            Return result
        End Function

        ''' <summary>
        ''' Checks whether a specific hash is being requested.
        ''' </summary>
        ''' <param name="hash">The hash to check for.</param>
        ''' <returns>True if the hash is in the request list.</returns>
        Public Function ContainsHash(hash As String) As Boolean
            For Each item As Object In RequestedItems
                If String.Equals(item.Hash, hash, StringComparison.OrdinalIgnoreCase) Then
                    Return True
                End If
            Next
            Return False
        End Function

        ''' <summary>
        ''' Serializes the getdata message to a byte array payload.
        ''' </summary>
        ''' <returns>The serialized payload bytes.</returns>
        Public Function Serialize() As Byte()
            Dim parts As New List(Of Byte())()

            ' Item count (4 bytes)
            parts.Add(BitConverter.GetBytes(RequestedItems.Count))

            ' Inventory vectors (36 bytes each)
            For Each item As Object In RequestedItems
                parts.Add(item.Serialize())
            Next

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
        ''' Deserializes a getdata message from a byte array payload.
        ''' </summary>
        ''' <param name="data">The payload bytes to deserialize.</param>
        ''' <returns>A populated GetDataMessage instance.</returns>
        Public Shared Function Deserialize(data As Byte()) As GetDataMessage
            If data Is Nothing OrElse data.Length < 4 Then
                Throw New ArgumentException("GetData message payload too short.")
            End If

            Dim msg As New GetDataMessage()
            Dim offset As Integer = 0

            ' Item count
            Dim count As Integer = BitConverter.ToInt32(data, offset)
            offset += 4

            If count < 0 OrElse count > MaxRequestItems Then
                Throw New ArgumentException($"Invalid request item count: {count}")
            End If

            ' Read inventory vectors
            For i As Integer = 0 To count - 1
                If offset + 36 > data.Length Then Exit For
                Dim vec As InventoryVector = InventoryVector.Deserialize(data, offset)
                msg.RequestedItems.Add(vec)
                offset += 36
            Next

            Return msg
        End Function

        ''' <summary>
        ''' Validates the message structure.
        ''' </summary>
        ''' <returns>True if the message is structurally valid.</returns>
        Public Function ValidateStructure() As Boolean
            If RequestedItems Is Nothing Then Return False
            If RequestedItems.Count = 0 Then Return False
            If RequestedItems.Count > MaxRequestItems Then Return False
            For Each item As Object In RequestedItems
                If String.IsNullOrEmpty(item.Hash) Then Return False
                If item.Hash.Length <> 64 Then Return False
                If item.Type = InventoryType.[Error] Then Return False
            Next
            Return True
        End Function

        ''' <summary>
        ''' Wraps this message in a NetworkMessage for transmission.
        ''' </summary>
        ''' <returns>A NetworkMessage with the "getdata" command.</returns>
        Public Function ToNetworkMessage() As NetworkMessage
            Return New NetworkMessage(NetworkCommands.GetData, Serialize())
        End Function

        Public Overrides Function ToString() As String
            Return $"GetDataMessage(Items={Count}, Blocks={GetBlockRequests().Count}, Txs={GetTransactionRequests().Count})"
        End Function

    End Class

End Namespace
