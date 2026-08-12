Imports CryptoCoin.Core
Imports CryptoCoin.Transactions
Imports CryptoCoin.Mining
Imports CryptoCoin.Explorer
Imports CryptoCoin.Persistence
Imports CryptoCoin.Node.Logging
Imports CryptoCoin.Node.Container
Imports CryptoCoin.Services.Host

Namespace CryptoCoin.Node

    ''' <summary>
    ''' Main node service that orchestrates blockchain, mempool, and miner.
    ''' Uses Castle.Windsor for dependency registration and NodeLogger for
    ''' structured logging via log4net + Enterprise Library.
    ''' </summary>
    Public Class NodeService

        Private ReadOnly _config As NodeConfig
        Private ReadOnly _blockchain As Blockchain
        Private ReadOnly _mempool As Mempool
        Private ReadOnly _assembler As BlockAssembler
        Private ReadOnly _miner As Miner
        Private ReadOnly _params As ChainParameters
        Private _rpcServer As RpcServer
        Private _explorerServer As ExplorerServer
        Private _wcfHost As NodeServiceHost
        Private _container As Castle.Windsor.IWindsorContainer
        Private _isRunning As Boolean

        Public ReadOnly Property Blockchain As Blockchain
            Get
                Return _blockchain
            End Get
        End Property

        Public ReadOnly Property Mempool As Mempool
            Get
                Return _mempool
            End Get
        End Property

        Public ReadOnly Property Miner As Miner
            Get
                Return _miner
            End Get
        End Property

        Public ReadOnly Property Parameters As ChainParameters
            Get
                Return _params
            End Get
        End Property

        Public ReadOnly Property IsRunning As Boolean
            Get
                Return _isRunning
            End Get
        End Property

        Public Sub New(config As NodeConfig)
            _config = config

            ' Configure logging first so all subsequent messages are captured
            Dim logDir As String = System.IO.Path.Combine(config.DataDir, "logs")
            NodeLogger.Configure(logDir)
            NodeLogger.Info("CryptoCoin Node initialising...")

            ' Select chain parameters based on network
            Select Case config.Network.ToLower()
                Case "testnet"
                    _params = ChainParameters.Testnet()
                Case "regtest"
                    _params = ChainParameters.Regtest()
                Case Else
                    _params = ChainParameters.Mainnet()
            End Select

            NodeLogger.Info($"Network: {config.Network}")

            ' Create blockchain — SQLite-backed if persistence is enabled
            If config.Persist Then
                Dim dataDir As String = System.IO.Path.Combine(config.DataDir, config.Network)
                _blockchain = PersistenceFactory.CreateBlockchain(dataDir, _params)
            Else
                _blockchain = New Blockchain(_params)
            End If

            _mempool = New Mempool()
            _assembler = New BlockAssembler(_mempool, _params)
            _miner = New Miner(_blockchain, _assembler)

            ' Register services with Castle.Windsor IoC container
            _container = NodeContainerFactory.Create(config, _params, _blockchain)
        End Sub

        ''' <summary>
        ''' Starts the node services.
        ''' </summary>
        Public Sub Start()
            If _isRunning Then Return
            _isRunning = True

            NodeLogger.Info($"Starting node on {_config.Network} network...")
            NodeLogger.Info($"Chain height: {_blockchain.Height}")
            NodeLogger.Info($"Genesis hash: {_blockchain.Tip.Hash.Substring(0, 16)}...")

            ' Keep console output for the user-facing startup banner
            Console.WriteLine($"Starting node on {_config.Network} network...")
            Console.WriteLine($"Chain height: {_blockchain.Height}")
            Console.WriteLine($"Genesis hash: {_blockchain.Tip.Hash.Substring(0, 16)}...")

            ' Start RPC server
            If _config.EnableRpc Then
                Dim handler As New RpcHandler(Me)
                _rpcServer = New RpcServer(_config.RpcPort, handler)
                _rpcServer.Start()
                NodeLogger.Info($"RPC server listening on port {_config.RpcPort}")
                Console.WriteLine($"RPC server listening on port {_config.RpcPort}")
            End If

            ' Start Explorer API server in-process if configured
            If _config.ExplorerPort > 0 Then
                _explorerServer = New ExplorerServer(_config.ExplorerPort, _blockchain, _mempool, Nothing, _params)
                _explorerServer.Start()
                NodeLogger.Info($"Explorer API listening on port {_config.ExplorerPort}")
                Console.WriteLine($"Explorer API listening on port {_config.ExplorerPort}")
            End If

            ' Start WCF services if configured
            If _config.WcfPort > 0 Then
                Try
                    _wcfHost = New NodeServiceHost(_config.WcfPort, _config.WcfApiKey)
                    _wcfHost.Start(_blockchain, _mempool, _params)
                    NodeLogger.Info($"WCF BlockchainService: {_wcfHost.BlockchainServiceUrl}")
                    NodeLogger.Info($"WCF WalletService:     {_wcfHost.WalletServiceUrl}")
                    NodeLogger.Info($"WCF API key:           {_config.WcfApiKey}")
                    Console.WriteLine($"WCF services on port {_config.WcfPort} (key: {_config.WcfApiKey})")
                Catch ex As Exception
                    NodeLogger.Error($"WCF host failed to start: {ex.Message}. " &
                                     "Try running VS as Administrator or use --wcf 0 to disable.", ex)
                    Console.WriteLine($"WARNING: WCF services failed to start: {ex.Message}")
                    _wcfHost = Nothing
                End Try
            End If

            ' Start mining if address configured
            If Not String.IsNullOrEmpty(_config.MinerAddress) Then
                _miner.Start(_config.MinerAddress, _config.MiningThreads)
                NodeLogger.Info($"Mining started → {_config.MinerAddress}")
                Console.WriteLine($"Mining started with address {_config.MinerAddress}")
            End If
        End Sub

        ''' <summary>
        ''' Stops the node services.
        ''' </summary>
        Public Sub [Stop]()
            If Not _isRunning Then Return
            _isRunning = False

            NodeLogger.Info("Stopping node services...")
            _miner.Stop()
            If _wcfHost IsNot Nothing Then _wcfHost.Stop()
            If _explorerServer IsNot Nothing Then _explorerServer.Stop()
            If _rpcServer IsNot Nothing Then _rpcServer.Stop()
            If _container IsNot Nothing Then _container.Dispose()

            NodeLogger.Info("Node stopped.")
            Console.WriteLine("Node services stopped.")
        End Sub

    End Class

End Namespace
