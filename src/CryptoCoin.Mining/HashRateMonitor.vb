Imports System.Threading
Imports CryptoCoin.Core

Namespace CryptoCoin.Mining

    ''' <summary>
    ''' Monitors and reports mining hash rate statistics.
    ''' </summary>
    Public Class HashRateMonitor

        Private _totalHashes As Long
        Private _windowHashes As Long
        Private _windowStart As DateTimeOffset
        Private _currentHashRate As Double
        Private ReadOnly _syncLock As New Object()
        Private Const WindowSeconds As Double = 10.0

        ''' <summary>
        ''' Gets the current hash rate in hashes per second.
        ''' </summary>
        Public ReadOnly Property CurrentHashRate As Double
            Get
                SyncLock _syncLock
                    Return _currentHashRate
                End SyncLock
            End Get
        End Property

        ''' <summary>
        ''' Gets the total number of hashes computed since start.
        ''' </summary>
        Public ReadOnly Property TotalHashes As Long
            Get
                Return Interlocked.Read(_totalHashes)
            End Get
        End Property

        ''' <summary>
        ''' Gets the formatted hash rate string (e.g., "1.5 MH/s").
        ''' </summary>
        Public ReadOnly Property FormattedHashRate As String
            Get
                Dim rate As Double = CurrentHashRate
                If rate >= 1000000000 Then
                    Return $"{rate / 1000000000:F2} GH/s"
                ElseIf rate >= 1000000 Then
                    Return $"{rate / 1000000:F2} MH/s"
                ElseIf rate >= 1000 Then
                    Return $"{rate / 1000:F2} KH/s"
                Else
                    Return $"{rate:F0} H/s"
                End If
            End Get
        End Property

        Public Sub New()
            Reset()
        End Sub

        ''' <summary>
        ''' Records hashes computed by a mining thread.
        ''' </summary>
        Public Sub AddHashes(count As Long)
            Interlocked.Add(_totalHashes, count)

            SyncLock _syncLock
                _windowHashes += count

                Dim elapsed As Double = (DateTimeOffset.UtcNow - _windowStart).TotalSeconds
                If elapsed >= WindowSeconds Then
                    _currentHashRate = _windowHashes / elapsed
                    _windowHashes = 0
                    _windowStart = DateTimeOffset.UtcNow
                End If
            End SyncLock
        End Sub

        ''' <summary>
        ''' Resets all statistics.
        ''' </summary>
        Public Sub Reset()
            SyncLock _syncLock
                _totalHashes = 0
                _windowHashes = 0
                _windowStart = DateTimeOffset.UtcNow
                _currentHashRate = 0
            End SyncLock
        End Sub

        ''' <summary>
        ''' Gets estimated time to find a block at current hash rate and difficulty.
        ''' </summary>
        Public Function EstimateTimeToBlock(difficultyBits As UInteger) As TimeSpan
            Dim rate As Double = CurrentHashRate
            If rate <= 0 Then Return TimeSpan.MaxValue

            Dim difficulty As Double = DifficultyCalculator.GetDifficultyRatio(difficultyBits)
            Dim expectedHashes As Double = difficulty * Math.Pow(2, 32)
            Dim seconds As Double = expectedHashes / rate

            If seconds > TimeSpan.MaxValue.TotalSeconds Then Return TimeSpan.MaxValue
            Return TimeSpan.FromSeconds(seconds)
        End Function

    End Class

End Namespace
