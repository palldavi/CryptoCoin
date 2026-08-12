Imports System.ServiceModel
Imports CryptoCoin.Services.DataContracts

Namespace CryptoCoin.Services.Contracts

    ''' <summary>
    ''' WCF service contract exposing blockchain query operations.
    ''' All operations require a valid API key in the custom SOAP header.
    '''
    ''' Modernisation note: on .NET 10 this would be a gRPC service definition
    ''' (.proto file) or a minimal API controller, with authentication handled
    ''' by ASP.NET Core middleware rather than a custom message inspector.
    ''' </summary>
    <ServiceContract(Namespace:="http://cryptocoin.services/2024/blockchain",
                     Name:="IBlockchainService")>
    Public Interface IBlockchainService

        ''' <summary>Returns the current chain height.</summary>
        <OperationContract()>
        Function GetBlockCount() As Integer

        ''' <summary>Returns the hash of the best (tip) block.</summary>
        <OperationContract()>
        Function GetBestBlockHash() As String

        ''' <summary>Returns block details by hash.</summary>
        <OperationContract()>
        Function GetBlock(hash As String) As BlockData

        ''' <summary>Returns block details by height.</summary>
        <OperationContract()>
        Function GetBlockByHeight(height As Integer) As BlockData

        ''' <summary>Returns the latest N blocks (up to 10).</summary>
        <OperationContract()>
        Function GetLatestBlocks() As BlockListData

        ''' <summary>Returns current network status and statistics.</summary>
        <OperationContract()>
        Function GetNetworkStatus() As NetworkStatusData

        ''' <summary>Returns current mempool contents.</summary>
        <OperationContract()>
        Function GetMempool() As MempoolData

    End Interface

End Namespace
