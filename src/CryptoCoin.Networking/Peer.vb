Imports System.Net

Namespace CryptoCoin.Networking

    ''' <summary>
    ''' Represents the services a peer advertises supporting.
    ''' </summary>
    <Flags>
    Public Enum PeerServices As ULong
        ''' <summary>No services advertised.</summary>
        None = 0UL
        ''' <summary>Full node that can serve complete blocks.</summary>
        FullNode = 1UL
        ''' <summary>Node supports bloom filtering for SPV clients.</summary>
        BloomFilter = 2UL
        ''' <summary>Node supports compact block relay.</summary>
        CompactBlocks = 4UL
        ''' <summary>Node serves block headers for light clients.</summary>
        HeadersOnly = 8UL
    End Enum

    ''' <summary>
    ''' Represents the current connection state of a peer.
    ''' </summary>
    Public Enum PeerState
        ''' <summary>Peer is disconnected.</summary>
        Disconnected
        ''' <summary>TCP connection established, awaiting version handshake.</summary>
        Connecting
        ''' <summary>Version handshake in progress.</summary>
        Handshaking
        ''' <summary>Peer is fully connected and operational.</summary>
        Connected
        ''' <summary>Peer is being disconnected gracefully.</summary>
        Disconnecting
    End Enum

    ''' <summary>
    ''' Represents a connected peer in the CryptoCoin P2P network.
    ''' Tracks connection metadata, protocol version, services, and latency.
    ''' </summary>
    Public Class Peer

        Private ReadOnly _latencySamples As New List(Of Long)()
        Private Const MaxLatencySamples As Integer = 20
        Private _lastPingNonce As ULong
        Private _lastPingSentTime As DateTime

        ''' <summary>
        ''' Unique identifier for this peer connection.
        ''' </summary>
        Public Property Id As Guid

        ''' <summary>
        ''' The IP address of the remote peer.
        ''' </summary>
        Public Property Address As IPAddress

        ''' <summary>
        ''' The TCP port of the remote peer.
        ''' </summary>
        Public Property Port As Integer

        ''' <summary>
        ''' The protocol version reported by the peer during handshake.
        ''' </summary>
        Public Property ProtocolVersion As Integer

        ''' <summary>
        ''' The services bitmap advertised by the peer.
        ''' </summary>
        Public Property Services As PeerServices

        ''' <summary>
        ''' The user agent string reported by the peer (e.g., "/CryptoCoin:1.0.0/").
        ''' </summary>
        Public Property UserAgent As String

        ''' <summary>
        ''' The best block height reported by the peer during handshake.
        ''' </summary>
        Public Property StartHeight As Integer

        ''' <summary>
        ''' Whether this is an inbound connection (peer connected to us).
        ''' </summary>
        Public Property IsInbound As Boolean

        ''' <summary>
        ''' The current connection state of this peer.
        ''' </summary>
        Public Property State As PeerState

        ''' <summary>
        ''' Timestamp when the connection was established.
        ''' </summary>
        Public Property ConnectedAt As DateTime

        ''' <summary>
        ''' Timestamp when we last received any message from this peer.
        ''' </summary>
        Public Property LastSeenAt As DateTime

        ''' <summary>
        ''' Timestamp when we last sent any message to this peer.
        ''' </summary>
        Public Property LastSentAt As DateTime

        ''' <summary>
        ''' The misbehavior score for this peer (used for banning decisions).
        ''' </summary>
        Public Property MisbehaviorScore As Integer

        ''' <summary>
        ''' Number of bytes received from this peer.
        ''' </summary>
        Public Property BytesReceived As Long

        ''' <summary>
        ''' Number of bytes sent to this peer.
        ''' </summary>
        Public Property BytesSent As Long

        ''' <summary>
        ''' Whether the peer has completed the version handshake.
        ''' </summary>
        Public ReadOnly Property IsHandshakeComplete As Boolean
            Get
                Return State = PeerState.Connected
            End Get
        End Property

        ''' <summary>
        ''' The average latency to this peer in milliseconds.
        ''' </summary>
        Public ReadOnly Property AverageLatencyMs As Double
            Get
                If _latencySamples.Count = 0 Then Return 0.0
                Dim total As Long = 0
                For Each sample As Object In _latencySamples
                    total += sample
                Next
                Return CDbl(total) / _latencySamples.Count
            End Get
        End Property

        ''' <summary>
        ''' The endpoint string representation (address:port).
        ''' </summary>
        Public ReadOnly Property EndPointString As String
            Get
                Return $"{Address}:{Port}"
            End Get
        End Property

        ''' <summary>
        ''' Duration of the connection.
        ''' </summary>
        Public ReadOnly Property ConnectionDuration As TimeSpan
            Get
                If State = PeerState.Disconnected Then Return TimeSpan.Zero
                Return DateTime.UtcNow - ConnectedAt
            End Get
        End Property

        ''' <summary>
        ''' Creates a new Peer instance with default values.
        ''' </summary>
        Public Sub New()
            Id = Guid.NewGuid()
            Address = IPAddress.None
            Port = 8333
            ProtocolVersion = 0
            Services = PeerServices.None
            UserAgent = String.Empty
            StartHeight = 0
            IsInbound = False
            State = PeerState.Disconnected
            ConnectedAt = DateTime.MinValue
            LastSeenAt = DateTime.MinValue
            LastSentAt = DateTime.MinValue
            MisbehaviorScore = 0
            BytesReceived = 0
            BytesSent = 0
        End Sub

        ''' <summary>
        ''' Creates a new Peer instance with the specified address and port.
        ''' </summary>
        ''' <param name="address">The IP address of the peer.</param>
        ''' <param name="port">The TCP port of the peer.</param>
        Public Sub New(address As IPAddress, port As Integer)
            Me.New()
            Me.Address = address
            Me.Port = port
        End Sub

        ''' <summary>
        ''' Records a latency sample from a ping/pong exchange.
        ''' </summary>
        ''' <param name="latencyMs">The measured latency in milliseconds.</param>
        Public Sub RecordLatency(latencyMs As Long)
            If latencyMs < 0 Then Return
            _latencySamples.Add(latencyMs)
            If _latencySamples.Count > MaxLatencySamples Then
                _latencySamples.RemoveAt(0)
            End If
        End Sub

        ''' <summary>
        ''' Generates a ping nonce and records the send time for latency measurement.
        ''' </summary>
        ''' <returns>The nonce to include in the ping message.</returns>
        Public Function GeneratePingNonce() As ULong
            Dim rng As New Random()
            Dim buffer(7) As Byte
            rng.NextBytes(buffer)
            _lastPingNonce = BitConverter.ToUInt64(buffer, 0)
            _lastPingSentTime = DateTime.UtcNow
            Return _lastPingNonce
        End Function

        ''' <summary>
        ''' Processes a pong response and records the latency if the nonce matches.
        ''' </summary>
        ''' <param name="nonce">The nonce from the pong message.</param>
        ''' <returns>True if the nonce matched and latency was recorded.</returns>
        Public Function ProcessPong(nonce As ULong) As Boolean
            If nonce <> _lastPingNonce Then Return False
            Dim elapsed As TimeSpan = DateTime.UtcNow - _lastPingSentTime
            RecordLatency(CLng(elapsed.TotalMilliseconds))
            Return True
        End Function

        ''' <summary>
        ''' Marks the peer as having completed the version handshake.
        ''' </summary>
        Public Sub CompleteHandshake()
            State = PeerState.Connected
            LastSeenAt = DateTime.UtcNow
        End Sub

        ''' <summary>
        ''' Adds to the misbehavior score. Returns True if the peer should be banned.
        ''' </summary>
        ''' <param name="score">The score to add.</param>
        ''' <param name="banThreshold">The threshold at which the peer is banned.</param>
        ''' <returns>True if the accumulated score exceeds the ban threshold.</returns>
        Public Function AddMisbehavior(score As Integer, banThreshold As Integer) As Boolean
            MisbehaviorScore += score
            Return MisbehaviorScore >= banThreshold
        End Function

        ''' <summary>
        ''' Updates the last seen timestamp.
        ''' </summary>
        Public Sub MarkSeen()
            LastSeenAt = DateTime.UtcNow
        End Sub

        ''' <summary>
        ''' Checks whether the peer supports a given service.
        ''' </summary>
        ''' <param name="service">The service flag to check.</param>
        ''' <returns>True if the peer advertises the service.</returns>
        Public Function SupportsService(service As PeerServices) As Boolean
            Return (Services And service) = service
        End Function

        Public Overrides Function ToString() As String
            Return $"Peer({EndPointString}, Version={ProtocolVersion}, Agent={UserAgent}, State={State})"
        End Function

    End Class

End Namespace
