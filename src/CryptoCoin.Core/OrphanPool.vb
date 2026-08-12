Namespace CryptoCoin.Core

    ''' <summary>
    ''' Pool for blocks whose parent has not yet been received.
    ''' These "orphan" blocks are held until their parent arrives.
    ''' </summary>
    Public Class OrphanPool

        Private ReadOnly _orphans As New Dictionary(Of String, Block)(StringComparer.OrdinalIgnoreCase)
        Private ReadOnly _byParent As New Dictionary(Of String, List(Of String))(StringComparer.OrdinalIgnoreCase)
        Private ReadOnly _syncLock As New Object()
        Private Const MaxOrphans As Integer = 1000

        ''' <summary>
        ''' Gets the number of orphan blocks in the pool.
        ''' </summary>
        Public ReadOnly Property Count As Integer
            Get
                SyncLock _syncLock
                    Return _orphans.Count
                End SyncLock
            End Get
        End Property

        ''' <summary>
        ''' Adds a block to the orphan pool.
        ''' </summary>
        Public Sub Add(block As Block)
            If block Is Nothing Then Return
            SyncLock _syncLock
                ' Evict oldest if at capacity
                If _orphans.Count >= MaxOrphans Then
                    EvictOldest()
                End If

                _orphans(block.Hash) = block

                ' Index by parent hash
                Dim parentHash As String = block.Header.PreviousBlockHash
                If Not _byParent.ContainsKey(parentHash) Then
                    _byParent(parentHash) = New List(Of String)()
                End If
                _byParent(parentHash).Add(block.Hash)
            End SyncLock
        End Sub

        ''' <summary>
        ''' Gets all orphan blocks that reference the given parent hash.
        ''' </summary>
        Public Function GetByParent(parentHash As String) As List(Of Block)
            SyncLock _syncLock
                Dim result As New List(Of Block)()
                Dim hashes As List(Of String) = Nothing
                If _byParent.TryGetValue(parentHash, hashes) Then
                    For Each hash As String In hashes
                        Dim block As Block = Nothing
                        If _orphans.TryGetValue(hash, block) Then
                            result.Add(block)
                        End If
                    Next
                End If
                Return result
            End SyncLock
        End Function

        ''' <summary>
        ''' Removes an orphan block by hash.
        ''' </summary>
        Public Sub Remove(hash As String)
            SyncLock _syncLock
                Dim block As Block = Nothing
                If _orphans.TryGetValue(hash, block) Then
                    _orphans.Remove(hash)
                    Dim parentHash As String = block.Header.PreviousBlockHash
                    If _byParent.ContainsKey(parentHash) Then
                        _byParent(parentHash).Remove(hash)
                        If _byParent(parentHash).Count = 0 Then
                            _byParent.Remove(parentHash)
                        End If
                    End If
                End If
            End SyncLock
        End Sub

        ''' <summary>
        ''' Checks if a block hash exists in the orphan pool.
        ''' </summary>
        Public Function Contains(hash As String) As Boolean
            SyncLock _syncLock
                Return _orphans.ContainsKey(hash)
            End SyncLock
        End Function

        ''' <summary>
        ''' Removes the oldest orphan to make room for new ones.
        ''' </summary>
        Private Sub EvictOldest()
            ' Simple eviction: remove first entry
            If _orphans.Count > 0 Then
                Dim firstKey As String = Nothing
                For Each key As String In _orphans.Keys
                    firstKey = key
                    Exit For
                Next
                If firstKey IsNot Nothing Then
                    Remove(firstKey)
                End If
            End If
        End Sub

        ''' <summary>
        ''' Clears all orphan blocks.
        ''' </summary>
        Public Sub Clear()
            SyncLock _syncLock
                _orphans.Clear()
                _byParent.Clear()
            End SyncLock
        End Sub

    End Class

End Namespace
