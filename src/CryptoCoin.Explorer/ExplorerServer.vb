Imports System.Net
Imports System.IO
Imports System.Text
Imports System.Threading
Imports CryptoCoin.Core
Imports CryptoCoin.Transactions

Namespace CryptoCoin.Explorer

    ''' <summary>
    ''' HTTP server for the block explorer API.
    ''' Pass a NodeProxy to forward queries to a live node instead of the local chain.
    ''' </summary>
    Public Class ExplorerServer

        Private ReadOnly _port As Integer
        Private ReadOnly _blockchain As Blockchain
        Private ReadOnly _mempool As Mempool
        Private ReadOnly _blockController As Controllers.BlockController
        Private ReadOnly _txController As Controllers.TransactionController
        Private ReadOnly _addressController As Controllers.AddressController
        Private ReadOnly _networkController As Controllers.NetworkController
        Private _listener As HttpListener
        Private _listenerThread As Thread
        Private _isRunning As Boolean

        Public Sub New(port As Integer, blockchain As Blockchain, mempool As Mempool,
                       Optional proxy As NodeProxy = Nothing,
                       Optional params As CryptoCoin.Core.ChainParameters = Nothing)
            _port = port
            _blockchain = blockchain
            _mempool = mempool
            _blockController   = New Controllers.BlockController(blockchain, proxy)
            _txController      = New Controllers.TransactionController(blockchain, mempool)
            _addressController = New Controllers.AddressController(blockchain)
            _networkController = New Controllers.NetworkController(blockchain, mempool, proxy, params)
        End Sub

        Public Sub Start()
            If _isRunning Then Return
            _isRunning = True

            _listener = New HttpListener()
            _listener.Prefixes.Add($"http://localhost:{_port}/")
            _listener.Prefixes.Add($"http://127.0.0.1:{_port}/")

            Try
                _listener.Start()
            Catch ex As HttpListenerException
                Console.WriteLine($"Explorer server failed to start: {ex.Message}")
                _isRunning = False
                Return
            End Try

            _listenerThread = New Thread(AddressOf ListenLoop)
            _listenerThread.IsBackground = True
            _listenerThread.Name = "ExplorerServer"
            _listenerThread.Start()
        End Sub

        Public Sub [Stop]()
            If Not _isRunning Then Return
            _isRunning = False
            Try
                _listener.Stop()
                _listener.Close()
            Catch ex As Exception
            End Try
        End Sub

        Private Sub ListenLoop()
            While _isRunning
                Try
                    Dim context As HttpListenerContext = _listener.GetContext()
                    ThreadPool.QueueUserWorkItem(Sub(state As Object) HandleRequest(context))
                Catch ex As HttpListenerException
                    If _isRunning Then Console.WriteLine($"Explorer listener error: {ex.Message}")
                Catch ex As ObjectDisposedException
                End Try
            End While
        End Sub

        Private Sub HandleRequest(context As HttpListenerContext)
            Try
                Dim path As String = context.Request.Url.AbsolutePath.ToLower().TrimEnd("/"c)
                Dim response As String = RouteRequest(path)

                Dim buffer As Byte() = Encoding.UTF8.GetBytes(response)
                context.Response.ContentType = "application/json"
                context.Response.ContentLength64 = buffer.Length
                context.Response.Headers.Add("Access-Control-Allow-Origin", "*")
                context.Response.OutputStream.Write(buffer, 0, buffer.Length)
                context.Response.Close()
            Catch ex As Exception
                Try
                    Dim errJson As Byte() = Encoding.UTF8.GetBytes($"{{""error"":""{ex.Message.Replace("""", "'")}""}}") 
                    context.Response.StatusCode = 500
                    context.Response.ContentType = "application/json"
                    context.Response.Headers.Add("Access-Control-Allow-Origin", "*")
                    context.Response.ContentLength64 = errJson.Length
                    context.Response.OutputStream.Write(errJson, 0, errJson.Length)
                    context.Response.Close()
                Catch
                End Try
            End Try
        End Sub

        Private Function RouteRequest(path As String) As String
            If path.StartsWith("/api/block/") Then
                Dim hash As String = path.Substring("/api/block/".Length)
                Return _blockController.GetBlock(hash)
            End If
            If path.StartsWith("/api/blocks/height/") Then
                Dim heightStr As String = path.Substring("/api/blocks/height/".Length)
                Dim height As Integer = Integer.Parse(heightStr)
                Return _blockController.GetBlockByHeight(height)
            End If
            If path = "/api/blocks/latest" Then Return _blockController.GetLatestBlocks()
            If path.StartsWith("/api/tx/") Then
                Dim txId As String = path.Substring("/api/tx/".Length)
                Return _txController.GetTransaction(txId)
            End If
            If path = "/api/mempool" Then Return _txController.GetMempoolInfo()
            If path.StartsWith("/api/address/") Then
                Dim address As String = path.Substring("/api/address/".Length)
                Return _addressController.GetAddressInfo(address)
            End If
            If path = "/api/network" Then Return _networkController.GetNetworkInfo()
            If path = "/api/status"  Then Return _networkController.GetStatus()
            Return "{""error"":""Not found""}"
        End Function

    End Class

End Namespace
