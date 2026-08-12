Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports CryptoCoin.Wallet
Imports CryptoCoin.Cryptography

Namespace CryptoCoin.Tests.Wallet

    <TestClass>
    Public Class WalletManagerTests

        Private Const TestPassword As String = "TestPassword123!"

        Private Function MakeWallet() As WalletManager
            Dim config As WalletConfig = WalletConfig.CreateDefault()
            Dim wallet As New WalletManager(config)
            wallet.CreateNewWallet(TestPassword)
            Return wallet
        End Function

        <TestMethod>
        Public Sub CreateNewWallet_ReturnsMnemonicPhrase()
            Dim config As WalletConfig = WalletConfig.CreateDefault()
            Dim wallet As New WalletManager(config)
            Dim phrase As String = wallet.CreateNewWallet(TestPassword)
            Assert.IsFalse(String.IsNullOrEmpty(phrase))
        End Sub

        <TestMethod>
        Public Sub CreateNewWallet_MnemonicHas12Words()
            Dim config As WalletConfig = WalletConfig.CreateDefault()
            Dim wallet As New WalletManager(config)
            Dim phrase As String = wallet.CreateNewWallet(TestPassword)
            Assert.AreEqual(12, phrase.Split(" "c).Length)
        End Sub

        <TestMethod>
        Public Sub CreateNewWallet_WalletIsUnlocked()
            Dim wallet As WalletManager = MakeWallet()
            Assert.IsTrue(wallet.IsUnlocked)
        End Sub

        <TestMethod>
        Public Sub CreateNewWallet_CreatesDefaultAccount()
            Dim wallet As WalletManager = MakeWallet()
            Assert.AreEqual(1, wallet.AccountCount)
        End Sub

        <TestMethod>
        Public Sub Lock_WalletBecomesLocked()
            Dim wallet As WalletManager = MakeWallet()
            wallet.Lock()
            Assert.IsFalse(wallet.IsUnlocked)
        End Sub

        <TestMethod>
        Public Sub Unlock_WithCorrectPassword_UnlocksWallet()
            Dim wallet As WalletManager = MakeWallet()
            wallet.Lock()
            wallet.Unlock(TestPassword)
            Assert.IsTrue(wallet.IsUnlocked)
        End Sub

        <TestMethod>
        Public Sub GetReceivingAddress_ReturnsValidAddress()
            Dim wallet As WalletManager = MakeWallet()
            Dim address As String = wallet.GetReceivingAddress()
            Assert.IsTrue(AddressEncoder.IsValid(address),
                $"Address '{address}' should be valid")
        End Sub

        <TestMethod>
        Public Sub GetReceivingAddress_StartsWithC()
            Dim wallet As WalletManager = MakeWallet()
            Dim address As String = wallet.GetReceivingAddress()
            Assert.IsTrue(address.StartsWith("C"),
                $"Mainnet address should start with C, got: {address.Substring(0, 1)}")
        End Sub

        <TestMethod>
        Public Sub GetReceivingAddress_SameWallet_SameAddress()
            Dim wallet As WalletManager = MakeWallet()
            Dim addr1 As String = wallet.GetReceivingAddress()
            Dim addr2 As String = wallet.GetReceivingAddress()
            Assert.AreEqual(addr1, addr2)
        End Sub

        <TestMethod>
        Public Sub GetChangeAddress_ReturnsValidAddress()
            Dim wallet As WalletManager = MakeWallet()
            Dim address As String = wallet.GetChangeAddress()
            Assert.IsTrue(AddressEncoder.IsValid(address))
        End Sub

        <TestMethod>
        Public Sub GetChangeAddress_DifferentFromReceivingAddress()
            Dim wallet As WalletManager = MakeWallet()
            Dim receiving As String = wallet.GetReceivingAddress()
            Dim change As String = wallet.GetChangeAddress()
            Assert.AreNotEqual(receiving, change)
        End Sub

        <TestMethod>
        Public Sub IsMyAddress_OwnAddress_ReturnsTrue()
            Dim wallet As WalletManager = MakeWallet()
            Dim address As String = wallet.GetReceivingAddress()
            Assert.IsTrue(wallet.IsMyAddress(address))
        End Sub

        <TestMethod>
        Public Sub IsMyAddress_ForeignAddress_ReturnsFalse()
            Dim wallet As WalletManager = MakeWallet()
            Dim foreignKp As New KeyPair()
            Dim foreignAddress As String = AddressEncoder.FromKeyPair(foreignKp)
            Assert.IsFalse(wallet.IsMyAddress(foreignAddress))
        End Sub

        <TestMethod>
        Public Sub CreateAccount_IncreasesAccountCount()
            Dim wallet As WalletManager = MakeWallet()
            Dim countBefore As Integer = wallet.AccountCount
            wallet.CreateAccount("Second Account")
            Assert.AreEqual(countBefore + 1, wallet.AccountCount)
        End Sub

        <TestMethod>
        Public Sub GetAllAccounts_ReturnsAllAccounts()
            Dim wallet As WalletManager = MakeWallet()
            wallet.CreateAccount("Account 2")
            Dim accounts As List(Of Account) = wallet.GetAllAccounts()
            Assert.AreEqual(2, accounts.Count)
        End Sub

        <TestMethod>
        Public Sub GetKeyPairForAddress_OwnAddress_ReturnsKeyPair()
            Dim wallet As WalletManager = MakeWallet()
            Dim address As String = wallet.GetReceivingAddress()
            Dim kp As KeyPair = wallet.GetKeyPairForAddress(address)
            Assert.IsNotNull(kp)
        End Sub

        <TestMethod>
        Public Sub GetKeyPairForAddress_KeyPairMatchesAddress()
            Dim wallet As WalletManager = MakeWallet()
            Dim address As String = wallet.GetReceivingAddress()
            Dim kp As KeyPair = wallet.GetKeyPairForAddress(address)
            Dim derivedAddress As String = AddressEncoder.FromKeyPair(kp)
            Assert.AreEqual(address, derivedAddress)
        End Sub

        <TestMethod>
        Public Sub RestoreFromMnemonic_SamePhrase_SameFirstAddress()
            ' Create wallet and get first address
            Dim config As WalletConfig = WalletConfig.CreateDefault()
            Dim wallet1 As New WalletManager(config)
            Dim phrase As String = wallet1.CreateNewWallet(TestPassword)
            Dim address1 As String = wallet1.GetReceivingAddress()

            ' Restore from same mnemonic
            Dim wallet2 As New WalletManager(config)
            wallet2.RestoreFromMnemonic(phrase, TestPassword)
            Dim address2 As String = wallet2.GetReceivingAddress()

            Assert.AreEqual(address1, address2,
                "Restored wallet should produce the same first address")
        End Sub

        <TestMethod>
        <ExpectedException(GetType(ArgumentException))>
        Public Sub RestoreFromMnemonic_InvalidPhrase_Throws()
            Dim config As WalletConfig = WalletConfig.CreateDefault()
            Dim wallet As New WalletManager(config)
            wallet.RestoreFromMnemonic("invalid phrase here", TestPassword)
        End Sub

    End Class

End Namespace
