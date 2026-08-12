Imports CryptoCoin.Core
Imports CryptoCoin.Transactions

Namespace CryptoCoin.Explorer.Controllers

    ''' <summary>
    ''' Handles network and status API requests.
    ''' When a NodeProxy is supplied the data is fetched live from the running node.
    ''' </summary>
    Public Class NetworkController

        Private ReadOnly _blockchain As Blockchain
        Private ReadOnly _mempool As Mempool
        Private ReadOnly _proxy As NodeProxy
        Private ReadOnly _params As ChainParameters

        Public Sub New(blockchain As Blockchain, mempool As Mempool,
                       Optional proxy As NodeProxy = Nothing,
                       Optional params As ChainParameters = Nothing)
            _blockchain = blockchain
            _mempool = mempool
            _proxy = proxy
            _params = If(params, ChainParameters.Mainnet())
        End Sub

        Public Function GetNetworkInfo() As String
            If _proxy IsNot Nothing Then
                ' Pull height + difficulty from node RPC
                Dim heightStr As String = _proxy.Fetch("getblockcount")
                Dim height As Integer = 0
                Integer.TryParse(heightStr, height)

                Dim diffStr As String = _proxy.Fetch("getdifficulty")
                Dim diff As Double = 0
                Double.TryParse(diffStr, System.Globalization.NumberStyles.Any,
                                System.Globalization.CultureInfo.InvariantCulture, diff)

                Dim miningJson As String = _proxy.Fetch("getmininginfo")
                Dim hashRate As Double = 0
                Dim mempoolSize As Integer = 0
                If miningJson IsNot Nothing Then
                    hashRate = ExtractDouble(miningJson, "hashrate")
                End If

                Dim mempoolJson As String = _proxy.Fetch("getmempoolinfo")
                If mempoolJson IsNot Nothing Then
                    mempoolSize = CInt(ExtractDouble(mempoolJson, "size"))
                End If

                Dim props As New List(Of String)()
                props.Add(JsonSerializer.PropStr("coin", _params.CoinName))
                props.Add(JsonSerializer.PropStr("symbol", _params.CoinSymbol))
                props.Add(JsonSerializer.PropInt("height", height))
                props.Add(JsonSerializer.PropDbl("difficulty", diff))
                props.Add(JsonSerializer.PropDbl("hashRate", hashRate))
                props.Add(JsonSerializer.PropInt("mempoolSize", mempoolSize))
                props.Add(JsonSerializer.PropInt("blockCount", height + 1))
                Return JsonSerializer.CreateObject(props.ToArray())
            End If

            ' Standalone path
            Dim difficulty As Double = DifficultyCalculator.GetDifficultyRatio(_blockchain.CurrentDifficulty)
            Dim hr As Double = DifficultyCalculator.EstimateHashRate(
                _blockchain.CurrentDifficulty, _params.TargetBlockTimeSeconds)

            Dim standaloneProps As New List(Of String)()
            standaloneProps.Add(JsonSerializer.PropStr("coin", _params.CoinName))
            standaloneProps.Add(JsonSerializer.PropStr("symbol", _params.CoinSymbol))
            standaloneProps.Add(JsonSerializer.PropInt("height", _blockchain.Height))
            standaloneProps.Add(JsonSerializer.PropDbl("difficulty", difficulty))
            standaloneProps.Add(JsonSerializer.PropDbl("hashRate", hr))
            standaloneProps.Add(JsonSerializer.PropInt("mempoolSize", _mempool.Count))
            standaloneProps.Add(JsonSerializer.PropInt("blockCount", _blockchain.BlockCount))
            Return JsonSerializer.CreateObject(standaloneProps.ToArray())
        End Function

        Public Function GetStatus() As String
            If _proxy IsNot Nothing Then
                Dim heightStr As String = _proxy.Fetch("getblockcount")
                Dim height As Integer = 0
                Integer.TryParse(heightStr, height)

                Dim bestHash As String = If(_proxy.Fetch("getbestblockhash"), "")

                Dim mempoolJson As String = _proxy.Fetch("getmempoolinfo")
                Dim mempoolCount As Integer = 0
                Dim mempoolBytes As Long = 0
                If mempoolJson IsNot Nothing Then
                    mempoolCount = CInt(ExtractDouble(mempoolJson, "size"))
                    mempoolBytes = CLng(ExtractDouble(mempoolJson, "bytes"))
                End If

                Dim props As New List(Of String)()
                props.Add(JsonSerializer.PropInt("height", height))
                props.Add(JsonSerializer.PropStr("bestBlockHash", bestHash))
                props.Add(JsonSerializer.PropLong("bestBlockTime", DateTimeOffset.UtcNow.ToUnixTimeSeconds()))
                props.Add(JsonSerializer.PropInt("blockCount", height + 1))
                props.Add(JsonSerializer.PropInt("mempoolCount", mempoolCount))
                props.Add(JsonSerializer.PropLong("mempoolBytes", mempoolBytes))
                Return JsonSerializer.CreateObject(props.ToArray())
            End If

            ' Standalone path
            Dim tip As BlockIndex = _blockchain.Tip
            Dim standaloneProps As New List(Of String)()
            standaloneProps.Add(JsonSerializer.PropInt("height", _blockchain.Height))
            standaloneProps.Add(JsonSerializer.PropStr("bestBlockHash", tip.Hash))
            standaloneProps.Add(JsonSerializer.PropLong("bestBlockTime", tip.Timestamp))
            standaloneProps.Add(JsonSerializer.PropInt("blockCount", _blockchain.BlockCount))
            standaloneProps.Add(JsonSerializer.PropInt("mempoolCount", _mempool.Count))
            standaloneProps.Add(JsonSerializer.PropLong("mempoolBytes", _mempool.TotalBytes))
            Return JsonSerializer.CreateObject(standaloneProps.ToArray())
        End Function

        Private Shared Function ExtractDouble(json As String, key As String) As Double
            Dim search As String = $"""{key}"":"
            Dim idx As Integer = json.IndexOf(search, StringComparison.OrdinalIgnoreCase)
            If idx < 0 Then Return 0
            Dim start As Integer = idx + search.Length
            While start < json.Length AndAlso json(start) = " "c
                start += 1
            End While
            Dim endPos As Integer = start
            While endPos < json.Length AndAlso json(endPos) <> ","c AndAlso
                  json(endPos) <> "}"c AndAlso json(endPos) <> "]"c
                endPos += 1
            End While
            Dim val As Double
            Double.TryParse(json.Substring(start, endPos - start).Trim(),
                            System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, val)
            Return val
        End Function

    End Class

End Namespace
