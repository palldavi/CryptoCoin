Imports CryptoCoin.Core

Namespace CryptoCoin.Explorer.Controllers

    ''' <summary>
    ''' Handles address-related API requests.
    ''' </summary>
    Public Class AddressController

        Private ReadOnly _blockchain As Blockchain

        Public Sub New(blockchain As Blockchain)
            _blockchain = blockchain
        End Sub

        ''' <summary>
        ''' Gets information about an address (transaction history from blocks).
        ''' </summary>
        Public Function GetAddressInfo(address As String) As String
            Dim txCount As Integer = 0
            Dim blockRefs As New List(Of String)()

            ' Scan blocks for transactions referencing this address
            ' (simplified - in production would use an address index)
            For h As Integer = 0 To _blockchain.Height
                Dim block As Block = _blockchain.GetBlockByHeight(h)
                If block Is Nothing Then Continue For

                For Each txId As String In block.TransactionIds
                    ' In a full implementation, we would deserialize the tx
                    ' and check inputs/outputs for the address
                    txCount += 0 ' Placeholder - would need UTXO index
                Next
            Next

            Dim props As New List(Of String)()
            props.Add(JsonSerializer.PropStr("address", address))
            props.Add(JsonSerializer.PropInt("txCount", txCount))
            props.Add(JsonSerializer.PropLong("balance", 0))
            props.Add(JsonSerializer.Prop("transactions", JsonSerializer.CreateArray(blockRefs)))

            Return JsonSerializer.CreateObject(props.ToArray())
        End Function

    End Class

End Namespace
