Imports System.Data.SQLite
Imports System.IO

Namespace CryptoCoin.Persistence

    ''' <summary>
    ''' Manages the SQLite database connection and schema creation.
    ''' The database file lives in the node's data directory.
    ''' </summary>
    Public Class Database
        Implements IDisposable

        Private ReadOnly _connectionString As String
        Private _connection As SQLiteConnection
        Private _disposed As Boolean

        ''' <summary>Gets the path to the SQLite database file.</summary>
        Public ReadOnly Property FilePath As String

        Public Sub New(dataDirectory As String, Optional fileName As String = "blockchain.db")
            If Not Directory.Exists(dataDirectory) Then
                Directory.CreateDirectory(dataDirectory)
            End If
            FilePath = Path.Combine(dataDirectory, fileName)
            _connectionString = $"Data Source={FilePath};Version=3;Journal Mode=WAL;Synchronous=Normal;"
        End Sub

        ''' <summary>
        ''' Opens the database connection and creates the schema if it does not exist.
        ''' </summary>
        Public Sub Open()
            _connection = New SQLiteConnection(_connectionString)
            _connection.Open()
            CreateSchema()
        End Sub

        ''' <summary>Gets the open database connection.</summary>
        Public ReadOnly Property Connection As SQLiteConnection
            Get
                Return _connection
            End Get
        End Property

        ''' <summary>
        ''' Creates all tables if they do not already exist.
        ''' </summary>
        Private Sub CreateSchema()
            Dim ddl As String = "
                CREATE TABLE IF NOT EXISTS blocks (
                    hash        TEXT    NOT NULL PRIMARY KEY,
                    height      INTEGER NOT NULL,
                    prev_hash   TEXT    NOT NULL,
                    timestamp   INTEGER NOT NULL,
                    bits        INTEGER NOT NULL,
                    nonce       INTEGER NOT NULL,
                    version     INTEGER NOT NULL,
                    merkle_root TEXT    NOT NULL,
                    tx_count    INTEGER NOT NULL,
                    total_work  TEXT    NOT NULL,
                    is_main     INTEGER NOT NULL DEFAULT 1,
                    raw_block   BLOB    NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_blocks_height ON blocks(height);
                CREATE INDEX IF NOT EXISTS idx_blocks_prev   ON blocks(prev_hash);

                CREATE TABLE IF NOT EXISTS chain_tip (
                    id          INTEGER NOT NULL PRIMARY KEY CHECK (id = 1),
                    tip_hash    TEXT    NOT NULL
                );

                CREATE TABLE IF NOT EXISTS metadata (
                    key         TEXT NOT NULL PRIMARY KEY,
                    value       TEXT NOT NULL
                );
            "
            Using cmd As New SQLiteCommand(ddl, _connection)
                cmd.ExecuteNonQuery()
            End Using
        End Sub

        ''' <summary>Begins a database transaction.</summary>
        Public Function BeginTransaction() As SQLiteTransaction
            Return _connection.BeginTransaction()
        End Function

        ''' <summary>Gets or sets a metadata value.</summary>
        Public Sub SetMetadata(key As String, value As String)
            Dim sql As String = "INSERT OR REPLACE INTO metadata (key, value) VALUES (@k, @v)"
            Using cmd As New SQLiteCommand(sql, _connection)
                cmd.Parameters.AddWithValue("@k", key)
                cmd.Parameters.AddWithValue("@v", value)
                cmd.ExecuteNonQuery()
            End Using
        End Sub

        ''' <summary>Gets a metadata value, or Nothing if not found.</summary>
        Public Function GetMetadata(key As String) As String
            Dim sql As String = "SELECT value FROM metadata WHERE key = @k"
            Using cmd As New SQLiteCommand(sql, _connection)
                cmd.Parameters.AddWithValue("@k", key)
                Dim result As Object = cmd.ExecuteScalar()
                If result Is Nothing OrElse result Is DBNull.Value Then Return Nothing
                Return result.ToString()
            End Using
        End Function

        Public Sub Dispose() Implements IDisposable.Dispose
            If Not _disposed Then
                If _connection IsNot Nothing Then
                    _connection.Close()
                    _connection.Dispose()
                End If
                _disposed = True
            End If
        End Sub

    End Class

End Namespace
