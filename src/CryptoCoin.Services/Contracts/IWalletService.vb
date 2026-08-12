Imports System.ServiceModel
Imports CryptoCoin.Services.DataContracts

Namespace CryptoCoin.Services.Contracts

    ''' <summary>
    ''' WCF service contract exposing wallet operations.
    ''' All operations require a valid API key in the custom SOAP header.
    ''' </summary>
    <ServiceContract(Namespace:="http://cryptocoin.services/2024/wallet",
                     Name:="IWalletService")>
    Public Interface IWalletService

        ''' <summary>Creates a new HD wallet and returns the mnemonic phrase.</summary>
        <OperationContract()>
        Function CreateWallet(request As CreateWalletRequest) As CreateWalletResponse

        ''' <summary>Returns the balance for a given address.</summary>
        <OperationContract()>
        Function GetBalance(address As String) As BalanceResponse

        ''' <summary>Generates a new receiving address.</summary>
        <OperationContract()>
        Function GetNewAddress(walletId As String) As NewAddressResponse

        ''' <summary>Broadcasts a transaction to the network.</summary>
        <OperationContract()>
        Function SendTransaction(request As SendTransactionRequest) As SendTransactionResponse

    End Interface

End Namespace
