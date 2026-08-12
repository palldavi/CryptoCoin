Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports CryptoCoin.Core

Namespace CryptoCoin.Tests.Core

    ''' <summary>
    ''' Tests for the injected-store constructor added in Phase 1.
    ''' Verifies that Blockchain correctly accepts external BlockStore and ChainState
    ''' instances and skips genesis initialisation when the store is pre-populated.
    ''' </summary>
    <TestClass>
    Public Class BlockchainInjectionTests

        <TestMethod>
        Public Sub New_WithEmptyInjectedStore_InitialisesGenesis()
            Dim store As New BlockStore()
            Dim state As New ChainState()
            Dim chain As New Blockchain(ChainParameters.Regtest(), store, state)

            Assert.AreEqual(0, chain.Height)
            Assert.IsNotNull(chain.Tip)
            Assert.AreEqual(1, chain.BlockCount)
        End Sub

        <TestMethod>
        Public Sub New_WithEmptyInjectedStore_GenesisHashMatchesStandaloneChain()
            ' Both chains use the same params so genesis hashes must match
            Dim standalone As New Blockchain(ChainParameters.Regtest())
            Dim injected As New Blockchain(ChainParameters.Regtest(), New BlockStore(), New ChainState())

            Assert.AreEqual(standalone.Tip.Hash, injected.Tip.Hash)
        End Sub

        <TestMethod>
        Public Sub New_WithPrePopulatedStore_SkipsGenesisInit()
            ' Simulate a pre-loaded store (as PersistenceFactory does on restart)
            Dim params As ChainParameters = ChainParameters.Regtest()
            Dim genesis As Block = GenesisBlock.Create(params)

            Dim store As New BlockStore()
            store.AddBlock(genesis)

            Dim state As New ChainState()
            Dim idx As New BlockIndex()
            idx.Hash = genesis.Hash
            idx.Height = 0
            idx.PreviousHash = genesis.Header.PreviousBlockHash
            idx.Timestamp = genesis.Header.Timestamp
            idx.Bits = genesis.Header.Bits
            idx.TransactionCount = genesis.TransactionCount
            idx.TotalWork = DifficultyCalculator.GetBlockWork(genesis.Header.Bits)
            state.SetTip(idx)

            params.GenesisBlockHash = genesis.Hash

            Dim chain As New Blockchain(params, store, state)

            ' Should have exactly 1 block — genesis was not added again
            Assert.AreEqual(1, chain.BlockCount)
            Assert.AreEqual(0, chain.Height)
        End Sub

        <TestMethod>
        Public Sub New_WithNullStore_FallsBackToDefaultStore()
            ' Passing Nothing should not throw — falls back to new BlockStore()
            Dim chain As New Blockchain(ChainParameters.Regtest(), Nothing, Nothing)
            Assert.AreEqual(0, chain.Height)
            Assert.AreEqual(1, chain.BlockCount)
        End Sub

        <TestMethod>
        Public Sub GetBlock_AfterInjection_ReturnsGenesisBlock()
            Dim chain As New Blockchain(ChainParameters.Regtest(), New BlockStore(), New ChainState())
            Dim genesis As Block = chain.GetBlockByHeight(0)
            Assert.IsNotNull(genesis)
            Assert.AreEqual(0, genesis.Height)
        End Sub

    End Class

End Namespace
