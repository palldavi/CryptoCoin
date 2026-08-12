Namespace CryptoCoin.Core

    ''' <summary>
    ''' Tracks the current state of the blockchain including the active chain tip,
    ''' block indices, and height-to-hash mapping.
    ''' </summary>
    Public Class ChainState

        Private ReadOnly _indices As New Dictionary(Of String, BlockIndex)(StringComparer.OrdinalIgnoreCase)
        Private ReadOnly _heightMap As New Dictionary(Of Integer, String)()
        Private _tip As BlockIndex
        Private ReadOnly _syncLock As New Object()

        ''' <summary>
        ''' Gets the current chain tip (highest block on the best chain).
        ''' </summary>
        Public ReadOnly Property Tip As BlockIndex
            Get
                SyncLock _syncLock
                    Return _tip
                End SyncLock
            End Get
        End Property

        ''' <summary>
        ''' Gets the total number of block indices tracked.
        ''' </summary>
        Public ReadOnly Property IndexCount As Integer
            Get
                SyncLock _syncLock
                    Return _indices.Count
                End SyncLock
            End Get
        End Property

        ''' <summary>
        ''' Sets the chain tip to a new block index.
        ''' </summary>
        Public Overridable Sub SetTip(index As BlockIndex)
            If index Is Nothing Then Throw New ArgumentNullException(NameOf(index))
            SyncLock _syncLock
                _tip = index
                _heightMap(index.Height) = index.Hash
                If Not _indices.ContainsKey(index.Hash) Then
                    _indices(index.Hash) = index
                End If
            End SyncLock
        End Sub

        ''' <summary>
        ''' Adds a block index to the state.
        ''' </summary>
        Public Overridable Sub AddIndex(index As BlockIndex)
            If index Is Nothing Then Throw New ArgumentNullException(NameOf(index))
            SyncLock _syncLock
                _indices(index.Hash) = index
                _heightMap(index.Height) = index.Hash
            End SyncLock
        End Sub

        ''' <summary>
        ''' Gets a block index by hash.
        ''' </summary>
        Public Function GetIndex(hash As String) As BlockIndex
            If String.IsNullOrEmpty(hash) Then Return Nothing
            SyncLock _syncLock
                Dim index As BlockIndex = Nothing
                _indices.TryGetValue(hash, index)
                Return index
            End SyncLock
        End Function

        ''' <summary>
        ''' Gets a block index by height.
        ''' </summary>
        Public Function GetByHeight(height As Integer) As BlockIndex
            SyncLock _syncLock
                Dim hash As String = Nothing
                If _heightMap.TryGetValue(height, hash) Then
                    Return GetIndex(hash)
                End If
                Return Nothing
            End SyncLock
        End Function

        ''' <summary>
        ''' Checks if a block index exists for the given hash.
        ''' </summary>
        Public Function HasIndex(hash As String) As Boolean
            If String.IsNullOrEmpty(hash) Then Return False
            SyncLock _syncLock
                Return _indices.ContainsKey(hash)
            End SyncLock
        End Function

        ''' <summary>
        ''' Gets the chain of block indices from genesis to tip.
        ''' </summary>
        Public Function GetChain() As List(Of BlockIndex)
            SyncLock _syncLock
                Dim chain As New List(Of BlockIndex)()
                Dim current As BlockIndex = _tip
                While current IsNot Nothing
                    chain.Insert(0, current)
                    If String.IsNullOrEmpty(current.PreviousHash) Then Exit While
                    current = GetIndex(current.PreviousHash)
                End While
                Return chain
            End SyncLock
        End Function

        ''' <summary>
        ''' Finds the common ancestor between two block indices.
        ''' </summary>
        Public Function FindFork(index1 As BlockIndex, index2 As BlockIndex) As BlockIndex
            If index1 Is Nothing OrElse index2 Is Nothing Then Return Nothing

            SyncLock _syncLock
                Dim a As BlockIndex = index1
                Dim b As BlockIndex = index2

                ' Bring both to the same height
                While a.Height > b.Height
                    a = GetIndex(a.PreviousHash)
                End While
                While b.Height > a.Height
                    b = GetIndex(b.PreviousHash)
                End While

                ' Walk back until they meet
                While a IsNot Nothing AndAlso b IsNot Nothing AndAlso a.Hash <> b.Hash
                    a = GetIndex(a.PreviousHash)
                    b = GetIndex(b.PreviousHash)
                End While

                Return a
            End SyncLock
        End Function

    End Class

End Namespace
