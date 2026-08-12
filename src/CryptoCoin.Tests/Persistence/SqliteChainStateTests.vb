Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports CryptoCoin.Core
Imports CryptoCoin.Persistence
Imports System.IO
Imports System.Numerics

Namespace CryptoCoin.Tests.Persistence

    ''' <summary>
    ''' Integration tests for SqliteChainState.
    ''' Verifies that the chain tip and block index survive a database round-trip.
    ''' </summary>
    <TestClass>
    Public Class SqliteChainStateTests

        Private _tempDir As String
        Private _db As Database
        Private _blockStore As SqliteBlockStore

        <TestInitialize>
        Public Sub Setup()
            _tempDir = Path.Combine(Path.GetTempPath(), "CryptoCoinStateTests_" & Guid.NewGuid().ToString("N"))
            _db = New Database(_tempDir)
            _db.Open()
            _blockStore = New SqliteBlockStore(_db)
        End Sub

        <TestCleanup>
        Public Sub Teardown()
            _db.Dispose()
            Try
                If Directory.Exists(_tempDir) Then Directory.Delete(_tempDir, True)
            Catch
            End Try
        End Sub

        Private Function MakeIndex(hash As String, height As Integer) As BlockIndex
            Dim idx As New BlockIndex()
            idx.Hash = hash
            idx.Height = height
            idx.PreviousHash = New String("0"c, 64)
            idx.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            idx.Bits = DifficultyCalculator.MinDifficultyBits
            idx.TransactionCount = 1
            idx.TotalWork = BigInteger.One
            idx.IsMainChain = True
            Return idx
        End Function

        <TestMethod>
        Public Sub SetTip_ThenGetPersistedTipHash_ReturnsCorrectHash()
            Dim genesis As Block = GenesisBlock.Create(ChainParameters.Regtest())
            _blockStore.AddBlock(genesis)

            Dim state As New SqliteChainState(_db, _blockStore)
            Dim idx As BlockIndex = MakeIndex(genesis.Hash, 0)
            state.SetTip(idx)

            Dim persisted As String = state.GetPersistedTipHash()
            Assert.AreEqual(genesis.Hash, persisted)
        End Sub

        <TestMethod>
        Public Sub LoadAll_AfterSetTip_RestoresTip()
            Dim genesis As Block = GenesisBlock.Create(ChainParameters.Regtest())
            _blockStore.AddBlock(genesis)

            Dim state1 As New SqliteChainState(_db, _blockStore)
            Dim idx As BlockIndex = MakeIndex(genesis.Hash, 0)
            state1.SetTip(idx)

            ' Simulate restart: new state loads from DB
            Dim blockStore2 As New SqliteBlockStore(_db)
            blockStore2.LoadAll()
            Dim state2 As New SqliteChainState(_db, blockStore2)
            state2.LoadAll()

            Assert.IsNotNull(state2.Tip)
            Assert.AreEqual(genesis.Hash, state2.Tip.Hash)
            Assert.AreEqual(0, state2.Tip.Height)
        End Sub

        <TestMethod>
        Public Sub LoadAll_EmptyDatabase_TipIsNull()
            Dim state As New SqliteChainState(_db, _blockStore)
            state.LoadAll()
            Assert.IsNull(state.Tip)
        End Sub

        <TestMethod>
        Public Sub AddIndex_ThenLoadAll_IndexIsRestored()
            Dim genesis As Block = GenesisBlock.Create(ChainParameters.Regtest())
            _blockStore.AddBlock(genesis)

            Dim state1 As New SqliteChainState(_db, _blockStore)
            Dim idx As BlockIndex = MakeIndex(genesis.Hash, 0)
            state1.AddIndex(idx)

            Dim blockStore2 As New SqliteBlockStore(_db)
            blockStore2.LoadAll()
            Dim state2 As New SqliteChainState(_db, blockStore2)
            state2.LoadAll()

            Dim restored As BlockIndex = state2.GetIndex(genesis.Hash)
            Assert.IsNotNull(restored)
            Assert.AreEqual(0, restored.Height)
        End Sub

    End Class

End Namespace
