Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports CryptoCoin.Mining
Imports CryptoCoin.Core
Imports CryptoCoin.Transactions
Imports CryptoCoin.Cryptography

Namespace CryptoCoin.Tests.Mining

    <TestClass>
    Public Class MinerTests

        Private Function MakeAddress() As String
            Dim kp As New KeyPair()
            Return AddressEncoder.FromKeyPair(kp)
        End Function

        Private Function MakeSetup() As Tuple(Of Blockchain, Miner)
            Dim params As ChainParameters = ChainParameters.Regtest()
            Dim blockchain As New Blockchain(params)
            Dim mempool As New Mempool()
            Dim assembler As New BlockAssembler(mempool, params)
            Dim miner As New Miner(blockchain, assembler)
            Return Tuple.Create(blockchain, miner)
        End Function

        <TestMethod>
        Public Sub New_IsMiningIsFalse()
            Dim setup As Tuple(Of Blockchain, Miner) = MakeSetup()
            Assert.IsFalse(setup.Item2.IsMining)
        End Sub

        <TestMethod>
        Public Sub New_HashRateIsZero()
            Dim setup As Tuple(Of Blockchain, Miner) = MakeSetup()
            Assert.AreEqual(0.0, setup.Item2.HashRate)
        End Sub

        <TestMethod>
        Public Sub New_BlocksMinedIsZero()
            Dim setup As Tuple(Of Blockchain, Miner) = MakeSetup()
            Assert.AreEqual(0, setup.Item2.BlocksMined)
        End Sub

        <TestMethod>
        Public Sub Start_SetsIsMiningTrue()
            Dim setup As Tuple(Of Blockchain, Miner) = MakeSetup()
            setup.Item2.Start(MakeAddress(), 1)
            Assert.IsTrue(setup.Item2.IsMining)
            setup.Item2.Stop()
        End Sub

        <TestMethod>
        Public Sub Stop_SetsIsMiningFalse()
            Dim setup As Tuple(Of Blockchain, Miner) = MakeSetup()
            setup.Item2.Start(MakeAddress(), 1)
            setup.Item2.Stop()
            Assert.IsFalse(setup.Item2.IsMining)
        End Sub

        <TestMethod>
        Public Sub MineSingleBlock_RegtestDifficulty_MinesBlock()
            Dim setup As Tuple(Of Blockchain, Miner) = MakeSetup()
            Dim block As Block = setup.Item2.MineSingleBlock(MakeAddress())
            Assert.IsNotNull(block, "Should mine a block at regtest difficulty")
        End Sub

        <TestMethod>
        Public Sub MineSingleBlock_IncreasesChainHeight()
            Dim setup As Tuple(Of Blockchain, Miner) = MakeSetup()
            Dim heightBefore As Integer = setup.Item1.Height
            setup.Item2.MineSingleBlock(MakeAddress())
            Assert.AreEqual(heightBefore + 1, setup.Item1.Height)
        End Sub

        <TestMethod>
        Public Sub MineSingleBlock_IncreasesBlocksMined()
            Dim setup As Tuple(Of Blockchain, Miner) = MakeSetup()
            setup.Item2.MineSingleBlock(MakeAddress())
            Assert.AreEqual(1, setup.Item2.BlocksMined)
        End Sub

        <TestMethod>
        Public Sub MineSingleBlock_MinedBlockHasValidHash()
            Dim setup As Tuple(Of Blockchain, Miner) = MakeSetup()
            Dim block As Block = setup.Item2.MineSingleBlock(MakeAddress())
            Assert.IsNotNull(block)
            Assert.AreEqual(64, block.Hash.Length)
        End Sub

        <TestMethod>
        Public Sub MineSingleBlock_MinedBlockHasCoinbaseTx()
            Dim setup As Tuple(Of Blockchain, Miner) = MakeSetup()
            Dim block As Block = setup.Item2.MineSingleBlock(MakeAddress())
            Assert.IsNotNull(block)
            Assert.IsTrue(block.TransactionIds.Count >= 1)
        End Sub

        <TestMethod>
        Public Sub MineSingleBlock_Twice_ChainHeightIsTwo()
            Dim setup As Tuple(Of Blockchain, Miner) = MakeSetup()
            Dim address As String = MakeAddress()
            setup.Item2.MineSingleBlock(address)
            setup.Item2.MineSingleBlock(address)
            Assert.AreEqual(2, setup.Item1.Height)
        End Sub

        <TestMethod>
        Public Sub BlockAssembler_CreateJob_ReturnsJob()
            Dim params As ChainParameters = ChainParameters.Regtest()
            Dim blockchain As New Blockchain(params)
            Dim mempool As New Mempool()
            Dim assembler As New BlockAssembler(mempool, params)
            Dim job As MiningJob = assembler.CreateJob(MakeAddress(), blockchain)
            Assert.IsNotNull(job)
            Assert.IsNotNull(job.Block)
            Assert.IsNotNull(job.JobId)
        End Sub

        <TestMethod>
        Public Sub BlockAssembler_CreateJob_BlockHeightIsChainHeightPlusOne()
            Dim params As ChainParameters = ChainParameters.Regtest()
            Dim blockchain As New Blockchain(params)
            Dim mempool As New Mempool()
            Dim assembler As New BlockAssembler(mempool, params)
            Dim job As MiningJob = assembler.CreateJob(MakeAddress(), blockchain)
            Assert.AreEqual(blockchain.Height + 1, job.Block.Height)
        End Sub

    End Class

End Namespace
