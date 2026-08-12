' ===============================================================================
' CryptoCoin.Sdk - RpcClient.vb
' Low-level HTTP JSON-RPC client with retry logic and error handling.
' ===============================================================================

Imports System
Imports System.IO
Imports System.Net
Imports System.Text
Imports System.Threading
Imports CryptoCoin.Sdk.Exceptions

Namespace CryptoCoin.Sdk

    ''' <summary>
    ''' Low-level JSON-RPC client for communicating with a CryptoCoin node.
    ''' Handles HTTP transport, authentication, request formatting, and retry logic.
    ''' </summary>
    Public Class RpcClient
        Implements IDisposable

        Private ReadOnly _endpoint As String
        Private ReadOnly _username As String
        Private ReadOnly _password As String
        Private ReadOnly _credentials As NetworkCredential
        Private _requestId As Integer
        Private _disposed As Boolean

        ''' <summary>Gets or sets the request timeout in milliseconds.</summary>
        Public Property TimeoutMs As Integer = 30000

        ''' <summary>Gets or sets the maximum number of retry attempts.</summary>
        Public Property MaxRetries As Integer = 3

        ''' <summary>Gets or sets the delay between retries in milliseconds.</summary>
        Public Property RetryDelayMs As Integer = 1000

        ''' <summary>Gets the RPC endpoint URL.</summary>
        Public ReadOnly Property Endpoint As String
            Get
                Return _endpoint
            End Get
        End Property

        ''' <summary>
        ''' Initializes a new RpcClient with the specified endpoint and credentials.
        ''' </summary>
        ''' <param name="endpoint">The full URL of the RPC endpoint.</param>
        ''' <param name="username">The authentication username.</param>
        ''' <param name="password">The authentication password.</param>
        Public Sub New(endpoint As String, username As String, password As String)
            _endpoint = endpoint
            _username = username
            _password = password
            _credentials = New NetworkCredential(username, password)
            _requestId = 0
        End Sub

        ''' <summary>
        ''' Executes a JSON-RPC method call with optional parameters.
        ''' </summary>
        ''' <param name="method">The RPC method name.</param>
        ''' <param name="params">Optional parameters as a JSON-formatted string.</param>
        ''' <returns>The result field from the JSON-RPC response.</returns>
        ''' <exception cref="RpcException">Thrown when the RPC call fails after all retries.</exception>
        Public Function [Call](method As String, Optional params As String = Nothing) As String
            Dim lastException As Exception = Nothing

            For attempt As Integer = 1 To MaxRetries
                Try
                    Return ExecuteRequest(method, params)
                Catch ex As RpcException
                    ' Don't retry application-level errors
                    Throw
                Catch ex As WebException
                    lastException = ex
                    If attempt < MaxRetries Then
                        Thread.Sleep(RetryDelayMs * attempt)
                    End If
                Catch ex As IOException
                    lastException = ex
                    If attempt < MaxRetries Then
                        Thread.Sleep(RetryDelayMs * attempt)
                    End If
                Catch ex As TimeoutException
                    lastException = ex
                    If attempt < MaxRetries Then
                        Thread.Sleep(RetryDelayMs * attempt)
                    End If
                End Try
            Next

            Throw New RpcException(-1,
                $"RPC call '{method}' failed after {MaxRetries} attempts: {lastException?.Message}",
                lastException)
        End Function

        ''' <summary>
        ''' Executes a single RPC request without retry logic.
        ''' </summary>
        Private Function ExecuteRequest(method As String, params As String) As String
            Dim id As Integer = Interlocked.Increment(_requestId)

            ' Build JSON-RPC request body
            Dim requestBody As String = BuildRequestJson(id, method, params)
            Dim bodyBytes As Byte() = Encoding.UTF8.GetBytes(requestBody)

            ' Create HTTP request
            Dim request As HttpWebRequest = CType(WebRequest.Create(_endpoint), HttpWebRequest)
            request.Method = "POST"
            request.ContentType = "application/json"
            request.ContentLength = bodyBytes.Length
            request.Timeout = TimeoutMs
            request.Credentials = _credentials
            request.PreAuthenticate = True

            ' Set Basic auth header directly
            Dim authString As String = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_username}:{_password}"))
            request.Headers.Add("Authorization", $"Basic {authString}")

            ' Write request body
            Using requestStream As Stream = request.GetRequestStream()
                requestStream.Write(bodyBytes, 0, bodyBytes.Length)
            End Using

            ' Read response
            Dim responseBody As String
            Try
                Using response As HttpWebResponse = CType(request.GetResponse(), HttpWebResponse)
                    Using reader As New StreamReader(response.GetResponseStream(), Encoding.UTF8)
                        responseBody = reader.ReadToEnd()
                    End Using
                End Using
            Catch ex As WebException
                If ex.Response IsNot Nothing Then
                    Using reader As New StreamReader(ex.Response.GetResponseStream(), Encoding.UTF8)
                        responseBody = reader.ReadToEnd()
                    End Using
                    ' Try to parse error from response
                    Dim errorCode As Integer = ParseErrorCode(responseBody)
                    Dim errorMessage As String = ParseErrorMessage(responseBody)
                    If errorCode <> 0 Then
                        Throw New RpcException(errorCode, errorMessage, ex)
                    End If
                End If
                Throw
            End Try

            ' Parse response
            Return ParseResult(responseBody)
        End Function

        ''' <summary>
        ''' Builds the JSON-RPC request body string.
        ''' </summary>
        Private Function BuildRequestJson(id As Integer, method As String, params As String) As String
            Dim sb As New StringBuilder()
            sb.Append("{""jsonrpc"":""2.0""")
            sb.Append($",""id"":{id}")
            sb.Append($",""method"":""{method}""")

            If params IsNot Nothing Then
                sb.Append($",""params"":[{params}]")
            Else
                sb.Append(",""params"":[]")
            End If

            sb.Append("}")
            Return sb.ToString()
        End Function

        ''' <summary>
        ''' Parses the result field from a JSON-RPC response.
        ''' </summary>
        Private Function ParseResult(responseBody As String) As String
            ' Check for error field
            Dim errorCode As Integer = ParseErrorCode(responseBody)
            If errorCode <> 0 Then
                Dim errorMessage As String = ParseErrorMessage(responseBody)
                Throw New RpcException(errorCode, errorMessage)
            End If

            ' Extract result value
            Dim resultKey As String = """result"":"
            Dim resultIndex As Integer = responseBody.IndexOf(resultKey, StringComparison.Ordinal)
            If resultIndex < 0 Then
                Throw New RpcException(-1, "No result field in response")
            End If

            Dim valueStart As Integer = resultIndex + resultKey.Length
            Return ExtractJsonValue(responseBody, valueStart)
        End Function

        ''' <summary>
        ''' Parses the error code from a JSON-RPC response.
        ''' </summary>
        Private Function ParseErrorCode(responseBody As String) As Integer
            Dim errorKey As String = """error"":"
            Dim errorIndex As Integer = responseBody.IndexOf(errorKey, StringComparison.Ordinal)
            If errorIndex < 0 Then Return 0

            ' Check if error is null
            Dim valueStart As Integer = errorIndex + errorKey.Length
            Dim trimmed As String = responseBody.Substring(valueStart).TrimStart()
            If trimmed.StartsWith("null") Then Return 0

            ' Extract code from error object
            Dim codeKey As String = """code"":"
            Dim codeIndex As Integer = responseBody.IndexOf(codeKey, errorIndex)
            If codeIndex < 0 Then Return -1

            Dim codeStart As Integer = codeIndex + codeKey.Length
            Dim codeEnd As Integer = codeStart
            While codeEnd < responseBody.Length AndAlso
                  (Char.IsDigit(responseBody(codeEnd)) OrElse responseBody(codeEnd) = "-"c)
                codeEnd += 1
            End While

            Dim codeStr As String = responseBody.Substring(codeStart, codeEnd - codeStart)
            Dim code As Integer
            Integer.TryParse(codeStr, code)
            Return code
        End Function

        ''' <summary>
        ''' Parses the error message from a JSON-RPC response.
        ''' </summary>
        Private Function ParseErrorMessage(responseBody As String) As String
            Dim msgKey As String = """message"":"""
            Dim msgIndex As Integer = responseBody.IndexOf(msgKey, StringComparison.Ordinal)
            If msgIndex < 0 Then Return "Unknown RPC error"

            Dim msgStart As Integer = msgIndex + msgKey.Length
            Dim msgEnd As Integer = responseBody.IndexOf(""""c, msgStart)
            If msgEnd < 0 Then Return "Unknown RPC error"

            Return responseBody.Substring(msgStart, msgEnd - msgStart)
        End Function

        ''' <summary>
        ''' Extracts a JSON value starting at the specified position.
        ''' </summary>
        Private Function ExtractJsonValue(json As String, startPos As Integer) As String
            Dim pos As Integer = startPos
            While pos < json.Length AndAlso Char.IsWhiteSpace(json(pos))
                pos += 1
            End While

            If pos >= json.Length Then Return ""

            Dim ch As Char = json(pos)

            ' String value
            If ch = """"c Then
                Dim endPos As Integer = pos + 1
                While endPos < json.Length
                    If json(endPos) = """"c AndAlso json(endPos - 1) <> "\"c Then
                        Return json.Substring(pos, endPos - pos + 1)
                    End If
                    endPos += 1
                End While
            End If

            ' Object or array
            If ch = "{"c OrElse ch = "["c Then
                Dim closeChar As Char = If(ch = "{"c, "}"c, "]"c)
                Dim depth As Integer = 1
                Dim endPos As Integer = pos + 1
                While endPos < json.Length AndAlso depth > 0
                    If json(endPos) = ch Then depth += 1
                    If json(endPos) = closeChar Then depth -= 1
                    endPos += 1
                End While
                Return json.Substring(pos, endPos - pos)
            End If

            ' Numeric, boolean, or null
            Dim valueEnd As Integer = pos
            While valueEnd < json.Length AndAlso json(valueEnd) <> ","c AndAlso
                  json(valueEnd) <> "}"c AndAlso json(valueEnd) <> "]"c
                valueEnd += 1
            End While

            Return json.Substring(pos, valueEnd - pos).Trim()
        End Function

        ''' <summary>
        ''' Disposes of the RPC client resources.
        ''' </summary>
        Public Sub Dispose() Implements IDisposable.Dispose
            _disposed = True
        End Sub

    End Class

End Namespace
