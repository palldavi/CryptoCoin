' ===============================================================================
' CryptoCoin.WalletCli - ConsoleUI.vb
' Console formatting helpers for colored output, progress bars, and table display.
' Provides a consistent visual experience for the CLI application.
' ===============================================================================

Imports System

Namespace CryptoCoin.WalletCli

    ''' <summary>
    ''' Provides console formatting utilities including colored output, progress indicators,
    ''' table rendering, and banner display for the wallet CLI application.
    ''' </summary>
    Public NotInheritable Class ConsoleUI

        Private Sub New()
            ' Static utility class - prevent instantiation
        End Sub

        ''' <summary>
        ''' Writes an informational message in cyan color.
        ''' </summary>
        ''' <param name="message">The message to display.</param>
        Public Shared Sub WriteInfo(message As String)
            WriteColored(message, ConsoleColor.Cyan)
        End Sub

        ''' <summary>
        ''' Writes a success message in green color.
        ''' </summary>
        ''' <param name="message">The message to display.</param>
        Public Shared Sub WriteSuccess(message As String)
            WriteColored($"  [OK] {message}", ConsoleColor.Green)
        End Sub

        ''' <summary>
        ''' Writes a warning message in yellow color.
        ''' </summary>
        ''' <param name="message">The message to display.</param>
        Public Shared Sub WriteWarning(message As String)
            WriteColored($"  [WARN] {message}", ConsoleColor.Yellow)
        End Sub

        ''' <summary>
        ''' Writes an error message in red color.
        ''' </summary>
        ''' <param name="message">The message to display.</param>
        Public Shared Sub WriteError(message As String)
            WriteColored($"  [ERROR] {message}", ConsoleColor.Red)
        End Sub

        ''' <summary>
        ''' Writes a progress message in dark yellow color.
        ''' </summary>
        ''' <param name="message">The progress message to display.</param>
        Public Shared Sub WriteProgress(message As String)
            WriteColored($"  ... {message}", ConsoleColor.DarkYellow)
        End Sub

        ''' <summary>
        ''' Writes a section header with underline formatting.
        ''' </summary>
        ''' <param name="title">The header title to display.</param>
        Public Shared Sub WriteHeader(title As String)
            Console.WriteLine()
            WriteColored($"  {title}", ConsoleColor.White)
            WriteColored($"  {New String("-"c, title.Length)}", ConsoleColor.DarkGray)
        End Sub

        ''' <summary>
        ''' Writes the application banner with name and version.
        ''' </summary>
        ''' <param name="appName">The application name.</param>
        ''' <param name="version">The application version.</param>
        Public Shared Sub WriteBanner(appName As String, version As String)
            Dim border As String = New String("="c, 50)
            WriteColored(border, ConsoleColor.DarkCyan)
            WriteColored($"  {appName} v{version}", ConsoleColor.Cyan)
            WriteColored($"  CryptoCoin Reference Implementation", ConsoleColor.DarkGray)
            WriteColored(border, ConsoleColor.DarkCyan)
        End Sub

        ''' <summary>
        ''' Displays a simple progress bar in the console.
        ''' </summary>
        ''' <param name="current">The current progress value.</param>
        ''' <param name="total">The total value representing 100% completion.</param>
        ''' <param name="label">Optional label to display alongside the progress bar.</param>
        Public Shared Sub WriteProgressBar(current As Integer, total As Integer, Optional label As String = "")
            Dim barWidth As Integer = 30
            Dim progress As Double = CDbl(current) / CDbl(total)
            Dim filled As Integer = CInt(Math.Floor(progress * barWidth))
            Dim empty As Integer = barWidth - filled

            Dim bar As String = New String(CChar("#"), filled) & New String(CChar("-"), empty)
            Dim percentage As Integer = CInt(Math.Floor(progress * 100))

            Console.Write(vbCr & $"  [{bar}] {percentage,3}%")
            If Not String.IsNullOrEmpty(label) Then
                Console.Write($" {label}")
            End If

            If current >= total Then
                Console.WriteLine()
            End If
        End Sub

        ''' <summary>
        ''' Renders a formatted table with headers and rows.
        ''' </summary>
        ''' <param name="headers">The column header names.</param>
        ''' <param name="rows">The data rows to display.</param>
        ''' <param name="columnWidths">Optional column widths. Auto-calculated if not specified.</param>
        Public Shared Sub WriteTable(headers As String(), rows As List(Of String()), Optional columnWidths As Integer() = Nothing)
            If columnWidths Is Nothing Then
                columnWidths = CalculateColumnWidths(headers, rows)
            End If

            ' Write header row
            Dim headerLine As String = "  "
            Dim separatorLine As String = "  "
            For i As Integer = 0 To headers.Length - 1
                headerLine &= PadColumn(headers(i), columnWidths(i))
                separatorLine &= New String("-"c, columnWidths(i))
                If i < headers.Length - 1 Then
                    headerLine &= "  "
                    separatorLine &= "  "
                End If
            Next

            WriteColored(headerLine, ConsoleColor.White)
            WriteColored(separatorLine, ConsoleColor.DarkGray)

            ' Write data rows
            For Each row As String() In rows
                Dim rowLine As String = "  "
                For i As Integer = 0 To Math.Min(row.Length, headers.Length) - 1
                    rowLine &= PadColumn(If(row(i), ""), columnWidths(i))
                    If i < headers.Length - 1 Then
                        rowLine &= "  "
                    End If
                Next
                Console.WriteLine(rowLine)
            Next
        End Sub

        ''' <summary>
        ''' Writes a key-value pair formatted for display.
        ''' </summary>
        ''' <param name="key">The label/key text.</param>
        ''' <param name="value">The value text.</param>
        ''' <param name="keyWidth">The width to allocate for the key column.</param>
        Public Shared Sub WriteKeyValue(key As String, value As String, Optional keyWidth As Integer = 20)
            Dim paddedKey As String = (key & ":").PadRight(keyWidth)
            Console.Write("  ")
            Dim oldColor As ConsoleColor = Console.ForegroundColor
            Console.ForegroundColor = ConsoleColor.Gray
            Console.Write(paddedKey)
            Console.ForegroundColor = ConsoleColor.White
            Console.WriteLine(value)
            Console.ForegroundColor = oldColor
        End Sub

        ''' <summary>
        ''' Writes a horizontal separator line.
        ''' </summary>
        ''' <param name="width">The width of the separator line.</param>
        Public Shared Sub WriteSeparator(Optional width As Integer = 50)
            WriteColored(New String("-"c, width), ConsoleColor.DarkGray)
        End Sub

        ''' <summary>
        ''' Writes a mnemonic phrase with word numbering for easy recording.
        ''' </summary>
        ''' <param name="words">The mnemonic words to display.</param>
        Public Shared Sub WriteMnemonic(words As String())
            Console.WriteLine()
            WriteColored("  Recovery Phrase (write these words down in order):", ConsoleColor.Yellow)
            Console.WriteLine()

            For i As Integer = 0 To words.Length - 1
                Dim number As String = $"{i + 1,2}."
                Dim oldColor As ConsoleColor = Console.ForegroundColor
                Console.ForegroundColor = ConsoleColor.DarkGray
                Console.Write($"  {number} ")
                Console.ForegroundColor = ConsoleColor.White
                Console.WriteLine(words(i))
                Console.ForegroundColor = oldColor
            Next

            Console.WriteLine()
        End Sub

        ''' <summary>
        ''' Writes text with the specified console color.
        ''' </summary>
        ''' <param name="text">The text to write.</param>
        ''' <param name="color">The foreground color to use.</param>
        Private Shared Sub WriteColored(text As String, color As ConsoleColor)
            Dim oldColor As ConsoleColor = Console.ForegroundColor
            Console.ForegroundColor = color
            Console.WriteLine(text)
            Console.ForegroundColor = oldColor
        End Sub

        ''' <summary>
        ''' Calculates optimal column widths based on header and data content.
        ''' </summary>
        ''' <param name="headers">The column headers.</param>
        ''' <param name="rows">The data rows.</param>
        ''' <returns>An array of calculated column widths.</returns>
        Private Shared Function CalculateColumnWidths(headers As String(), rows As List(Of String())) As Integer()
            Dim widths(headers.Length - 1) As Integer

            ' Start with header widths
            For i As Integer = 0 To headers.Length - 1
                widths(i) = headers(i).Length
            Next

            ' Check data widths
            For Each row As String() In rows
                For i As Integer = 0 To Math.Min(row.Length, headers.Length) - 1
                    Dim cellLength As Integer = If(row(i) IsNot Nothing, row(i).Length, 0)
                    If cellLength > widths(i) Then
                        widths(i) = cellLength
                    End If
                Next
            Next

            ' Cap maximum width
            For i As Integer = 0 To widths.Length - 1
                If widths(i) > 40 Then widths(i) = 40
            Next

            Return widths
        End Function

        ''' <summary>
        ''' Pads or truncates a string to fit the specified column width.
        ''' </summary>
        ''' <param name="text">The text to pad.</param>
        ''' <param name="width">The target width.</param>
        ''' <returns>The padded or truncated string.</returns>
        Private Shared Function PadColumn(text As String, width As Integer) As String
            If text.Length > width Then
                Return text.Substring(0, width - 3) & "..."
            End If
            Return text.PadRight(width)
        End Function

    End Class

End Namespace
