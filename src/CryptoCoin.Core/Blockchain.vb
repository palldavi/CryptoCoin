Imports CryptoCoin.Cryptography

Namespace CryptoCoin.Core

    ''' <summary>
    ''' Manages the blockchain state including the chain of blocks,
    ''' validation, and reorganization logic.
    ''' </summary>
    Public Class Blockchain

        Private ReadOnly _params As ChainParameters
        Private ReadOnly _store As BlockStore
        Private ReadOnly _chainState As ChainState
        Private ReadOnly _orphanPool As OrphanPool
        Private ReadOnly _syncLock As New Object()

        ''' <summary>
        ''' Gets the chain parameters.
        ''' </summary>
        Public ReadOnly Property Parameters As ChainParameters
            Get
                Return _params
            End Get
        End Property

        ''' <summary>
        ''' Gets the current chain tip (latest block).
        ''' </summary>
        Public ReadOnly Property Tip As BlockIndex
            Get
                Return _chainState.Tip
            End Get
        End Property

        ''' <summary>
        ''' Gets the current blockchain height.
        ''' </summary>
        Public ReadOnly Property Height As Integer
            Get
                If _chainState.Tip Is Nothing Then Return -1
                Return _chainState.Tip.Height
            End Get
        End Property

        ''' <summary>
        ''' Gets the current difficulty target bits.
        ''' </summary>
        Public ReadOnly Property CurrentDifficulty As UInteger
            Get
                If _chainState.Tip Is Nothing Then Return DifficultyCalculator.MinDifficultyBits
                Return _chainState.Tip.Bits
            End Get
        End Property

        ''' <summary>
        ''' Gets the total number of blocks in the chain.
        ''' </summary>
        Public ReadOnly Property BlockCount As Integer
            Get
                Return _store.Count
            End Get
        End Property

        ''' <summary>
        ''' Event raised when a new block is added to the chain.
        ''' </summary>
        Public Event BlockAdded As EventHandler(Of BlockAddedEventArgs)

        ''' <summary>
        ''' Event raised when a chain reorganization occurs.
        ''' </summary>
        Public Event ChainReorganized As EventHandler(Of ChainReorgEventArgs)

        Public Sub New(Optional params As ChainParameters = Nothing)
            _params = If(params, ChainParameters.Mainnet())
            _store = New BlockStore()
            _chainState = New ChainState()
            _orphanPool = New OrphanPool()

            ' Initialize with genesis block
            InitializeGenesis()
        End Sub

        ''' <summary>
        ''' Creates a Blockchain with injected storage implementations.
        ''' Used by CryptoCoin.Persistence to provide SQLite-backed storage.
        ''' If the store already contains blocks (loaded from disk) genesis
        ''' initialization is skipped.
        ''' </summary>
        Public Sub New(params As ChainParameters, store As BlockStore, chainState As ChainState)
            _params = If(params, ChainParameters.Mainnet())
            _store = If(store, New BlockStore())
            _chainState = If(chainState, New ChainState())
            _orphanPool = New OrphanPool()

            ' Only initialize genesis if the store is empty (fresh chain)
            If _store.Count = 0 Then
                InitializeGenesis()
            Else
                ' Restore genesis hash parameter from the persisted genesis block
                Dim genesisIndex As BlockIndex = _chainState.GetByHeight(0)
                If genesisIndex IsNot Nothing Then
                    _params.GenesisBlockHash = genesisIndex.Hash
                End If
            End If
        End Sub

        Private Sub InitializeGenesis()
            Dim genesis As Block = GenesisBlock.Create(_params)
            Dim index As New BlockIndex()
            index.Hash = genesis.Hash
            index.Height = 0
            index.PreviousHash = genesis.Header.PreviousBlockHash
            index.Timestamp = genesis.Header.Timestamp
            index.Bits = genesis.Header.Bits
            index.TransactionCount = genesis.TransactionCount
            index.TotalWork = DifficultyCalculator.GetBlockWork(genesis.Header.Bits)

            _store.AddBlock(genesis)
            _chainState.SetTip(index)
            _params.GenesisBlockHash = genesis.Hash
        End Sub

        ''' <summary>
        ''' Attempts to add a new block to the blockchain.
        ''' </summary>
        Public Function AddBlock(block As Block) As BlockValidationResult
            SyncLock _syncLock
                ' Basic structure validation
                Dim result As BlockValidationResult = block.ValidateStructure(_params)
                If Not result.IsValid Then Return result

                ' Check if we already have this block
                If _store.HasBlock(block.Hash) Then
                    result.AddError("Block already exists in chain.")
                    Return result
                End If

                ' Check if previous block exists
                If Not _store.HasBlock(block.Header.PreviousBlockHash) Then
                    ' Add to orphan pool
                    _orphanPool.Add(block)
                    result.AddError("Previous block not found. Added to orphan pool.")
                    Return result
                End If

                ' Validate against consensus rules
                result = ValidateBlock(block)
                If Not result.IsValid Then Return result

                ' Add to store
                _store.AddBlock(block)

                ' Update chain state
                Dim parentIndex As BlockIndex = _chainState.GetIndex(block.Header.PreviousBlockHash)
                Dim newIndex As New BlockIndex()
                newIndex.Hash = block.Hash
                newIndex.Height = parentIndex.Height + 1
                newIndex.PreviousHash = block.Header.PreviousBlockHash
                newIndex.Timestamp = block.Header.Timestamp
                newIndex.Bits = block.Header.Bits
                newIndex.TransactionCount = block.TransactionCount
                newIndex.TotalWork = parentIndex.TotalWork + DifficultyCalculator.GetBlockWork(block.Header.Bits)

                _chainState.AddIndex(newIndex)

                ' Check if this extends the best chain
                If newIndex.TotalWork > _chainState.Tip.TotalWork Then
                    _chainState.SetTip(newIndex)
                    RaiseEvent BlockAdded(Me, New BlockAddedEventArgs(block, newIndex))
                End If

                ' Process orphans that may connect to this block
                ProcessOrphans(block.Hash)

                Return result
            End SyncLock
        End Function

        ''' <summary>
        ''' Validates a block against consensus rules.
        ''' </summary>
        Private Function ValidateBlock(block As Block) As BlockValidationResult
            Dim result As New BlockValidationResult()

            ' Timestamp validation
            Dim currentTime As Long = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            If block.Header.Timestamp > currentTime + _params.MaxTimeDriftSeconds Then
                result.AddError("Block timestamp too far in the future.")
            End If

            ' Difficulty validation
            Dim expectedBits As UInteger = GetNextDifficulty(block.Header.PreviousBlockHash)
            If block.Header.Bits <> expectedBits Then
                result.AddError($"Incorrect difficulty. Expected {expectedBits}, got {block.Header.Bits}.")
            End If

            ' Height validation
            Dim parentIndex As BlockIndex = _chainState.GetIndex(block.Header.PreviousBlockHash)
            If parentIndex IsNot Nothing Then
                Dim expectedHeight As Integer = parentIndex.Height + 1
                If block.Header.Height <> expectedHeight Then
                    result.AddError($"Incorrect height. Expected {expectedHeight}, got {block.Header.Height}.")
                End If
            End If

            Return result
        End Function

        ''' <summary>
        ''' Gets the next difficulty target for a block following the given parent.
        ''' </summary>
        Public Function GetNextDifficulty(parentHash As String) As UInteger
            Dim parentIndex As BlockIndex = _chainState.GetIndex(parentHash)
            If parentIndex Is Nothing Then Return DifficultyCalculator.MinDifficultyBits

            ' Check if we need to adjust
            Dim nextHeight As Integer = parentIndex.Height + 1
            If nextHeight Mod _params.DifficultyAdjustmentInterval <> 0 Then
                Return parentIndex.Bits
            End If

            ' Find the block at the start of this adjustment period
            Dim periodStart As BlockIndex = parentIndex
            For i As Integer = 1 To _params.DifficultyAdjustmentInterval - 1
                periodStart = _chainState.GetIndex(periodStart.PreviousHash)
                If periodStart Is Nothing Then Return parentIndex.Bits
            Next

            Dim actualTimespan As Long = parentIndex.Timestamp - periodStart.Timestamp
            Return DifficultyCalculator.CalculateNextTarget(parentIndex.Bits, actualTimespan, _params)
        End Function

        ''' <summary>
        ''' Processes orphan blocks that may now connect to the chain.
        ''' </summary>
        Private Sub ProcessOrphans(parentHash As String)
            Dim orphans As List(Of Block) = _orphanPool.GetByParent(parentHash)
            For Each orphan As Block In orphans
                _orphanPool.Remove(orphan.Hash)
                AddBlock(orphan)
            Next
        End Sub

        ''' <summary>
        ''' Gets a block by its hash.
        ''' </summary>
        Public Function GetBlock(hash As String) As Block
            Return _store.GetBlock(hash)
        End Function

        ''' <summary>
        ''' Gets a block by its height (on the main chain).
        ''' </summary>
        Public Function GetBlockByHeight(height As Integer) As Block
            Dim index As BlockIndex = _chainState.GetByHeight(height)
            If index Is Nothing Then Return Nothing
            Return _store.GetBlock(index.Hash)
        End Function

        ''' <summary>
        ''' Gets the block index for a given hash.
        ''' </summary>
        Public Function GetBlockIndex(hash As String) As BlockIndex
            Return _chainState.GetIndex(hash)
        End Function

        ''' <summary>
        ''' Gets block hashes starting from a given height.
        ''' </summary>
        Public Function GetBlockHashes(startHeight As Integer, count As Integer) As List(Of String)
            Dim hashes As New List(Of String)()
            For h As Integer = startHeight To Math.Min(startHeight + count - 1, Height)
                Dim index As BlockIndex = _chainState.GetByHeight(h)
                If index IsNot Nothing Then
                    hashes.Add(index.Hash)
                End If
            Next
            Return hashes
        End Function

    End Class

    ''' <summary>
    ''' Event args for when a new block is added.
    ''' </summary>
    Public Class BlockAddedEventArgs
        Inherits EventArgs

        Public ReadOnly Property Block As Block
        Public ReadOnly Property Index As BlockIndex

        Public Sub New(block As Block, index As BlockIndex)
            Me.Block = block
            Me.Index = index
        End Sub
    End Class

    ''' <summary>
    ''' Event args for chain reorganization.
    ''' </summary>
    Public Class ChainReorgEventArgs
        Inherits EventArgs

        Public ReadOnly Property DisconnectedBlocks As List(Of Block)
        Public ReadOnly Property ConnectedBlocks As List(Of Block)

        Public Sub New(disconnected As List(Of Block), connected As List(Of Block))
            Me.DisconnectedBlocks = disconnected
            Me.ConnectedBlocks = connected
        End Sub
    End Class

End Namespace
