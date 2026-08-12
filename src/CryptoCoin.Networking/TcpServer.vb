Imports System.Net
Imports System.Net.Sockets
Imports System.Threading

Namespace CryptoCoin.Networking

    ''' <summary>
    ''' TCP listener that accepts incoming peer connections on the P2P network port.
    ''' Manages the server socket lifecycle, connection acceptance, and integration
    ''' with the PeerManager for new inbound connections.
    ''' </summary>
    Public Class TcpServer
        Implements IDisposable

        Private _listener As TcpListener
        Private _listenThread As Thread
        Private _isRunning As Boolean
        Private _disposed As Boolean
        Private ReadOnly _peerManager As PeerManager
        Private ReadOnly _banManager As BanManager
        Private ReadOnly _syncLock As New Object()

        ''' <summary>
        ''' The default P2P network port.
        ''' </summary>
        Public Const DefaultPort As Integer = 8333

        ''' <summary>
        ''' Maximum pending connection backlog.
        ''' </summary>
        Public Const ConnectionBacklog As Integer = 64

        ''' <summary>
        ''' Timeout for accepting connections in milliseconds.
        ''' </summary>
        Public Const AcceptTimeoutMs As Integer = 1000

        ''' <summary>
        ''' The IP address the server is bound to.
        ''' </summary>
        Public Property BindAddress As IPAddress

        ''' <summary>
        ''' The port the server is listening on.
        ''' </summary>
        Public Property ListenPort As Integer

        ''' <summary>
        ''' Whether the server is currently accepting connections.
        ''' </summary>
        Public ReadOnly Property IsRunning As Boolean
            Get
                Return _isRunning
            End Get
        End Property

        ''' <summary>
        ''' Total number of connections accepted since the server started.
        ''' </summary>
        Public Property TotalConnectionsAccepted As Long

        ''' <summary>
        ''' Total number of connections rejected (banned, limit reached, etc.).
        ''' </summary>
        Public Property TotalConnectionsRejected As Long

        ''' <summary>
        ''' Event raised when a new inbound connection is accepted.
        ''' </summary>
        Public Event ConnectionAccepted As EventHandler(Of TcpConnectionEventArgs)

        ''' <summary>
        ''' Event raised when a connection is rejected.
        ''' </summary>
        Public Event ConnectionRejected As EventHandler(Of TcpConnectionEventArgs)

        ''' <summary>
        ''' Creates a new TcpServer bound to all interfaces on the default port.
        ''' </summary>
        ''' <param name="peerManager">The peer manager for registering new connections.</param>
        ''' <param name="banManager">The ban manager for checking banned addresses.</param>
        Public Sub New(peerManager As PeerManager, banManager As BanManager)
            _peerManager = peerManager
            _banManager = banManager
            BindAddress = IPAddress.Any
            ListenPort = DefaultPort
            _isRunning = False
            _disposed = False
            TotalConnectionsAccepted = 0
            TotalConnectionsRejected = 0
        End Sub

        ''' <summary>
        ''' Creates a new TcpServer bound to the specified address and port.
        ''' </summary>
        ''' <param name="peerManager">The peer manager.</param>
        ''' <param name="banManager">The ban manager.</param>
        ''' <param name="bindAddress">The IP address to bind to.</param>
        ''' <param name="port">The port to listen on.</param>
        Public Sub New(peerManager As PeerManager, banManager As BanManager,
                       bindAddress As IPAddress, port As Integer)
            _peerManager = peerManager
            _banManager = banManager
            Me.BindAddress = bindAddress
            Me.ListenPort = port
            _isRunning = False
            _disposed = False
            TotalConnectionsAccepted = 0
            TotalConnectionsRejected = 0
        End Sub

        ''' <summary>
        ''' Starts the TCP server and begins accepting connections.
        ''' </summary>
        Public Sub Start()
            If _isRunning Then Return

            _listener = New TcpListener(BindAddress, ListenPort)
            _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, True)
            _listener.Start(ConnectionBacklog)
            _isRunning = True

            _listenThread = New Thread(AddressOf AcceptLoop)
            _listenThread.IsBackground = True
            _listenThread.Name = "CryptoCoin.TcpServer.AcceptLoop"
            _listenThread.Start()
        End Sub

        ''' <summary>
        ''' Stops the TCP server and closes the listening socket.
        ''' </summary>
        Public Sub [Stop]()
            _isRunning = False

            If _listener IsNot Nothing Then
                Try
                    _listener.Stop()
                Catch ex As SocketException
                    ' Expected during shutdown
                End Try
            End If

            If _listenThread IsNot Nothing AndAlso _listenThread.IsAlive Then
                _listenThread.Join(TimeSpan.FromSeconds(5))
            End If
        End Sub

        ''' <summary>
        ''' The main accept loop that runs on a background thread.
        ''' </summary>
        Private Sub AcceptLoop()
            While _isRunning
                Try
                    If Not _listener.Pending() Then
                        Thread.Sleep(100)
                        Continue While
                    End If

                    Dim client As TcpClient = _listener.AcceptTcpClient()
                    HandleNewConnection(client)

                Catch ex As SocketException When Not _isRunning
                    ' Server is shutting down, exit gracefully
                    Exit While
                Catch ex As ObjectDisposedException
                    ' Listener was disposed, exit
                    Exit While
                Catch ex As Exception
                    ' Log and continue accepting
                    System.Diagnostics.Debug.WriteLine($"TcpServer accept error: {ex.Message}")
                    Thread.Sleep(100)
                End Try
            End While
        End Sub

        ''' <summary>
        ''' Handles a newly accepted TCP connection.
        ''' </summary>
        ''' <param name="client">The accepted TcpClient.</param>
        Private Sub HandleNewConnection(client As TcpClient)
            Dim remoteEndPoint As IPEndPoint = DirectCast(client.Client.RemoteEndPoint, IPEndPoint)
            Dim remoteAddress As IPAddress = remoteEndPoint.Address
            Dim remotePort As Integer = remoteEndPoint.Port

            ' Check if the peer is banned
            If _banManager.IsBanned(remoteAddress) Then
                TotalConnectionsRejected += 1
                RaiseEvent ConnectionRejected(Me, New TcpConnectionEventArgs(remoteAddress, remotePort, "Banned"))
                client.Close()
                Return
            End If

            ' Create a new peer for this inbound connection
            Dim peer As New Peer(remoteAddress, remotePort)
            peer.IsInbound = True
            peer.State = PeerState.Connecting
            peer.ConnectedAt = DateTime.UtcNow

            ' Try to add to peer manager
            If Not _peerManager.TryAddPeer(peer) Then
                TotalConnectionsRejected += 1
                RaiseEvent ConnectionRejected(Me, New TcpConnectionEventArgs(remoteAddress, remotePort, "Connection limit reached"))
                client.Close()
                Return
            End If

            ' Configure the socket
            client.NoDelay = True
            client.ReceiveTimeout = 60000
            client.SendTimeout = 30000
            client.ReceiveBufferSize = 65536
            client.SendBufferSize = 65536

            TotalConnectionsAccepted += 1
            RaiseEvent ConnectionAccepted(Me, New TcpConnectionEventArgs(remoteAddress, remotePort, "Accepted"))
        End Sub

        Protected Overridable Sub Dispose(disposing As Boolean)
            If Not _disposed Then
                If disposing Then
                    [Stop]()
                    If _listener IsNot Nothing Then
                        _listener.Server.Dispose()
                    End If
                End If
                _disposed = True
            End If
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub

    End Class

    ''' <summary>
    ''' Event arguments for TCP connection events.
    ''' </summary>
    Public Class TcpConnectionEventArgs
        Inherits EventArgs

        ''' <summary>
        ''' The remote IP address.
        ''' </summary>
        Public ReadOnly Property Address As IPAddress

        ''' <summary>
        ''' The remote port.
        ''' </summary>
        Public ReadOnly Property Port As Integer

        ''' <summary>
        ''' A description of the event (e.g., "Accepted", "Banned", "Connection limit reached").
        ''' </summary>
        Public ReadOnly Property Reason As String

        ''' <summary>
        ''' Creates new TcpConnectionEventArgs.
        ''' </summary>
        Public Sub New(address As IPAddress, port As Integer, reason As String)
            Me.Address = address
            Me.Port = port
            Me.Reason = reason
        End Sub

    End Class

End Namespace
