Imports System.Net
Imports System.Threading

Namespace CryptoCoin.Networking

    ''' <summary>
    ''' Represents a ban entry for a misbehaving peer.
    ''' </summary>
    Public Class BanEntry

        ''' <summary>
        ''' The banned IP address.
        ''' </summary>
        Public Property Address As IPAddress

        ''' <summary>
        ''' The reason for the ban.
        ''' </summary>
        Public Property Reason As String

        ''' <summary>
        ''' When the ban was created.
        ''' </summary>
        Public Property BannedAt As DateTime

        ''' <summary>
        ''' When the ban expires.
        ''' </summary>
        Public Property ExpiresAt As DateTime

        ''' <summary>
        ''' The accumulated misbehavior score that triggered the ban.
        ''' </summary>
        Public Property Score As Integer

        ''' <summary>
        ''' Number of times this address has been banned.
        ''' </summary>
        Public Property BanCount As Integer

        ''' <summary>
        ''' Whether this ban has expired.
        ''' </summary>
        Public ReadOnly Property IsExpired As Boolean
            Get
                Return DateTime.UtcNow >= ExpiresAt
            End Get
        End Property

        ''' <summary>
        ''' The remaining duration of the ban.
        ''' </summary>
        Public ReadOnly Property RemainingDuration As TimeSpan
            Get
                If IsExpired Then Return TimeSpan.Zero
                Return ExpiresAt - DateTime.UtcNow
            End Get
        End Property

        Public Sub New()
            Address = IPAddress.None
            Reason = String.Empty
            BannedAt = DateTime.UtcNow
            ExpiresAt = DateTime.UtcNow
            Score = 0
            BanCount = 1
        End Sub

        Public Overrides Function ToString() As String
            Return $"BanEntry({Address}, Reason={Reason}, Expires={ExpiresAt:u})"
        End Function

    End Class

    ''' <summary>
    ''' Tracks misbehaving peers, manages ban scores, and enforces automatic banning.
    ''' Peers accumulate misbehavior scores for protocol violations, and are banned
    ''' when their score exceeds the threshold.
    ''' </summary>
    Public Class BanManager
        Implements IDisposable

        Private ReadOnly _banList As New Dictionary(Of String, BanEntry)()
        Private ReadOnly _misbehaviorScores As New Dictionary(Of String, Integer)()
        Private ReadOnly _syncLock As New Object()
        Private _cleanupTimer As Timer
        Private _disposed As Boolean

        ''' <summary>
        ''' Default ban duration in hours.
        ''' </summary>
        Public Const DefaultBanDurationHours As Integer = 24

        ''' <summary>
        ''' Default misbehavior score threshold for automatic banning.
        ''' </summary>
        Public Const DefaultBanThreshold As Integer = 100

        ''' <summary>
        ''' Interval between ban list cleanup cycles in milliseconds.
        ''' </summary>
        Public Const CleanupIntervalMs As Integer = 600000 ' 10 minutes

        ''' <summary>
        ''' Score penalty for sending an invalid block.
        ''' </summary>
        Public Const ScoreInvalidBlock As Integer = 100

        ''' <summary>
        ''' Score penalty for sending an invalid transaction.
        ''' </summary>
        Public Const ScoreInvalidTransaction As Integer = 10

        ''' <summary>
        ''' Score penalty for sending too many messages (flooding).
        ''' </summary>
        Public Const ScoreFlooding As Integer = 50

        ''' <summary>
        ''' Score penalty for protocol violations.
        ''' </summary>
        Public Const ScoreProtocolViolation As Integer = 20

        ''' <summary>
        ''' Score penalty for sending unrequested data.
        ''' </summary>
        Public Const ScoreUnrequestedData As Integer = 20

        ''' <summary>
        ''' The misbehavior threshold for banning.
        ''' </summary>
        Public Property BanThreshold As Integer

        ''' <summary>
        ''' Gets the number of currently banned addresses.
        ''' </summary>
        Public ReadOnly Property BannedCount As Integer
            Get
                SyncLock _syncLock
                    Return _banList.Count
                End SyncLock
            End Get
        End Property

        ''' <summary>
        ''' Creates a new BanManager with default settings.
        ''' </summary>
        Public Sub New()
            BanThreshold = DefaultBanThreshold
            _disposed = False
            StartCleanupTimer()
        End Sub

        ''' <summary>
        ''' Creates a new BanManager with a custom ban threshold.
        ''' </summary>
        ''' <param name="banThreshold">The misbehavior score threshold for banning.</param>
        Public Sub New(banThreshold As Integer)
            Me.BanThreshold = banThreshold
            _disposed = False
            StartCleanupTimer()
        End Sub

        ''' <summary>
        ''' Checks whether an IP address is currently banned.
        ''' </summary>
        ''' <param name="address">The IP address to check.</param>
        ''' <returns>True if the address is banned and the ban has not expired.</returns>
        Public Function IsBanned(address As IPAddress) As Boolean
            If address Is Nothing Then Return False
            Dim key As String = address.ToString()

            SyncLock _syncLock
                If Not _banList.ContainsKey(key) Then Return False
                Dim entry As BanEntry = _banList(key)
                If entry.IsExpired Then
                    _banList.Remove(key)
                    Return False
                End If
                Return True
            End SyncLock
        End Function

        ''' <summary>
        ''' Bans a peer's IP address for the specified duration.
        ''' </summary>
        ''' <param name="address">The IP address to ban.</param>
        ''' <param name="reason">The reason for the ban.</param>
        ''' <param name="duration">The duration of the ban.</param>
        Public Sub BanPeer(address As IPAddress, reason As String, duration As TimeSpan)
            If address Is Nothing Then Return
            Dim key As String = address.ToString()

            SyncLock _syncLock
                If _banList.ContainsKey(key) Then
                    ' Extend existing ban
                    Dim existing As BanEntry = _banList(key)
                    existing.ExpiresAt = DateTime.UtcNow.Add(duration)
                    existing.Reason = reason
                    existing.BanCount += 1
                Else
                    Dim entry As New BanEntry()
                    entry.Address = address
                    entry.Reason = reason
                    entry.BannedAt = DateTime.UtcNow
                    entry.ExpiresAt = DateTime.UtcNow.Add(duration)
                    _banList.Add(key, entry)
                End If

                ' Clear misbehavior score
                If _misbehaviorScores.ContainsKey(key) Then
                    _misbehaviorScores.Remove(key)
                End If
            End SyncLock
        End Sub

        ''' <summary>
        ''' Bans a peer with the default ban duration.
        ''' </summary>
        ''' <param name="address">The IP address to ban.</param>
        ''' <param name="reason">The reason for the ban.</param>
        Public Sub BanPeer(address As IPAddress, reason As String)
            BanPeer(address, reason, TimeSpan.FromHours(DefaultBanDurationHours))
        End Sub

        ''' <summary>
        ''' Adds misbehavior score for a peer. If the accumulated score exceeds
        ''' the threshold, the peer is automatically banned.
        ''' </summary>
        ''' <param name="address">The peer's IP address.</param>
        ''' <param name="score">The misbehavior score to add.</param>
        ''' <param name="reason">The reason for the misbehavior.</param>
        ''' <returns>True if the peer was banned as a result.</returns>
        Public Function AddMisbehavior(address As IPAddress, score As Integer, reason As String) As Boolean
            If address Is Nothing OrElse score <= 0 Then Return False
            Dim key As String = address.ToString()

            SyncLock _syncLock
                Dim currentScore As Integer = 0
                _misbehaviorScores.TryGetValue(key, currentScore)
                currentScore += score
                _misbehaviorScores(key) = currentScore

                If currentScore >= BanThreshold Then
                    ' Auto-ban with escalating duration based on ban count
                    Dim existingBanCount As Integer = 0
                    If _banList.ContainsKey(key) Then
                        existingBanCount = _banList(key).BanCount
                    End If

                    Dim durationHours As Integer = DefaultBanDurationHours * CInt(Math.Pow(2, existingBanCount))
                    BanPeer(address, reason, TimeSpan.FromHours(durationHours))
                    Return True
                End If
            End SyncLock

            Return False
        End Function

        ''' <summary>
        ''' Gets the current misbehavior score for an address.
        ''' </summary>
        ''' <param name="address">The IP address to query.</param>
        ''' <returns>The current misbehavior score.</returns>
        Public Function GetMisbehaviorScore(address As IPAddress) As Integer
            If address Is Nothing Then Return 0
            Dim key As String = address.ToString()

            SyncLock _syncLock
                Dim score As Integer = 0
                _misbehaviorScores.TryGetValue(key, score)
                Return score
            End SyncLock
        End Function

        ''' <summary>
        ''' Manually unbans an IP address.
        ''' </summary>
        ''' <param name="address">The IP address to unban.</param>
        ''' <returns>True if the address was found and unbanned.</returns>
        Public Function UnbanPeer(address As IPAddress) As Boolean
            If address Is Nothing Then Return False
            Dim key As String = address.ToString()

            SyncLock _syncLock
                _misbehaviorScores.Remove(key)
                Return _banList.Remove(key)
            End SyncLock
        End Function

        ''' <summary>
        ''' Gets all currently active ban entries.
        ''' </summary>
        ''' <returns>A list of active ban entries.</returns>
        Public Function GetBanList() As List(Of BanEntry)
            SyncLock _syncLock
                Dim result As New List(Of BanEntry)()
                For Each kvp As Object In _banList
                    If Not kvp.Value.IsExpired Then
                        result.Add(kvp.Value)
                    End If
                Next
                Return result
            End SyncLock
        End Function

        ''' <summary>
        ''' Clears all bans and misbehavior scores.
        ''' </summary>
        Public Sub ClearAll()
            SyncLock _syncLock
                _banList.Clear()
                _misbehaviorScores.Clear()
            End SyncLock
        End Sub

        ''' <summary>
        ''' Removes expired ban entries from the ban list.
        ''' </summary>
        Private Sub CleanupExpiredBans(state As Object)
            SyncLock _syncLock
                Dim toRemove As New List(Of String)()
                For Each kvp As Object In _banList
                    If kvp.Value.IsExpired Then
                        toRemove.Add(kvp.Key)
                    End If
                Next
                For Each key As Object In toRemove
                    _banList.Remove(key)
                Next
            End SyncLock
        End Sub

        ''' <summary>
        ''' Starts the periodic cleanup timer.
        ''' </summary>
        Private Sub StartCleanupTimer()
            _cleanupTimer = New Timer(
                AddressOf CleanupExpiredBans,
                Nothing,
                CleanupIntervalMs,
                CleanupIntervalMs)
        End Sub

        Protected Overridable Sub Dispose(disposing As Boolean)
            If Not _disposed Then
                If disposing Then
                    If _cleanupTimer IsNot Nothing Then
                        _cleanupTimer.Dispose()
                        _cleanupTimer = Nothing
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

End Namespace
