Imports System.Collections.Concurrent

Namespace CryptoCoin.Networking

    ''' <summary>
    ''' Delegate for handling a specific type of network message.
    ''' </summary>
    ''' <param name="peer">The peer that sent the message.</param>
    ''' <param name="message">The received network message.</param>
    Public Delegate Sub MessageHandlerDelegate(peer As Peer, message As NetworkMessage)

    ''' <summary>
    ''' Defines the interface for processing incoming network messages.
    ''' </summary>
    Public Interface IMessageProcessor
        ''' <summary>
        ''' Processes an incoming network message from a peer.
        ''' </summary>
        ''' <param name="peer">The peer that sent the message.</param>
        ''' <param name="message">The network message to process.</param>
        Sub ProcessMessage(peer As Peer, message As NetworkMessage)

        ''' <summary>
        ''' Gets the command name this processor handles.
        ''' </summary>
        ReadOnly Property Command As String
    End Interface

    ''' <summary>
    ''' Routes incoming network messages to the appropriate registered handlers.
    ''' Supports handler registration by command name, message queuing, and
    ''' statistics tracking for each message type.
    ''' </summary>
    Public Class MessageHandler

        Private ReadOnly _handlers As New Dictionary(Of String, MessageHandlerDelegate)()
        Private ReadOnly _processors As New Dictionary(Of String, IMessageProcessor)()
        Private ReadOnly _messageQueue As New ConcurrentQueue(Of Tuple(Of Peer, NetworkMessage))()
        Private ReadOnly _messageStats As New ConcurrentDictionary(Of String, Long)()
        Private ReadOnly _syncLock As New Object()
        Private _isProcessing As Boolean

        ''' <summary>
        ''' Gets the number of messages currently queued for processing.
        ''' </summary>
        Public ReadOnly Property QueuedMessageCount As Integer
            Get
                Return _messageQueue.Count
            End Get
        End Property

        ''' <summary>
        ''' Gets whether the handler is currently processing messages.
        ''' </summary>
        Public ReadOnly Property IsProcessing As Boolean
            Get
                Return _isProcessing
            End Get
        End Property

        ''' <summary>
        ''' Creates a new MessageHandler instance.
        ''' </summary>
        Public Sub New()
            _isProcessing = False
        End Sub

        ''' <summary>
        ''' Registers a delegate handler for a specific command.
        ''' </summary>
        ''' <param name="command">The command name to handle (e.g., "block", "tx").</param>
        ''' <param name="handler">The delegate to invoke when the command is received.</param>
        Public Sub RegisterHandler(command As String, handler As MessageHandlerDelegate)
            If String.IsNullOrEmpty(command) Then
                Throw New ArgumentNullException(NameOf(command))
            End If
            If handler Is Nothing Then
                Throw New ArgumentNullException(NameOf(handler))
            End If

            SyncLock _syncLock
                _handlers(command.ToLowerInvariant()) = handler
            End SyncLock
        End Sub

        ''' <summary>
        ''' Registers a message processor for a specific command.
        ''' </summary>
        ''' <param name="processor">The processor to register.</param>
        Public Sub RegisterProcessor(processor As IMessageProcessor)
            If processor Is Nothing Then
                Throw New ArgumentNullException(NameOf(processor))
            End If

            SyncLock _syncLock
                _processors(processor.Command.ToLowerInvariant()) = processor
            End SyncLock
        End Sub

        ''' <summary>
        ''' Unregisters the handler for a specific command.
        ''' </summary>
        ''' <param name="command">The command name to unregister.</param>
        ''' <returns>True if a handler was removed.</returns>
        Public Function UnregisterHandler(command As String) As Boolean
            If String.IsNullOrEmpty(command) Then Return False

            SyncLock _syncLock
                Dim key As String = command.ToLowerInvariant()
                Dim removed As Boolean = _handlers.Remove(key)
                removed = _processors.Remove(key) OrElse removed
                Return removed
            End SyncLock
        End Function

        ''' <summary>
        ''' Handles an incoming message by routing it to the appropriate handler.
        ''' If no handler is registered, the message is silently dropped.
        ''' </summary>
        ''' <param name="peer">The peer that sent the message.</param>
        ''' <param name="message">The received network message.</param>
        Public Sub HandleMessage(peer As Peer, message As NetworkMessage)
            If peer Is Nothing OrElse message Is Nothing Then Return

            Dim command As String = message.Command.ToLowerInvariant()

            ' Update statistics
            _messageStats.AddOrUpdate(command, 1L, Function(key, existing) existing + 1L)

            ' Update peer last seen
            peer.MarkSeen()

            ' Try delegate handler first
            Dim handler As MessageHandlerDelegate = Nothing
            Dim processor As IMessageProcessor = Nothing

            SyncLock _syncLock
                _handlers.TryGetValue(command, handler)
                _processors.TryGetValue(command, processor)
            End SyncLock

            If handler IsNot Nothing Then
                Try
                    handler.Invoke(peer, message)
                Catch ex As Exception
                    ' Log error but don't crash the message loop
                    System.Diagnostics.Debug.WriteLine($"Error handling message '{command}' from {peer.EndPointString}: {ex.Message}")
                End Try
            ElseIf processor IsNot Nothing Then
                Try
                    processor.ProcessMessage(peer, message)
                Catch ex As Exception
                    System.Diagnostics.Debug.WriteLine($"Error processing message '{command}' from {peer.EndPointString}: {ex.Message}")
                End Try
            End If
        End Sub

        ''' <summary>
        ''' Enqueues a message for asynchronous processing.
        ''' </summary>
        ''' <param name="peer">The peer that sent the message.</param>
        ''' <param name="message">The network message to queue.</param>
        Public Sub EnqueueMessage(peer As Peer, message As NetworkMessage)
            If peer Is Nothing OrElse message Is Nothing Then Return
            _messageQueue.Enqueue(Tuple.Create(peer, message))
        End Sub

        ''' <summary>
        ''' Processes all queued messages synchronously.
        ''' </summary>
        ''' <returns>The number of messages processed.</returns>
        Public Function ProcessQueue() As Integer
            _isProcessing = True
            Dim processed As Integer = 0

            Try
                Dim item As Tuple(Of Peer, NetworkMessage) = Nothing
                While _messageQueue.TryDequeue(item)
                    HandleMessage(item.Item1, item.Item2)
                    processed += 1
                End While
            Finally
                _isProcessing = False
            End Try

            Return processed
        End Function

        ''' <summary>
        ''' Processes up to the specified number of queued messages.
        ''' </summary>
        ''' <param name="maxMessages">Maximum number of messages to process.</param>
        ''' <returns>The number of messages actually processed.</returns>
        Public Function ProcessQueue(maxMessages As Integer) As Integer
            _isProcessing = True
            Dim processed As Integer = 0

            Try
                Dim item As Tuple(Of Peer, NetworkMessage) = Nothing
                While processed < maxMessages AndAlso _messageQueue.TryDequeue(item)
                    HandleMessage(item.Item1, item.Item2)
                    processed += 1
                End While
            Finally
                _isProcessing = False
            End Try

            Return processed
        End Function

        ''' <summary>
        ''' Gets the total number of messages received for a specific command.
        ''' </summary>
        ''' <param name="command">The command name to query.</param>
        ''' <returns>The total message count for that command.</returns>
        Public Function GetMessageCount(command As String) As Long
            Dim count As Long = 0
            _messageStats.TryGetValue(command.ToLowerInvariant(), count)
            Return count
        End Function

        ''' <summary>
        ''' Gets all message statistics as a dictionary of command to count.
        ''' </summary>
        ''' <returns>A dictionary of message counts by command.</returns>
        Public Function GetAllStats() As Dictionary(Of String, Long)
            Return New Dictionary(Of String, Long)(_messageStats)
        End Function

        ''' <summary>
        ''' Checks whether a handler is registered for the given command.
        ''' </summary>
        ''' <param name="command">The command name to check.</param>
        ''' <returns>True if a handler or processor is registered.</returns>
        Public Function HasHandler(command As String) As Boolean
            If String.IsNullOrEmpty(command) Then Return False
            Dim key As String = command.ToLowerInvariant()
            SyncLock _syncLock
                Return _handlers.ContainsKey(key) OrElse _processors.ContainsKey(key)
            End SyncLock
        End Function

        ''' <summary>
        ''' Resets all message statistics.
        ''' </summary>
        Public Sub ResetStats()
            _messageStats.Clear()
        End Sub

    End Class

End Namespace
