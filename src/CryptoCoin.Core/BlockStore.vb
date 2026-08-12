Namespace CryptoCoin.Core

    ''' <summary>
    ''' In-memory block storage. In production, this would be backed by LevelDB or similar.
    ''' Stores complete blocks indexed by their hash.
    ''' </summary>
    Public Class BlockStore

        Private ReadOnly _blocks As New Dictionary(Of String, Block)(StringComparer.OrdinalIgnoreCase)
        Private ReadOnly _syncLock As New Object()

        ''' <summary>
        ''' Gets the number of blocks stored.
        ''' </summary>
        Public ReadOnly Property Count As Integer
            Get
                SyncLock _syncLock
                    Return _blocks.Count
                End SyncLock
            End Get
        End Property

        ''' <summary>
        ''' Adds a block to the store.
        ''' </summary>
        Public Overridable Sub AddBlock(block As Block)
            If block Is Nothing Then Throw New ArgumentNullException(NameOf(block))
            SyncLock _syncLock
                _blocks(block.Hash) = block
            End SyncLock
        End Sub

        ''' <summary>
        ''' Gets a block by its hash.
        ''' </summary>
        Public Function GetBlock(hash As String) As Block
            If String.IsNullOrEmpty(hash) Then Return Nothing
            SyncLock _syncLock
                Dim block As Block = Nothing
                _blocks.TryGetValue(hash, block)
                Return block
            End SyncLock
        End Function

        ''' <summary>
        ''' Checks if a block exists in the store.
        ''' </summary>
        Public Function HasBlock(hash As String) As Boolean
            If String.IsNullOrEmpty(hash) Then Return False
            SyncLock _syncLock
                Return _blocks.ContainsKey(hash)
            End SyncLock
        End Function

        ''' <summary>
        ''' Removes a block from the store.
        ''' </summary>
        Public Function RemoveBlock(hash As String) As Boolean
            If String.IsNullOrEmpty(hash) Then Return False
            SyncLock _syncLock
                Return _blocks.Remove(hash)
            End SyncLock
        End Function

        ''' <summary>
        ''' Gets all block hashes in the store.
        ''' </summary>
        Public Function GetAllHashes() As List(Of String)
            SyncLock _syncLock
                Return New List(Of String)(_blocks.Keys)
            End SyncLock
        End Function

        ''' <summary>
        ''' Clears all blocks from the store.
        ''' </summary>
        Public Sub Clear()
            SyncLock _syncLock
                _blocks.Clear()
            End SyncLock
        End Sub

    End Class

End Namespace
