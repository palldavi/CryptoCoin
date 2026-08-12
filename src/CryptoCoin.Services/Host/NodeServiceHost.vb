Imports System.ServiceModel
Imports System.ServiceModel.Description
Imports CryptoCoin.Core
Imports CryptoCoin.Transactions
Imports CryptoCoin.Services.Contracts
Imports CryptoCoin.Services.Implementations
Imports CryptoCoin.Services.Security

Namespace CryptoCoin.Services.Host

    ''' <summary>
    ''' Self-hosted WCF service host for the CryptoCoin Node.
    ''' Hosts IBlockchainService and IWalletService on BasicHttpBinding.
    ''' All endpoints are secured with a shared API key header.
    '''
    ''' Default base address: http://localhost:8090/cryptocoin/
    '''   IBlockchainService: http://localhost:8090/cryptocoin/blockchain
    '''   IWalletService:     http://localhost:8090/cryptocoin/wallet
    '''
    ''' Modernisation note: on .NET 10 this self-hosting pattern is replaced by
    ''' CoreWCF hosted in ASP.NET Core (app.UseServiceModel), or the services
    ''' are rewritten as gRPC or minimal API endpoints.
    ''' </summary>
    Public Class NodeServiceHost

        Private ReadOnly _port As Integer
        Private ReadOnly _apiKey As String
        Private _blockchainHost As ServiceHost
        Private _walletHost As ServiceHost
        Private _isRunning As Boolean

        Public Sub New(port As Integer, apiKey As String)
            _port = port
            _apiKey = apiKey
        End Sub

        ''' <summary>
        ''' Starts both WCF service hosts using the provided blockchain and mempool.
        ''' </summary>
        Public Sub Start(blockchain As Blockchain, mempool As Mempool, params As ChainParameters)
            If _isRunning Then Return

            ' Each service host gets its own base URI to avoid URI registration conflicts
            Dim blockchainBaseUri As New Uri($"http://localhost:{_port}/cryptocoin/blockchain")
            Dim walletBaseUri     As New Uri($"http://localhost:{_port}/cryptocoin/wallet")

            ' ── IBlockchainService ───────────────────────────────────────────
            Dim blockchainImpl As New BlockchainServiceImpl(blockchain, mempool, params)
            _blockchainHost = New ServiceHost(blockchainImpl, blockchainBaseUri)

            Dim blockchainBinding As New BasicHttpBinding()
            blockchainBinding.MaxReceivedMessageSize = 10 * 1024 * 1024  ' 10 MB
            blockchainBinding.HostNameComparisonMode = HostNameComparisonMode.Exact

            Dim blockchainEndpoint As ServiceEndpoint =
                _blockchainHost.AddServiceEndpoint(
                    GetType(IBlockchainService),
                    blockchainBinding,
                    "")   ' empty relative address — endpoint IS the base URI

            blockchainEndpoint.Behaviors.Add(New ApiKeyServiceBehavior(_apiKey))
            _blockchainHost.Open()

            ' ── IWalletService ───────────────────────────────────────────────
            Dim walletImpl As New WalletServiceImpl()
            _walletHost = New ServiceHost(walletImpl, walletBaseUri)

            Dim walletBinding As New BasicHttpBinding()
            walletBinding.HostNameComparisonMode = HostNameComparisonMode.Exact

            Dim walletEndpoint As ServiceEndpoint =
                _walletHost.AddServiceEndpoint(
                    GetType(IWalletService),
                    walletBinding,
                    "")   ' empty relative address — endpoint IS the base URI

            walletEndpoint.Behaviors.Add(New ApiKeyServiceBehavior(_apiKey))
            _walletHost.Open()

            _isRunning = True
        End Sub

        ''' <summary>Stops both WCF service hosts.</summary>
        Public Sub [Stop]()
            If Not _isRunning Then Return
            _isRunning = False
            Try
                _blockchainHost?.Close()
                _walletHost?.Close()
            Catch
                _blockchainHost?.Abort()
                _walletHost?.Abort()
            End Try
        End Sub

        ''' <summary>Gets the base URL for the blockchain service.</summary>
        Public ReadOnly Property BlockchainServiceUrl As String
            Get
                Return $"http://localhost:{_port}/cryptocoin/blockchain"
            End Get
        End Property

        ''' <summary>Gets the base URL for the wallet service.</summary>
        Public ReadOnly Property WalletServiceUrl As String
            Get
                Return $"http://localhost:{_port}/cryptocoin/wallet"
            End Get
        End Property

    End Class

End Namespace
