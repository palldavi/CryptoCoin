Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports CryptoCoin.Core
Imports CryptoCoin.Persistence
Imports System.IO

Namespace CryptoCoin.Tests.Persistence

    ''' <summary>
    ''' Integration tests for SqliteBlockStore.
    ''' Each test uses a fresh temporary database file that is deleted on cleanup.
    ''' </summary>
    <TestClass>
    Public Class SqliteBlockStoreTests

        Private _tempDir As String
        Private _db As Database

        <TestInitialize>
        Public Sub Setup()
            _tempDir = Path.Combine(Path.GetTempPath(), "CryptoCoinTests_" & Guid.NewGuid().ToString("N"))
            _db = New Database(_tempDir)
            _db.Open()
        End Sub

        <TestCleanup>
        Public Sub Teardown()
            _db.Dispose()
            Try
                If Directory.Exists(_tempDir) Then
                    Directory.Delete(_tempDir, True)
                End If
            Catch
                ' Ignore cleanup errors
            End Try
        End Sub

        <TestMethod>
        Public Sub AddBlock_ThenGetPersistedCount_ReturnsOne()
            Dim store As New SqliteBlockStore(_db)
            Dim genesis As Block = GenesisBlock.Create(ChainParameters.Regtest())
            store.AddBlock(genesis)
            Assert.AreEqual(1, store.GetPersistedCount())
        End Sub

        <TestMethod>
        Public Sub AddBlock_ThenLoadAll_BlockIsInMemoryCache()
            Dim store As New SqliteBlockStore(_db)
            Dim genesis As Block = GenesisBlock.Create(ChainParameters.Regtest())
            store.AddBlock(genesis)

            ' Create a fresh store pointing at the same DB and load
            Dim store2 As New SqliteBlockStore(_db)
            store2.LoadAll()

            Dim loaded As Block = store2.GetBlock(genesis.Hash)
            Assert.IsNotNull(loaded)
            Assert.AreEqual(genesis.Hash, loaded.Hash)
        End Sub

        <TestMethod>
        Public Sub AddBlock_SameBlockTwice_CountRemainsOne()
            Dim store As New SqliteBlockStore(_db)
            Dim genesis As Block = GenesisBlock.Create(ChainParameters.Regtest())
            store.AddBlock(genesis)
            store.AddBlock(genesis)   ' INSERT OR IGNORE — should not throw or duplicate
            Assert.AreEqual(1, store.GetPersistedCount())
        End Sub

        <TestMethod>
        Public Sub LoadAll_EmptyDatabase_StoreRemainsEmpty()
            Dim store As New SqliteBlockStore(_db)
            store.LoadAll()
            Assert.AreEqual(0, store.Count)
        End Sub

        <TestMethod>
        Public Sub AddBlock_LoadAll_TransactionIdsPreserved()
            Dim store As New SqliteBlockStore(_db)
            Dim genesis As Block = GenesisBlock.Create(ChainParameters.Regtest())
            store.AddBlock(genesis)

            Dim store2 As New SqliteBlockStore(_db)
            store2.LoadAll()
            Dim loaded As Block = store2.GetBlock(genesis.Hash)

            Assert.IsNotNull(loaded)
            Assert.AreEqual(genesis.TransactionCount, loaded.TransactionCount)
        End Sub

        <TestMethod>
        Public Sub AddBlock_LoadAll_HeightPreserved()
            Dim store As New SqliteBlockStore(_db)
            Dim genesis As Block = GenesisBlock.Create(ChainParameters.Regtest())
            store.AddBlock(genesis)

            Dim store2 As New SqliteBlockStore(_db)
            store2.LoadAll()
            Dim loaded As Block = store2.GetBlock(genesis.Hash)

            Assert.AreEqual(0, loaded.Height)
        End Sub

    End Class

End Namespace
