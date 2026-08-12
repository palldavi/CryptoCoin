Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports CryptoCoin.Core
Imports CryptoCoin.Transactions
Imports CryptoCoin.Services.Implementations
Imports CryptoCoin.Services.DataContracts

Namespace CryptoCoin.Tests.Services

    ''' <summary>
    ''' Unit tests for BlockchainServiceImpl.
    ''' Tests the WCF service implementation directly against a real in-memory
    ''' blockchain — no HTTP or WCF channel needed.
    ''' </summary>
    <TestClass>
    Public Class BlockchainServiceTests

        Private _blockchain As Blockchain
        Private _mempool As Mempool
        Private _params As ChainParameters
        Private _service As BlockchainServiceImpl

        <TestInitialize>
        Public Sub Setup()
            _params = ChainParameters.Regtest()
            _blockchain = New Blockchain(_params)
            _mempool = New Mempool()
            _service = New BlockchainServiceImpl(_blockchain, _mempool, _params)
        End Sub

        <TestMethod>
        Public Sub GetBlockCount_FreshChain_ReturnsZero()
            Assert.AreEqual(0, _service.GetBlockCount())
        End Sub

        <TestMethod>
        Public Sub GetBestBlockHash_FreshChain_ReturnsGenesisHash()
            Dim hash As String = _service.GetBestBlockHash()
            Assert.IsFalse(String.IsNullOrEmpty(hash))
            Assert.AreEqual(64, hash.Length)
            Assert.AreEqual(_blockchain.Tip.Hash, hash)
        End Sub

        <TestMethod>
        Public Sub GetBlock_GenesisHash_ReturnsBlockData()
            Dim hash As String = _blockchain.Tip.Hash
            Dim data As BlockData = _service.GetBlock(hash)
            Assert.IsNotNull(data)
            Assert.AreEqual(hash, data.Hash)
            Assert.AreEqual(0, data.Height)
        End Sub

        <TestMethod>
        Public Sub GetBlock_UnknownHash_ReturnsNothing()
            Dim data As BlockData = _service.GetBlock(New String("a"c, 64))
            Assert.IsNull(data)
        End Sub

        <TestMethod>
        Public Sub GetBlockByHeight_Zero_ReturnsGenesisBlock()
            Dim data As BlockData = _service.GetBlockByHeight(0)
            Assert.IsNotNull(data)
            Assert.AreEqual(0, data.Height)
        End Sub

        <TestMethod>
        Public Sub GetBlockByHeight_BeyondTip_ReturnsNothing()
            Dim data As BlockData = _service.GetBlockByHeight(999)
            Assert.IsNull(data)
        End Sub

        <TestMethod>
        Public Sub GetLatestBlocks_FreshChain_ReturnsOneBlock()
            Dim result As BlockListData = _service.GetLatestBlocks()
            Assert.IsNotNull(result)
            Assert.AreEqual(1, result.Blocks.Count)
            Assert.AreEqual(1, result.TotalCount)
        End Sub

        <TestMethod>
        Public Sub GetLatestBlocks_BlockDataHasTransactionIds()
            Dim result As BlockListData = _service.GetLatestBlocks()
            Dim genesis As BlockData = result.Blocks(0)
            Assert.IsNotNull(genesis.TransactionIds)
            Assert.IsTrue(genesis.TransactionIds.Count > 0)
        End Sub

        <TestMethod>
        Public Sub GetNetworkStatus_ReturnsCorrectHeight()
            Dim status As NetworkStatusData = _service.GetNetworkStatus()
            Assert.IsNotNull(status)
            Assert.AreEqual(0, status.Height)
        End Sub

        <TestMethod>
        Public Sub GetNetworkStatus_CoinNameIsNotEmpty()
            Dim status As NetworkStatusData = _service.GetNetworkStatus()
            Assert.IsFalse(String.IsNullOrEmpty(status.CoinName))
            Assert.AreEqual("CryptoCoin", status.CoinName)
        End Sub

        <TestMethod>
        Public Sub GetNetworkStatus_CoinSymbolIsCRC()
            Dim status As NetworkStatusData = _service.GetNetworkStatus()
            Assert.AreEqual("CRC", status.CoinSymbol)
        End Sub

        <TestMethod>
        Public Sub GetMempool_EmptyMempool_ReturnsZeroCount()
            Dim data As MempoolData = _service.GetMempool()
            Assert.IsNotNull(data)
            Assert.AreEqual(0, data.TransactionCount)
        End Sub

        <TestMethod>
        Public Sub GetBlock_TransactionIdsMatchBlockchain()
            Dim hash As String = _blockchain.Tip.Hash
            Dim data As BlockData = _service.GetBlock(hash)
            Dim block As Block = _blockchain.GetBlock(hash)

            Assert.AreEqual(block.TransactionIds.Count, data.TransactionIds.Count)
            For i As Integer = 0 To block.TransactionIds.Count - 1
                Assert.AreEqual(block.TransactionIds(i), data.TransactionIds(i))
            Next
        End Sub

    End Class

End Namespace
