Imports System.Data.SQLite
Imports System.Numerics
Imports CryptoCoin.Core

Namespace CryptoCoin.Persistence

    ''' <summary>
    ''' SQLite-backed implementation of ChainState.
    ''' Persists the block index and chain tip so the node can resume
    ''' from where it left off after a restart.
    ''' </summary>
    Public Class SqliteChainState
        Inherits ChainState

        Private ReadOnly _db As Database
        Private ReadOnly _blockStore As SqliteBlockStore
        Private ReadOnly _syncLock As New Object()

        Public Sub New(db As Database, blockStore As SqliteBlockStore)
            If db Is Nothing Then Throw New ArgumentNullException(NameOf(db))
            If blockStore Is Nothing Then Throw New ArgumentNullException(NameOf(blockStore))
            _db = db
            _blockStore = blockStore
        End Sub

        ''' <summary>
        ''' Adds a block index and persists the total_work back to the blocks table.
        ''' </summary>
        Public Overrides Sub AddIndex(index As BlockIndex)
            MyBase.AddIndex(index)
            PersistIndex(index)
        End Sub

        ''' <summary>
        ''' Sets the chain tip and persists it to the chain_tip table.
        ''' </summary>
        Public Overrides Sub SetTip(index As BlockIndex)
            MyBase.SetTip(index)
            PersistIndex(index)
            PersistTip(index.Hash)
        End Sub

        ''' <summary>
        ''' Loads all block indices from SQLite into the in-memory state.
        ''' Also restores the chain tip. Called once on startup.
        ''' </summary>
        Public Sub LoadAll()
            ' Load all block index rows
            Dim sql As String = "SELECT hash, height, prev_hash, timestamp, bits, tx_count, total_work, is_main FROM blocks ORDER BY height ASC"
            Using cmd As New SQLiteCommand(sql, _db.Connection)
            Using reader As SQLiteDataReader = cmd.ExecuteReader()
                While reader.Read()
                    Dim idx As New BlockIndex()
                    idx.Hash           = reader.GetString(0)
                    idx.Height         = reader.GetInt32(1)
                    idx.PreviousHash   = reader.GetString(2)
                    idx.Timestamp      = reader.GetInt64(3)
                    idx.Bits           = CUInt(reader.GetInt64(4))
                    idx.TransactionCount = reader.GetInt32(5)
                    idx.TotalWork      = ParseBigInteger(reader.GetString(6))
                    idx.IsMainChain    = (reader.GetInt32(7) = 1)
                    idx.Status         = BlockStatus.Valid
                    MyBase.AddIndex(idx)
                End While
            End Using
            End Using

            ' Restore tip
            Dim tipHash As String = GetPersistedTipHash()
            If Not String.IsNullOrEmpty(tipHash) Then
                Dim tipIndex As BlockIndex = MyBase.GetIndex(tipHash)
                If tipIndex IsNot Nothing Then
                    MyBase.SetTip(tipIndex)
                End If
            End If
        End Sub

        ''' <summary>Gets the persisted tip hash from the chain_tip table.</summary>
        Public Function GetPersistedTipHash() As String
            Using cmd As New SQLiteCommand("SELECT tip_hash FROM chain_tip WHERE id = 1", _db.Connection)
                Dim result As Object = cmd.ExecuteScalar()
                If result Is Nothing OrElse result Is DBNull.Value Then Return Nothing
                Return result.ToString()
            End Using
        End Function

        ' ── Private helpers ──────────────────────────────────────────────────

        Private Sub PersistIndex(index As BlockIndex)
            ' Update total_work and is_main on the blocks row
            _blockStore.UpdateBlockIndex(index.Hash, index.TotalWork, index.IsMainChain)
        End Sub

        Private Sub PersistTip(hash As String)
            Dim sql As String = "INSERT OR REPLACE INTO chain_tip (id, tip_hash) VALUES (1, @hash)"
            Using cmd As New SQLiteCommand(sql, _db.Connection)
                cmd.Parameters.AddWithValue("@hash", hash)
                cmd.ExecuteNonQuery()
            End Using
        End Sub

        Private Shared Function ParseBigInteger(s As String) As BigInteger
            If String.IsNullOrEmpty(s) Then Return BigInteger.Zero
            Dim result As BigInteger
            If BigInteger.TryParse(s, result) Then Return result
            Return BigInteger.Zero
        End Function

    End Class

End Namespace
