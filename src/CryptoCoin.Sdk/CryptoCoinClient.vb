' ===============================================================================
' CryptoCoin.Sdk - CryptoCoinClient.vb
' Main SDK client that connects to a CryptoCoin node via JSON-RPC.
' Provides typed methods for all common node operations.
' ===============================================================================

Imports System
Imports System.Collections.Generic
Imports CryptoCoin.Sdk.Models
Imports CryptoCoin.Sdk.Exceptions

Namespace CryptoCoin.Sdk

    ''' <summary>
    ''' Main client for interacting with a CryptoCoin node.
    ''' Provides strongly-typed methods for querying blockchain data, submitting transactions,
    ''' and monitoring network status via the node's JSON-RPC interface.
    ''' </summary>
    Public Class CryptoCoinClient
        Implements IDisposable

        Private ReadOnly _rpcClient As RpcClient
        Private _disposed As Boolean

        ''' <summary>Gets the RPC endpoint URL this client is connected to.</summary>
        Public ReadOnly Property Endpoint As String
            Get
                Return _rpcClient.Endpoint
            End Get
        End Property

        ''' <summary>Gets whether the client is connected and responsive.</summary>
        Public ReadOnly Property IsConnected As Boolean
            Get
                Try
                    GetBlockCount()
                    Return True
                Catch
                    Return False
                End Try
            End Get
        End Property

        ''' <summary>
        ''' Initializes a new CryptoCoinClient connecting to the specified node.
        ''' </summary>
        ''' <param name="host">The node hostname or IP address.</param>
        ''' <param name="port">The RPC port number (default: 8332).</param>
        ''' <param name="username">The RPC authentication username.</param>
        ''' <param name="password">The RPC authentication password.</param>
        Public Sub New(host As String, Optional port As Integer = 8332,
                       Optional username As String = "cryptocoin",
                       Optional password As String = "changeme")
            Dim endpoint As String = $"http://{host}:{port}/"
            _rpcClient = New RpcClient(endpoint, username, password)
        End Sub

        ''' <summary>
        ''' Initializes a new CryptoCoinClient with a pre-configured RPC client.
        ''' </summary>
        ''' <param name="rpcClient">The RPC client instance to use.</param>
        Public Sub New(rpcClient As RpcClient)
            _rpcClient = rpcClient
        End Sub

        ' --- Blockchain Methods ---

        ''' <summary>
        ''' Gets the current block height of the blockchain.
        ''' </summary>
        ''' <returns>The current block count/height.</returns>
        ''' <exception cref="RpcException">Thrown when the RPC call fails.</exception>
        Public Function GetBlockCount() As Integer
            Dim result As String = _rpcClient.Call("getblockcount")
            Dim count As Integer
            If Integer.TryParse(result, count) Then
                Return count
            End If
            Throw New RpcException(-1, "Invalid response for getblockcount")
        End Function

        ''' <summary>
        ''' Gets the block hash at the specified height.
        ''' </summary>
        ''' <param name="height">The block height.</param>
        ''' <returns>The block hash as a hex string.</returns>
        Public Function GetBlockHash(height As Integer) As String
            Return _rpcClient.Call("getblockhash", height.ToString())
        End Function

        ''' <summary>
        ''' Gets detailed block information by hash.
        ''' </summary>
        ''' <param name="blockHash">The block hash as a hex string.</param>
        ''' <returns>A BlockInfo object with the block details.</returns>
        Public Function GetBlock(blockHash As String) As BlockInfo
            Dim json As String = _rpcClient.Call("getblock", $"""{blockHash}""")
            Return BlockInfo.FromJson(json)
        End Function

        ''' <summary>
        ''' Gets detailed block information by height.
        ''' </summary>
        ''' <param name="height">The block height.</param>
        ''' <returns>A BlockInfo object with the block details.</returns>
        Public Function GetBlockByHeight(height As Integer) As BlockInfo
            Dim hash As String = GetBlockHash(height)
            Return GetBlock(hash.Trim(""""c))
        End Function

        ''' <summary>
        ''' Gets the best (latest) block hash.
        ''' </summary>
        ''' <returns>The hash of the latest block.</returns>
        Public Function GetBestBlockHash() As String
            Dim height As Integer = GetBlockCount()
            Return GetBlockHash(height)
        End Function

        ' --- Transaction Methods ---

        ''' <summary>
        ''' Gets transaction information by transaction ID.
        ''' </summary>
        ''' <param name="txId">The transaction ID (hash) as a hex string.</param>
        ''' <returns>A TransactionInfo object with the transaction details.</returns>
        Public Function GetTransaction(txId As String) As TransactionInfo
            Dim json As String = _rpcClient.Call("gettransaction", $"""{txId}""")
            Return TransactionInfo.FromJson(json)
        End Function

        ''' <summary>
        ''' Submits a raw transaction to the network.
        ''' </summary>
        ''' <param name="rawTxHex">The serialized transaction in hex format.</param>
        ''' <returns>The transaction ID if accepted.</returns>
        ''' <exception cref="RpcException">Thrown if the transaction is rejected.</exception>
        Public Function SendRawTransaction(rawTxHex As String) As String
            Dim result As String = _rpcClient.Call("sendrawtransaction", $"""{rawTxHex}""")
            Return result.Trim(""""c)
        End Function

        ''' <summary>
        ''' Decodes a raw transaction hex without broadcasting it.
        ''' </summary>
        ''' <param name="rawTxHex">The serialized transaction in hex format.</param>
        ''' <returns>A TransactionInfo object with the decoded transaction.</returns>
        Public Function DecodeRawTransaction(rawTxHex As String) As TransactionInfo
            Dim json As String = _rpcClient.Call("decoderawtransaction", $"""{rawTxHex}""")
            Return TransactionInfo.FromJson(json)
        End Function

        ' --- Network Methods ---

        ''' <summary>
        ''' Gets network information including connections and protocol version.
        ''' </summary>
        ''' <returns>A NetworkInfo object with network details.</returns>
        Public Function GetNetworkInfo() As NetworkInfo
            Dim json As String = _rpcClient.Call("getnetworkinfo")
            Return NetworkInfo.FromJson(json)
        End Function

        ''' <summary>
        ''' Gets information about connected peers.
        ''' </summary>
        ''' <returns>A list of peer information dictionaries.</returns>
        Public Function GetPeerInfo() As String
            Return _rpcClient.Call("getpeerinfo")
        End Function

        ''' <summary>
        ''' Gets the number of connected peers.
        ''' </summary>
        ''' <returns>The connection count.</returns>
        Public Function GetConnectionCount() As Integer
            Dim info As NetworkInfo = GetNetworkInfo()
            Return info.Connections
        End Function

        ' --- Mining Methods ---

        ''' <summary>
        ''' Gets mining-related information.
        ''' </summary>
        ''' <returns>A dictionary with mining information.</returns>
        Public Function GetMiningInfo() As String
            Return _rpcClient.Call("getmininginfo")
        End Function

        ' --- Mempool Methods ---

        ''' <summary>
        ''' Gets mempool information including size and transaction count.
        ''' </summary>
        ''' <returns>The mempool info as a JSON string.</returns>
        Public Function GetMempoolInfo() As String
            Return _rpcClient.Call("getmempoolinfo")
        End Function

        ' --- Wallet Methods ---

        ''' <summary>
        ''' Gets the wallet balance.
        ''' </summary>
        ''' <returns>The wallet balance in CRC.</returns>
        Public Function GetBalance() As Decimal
            Dim result As String = _rpcClient.Call("getbalance")
            Dim balance As Decimal
            If Decimal.TryParse(result, balance) Then
                Return balance
            End If
            Return 0D
        End Function

        ' --- Utility Methods ---

        ''' <summary>
        ''' Gets general node information.
        ''' </summary>
        ''' <returns>The node info as a JSON string.</returns>
        Public Function GetInfo() As String
            Return _rpcClient.Call("getinfo")
        End Function

        ''' <summary>
        ''' Sends a stop command to the node for graceful shutdown.
        ''' </summary>
        Public Sub StopNode()
            Try
                _rpcClient.Call("stop")
            Catch
                ' Node may disconnect before responding
            End Try
        End Sub

        ''' <summary>
        ''' Pings the node to check connectivity.
        ''' </summary>
        ''' <returns>True if the node responds; otherwise, False.</returns>
        Public Function Ping() As Boolean
            Try
                GetBlockCount()
                Return True
            Catch
                Return False
            End Try
        End Function

        ''' <summary>
        ''' Creates a new TransactionRequestBuilder for constructing transactions.
        ''' </summary>
        ''' <returns>A new TransactionRequestBuilder instance.</returns>
        Public Function CreateTransaction() As Builders.TransactionRequestBuilder
            Return New Builders.TransactionRequestBuilder(Me)
        End Function

        ''' <summary>
        ''' Disposes of the client and releases resources.
        ''' </summary>
        Public Sub Dispose() Implements IDisposable.Dispose
            If Not _disposed Then
                _rpcClient.Dispose()
                _disposed = True
            End If
        End Sub

    End Class

End Namespace
