Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports CryptoCoin.Cryptography
Imports System.Numerics

Namespace CryptoCoin.Tests.Cryptography

    <TestClass>
    Public Class KeyPairTests

        <TestMethod>
        Public Sub New_RandomKeyPair_GeneratesValidKeys()
            Dim kp As New KeyPair()
            Assert.IsNotNull(kp.PrivateKeyBytes)
            Assert.AreEqual(32, kp.PrivateKeyBytes.Length)
            Assert.IsNotNull(kp.PublicKey)
            Assert.IsFalse(kp.PublicKey.IsInfinity)
        End Sub

        <TestMethod>
        Public Sub New_RandomKeyPair_CompressedPublicKeyIs33Bytes()
            Dim kp As New KeyPair()
            Assert.AreEqual(33, kp.CompressedPublicKey.Length)
        End Sub

        <TestMethod>
        Public Sub New_RandomKeyPair_UncompressedPublicKeyIs65Bytes()
            Dim kp As New KeyPair()
            Assert.AreEqual(65, kp.UncompressedPublicKey.Length)
        End Sub

        <TestMethod>
        Public Sub New_RandomKeyPair_CompressedPrefixIs02Or03()
            Dim kp As New KeyPair()
            Dim prefix As Byte = kp.CompressedPublicKey(0)
            Assert.IsTrue(prefix = 2 OrElse prefix = 3,
                $"Compressed public key prefix should be 02 or 03, got {prefix:X2}")
        End Sub

        <TestMethod>
        Public Sub New_RandomKeyPair_UncompressedPrefixIs04()
            Dim kp As New KeyPair()
            Assert.AreEqual(CByte(4), kp.UncompressedPublicKey(0))
        End Sub

        <TestMethod>
        Public Sub New_FromPrivateKeyBytes_DeterminsticPublicKey()
            Dim privBytes As Byte() = HashUtil.FromHex(KnownPrivateKeyHex)
            Dim kp1 As New KeyPair(privBytes)
            Dim kp2 As New KeyPair(privBytes)
            AssertBytesEqual(kp1.CompressedPublicKey, kp2.CompressedPublicKey)
        End Sub

        <TestMethod>
        Public Sub New_FromPrivateKeyBytes_PrivateKeyRoundtrips()
            Dim privBytes As Byte() = HashUtil.FromHex(KnownPrivateKeyHex)
            Dim kp As New KeyPair(privBytes)
            AssertBytesEqual(privBytes, kp.PrivateKeyBytes)
        End Sub

        <TestMethod>
        <ExpectedException(GetType(ArgumentException))>
        Public Sub New_WrongLengthPrivateKey_Throws()
            Dim shortKey As Byte() = {1, 2, 3}
            Dim kp As New KeyPair(shortKey)
        End Sub

        <TestMethod>
        <ExpectedException(GetType(ArgumentNullException))>
        Public Sub New_NullPrivateKey_Throws()
            Dim kp As New KeyPair(CType(Nothing, Byte()))
        End Sub

        <TestMethod>
        Public Sub FromHex_ValidHex_CreatesKeyPair()
            Dim kp As KeyPair = KeyPair.FromHex(KnownPrivateKeyHex)
            Assert.IsNotNull(kp)
            Assert.AreEqual(KnownPrivateKeyHex, kp.ToHex())
        End Sub

        <TestMethod>
        Public Sub ToHex_RoundtripsWithFromHex()
            Dim kp1 As New KeyPair()
            Dim hex As String = kp1.ToHex()
            Dim kp2 As KeyPair = KeyPair.FromHex(hex)
            AssertBytesEqual(kp1.CompressedPublicKey, kp2.CompressedPublicKey)
        End Sub

        <TestMethod>
        Public Sub ToWif_Compressed_StartsWithK()
            ' Mainnet compressed WIF starts with 'K' or 'L'
            Dim kp As New KeyPair()
            Dim wif As String = kp.ToWif(compressed:=True)
            Assert.IsTrue(wif.StartsWith("K") OrElse wif.StartsWith("L"),
                $"Compressed mainnet WIF should start with K or L, got: {wif.Substring(0, 1)}")
        End Sub

        <TestMethod>
        Public Sub ToWif_FromWif_Roundtrip()
            Dim kp1 As New KeyPair()
            Dim wif As String = kp1.ToWif()
            Dim kp2 As KeyPair = KeyPair.FromWif(wif)
            AssertBytesEqual(kp1.PrivateKeyBytes, kp2.PrivateKeyBytes)
        End Sub

        <TestMethod>
        Public Sub TwoRandomKeyPairs_HaveDifferentKeys()
            Dim kp1 As New KeyPair()
            Dim kp2 As New KeyPair()
            Assert.IsFalse(HashUtil.ConstantTimeEquals(kp1.PrivateKeyBytes, kp2.PrivateKeyBytes),
                "Two random key pairs should have different private keys")
        End Sub

        <TestMethod>
        Public Sub PublicKey_IsOnCurve()
            Dim kp As New KeyPair()
            Assert.IsTrue(Secp256k1Curve.IsOnCurve(kp.PublicKey))
        End Sub

    End Class

End Namespace
