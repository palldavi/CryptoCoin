Imports System.ServiceModel
Imports System.ServiceModel.Channels
Imports System.ServiceModel.Dispatcher
Imports System.ServiceModel.Description

Namespace CryptoCoin.Services.Security

    ''' <summary>
    ''' WCF client-side message inspector.
    ''' Adds the API key header to every outbound SOAP request.
    ''' Applied via ApiKeyEndpointBehavior.
    ''' </summary>
    Public Class ApiKeyClientInspector
        Implements IClientMessageInspector

        Private ReadOnly _apiKey As String

        Public Sub New(apiKey As String)
            _apiKey = apiKey
        End Sub

        Public Function BeforeSendRequest(ByRef request As Message,
                                          channel As IClientChannel) As Object _
                        Implements IClientMessageInspector.BeforeSendRequest
            request.Headers.Add(New ApiKeyHeader(_apiKey))
            Return Nothing
        End Function

        Public Sub AfterReceiveReply(ByRef reply As Message,
                                     correlationState As Object) _
                   Implements IClientMessageInspector.AfterReceiveReply
            ' Nothing to do on the reply
        End Sub

    End Class

    ''' <summary>
    ''' WCF endpoint behavior that attaches ApiKeyClientInspector to a client channel.
    ''' Usage: channel.Endpoint.Behaviors.Add(New ApiKeyEndpointBehavior("my-key"))
    ''' </summary>
    Public Class ApiKeyEndpointBehavior
        Implements IEndpointBehavior

        Private ReadOnly _apiKey As String

        Public Sub New(apiKey As String)
            _apiKey = apiKey
        End Sub

        Public Sub AddBindingParameters(endpoint As ServiceEndpoint,
                                        bindingParameters As BindingParameterCollection) _
                   Implements IEndpointBehavior.AddBindingParameters
        End Sub

        Public Sub ApplyClientBehavior(endpoint As ServiceEndpoint,
                                       clientRuntime As ClientRuntime) _
                   Implements IEndpointBehavior.ApplyClientBehavior
            clientRuntime.ClientMessageInspectors.Add(New ApiKeyClientInspector(_apiKey))
        End Sub

        Public Sub ApplyDispatchBehavior(endpoint As ServiceEndpoint,
                                         endpointDispatcher As EndpointDispatcher) _
                   Implements IEndpointBehavior.ApplyDispatchBehavior
        End Sub

        Public Sub Validate(endpoint As ServiceEndpoint) _
               Implements IEndpointBehavior.Validate
        End Sub

    End Class

End Namespace
