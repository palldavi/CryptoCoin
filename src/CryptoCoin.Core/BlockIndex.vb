Imports System.Numerics

Namespace CryptoCoin.Core

    ''' <summary>
    ''' Lightweight index entry for a block in the chain.
    ''' Contains essential metadata without the full block data.
    ''' </summary>
    Public Class BlockIndex

        ''' <summary>
        ''' The block hash.
        ''' </summary>
        Public Property Hash As String

        ''' <summary>
        ''' The previous block hash.
        ''' </summary>
        Public Property PreviousHash As String

        ''' <summary>
        ''' The block height.
        ''' </summary>
        Public Property Height As Integer

        ''' <summary>
        ''' The block timestamp.
        ''' </summary>
        Public Property Timestamp As Long

        ''' <summary>
        ''' The difficulty target bits.
        ''' </summary>
        Public Property Bits As UInteger

        ''' <summary>
        ''' Number of transactions in the block.
        ''' </summary>
        Public Property TransactionCount As Integer

        ''' <summary>
        ''' Cumulative proof-of-work on this chain up to and including this block.
        ''' </summary>
        Public Property TotalWork As BigInteger

        ''' <summary>
        ''' Whether this block is on the main (best) chain.
        ''' </summary>
        Public Property IsMainChain As Boolean = True

        ''' <summary>
        ''' The block status (valid, invalid, etc.).
        ''' </summary>
        Public Property Status As BlockStatus = BlockStatus.Valid

        ''' <summary>
        ''' Gets the DateTime representation of the timestamp.
        ''' </summary>
        Public ReadOnly Property DateTime As DateTimeOffset
            Get
                Return DateTimeOffset.FromUnixTimeSeconds(Timestamp)
            End Get
        End Property

        Public Overrides Function ToString() As String
            Return $"BlockIndex(Height={Height}, Hash={If(Hash IsNot Nothing AndAlso Hash.Length > 16, Hash.Substring(0, 16), Hash)}...)"
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            Dim other As BlockIndex = TryCast(obj, BlockIndex)
            If other Is Nothing Then Return False
            Return String.Equals(Hash, other.Hash, StringComparison.OrdinalIgnoreCase)
        End Function

        Public Overrides Function GetHashCode() As Integer
            If Hash Is Nothing Then Return 0
            Return Hash.GetHashCode()
        End Function

    End Class

    ''' <summary>
    ''' Status of a block in the chain.
    ''' </summary>
    Public Enum BlockStatus
        ''' <summary>Block is valid and on the main chain.</summary>
        Valid = 0
        ''' <summary>Block header is valid but transactions not yet validated.</summary>
        HeaderValid = 1
        ''' <summary>Block failed validation.</summary>
        Invalid = 2
        ''' <summary>Block is valid but on a side chain.</summary>
        SideChain = 3
    End Enum

End Namespace
