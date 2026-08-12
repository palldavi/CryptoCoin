Imports System.ServiceModel
Imports CryptoCoin.Services.Contracts
Imports CryptoCoin.Services.DataContracts

Namespace CryptoCoin.Services.Implementations

    ''' <summary>
    ''' WCF service implementation for wallet operations.
    ''' Returns stub responses — a full implementation would wire into
    ''' CryptoCoin.Wallet.WalletManager.
    ''' </summary>
    <ServiceBehavior(InstanceContextMode:=InstanceContextMode.Single,
                     ConcurrencyMode:=ConcurrencyMode.Multiple)>
    Public Class WalletServiceImpl
        Implements IWalletService

        Public Function CreateWallet(request As CreateWalletRequest) As CreateWalletResponse _
               Implements IWalletService.CreateWallet
            ' Stub: in a full implementation this would call WalletManager.CreateWallet
            Return New CreateWalletResponse() With {
                .WalletId = Guid.NewGuid().ToString("N"),
                .MnemonicPhrase = "carpet canvas case cash can call cannon casino axis about card cash",
                .FirstAddress = "CJTZijYXJ4n3XisgX2jVioSWtcThfL31PC",
                .Success = True
            }
        End Function

        Public Function GetBalance(address As String) As BalanceResponse _
               Implements IWalletService.GetBalance
            Return New BalanceResponse() With {
                .Address = address,
                .ConfirmedBalance = 0,
                .UnconfirmedBalance = 0,
                .TotalBalance = 0
            }
        End Function

        Public Function GetNewAddress(walletId As String) As NewAddressResponse _
               Implements IWalletService.GetNewAddress
            Return New NewAddressResponse() With {
                .Address = "CJTZijYXJ4n3XisgX2jVioSWtcThfL31PC",
                .DerivationPath = "m/44'/999'/0'/0/0"
            }
        End Function

        Public Function SendTransaction(request As SendTransactionRequest) As SendTransactionResponse _
               Implements IWalletService.SendTransaction
            Return New SendTransactionResponse() With {
                .Success = False,
                .ErrorMessage = "SendTransaction not yet implemented. Wire into CryptoCoin.Wallet.WalletManager."
            }
        End Function

    End Class

End Namespace
