Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports CryptoCoin.Cryptography

Namespace CryptoCoin.Tests.Cryptography

    <TestClass>
    Public Class AddressTests

        <TestMethod>
        Public Sub FromKeyPair_ReturnsNonEmptyAddress()
            Dim kp As New KeyPair()
            Dim address As String = AddressEncoder.FromKeyPair(kp)
            Assert.IsFalse(String.IsNullOrEmpty(address))
        End Sub

        <TestMethod>
        Public Sub FromKeyPair_MainnetAddress_StartsWithC()
            ' Version byte 0x1C encodes to 'C' prefix in Base58
            Dim kp As New KeyPair()
            Dim address As String = AddressEncoder.FromKeyPair(kp)
            Assert.IsTrue(address.StartsWith("C"),
                $"Mainnet address should start with 'C', got: {address.Substring(0, 1)}")
        End Sub

        <TestMethod>
        Public Sub FromKeyPair_SameKeyPair_SameAddress()
            Dim privBytes As Byte() = HashUtil.FromHex(KnownPrivateKeyHex)
            Dim kp1 As New KeyPair(privBytes)
            Dim kp2 As New KeyPair(privBytes)
            Assert.AreEqual(AddressEncoder.FromKeyPair(kp1), AddressEncoder.FromKeyPair(kp2))
        End Sub

        <TestMethod>
        Public Sub FromKeyPair_DifferentKeyPairs_DifferentAddresses()
            Dim kp1 As New KeyPair()
            Dim kp2 As New KeyPair()
            Assert.AreNotEqual(AddressEncoder.FromKeyPair(kp1), AddressEncoder.FromKeyPair(kp2))
        End Sub

        <TestMethod>
        Public Sub IsValid_ValidAddress_ReturnsTrue()
            Dim kp As New KeyPair()
            Dim address As String = AddressEncoder.FromKeyPair(kp)
            Assert.IsTrue(AddressEncoder.IsValid(address))
        End Sub

        <TestMethod>
        Public Sub IsValid_EmptyString_ReturnsFalse()
            Assert.IsFalse(AddressEncoder.IsValid(""))
        End Sub

        <TestMethod>
        Public Sub IsValid_NullString_ReturnsFalse()
            Assert.IsFalse(AddressEncoder.IsValid(Nothing))
        End Sub

        <TestMethod>
        Public Sub IsValid_GarbageString_ReturnsFalse()
            Assert.IsFalse(AddressEncoder.IsValid("notanaddress"))
        End Sub

        <TestMethod>
        Public Sub IsValid_TamperedAddress_ReturnsFalse()
            Dim kp As New KeyPair()
            Dim address As String = AddressEncoder.FromKeyPair(kp)
            ' Flip one character
            Dim chars As Char() = address.ToCharArray()
            chars(5) = If(chars(5) = "A"c, "B"c, "A"c)
            Assert.IsFalse(AddressEncoder.IsValid(New String(chars)))
        End Sub

        <TestMethod>
        Public Sub GetHash160_ValidAddress_Returns20Bytes()
            Dim kp As New KeyPair()
            Dim address As String = AddressEncoder.FromKeyPair(kp)
            Dim hash As Byte() = AddressEncoder.GetHash160(address)
            Assert.AreEqual(20, hash.Length)
        End Sub

        <TestMethod>
        Public Sub GetHash160_MatchesHash160OfPublicKey()
            Dim kp As New KeyPair()
            Dim address As String = AddressEncoder.FromKeyPair(kp)
            Dim fromAddress As Byte() = AddressEncoder.GetHash160(address)
            Dim fromKey As Byte() = HashUtil.Hash160(kp.CompressedPublicKey)
            AssertBytesEqual(fromKey, fromAddress)
        End Sub

        <TestMethod>
        Public Sub GetVersion_MainnetAddress_ReturnsMainnetByte()
            Dim kp As New KeyPair()
            Dim address As String = AddressEncoder.FromKeyPair(kp, AddressEncoder.MainnetP2PKH)
            Assert.AreEqual(AddressEncoder.MainnetP2PKH, AddressEncoder.GetVersion(address))
        End Sub

        <TestMethod>
        Public Sub IsMainnet_MainnetAddress_ReturnsTrue()
            Dim kp As New KeyPair()
            Dim address As String = AddressEncoder.FromKeyPair(kp)
            Assert.IsTrue(AddressEncoder.IsMainnet(address))
        End Sub

        <TestMethod>
        Public Sub FromPublicKey_CompressedAndUncompressed_DifferentAddresses()
            Dim kp As New KeyPair()
            Dim addrCompressed As String = AddressEncoder.FromPublicKey(kp.CompressedPublicKey)
            Dim addrUncompressed As String = AddressEncoder.FromPublicKey(kp.UncompressedPublicKey)
            Assert.AreNotEqual(addrCompressed, addrUncompressed)
        End Sub

        <TestMethod>
        <ExpectedException(GetType(FormatException))>
        Public Sub GetHash160_InvalidAddress_Throws()
            AddressEncoder.GetHash160("invalidaddress")
        End Sub

    End Class

End Namespace
