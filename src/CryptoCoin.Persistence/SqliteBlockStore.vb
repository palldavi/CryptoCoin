Imports System.Data.SQLite
Imports System.Numerics
Imports CryptoCoin.Core
Imports CryptoCoin.Cryptography

Namespace CryptoCoin.Persistence

    ''' <summary>
    ''' SQLite-backed implementation of BlockStore.
    ''' Persists full block data to disk so the chain survives node restarts.
    ''' Replaces the in-memory Dictionary used by the default BlockStore.
    ''' </summary>
    Public Class SqliteBlockStore
        Inherits BlockStore

        Private ReadOnly _db As Database
        Private ReadOnly _syncLock As New Object()

        Public Sub New(db As Database)
            If db Is Nothing Then Throw New ArgumentNullException(NameOf(db))
            _db = db
        End Sub

        ''' <summary>
        ''' Adds a block to the SQLite store.
        ''' If the block already exists the call is a no-op.
        ''' </summary>
        Public Overrides Sub AddBlock(block As Block)
            If block Is Nothing Then Throw New ArgumentNullException(NameOf(block))

            SyncLock _syncLock
                ' Also keep in the in-memory cache (base class dictionary)
                MyBase.AddBlock(block)

                Dim sql As String = "
                    INSERT OR IGNORE INTO blocks
                        (hash, height, prev_hash, timestamp, bits, nonce, version,
                         merkle_root, tx_count, total_work, is_main, raw_block)
                    VALUES
                        (@hash, @height, @prev, @ts, @bits, @nonce, @ver,
                         @merkle, @txcount, @work, 1, @raw)"

                Using cmd As New SQLiteCommand(sql, _db.Connection)
                    cmd.Parameters.AddWithValue("@hash",    block.Hash)
                    cmd.Parameters.AddWithValue("@height",  block.Height)
                    cmd.Parameters.AddWithValue("@prev",    block.Header.PreviousBlockHash)
                    cmd.Parameters.AddWithValue("@ts",      block.Header.Timestamp)
                    cmd.Parameters.AddWithValue("@bits",    CLng(block.Header.Bits))
                    cmd.Parameters.AddWithValue("@nonce",   CLng(block.Header.Nonce))
                    cmd.Parameters.AddWithValue("@ver",     block.Header.Version)
                    cmd.Parameters.AddWithValue("@merkle",  block.Header.MerkleRoot)
                    cmd.Parameters.AddWithValue("@txcount", block.TransactionCount)
                    cmd.Parameters.AddWithValue("@work",    "0")   ' updated by SqliteChainState
                    cmd.Parameters.AddWithValue("@raw",     block.Serialize())
                    cmd.ExecuteNonQuery()
                End Using
            End SyncLock
        End Sub

        ''' <summary>
        ''' Updates the total_work and is_main fields for a block.
        ''' Called by SqliteChainState after computing cumulative work.
        ''' </summary>
        Public Sub UpdateBlockIndex(hash As String, totalWork As BigInteger, isMain As Boolean)
            SyncLock _syncLock
                Dim sql As String = "UPDATE blocks SET total_work = @work, is_main = @main WHERE hash = @hash"
                Using cmd As New SQLiteCommand(sql, _db.Connection)
                    cmd.Parameters.AddWithValue("@work", totalWork.ToString())
                    cmd.Parameters.AddWithValue("@main", If(isMain, 1, 0))
                    cmd.Parameters.AddWithValue("@hash", hash)
                    cmd.ExecuteNonQuery()
                End Using
            End SyncLock
        End Sub

        ''' <summary>
        ''' Loads all persisted blocks from SQLite into the in-memory cache.
        ''' Called once on startup.
        ''' </summary>
        Public Sub LoadAll()
            SyncLock _syncLock
                Dim sql As String = "SELECT raw_block FROM blocks ORDER BY height ASC"
                Using cmd As New SQLiteCommand(sql, _db.Connection)
                Using reader As SQLiteDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        Dim raw As Byte() = CType(reader("raw_block"), Byte())
                        Dim block As Block = DeserializeBlock(raw)
                        If block IsNot Nothing Then
                            ' Load into base class in-memory store without re-persisting
                            MyBase.AddBlock(block)
                        End If
                    End While
                End Using
                End Using
            End SyncLock
        End Sub

        ''' <summary>
        ''' Gets the count from SQLite (authoritative on startup before cache is warm).
        ''' </summary>
        Public Function GetPersistedCount() As Integer
            Using cmd As New SQLiteCommand("SELECT COUNT(*) FROM blocks", _db.Connection)
                Return CInt(cmd.ExecuteScalar())
            End Using
        End Function

        ' ── Deserialization ──────────────────────────────────────────────────

        Private Shared Function DeserializeBlock(raw As Byte()) As Block
            Try
                If raw Is Nothing OrElse raw.Length < 80 Then Return Nothing

                Dim offset As Integer = 0
                Dim header As New BlockHeader()

                ' Version (4 bytes LE)
                header.Version = BitConverter.ToInt32(raw, offset) : offset += 4
                ' PreviousBlockHash (32 bytes)
                header.PreviousBlockHash = HashUtil.ToHex(ReadBytes(raw, offset, 32)) : offset += 32
                ' MerkleRoot (32 bytes)
                header.MerkleRoot = HashUtil.ToHex(ReadBytes(raw, offset, 32)) : offset += 32
                ' Timestamp (4 bytes LE)
                header.Timestamp = CLng(BitConverter.ToUInt32(raw, offset)) : offset += 4
                ' Bits (4 bytes LE)
                header.Bits = BitConverter.ToUInt32(raw, offset) : offset += 4
                ' Nonce (4 bytes LE)
                header.Nonce = BitConverter.ToUInt32(raw, offset) : offset += 4

                ' Transaction count (varint)
                Dim txCount As Long = CLng(Serialization.VarInt.Decode(raw, offset))

                ' Transaction IDs (32 bytes each)
                Dim txIds As New List(Of String)()
                For i As Integer = 0 To CInt(txCount) - 1
                    If offset + 32 > raw.Length Then Exit For
                    txIds.Add(HashUtil.ToHex(ReadBytes(raw, offset, 32)))
                    offset += 32
                Next

                Dim block As New Block(header, txIds)
                Return block
            Catch
                Return Nothing
            End Try
        End Function

        Private Shared Function ReadBytes(data As Byte(), offset As Integer, count As Integer) As Byte()
            Dim result(count - 1) As Byte
            Array.Copy(data, offset, result, 0, count)
            Return result
        End Function

    End Class

End Namespace
