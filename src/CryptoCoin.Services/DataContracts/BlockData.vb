Imports System.Collections.Generic
Imports System.Runtime.Serialization

Namespace CryptoCoin.Services.DataContracts

    ''' <summary>
    ''' WCF data contract for block information.
    ''' Modernisation note: on .NET 10 this would be a plain record or class
    ''' serialised by System.Text.Json rather than DataContractSerializer.
    ''' </summary>
    <DataContract(Namespace:="http://cryptocoin.services/2024/blockchain")>
    Public Class BlockData

        <DataMember(Order:=1)> Public Property Hash As String
        <DataMember(Order:=2)> Public Property Height As Integer
        <DataMember(Order:=3)> Public Property PreviousHash As String
        <DataMember(Order:=4)> Public Property MerkleRoot As String
        <DataMember(Order:=5)> Public Property Timestamp As Long
        <DataMember(Order:=6)> Public Property Bits As Long
        <DataMember(Order:=7)> Public Property Nonce As Long
        <DataMember(Order:=8)> Public Property TransactionCount As Integer
        <DataMember(Order:=9)> Public Property Size As Integer
        <DataMember(Order:=10)> Public Property TransactionIds As List(Of String)

        Public Sub New()
            TransactionIds = New List(Of String)()
        End Sub

    End Class

    ''' <summary>WCF data contract for a block list response.</summary>
    <DataContract(Namespace:="http://cryptocoin.services/2024/blockchain")>
    Public Class BlockListData
        <DataMember(Order:=1)> Public Property Blocks As List(Of BlockData)
        <DataMember(Order:=2)> Public Property TotalCount As Integer

        Public Sub New()
            Blocks = New List(Of BlockData)()
        End Sub
    End Class

End Namespace
