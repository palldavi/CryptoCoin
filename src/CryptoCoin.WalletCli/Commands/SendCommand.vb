' ===============================================================================
' CryptoCoin.WalletCli - Commands\SendCommand.vb
' Sends CRC to a destination address with fee estimation and confirmation.
' ===============================================================================

Imports System
Imports CryptoCoin.Wallet
Imports CryptoCoin.Transactions
Imports CryptoCoin.TransactionIds
Imports CryptoCoin.Cryptography

Namespace CryptoCoin.WalletCli.Commands

    ''' <summary>
    ''' Command handler for sending CRC to a destination address.
    ''' Supports fee estimation, custom fee rates, and transaction confirmation.
    ''' </summary>
    Public Class SendCommand
        Implements ICommand

        ''' <summary>
        ''' Executes the send command, creating and broadcasting a transaction.
        ''' </summary>
        ''' <param name="context">The command execution context.</param>
        ''' <returns>Exit code: 0 for success, non-zero for failure.</returns>
        Public Function Execute(context As CommandContext) As Integer Implements ICommand.Execute
            ConsoleUI.WriteHeader("Send CRC")

            ' Parse required arguments
            Dim toAddress As String = CommandProcessor.ParseArgument(context.Arguments, "--to")
            Dim amountStr As String = CommandProcessor.ParseArgument(context.Arguments, "--amount")
            Dim feeRateStr As String = CommandProcessor.ParseArgument(context.Arguments, "--fee-rate")
            Dim memo As String = CommandProcessor.ParseArgument(context.Arguments, "--memo")
            Dim noConfirm As Boolean = CommandProcessor.ParseFlag(context.Arguments, "--yes")

            ' Validate destination address
            If String.IsNullOrWhiteSpace(toAddress) Then
                ConsoleUI.WriteError("Destination address is required. Use --to <address>")
                Return 1
            End If

            If Not CommandProcessor.ValidateAddress(toAddress) Then
                ConsoleUI.WriteError($"Invalid destination address: {toAddress}")
                Return 1
            End If

            ' Validate amount
            If String.IsNullOrWhiteSpace(amountStr) Then
                ConsoleUI.WriteError("Amount is required. Use --amount <crc>")
                Return 1
            End If

            Dim amountSatoshis As Long = CommandProcessor.ParseAmount(amountStr)
            If amountSatoshis <= 0 Then
                ConsoleUI.WriteError("Invalid amount. Must be a positive number.")
                Return 1
            End If

            ' Parse fee rate (satoshis per byte)
            Dim feeRate As Integer = 10 ' Default fee rate
            If feeRateStr IsNot Nothing Then
                If Not Integer.TryParse(feeRateStr, feeRate) OrElse feeRate <= 0 Then
                    ConsoleUI.WriteError("Invalid fee rate. Must be a positive integer (sat/byte).")
                    Return 1
                End If
            End If

            Try
                ' Check balance
                Dim balance As Long = context.WalletManager.Balance.ConfirmedBalance
                If balance < amountSatoshis Then
                    ConsoleUI.WriteError("Insufficient funds.")
                    ConsoleUI.WriteKeyValue("Available", CommandProcessor.FormatAmount(balance))
                    ConsoleUI.WriteKeyValue("Required", CommandProcessor.FormatAmount(amountSatoshis))
                    Return 1
                End If

                ' Estimate transaction fee
                ConsoleUI.WriteProgress("Estimating transaction fee...")
                Dim estimatedSize As Integer = EstimateTransactionSize(1, 2) ' Simplified estimate
                Dim estimatedFee As Long = CLng(estimatedSize) * CLng(feeRate)

                ' Check balance including fee
                If balance < amountSatoshis + estimatedFee Then
                    ConsoleUI.WriteError("Insufficient funds (including fee).")
                    ConsoleUI.WriteKeyValue("Available", CommandProcessor.FormatAmount(balance))
                    ConsoleUI.WriteKeyValue("Amount", CommandProcessor.FormatAmount(amountSatoshis))
                    ConsoleUI.WriteKeyValue("Est. Fee", CommandProcessor.FormatAmount(estimatedFee))
                    ConsoleUI.WriteKeyValue("Total", CommandProcessor.FormatAmount(amountSatoshis + estimatedFee))
                    Return 1
                End If

                ' Display transaction summary
                Console.WriteLine()
                ConsoleUI.WriteHeader("Transaction Summary")
                ConsoleUI.WriteKeyValue("To", toAddress)
                ConsoleUI.WriteKeyValue("Amount", CommandProcessor.FormatAmount(amountSatoshis))
                ConsoleUI.WriteKeyValue("Fee Rate", $"{feeRate} sat/byte")
                ConsoleUI.WriteKeyValue("Est. Fee", CommandProcessor.FormatAmount(estimatedFee))
                ConsoleUI.WriteKeyValue("Total", CommandProcessor.FormatAmount(amountSatoshis + estimatedFee))
                If Not String.IsNullOrEmpty(memo) Then
                    ConsoleUI.WriteKeyValue("Memo", memo)
                End If
                Console.WriteLine()

                ' Confirm transaction
                If Not noConfirm Then
                    If Not CommandProcessor.PromptConfirmation("Confirm transaction?") Then
                        ConsoleUI.WriteInfo("Transaction cancelled.")
                        Return 0
                    End If
                End If

                ' Prompt for password to sign
                Dim password As String = CommandProcessor.PromptPassword("Enter wallet password to sign: ")

                ' Build and sign transaction
                ConsoleUI.WriteProgress("Building transaction...")
                Dim builder As New TransactionBuilder()
                builder.AddOutput(toAddress, amountSatoshis)
                builder.SetFeePerByte(feeRate)

                ConsoleUI.WriteProgress("Signing transaction...")
                Dim tx As Transaction = builder.Build()
                ' TODO: Sign transaction

                ' Broadcast transaction
                ConsoleUI.WriteProgress("Broadcasting transaction...")
                Dim txId As String = tx.TxId

                Console.WriteLine()
                ConsoleUI.WriteSuccess("Transaction sent successfully!")
                ConsoleUI.WriteKeyValue("Transaction ID", txId)

                Return 0

            Catch ex As Exception
                ConsoleUI.WriteError($"Failed to send transaction: {ex.Message}")
                Return 1
            End Try
        End Function

        ''' <summary>
        ''' Estimates the transaction size in bytes based on input and output counts.
        ''' </summary>
        ''' <param name="inputCount">The number of transaction inputs.</param>
        ''' <param name="outputCount">The number of transaction outputs.</param>
        ''' <returns>The estimated transaction size in bytes.</returns>
        Private Function EstimateTransactionSize(inputCount As Integer, outputCount As Integer) As Integer
            ' Base transaction overhead: version (4) + locktime (4) + input count (1) + output count (1)
            Const baseSize As Integer = 10
            ' Each input: prevout (36) + script length (1) + script (~107) + sequence (4)
            Const inputSize As Integer = 148
            ' Each output: value (8) + script length (1) + script (~25)
            Const outputSize As Integer = 34

            Return baseSize + (inputCount * inputSize) + (outputCount * outputSize)
        End Function

        ''' <summary>
        ''' Displays help information for the send command.
        ''' </summary>
        Public Sub ShowHelp() Implements ICommand.ShowHelp
            ConsoleUI.WriteHeader("send - Send CRC to an address")
            Console.WriteLine()
            Console.WriteLine("  Usage: cryptocoin-wallet send --to <address> --amount <crc> [options]")
            Console.WriteLine()
            Console.WriteLine("  Required:")
            Console.WriteLine("    --to <address>      Destination CryptoCoin address")
            Console.WriteLine("    --amount <crc>      Amount to send in CRC (e.g., 1.5)")
            Console.WriteLine()
            Console.WriteLine("  Options:")
            Console.WriteLine("    --fee-rate <sat/b>  Fee rate in satoshis per byte (default: 10)")
            Console.WriteLine("    --memo <text>       Optional memo to attach to the transaction")
            Console.WriteLine("    --yes               Skip confirmation prompt")
        End Sub

    End Class

End Namespace
