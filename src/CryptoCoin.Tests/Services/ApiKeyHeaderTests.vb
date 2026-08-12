Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.ServiceModel
Imports System.ServiceModel.Channels
Imports CryptoCoin.Services.Security

Namespace CryptoCoin.Tests.Services

    ''' <summary>
    ''' Unit tests for the WCF API key security header.
    ''' Verifies that the header can be written to and read from a SOAP message.
    ''' </summary>
    <TestClass>
    Public Class ApiKeyHeaderTests

        <TestMethod>
        Public Sub ApiKeyHeader_Name_IsApiKey()
            Dim header As New ApiKeyHeader("test-key")
            Assert.AreEqual("ApiKey", header.Name)
        End Sub

        <TestMethod>
        Public Sub ApiKeyHeader_Namespace_IsSecurityNamespace()
            Dim header As New ApiKeyHeader("test-key")
            Assert.AreEqual("http://cryptocoin.services/2024/security", header.Namespace)
        End Sub

        <TestMethod>
        Public Sub ApiKeyHeader_WriteAndRead_RoundTrip()
            Const key As String = "cryptocoin-demo-key"

            ' Create a WCF message and add the header
            Dim msg As Message = Message.CreateMessage(
                MessageVersion.Soap11, "test-action", "test-body")
            msg.Headers.Add(New ApiKeyHeader(key))

            ' Read it back
            Dim readBack As String = ApiKeyHeader.ReadFrom(msg)
            Assert.AreEqual(key, readBack)
        End Sub

        <TestMethod>
        Public Sub ApiKeyHeader_ReadFrom_MissingHeader_ReturnsNothing()
            Dim msg As Message = Message.CreateMessage(
                MessageVersion.Soap11, "test-action", "test-body")

            Dim result As String = ApiKeyHeader.ReadFrom(msg)
            Assert.IsNull(result)
        End Sub

        <TestMethod>
        Public Sub ApiKeyHeader_WriteAndRead_EmptyKey_RoundTrip()
            Dim msg As Message = Message.CreateMessage(
                MessageVersion.Soap11, "test-action", "test-body")
            msg.Headers.Add(New ApiKeyHeader(""))

            Dim readBack As String = ApiKeyHeader.ReadFrom(msg)
            Assert.AreEqual("", readBack)
        End Sub

        <TestMethod>
        Public Sub ApiKeyHeader_WriteAndRead_SpecialCharacters_RoundTrip()
            Const key As String = "key-with-special_chars.123"
            Dim msg As Message = Message.CreateMessage(
                MessageVersion.Soap11, "test-action", "test-body")
            msg.Headers.Add(New ApiKeyHeader(key))

            Dim readBack As String = ApiKeyHeader.ReadFrom(msg)
            Assert.AreEqual(key, readBack)
        End Sub

    End Class

End Namespace
