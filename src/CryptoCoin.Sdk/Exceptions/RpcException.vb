' ===============================================================================
' CryptoCoin.Sdk - Exceptions\RpcException.vb
' Custom exception for JSON-RPC communication errors.
' ===============================================================================

Imports System

Namespace CryptoCoin.Sdk.Exceptions

    ''' <summary>
    ''' Exception thrown when a JSON-RPC call to the CryptoCoin node fails.
    ''' Contains the RPC error code and message from the node response.
    ''' </summary>
    <Serializable>
    Public Class RpcException
        Inherits Exception

        ''' <summary>Gets the JSON-RPC error code returned by the node.</summary>
        Public ReadOnly Property ErrorCode As Integer

        ''' <summary>Gets the RPC method that was called (if available).</summary>
        Public ReadOnly Property RpcMethod As String

        ''' <summary>
        ''' Initializes a new RpcException with the specified error code and message.
        ''' </summary>
        ''' <param name="errorCode">The JSON-RPC error code.</param>
        ''' <param name="message">The error message.</param>
        Public Sub New(errorCode As Integer, message As String)
            MyBase.New(message)
            Me.ErrorCode = errorCode
        End Sub

        ''' <summary>
        ''' Initializes a new RpcException with error code, message, and inner exception.
        ''' </summary>
        ''' <param name="errorCode">The JSON-RPC error code.</param>
        ''' <param name="message">The error message.</param>
        ''' <param name="innerException">The inner exception that caused this error.</param>
        Public Sub New(errorCode As Integer, message As String, innerException As Exception)
            MyBase.New(message, innerException)
            Me.ErrorCode = errorCode
        End Sub

        ''' <summary>
        ''' Initializes a new RpcException with error code, message, method, and inner exception.
        ''' </summary>
        ''' <param name="errorCode">The JSON-RPC error code.</param>
        ''' <param name="message">The error message.</param>
        ''' <param name="rpcMethod">The RPC method that was called.</param>
        ''' <param name="innerException">The inner exception that caused this error.</param>
        Public Sub New(errorCode As Integer, message As String, rpcMethod As String, innerException As Exception)
            MyBase.New(message, innerException)
            Me.ErrorCode = errorCode
            Me.RpcMethod = rpcMethod
        End Sub

        ''' <summary>
        ''' Returns a string representation of this exception including the error code.
        ''' </summary>
        Public Overrides Function ToString() As String
            Dim methodInfo As String = If(Not String.IsNullOrEmpty(RpcMethod), $" (method: {RpcMethod})", "")
            Return $"RpcException [code={ErrorCode}]{methodInfo}: {Message}"
        End Function

    End Class

End Namespace
