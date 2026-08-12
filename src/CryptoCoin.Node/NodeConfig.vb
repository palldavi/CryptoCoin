Namespace CryptoCoin.Node

    ''' <summary>
    ''' Configuration settings for the CryptoCoin node.
    ''' </summary>
    Public Class NodeConfig

        ''' <summary>
        ''' Network type: mainnet, testnet, or regtest.
        ''' </summary>
        Public Property Network As String = "mainnet"

        ''' <summary>
        ''' RPC server port.
        ''' </summary>
        Public Property RpcPort As Integer = 8332

        ''' <summary>
        ''' Explorer API port (0 = disabled).
        ''' </summary>
        Public Property ExplorerPort As Integer = 0

        ''' <summary>
        ''' Miner address for block rewards (empty = no mining).
        ''' </summary>
        Public Property MinerAddress As String = ""

        ''' <summary>
        ''' Number of mining threads (0 = auto).
        ''' </summary>
        Public Property MiningThreads As Integer = 0

        ''' <summary>
        ''' Whether to enable the RPC server.
        ''' </summary>
        Public Property EnableRpc As Boolean = True

        ''' <summary>
        ''' Data directory path (used for SQLite blockchain database).
        ''' </summary>
        Public Property DataDir As String = "data"

        ''' <summary>
        ''' Whether to persist the blockchain to SQLite (default: False).
        ''' Set to True and ensure SQLite.Interop.dll is present to enable persistence.
        ''' </summary>
        Public Property Persist As Boolean = False

        ''' <summary>
        ''' WCF service port (0 = disabled). Default: 8090.
        ''' Services hosted at http://localhost:{WcfPort}/cryptocoin/
        ''' </summary>
        Public Property WcfPort As Integer = 0

        ''' <summary>
        ''' Shared secret API key for WCF service authentication.
        ''' Clients must include this in the ApiKey SOAP header.
        ''' </summary>
        Public Property WcfApiKey As String = "cryptocoin-demo-key"

    End Class

End Namespace
