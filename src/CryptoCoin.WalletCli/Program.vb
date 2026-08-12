' ===============================================================================
' CryptoCoin.WalletCli - Program.vb
' Main entry point for the CryptoCoin wallet command-line interface.
' Parses command line arguments and dispatches to the appropriate command handler.
' ===============================================================================

Imports System
Imports System.Reflection

Namespace CryptoCoin.WalletCli

    ''' <summary>
    ''' Main entry point for the CryptoCoin Wallet CLI application.
    ''' Provides a command-line interface for managing wallets, sending and receiving CRC,
    ''' viewing balances, and managing transaction history.
    ''' </summary>
    Public Module Program

        Private Const AppName As String = "CryptoCoin Wallet CLI"
        Private Const AppVersion As String = "1.0.0"

        ''' <summary>
        ''' Application entry point. Parses command line arguments and dispatches commands.
        ''' </summary>
        ''' <param name="args">Command line arguments passed to the application.</param>
        Public Sub Main(args As String())
            Console.Title = $"{AppName} v{AppVersion}"

            Try
                If args Is Nothing OrElse args.Length = 0 Then
                    ShowUsage()
                    Exit Sub
                End If

                Dim command As String = args(0).ToLowerInvariant()
                Dim commandArgs As String() = If(args.Length > 1,
                    args.Skip(1).ToArray(),
                    Array.Empty(Of String)())

                ' Handle global flags
                If command = "--version" OrElse command = "-v" Then
                    ConsoleUI.WriteInfo($"{AppName} v{AppVersion}")
                    Exit Sub
                End If

                If command = "--help" OrElse command = "-h" Then
                    ShowUsage()
                    Exit Sub
                End If

                ' Parse optional wallet path from arguments
                Dim walletPath As String = GetWalletPath(commandArgs)

                ' Create and execute the command processor
                Dim processor As New CommandProcessor(walletPath)
                Dim exitCode As Integer = processor.Execute(command, commandArgs)

                Environment.ExitCode = exitCode

            Catch ex As UnauthorizedAccessException
                ConsoleUI.WriteError($"Access denied: {ex.Message}")
                Environment.ExitCode = 2

            Catch ex As Exception
                ConsoleUI.WriteError($"Unexpected error: {ex.Message}")
#If DEBUG Then
                ConsoleUI.WriteError(ex.StackTrace)
#End If
                Environment.ExitCode = 1
            End Try
        End Sub

        ''' <summary>
        ''' Extracts the wallet path from command arguments if specified with --wallet flag.
        ''' </summary>
        ''' <param name="args">The command arguments to search.</param>
        ''' <returns>The wallet file path, or the default path if not specified.</returns>
        Private Function GetWalletPath(args As String()) As String
            Dim defaultPath As String = IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "CryptoCoin", "wallet.dat")

            For i As Integer = 0 To args.Length - 2
                If args(i) = "--wallet" OrElse args(i) = "-w" Then
                    Return args(i + 1)
                End If
            Next

            Return defaultPath
        End Function

        ''' <summary>
        ''' Displays the application usage information and available commands.
        ''' </summary>
        Private Sub ShowUsage()
            ConsoleUI.WriteBanner(AppName, AppVersion)
            Console.WriteLine()
            ConsoleUI.WriteHeader("Usage")
            Console.WriteLine("  cryptocoin-wallet <command> [options]")
            Console.WriteLine()
            ConsoleUI.WriteHeader("Commands")
            Console.WriteLine("  create-wallet    Create a new wallet with a fresh mnemonic seed")
            Console.WriteLine("  restore          Restore a wallet from a mnemonic phrase")
            Console.WriteLine("  send             Send CRC to a destination address")
            Console.WriteLine("  receive          Generate a new receiving address")
            Console.WriteLine("  balance          Display wallet balance (confirmed/unconfirmed)")
            Console.WriteLine("  history          Show transaction history")
            Console.WriteLine("  address          List or generate wallet addresses")
            Console.WriteLine("  backup           Export wallet backup to file")
            Console.WriteLine("  import           Import wallet from backup file")
            Console.WriteLine()
            ConsoleUI.WriteHeader("Global Options")
            Console.WriteLine("  --wallet, -w     Path to wallet file (default: %APPDATA%\CryptoCoin\wallet.dat)")
            Console.WriteLine("  --version, -v    Show version information")
            Console.WriteLine("  --help, -h       Show this help message")
            Console.WriteLine()
            ConsoleUI.WriteInfo("Use 'cryptocoin-wallet <command> --help' for more information about a command.")
        End Sub

    End Module

End Namespace
