Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports CryptoCoin.Cryptography

Namespace CryptoCoin.Tests.Cryptography

    <TestClass>
    Public Class HdKeyTests

        Private Function GetMasterKey() As HdKeyDerivation.ExtendedKey
            Dim m As New Mnemonic(12)
            Dim seed As Byte() = m.ToSeed()
            Return HdKeyDerivation.MasterKeyFromSeed(seed)
        End Function

        <TestMethod>
        Public Sub MasterKeyFromSeed_Returns64ByteSeed_Succeeds()
            Dim seed(63) As Byte
            For i As Integer = 0 To 63
                seed(i) = CByte(i)
            Next
            Dim master As HdKeyDerivation.ExtendedKey = HdKeyDerivation.MasterKeyFromSeed(seed)
            Assert.IsNotNull(master)
            Assert.IsTrue(master.IsPrivate)
        End Sub

        <TestMethod>
        Public Sub MasterKeyFromSeed_KeyDataIs32Bytes()
            Dim master As HdKeyDerivation.ExtendedKey = GetMasterKey()
            Assert.AreEqual(32, master.KeyData.Length)
        End Sub

        <TestMethod>
        Public Sub MasterKeyFromSeed_ChainCodeIs32Bytes()
            Dim master As HdKeyDerivation.ExtendedKey = GetMasterKey()
            Assert.AreEqual(32, master.ChainCode.Length)
        End Sub

        <TestMethod>
        Public Sub MasterKeyFromSeed_DepthIsZero()
            Dim master As HdKeyDerivation.ExtendedKey = GetMasterKey()
            Assert.AreEqual(CByte(0), master.Depth)
        End Sub

        <TestMethod>
        Public Sub MasterKeyFromSeed_SameSeed_SameKey()
            Dim seed(31) As Byte
            For i As Integer = 0 To 31
                seed(i) = CByte(i + 1)
            Next
            Dim k1 As HdKeyDerivation.ExtendedKey = HdKeyDerivation.MasterKeyFromSeed(seed)
            Dim k2 As HdKeyDerivation.ExtendedKey = HdKeyDerivation.MasterKeyFromSeed(seed)
            AssertBytesEqual(k1.KeyData, k2.KeyData)
            AssertBytesEqual(k1.ChainCode, k2.ChainCode)
        End Sub

        <TestMethod>
        <ExpectedException(GetType(ArgumentNullException))>
        Public Sub MasterKeyFromSeed_NullSeed_Throws()
            HdKeyDerivation.MasterKeyFromSeed(Nothing)
        End Sub

        <TestMethod>
        <ExpectedException(GetType(ArgumentException))>
        Public Sub MasterKeyFromSeed_TooShortSeed_Throws()
            HdKeyDerivation.MasterKeyFromSeed(New Byte() {1, 2, 3})
        End Sub

        <TestMethod>
        Public Sub DeriveChild_NormalIndex_Succeeds()
            Dim master As HdKeyDerivation.ExtendedKey = GetMasterKey()
            Dim child As HdKeyDerivation.ExtendedKey = HdKeyDerivation.DeriveChild(master, 0)
            Assert.IsNotNull(child)
            Assert.IsTrue(child.IsPrivate)
        End Sub

        <TestMethod>
        Public Sub DeriveChild_DepthIncrements()
            Dim master As HdKeyDerivation.ExtendedKey = GetMasterKey()
            Dim child As HdKeyDerivation.ExtendedKey = HdKeyDerivation.DeriveChild(master, 0)
            Assert.AreEqual(CByte(1), child.Depth)
        End Sub

        <TestMethod>
        Public Sub DeriveChild_DifferentIndices_DifferentKeys()
            Dim master As HdKeyDerivation.ExtendedKey = GetMasterKey()
            Dim child0 As HdKeyDerivation.ExtendedKey = HdKeyDerivation.DeriveChild(master, 0)
            Dim child1 As HdKeyDerivation.ExtendedKey = HdKeyDerivation.DeriveChild(master, 1)
            Assert.IsFalse(HashUtil.ConstantTimeEquals(child0.KeyData, child1.KeyData))
        End Sub

        <TestMethod>
        Public Sub DeriveChild_HardenedIndex_Succeeds()
            Dim master As HdKeyDerivation.ExtendedKey = GetMasterKey()
            Dim hardened As HdKeyDerivation.ExtendedKey = HdKeyDerivation.DeriveChild(master, &H80000000UI)
            Assert.IsNotNull(hardened)
        End Sub

        <TestMethod>
        Public Sub DerivePath_SimpleDepth1_Succeeds()
            Dim master As HdKeyDerivation.ExtendedKey = GetMasterKey()
            Dim child As HdKeyDerivation.ExtendedKey = HdKeyDerivation.DerivePath(master, "m/0")
            Assert.IsNotNull(child)
            Assert.AreEqual(CByte(1), child.Depth)
        End Sub

        <TestMethod>
        Public Sub DerivePath_HardenedPath_Succeeds()
            Dim master As HdKeyDerivation.ExtendedKey = GetMasterKey()
            Dim child As HdKeyDerivation.ExtendedKey = HdKeyDerivation.DerivePath(master, "m/44'/999'/0'")
            Assert.IsNotNull(child)
            Assert.AreEqual(CByte(3), child.Depth)
        End Sub

        <TestMethod>
        Public Sub DerivePath_FullBip44Path_Succeeds()
            Dim master As HdKeyDerivation.ExtendedKey = GetMasterKey()
            Dim child As HdKeyDerivation.ExtendedKey = HdKeyDerivation.DerivePath(master, "m/44'/999'/0'/0/0")
            Assert.IsNotNull(child)
            Assert.AreEqual(CByte(5), child.Depth)
        End Sub

        <TestMethod>
        Public Sub DerivePath_SamePath_SameKey()
            Dim master As HdKeyDerivation.ExtendedKey = GetMasterKey()
            Dim k1 As HdKeyDerivation.ExtendedKey = HdKeyDerivation.DerivePath(master, "m/44'/999'/0'/0/0")
            Dim k2 As HdKeyDerivation.ExtendedKey = HdKeyDerivation.DerivePath(master, "m/44'/999'/0'/0/0")
            AssertBytesEqual(k1.KeyData, k2.KeyData)
        End Sub

        <TestMethod>
        Public Sub DerivePath_DifferentPaths_DifferentKeys()
            Dim master As HdKeyDerivation.ExtendedKey = GetMasterKey()
            Dim k1 As HdKeyDerivation.ExtendedKey = HdKeyDerivation.DerivePath(master, "m/44'/999'/0'/0/0")
            Dim k2 As HdKeyDerivation.ExtendedKey = HdKeyDerivation.DerivePath(master, "m/44'/999'/0'/0/1")
            Assert.IsFalse(HashUtil.ConstantTimeEquals(k1.KeyData, k2.KeyData))
        End Sub

        <TestMethod>
        Public Sub GetKeyPair_FromDerivedKey_ProducesValidAddress()
            Dim master As HdKeyDerivation.ExtendedKey = GetMasterKey()
            Dim child As HdKeyDerivation.ExtendedKey = HdKeyDerivation.DerivePath(master, "m/44'/999'/0'/0/0")
            Dim kp As KeyPair = child.GetKeyPair()
            Dim address As String = AddressEncoder.FromKeyPair(kp)
            Assert.IsTrue(AddressEncoder.IsValid(address))
        End Sub

        <TestMethod>
        Public Sub Serialize_StartsWithXprv()
            Dim master As HdKeyDerivation.ExtendedKey = GetMasterKey()
            Dim serialized As String = master.Serialize()
            Assert.IsTrue(serialized.StartsWith("xprv"),
                $"Private extended key should serialize to xprv..., got: {serialized.Substring(0, 4)}")
        End Sub

        <TestMethod>
        Public Sub GetCryptoCoinPath_DefaultArgs_ReturnsExpectedPath()
            Dim path As String = HdKeyDerivation.GetCryptoCoinPath()
            Assert.AreEqual("m/44'/999'/0'/0/0", path)
        End Sub

        <TestMethod>
        Public Sub GetCryptoCoinPath_CustomArgs_ReturnsExpectedPath()
            Dim path As String = HdKeyDerivation.GetCryptoCoinPath(account:=1, change:=0, addressIndex:=5)
            Assert.AreEqual("m/44'/999'/1'/0/5", path)
        End Sub

    End Class

End Namespace
