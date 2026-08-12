Imports CryptoCoin.Core

Namespace CryptoCoin.Mining

    ''' <summary>
    ''' Represents a mining job - a candidate block ready for proof-of-work computation.
    ''' </summary>
    Public Class MiningJob

        ''' <summary>
        ''' Unique job identifier.
        ''' </summary>
        Public Property JobId As String

        ''' <summary>
        ''' The candidate block to mine.
        ''' </summary>
        Public Property Block As Block

        ''' <summary>
        ''' The difficulty target bits.
        ''' </summary>
        Public Property TargetBits As UInteger

        ''' <summary>
        ''' When this job was created.
        ''' </summary>
        Public Property CreatedAt As DateTimeOffset

        ''' <summary>
        ''' Whether this job is still valid (not superseded by a new block).
        ''' </summary>
        Public Property IsValid As Boolean = True

        ''' <summary>
        ''' The total fees from transactions included in this block.
        ''' </summary>
        Public Property TotalFees As Long

        ''' <summary>
        ''' The block reward (subsidy + fees).
        ''' </summary>
        Public Property TotalReward As Long

        Public Sub New()
            JobId = Guid.NewGuid().ToString("N").Substring(0, 16)
            CreatedAt = DateTimeOffset.UtcNow
        End Sub

        ''' <summary>
        ''' Gets the age of this job in seconds.
        ''' </summary>
        Public ReadOnly Property AgeSeconds As Double
            Get
                Return (DateTimeOffset.UtcNow - CreatedAt).TotalSeconds
            End Get
        End Property

        Public Overrides Function ToString() As String
            Return $"MiningJob(Id={JobId}, Height={Block?.Height}, Target={TargetBits:X8})"
        End Function

    End Class

End Namespace
