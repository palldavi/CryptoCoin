Imports System.ServiceModel.Channels
Imports System.Xml

Namespace CryptoCoin.Services.Security

    ''' <summary>
    ''' Custom SOAP message header that carries the API key.
    ''' Added to every outbound WCF request by ApiKeyClientInspector
    ''' and validated on every inbound request by ApiKeyServiceInspector.
    '''
    ''' Modernisation note: on .NET 10 / CoreWCF this pattern is replaced by
    ''' ASP.NET Core authentication middleware (API key, JWT bearer, etc.).
    ''' </summary>
    Public Class ApiKeyHeader
        Inherits MessageHeader

        Public Const HeaderName As String = "ApiKey"
        Public Const HeaderNamespace As String = "http://cryptocoin.services/2024/security"

        Private ReadOnly _apiKey As String

        Public Sub New(apiKey As String)
            _apiKey = apiKey
        End Sub

        Public Overrides ReadOnly Property Name As String
            Get
                Return HeaderName
            End Get
        End Property

        Public Overrides ReadOnly Property [Namespace] As String
            Get
                Return HeaderNamespace
            End Get
        End Property

        Protected Overrides Sub OnWriteHeaderContents(writer As XmlDictionaryWriter,
                                                      messageVersion As MessageVersion)
            writer.WriteString(_apiKey)
        End Sub

        ''' <summary>
        ''' Reads the API key value from an inbound message header.
        ''' Returns Nothing if the header is absent.
        ''' </summary>
        Public Shared Function ReadFrom(message As Message) As String
            Dim idx As Integer = message.Headers.FindHeader(HeaderName, HeaderNamespace)
            If idx < 0 Then Return Nothing
            Dim reader As XmlDictionaryReader = message.Headers.GetReaderAtHeader(idx)
            reader.ReadStartElement()
            Return reader.ReadString()
        End Function

    End Class

End Namespace
