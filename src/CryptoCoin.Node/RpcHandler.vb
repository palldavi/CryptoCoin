Imports CryptoCoin.Core
Imports CryptoCoin.Transactions
Imports CryptoCoin.Mining

Namespace CryptoCoin.Node

    ''' <summary>
    ''' Handles JSON-RPC method calls using actual Blockchain/Mempool/Miner APIs.
    ''' </summary>
    Public Class RpcHandler

        Private ReadOnly _node As NodeService

        Public Sub New(node As NodeService)
            _node = node
        End Sub

        ''' <summary>
        ''' Parses and dispatches an RPC request, returns JSON response.
        ''' </summary>
        Public Function HandleRequest(requestBody As String) As String
            ' Simple JSON parsing (no external dependencies)
            Dim method As String = ExtractJsonString(requestBody, "method")
            Dim id As String = ExtractJsonString(requestBody, "id")

            Dim result As String = ""
            Dim errorMsg As String = ""

            Try
                Select Case method
                    Case "getblockcount"
                        result = _node.Blockchain.Height.ToString()
                    Case "getbestblockhash"
                        result = Quote(_node.Blockchain.Tip.Hash)
                    Case "getblock"
                        result = HandleGetBlock(requestBody)
                    Case "getblockbyheight"
                        result = HandleGetBlockByHeight(requestBody)
                    Case "getmempoolinfo"
                        result = HandleGetMempoolInfo()
                    Case "getmininginfo"
                        result = HandleGetMiningInfo()
                    Case "startmining"
                        result = HandleStartMining(requestBody)
                    Case "stopmining"
                        _node.Miner.Stop()
                        result = Quote("Mining stopped")
                    Case "getdifficulty"
                        Dim ratio As Double = DifficultyCalculator.GetDifficultyRatio(_node.Blockchain.CurrentDifficulty)
                        result = ratio.ToString("F8")
                    Case Else
                        errorMsg = $"Method not found: {method}"
                End Select
            Catch ex As Exception
                errorMsg = ex.Message
            End Try

            If Not String.IsNullOrEmpty(errorMsg) Then
                Return $"{{""id"":{Quote(id)},""error"":{{""message"":{Quote(errorMsg)}}},""result"":null}}"
            End If

            Return $"{{""id"":{Quote(id)},""error"":null,""result"":{result}}}"
        End Function

        Private Function HandleGetBlock(requestBody As String) As String
            Dim hash As String = ExtractParam(requestBody, 0)
            Dim block As Block = _node.Blockchain.GetBlock(hash)
            If block Is Nothing Then Return "null"

            Return $"{{""hash"":{Quote(block.Hash)},""height"":{block.Height},""txcount"":{block.TransactionCount},""timestamp"":{block.Header.Timestamp},""bits"":{block.Header.Bits},""nonce"":{block.Header.Nonce}}}"
        End Function

        Private Function HandleGetBlockByHeight(requestBody As String) As String
            Dim heightStr As String = ExtractParam(requestBody, 0)
            Dim height As Integer = Integer.Parse(heightStr)
            Dim block As Block = _node.Blockchain.GetBlockByHeight(height)
            If block Is Nothing Then Return "null"

            Return $"{{""hash"":{Quote(block.Hash)},""height"":{block.Height},""txcount"":{block.TransactionCount},""timestamp"":{block.Header.Timestamp}}}"
        End Function

        Private Function HandleGetMempoolInfo() As String
            Dim pool As Mempool = _node.Mempool
            Return $"{{""size"":{pool.Count},""bytes"":{pool.TotalBytes},""fees"":{pool.TotalFees}}}"
        End Function

        Private Function HandleGetMiningInfo() As String
            Dim miner As Miner = _node.Miner
            Dim mining As String = If(miner.IsMining, "true", "false")
            Return $"{{""mining"":{mining},""hashrate"":{miner.HashRate:F2},""blocks"":{miner.BlocksMined},""difficulty"":{DifficultyCalculator.GetDifficultyRatio(_node.Blockchain.CurrentDifficulty):F8}}}"
        End Function

        Private Function HandleStartMining(requestBody As String) As String
            Dim address As String = ExtractParam(requestBody, 0)
            If String.IsNullOrEmpty(address) Then
                Return Quote("Error: address required")
            End If
            _node.Miner.Start(address, 0)
            Return Quote("Mining started")
        End Function

        ' Simple JSON helpers (no external library needed)
        Private Shared Function ExtractJsonString(json As String, key As String) As String
            Dim search As String = $"""{key}"""
            Dim idx As Integer = json.IndexOf(search, StringComparison.OrdinalIgnoreCase)
            If idx < 0 Then Return ""
            idx = json.IndexOf(":"c, idx + search.Length)
            If idx < 0 Then Return ""
            idx += 1
            ' Skip whitespace
            While idx < json.Length AndAlso (json(idx) = " "c OrElse json(idx) = """"c)
                If json(idx) = """"c Then
                    idx += 1
                    Dim endIdx As Integer = json.IndexOf(""""c, idx)
                    If endIdx < 0 Then Return ""
                    Return json.Substring(idx, endIdx - idx)
                End If
                idx += 1
            End While
            ' Non-string value
            Dim valEnd As Integer = json.IndexOfAny(New Char() {","c, "}"c}, idx)
            If valEnd < 0 Then valEnd = json.Length
            Return json.Substring(idx, valEnd - idx).Trim()
        End Function

        Private Shared Function ExtractParam(json As String, index As Integer) As String
            Dim paramsIdx As Integer = json.IndexOf("""params""", StringComparison.OrdinalIgnoreCase)
            If paramsIdx < 0 Then Return ""
            Dim arrStart As Integer = json.IndexOf("["c, paramsIdx)
            If arrStart < 0 Then Return ""
            arrStart += 1
            ' Find the nth parameter
            Dim current As Integer = 0
            Dim pos As Integer = arrStart
            While current < index AndAlso pos < json.Length
                If json(pos) = ","c Then current += 1
                pos += 1
            End While
            ' Skip whitespace and quotes
            While pos < json.Length AndAlso (json(pos) = " "c OrElse json(pos) = """"c)
                If json(pos) = """"c Then
                    pos += 1
                    Dim endPos As Integer = json.IndexOf(""""c, pos)
                    If endPos < 0 Then Return ""
                    Return json.Substring(pos, endPos - pos)
                End If
                pos += 1
            End While
            Dim valEnd As Integer = json.IndexOfAny(New Char() {","c, "]"c}, pos)
            If valEnd < 0 Then valEnd = json.Length
            Return json.Substring(pos, valEnd - pos).Trim()
        End Function

        Private Shared Function Quote(value As String) As String
            Return $"""{value}"""
        End Function

    End Class

End Namespace
