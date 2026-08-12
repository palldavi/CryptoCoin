Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports CryptoCoin.Wallet
Imports CryptoCoin.Cryptography

Namespace CryptoCoin.Tests.Wallet

    <TestClass>
    Public Class KeyStoreTests

        Private Const TestPassword As String = "SecurePassword42!"

        <TestMethod>
        Public Sub New_IsLocked()
            Dim store As New KeyStore()
            Assert.IsFalse(store.IsUnlocked)
        End Sub

        <TestMethod>
        Public Sub New_KeyCountIsZero()
            Dim store As New KeyStore()
            Assert.AreEqual(0, store.KeyCount)
        End Sub

        <TestMethod>
        Public Sub Unlock_SetsIsUnlockedTrue()
            Dim store As New KeyStore()
            store.Unlock(TestPassword)
            Assert.IsTrue(store.IsUnlocked)
        End Sub

        <TestMethod>
        Public Sub Lock_SetsIsUnlockedFalse()
            Dim store As New KeyStore()
            store.Unlock(TestPassword)
            store.Lock()
            Assert.IsFalse(store.IsUnlocked)
        End Sub

        <TestMethod>
        Public Sub StoreKey_IncreasesKeyCount()
            Dim store As New KeyStore()
            store.Unlock(TestPassword)
            Dim kp As New KeyPair()
            store.StoreKey("key1", kp.PrivateKeyBytes)
            Assert.AreEqual(1, store.KeyCount)
        End Sub

        <TestMethod>
        Public Sub RetrieveKey_AfterStore_ReturnsOriginalKey()
            Dim store As New KeyStore()
            store.Unlock(TestPassword)
            Dim kp As New KeyPair()
            store.StoreKey("mykey", kp.PrivateKeyBytes)
            Dim retrieved As Byte() = store.RetrieveKey("mykey")
            AssertBytesEqual(kp.PrivateKeyBytes, retrieved)
        End Sub

        <TestMethod>
        Public Sub RetrieveKey_NonExistent_ReturnsNothing()
            Dim store As New KeyStore()
            store.Unlock(TestPassword)
            Dim result As Byte() = store.RetrieveKey("doesnotexist")
            Assert.IsNull(result)
        End Sub

        <TestMethod>
        Public Sub ContainsKey_AfterStore_ReturnsTrue()
            Dim store As New KeyStore()
            store.Unlock(TestPassword)
            Dim kp As New KeyPair()
            store.StoreKey("testkey", kp.PrivateKeyBytes)
            Assert.IsTrue(store.ContainsKey("testkey"))
        End Sub

        <TestMethod>
        Public Sub ContainsKey_NotStored_ReturnsFalse()
            Dim store As New KeyStore()
            store.Unlock(TestPassword)
            Assert.IsFalse(store.ContainsKey("missing"))
        End Sub

        <TestMethod>
        Public Sub RemoveKey_DecreasesKeyCount()
            Dim store As New KeyStore()
            store.Unlock(TestPassword)
            Dim kp As New KeyPair()
            store.StoreKey("removeMe", kp.PrivateKeyBytes)
            store.RemoveKey("removeMe")
            Assert.AreEqual(0, store.KeyCount)
        End Sub

        <TestMethod>
        Public Sub RemoveKey_KeyNoLongerRetrievable()
            Dim store As New KeyStore()
            store.Unlock(TestPassword)
            Dim kp As New KeyPair()
            store.StoreKey("gone", kp.PrivateKeyBytes)
            store.RemoveKey("gone")
            Assert.IsNull(store.RetrieveKey("gone"))
        End Sub

        <TestMethod>
        <ExpectedException(GetType(InvalidOperationException))>
        Public Sub StoreKey_WhenLocked_Throws()
            Dim store As New KeyStore()
            ' Not unlocked
            Dim kp As New KeyPair()
            store.StoreKey("key", kp.PrivateKeyBytes)
        End Sub

        <TestMethod>
        <ExpectedException(GetType(InvalidOperationException))>
        Public Sub RetrieveKey_WhenLocked_Throws()
            Dim store As New KeyStore()
            store.Unlock(TestPassword)
            Dim kp As New KeyPair()
            store.StoreKey("key", kp.PrivateKeyBytes)
            store.Lock()
            store.RetrieveKey("key")
        End Sub

        <TestMethod>
        Public Sub VerifyPassword_CorrectPassword_ReturnsTrue()
            Dim store As New KeyStore()
            store.Unlock(TestPassword)
            Dim kp As New KeyPair()
            store.StoreKey("k", kp.PrivateKeyBytes)
            Assert.IsTrue(store.VerifyPassword(TestPassword))
        End Sub

        <TestMethod>
        Public Sub VerifyPassword_WrongPassword_ReturnsFalse()
            Dim store As New KeyStore()
            store.Unlock(TestPassword)
            Dim kp As New KeyPair()
            store.StoreKey("k", kp.PrivateKeyBytes)
            Assert.IsFalse(store.VerifyPassword("WrongPassword"))
        End Sub

        <TestMethod>
        Public Sub StoreKeyPair_RetrieveKeyPair_Roundtrip()
            Dim store As New KeyStore()
            store.Unlock(TestPassword)
            Dim kp1 As New KeyPair()
            store.StoreKeyPair("mykp", kp1)
            Dim kp2 As KeyPair = store.RetrieveKeyPair("mykp")
            Assert.IsNotNull(kp2)
            AssertBytesEqual(kp1.PrivateKeyBytes, kp2.PrivateKeyBytes)
        End Sub

        <TestMethod>
        Public Sub GetAllIdentifiers_ReturnsAllKeys()
            Dim store As New KeyStore()
            store.Unlock(TestPassword)
            Dim kp As New KeyPair()
            store.StoreKey("a", kp.PrivateKeyBytes)
            store.StoreKey("b", kp.PrivateKeyBytes)
            store.StoreKey("c", kp.PrivateKeyBytes)
            Dim ids As List(Of String) = store.GetAllIdentifiers()
            Assert.AreEqual(3, ids.Count)
        End Sub

        <TestMethod>
        Public Sub ChangePassword_NewPasswordWorks()
            Dim store As New KeyStore()
            store.Unlock(TestPassword)
            Dim kp As New KeyPair()
            store.StoreKey("key", kp.PrivateKeyBytes)
            store.ChangePassword("NewPassword456!")
            ' Old password should no longer work
            Assert.IsFalse(store.VerifyPassword(TestPassword))
            ' New password should work
            Assert.IsTrue(store.VerifyPassword("NewPassword456!"))
        End Sub

        <TestMethod>
        Public Sub ChangePassword_KeysStillAccessible()
            Dim store As New KeyStore()
            store.Unlock(TestPassword)
            Dim kp As New KeyPair()
            store.StoreKey("key", kp.PrivateKeyBytes)
            store.ChangePassword("NewPassword456!")
            Dim retrieved As Byte() = store.RetrieveKey("key")
            AssertBytesEqual(kp.PrivateKeyBytes, retrieved)
        End Sub

    End Class

End Namespace
