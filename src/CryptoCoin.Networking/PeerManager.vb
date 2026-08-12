Imports System.Net
Imports System.Threading

Namespace CryptoCoin.Networking

    ''' <summary>
    ''' Manages the collection of connected peers in the CryptoCoin P2P network.
    ''' Handles peer discovery, connection limits, scoring, and ban list enforcement.
    ''' </summary>
    Public Class PeerManager
        Implements IDisposable

        Private ReadOnly _peers As New Dictionary(Of Guid, Peer)()
        Private ReadOnly _syncLock As New Object()
        Private ReadOnly _banManager As BanManager
        Private ReadOnly _peerDiscovery As PeerDiscovery
        Private _maintenanceTimer As Timer
        Private _disposed As Boolean

        ''' <summary>
        ''' Maximum number of simultaneous peer connections allowed.
        ''' </summary>
        Public Const MaxPeers As Integer = 125

        ''' <summary>
        ''' Maximum number of inbound connections allowed.
        ''' </summary>
        Public Const MaxInboundPeers As Integer = 117

        ''' <summary>
        ''' Maximum number of outbound connections allowed.
        ''' </summary>
        Public Const MaxOutboundPeers As Integer = 8

        ''' <summary>
        ''' Interval between peer maintenance cycles in milliseconds.
        ''' </summary>
        Public Const MaintenanceIntervalMs As Integer = 30000

        ''' <summary>
        ''' Timeout for peers that have not been seen (in minutes).
        ''' </summary>
        Public Const PeerTimeoutMinutes As Integer = 90

        ''' <summary>
        ''' The misbehavior score threshold at which a peer is banned.
        ''' </summary>
        Public Const BanThreshold As Integer = 100

        ''' <summary>
        ''' Gets the current number of connected peers.
        ''' </summary>
        Public ReadOnly Property ConnectedPeerCount As Integer
            Get
                SyncLock _syncLock
                    Return _peers.Count
                End SyncLock
            End Get
        End Property

        ''' <summary>
        ''' Gets the number of inbound connections.
        ''' </summary>
        Public ReadOnly Property InboundCount As Integer
            Get
                SyncLock _syncLock
                    Dim count As Integer = 0
                    For Each kvp As Object In _peers
                        If kvp.Value.IsInbound Then count += 1
                    Next
                    Return count
                End SyncLock
            End Get
        End Property

        ''' <summary>
        ''' Gets the number of outbound connections.
        ''' </summary>
        Public ReadOnly Property OutboundCount As Integer
            Get
                SyncLock _syncLock
                    Dim count As Integer = 0
                    For Each kvp As Object In _peers
                        If Not kvp.Value.IsInbound Then count += 1
                    Next
                    Return count
                End SyncLock
            End Get
        End Property

        ''' <summary>
        ''' Gets the associated ban manager.
        ''' </summary>
        Public ReadOnly Property Bans As BanManager
            Get
                Return _banManager
            End Get
        End Property

        ''' <summary>
        ''' Creates a new PeerManager with the specified ban manager and peer discovery service.
        ''' </summary>
        ''' <param name="banManager">The ban manager for tracking misbehaving peers.</param>
        ''' <param name="peerDiscovery">The peer discovery service for finding new peers.</param>
        Public Sub New(banManager As BanManager, peerDiscovery As PeerDiscovery)
            _banManager = banManager
            _peerDiscovery = peerDiscovery
            _disposed = False
        End Sub

        ''' <summary>
        ''' Starts the peer manager maintenance loop.
        ''' </summary>
        Public Sub Start()
            _maintenanceTimer = New Timer(
                AddressOf PerformMaintenance,
                Nothing,
                MaintenanceIntervalMs,
                MaintenanceIntervalMs)
        End Sub

        ''' <summary>
        ''' Stops the peer manager and disconnects all peers.
        ''' </summary>
        Public Sub [Stop]()
            If _maintenanceTimer IsNot Nothing Then
                _maintenanceTimer.Dispose()
                _maintenanceTimer = Nothing
            End If
            DisconnectAll()
        End Sub

        ''' <summary>
        ''' Attempts to add a new peer connection. Returns False if connection limits
        ''' are reached or the peer is banned.
        ''' </summary>
        ''' <param name="peer">The peer to add.</param>
        ''' <returns>True if the peer was added successfully.</returns>
        Public Function TryAddPeer(peer As Peer) As Boolean
            If peer Is Nothing Then Return False

            ' Check if the peer is banned
            If _banManager.IsBanned(peer.Address) Then
                Return False
            End If

            SyncLock _syncLock
                ' Check total connection limit
                If _peers.Count >= MaxPeers Then
                    Return False
                End If

                ' Check directional limits
                If peer.IsInbound Then
                    Dim inbound As Integer = 0
                    For Each kvp As Object In _peers
                        If kvp.Value.IsInbound Then inbound += 1
                    Next
                    If inbound >= MaxInboundPeers Then Return False
                Else
                    Dim outbound As Integer = 0
                    For Each kvp As Object In _peers
                        If Not kvp.Value.IsInbound Then outbound += 1
                    Next
                    If outbound >= MaxOutboundPeers Then Return False
                End If

                ' Check for duplicate connections
                For Each kvp As Object In _peers
                    If kvp.Value.Address.Equals(peer.Address) AndAlso kvp.Value.Port = peer.Port Then
                        Return False
                    End If
                Next

                _peers.Add(peer.Id, peer)
            End SyncLock

            Return True
        End Function

        ''' <summary>
        ''' Removes a peer from the connected peers list.
        ''' </summary>
        ''' <param name="peerId">The unique identifier of the peer to remove.</param>
        ''' <returns>True if the peer was found and removed.</returns>
        Public Function RemovePeer(peerId As Guid) As Boolean
            SyncLock _syncLock
                Return _peers.Remove(peerId)
            End SyncLock
        End Function

        ''' <summary>
        ''' Gets a peer by its unique identifier.
        ''' </summary>
        ''' <param name="peerId">The peer's unique identifier.</param>
        ''' <returns>The peer, or Nothing if not found.</returns>
        Public Function GetPeer(peerId As Guid) As Peer
            SyncLock _syncLock
                Dim peer As Peer = Nothing
                _peers.TryGetValue(peerId, peer)
                Return peer
            End SyncLock
        End Function

        ''' <summary>
        ''' Gets a snapshot of all currently connected peers.
        ''' </summary>
        ''' <returns>A list of all connected peers.</returns>
        Public Function GetAllPeers() As List(Of Peer)
            SyncLock _syncLock
                Return New List(Of Peer)(_peers.Values)
            End SyncLock
        End Function

        ''' <summary>
        ''' Gets peers that have completed the version handshake.
        ''' </summary>
        ''' <returns>A list of fully connected peers.</returns>
        Public Function GetHandshakedPeers() As List(Of Peer)
            SyncLock _syncLock
                Dim result As New List(Of Peer)()
                For Each kvp As Object In _peers
                    If kvp.Value.IsHandshakeComplete Then
                        result.Add(kvp.Value)
                    End If
                Next
                Return result
            End SyncLock
        End Function

        ''' <summary>
        ''' Reports misbehavior for a peer. If the score exceeds the threshold,
        ''' the peer is banned and disconnected.
        ''' </summary>
        ''' <param name="peerId">The peer's unique identifier.</param>
        ''' <param name="score">The misbehavior score to add.</param>
        ''' <param name="reason">The reason for the misbehavior report.</param>
        Public Sub ReportMisbehavior(peerId As Guid, score As Integer, reason As String)
            Dim peer As Peer = GetPeer(peerId)
            If peer Is Nothing Then Return

            Dim shouldBan As Boolean = peer.AddMisbehavior(score, BanThreshold)
            If shouldBan Then
                _banManager.BanPeer(peer.Address, reason, TimeSpan.FromHours(24))
                RemovePeer(peerId)
            End If
        End Sub

        ''' <summary>
        ''' Selects the best peer for block download based on height and latency.
        ''' </summary>
        ''' <returns>The best peer for syncing, or Nothing if no suitable peer exists.</returns>
        Public Function SelectBestSyncPeer() As Peer
            SyncLock _syncLock
                Dim bestPeer As Peer = Nothing
                Dim bestScore As Double = Double.MinValue

                For Each kvp As Object In _peers
                    Dim p As Peer = kvp.Value
                    If Not p.IsHandshakeComplete Then Continue For
                    If Not p.SupportsService(PeerServices.FullNode) Then Continue For

                    ' Score based on height (higher is better) and latency (lower is better)
                    Dim heightScore As Double = CDbl(p.StartHeight)
                    Dim latencyPenalty As Double = p.AverageLatencyMs / 1000.0
                    Dim score As Double = heightScore - latencyPenalty

                    If score > bestScore Then
                        bestScore = score
                        bestPeer = p
                    End If
                Next

                Return bestPeer
            End SyncLock
        End Function

        ''' <summary>
        ''' Disconnects all peers gracefully.
        ''' </summary>
        Public Sub DisconnectAll()
            SyncLock _syncLock
                For Each kvp As Object In _peers
                    kvp.Value.State = PeerState.Disconnected
                Next
                _peers.Clear()
            End SyncLock
        End Sub

        ''' <summary>
        ''' Performs periodic maintenance: evicts stale peers, attempts new connections.
        ''' </summary>
        Private Sub PerformMaintenance(state As Object)
            EvictStalePeers()
            AttemptNewConnections()
        End Sub

        ''' <summary>
        ''' Evicts peers that have not been seen within the timeout period.
        ''' </summary>
        Private Sub EvictStalePeers()
            Dim now As DateTime = DateTime.UtcNow
            Dim toRemove As New List(Of Guid)()

            SyncLock _syncLock
                For Each kvp As Object In _peers
                    Dim peer As Peer = kvp.Value
                    If peer.LastSeenAt <> DateTime.MinValue Then
                        Dim elapsed As TimeSpan = now - peer.LastSeenAt
                        If elapsed.TotalMinutes > PeerTimeoutMinutes Then
                            toRemove.Add(kvp.Key)
                        End If
                    End If
                Next

                For Each id As Object In toRemove
                    _peers(id).State = PeerState.Disconnected
                    _peers.Remove(id)
                Next
            End SyncLock
        End Sub

        ''' <summary>
        ''' Attempts to establish new outbound connections if below the target.
        ''' </summary>
        Private Sub AttemptNewConnections()
            If OutboundCount >= MaxOutboundPeers Then Return

            Dim needed As Integer = MaxOutboundPeers - OutboundCount
            Dim candidates As List(Of IPEndPoint) = _peerDiscovery.GetCandidates(needed)

            For Each endpoint As Object In candidates
                If _banManager.IsBanned(endpoint.Address) Then Continue For

                Dim peer As New Peer(endpoint.Address, endpoint.Port)
                peer.IsInbound = False
                peer.State = PeerState.Connecting
                peer.ConnectedAt = DateTime.UtcNow
                TryAddPeer(peer)
            Next
        End Sub

        ''' <summary>
        ''' Broadcasts a message to all connected and handshaked peers.
        ''' </summary>
        ''' <param name="message">The network message to broadcast.</param>
        Public Sub BroadcastMessage(message As NetworkMessage)
            Dim peers As List(Of Peer) = GetHandshakedPeers()
            For Each peer As Object In peers
                peer.LastSentAt = DateTime.UtcNow
                peer.BytesSent += message.PayloadLength
            Next
        End Sub

        Protected Overridable Sub Dispose(disposing As Boolean)
            If Not _disposed Then
                If disposing Then
                    [Stop]()
                End If
                _disposed = True
            End If
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub

    End Class

End Namespace
