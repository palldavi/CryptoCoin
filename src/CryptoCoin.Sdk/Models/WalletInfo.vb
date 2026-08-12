' ===============================================================================
' CryptoCoin.Sdk - Models\WalletInfo.vb
' Wallet status model returned by SDK client methods.
' ===============================================================================

Imports System

Namespace CryptoCoin.Sdk.Models

    ''' <summary>
    ''' Represents wallet status information from a CryptoCoin node.
    ''' Includes balance, transaction count, and key pool information.
    ''' </summary>
    Public Class WalletInfo

        ''' <summary>Gets or sets the wallet name/identifier.</summary>
        Public Property WalletName As String

        ''' <summary>Gets or sets the confirmed balance in CRC.</summary>
        Public Property Balance As Decimal

        ''' <summary>Gets or sets the unconfirmed balance in CRC.</summary>
        Public Property UnconfirmedBalance As Decimal

        ''' <summary>Gets or sets the immature balance (mining rewards not yet mature).</summary>
        Public Property ImmatureBalance As Decimal

        ''' <summary>Gets or sets the total number of transactions.</summary>
        Public Property TransactionCount As Integer

        ''' <summary>Gets or sets the key pool size (pre-generated keys).</summary>
        Public Property KeyPoolSize As Integer

        ''' <summary>Gets or sets the oldest key in the key pool timestamp.</summary>
        Public Property KeyPoolOldest As Long

        ''' <summary>Gets or sets whether the wallet is encrypted.</summary>
        Public Property IsEncrypted As Boolean

        ''' <summary>Gets or sets whether the wallet is locked.</summary>
        Public Property IsLocked As Boolean

        ''' <summary>Gets the total balance (confirmed + unconfirmed).</summary>
        Public ReadOnly Property TotalBalance As Decimal
            Get
                Return Balance + UnconfirmedBalance
            End Get
        End Property

        ''' <summary>
        ''' Initializes a new empty WalletInfo instance.
        ''' </summary>
        Public Sub New()
            WalletName = "default"
        End Sub

        ''' <summary>
        ''' Parses a WalletInfo from a JSON string response.
        ''' </summary>
        ''' <param name="json">The JSON string to parse.</param>
        ''' <returns>A populated WalletInfo instance.</returns>
        Public Shared Function FromJson(json As String) As WalletInfo
            Dim info As New WalletInfo()

            If String.IsNullOrEmpty(json) Then Return info

            info.WalletName = ParseStringField(json, "walletname")
            info.TransactionCount = ParseIntField(json, "txcount")
            info.KeyPoolSize = ParseIntField(json, "keypoolsize")
            info.KeyPoolOldest = CLng(ParseIntField(json, "keypoololdest"))

            Dim balanceStr As String = ParseNumericField(json, "balance")
            Decimal.TryParse(balanceStr, info.Balance)

            Dim unconfStr As String = ParseNumericField(json, "unconfirmed_balance")
            Decimal.TryParse(unconfStr, info.UnconfirmedBalance)

            Dim immatureStr As String = ParseNumericField(json, "immature_balance")
            Decimal.TryParse(immatureStr, info.ImmatureBalance)

            Return info
        End Function

        ''' <summary>
        ''' Returns a string representation of the wallet info.
        ''' </summary>
        Public Overrides Function ToString() As String
            Return $"Wallet '{WalletName}': Balance={Balance:F8} CRC, Txs={TransactionCount}"
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

        Private Shared Function ParseNumericField(json As String, key As String) As String
            Dim searchKey As String = $"""{key}"":"
            Dim idx As Integer = json.IndexOf(searchKey, StringComparison.Ordinal)
            If idx < 0 Then Return "0"
            Dim start As Integer = idx + searchKey.Length
            While start < json.Length AndAlso Char.IsWhiteSpace(json(start))
                start += 1
            End While
            Dim endIdx As Integer = start
            While endIdx < json.Length AndAlso (Char.IsDigit(json(endIdx)) OrElse
                  json(endIdx) = "."c OrElse json(endIdx) = "-"c)
                endIdx += 1
            End While
            Return json.Substring(start, endIdx - start)
        End Function

    End Class

End Namespace
