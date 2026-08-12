' ===============================================================================
' CryptoCoin.WalletCli - Commands\BackupCommand.vb
' Exports wallet backup to an encrypted file for safe storage.
' ===============================================================================

Imports System
Imports System.IO
Imports CryptoCoin.Wallet

Namespace CryptoCoin.WalletCli.Commands

    ''' <summary>
    ''' Command handler for exporting wallet backups.
    ''' Creates encrypted backup files that can be used to restore the wallet.
    ''' </summary>
    Public Class BackupCommand
        Implements ICommand

        ''' <summary>
        ''' Executes the backup command, exporting the wallet to a backup file.
        ''' </summary>
        ''' <param name="context">The command execution context.</param>
        ''' <returns>Exit code: 0 for success, non-zero for failure.</returns>
        Public Function Execute(context As CommandContext) As Integer Implements ICommand.Execute
            ConsoleUI.WriteHeader("Wallet Backup")

            ' Parse output path
            Dim outputPath As String = CommandProcessor.ParseArgument(context.Arguments, "--output")
            If String.IsNullOrWhiteSpace(outputPath) Then
                outputPath = CommandProcessor.ParseArgument(context.Arguments, "-o")
            End If

            If String.IsNullOrWhiteSpace(outputPath) Then
                ' Generate default backup filename with timestamp
                Dim timestamp As String = DateTime.Now.ToString("yyyyMMdd_HHmmss")
                outputPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    $"cryptocoin_backup_{timestamp}.bak")
            End If

            ' Check if output file already exists
            If File.Exists(outputPath) Then
                If Not CommandProcessor.PromptConfirmation($"File '{outputPath}' already exists. Overwrite?") Then
                    ConsoleUI.WriteInfo("Backup cancelled.")
                    Return 0
                End If
            End If

            Try
                ' Prompt for password to verify wallet access
                Dim password As String = CommandProcessor.PromptPassword("Enter wallet password: ")

                ' Determine backup format
                Dim includeHistory As Boolean = CommandProcessor.ParseFlag(context.Arguments, "--include-history")
                Dim encryptBackup As Boolean = Not CommandProcessor.ParseFlag(context.Arguments, "--no-encrypt")

                ConsoleUI.WriteProgress("Creating wallet backup...")

                ' Create backup using WalletBackup
                Dim backup As WalletBackup = context.WalletManager.CreateBackup()

                If encryptBackup Then
                    ' Prompt for backup encryption password
                    Dim backupPassword As String = CommandProcessor.PromptPassword("Enter backup encryption password: ")
                    Dim confirmPassword As String = CommandProcessor.PromptPassword("Confirm backup password: ")

                    If backupPassword <> confirmPassword Then
                        ConsoleUI.WriteError("Passwords do not match.")
                        Return 1
                    End If

                    ConsoleUI.WriteProgress("Encrypting and exporting backup...")
                    backup.ExportEncrypted(outputPath, backupPassword)
                Else
                    ' Export without encryption - use a default password
                    ConsoleUI.WriteProgress("Exporting backup...")
                    backup.ExportEncrypted(outputPath, password)
                End If

                ' Ensure output directory exists
                Dim outputDir As String = Path.GetDirectoryName(outputPath)
                If Not String.IsNullOrEmpty(outputDir) AndAlso Not Directory.Exists(outputDir) Then
                    Directory.CreateDirectory(outputDir)
                End If

                Console.WriteLine()
                ConsoleUI.WriteSuccess("Wallet backup created successfully!")
                ConsoleUI.WriteKeyValue("Backup file", outputPath)
                ConsoleUI.WriteKeyValue("Encrypted", If(encryptBackup, "Yes", "No"))
                ConsoleUI.WriteKeyValue("History", If(includeHistory, "Included", "Excluded"))
                Console.WriteLine()
                ConsoleUI.WriteWarning("Store this backup in a secure location.")
                ConsoleUI.WriteWarning("You will need the backup password to restore.")

                Return 0

            Catch ex As UnauthorizedAccessException
                ConsoleUI.WriteError($"Access denied: {ex.Message}")
                Return 2

            Catch ex As Exception
                ConsoleUI.WriteError($"Failed to create backup: {ex.Message}")
                Return 1
            End Try
        End Function

        ''' <summary>
        ''' Displays help information for the backup command.
        ''' </summary>
        Public Sub ShowHelp() Implements ICommand.ShowHelp
            ConsoleUI.WriteHeader("backup - Export wallet backup")
            Console.WriteLine()
            Console.WriteLine("  Usage: cryptocoin-wallet backup [options]")
            Console.WriteLine()
            Console.WriteLine("  Options:")
            Console.WriteLine("    --output, -o <path>   Output file path (default: Desktop)")
            Console.WriteLine("    --include-history     Include transaction history in backup")
            Console.WriteLine("    --no-encrypt          Skip backup encryption (not recommended)")
        End Sub

    End Class

End Namespace
