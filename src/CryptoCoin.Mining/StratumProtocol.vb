Imports System.IO
Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports System.Threading

Namespace CryptoCoin.Mining

    ''' <summary>
    ''' Implements the Stratum mining protocol for pool-miner communication.
    ''' Handles JSON-RPC messages over TCP.
    ''' </summary>
    Public Class StratumProtocol

        Private ReadOnly _pool As MiningPool
        Private _listener As TcpListener
        Private _isRunning As Boolean
        Private ReadOnly _clients As New List(Of StratumClient)()
        Private ReadOnly _syncLock As New Object()
        Private _nextId As Integer = 1

        ''' <summary>
        ''' Gets whether the stratum server is running.
        ''' </summary>
        Public ReadOnly Property IsRunning As Boolean
            Get
                Return _isRunning
            End Get
        End Property

        ''' <summary>
        ''' Gets the number of connected clients.
        ''' </summary>
        Public ReadOnly Property ClientCount As Integer
            Get
                SyncLock _syncLock
                    Return _clients.Count
                End SyncLock
            End Get
        End Property

        Public Sub New(pool As MiningPool)
            If pool Is Nothing Then Throw New ArgumentNullException(NameOf(pool))
            _pool = pool
        End Sub

        ''' <summary>
        ''' Starts the stratum server on the specified port.
        ''' </summary>
        Public Sub Start(port As Integer)
            If _isRunning Then Return

            _listener = New TcpListener(IPAddress.Any, port)
            _listener.Start()
            _isRunning = True

            Dim acceptThread As New Thread(AddressOf AcceptLoop)
            acceptThread.IsBackground = True
            acceptThread.Name = "Stratum-Accept"
            acceptThread.Start()
        End Sub

        ''' <summary>
        ''' Stops the stratum server.
        ''' </summary>
        Public Sub [Stop]()
            _isRunning = False
            _listener?.Stop()

            SyncLock _syncLock
                For Each client As Object In _clients
                    client.Disconnect()
                Next
                _clients.Clear()
            End SyncLock
        End Sub

        Private Sub AcceptLoop()
            Try
                While _isRunning
                    Dim tcpClient As TcpClient = _listener.AcceptTcpClient()
                    Dim client As New StratumClient(tcpClient, _nextId)
                    _nextId += 1

                    SyncLock _syncLock
                        _clients.Add(client)
                    End SyncLock

                    Dim clientThread As New Thread(Sub() HandleClient(client))
                    clientThread.IsBackground = True
                    clientThread.Name = $"Stratum-Client-{client.Id}"
                    clientThread.Start()
                End While
            Catch ex As SocketException
                ' Server stopped
            End Try
        End Sub

        Private Sub HandleClient(client As StratumClient)
            Try
                Using reader As New StreamReader(client.GetStream(), Encoding.UTF8)
                    While _isRunning AndAlso client.IsConnected
                        Dim line As String = reader.ReadLine()
                        If line Is Nothing Then Exit While

                        ProcessMessage(client, line)
                    End While
                End Using
            Catch
                ' Client disconnected
            Finally
                SyncLock _syncLock
                    _clients.Remove(client)
                End SyncLock
                client.Disconnect()
            End Try
        End Sub

        Private Sub ProcessMessage(client As StratumClient, message As String)
            ' Simple JSON-RPC parsing (in production, use a proper JSON library)
            If message.Contains("""mining.subscribe""") Then
                HandleSubscribe(client, message)
            ElseIf message.Contains("""mining.authorize""") Then
                HandleAuthorize(client, message)
            ElseIf message.Contains("""mining.submit""") Then
                HandleSubmit(client, message)
            End If
        End Sub

        Private Sub HandleSubscribe(client As StratumClient, message As String)
            ' Send subscription response
            Dim response As String = $"{{""id"":1,""result"":[[""mining.notify"",""{client.Id:X8}""],""{Guid.NewGuid():N}"",4],""error"":null}}"
            SendToClient(client, response)
        End Sub

        Private Sub HandleAuthorize(client As StratumClient, message As String)
            ' Extract worker name (simplified parsing)
            Dim workerName As String = $"worker_{client.Id}"
            client.WorkerName = workerName
            client.IsAuthorized = True

            ' Register with pool
            _pool.RegisterWorker(workerName, "")

            Dim response As String = $"{{""id"":2,""result"":true,""error"":null}}"
            SendToClient(client, response)

            ' Send current job
            SendJob(client)
        End Sub

        Private Sub HandleSubmit(client As StratumClient, message As String)
            ' Simplified share submission handling
            If Not client.IsAuthorized Then
                Dim errorResponse As String = $"{{""id"":3,""result"":false,""error"":[24,""Unauthorized"",null]}}"
                SendToClient(client, errorResponse)
                Return
            End If

            ' In production, parse nonce from message and validate
            Dim result As ShareResult = _pool.SubmitShare(client.WorkerName, 0, "")
            Dim accepted As String = If(result.Accepted, "true", "false")
            Dim response As String = $"{{""id"":3,""result"":{accepted},""error"":null}}"
            SendToClient(client, response)
        End Sub

        ''' <summary>
        ''' Sends a new job to a specific client.
        ''' </summary>
        Private Sub SendJob(client As StratumClient)
            Dim job As MiningJob = _pool.CurrentJob
            If job Is Nothing Then Return

            Dim notify As String = $"{{""id"":null,""method"":""mining.notify"",""params"":[""{job.JobId}"",""{job.Block.Header.PreviousBlockHash}"",""{job.Block.Header.MerkleRoot}"",""{job.Block.Header.Timestamp}"",""{job.TargetBits:X8}"",true]}}"
            SendToClient(client, notify)
        End Sub

        ''' <summary>
        ''' Broadcasts a new job to all connected clients.
        ''' </summary>
        Public Sub BroadcastJob(job As MiningJob)
            _pool.UpdateJob(job)

            SyncLock _syncLock
                For Each client As Object In _clients
                    If client.IsAuthorized Then
                        SendJob(client)
                    End If
                Next
            End SyncLock
        End Sub

        ''' <summary>
        ''' Sends a difficulty update to all clients.
        ''' </summary>
        Public Sub BroadcastDifficulty(difficulty As Double)
            Dim message As String = $"{{""id"":null,""method"":""mining.set_difficulty"",""params"":[{difficulty}]}}"
            SyncLock _syncLock
                For Each client As Object In _clients
                    SendToClient(client, message)
                Next
            End SyncLock
        End Sub

        Private Sub SendToClient(client As StratumClient, message As String)
            Try
                If client.IsConnected Then
                    Dim data As Byte() = Encoding.UTF8.GetBytes(message & vbLf)
                    client.GetStream().Write(data, 0, data.Length)
                End If
            Catch
                ' Client disconnected
            End Try
        End Sub

    End Class

    ''' <summary>
    ''' Represents a connected stratum mining client.
    ''' </summary>
    Public Class StratumClient

        Private ReadOnly _tcpClient As TcpClient

        Public ReadOnly Property Id As Integer
        Public Property WorkerName As String
        Public Property IsAuthorized As Boolean
        Public Property ConnectedAt As DateTimeOffset

        Public ReadOnly Property IsConnected As Boolean
            Get
                Return _tcpClient?.Connected
            End Get
        End Property

        Public Sub New(tcpClient As TcpClient, id As Integer)
            _tcpClient = tcpClient
            Me.Id = id
            ConnectedAt = DateTimeOffset.UtcNow
        End Sub

        Public Function GetStream() As NetworkStream
            Return _tcpClient.GetStream()
        End Function

        Public Sub Disconnect()
            Try
                _tcpClient?.Close()
            Catch
            End Try
        End Sub

    End Class

End Namespace
