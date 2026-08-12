Imports System.Runtime.Serialization

Namespace CryptoCoin.Services.DataContracts

    ''' <summary>WCF data contract for wallet creation request.</summary>
    <DataContract(Namespace:="http://cryptocoin.services/2024/wallet")>
    Public Class CreateWalletRequest
        <DataMember(Order:=1)> Public Property WalletName As String
        <DataMember(Order:=2)> Public Property Passphrase As String
    End Class

    ''' <summary>WCF data contract for wallet creation response.</summary>
    <DataContract(Namespace:="http://cryptocoin.services/2024/wallet")>
    Public Class CreateWalletResponse
        <DataMember(Order:=1)> Public Property WalletId As String
        <DataMember(Order:=2)> Public Property MnemonicPhrase As String
        <DataMember(Order:=3)> Public Property FirstAddress As String
        <DataMember(Order:=4)> Public Property Success As Boolean
        <DataMember(Order:=5)> Public Property ErrorMessage As String
    End Class

    ''' <summary>WCF data contract for balance response.</summary>
    <DataContract(Namespace:="http://cryptocoin.services/2024/wallet")>
    Public Class BalanceResponse
        <DataMember(Order:=1)> Public Property Address As String
        <DataMember(Order:=2)> Public Property ConfirmedBalance As Long
        <DataMember(Order:=3)> Public Property UnconfirmedBalance As Long
        <DataMember(Order:=4)> Public Property TotalBalance As Long
    End Class

    ''' <summary>WCF data contract for address generation response.</summary>
    <DataContract(Namespace:="http://cryptocoin.services/2024/wallet")>
    Public Class NewAddressResponse
        <DataMember(Order:=1)> Public Property Address As String
        <DataMember(Order:=2)> Public Property DerivationPath As String
    End Class

    ''' <summary>WCF data contract for send transaction request.</summary>
    <DataContract(Namespace:="http://cryptocoin.services/2024/wallet")>
    Public Class SendTransactionRequest
        <DataMember(Order:=1)> Public Property FromAddress As String
        <DataMember(Order:=2)> Public Property ToAddress As String
        <DataMember(Order:=3)> Public Property AmountSatoshis As Long
        <DataMember(Order:=4)> Public Property FeeSatoshis As Long
    End Class

    ''' <summary>WCF data contract for send transaction response.</summary>
    <DataContract(Namespace:="http://cryptocoin.services/2024/wallet")>
    Public Class SendTransactionResponse
        <DataMember(Order:=1)> Public Property TxId As String
        <DataMember(Order:=2)> Public Property Success As Boolean
        <DataMember(Order:=3)> Public Property ErrorMessage As String
    End Class

End Namespace
