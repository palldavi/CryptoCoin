' ===============================================================================
' CryptoCoin.Sdk - Models\NetworkInfo.vb
' Network status model returned by SDK client methods.
' ===============================================================================

Imports System

Namespace CryptoCoin.Sdk.Models

    ''' <summary>
    ''' Represents network status information from a CryptoCoin node.
    ''' Includes connection count, protocol version, and relay status.
    ''' </summary>
    Public Class NetworkInfo

        ''' <summary>Gets or sets the node software version string.</summary>
        Public Property Version As String

        ''' <summary>Gets or sets the protocol version number.</summary>
        Public Property ProtocolVersion As Integer

        ''' <summary>Gets or sets the number of active connections.</summary>
        Public Property Connections As Integer

        ''' <summary>Gets or sets whether the node relays transactions.</summary>
        Public Property RelayEnabled As Boolean

        ''' <summary>Gets or sets the local node address.</summary>
        Public Property LocalAddress As String

        ''' <summary>Gets or sets the network name (mainnet, testnet).</summary>
        Public Property NetworkName As String

        ''' <summary>Gets or sets the minimum relay fee rate.</summary>
        Public Property MinRelayFee As Decimal

        ''' <summary>
        ''' Initializes a new empty NetworkInfo instance.
        ''' </summary>
        Public Sub New()
            Version = String.Empty
            LocalAddress = String.Empty
            NetworkName = "mainnet"
        End Sub

        ''' <summary>
        ''' Parses a NetworkInfo from a JSON string response.
        ''' </summary>
        ''' <param name="json">The JSON string to parse.</param>
        ''' <returns>A populated NetworkInfo instance.</returns>
        Public Shared Function FromJson(json As String) As NetworkInfo
            Dim info As New NetworkInfo()

            If String.IsNullOrEmpty(json) Then Return info

            info.Version = ParseStringField(json, "version")
            info.ProtocolVersion = ParseIntField(json, "protocolversion")
            info.Connections = ParseIntField(json, "connections")
            info.NetworkName = ParseStringField(json, "network")

            Dim relayStr As String = ParseStringField(json, "relay")
            info.RelayEnabled = relayStr <> "false"

            Return info
        End Function

        ''' <summary>
        ''' Returns a string representation of the network info.
        ''' </summary>
        Public Overrides Function ToString() As String
            Return $"Network: {NetworkName}, Connections: {Connections}, Version: {Version}"
        End Function

        Private Shared Function ParseStringField(json As String, key As String) As String
            Dim searchKey As String = $"""{key}"":"""
            Dim idx As Integer = json.IndexOf(searchKey, StringComparison.Ordinal)
            If idx < 0 Then Return String.Empty
            Dim start As Integer = idx + searchKey.Length
            Dim endIdx As Integer = json.IndexOf(""""c, start)
            If endIdx < 0 Then Return String.Empty
            Return json.Substring(start, endIdx - start)
        End Function

        Private Shared Function ParseIntField(json As String, key As String) As Integer
            Dim searchKey As String = $"""{key}"":"
            Dim idx As Integer = json.IndexOf(searchKey, StringComparison.Ordinal)
            If idx < 0 Then Return 0
            Dim start As Integer = idx + searchKey.Length
            While start < json.Length AndAlso Char.IsWhiteSpace(json(start))
                start += 1
            End While
            Dim endIdx As Integer = start
            While endIdx < json.Length AndAlso (Char.IsDigit(json(endIdx)) OrElse json(endIdx) = "-"c)
                endIdx += 1
            End While
            Dim numStr As String = json.Substring(start, endIdx - start)
            Dim result As Integer
            Integer.TryParse(numStr, result)
            Return result
        End Function

    End Class

End Namespace
