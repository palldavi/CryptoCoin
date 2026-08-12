Imports CryptoCoin.Cryptography

Namespace CryptoCoin.Core

    ''' <summary>
    ''' Represents a complete block in the CryptoCoin blockchain.
    ''' Contains a header and a list of transactions.
    ''' </summary>
    Public Class Block

        ''' <summary>
        ''' The block header containing metadata and proof-of-work.
        ''' </summary>
        Public Property Header As BlockHeader

        ''' <summary>
        ''' The list of transaction IDs in this block.
        ''' The first transaction is always the coinbase transaction.
        ''' </summary>
        Public Property TransactionIds As List(Of String)

        ''' <summary>
        ''' Raw serialized transaction data (for full blocks).
        ''' </summary>
        Public Property TransactionData As List(Of Byte())

        ''' <summary>
        ''' The block hash (computed from header).
        ''' </summary>
        Public ReadOnly Property Hash As String
            Get
                If Header Is Nothing Then Return String.Empty
                Return Header.ComputeHash()
            End Get
        End Property

        ''' <summary>
        ''' The block height.
        ''' </summary>
        Public ReadOnly Property Height As Integer
            Get
                If Header Is Nothing Then Return -1
                Return Header.Height
            End Get
        End Property

        ''' <summary>
        ''' Number of transactions in the block.
        ''' </summary>
        Public ReadOnly Property TransactionCount As Integer
            Get
                If TransactionIds Is Nothing Then Return 0
                Return TransactionIds.Count
            End Get
        End Property

        ''' <summary>
        ''' Block size in bytes (approximate).
        ''' </summary>
        Public ReadOnly Property Size As Integer
            Get
                Dim headerSize As Integer = 80
                Dim txSize As Integer = 0
                If TransactionData IsNot Nothing Then
                    For Each tx As Byte() In TransactionData
                        txSize += tx.Length
                    Next
                End If
                Return headerSize + txSize
            End Get
        End Property

        Public Sub New()
            TransactionIds = New List(Of String)()
            TransactionData = New List(Of Byte())()
            Header = New BlockHeader()
        End Sub

        Public Sub New(header As BlockHeader, transactionIds As List(Of String))
            Me.Header = header
            Me.TransactionIds = If(transactionIds, New List(Of String)())
            Me.TransactionData = New List(Of Byte())()
        End Sub

        ''' <summary>
        ''' Computes the Merkle root from the transaction IDs.
        ''' </summary>
        Public Function ComputeMerkleRoot() As String
            If TransactionIds Is Nothing OrElse TransactionIds.Count = 0 Then
                Return New String("0"c, 64)
            End If

            Dim tree As CryptoCoin.Cryptography.MerkleTree = CryptoCoin.Cryptography.MerkleTree.FromHexStrings(TransactionIds)
            Return HashUtil.ToHex(tree.Root)
        End Function

        ''' <summary>
        ''' Validates that the Merkle root in the header matches the transactions.
        ''' </summary>
        Public Function ValidateMerkleRoot() As Boolean
            Dim computed As String = ComputeMerkleRoot()
            Return String.Equals(computed, Header.MerkleRoot, StringComparison.OrdinalIgnoreCase)
        End Function

        ''' <summary>
        ''' Validates basic block structure.
        ''' </summary>
        Public Function ValidateStructure(params As ChainParameters) As BlockValidationResult
            Dim result As New BlockValidationResult()

            ' Check header exists
            If Header Is Nothing Then
                result.AddError("Block header is missing.")
                Return result
            End If

            ' Check transaction count
            If TransactionIds Is Nothing OrElse TransactionIds.Count = 0 Then
                result.AddError("Block must contain at least one transaction (coinbase).")
                Return result
            End If

            If TransactionIds.Count > params.MaxTransactionsPerBlock Then
                result.AddError($"Block exceeds maximum transaction count ({params.MaxTransactionsPerBlock}).")
            End If

            ' Check block size
            If Size > params.MaxBlockSize Then
                result.AddError($"Block exceeds maximum size ({params.MaxBlockSize} bytes).")
            End If

            ' Validate Merkle root
            If Not ValidateMerkleRoot() Then
                result.AddError("Merkle root mismatch.")
            End If

            ' Check proof-of-work
            If Not Header.MeetsTarget() Then
                result.AddError("Block does not meet difficulty target.")
            End If

            Return result
        End Function

        ''' <summary>
        ''' Serializes the complete block to bytes.
        ''' </summary>
        Public Function Serialize() As Byte()
            Dim parts As New List(Of Byte())()

            ' Header (80 bytes)
            parts.Add(Header.Serialize())

            ' Transaction count (varint)
            parts.Add(Serialization.VarInt.Encode(CLng(TransactionIds.Count)))

            ' Transaction IDs
            For Each txId As String In TransactionIds
                parts.Add(HashUtil.FromHex(txId))
            Next

            ' Compute total size and combine
            Dim totalSize As Integer = 0
            For Each p As Byte() In parts
                totalSize += p.Length
            Next

            Dim result(totalSize - 1) As Byte
            Dim offset As Integer = 0
            For Each p As Byte() In parts
                Array.Copy(p, 0, result, offset, p.Length)
                offset += p.Length
            Next

            Return result
        End Function

        Public Overrides Function ToString() As String
            Return $"Block(Height={Height}, Hash={Hash.Substring(0, 16)}..., Txs={TransactionCount})"
        End Function

    End Class

    ''' <summary>
    ''' Result of block validation containing any errors found.
    ''' </summary>
    Public Class BlockValidationResult

        Public Property Errors As List(Of String)

        Public ReadOnly Property IsValid As Boolean
            Get
                Return Errors.Count = 0
            End Get
        End Property

        Public Sub New()
            Errors = New List(Of String)()
        End Sub

        Public Sub AddError(message As String)
            Errors.Add(message)
        End Sub

        Public Overrides Function ToString() As String
            If IsValid Then Return "Valid"
            Return $"Invalid: {String.Join("; ", Errors)}"
        End Function

    End Class

End Namespace
