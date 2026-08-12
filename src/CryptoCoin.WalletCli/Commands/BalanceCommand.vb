' ===============================================================================
' CryptoCoin.WalletCli - Commands\BalanceCommand.vb
' Displays wallet balance including confirmed and unconfirmed amounts.
' ===============================================================================

Imports System
Imports CryptoCoin.Wallet

Namespace CryptoCoin.WalletCli.Commands

    ''' <summary>
    ''' Command handler for displaying the wallet balance.
    ''' Shows confirmed, unconfirmed, and total balance amounts.
    ''' </summary>
    Public Class BalanceCommand
        Implements ICommand

        ''' <summary>
        ''' Executes the balance command, displaying current wallet balances.
        ''' </summary>
        ''' <param name="context">The command execution context.</param>
        ''' <returns>Exit code: 0 for success, non-zero for failure.</returns>
        Public Function Execute(context As CommandContext) As Integer Implements ICommand.Execute
            ConsoleUI.WriteHeader("Wallet Balance")

            Try
                Dim confirmed As Long = context.WalletManager.Balance.ConfirmedBalance
                Dim unconfirmed As Long = context.WalletManager.Balance.UnconfirmedBalance
                Dim total As Long = confirmed + unconfirmed

                Console.WriteLine()
                ConsoleUI.WriteKeyValue("Confirmed", CommandProcessor.FormatAmount(confirmed), 16)
                ConsoleUI.WriteKeyValue("Unconfirmed", CommandProcessor.FormatAmount(unconfirmed), 16)
                ConsoleUI.WriteSeparator(40)
                ConsoleUI.WriteKeyValue("Total", CommandProcessor.FormatAmount(total), 16)
                Console.WriteLine()

                ' Show verbose details if requested
                If CommandProcessor.ParseFlag(context.Arguments, "--verbose") OrElse
                   CommandProcessor.ParseFlag(context.Arguments, "-v") Then
                    ShowDetailedBalance(context)
                End If

                Return 0

            Catch ex As Exception
                ConsoleUI.WriteError($"Failed to retrieve balance: {ex.Message}")
                Return 1
            End Try
        End Function

        ''' <summary>
        ''' Shows detailed balance information including per-address breakdown.
        ''' </summary>
        ''' <param name="context">The command execution context.</param>
        Private Sub ShowDetailedBalance(context As CommandContext)
            ConsoleUI.WriteHeader("Address Breakdown")

            Dim accounts = context.WalletManager.GetAllAccounts()
            Dim headers As String() = {"Address", "Balance", "Tx Count"}
            Dim rows As New List(Of String())

            For Each account As Object In accounts
                rows.Add(New String() {
                    TruncateAddress(account.Address),
                    CommandProcessor.FormatAmount(account.Balance),
                    account.TransactionCount.ToString()
                })
            Next

            If rows.Count > 0 Then
                ConsoleUI.WriteTable(headers, rows)
            Else
                ConsoleUI.WriteInfo("No addresses with balance found.")
            End If
        End Sub

        ''' <summary>
        ''' Truncates a long address for display purposes.
        ''' </summary>
        ''' <param name="address">The full address string.</param>
        ''' <returns>A truncated address showing the first and last characters.</returns>
        Private Function TruncateAddress(address As String) As String
            If address.Length <= 20 Then Return address
            Return address.Substring(0, 10) & "..." & address.Substring(address.Length - 8)
        End Function

        ''' <summary>
        ''' Displays help information for the balance command.
        ''' </summary>
        Public Sub ShowHelp() Implements ICommand.ShowHelp
            ConsoleUI.WriteHeader("balance - Display wallet balance")
            Console.WriteLine()
            Console.WriteLine("  Usage: cryptocoin-wallet balance [options]")
            Console.WriteLine()
            Console.WriteLine("  Options:")
            Console.WriteLine("    --verbose, -v   Show per-address balance breakdown")
        End Sub

    End Class

End Namespace
