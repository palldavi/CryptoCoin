Imports System.Runtime.Serialization

Namespace CryptoCoin.Services.DataContracts

    ''' <summary>WCF data contract for network status information.</summary>
    <DataContract(Namespace:="http://cryptocoin.services/2024/blockchain")>
    Public Class NetworkStatusData
        <DataMember(Order:=1)> Public Property Height As Integer
        <DataMember(Order:=2)> Public Property BestBlockHash As String
        <DataMember(Order:=3)> Public Property BestBlockTime As Long
        <DataMember(Order:=4)> Public Property BlockCount As Integer
        <DataMember(Order:=5)> Public Property MempoolCount As Integer
        <DataMember(Order:=6)> Public Property MempoolBytes As Long
        <DataMember(Order:=7)> Public Property CoinName As String
        <DataMember(Order:=8)> Public Property CoinSymbol As String
        <DataMember(Order:=9)> Public Property Difficulty As Double
        <DataMember(Order:=10)> Public Property HashRate As Double
    End Class

End Namespace
