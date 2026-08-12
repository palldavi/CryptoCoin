Imports CryptoCoin.Cryptography

Namespace CryptoCoin.Wallet

    ''' <summary>
    ''' Configuration settings for a CryptoCoin wallet instance.
    ''' Controls network selection, fee policies, and derivation parameters.
    ''' </summary>
    Public Class WalletConfig

        ''' <summary>
        ''' The network type for this wallet (Mainnet or Testnet).
        ''' </summary>
        Public Property Network As NetworkType = NetworkType.Mainnet

        ''' <summary>
        ''' The default transaction fee in satoshis per byte.
        ''' </summary>
        Public Property DefaultFeePerByte As Long = 10L

        ''' <summary>
        ''' The minimum transaction fee in satoshis.
        ''' </summary>
        Public Property MinimumFee As Long = 1000L

        ''' <summary>
        ''' The maximum transaction fee in satoshis (safety limit).
        ''' </summary>
        Public Property MaximumFee As Long = 10000000L

        ''' <summary>
        ''' The gap limit for address discovery (BIP44 standard is 20).
        ''' </summary>
        Public Property GapLimit As Integer = 20

        ''' <summary>
        ''' The number of confirmations required to consider a transaction confirmed.
        ''' </summary>
        Public Property RequiredConfirmations As Integer = 6

        ''' <summary>
        ''' The number of confirmations required for coinbase maturity.
        ''' </summary>
        Public Property CoinbaseMaturity As Integer = 100

        ''' <summary>
        ''' The BIP44 coin type for CryptoCoin (imaginary coin type 999).
        ''' </summary>
        Public Property CoinType As Integer = 999

        ''' <summary>
        ''' Whether to use compressed public keys for address generation.
        ''' </summary>
        Public Property UseCompressedKeys As Boolean = True

        ''' <summary>
        ''' The wallet file path for persistence.
        ''' </summary>
        Public Property WalletFilePath As String = "wallet.dat"

        ''' <summary>
        ''' Whether to automatically save the wallet after changes.
        ''' </summary>
        Public Property AutoSave As Boolean = True

        ''' <summary>
        ''' The PBKDF2 iteration count for key derivation from password.
        ''' </summary>
        Public Property Pbkdf2Iterations As Integer = 100000

        ''' <summary>
        ''' Gets the address version byte based on the current network.
        ''' </summary>
        Public ReadOnly Property AddressVersion As Byte
            Get
                If Network = NetworkType.Mainnet Then
                    Return AddressEncoder.MainnetP2PKH
                Else
                    Return AddressEncoder.TestnetP2PKH
                End If
            End Get
        End Property

        ''' <summary>
        ''' Creates a default wallet configuration for mainnet.
        ''' </summary>
        Public Shared Function CreateDefault() As WalletConfig
            Return New WalletConfig()
        End Function

        ''' <summary>
        ''' Creates a wallet configuration for testnet with relaxed parameters.
        ''' </summary>
        Public Shared Function CreateTestnet() As WalletConfig
            Dim config As New WalletConfig()
            config.Network = NetworkType.Testnet
            config.RequiredConfirmations = 1
            config.DefaultFeePerByte = 1L
            config.MinimumFee = 100L
            Return config
        End Function

    End Class

    ''' <summary>
    ''' Represents the network type for the wallet.
    ''' </summary>
    Public Enum NetworkType
        ''' <summary>Main production network.</summary>
        Mainnet = 0
        ''' <summary>Test network for development.</summary>
        Testnet = 1
    End Enum

End Namespace
