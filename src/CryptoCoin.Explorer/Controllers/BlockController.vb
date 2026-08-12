Imports CryptoCoin.Core

Namespace CryptoCoin.Explorer.Controllers

    ''' <summary>
    ''' Handles block-related API requests.
    ''' When a NodeProxy is supplied the data is fetched live from the running node.
    ''' </summary>
    Public Class BlockController

        Private ReadOnly _blockchain As Blockchain
        Private ReadOnly _proxy As NodeProxy   ' Nothing when running standalone

        Public Sub New(blockchain As Blockchain, Optional proxy As NodeProxy = Nothing)
            _blockchain = blockchain
            _proxy = proxy
        End Sub

        Public Function GetBlock(hash As String) As String
            If _proxy IsNot Nothing Then
                Dim result As String = _proxy.Fetch("getblock", $"""{hash}""")
                If result Is Nothing Then Return "{""error"":""Block not found""}"
                Return result
            End If
            Dim block As Block = _blockchain.GetBlock(hash)
            If block Is Nothing Then Return "{""error"":""Block not found""}"
            Return SerializeBlock(block)
        End Function

        Public Function GetBlockByHeight(height As Integer) As String
            If _proxy IsNot Nothing Then
                Dim result As String = _proxy.Fetch("getblockbyheight", height.ToString())
                If result Is Nothing Then Return "{""error"":""Block not found""}"
                Return result
            End If
            Dim block As Block = _blockchain.GetBlockByHeight(height)
            If block Is Nothing Then Return "{""error"":""Block not found""}"
            Return SerializeBlock(block)
        End Function

        Public Function GetLatestBlocks() As String
            If _proxy IsNot Nothing Then
                ' Ask the node for its current height, then fetch blocks top-down
                Dim heightStr As String = _proxy.Fetch("getblockcount")
                Dim height As Integer = 0
                If heightStr Is Nothing OrElse Not Integer.TryParse(heightStr, height) Then
                    Return "[]"
                End If

                Dim blocks As New List(Of String)()
                Dim endHeight As Integer = Math.Max(0, height - 9)
                For h As Integer = height To endHeight Step -1
                    Dim b As String = _proxy.Fetch("getblockbyheight", h.ToString())
                    If b IsNot Nothing Then blocks.Add(b)
                Next
                Return JsonSerializer.CreateArray(blocks)
            End If

            ' Standalone path (original logic)
            Dim localBlocks As New List(Of String)()
            Dim startHeight As Integer = _blockchain.Height
            Dim localEnd As Integer = Math.Max(0, startHeight - 9)
            For h As Integer = startHeight To localEnd Step -1
                Dim block As Block = _blockchain.GetBlockByHeight(h)
                If block IsNot Nothing Then localBlocks.Add(SerializeBlock(block))
            Next
            Return JsonSerializer.CreateArray(localBlocks)
        End Function

        Private Function SerializeBlock(block As Block) As String
            Dim props As New List(Of String)()
            props.Add(JsonSerializer.PropStr("hash", block.Hash))
            props.Add(JsonSerializer.PropInt("height", block.Height))
            props.Add(JsonSerializer.PropLong("timestamp", block.Header.Timestamp))
            props.Add(JsonSerializer.PropInt("txcount", block.TransactionCount))
            props.Add(JsonSerializer.PropStr("previousHash", block.Header.PreviousBlockHash))
            props.Add(JsonSerializer.PropStr("merkleRoot", block.Header.MerkleRoot))
            props.Add(JsonSerializer.PropLong("bits", CLng(block.Header.Bits)))
            props.Add(JsonSerializer.PropLong("nonce", CLng(block.Header.Nonce)))
            props.Add(JsonSerializer.PropInt("size", block.Size))

            Dim txIds As New List(Of String)()
            For Each txId As String In block.TransactionIds
                txIds.Add(JsonSerializer.QuoteString(txId))
            Next
            props.Add(JsonSerializer.Prop("transactions", JsonSerializer.CreateArray(txIds)))

            Return JsonSerializer.CreateObject(props.ToArray())
        End Function

    End Class

End Namespace
