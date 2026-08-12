Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports CryptoCoin.Core

Namespace CryptoCoin.Tests.Core

    <TestClass>
    Public Class BlockchainTests

        Private Function MakeBlockchain() As Blockchain
            Return New Blockchain(ChainParameters.Regtest())
        End Function

        <TestMethod>
        Public Sub New_StartsAtHeightZero()
            Dim chain As Blockchain = MakeBlockchain()
            Assert.AreEqual(0, chain.Height)
        End Sub

        <TestMethod>
        Public Sub New_HasGenesisBlock()
            Dim chain As Blockchain = MakeBlockchain()
            Assert.IsNotNull(chain.Tip)
            Assert.AreEqual(0, chain.Tip.Height)
        End Sub

        <TestMethod>
        Public Sub New_GenesisBlockHashIsNonEmpty()
            Dim chain As Blockchain = MakeBlockchain()
            Assert.IsFalse(String.IsNullOrEmpty(chain.Tip.Hash))
            Assert.AreEqual(64, chain.Tip.Hash.Length)
        End Sub

        <TestMethod>
        Public Sub GetBlockByHeight_Zero_ReturnsGenesisBlock()
            Dim chain As Blockchain = MakeBlockchain()
            Dim genesis As Block = chain.GetBlockByHeight(0)
            Assert.IsNotNull(genesis)
            Assert.AreEqual(0, genesis.Height)
        End Sub

        <TestMethod>
        Public Sub GetBlock_GenesisHash_ReturnsGenesisBlock()
            Dim chain As Blockchain = MakeBlockchain()
            Dim genesisHash As String = chain.Tip.Hash
            Dim genesis As Block = chain.GetBlock(genesisHash)
            Assert.IsNotNull(genesis)
            Assert.AreEqual(0, genesis.Height)
        End Sub

        <TestMethod>
        Public Sub GetBlock_UnknownHash_ReturnsNothing()
            Dim chain As Blockchain = MakeBlockchain()
            Dim result As Block = chain.GetBlock(New String("a"c, 64))
            Assert.IsNull(result)
        End Sub

        <TestMethod>
        Public Sub GetBlockByHeight_NegativeHeight_ReturnsNothing()
            Dim chain As Blockchain = MakeBlockchain()
            Dim result As Block = chain.GetBlockByHeight(-1)
            Assert.IsNull(result)
        End Sub

        <TestMethod>
        Public Sub GetBlockByHeight_BeyondTip_ReturnsNothing()
            Dim chain As Blockchain = MakeBlockchain()
            Dim result As Block = chain.GetBlockByHeight(999)
            Assert.IsNull(result)
        End Sub

        <TestMethod>
        Public Sub BlockCount_AfterInit_IsOne()
            Dim chain As Blockchain = MakeBlockchain()
            Assert.AreEqual(1, chain.BlockCount)
        End Sub

        <TestMethod>
        Public Sub Parameters_ReturnsChainParameters()
            Dim chain As Blockchain = MakeBlockchain()
            Assert.IsNotNull(chain.Parameters)
        End Sub

        <TestMethod>
        Public Sub GetBlockHashes_FromZero_ReturnsGenesisHash()
            Dim chain As Blockchain = MakeBlockchain()
            Dim hashes As List(Of String) = chain.GetBlockHashes(0, 1)
            Assert.AreEqual(1, hashes.Count)
            Assert.AreEqual(chain.Tip.Hash, hashes(0))
        End Sub

        <TestMethod>
        Public Sub GetNextDifficulty_AtGenesis_ReturnsMinDifficulty()
            Dim chain As Blockchain = MakeBlockchain()
            Dim nextBits As UInteger = chain.GetNextDifficulty(chain.Tip.Hash)
            Assert.AreEqual(DifficultyCalculator.MinDifficultyBits, nextBits)
        End Sub

        <TestMethod>
        Public Sub Tip_IsNotNull()
            Dim chain As Blockchain = MakeBlockchain()
            Assert.IsNotNull(chain.Tip)
        End Sub

        <TestMethod>
        Public Sub Tip_PreviousHashIsAllZeros_ForGenesis()
            Dim chain As Blockchain = MakeBlockchain()
            Assert.AreEqual(New String("0"c, 64), chain.Tip.PreviousHash)
        End Sub

    End Class

End Namespace
