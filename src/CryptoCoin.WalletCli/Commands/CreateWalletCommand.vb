' ===============================================================================
' CryptoCoin.WalletCli - Commands\CreateWalletCommand.vb
' Creates a new wallet with mnemonic seed phrase display.
' ===============================================================================

Imports System
Imports System.IO
Imports CryptoCoin.Wallet
Imports CryptoCoin.Cryptography

Namespace CryptoCoin.WalletCli.Commands

    ''' <summary>
    ''' Command handler for creating a new CryptoCoin wallet.
    ''' Generates a new HD wallet with a BIP-39 mnemonic recovery phrase.
    ''' </summary>
    Public Class CreateWalletCommand
        Implements ICommand

        ''' <summary>
        ''' Executes the create-wallet command, generating a new wallet and displaying the mnemonic.
        ''' </summary>
        ''' <param name="context">The command execution context.</param>
        ''' <returns>Exit code: 0 for success, non-zero for failure.</returns>
        Public Function Execute(context As CommandContext) As Integer Implements ICommand.Execute
            ConsoleUI.WriteHeader("Create New Wallet")

            ' Check if wallet already exists
            If File.Exists(context.WalletPath) Then
                ConsoleUI.WriteWarning("A wallet already exists at the specified path.")
                ConsoleUI.WriteInfo($"Path: {context.WalletPath}")

                If Not CommandProcessor.PromptConfirmation("Overwrite existing wallet?") Then
                    ConsoleUI.WriteInfo("Operation cancelled.")
                    Return 0
                End If
            End If

            ' Prompt for password
            Dim password As String = CommandProcessor.PromptPassword("Enter wallet password: ")
            If String.IsNullOrEmpty(password) Then
                ConsoleUI.WriteError("Password cannot be empty.")
                Return 1
            End If

            Dim confirmPassword As String = CommandProcessor.PromptPassword("Confirm password: ")
            If password <> confirmPassword Then
                ConsoleUI.WriteError("Passwords do not match.")
                Return 1
            End If

            ' Determine mnemonic word count
            Dim wordCount As Integer = 24
            Dim wordCountArg As String = CommandProcessor.ParseArgument(context.Arguments, "--words")
            If wordCountArg IsNot Nothing Then
                If Not Integer.TryParse(wordCountArg, wordCount) OrElse
                   (wordCount <> 12 AndAlso wordCount <> 18 AndAlso wordCount <> 24) Then
                    ConsoleUI.WriteError("Word count must be 12, 18, or 24.")
                    Return 1
                End If
            End If

            Try
                ' Generate mnemonic
                ConsoleUI.WriteProgress("Generating secure mnemonic seed...")
                Dim mnemonic As New Mnemonic(wordCount)
                Dim words As String() = mnemonic.Words

                ' Display mnemonic with warning
                Console.WriteLine()
                ConsoleUI.WriteWarning("IMPORTANT: Write down the following recovery phrase!")
                ConsoleUI.WriteWarning("Store it in a safe place. You will need it to recover your wallet.")
                ConsoleUI.WriteWarning("Anyone with this phrase can access your funds.")

                ConsoleUI.WriteMnemonic(words)

                ' Confirm user has recorded the phrase
                ConsoleUI.WriteInfo("Please verify you have recorded the phrase correctly.")
                If Not CommandProcessor.PromptConfirmation("Have you safely stored your recovery phrase?") Then
                    ConsoleUI.WriteWarning("Wallet creation cancelled. Please try again when ready.")
                    Return 1
                End If

                ' Create the wallet
                ConsoleUI.WriteProgress("Creating wallet...")
                Dim walletManager As New WalletManager(WalletConfig.CreateDefault())
                Dim mnemonicPhrase As String = String.Join(" ", words)
                walletManager.RestoreFromMnemonic(mnemonicPhrase, password)

                ' Ensure directory exists
                Dim walletDir As String = Path.GetDirectoryName(context.WalletPath)
                If Not Directory.Exists(walletDir) Then
                    Directory.CreateDirectory(walletDir)
                End If

                ' Save wallet
                walletManager.Save(context.WalletPath)

                Console.WriteLine()
                ConsoleUI.WriteSuccess("Wallet created successfully!")
                ConsoleUI.WriteKeyValue("Wallet file", context.WalletPath)
                ConsoleUI.WriteKeyValue("Word count", wordCount.ToString())

                Return 0

            Catch ex As Exception
                ConsoleUI.WriteError($"Failed to create wallet: {ex.Message}")
                Return 1
            End Try
        End Function

        ''' <summary>
        ''' Displays help information for the create-wallet command.
        ''' </summary>
        Public Sub ShowHelp() Implements ICommand.ShowHelp
            ConsoleUI.WriteHeader("create-wallet - Create a new wallet")
            Console.WriteLine()
            Console.WriteLine("  Usage: cryptocoin-wallet create-wallet [options]")
            Console.WriteLine()
            Console.WriteLine("  Options:")
            Console.WriteLine("    --words <count>   Number of mnemonic words (12, 18, or 24; default: 24)")
            Console.WriteLine()
            Console.WriteLine("  Creates a new HD wallet with a BIP-39 compatible mnemonic recovery phrase.")
            Console.WriteLine("  The wallet will be encrypted with the password you provide.")
        End Sub

    End Class

End Namespace
