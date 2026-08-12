Imports System.ServiceModel
Imports System.ServiceModel.Channels
Imports System.ServiceModel.Dispatcher
Imports System.ServiceModel.Description

Namespace CryptoCoin.Services.Security

    ''' <summary>
    ''' WCF service-side message inspector.
    ''' Validates the API key header on every inbound SOAP request.
    ''' Rejects requests with a missing or incorrect key with FaultException.
    ''' </summary>
    Public Class ApiKeyServiceInspector
        Implements IDispatchMessageInspector

        Private ReadOnly _expectedKey As String

        Public Sub New(expectedKey As String)
            _expectedKey = expectedKey
        End Sub

        Public Function AfterReceiveRequest(ByRef request As Message,
                                            channel As IClientChannel,
                                            instanceContext As InstanceContext) As Object _
                        Implements IDispatchMessageInspector.AfterReceiveRequest

            Dim receivedKey As String = ApiKeyHeader.ReadFrom(request)

            If String.IsNullOrEmpty(receivedKey) Then
                Throw New FaultException("API key header is missing. " &
                    "Include the ApiKey header in the http://cryptocoin.services/2024/security namespace.")
            End If

            If Not String.Equals(receivedKey, _expectedKey, StringComparison.Ordinal) Then
                Throw New FaultException("Invalid API key.")
            End If

            Return Nothing
        End Function

        Public Sub BeforeSendReply(ByRef reply As Message,
                                   correlationState As Object) _
                   Implements IDispatchMessageInspector.BeforeSendReply
            ' Nothing to add to replies
        End Sub

    End Class

    ''' <summary>
    ''' WCF endpoint behavior that attaches ApiKeyServiceInspector to a service endpoint.
    ''' Applied to each ServiceEndpoint when the ServiceHost is opened.
    ''' </summary>
    Public Class ApiKeyServiceBehavior
        Implements IEndpointBehavior

        Private ReadOnly _expectedKey As String

        Public Sub New(expectedKey As String)
            _expectedKey = expectedKey
        End Sub

        Public Sub AddBindingParameters(endpoint As ServiceEndpoint,
                                        bindingParameters As BindingParameterCollection) _
                   Implements IEndpointBehavior.AddBindingParameters
        End Sub

        Public Sub ApplyClientBehavior(endpoint As ServiceEndpoint,
                                       clientRuntime As ClientRuntime) _
                   Implements IEndpointBehavior.ApplyClientBehavior
        End Sub

        Public Sub ApplyDispatchBehavior(endpoint As ServiceEndpoint,
                                         endpointDispatcher As EndpointDispatcher) _
                   Implements IEndpointBehavior.ApplyDispatchBehavior
            endpointDispatcher.DispatchRuntime.MessageInspectors.Add(
                New ApiKeyServiceInspector(_expectedKey))
        End Sub

        Public Sub Validate(endpoint As ServiceEndpoint) _
               Implements IEndpointBehavior.Validate
        End Sub

    End Class

End Namespace
