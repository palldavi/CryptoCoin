' ===============================================================================
' CryptoCoin.Sdk - Models\TransactionInfo.vb
' Transaction information model returned by SDK client methods.
' ===============================================================================

Imports System
Imports System.Collections.Generic

Namespace CryptoCoin.Sdk.Models

    ''' <summary>
    ''' Represents detailed information about a transaction in the CryptoCoin blockchain.
    ''' Returned by the SDK client's GetTransaction method.
    ''' </summary>
    Public Class TransactionInfo

        ''' <summary>Gets or sets the transaction ID (hash) as a hex string.</summary>
        Public Property TxId As String

        ''' <summary>Gets or sets the transaction version.</summary>
        Public Property Version As Integer

        ''' <summary>Gets or sets the number of inputs.</summary>
        Public Property InputCount As Integer

        ''' <summary>Gets or sets the number of outputs.</summary>
        Public Property OutputCount As Integer

        ''' <summary>Gets or sets the transaction lock time.</summary>
        Public Property LockTime As UInteger

        ''' <summary>Gets or sets the block hash containing this transaction.</summary>
        Public Property BlockHash As String

        ''' <summary>Gets or sets the block height containing this transaction.</summary>
        Public Property BlockHeight As Integer

        ''' <summary>Gets or sets the number of confirmations.</summary>
        Public Property Confirmations As Integer

        ''' <summary>Gets or sets the transaction timestamp.</summary>
        Public Property Timestamp As Long

        ''' <summary>Gets or sets the transaction size in bytes.</summary>
        Public Property Size As Integer

        ''' <summary>Gets or sets the transaction fee in satoshis.</summary>
        Public Property Fee As Long

        ''' <summary>Gets or sets the list of transaction inputs.</summary>
        Public Property Inputs As List(Of TxInput)

        ''' <summary>Gets or sets the list of transaction outputs.</summary>
        Public Property Outputs As List(Of TxOutput)

        ''' <summary>Gets whether this is a coinbase transaction.</summary>
        Public ReadOnly Property IsCoinbase As Boolean
            Get
                Return Inputs IsNot Nothing AndAlso Inputs.Count = 1 AndAlso
                       Inputs(0).PreviousTxId = "0000000000000000000000000000000000000000000000000000000000000000"
            End Get
        End Property

        ''' <summary>
        ''' Initializes a new empty TransactionInfo instance.
        ''' </summary>
        Public Sub New()
            Inputs = New List(Of TxInput)()
            Outputs = New List(Of TxOutput)()
        End Sub

        ''' <summary>
        ''' Parses a TransactionInfo from a JSON string response.
        ''' </summary>
        ''' <param name="json">The JSON string to parse.</param>
        ''' <returns>A populated TransactionInfo instance.</returns>
        Public Shared Function FromJson(json As String) As TransactionInfo
            Dim info As New TransactionInfo()

            If String.IsNullOrEmpty(json) Then Return info

            info.TxId = ParseStringField(json, "txid")
            info.Version = ParseIntField(json, "version")
            info.InputCount = ParseIntField(json, "vin_count")
            info.OutputCount = ParseIntField(json, "vout_count")
            info.LockTime = CUInt(ParseIntField(json, "locktime"))
            info.BlockHash = ParseStringField(json, "blockhash")
            info.BlockHeight = ParseIntField(json, "blockheight")
            info.Confirmations = ParseIntField(json, "confirmations")
            info.Timestamp = CLng(ParseIntField(json, "time"))
            info.Size = ParseIntField(json, "size")
            info.Fee = CLng(ParseIntField(json, "fee"))

            Return info
        End Function

        ''' <summary>
        ''' Gets the total output value of this transaction.
        ''' </summary>
        Public Function GetTotalOutputValue() As Long
            Dim total As Long = 0
            If Outputs IsNot Nothing Then
                For Each output As Object In Outputs
                    total += output.Value
                Next
            End If
            Return total
        End Function

        ''' <summary>
        ''' Returns a string representation of this transaction info.
        ''' </summary>
        Public Overrides Function ToString() As String
            Return $"Tx {TxId?.Substring(0, 16)}... ({InputCount} in, {OutputCount} out)"
        End Function

        ' --- JSON parsing helpers ---

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

    ''' <summary>
    ''' Represents a transaction input in the SDK model.
    ''' </summary>
    Public Class TxInput
        ''' <summary>Gets or sets the previous transaction ID being spent.</summary>
        Public Property PreviousTxId As String

        ''' <summary>Gets or sets the output index being spent.</summary>
        Public Property OutputIndex As Integer

        ''' <summary>Gets or sets the signature script hex.</summary>
        Public Property ScriptSig As String

        ''' <summary>Gets or sets the sequence number.</summary>
        Public Property Sequence As UInteger
    End Class

    ''' <summary>
    ''' Represents a transaction output in the SDK model.
    ''' </summary>
    Public Class TxOutput
        ''' <summary>Gets or sets the output value in satoshis.</summary>
        Public Property Value As Long

        ''' <summary>Gets or sets the output index.</summary>
        Public Property Index As Integer

        ''' <summary>Gets or sets the script public key hex.</summary>
        Public Property ScriptPubKey As String

        ''' <summary>Gets or sets the destination address (if decodable).</summary>
        Public Property Address As String
    End Class

End Namespace
