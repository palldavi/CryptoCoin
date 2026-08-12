Imports CryptoCoin.Core

Namespace CryptoCoin.Node

    ''' <summary>
    ''' Manages blockchain synchronization with peers.
    ''' </summary>
    Public Class SyncManager

        Private ReadOnly _blockchain As Blockchain
        Private _isSyncing As Boolean
        Private _syncTarget As Integer

        ''' <summary>
        ''' Whether the node is currently syncing.
        ''' </summary>
        Public ReadOnly Property IsSyncing As Boolean
            Get
                Return _isSyncing
            End Get
        End Property

        ''' <summary>
        ''' The target height to sync to.
        ''' </summary>
        Public ReadOnly Property SyncTarget As Integer
            Get
                Return _syncTarget
            End Get
        End Property

        ''' <summary>
        ''' Current sync progress (0.0 to 1.0).
        ''' </summary>
        Public ReadOnly Property Progress As Double
            Get
                If _syncTarget <= 0 Then Return 1.0
                Return CDbl(_blockchain.Height) / _syncTarget
            End Get
        End Property

        Public Sub New(blockchain As Blockchain)
            _blockchain = blockchain
            _isSyncing = False
            _syncTarget = 0
        End Sub

        ''' <summary>
        ''' Starts syncing to the given target height.
        ''' </summary>
        Public Sub StartSync(targetHeight As Integer)
            If targetHeight <= _blockchain.Height Then Return
            _syncTarget = targetHeight
            _isSyncing = True
        End Sub

        ''' <summary>
        ''' Processes a received block during sync.
        ''' </summary>
        Public Function ProcessSyncBlock(block As Block) As Boolean
            Dim result As BlockValidationResult = _blockchain.AddBlock(block)
            If result.IsValid Then
                If _blockchain.Height >= _syncTarget Then
                    _isSyncing = False
                End If
                Return True
            End If
            Return False
        End Function

        ''' <summary>
        ''' Stops the sync process.
        ''' </summary>
        Public Sub StopSync()
            _isSyncing = False
            _syncTarget = 0
        End Sub

        ''' <summary>
        ''' Gets block locator hashes for requesting blocks from peers.
        ''' </summary>
        Public Function GetBlockLocator() As List(Of String)
            Dim locator As New List(Of String)()
            Dim height As Integer = _blockchain.Height
            Dim stepSize As Integer = 1

            While height >= 0
                Dim block As Block = _blockchain.GetBlockByHeight(height)
                If block IsNot Nothing Then
                    locator.Add(block.Hash)
                End If
                If height = 0 Then Exit While
                height -= stepSize
                If locator.Count > 10 Then stepSize *= 2
                If height < 0 Then height = 0
            End While

            Return locator
        End Function

    End Class

End Namespace
