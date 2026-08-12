Imports CryptoCoin.Core

Namespace CryptoCoin.Persistence

    ''' <summary>
    ''' Creates and wires up the SQLite persistence components.
    ''' Returns a fully-loaded Blockchain backed by SQLite storage.
    ''' </summary>
    Public Class PersistenceFactory

        ''' <summary>
        ''' Opens (or creates) the SQLite database in the given directory and
        ''' returns a Blockchain whose BlockStore and ChainState are SQLite-backed.
        ''' Any previously persisted blocks are loaded back into memory automatically.
        ''' </summary>
        ''' <param name="dataDirectory">Directory where blockchain.db will be stored.</param>
        ''' <param name="params">Chain parameters (mainnet / testnet / regtest).</param>
        ''' <returns>A ready-to-use Blockchain instance.</returns>
        Public Shared Function CreateBlockchain(dataDirectory As String,
                                                params As ChainParameters) As Blockchain
            ' Open database
            Dim db As New Database(dataDirectory)
            db.Open()

            ' Create SQLite-backed store and state
            Dim blockStore As New SqliteBlockStore(db)
            Dim chainState As New SqliteChainState(db, blockStore)

            ' Check if we have persisted data
            Dim persistedCount As Integer = blockStore.GetPersistedCount()

            If persistedCount > 0 Then
                ' Resume from persisted chain
                Console.WriteLine($"[Persistence] Loading {persistedCount} block(s) from {db.FilePath}...")
                blockStore.LoadAll()
                chainState.LoadAll()
                Console.WriteLine($"[Persistence] Chain restored to height {chainState.Tip?.Height}.")

                ' Create blockchain with pre-loaded store and state
                Return New Blockchain(params, blockStore, chainState)
            Else
                ' Fresh chain — create blockchain normally (genesis block will be added)
                Console.WriteLine($"[Persistence] New chain. Database: {db.FilePath}")
                Dim blockchain As New Blockchain(params, blockStore, chainState)
                Return blockchain
            End If
        End Function

    End Class

End Namespace
