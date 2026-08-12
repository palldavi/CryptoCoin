Imports System
Imports log4net
Imports log4net.Config
Imports log4net.Appender
Imports log4net.Layout
Imports log4net.Repository.Hierarchy
Imports Microsoft.Practices.EnterpriseLibrary.Logging
Imports Microsoft.Practices.EnterpriseLibrary.Logging.Formatters
Imports Microsoft.Practices.EnterpriseLibrary.Logging.TraceListeners

Namespace CryptoCoin.Node.Logging

    ''' <summary>
    ''' Centralised logging facade for the CryptoCoin Node.
    ''' Writes to two logging frameworks simultaneously:
    '''   - log4net  (rolling file + console appenders)
    '''   - Enterprise Library Logging (formatted trace listener)
    ''' Both are configured entirely in code — no XML config files required.
    '''
    ''' Modernisation note: on .NET 10 both frameworks would be replaced by
    ''' Microsoft.Extensions.Logging with an ILogger abstraction.
    ''' </summary>
    Public Class NodeLogger

        ' ── log4net ─────────────────────────────────────────────────────────
        Private Shared ReadOnly _log4net As ILog =
            LogManager.GetLogger(GetType(NodeLogger))

        ' ── Enterprise Library ───────────────────────────────────────────────
        Private Shared _entLibWriter As LogWriter

        ' ── Initialisation ───────────────────────────────────────────────────

        ''' <summary>
        ''' Configures both logging frameworks. Call once at application startup.
        ''' </summary>
        Public Shared Sub Configure(Optional logDirectory As String = "logs")
            ConfigureLog4Net(logDirectory)
            ConfigureEnterpriseLibrary(logDirectory)
        End Sub

        Private Shared Sub ConfigureLog4Net(logDirectory As String)
            Dim hierarchy As Hierarchy = CType(LogManager.GetRepository(), Hierarchy)

            ' Pattern layout shared by all appenders
            Dim layout As New PatternLayout()
            layout.ConversionPattern = "%date{yyyy-MM-dd HH:mm:ss} [%-5level] %message%newline"
            layout.ActivateOptions()

            ' Console appender
            Dim consoleAppender As New ConsoleAppender()
            consoleAppender.Layout = layout
            consoleAppender.ActivateOptions()

            ' Rolling file appender
            If Not System.IO.Directory.Exists(logDirectory) Then
                System.IO.Directory.CreateDirectory(logDirectory)
            End If
            Dim fileAppender As New RollingFileAppender()
            fileAppender.File = System.IO.Path.Combine(logDirectory, "node.log")
            fileAppender.AppendToFile = True
            fileAppender.RollingStyle = RollingFileAppender.RollingMode.Size
            fileAppender.MaxSizeRollBackups = 5
            fileAppender.MaximumFileSize = "10MB"
            fileAppender.StaticLogFileName = True
            fileAppender.Layout = layout
            fileAppender.ActivateOptions()

            hierarchy.Root.AddAppender(consoleAppender)
            hierarchy.Root.AddAppender(fileAppender)
            hierarchy.Root.Level = log4net.Core.Level.Debug
            hierarchy.Configured = True
        End Sub

        Private Shared Sub ConfigureEnterpriseLibrary(logDirectory As String)
            ' Text formatter
            Dim formatter As New TextFormatter(
                "EntLib [{severity}] {timestamp} - {message}")

            ' Flat file trace listener
            If Not System.IO.Directory.Exists(logDirectory) Then
                System.IO.Directory.CreateDirectory(logDirectory)
            End If
            Dim listener As New FlatFileTraceListener(
                System.IO.Path.Combine(logDirectory, "node-entlib.log"),
                "--- Log Entry ---", "--- End ---", formatter)

            ' Build log writer
            Dim config As New LoggingConfiguration()
            config.AddLogSource("General", Diagnostics.TraceEventType.Information, True) _
                  .AddTraceListener(listener)

            _entLibWriter = New LogWriter(config)
        End Sub

        ' ── Public logging methods ───────────────────────────────────────────

        Public Shared Sub Info(message As String)
            _log4net.Info(message)
            WriteEntLib(message, Diagnostics.TraceEventType.Information)
        End Sub

        Public Shared Sub Warn(message As String)
            _log4net.Warn(message)
            WriteEntLib(message, Diagnostics.TraceEventType.Warning)
        End Sub

        Public Shared Sub [Error](message As String, Optional ex As Exception = Nothing)
            If ex IsNot Nothing Then
                _log4net.Error(message, ex)
            Else
                _log4net.Error(message)
            End If
            WriteEntLib(If(ex IsNot Nothing, $"{message} | {ex.Message}", message),
                        Diagnostics.TraceEventType.Error)
        End Sub

        Public Shared Sub Debug(message As String)
            _log4net.Debug(message)
            WriteEntLib(message, Diagnostics.TraceEventType.Verbose)
        End Sub

        Private Shared Sub WriteEntLib(message As String, severity As Diagnostics.TraceEventType)
            Try
                If _entLibWriter IsNot Nothing Then
                    Dim entry As New LogEntry()
                    entry.Message = message
                    entry.Severity = severity
                    entry.Categories.Add("General")
                    _entLibWriter.Write(entry)
                End If
            Catch
                ' Never let logging failures crash the application
            End Try
        End Sub

    End Class

End Namespace
