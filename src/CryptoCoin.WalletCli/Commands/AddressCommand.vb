' ===============================================================================
' CryptoCoin.WalletCli - Commands\AddressCommand.vb
' Generates and displays receiving addresses for the wallet.
' ===============================================================================

Imports System
Imports CryptoCoin.Wallet
Imports CryptoCoin.Cryptography

Namespace CryptoCoin.WalletCli.Commands

    ''' <summary>
    ''' Command handler for generating and displaying wallet receiving addresses.
    ''' Supports generating new addresses and listing existing ones.
    ''' </summary>
    Public Class AddressCommand
        Implements ICommand

        ''' <summary>
        ''' Executes the address command, generating or listing wallet addresses.
        ''' </summary>
        ''' <param name="context">The command execution context.</param>
        ''' <returns>Exit code: 0 for success, non-zero for failure.</returns>
        Public Function Execute(context As CommandContext) As Integer Implements ICommand.Execute
            Dim generateNew As Boolean = CommandProcessor.ParseFlag(context.Arguments, "--new")
            Dim listAll As Boolean = CommandProcessor.ParseFlag(context.Arguments, "--list")
            Dim showQr As Boolean = CommandProcessor.ParseFlag(context.Arguments, "--qr")

            Try
                If listAll Then
                    Return ListAddresses(context)
                End If

                ' Default behavior: generate or show current receiving address
                Return ShowReceivingAddress(context, generateNew, showQr)

            Catch ex As Exception
                ConsoleUI.WriteError($"Failed to process address command: {ex.Message}")
                Return 1
            End Try
        End Function

        ''' <summary>
        ''' Shows the current receiving address or generates a new one.
        ''' </summary>
        Private Function ShowReceivingAddress(context As CommandContext, generateNew As Boolean, showQr As Boolean) As Integer
            ConsoleUI.WriteHeader("Receiving Address")

            Dim address As String
            If generateNew Then
                ConsoleUI.WriteProgress("Generating new receiving address...")
                address = context.WalletManager.GetReceivingAddress()
                ConsoleUI.WriteSuccess("New address generated!")
            Else
                address = context.WalletManager.GetReceivingAddress()
            End If

            Console.WriteLine()
            ConsoleUI.WriteKeyValue("Address", address)
            Console.WriteLine()

            If showQr Then
                DisplayAsciiQr(address)
            End If

            ConsoleUI.WriteInfo("Share this address to receive CRC payments.")
            ConsoleUI.WriteWarning("Each address should ideally be used only once for privacy.")

            Return 0
        End Function

        ''' <summary>
        ''' Lists all wallet addresses with their balances.
        ''' </summary>
        Private Function ListAddresses(context As CommandContext) As Integer
            ConsoleUI.WriteHeader("Wallet Addresses")

            Dim accounts = context.WalletManager.GetAllAccounts()
            Dim headers As String() = {"#", "Address", "Balance", "Used"}
            Dim rows As New List(Of String())
            Dim index As Integer = 1

            For Each account As Object In accounts
                rows.Add(New String() {
                    index.ToString(),
                    account.Address,
                    CommandProcessor.FormatAmount(account.Balance),
                    If(account.TransactionCount > 0, "Yes", "No")
                })
                index += 1
            Next

            If rows.Count > 0 Then
                ConsoleUI.WriteTable(headers, rows)
            Else
                ConsoleUI.WriteInfo("No addresses found. Use --new to generate one.")
            End If

            Return 0
        End Function

        ''' <summary>
        ''' Displays a simplified ASCII representation of the address for visual verification.
        ''' </summary>
        ''' <param name="address">The address to display.</param>
        Private Sub DisplayAsciiQr(address As String)
            ' Simple ASCII art border around the address for visual emphasis
            Dim border As String = New String("*"c, address.Length + 6)
            Console.WriteLine($"  {border}")
            Console.WriteLine($"  *  {address}  *")
            Console.WriteLine($"  {border}")
            Console.WriteLine()
        End Sub

        ''' <summary>
        ''' Displays help information for the address command.
        ''' </summary>
        Public Sub ShowHelp() Implements ICommand.ShowHelp
            ConsoleUI.WriteHeader("address - Manage receiving addresses")
            Console.WriteLine()
            Console.WriteLine("  Usage: cryptocoin-wallet address [options]")
            Console.WriteLine()
            Console.WriteLine("  Options:")
            Console.WriteLine("    --new     Generate a new receiving address")
            Console.WriteLine("    --list    List all wallet addresses")
            Console.WriteLine("    --qr      Display address with visual emphasis")
        End Sub

    End Class

End Namespace
