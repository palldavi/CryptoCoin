' ===============================================================================
' CryptoCoin.Sdk - Builders\TransactionRequestBuilder.vb
' Fluent builder for creating and submitting transaction requests.
' ===============================================================================

Imports System
Imports System.Collections.Generic
Imports System.Text
Imports CryptoCoin.Sdk.Exceptions

Namespace CryptoCoin.Sdk.Builders

    ''' <summary>
    ''' Fluent builder for constructing and submitting CryptoCoin transaction requests.
    ''' Provides a chainable API for specifying inputs, outputs, fees, and metadata.
    ''' </summary>
    ''' <example>
    ''' Dim txId = client.CreateTransaction() _
    '''     .From("source_address") _
    '''     .To("dest_address", 1.5D) _
    '''     .WithFeeRate(10) _
    '''     .WithMemo("Payment for services") _
    '''     .Send()
    ''' </example>
    Public Class TransactionRequestBuilder

        Private ReadOnly _client As CryptoCoinClient
        Private ReadOnly _recipients As New List(Of RecipientEntry)()
        Private ReadOnly _inputs As New List(Of InputEntry)()
        Private _fromAddress As String
        Private _feeRate As Integer = 10
        Private _absoluteFee As Long = -1
        Private _memo As String
        Private _changeAddress As String
        Private _lockTime As UInteger = 0
        Private _subtractFeeFromAmount As Boolean = False
        Private _dryRun As Boolean = False

        ''' <summary>
        ''' Initializes a new TransactionRequestBuilder with the specified client.
        ''' </summary>
        ''' <param name="client">The CryptoCoinClient to use for submission.</param>
        Public Sub New(client As CryptoCoinClient)
            _client = client
        End Sub

        ''' <summary>
        ''' Specifies the source address for the transaction.
        ''' </summary>
        ''' <param name="address">The source address.</param>
        ''' <returns>This builder instance for method chaining.</returns>
        Public Function From(address As String) As TransactionRequestBuilder
            _fromAddress = address
            Return Me
        End Function

        ''' <summary>
        ''' Adds a recipient with the specified address and amount.
        ''' </summary>
        ''' <param name="address">The destination address.</param>
        ''' <param name="amountCrc">The amount in CRC.</param>
        ''' <returns>This builder instance for method chaining.</returns>
        Public Function [To](address As String, amountCrc As Decimal) As TransactionRequestBuilder
            If String.IsNullOrWhiteSpace(address) Then
                Throw New ArgumentException("Recipient address cannot be empty.", NameOf(address))
            End If
            If amountCrc <= 0 Then
                Throw New ArgumentException("Amount must be positive.", NameOf(amountCrc))
            End If

            _recipients.Add(New RecipientEntry() With {
                .Address = address,
                .AmountSatoshis = CLng(amountCrc * 100000000D)
            })
            Return Me
        End Function

        ''' <summary>
        ''' Adds a recipient with the specified address and amount in satoshis.
        ''' </summary>
        ''' <param name="address">The destination address.</param>
        ''' <param name="amountSatoshis">The amount in satoshis.</param>
        ''' <returns>This builder instance for method chaining.</returns>
        Public Function ToSatoshis(address As String, amountSatoshis As Long) As TransactionRequestBuilder
            If String.IsNullOrWhiteSpace(address) Then
                Throw New ArgumentException("Recipient address cannot be empty.", NameOf(address))
            End If
            If amountSatoshis <= 0 Then
                Throw New ArgumentException("Amount must be positive.", NameOf(amountSatoshis))
            End If

            _recipients.Add(New RecipientEntry() With {
                .Address = address,
                .AmountSatoshis = amountSatoshis
            })
            Return Me
        End Function

        ''' <summary>
        ''' Adds a specific UTXO as an input to the transaction.
        ''' </summary>
        ''' <param name="txId">The transaction ID containing the UTXO.</param>
        ''' <param name="outputIndex">The output index within the transaction.</param>
        ''' <returns>This builder instance for method chaining.</returns>
        Public Function AddInput(txId As String, outputIndex As Integer) As TransactionRequestBuilder
            _inputs.Add(New InputEntry() With {
                .TxId = txId,
                .OutputIndex = outputIndex
            })
            Return Me
        End Function

        ''' <summary>
        ''' Sets the fee rate in satoshis per byte.
        ''' </summary>
        ''' <param name="satoshisPerByte">The fee rate.</param>
        ''' <returns>This builder instance for method chaining.</returns>
        Public Function WithFeeRate(satoshisPerByte As Integer) As TransactionRequestBuilder
            If satoshisPerByte <= 0 Then
                Throw New ArgumentException("Fee rate must be positive.", NameOf(satoshisPerByte))
            End If
            _feeRate = satoshisPerByte
            _absoluteFee = -1
            Return Me
        End Function

        ''' <summary>
        ''' Sets an absolute fee amount in satoshis.
        ''' </summary>
        ''' <param name="feeSatoshis">The absolute fee in satoshis.</param>
        ''' <returns>This builder instance for method chaining.</returns>
        Public Function WithAbsoluteFee(feeSatoshis As Long) As TransactionRequestBuilder
            If feeSatoshis < 0 Then
                Throw New ArgumentException("Fee cannot be negative.", NameOf(feeSatoshis))
            End If
            _absoluteFee = feeSatoshis
            Return Me
        End Function

        ''' <summary>
        ''' Attaches a memo/note to the transaction.
        ''' </summary>
        ''' <param name="memo">The memo text.</param>
        ''' <returns>This builder instance for method chaining.</returns>
        Public Function WithMemo(memo As String) As TransactionRequestBuilder
            _memo = memo
            Return Me
        End Function

        ''' <summary>
        ''' Sets the change address for any remaining funds.
        ''' </summary>
        ''' <param name="address">The change address.</param>
        ''' <returns>This builder instance for method chaining.</returns>
        Public Function WithChangeAddress(address As String) As TransactionRequestBuilder
            _changeAddress = address
            Return Me
        End Function

        ''' <summary>
        ''' Sets the transaction lock time.
        ''' </summary>
        ''' <param name="lockTime">The lock time value.</param>
        ''' <returns>This builder instance for method chaining.</returns>
        Public Function WithLockTime(lockTime As UInteger) As TransactionRequestBuilder
            _lockTime = lockTime
            Return Me
        End Function

        ''' <summary>
        ''' Indicates that the fee should be subtracted from the send amount.
        ''' </summary>
        ''' <returns>This builder instance for method chaining.</returns>
        Public Function SubtractFeeFromAmount() As TransactionRequestBuilder
            _subtractFeeFromAmount = True
            Return Me
        End Function

        ''' <summary>
        ''' Enables dry-run mode (validates without broadcasting).
        ''' </summary>
        ''' <returns>This builder instance for method chaining.</returns>
        Public Function AsDryRun() As TransactionRequestBuilder
            _dryRun = True
            Return Me
        End Function

        ''' <summary>
        ''' Builds and submits the transaction to the network.
        ''' </summary>
        ''' <returns>The transaction ID if successful.</returns>
        ''' <exception cref="InvalidOperationException">Thrown if the builder state is invalid.</exception>
        ''' <exception cref="RpcException">Thrown if the transaction is rejected.</exception>
        Public Function Send() As String
            Validate()

            ' Build the raw transaction via RPC
            Dim rawTxHex As String = BuildRawTransaction()

            If _dryRun Then
                Return $"dry-run:{rawTxHex.Substring(0, Math.Min(16, rawTxHex.Length))}"
            End If

            ' Submit to network
            Return _client.SendRawTransaction(rawTxHex)
        End Function

        ''' <summary>
        ''' Validates the builder state before submission.
        ''' </summary>
        Private Sub Validate()
            If _recipients.Count = 0 Then
                Throw New InvalidOperationException("At least one recipient is required.")
            End If

            For Each recipient As Object In _recipients
                If String.IsNullOrWhiteSpace(recipient.Address) Then
                    Throw New InvalidOperationException("All recipients must have a valid address.")
                End If
                If recipient.AmountSatoshis <= 0 Then
                    Throw New InvalidOperationException("All recipients must have a positive amount.")
                End If
            Next
        End Sub

        ''' <summary>
        ''' Builds the raw transaction hex string.
        ''' </summary>
        Private Function BuildRawTransaction() As String
            ' In a full implementation, this would construct the transaction
            ' using the Transactions library and serialize it
            Dim sb As New StringBuilder()
            sb.Append("01000000") ' Version

            ' Inputs
            sb.Append(FormatVarInt(_inputs.Count))
            For Each input As Object In _inputs
                sb.Append(input.TxId)
                sb.Append(input.OutputIndex.ToString("x8"))
                sb.Append("00") ' Script length (unsigned)
                sb.Append("ffffffff") ' Sequence
            Next

            ' Outputs
            sb.Append(FormatVarInt(_recipients.Count))
            For Each recipient As Object In _recipients
                sb.Append(recipient.AmountSatoshis.ToString("x16"))
                sb.Append("1976a914") ' P2PKH script prefix
                sb.Append(New String("0"c, 40)) ' Placeholder for pubkey hash
                sb.Append("88ac") ' P2PKH script suffix
            Next

            ' Lock time
            sb.Append(_lockTime.ToString("x8"))

            Return sb.ToString()
        End Function

        Private Function FormatVarInt(value As Integer) As String
            If value < 253 Then
                Return value.ToString("x2")
            End If
            Return "fd" & value.ToString("x4")
        End Function

        ''' <summary>
        ''' Represents a transaction recipient entry.
        ''' </summary>
        Private Class RecipientEntry
            Public Property Address As String
            Public Property AmountSatoshis As Long
        End Class

        ''' <summary>
        ''' Represents a transaction input entry.
        ''' </summary>
        Private Class InputEntry
            Public Property TxId As String
            Public Property OutputIndex As Integer
        End Class

    End Class

End Namespace
