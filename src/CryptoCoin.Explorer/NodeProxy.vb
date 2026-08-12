Imports System.IO
Imports System.Net
Imports System.Text

Namespace CryptoCoin.Explorer

    ''' <summary>
    ''' Lightweight HTTP client that forwards requests to a running CryptoCoin node's
    ''' RPC endpoint and returns raw JSON results. Used when the Explorer is run
    ''' alongside a node via --nodeurl.
    ''' </summary>
    Public Class NodeProxy

        Private ReadOnly _nodeUrl As String

        Public Sub New(nodeUrl As String)
            _nodeUrl = nodeUrl.TrimEnd("/"c) & "/"
        End Sub

        ''' <summary>
        ''' Calls an RPC method on the node and returns the raw result JSON string.
        ''' Returns Nothing on any error.
        ''' </summary>
        Public Function Fetch(method As String, Optional params As String = Nothing) As String
            Try
                Dim body As String
                If params IsNot Nothing Then
                    body = $"{{""method"":""{method}"",""params"":[{params}],""id"":1}}"
                Else
                    body = $"{{""method"":""{method}"",""params"":[],""id"":1}}"
                End If

                Dim bytes As Byte() = System.Text.Encoding.UTF8.GetBytes(body)
                Dim req As HttpWebRequest = CType(WebRequest.Create(_nodeUrl), HttpWebRequest)
                req.Method = "POST"
                req.ContentType = "application/json"
                req.ContentLength = bytes.Length
                req.Timeout = 5000
                req.SendChunked = False
                req.ProtocolVersion = New Version(1, 1)
                req.KeepAlive = False

                Dim stream As System.IO.Stream = req.GetRequestStream()
                stream.Write(bytes, 0, bytes.Length)
                stream.Close()

                Dim resp As HttpWebResponse = CType(req.GetResponse(), HttpWebResponse)
                Dim reader As New System.IO.StreamReader(resp.GetResponseStream(), System.Text.Encoding.UTF8)
                Dim json As String = reader.ReadToEnd()
                reader.Close()
                resp.Close()

                Dim result As String = ExtractResult(json)
                Console.WriteLine($"[Proxy] {method} -> {If(result IsNot Nothing, result.Substring(0, Math.Min(80, result.Length)), "NULL")}")
                Return result
            Catch ex As Exception
                Console.WriteLine($"[Proxy] {method} FAILED: {ex.Message}")
                Return Nothing
            End Try
        End Function

        Public Function FetchRaw(method As String, Optional params As String = Nothing) As String
            Try
                Dim body As String
                If params IsNot Nothing Then
                    body = $"{{""method"":""{method}"",""params"":[{params}],""id"":1}}"
                Else
                    body = $"{{""method"":""{method}"",""params"":[],""id"":1}}"
                End If

                Dim bytes As Byte() = System.Text.Encoding.UTF8.GetBytes(body)
                Dim req As HttpWebRequest = CType(WebRequest.Create(_nodeUrl), HttpWebRequest)
                req.Method = "POST"
                req.ContentType = "application/json"
                req.ContentLength = bytes.Length
                req.Timeout = 5000
                req.SendChunked = False
                req.ProtocolVersion = New Version(1, 1)
                req.KeepAlive = False

                Dim stream As System.IO.Stream = req.GetRequestStream()
                stream.Write(bytes, 0, bytes.Length)
                stream.Close()

                Dim resp As HttpWebResponse = CType(req.GetResponse(), HttpWebResponse)
                Dim reader As New System.IO.StreamReader(resp.GetResponseStream(), System.Text.Encoding.UTF8)
                Dim result As String = reader.ReadToEnd()
                reader.Close()
                resp.Close()
                Return result
            Catch ex As Exception
                Console.WriteLine($"[Proxy] FetchRaw {method} FAILED: {ex.Message}")
                Return Nothing
            End Try
        End Function

        ''' <summary>Extracts the value of the "result" key from a JSON-RPC response.</summary>
        Private Shared Function ExtractResult(json As String) As String
            If String.IsNullOrEmpty(json) Then Return Nothing
            Dim key As String = """result"":"
            Dim idx As Integer = json.IndexOf(key, StringComparison.Ordinal)
            If idx < 0 Then Return Nothing
            Dim start As Integer = idx + key.Length
            ' Skip whitespace
            While start < json.Length AndAlso json(start) = " "c
                start += 1
            End While
            If start >= json.Length Then Return Nothing
            Dim ch As Char = json(start)
            ' null
            If json.Substring(start).StartsWith("null") Then Return Nothing
            ' string
            If ch = """"c Then
                Dim endQ As Integer = json.IndexOf(""""c, start + 1)
                If endQ < 0 Then Return Nothing
                Return json.Substring(start + 1, endQ - start - 1)
            End If
            ' object or array
            If ch = "{"c OrElse ch = "["c Then
                Dim close As Char = If(ch = "{"c, "}"c, "]"c)
                Dim depth As Integer = 1
                Dim pos As Integer = start + 1
                While pos < json.Length AndAlso depth > 0
                    If json(pos) = ch Then depth += 1
                    If json(pos) = close Then depth -= 1
                    pos += 1
                End While
                Return json.Substring(start, pos - start)
            End If
            ' number / bool
            Dim endPos As Integer = start
            While endPos < json.Length AndAlso json(endPos) <> ","c AndAlso
                  json(endPos) <> "}"c AndAlso json(endPos) <> "]"c
                endPos += 1
            End While
            Return json.Substring(start, endPos - start).Trim()
        End Function

    End Class

End Namespace
