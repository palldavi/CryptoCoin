Imports System.Collections.Generic
Imports System.Runtime.Serialization

Namespace CryptoCoin.Services.DataContracts

    ''' <summary>WCF data contract for mempool information.</summary>
    <DataContract(Namespace:="http://cryptocoin.services/2024/blockchain")>
    Public Class MempoolData
        <DataMember(Order:=1)> Public Property TransactionCount As Integer
        <DataMember(Order:=2)> Public Property TotalBytes As Long
        <DataMember(Order:=3)> Public Property TotalFees As Long
        <DataMember(Order:=4)> Public Property Transactions As List(Of MempoolEntryData)

        Public Sub New()
            Transactions = New List(Of MempoolEntryData)()
        End Sub
    End Class

    ''' <summary>WCF data contract for a single mempool entry.</summary>
    <DataContract(Namespace:="http://cryptocoin.services/2024/blockchain")>
    Public Class MempoolEntryData
        <DataMember(Order:=1)> Public Property TxId As String
        <DataMember(Order:=2)> Public Property Fee As Long
        <DataMember(Order:=3)> Public Property Size As Integer
        <DataMember(Order:=4)> Public Property FeeRate As Double
    End Class

End Namespace
