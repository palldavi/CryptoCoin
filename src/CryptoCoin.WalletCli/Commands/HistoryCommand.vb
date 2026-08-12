' ===============================================================================
' CryptoCoin.WalletCli - Commands\HistoryCommand.vb
' Displays transaction history with pagination support.
' ===============================================================================

Imports System
Imports CryptoCoin.Wallet

Namespace CryptoCoin.WalletCli.Commands

    ''' <summary>
    ''' Command handler for displaying wallet transaction history.
    ''' Supports pagination and filtering by transaction type.
    ''' </summary>
    Public Class HistoryCommand
        Implements ICommand

        Private Const DefaultPageSize As Integer = 10

        ''' <summary>
        ''' Executes the history command, displaying transaction history with pagination.
        ''' </summary>
        ''' <param name="context">The command execution context.</param>
        ''' <returns>Exit code: 0 for success, non-zero for failure.</returns>
        Public Function Execute(context As CommandContext) As Integer Implements ICommand.Execute
            ConsoleUI.WriteHeader("Transaction History")

            Try
                ' Parse pagination arguments
                Dim pageStr As String = CommandProcessor.ParseArgument(context.Arguments, "--page")
                Dim limitStr As String = CommandProcessor.ParseArgument(context.Arguments, "--limit")
                Dim filterType As String = CommandProcessor.ParseArgument(context.Arguments, "--type")

                Dim page As Integer = 1
                Dim limit As Integer = DefaultPageSize

                If pageStr IsNot Nothing Then Integer.TryParse(pageStr, page)
                If limitStr IsNot Nothing Then Integer.TryParse(limitStr, limit)
                If page < 1 Then page = 1
                If limit < 1 OrElse limit > 100 Then limit = DefaultPageSize

                ' Get transaction history
                Dim history As TransactionHistory = context.WalletManager.History
                Dim transactions As List(Of WalletTransaction) = history.GetAllTransactions()

                ' Apply type filter if specified
                If Not String.IsNullOrEmpty(filterType) Then
                    Select Case filterType.ToLowerInvariant()
                        Case "sent", "send"
                            transactions = transactions.Where(Function(t) t.Amount < 0).ToList()
                        Case "received", "receive"
                            transactions = transactions.Where(Function(t) t.Amount > 0).ToList()
                    End Select
                End If

                ' Calculate pagination
                Dim totalCount As Integer = transactions.Count
                Dim totalPages As Integer = CInt(Math.Ceiling(CDbl(totalCount) / CDbl(limit)))
                Dim skip As Integer = (page - 1) * limit
                Dim pageTransactions = transactions.Skip(skip).Take(limit).ToList()

                If totalCount = 0 Then
                    ConsoleUI.WriteInfo("No transactions found.")
                    Return 0
                End If

                ' Display transactions as table
                Dim headers As String() = {"Date", "Type", "Amount", "TxID", "Conf."}
                Dim rows As New List(Of String())

                For Each tx As Object In pageTransactions
                    Dim txType As String = If(tx.Amount >= 0, "RECV", "SENT")
                    Dim txIdShort As String = If(tx.TransactionId.Length > 16,
                        tx.TransactionId.Substring(0, 16) & "...",
                        tx.TransactionId)

                    rows.Add(New String() {
                        tx.Timestamp.ToString("yyyy-MM-dd HH:mm"),
                        txType,
                        CommandProcessor.FormatAmount(Math.Abs(tx.Amount)),
                        txIdShort,
                        tx.Confirmations.ToString()
                    })
                Next

                ConsoleUI.WriteTable(headers, rows)

                ' Display pagination info
                Console.WriteLine()
                ConsoleUI.WriteInfo($"  Page {page} of {totalPages} ({totalCount} total transactions)")
                If page < totalPages Then
                    ConsoleUI.WriteInfo($"  Use --page {page + 1} to see the next page")
                End If

                Return 0

            Catch ex As Exception
                ConsoleUI.WriteError($"Failed to retrieve transaction history: {ex.Message}")
                Return 1
            End Try
        End Function

        ''' <summary>
        ''' Displays help information for the history command.
        ''' </summary>
        Public Sub ShowHelp() Implements ICommand.ShowHelp
            ConsoleUI.WriteHeader("history - Show transaction history")
            Console.WriteLine()
            Console.WriteLine("  Usage: cryptocoin-wallet history [options]")
            Console.WriteLine()
            Console.WriteLine("  Options:")
            Console.WriteLine("    --page <number>   Page number (default: 1)")
            Console.WriteLine("    --limit <count>   Transactions per page (default: 10, max: 100)")
            Console.WriteLine("    --type <filter>   Filter by type: sent, received")
        End Sub

    End Class

End Namespace
