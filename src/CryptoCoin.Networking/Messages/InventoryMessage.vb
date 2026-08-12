Namespace CryptoCoin.Networking

    ''' <summary>
    ''' Defines the types of inventory items that can be announced.
    ''' </summary>
    Public Enum InventoryType As Integer
        ''' <summary>Error or unknown type.</summary>
        [Error] = 0
        ''' <summary>Transaction hash.</summary>
        Transaction = 1
        ''' <summary>Block hash.</summary>
        Block = 2
        ''' <summary>Filtered block (for SPV clients).</summary>
        FilteredBlock = 3
        ''' <summary>Compact block.</summary>
        CompactBlock = 4
    End Enum

    ''' <summary>
    ''' Represents a single inventory vector (type + hash pair).
    ''' </summary>
    Public Class InventoryVector

        ''' <summary>
        ''' The type of object being referenced.
        ''' </summary>
        Public Property Type As InventoryType

        ''' <summary>
        ''' The hash of the referenced object (32 bytes, hex-encoded).
        ''' </summary>
        Public Property Hash As String

        ''' <summary>
        ''' Creates a new empty InventoryVector.
        ''' </summary>
        Public Sub New()
            Type = InventoryType.[Error]
            Hash = String.Empty
        End Sub

        ''' <summary>
        ''' Creates a new InventoryVector with the specified type and hash.
        ''' </summary>
        ''' <param name="invType">The inventory type.</param>
        ''' <param name="hash">The object hash (hex-encoded).</param>
        Public Sub New(invType As InventoryType, hash As String)
            Me.Type = invType
            Me.Hash = If(hash, String.Empty)
        End Sub

        ''' <summary>
        ''' Serializes this inventory vector to bytes (36 bytes: 4 type + 32 hash).
        ''' </summary>
        ''' <returns>The serialized bytes.</returns>
        Public Function Serialize() As Byte()
            Dim result(35) As Byte

            ' Type (4 bytes, little-endian)
            Dim typeBytes As Byte() = BitConverter.GetBytes(CInt(Type))
            Array.Copy(typeBytes, 0, result, 0, 4)

            ' Hash (32 bytes)
            Dim hashBytes As Byte() = HexToBytes(Hash.PadLeft(64, "0"c))
            Array.Copy(hashBytes, 0, result, 4, 32)

            Return result
        End Function

        ''' <summary>
        ''' Deserializes an inventory vector from bytes at the given offset.
        ''' </summary>
        ''' <param name="data">The source byte array.</param>
        ''' <param name="offset">The offset to start reading from.</param>
        ''' <returns>A populated InventoryVector.</returns>
        Public Shared Function Deserialize(data As Byte(), offset As Integer) As InventoryVector
            If data Is Nothing OrElse offset + 36 > data.Length Then
                Throw New ArgumentException("Insufficient data for inventory vector.")
            End If

            Dim vec As New InventoryVector()
            vec.Type = CType(BitConverter.ToInt32(data, offset), InventoryType)

            Dim hashBytes(31) As Byte
            Array.Copy(data, offset + 4, hashBytes, 0, 32)
            vec.Hash = BytesToHex(hashBytes)

            Return vec
        End Function

        Private Shared Function HexToBytes(hex As String) As Byte()
            Dim bytes(hex.Length \ 2 - 1) As Byte
            For i As Integer = 0 To bytes.Length - 1
                bytes(i) = Convert.ToByte(hex.Substring(i * 2, 2), 16)
            Next
            Return bytes
        End Function

        Private Shared Function BytesToHex(bytes As Byte()) As String
            Dim sb As New System.Text.StringBuilder(bytes.Length * 2)
            For Each b As Object In bytes
                sb.Append(b.ToString("x2"))
            Next
            Return sb.ToString()
        End Function

        Public Overrides Function ToString() As String
            Return $"InvVector({Type}, {Hash.Substring(0, Math.Min(16, Hash.Length))}...)"
        End Function

    End Class

    ''' <summary>
    ''' Inventory announcement message (INV) used to advertise knowledge of
    ''' transactions or blocks to peers. Peers can then request the full data
    ''' using GetData messages.
    ''' </summary>
    Public Class InventoryMessage

        ''' <summary>
        ''' Maximum number of inventory vectors in a single message.
        ''' </summary>
        Public Const MaxInventoryItems As Integer = 50000

        ''' <summary>
        ''' The list of inventory vectors being announced.
        ''' </summary>
        Public Property Inventory As List(Of InventoryVector)

        ''' <summary>
        ''' The number of inventory items in this message.
        ''' </summary>
        Public ReadOnly Property Count As Integer
            Get
                If Inventory Is Nothing Then Return 0
                Return Inventory.Count
            End Get
        End Property

        ''' <summary>
        ''' Creates a new empty InventoryMessage.
        ''' </summary>
        Public Sub New()
            Inventory = New List(Of InventoryVector)()
        End Sub

        ''' <summary>
        ''' Creates an InventoryMessage with the specified vectors.
        ''' </summary>
        ''' <param name="vectors">The inventory vectors to include.</param>
        Public Sub New(vectors As IEnumerable(Of InventoryVector))
            Inventory = New List(Of InventoryVector)(vectors)
        End Sub

        ''' <summary>
        ''' Adds a transaction hash to the inventory.
        ''' </summary>
        ''' <param name="txHash">The transaction hash (hex-encoded).</param>
        Public Sub AddTransaction(txHash As String)
            If Inventory.Count >= MaxInventoryItems Then Return
            Inventory.Add(New InventoryVector(InventoryType.Transaction, txHash))
        End Sub

        ''' <summary>
        ''' Adds a block hash to the inventory.
        ''' </summary>
        ''' <param name="blockHash">The block hash (hex-encoded).</param>
        Public Sub AddBlock(blockHash As String)
            If Inventory.Count >= MaxInventoryItems Then Return
            Inventory.Add(New InventoryVector(InventoryType.Block, blockHash))
        End Sub

        ''' <summary>
        ''' Gets all transaction inventory vectors.
        ''' </summary>
        ''' <returns>A list of transaction inventory vectors.</returns>
        Public Function GetTransactions() As List(Of InventoryVector)
            Dim result As New List(Of InventoryVector)()
            For Each vec As Object In Inventory
                If vec.Type = InventoryType.Transaction Then
                    result.Add(vec)
                End If
            Next
            Return result
        End Function

        ''' <summary>
        ''' Gets all block inventory vectors.
        ''' </summary>
        ''' <returns>A list of block inventory vectors.</returns>
        Public Function GetBlocks() As List(Of InventoryVector)
            Dim result As New List(Of InventoryVector)()
            For Each vec As Object In Inventory
                If vec.Type = InventoryType.Block Then
                    result.Add(vec)
                End If
            Next
            Return result
        End Function

        ''' <summary>
        ''' Serializes the inventory message to a byte array payload.
        ''' </summary>
        ''' <returns>The serialized payload bytes.</returns>
        Public Function Serialize() As Byte()
            Dim parts As New List(Of Byte())()

            ' Count (4 bytes)
            parts.Add(BitConverter.GetBytes(Inventory.Count))

            ' Inventory vectors (36 bytes each)
            For Each vec As Object In Inventory
                parts.Add(vec.Serialize())
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
        ''' Deserializes an inventory message from a byte array payload.
        ''' </summary>
        ''' <param name="data">The payload bytes to deserialize.</param>
        ''' <returns>A populated InventoryMessage instance.</returns>
        Public Shared Function Deserialize(data As Byte()) As InventoryMessage
            If data Is Nothing OrElse data.Length < 4 Then
                Throw New ArgumentException("Inventory message payload too short.")
            End If

            Dim msg As New InventoryMessage()
            Dim offset As Integer = 0

            ' Count
            Dim count As Integer = BitConverter.ToInt32(data, offset)
            offset += 4

            ' Validate count
            If count < 0 OrElse count > MaxInventoryItems Then
                Throw New ArgumentException($"Invalid inventory count: {count}")
            End If

            ' Read vectors
            For i As Integer = 0 To count - 1
                If offset + 36 > data.Length Then Exit For
                Dim vec As InventoryVector = InventoryVector.Deserialize(data, offset)
                msg.Inventory.Add(vec)
                offset += 36
            Next

            Return msg
        End Function

        ''' <summary>
        ''' Validates the inventory message structure.
        ''' </summary>
        ''' <returns>True if the message is structurally valid.</returns>
        Public Function ValidateStructure() As Boolean
            If Inventory Is Nothing Then Return False
            If Inventory.Count > MaxInventoryItems Then Return False
            For Each vec As Object In Inventory
                If String.IsNullOrEmpty(vec.Hash) Then Return False
                If vec.Hash.Length <> 64 Then Return False
            Next
            Return True
        End Function

        ''' <summary>
        ''' Wraps this inventory message in a NetworkMessage for transmission.
        ''' </summary>
        ''' <returns>A NetworkMessage with the "inv" command.</returns>
        Public Function ToNetworkMessage() As NetworkMessage
            Return New NetworkMessage(NetworkCommands.Inv, Serialize())
        End Function

        Public Overrides Function ToString() As String
            Return $"InventoryMessage(Count={Count}, Txs={GetTransactions().Count}, Blocks={GetBlocks().Count})"
        End Function

    End Class

End Namespace
