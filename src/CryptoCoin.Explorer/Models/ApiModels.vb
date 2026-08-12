Namespace CryptoCoin.Explorer.Models

    ''' <summary>
    ''' API response model for block information.
    ''' </summary>
    Public Class BlockResponse
        Public Property Hash As String
        Public Property Height As Integer
        Public Property Timestamp As Long
        Public Property TransactionCount As Integer
        Public Property PreviousHash As String
        Public Property MerkleRoot As String
        Public Property Bits As Long
        Public Property Nonce As Long
        Public Property Size As Integer
    End Class

    ''' <summary>
    ''' API response model for transaction information.
    ''' </summary>
    Public Class TransactionResponse
        Public Property TxId As String
        Public Property IsCoinbase As Boolean
        Public Property InputCount As Integer
        Public Property OutputCount As Integer
        Public Property TotalOutput As Long
        Public Property Size As Integer
        Public Property InMempool As Boolean
        Public Property BlockHeight As Integer
        Public Property Confirmations As Integer
    End Class

    ''' <summary>
    ''' API response model for address information.
    ''' </summary>
    Public Class AddressResponse
        Public Property Address As String
        Public Property Balance As Long
        Public Property TxCount As Integer
    End Class

    ''' <summary>
    ''' API response model for network status.
    ''' </summary>
    Public Class NetworkResponse
        Public Property Height As Integer
        Public Property Difficulty As Double
        Public Property HashRate As Double
        Public Property MempoolSize As Integer
        Public Property BlockCount As Integer
        Public Property CoinName As String
        Public Property CoinSymbol As String
    End Class

    ''' <summary>
    ''' API response model for mempool information.
    ''' </summary>
    Public Class MempoolResponse
        Public Property Size As Integer
        Public Property Bytes As Long
        Public Property Fees As Long
    End Class

End Namespace
