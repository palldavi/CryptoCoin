Imports System.Text
Imports CryptoCoin.Cryptography

Namespace CryptoCoin.Wallet

    ''' <summary>
    ''' Creates and parses CryptoCoin payment request URIs.
    ''' Format: cryptocoin:address?amount=X&amp;label=Y&amp;message=Z
    ''' Similar to BIP21 for Bitcoin.
    ''' </summary>
    Public Class PaymentRequest

        Private Const UriScheme As String = "cryptocoin"

        ''' <summary>
        ''' The destination address for the payment.
        ''' </summary>
        Public Property Address As String

        ''' <summary>
        ''' The requested amount in CryptoCoin (decimal, e.g., 1.5 = 1.5 CCC).
        ''' Zero means no specific amount is requested.
        ''' </summary>
        Public Property Amount As Decimal

        ''' <summary>
        ''' A label for the address (e.g., recipient name).
        ''' </summary>
        Public Property Label As String = String.Empty

        ''' <summary>
        ''' A message describing the purpose of the payment.
        ''' </summary>
        Public Property Message As String = String.Empty

        ''' <summary>
        ''' Additional custom parameters in the request.
        ''' </summary>
        Public ReadOnly Property Parameters As Dictionary(Of String, String)

        ''' <summary>
        ''' Creates a new empty payment request.
        ''' </summary>
        Public Sub New()
            _Parameters = New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
        End Sub

        ''' <summary>
        ''' Creates a payment request for the specified address.
        ''' </summary>
        ''' <param name="address">The destination CryptoCoin address.</param>
        Public Sub New(address As String)
            If String.IsNullOrEmpty(address) Then Throw New ArgumentNullException(NameOf(address))
            If Not AddressEncoder.IsValid(address) Then
                Throw New ArgumentException("Invalid CryptoCoin address.", NameOf(address))
            End If

            Me.Address = address
            _Parameters = New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
        End Sub

        ''' <summary>
        ''' Creates a payment request with address and amount.
        ''' </summary>
        ''' <param name="address">The destination CryptoCoin address.</param>
        ''' <param name="amount">The requested amount in CryptoCoin.</param>
        Public Sub New(address As String, amount As Decimal)
            Me.New(address)
            If amount < 0 Then Throw New ArgumentOutOfRangeException(NameOf(amount), "Amount cannot be negative.")
            Me.Amount = amount
        End Sub

        ''' <summary>
        ''' Creates a payment request with address, amount, and label.
        ''' </summary>
        ''' <param name="address">The destination CryptoCoin address.</param>
        ''' <param name="amount">The requested amount in CryptoCoin.</param>
        ''' <param name="label">A label for the recipient.</param>
        Public Sub New(address As String, amount As Decimal, label As String)
            Me.New(address, amount)
            Me.Label = If(label, String.Empty)
        End Sub

        ''' <summary>
        ''' Converts the payment request to a URI string.
        ''' </summary>
        ''' <returns>A URI in the format cryptocoin:address?amount=X&amp;label=Y</returns>
        Public Function ToUri() As String
            If String.IsNullOrEmpty(Address) Then
                Throw New InvalidOperationException("Address must be set before generating URI.")
            End If

            Dim sb As New StringBuilder()
            sb.Append(UriScheme)
            sb.Append(":")
            sb.Append(Address)

            Dim hasParams As Boolean = False

            If Amount > 0 Then
                sb.Append(If(hasParams, "&", "?"))
                sb.Append("amount=")
                sb.Append(Amount.ToString("G"))
                hasParams = True
            End If

            If Not String.IsNullOrEmpty(Label) Then
                sb.Append(If(hasParams, "&", "?"))
                sb.Append("label=")
                sb.Append(Uri.EscapeDataString(Label))
                hasParams = True
            End If

            If Not String.IsNullOrEmpty(Message) Then
                sb.Append(If(hasParams, "&", "?"))
                sb.Append("message=")
                sb.Append(Uri.EscapeDataString(Message))
                hasParams = True
            End If

            For Each kvp As Object In Parameters
                sb.Append(If(hasParams, "&", "?"))
                sb.Append(Uri.EscapeDataString(kvp.Key))
                sb.Append("=")
                sb.Append(Uri.EscapeDataString(kvp.Value))
                hasParams = True
            Next

            Return sb.ToString()
        End Function

        ''' <summary>
        ''' Parses a payment request URI string.
        ''' </summary>
        ''' <param name="uri">The URI to parse (e.g., "cryptocoin:CAddr123?amount=1.5&amp;label=Bob").</param>
        ''' <returns>A populated PaymentRequest instance.</returns>
        Public Shared Function Parse(uri As String) As PaymentRequest
            If String.IsNullOrEmpty(uri) Then Throw New ArgumentNullException(NameOf(uri))

            ' Validate scheme
            If Not uri.StartsWith(UriScheme & ":", StringComparison.OrdinalIgnoreCase) Then
                Throw New FormatException($"URI must start with '{UriScheme}:'.")
            End If

            Dim remainder As String = uri.Substring(UriScheme.Length + 1)
            Dim request As New PaymentRequest()

            ' Split address from parameters
            Dim queryIndex As Integer = remainder.IndexOf("?"c)
            If queryIndex >= 0 Then
                request.Address = remainder.Substring(0, queryIndex)
                Dim queryString As String = remainder.Substring(queryIndex + 1)
                ParseQueryString(queryString, request)
            Else
                request.Address = remainder
            End If

            If String.IsNullOrEmpty(request.Address) Then
                Throw New FormatException("Payment request URI must contain an address.")
            End If

            Return request
        End Function

        ''' <summary>
        ''' Attempts to parse a payment request URI without throwing exceptions.
        ''' </summary>
        ''' <param name="uri">The URI to parse.</param>
        ''' <param name="request">The parsed request if successful.</param>
        ''' <returns>True if parsing succeeded; otherwise false.</returns>
        Public Shared Function TryParse(uri As String, ByRef request As PaymentRequest) As Boolean
            Try
                request = Parse(uri)
                Return True
            Catch
                request = Nothing
                Return False
            End Try
        End Function

        ''' <summary>
        ''' Gets the amount in satoshis (1 CCC = 100,000,000 satoshis).
        ''' </summary>
        Public ReadOnly Property AmountInSatoshis As Long
            Get
                Return CLng(Amount * 100000000D)
            End Get
        End Property

        Private Shared Sub ParseQueryString(queryString As String, request As PaymentRequest)
            If String.IsNullOrEmpty(queryString) Then Return

            Dim pairs As String() = queryString.Split("&"c)
            For Each pair As Object In pairs
                Dim eqIndex As Integer = pair.IndexOf("="c)
                If eqIndex < 0 Then Continue For

                Dim key As String = Uri.UnescapeDataString(pair.Substring(0, eqIndex)).ToLowerInvariant()
                Dim value As String = Uri.UnescapeDataString(pair.Substring(eqIndex + 1))

                Select Case key
                    Case "amount"
                        Dim parsedAmount As Decimal
                        If Decimal.TryParse(value, Globalization.NumberStyles.Any,
                                           Globalization.CultureInfo.InvariantCulture, parsedAmount) Then
                            request.Amount = parsedAmount
                        End If
                    Case "label"
                        request.Label = value
                    Case "message"
                        request.Message = value
                    Case Else
                        request.Parameters(key) = value
                End Select
            Next
        End Sub

        Public Overrides Function ToString() As String
            Return ToUri()
        End Function

    End Class

End Namespace
