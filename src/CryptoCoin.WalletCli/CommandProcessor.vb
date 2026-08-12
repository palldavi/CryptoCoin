' ===============================================================================
' CryptoCoin.WalletCli - CommandProcessor.vb
' Processes CLI commands by routing to the appropriate command handler.
' Manages wallet initialization and provides shared context to commands.
' ===============================================================================

Imports System
Imports System.IO
Imports CryptoCoin.Wallet
Imports CryptoCoin.Cryptography
Imports CryptoCoin.TransactionIds

Namespace CryptoCoin.WalletCli

    ''' <summary>
    ''' Processes command-line commands by routing them to the appropriate handler.
    ''' Manages wallet state and provides shared context for all command operations.
    ''' </summary>
    Public Class CommandProcessor

        Private ReadOnly _walletPath As String
        Private _walletManager As WalletManager
        Private ReadOnly _commands As Dictionary(Of String, ICommand)

        ''' <summary>
        ''' Initializes a new instance of the CommandProcessor with the specified wallet path.
        ''' </summary>
        ''' <param name="walletPath">The file path to the wallet data file.</param>
        Public Sub New(walletPath As String)
            _walletPath = walletPath
            _commands = New Dictionary(Of String, ICommand)(StringComparer.OrdinalIgnoreCase)
            RegisterCommands()
        End Sub

        ''' <summary>
        ''' Registers all available commands in the command dictionary.
        ''' </summary>
        Private Sub RegisterCommands()
            _commands.Add("create-wallet", New Commands.CreateWalletCommand())
            _commands.Add("send", New Commands.SendCommand())
            _commands.Add("balance", New Commands.BalanceCommand())
            _commands.Add("history", New Commands.HistoryCommand())
            _commands.Add("address", New Commands.AddressCommand())
            _commands.Add("receive", New Commands.AddressCommand())
            _commands.Add("backup", New Commands.BackupCommand())
        End Sub

        ''' <summary>
        ''' Executes the specified command with the given arguments.
        ''' </summary>
        ''' <param name="commandName">The name of the command to execute.</param>
        ''' <param name="args">The arguments to pass to the command.</param>
        ''' <returns>Exit code: 0 for success, non-zero for failure.</returns>
        Public Function Execute(commandName As String, args As String()) As Integer
            ' Check if the command exists
            If Not _commands.ContainsKey(commandName) Then
                ConsoleUI.WriteError($"Unknown command: '{commandName}'")
                ConsoleUI.WriteInfo("Use '--help' to see available commands.")
                Return 1
            End If

            Dim command As ICommand = _commands(commandName)

            ' Check for help flag on the command
            If args IsNot Nothing AndAlso args.Any(Function(a) a = "--help" OrElse a = "-h") Then
                command.ShowHelp()
                Return 0
            End If

            ' Initialize wallet if needed (skip for create-wallet)
            If commandName <> "create-wallet" AndAlso commandName <> "restore" Then
                If Not InitializeWallet() Then
                    Return 1
                End If
            End If

            ' Create execution context
            Dim context As New CommandContext() With {
                .WalletPath = _walletPath,
                .WalletManager = _walletManager,
                .Arguments = args
            }

            ' Execute the command
            Return command.Execute(context)
        End Function

        ''' <summary>
        ''' Initializes the wallet manager by loading the wallet from disk.
        ''' </summary>
        ''' <returns>True if the wallet was loaded successfully; otherwise, False.</returns>
        Private Function InitializeWallet() As Boolean
            If Not File.Exists(_walletPath) Then
                ConsoleUI.WriteError("No wallet found at the specified path.")
                ConsoleUI.WriteInfo($"Path: {_walletPath}")
                ConsoleUI.WriteInfo("Use 'create-wallet' to create a new wallet.")
                Return False
            End If

            Try
                ConsoleUI.WriteProgress("Loading wallet...")
                _walletManager = WalletManager.Load(_walletPath, WalletConfig.CreateDefault())
                Return True

            Catch ex As Exception
                ConsoleUI.WriteError($"Failed to load wallet: {ex.Message}")
                Return False
            End Try
        End Function

        ''' <summary>
        ''' Prompts the user for their wallet password securely.
        ''' </summary>
        ''' <returns>The entered password string.</returns>
        Public Shared Function PromptPassword(prompt As String) As String
            Console.Write(prompt)
            Dim password As String = String.Empty

            Dim key As ConsoleKeyInfo
            Do
                key = Console.ReadKey(intercept:=True)
                If key.Key = ConsoleKey.Backspace AndAlso password.Length > 0 Then
                    password = password.Substring(0, password.Length - 1)
                    Console.Write(vbBack & " " & vbBack)
                ElseIf key.Key <> ConsoleKey.Enter AndAlso key.Key <> ConsoleKey.Backspace Then
                    password &= key.KeyChar
                    Console.Write("*")
                End If
            Loop While key.Key <> ConsoleKey.Enter

            Console.WriteLine()
            Return password
        End Function

        ''' <summary>
        ''' Prompts the user for confirmation with a yes/no question.
        ''' </summary>
        ''' <param name="message">The confirmation message to display.</param>
        ''' <returns>True if the user confirmed; otherwise, False.</returns>
        Public Shared Function PromptConfirmation(message As String) As Boolean
            Console.Write($"{message} [y/N]: ")
            Dim response As String = Console.ReadLine()
            Return response IsNot Nothing AndAlso
                   (response.Trim().ToLowerInvariant() = "y" OrElse
                    response.Trim().ToLowerInvariant() = "yes")
        End Function

        ''' <summary>
        ''' Parses a named argument value from the argument array.
        ''' </summary>
        ''' <param name="args">The argument array to search.</param>
        ''' <param name="name">The argument name (e.g., "--amount").</param>
        ''' <returns>The argument value, or Nothing if not found.</returns>
        Public Shared Function ParseArgument(args As String(), name As String) As String
            If args Is Nothing Then Return Nothing

            For i As Integer = 0 To args.Length - 2
                If args(i).Equals(name, StringComparison.OrdinalIgnoreCase) Then
                    Return args(i + 1)
                End If
            Next

            Return Nothing
        End Function

        ''' <summary>
        ''' Parses a flag argument from the argument array.
        ''' </summary>
        ''' <param name="args">The argument array to search.</param>
        ''' <param name="name">The flag name (e.g., "--verbose").</param>
        ''' <returns>True if the flag is present; otherwise, False.</returns>
        Public Shared Function ParseFlag(args As String(), name As String) As Boolean
            If args Is Nothing Then Return False
            Return args.Any(Function(a) a.Equals(name, StringComparison.OrdinalIgnoreCase))
        End Function

        ''' <summary>
        ''' Formats a CRC amount for display with proper decimal places.
        ''' </summary>
        ''' <param name="satoshis">The amount in satoshis (smallest unit).</param>
        ''' <returns>A formatted string representing the CRC amount.</returns>
        Public Shared Function FormatAmount(satoshis As Long) As String
            Dim crc As Decimal = CDec(satoshis) / 100000000D
            Return $"{crc:F8} CRC"
        End Function

        ''' <summary>
        ''' Parses a CRC amount string to satoshis.
        ''' </summary>
        ''' <param name="amountStr">The amount string (e.g., "1.5").</param>
        ''' <returns>The amount in satoshis, or -1 if parsing failed.</returns>
        Public Shared Function ParseAmount(amountStr As String) As Long
            Dim amount As Decimal
            If Not Decimal.TryParse(amountStr, amount) Then
                Return -1
            End If

            If amount <= 0 Then
                Return -1
            End If

            Return CLng(amount * 100000000D)
        End Function

        ''' <summary>
        ''' Validates a CryptoCoin address format.
        ''' </summary>
        ''' <param name="address">The address string to validate.</param>
        ''' <returns>True if the address format is valid; otherwise, False.</returns>
        Public Shared Function ValidateAddress(address As String) As Boolean
            If String.IsNullOrWhiteSpace(address) Then Return False
            If address.Length < 26 OrElse address.Length > 62 Then Return False

            ' Check for valid prefix (CRC mainnet addresses start with 'C' or 'c')
            If Not address.StartsWith("C") AndAlso Not address.StartsWith("c") Then
                Return False
            End If

            Try
                ' Attempt Base58Check decode for validation
                Dim decoded As Byte() = Base58Encoder.DecodeCheck(address)
                Return decoded IsNot Nothing AndAlso decoded.Length > 0
            Catch
                Return False
            End Try
        End Function

    End Class

    ''' <summary>
    ''' Interface for all CLI commands.
    ''' </summary>
    Public Interface ICommand
        ''' <summary>
        ''' Executes the command with the given context.
        ''' </summary>
        ''' <param name="context">The command execution context.</param>
        ''' <returns>Exit code: 0 for success, non-zero for failure.</returns>
        Function Execute(context As CommandContext) As Integer

        ''' <summary>
        ''' Displays help information for this command.
        ''' </summary>
        Sub ShowHelp()
    End Interface

    ''' <summary>
    ''' Provides shared context for command execution including wallet state and arguments.
    ''' </summary>
    Public Class CommandContext
        ''' <summary>Gets or sets the path to the wallet file.</summary>
        Public Property WalletPath As String

        ''' <summary>Gets or sets the wallet manager instance.</summary>
        Public Property WalletManager As WalletManager

        ''' <summary>Gets or sets the command arguments.</summary>
        Public Property Arguments As String()
    End Class

End Namespace
