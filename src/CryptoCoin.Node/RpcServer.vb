Imports System.Net
Imports System.IO
Imports System.Text
Imports System.Threading

Namespace CryptoCoin.Node

    ''' <summary>
    ''' Simple HTTP-based JSON-RPC server for the node.
    ''' </summary>
    Public Class RpcServer

        Private ReadOnly _port As Integer
        Private ReadOnly _handler As RpcHandler
        Private _listener As HttpListener
        Private _listenerThread As Thread
        Private _isRunning As Boolean

        Public Sub New(port As Integer, handler As RpcHandler)
            _port = port
            _handler = handler
        End Sub

        ''' <summary>
        ''' Starts the RPC server.
        ''' </summary>
        Public Sub Start()
            If _isRunning Then Return
            _isRunning = True

            _listener = New HttpListener()
            _listener.Prefixes.Add($"http://localhost:{_port}/")
            _listener.Prefixes.Add($"http://127.0.0.1:{_port}/")

            Try
                _listener.Start()
            Catch ex As HttpListenerException
                Console.WriteLine($"RPC server failed to start: {ex.Message}")
                _isRunning = False
                Return
            End Try

            _listenerThread = New Thread(AddressOf ListenLoop)
            _listenerThread.IsBackground = True
            _listenerThread.Name = "RpcServer"
            _listenerThread.Start()
        End Sub

        ''' <summary>
        ''' Stops the RPC server.
        ''' </summary>
        Public Sub [Stop]()
            If Not _isRunning Then Return
            _isRunning = False

            Try
                _listener.Stop()
                _listener.Close()
            Catch ex As Exception
                ' Ignore shutdown errors
            End Try
        End Sub

        Private Sub ListenLoop()
            While _isRunning
                Try
                    Dim context As HttpListenerContext = _listener.GetContext()
                    ThreadPool.QueueUserWorkItem(Sub(state As Object) HandleRequest(context))
                Catch ex As HttpListenerException
                    If _isRunning Then
                        Console.WriteLine($"RPC listener error: {ex.Message}")
                    End If
                Catch ex As ObjectDisposedException
                    ' Listener was closed
                End Try
            End While
        End Sub

        Private Sub HandleRequest(context As HttpListenerContext)
            Try
                Dim request As HttpListenerRequest = context.Request
                Dim response As HttpListenerResponse = context.Response

                If request.HttpMethod <> "POST" Then
                    response.StatusCode = 405
                    response.Close()
                    Return
                End If

                ' Read request body
                Dim body As String = ""
                Using reader As New StreamReader(request.InputStream, Encoding.UTF8)
                    body = reader.ReadToEnd()
                End Using

                ' Process RPC call
                Dim result As String = _handler.HandleRequest(body)

                ' Write response
                Dim buffer As Byte() = Encoding.UTF8.GetBytes(result)
                response.ContentType = "application/json"
                response.ContentLength64 = buffer.Length
                response.OutputStream.Write(buffer, 0, buffer.Length)
                response.Close()
            Catch ex As Exception
                Try
                    Dim errJson As Byte() = Encoding.UTF8.GetBytes($"{{""error"":""{ex.Message.Replace("""", "'")}""}}") 
                    context.Response.StatusCode = 500
                    context.Response.ContentType = "application/json"
                    context.Response.ContentLength64 = errJson.Length
                    context.Response.OutputStream.Write(errJson, 0, errJson.Length)
                    context.Response.Close()
                Catch
                    ' Ignore
                End Try
            End Try
        End Sub

    End Class

End Namespace
