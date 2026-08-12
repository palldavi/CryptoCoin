' ===============================================================================
' CryptoCoin.Sdk - Models\BlockInfo.vb
' Block information model returned by SDK client methods.
' ===============================================================================

Imports System
Imports System.Collections.Generic

Namespace CryptoCoin.Sdk.Models

    ''' <summary>
    ''' Represents detailed information about a block in the CryptoCoin blockchain.
    ''' Returned by the SDK client's GetBlock and GetBlockByHeight methods.
    ''' </summary>
    Public Class BlockInfo

        ''' <summary>Gets or sets the block hash as a hex string.</summary>
        Public Property Hash As String

        ''' <summary>Gets or sets the block height in the chain.</summary>
        Public Property Height As Integer

        ''' <summary>Gets or sets the block version number.</summary>
        Public Property Version As Integer

        ''' <summary>Gets or sets the hash of the previous block.</summary>
        Public Property PreviousBlockHash As String

        ''' <summary>Gets or sets the merkle root of the block's transactions.</summary>
        Public Property MerkleRoot As String

        ''' <summary>Gets or sets the block timestamp as Unix epoch seconds.</summary>
        Public Property Timestamp As Long

        ''' <summary>Gets or sets the block timestamp as a DateTime.</summary>
        Public ReadOnly Property Time As DateTime
            Get
                Return DateTimeOffset.FromUnixTimeSeconds(Timestamp).UtcDateTime
            End Get
        End Property

        ''' <summary>Gets or sets the difficulty target (nBits).</summary>
        Public Property DifficultyTarget As UInteger

        ''' <summary>Gets or sets the nonce value used in mining.</summary>
        Public Property Nonce As UInteger

        ''' <summary>Gets or sets the number of transactions in the block.</summary>
        Public Property TransactionCount As Integer

        ''' <summary>Gets or sets the list of transaction IDs in this block.</summary>
        Public Property TransactionIds As List(Of String)

        ''' <summary>Gets or sets the block size in bytes.</summary>
        Public Property Size As Integer

        ''' <summary>Gets or sets the number of confirmations.</summary>
        Public Property Confirmations As Integer

        ''' <summary>Gets or sets the hash of the next block (if any).</summary>
        Public Property NextBlockHash As String

        ''' <summary>
        ''' Initializes a new empty BlockInfo instance.
        ''' </summary>
        Public Sub New()
            TransactionIds = New List(Of String)()
        End Sub

        ''' <summary>
        ''' Parses a BlockInfo from a JSON string response.
        ''' </summary>
        ''' <param name="json">The JSON string to parse.</param>
        ''' <returns>A populated BlockInfo instance.</returns>
        Public Shared Function FromJson(json As String) As BlockInfo
            Dim info As New BlockInfo()

            If String.IsNullOrEmpty(json) Then Return info

            info.Hash = ParseStringField(json, "hash")
            info.Height = ParseIntField(json, "height")
            info.Version = ParseIntField(json, "version")
            info.PreviousBlockHash = ParseStringField(json, "previousblockhash")
            info.MerkleRoot = ParseStringField(json, "merkleroot")
            info.Timestamp = ParseLongField(json, "time")
            info.Nonce = CUInt(ParseLongField(json, "nonce"))
            info.TransactionCount = ParseIntField(json, "tx_count")
            info.Size = ParseIntField(json, "size")
            info.Confirmations = ParseIntField(json, "confirmations")
            info.NextBlockHash = ParseStringField(json, "nextblockhash")

            ' Parse transaction ID array
            Dim txArrayStr As String = ParseArrayField(json, "tx")
            If Not String.IsNullOrEmpty(txArrayStr) Then
                info.TransactionIds = ParseStringArray(txArrayStr)
            End If

            Return info
        End Function

        ''' <summary>
        ''' Returns a string representation of this block info.
        ''' </summary>
        Public Overrides Function ToString() As String
            Return $"Block #{Height} ({Hash?.Substring(0, 16)}...) - {TransactionCount} txs"
        End Function

        ' --- JSON parsing helpers ---

        Private Shared Function ParseStringField(json As String, key As String) As String
            Dim searchKey As String = $"""{key}"":"""
            Dim idx As Integer = json.IndexOf(searchKey, StringComparison.Ordinal)
            If idx < 0 Then Return String.Empty
            Dim start As Integer = idx + searchKey.Length
            Dim endIdx As Integer = json.IndexOf(""""c, start)
            If endIdx < 0 Then Return String.Empty
            Return json.Substring(start, endIdx - start)
        End Function

        Private Shared Function ParseIntField(json As String, key As String) As Integer
            Dim value As Long = ParseLongField(json, key)
            Return CInt(Math.Min(value, Integer.MaxValue))
        End Function

        Private Shared Function ParseLongField(json As String, key As String) As Long
            Dim searchKey As String = $"""{key}"":"
            Dim idx As Integer = json.IndexOf(searchKey, StringComparison.Ordinal)
            If idx < 0 Then Return 0

            Dim start As Integer = idx + searchKey.Length
            While start < json.Length AndAlso Char.IsWhiteSpace(json(start))
                start += 1
            End While

            Dim endIdx As Integer = start
            While endIdx < json.Length AndAlso (Char.IsDigit(json(endIdx)) OrElse json(endIdx) = "-"c)
                endIdx += 1
            End While

            Dim numStr As String = json.Substring(start, endIdx - start)
            Dim result As Long
            Long.TryParse(numStr, result)
            Return result
        End Function

        Private Shared Function ParseArrayField(json As String, key As String) As String
            Dim searchKey As String = $"""{key}"":"
            Dim idx As Integer = json.IndexOf(searchKey, StringComparison.Ordinal)
            If idx < 0 Then Return String.Empty

            Dim start As Integer = json.IndexOf("["c, idx)
            If start < 0 Then Return String.Empty

            Dim depth As Integer = 1
            Dim pos As Integer = start + 1
            While pos < json.Length AndAlso depth > 0
                If json(pos) = "["c Then depth += 1
                If json(pos) = "]"c Then depth -= 1
                pos += 1
            End While

            Return json.Substring(start, pos - start)
        End Function

        Private Shared Function ParseStringArray(arrayJson As String) As List(Of String)
            Dim result As New List(Of String)()
            Dim content As String = arrayJson.Trim().TrimStart("["c).TrimEnd("]"c)
            If String.IsNullOrWhiteSpace(content) Then Return result

            Dim items As String() = content.Split(","c)
            For Each item As Object In items
                Dim cleaned As String = item.Trim().Trim(""""c)
                If Not String.IsNullOrEmpty(cleaned) Then
                    result.Add(cleaned)
                End If
            Next

            Return result
        End Function

    End Class

End Namespace
