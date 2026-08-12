Imports System.Net
Imports System.Net.Sockets
Imports System.Threading

Namespace CryptoCoin.Networking

    ''' <summary>
    ''' Handles peer discovery through DNS seeds, peer exchange, and address management.
    ''' Maintains a database of known peer addresses and provides candidates for
    ''' new outbound connections.
    ''' </summary>
    Public Class PeerDiscovery

        Private ReadOnly _knownAddresses As New Dictionary(Of String, PeerAddressEntry)()
        Private ReadOnly _dnsSeeds As New List(Of String)()
        Private ReadOnly _syncLock As New Object()
        Private _lastDnsQuery As DateTime
        Private _lastPeerExchange As DateTime

        ''' <summary>
        ''' Maximum number of addresses to store in the address database.
        ''' </summary>
        Public Const MaxStoredAddresses As Integer = 20480

        ''' <summary>
        ''' Minimum interval between DNS seed queries in minutes.
        ''' </summary>
        Public Const DnsQueryIntervalMinutes As Integer = 60

        ''' <summary>
        ''' Maximum age of an address before it is considered stale (in hours).
        ''' </summary>
        Public Const MaxAddressAgeHours As Integer = 72

        ''' <summary>
        ''' Number of addresses to return per GetCandidates call.
        ''' </summary>
        Public Const DefaultCandidateCount As Integer = 8

        ''' <summary>
        ''' Gets the number of known addresses in the database.
        ''' </summary>
        Public ReadOnly Property KnownAddressCount As Integer
            Get
                SyncLock _syncLock
                    Return _knownAddresses.Count
                End SyncLock
            End Get
        End Property

        ''' <summary>
        ''' Gets the configured DNS seed hostnames.
        ''' </summary>
        Public ReadOnly Property DnsSeeds As IReadOnlyList(Of String)
            Get
                Return _dnsSeeds.AsReadOnly()
            End Get
        End Property

        ''' <summary>
        ''' Creates a new PeerDiscovery instance with default DNS seeds.
        ''' </summary>
        Public Sub New()
            _lastDnsQuery = DateTime.MinValue
            _lastPeerExchange = DateTime.MinValue

            ' Default DNS seeds for the CryptoCoin network
            _dnsSeeds.Add("seed1.cryptocoin.example.com")
            _dnsSeeds.Add("seed2.cryptocoin.example.com")
            _dnsSeeds.Add("seed3.cryptocoin.example.com")
            _dnsSeeds.Add("seed4.cryptocoin.example.com")
        End Sub

        ''' <summary>
        ''' Creates a PeerDiscovery instance with custom DNS seeds.
        ''' </summary>
        ''' <param name="seeds">The DNS seed hostnames to use.</param>
        Public Sub New(seeds As IEnumerable(Of String))
            _lastDnsQuery = DateTime.MinValue
            _lastPeerExchange = DateTime.MinValue
            _dnsSeeds.AddRange(seeds)
        End Sub

        ''' <summary>
        ''' Adds a DNS seed hostname to the seed list.
        ''' </summary>
        ''' <param name="hostname">The DNS seed hostname.</param>
        Public Sub AddDnsSeed(hostname As String)
            If String.IsNullOrWhiteSpace(hostname) Then Return
            If Not _dnsSeeds.Contains(hostname) Then
                _dnsSeeds.Add(hostname)
            End If
        End Sub

        ''' <summary>
        ''' Queries all DNS seeds and adds discovered addresses to the database.
        ''' Respects the minimum query interval to avoid excessive DNS lookups.
        ''' </summary>
        ''' <returns>The number of new addresses discovered.</returns>
        Public Function QueryDnsSeeds() As Integer
            ' Rate limit DNS queries
            If (DateTime.UtcNow - _lastDnsQuery).TotalMinutes < DnsQueryIntervalMinutes Then
                Return 0
            End If

            _lastDnsQuery = DateTime.UtcNow
            Dim discovered As Integer = 0

            For Each seed As String In _dnsSeeds
                Try
                    Dim addresses As IPAddress() = Dns.GetHostAddresses(seed)
                    For Each addr As IPAddress In addresses
                        If AddAddress(addr, TcpServer.DefaultPort, PeerServices.FullNode) Then
                            discovered += 1
                        End If
                    Next
                Catch ex As SocketException
                    ' DNS resolution failed for this seed, continue with others
                    System.Diagnostics.Debug.WriteLine($"DNS seed query failed for {seed}: {ex.Message}")
                End Try
            Next

            Return discovered
        End Function

        ''' <summary>
        ''' Adds a peer address to the known address database.
        ''' </summary>
        ''' <param name="address">The IP address.</param>
        ''' <param name="port">The TCP port.</param>
        ''' <param name="services">The services advertised by the peer.</param>
        ''' <returns>True if the address was newly added.</returns>
        Public Function AddAddress(address As IPAddress, port As Integer,
                                   services As PeerServices) As Boolean
            If address Is Nothing Then Return False
            If IsLocalAddress(address) Then Return False

            Dim key As String = $"{address}:{port}"

            SyncLock _syncLock
                If _knownAddresses.ContainsKey(key) Then
                    ' Update existing entry
                    _knownAddresses(key).LastSeen = DateTime.UtcNow
                    _knownAddresses(key).Services = services
                    Return False
                End If

                ' Evict old entries if at capacity
                If _knownAddresses.Count >= MaxStoredAddresses Then
                    EvictStaleAddresses()
                End If

                If _knownAddresses.Count >= MaxStoredAddresses Then
                    Return False ' Still full after eviction
                End If

                Dim entry As New PeerAddressEntry()
                entry.Address = address
                entry.Port = port
                entry.Services = services
                entry.LastSeen = DateTime.UtcNow
                entry.FailedAttempts = 0
                entry.LastAttempt = DateTime.MinValue

                _knownAddresses.Add(key, entry)
            End SyncLock

            Return True
        End Function

        ''' <summary>
        ''' Processes addresses received from a peer's addr message.
        ''' </summary>
        ''' <param name="addresses">The network addresses from the message.</param>
        ''' <returns>The number of new addresses added.</returns>
        Public Function ProcessAddrMessage(addresses As List(Of NetworkAddress)) As Integer
            If addresses Is Nothing Then Return 0

            Dim added As Integer = 0
            For Each netAddr As NetworkAddress In addresses
                ' Skip addresses that are too old
                If netAddr.AgeSeconds > 86400 * 10 Then Continue For ' 10 days

                If AddAddress(netAddr.Address, netAddr.Port, netAddr.Services) Then
                    added += 1
                End If
            Next

            Return added
        End Function

        ''' <summary>
        ''' Gets candidate addresses for new outbound connections.
        ''' Prioritizes recently seen addresses with good connection history.
        ''' </summary>
        ''' <param name="count">The number of candidates to return.</param>
        ''' <returns>A list of candidate endpoints.</returns>
        Public Function GetCandidates(count As Integer) As List(Of IPEndPoint)
            Dim result As New List(Of IPEndPoint)()

            SyncLock _syncLock
                ' Sort by score (recently seen, few failures)
                Dim candidates = New List(Of PeerAddressEntry)(_knownAddresses.Values)
                candidates.Sort(Function(a, b) b.Score.CompareTo(a.Score))

                For Each entry As PeerAddressEntry In candidates
                    If result.Count >= count Then Exit For

                    ' Skip entries with too many failures
                    If entry.FailedAttempts > 5 Then Continue For

                    ' Skip entries attempted too recently
                    If entry.LastAttempt <> DateTime.MinValue Then
                        Dim elapsed As TimeSpan = DateTime.UtcNow - entry.LastAttempt
                        Dim backoffMinutes As Double = Math.Pow(2, entry.FailedAttempts)
                        If elapsed.TotalMinutes < backoffMinutes Then Continue For
                    End If

                    result.Add(New IPEndPoint(entry.Address, entry.Port))
                    entry.LastAttempt = DateTime.UtcNow
                Next
            End SyncLock

            Return result
        End Function

        ''' <summary>
        ''' Records a successful connection to an address.
        ''' </summary>
        ''' <param name="address">The IP address.</param>
        ''' <param name="port">The TCP port.</param>
        Public Sub RecordSuccess(address As IPAddress, port As Integer)
            Dim key As String = $"{address}:{port}"
            SyncLock _syncLock
                If _knownAddresses.ContainsKey(key) Then
                    _knownAddresses(key).FailedAttempts = 0
                    _knownAddresses(key).LastSeen = DateTime.UtcNow
                    _knownAddresses(key).SuccessfulConnections += 1
                End If
            End SyncLock
        End Sub

        ''' <summary>
        ''' Records a failed connection attempt to an address.
        ''' </summary>
        ''' <param name="address">The IP address.</param>
        ''' <param name="port">The TCP port.</param>
        Public Sub RecordFailure(address As IPAddress, port As Integer)
            Dim key As String = $"{address}:{port}"
            SyncLock _syncLock
                If _knownAddresses.ContainsKey(key) Then
                    _knownAddresses(key).FailedAttempts += 1
                End If
            End SyncLock
        End Sub

        ''' <summary>
        ''' Gets a random selection of known addresses for sharing with peers.
        ''' </summary>
        ''' <param name="count">Maximum number of addresses to return.</param>
        ''' <returns>A list of network addresses suitable for sharing.</returns>
        Public Function GetAddressesForRelay(count As Integer) As List(Of NetworkAddress)
            Dim result As New List(Of NetworkAddress)()
            Dim rng As New Random()

            SyncLock _syncLock
                Dim entries = New List(Of PeerAddressEntry)(_knownAddresses.Values)

                ' Shuffle using Fisher-Yates
                For i As Integer = entries.Count - 1 To 1 Step -1
                    Dim j As Integer = rng.Next(i + 1)
                    Dim temp As PeerAddressEntry = entries(i)
                    entries(i) = entries(j)
                    entries(j) = temp
                Next

                For Each entry As PeerAddressEntry In entries
                    If result.Count >= count Then Exit For
                    ' Only relay fresh addresses
                    Dim age As TimeSpan = DateTime.UtcNow - entry.LastSeen
                    If age.TotalHours <= MaxAddressAgeHours Then
                        result.Add(New NetworkAddress(entry.Address, entry.Port, entry.Services))
                    End If
                Next
            End SyncLock

            Return result
        End Function

        ''' <summary>
        ''' Removes stale addresses that haven't been seen recently.
        ''' </summary>
        Private Sub EvictStaleAddresses()
            Dim toRemove As New List(Of String)()
            Dim now As DateTime = DateTime.UtcNow

            For Each kvp As KeyValuePair(Of String, PeerAddressEntry) In _knownAddresses
                Dim age As TimeSpan = now - kvp.Value.LastSeen
                If age.TotalHours > MaxAddressAgeHours Then
                    toRemove.Add(kvp.Key)
                End If
            Next

            For Each key As String In toRemove
                _knownAddresses.Remove(key)
            Next
        End Sub

        ''' <summary>
        ''' Checks if an address is a local/private address that should not be stored.
        ''' </summary>
        Private Shared Function IsLocalAddress(address As IPAddress) As Boolean
            If IPAddress.IsLoopback(address) Then Return True
            Dim bytes As Byte() = address.GetAddressBytes()
            If bytes.Length = 4 Then
                ' 10.x.x.x
                If bytes(0) = 10 Then Return True
                ' 172.16.x.x - 172.31.x.x
                If bytes(0) = 172 AndAlso bytes(1) >= 16 AndAlso bytes(1) <= 31 Then Return True
                ' 192.168.x.x
                If bytes(0) = 192 AndAlso bytes(1) = 168 Then Return True
            End If
            Return False
        End Function

    End Class

    ''' <summary>
    ''' Represents a stored peer address entry with connection history.
    ''' </summary>
    Public Class PeerAddressEntry

        ''' <summary>The IP address of the peer.</summary>
        Public Property Address As IPAddress

        ''' <summary>The TCP port of the peer.</summary>
        Public Property Port As Integer

        ''' <summary>The services advertised by the peer.</summary>
        Public Property Services As PeerServices

        ''' <summary>When this address was last seen active.</summary>
        Public Property LastSeen As DateTime

        ''' <summary>When the last connection attempt was made.</summary>
        Public Property LastAttempt As DateTime

        ''' <summary>Number of consecutive failed connection attempts.</summary>
        Public Property FailedAttempts As Integer

        ''' <summary>Total number of successful connections to this address.</summary>
        Public Property SuccessfulConnections As Integer

        ''' <summary>
        ''' Computed score for prioritizing connection candidates.
        ''' Higher is better.
        ''' </summary>
        Public ReadOnly Property Score As Double
            Get
                Dim agePenalty As Double = (DateTime.UtcNow - LastSeen).TotalHours
                Dim failurePenalty As Double = FailedAttempts * 10.0
                Dim successBonus As Double = SuccessfulConnections * 5.0
                Return successBonus - agePenalty - failurePenalty
            End Get
        End Property

    End Class

End Namespace
