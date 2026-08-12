Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports CryptoCoin.Cryptography
Imports System.Text

Namespace CryptoCoin.Tests.Cryptography

    <TestClass>
    Public Class EcdsaTests

        Private Function MakeMessageHash(message As String) As Byte()
            Return HashUtil.DoubleSha256(Encoding.UTF8.GetBytes(message))
        End Function

        <TestMethod>
        Public Sub Sign_Verify_ValidSignature_ReturnsTrue()
            Dim kp As New KeyPair()
            Dim hash As Byte() = MakeMessageHash("Hello CryptoCoin")
            Dim sig As EcdsaSignature = EcdsaSigner.Sign(hash, kp)
            Assert.IsTrue(EcdsaSigner.Verify(hash, sig, kp))
        End Sub

        <TestMethod>
        Public Sub Sign_Verify_WrongMessage_ReturnsFalse()
            Dim kp As New KeyPair()
            Dim hash1 As Byte() = MakeMessageHash("message one")
            Dim hash2 As Byte() = MakeMessageHash("message two")
            Dim sig As EcdsaSignature = EcdsaSigner.Sign(hash1, kp)
            Assert.IsFalse(EcdsaSigner.Verify(hash2, sig, kp))
        End Sub

        <TestMethod>
        Public Sub Sign_Verify_WrongKey_ReturnsFalse()
            Dim kp1 As New KeyPair()
            Dim kp2 As New KeyPair()
            Dim hash As Byte() = MakeMessageHash("test message")
            Dim sig As EcdsaSignature = EcdsaSigner.Sign(hash, kp1)
            Assert.IsFalse(EcdsaSigner.Verify(hash, sig, kp2))
        End Sub

        <TestMethod>
        Public Sub Sign_ProducesNonZeroRAndS()
            Dim kp As New KeyPair()
            Dim hash As Byte() = MakeMessageHash("test")
            Dim sig As EcdsaSignature = EcdsaSigner.Sign(hash, kp)
            Assert.AreNotEqual(System.Numerics.BigInteger.Zero, sig.R)
            Assert.AreNotEqual(System.Numerics.BigInteger.Zero, sig.S)
        End Sub

        <TestMethod>
        Public Sub Sign_LowS_SIsLessThanHalfN()
            ' BIP62 low-S requirement
            Dim kp As New KeyPair()
            Dim hash As Byte() = MakeMessageHash("low-s test")
            Dim sig As EcdsaSignature = EcdsaSigner.Sign(hash, kp)
            Dim halfN As System.Numerics.BigInteger = Secp256k1Curve.N >> 1
            Assert.IsTrue(sig.S <= halfN, "Signature S should be <= n/2 (low-S)")
        End Sub

        <TestMethod>
        Public Sub Sign_TwoSignaturesOfSameMessage_AreDifferent()
            ' ECDSA with random k produces different signatures each time
            Dim kp As New KeyPair()
            Dim hash As Byte() = MakeMessageHash("same message")
            Dim sig1 As EcdsaSignature = EcdsaSigner.Sign(hash, kp)
            Dim sig2 As EcdsaSignature = EcdsaSigner.Sign(hash, kp)
            ' R values should differ (different random k each time)
            Assert.AreNotEqual(sig1.R, sig2.R)
        End Sub

        <TestMethod>
        Public Sub Verify_NullHash_ReturnsFalse()
            Dim kp As New KeyPair()
            Dim hash As Byte() = MakeMessageHash("test")
            Dim sig As EcdsaSignature = EcdsaSigner.Sign(hash, kp)
            Assert.IsFalse(EcdsaSigner.Verify(Nothing, sig, kp.PublicKey))
        End Sub

        <TestMethod>
        Public Sub Verify_NullSignature_ReturnsFalse()
            Dim kp As New KeyPair()
            Dim hash As Byte() = MakeMessageHash("test")
            Assert.IsFalse(EcdsaSigner.Verify(hash, Nothing, kp.PublicKey))
        End Sub

        <TestMethod>
        Public Sub Verify_InfinityPublicKey_ReturnsFalse()
            Dim kp As New KeyPair()
            Dim hash As Byte() = MakeMessageHash("test")
            Dim sig As EcdsaSignature = EcdsaSigner.Sign(hash, kp)
            Assert.IsFalse(EcdsaSigner.Verify(hash, sig, EcPoint.Infinity))
        End Sub

        <TestMethod>
        Public Sub DerSerialization_Roundtrip()
            Dim kp As New KeyPair()
            Dim hash As Byte() = MakeMessageHash("der test")
            Dim sig As EcdsaSignature = EcdsaSigner.Sign(hash, kp)
            Dim der As Byte() = sig.ToDer()
            Dim restored As EcdsaSignature = EcdsaSignature.FromDer(der)
            Assert.AreEqual(sig.R, restored.R)
            Assert.AreEqual(sig.S, restored.S)
        End Sub

        <TestMethod>
        Public Sub DerSerialization_StartsWithSequenceTag()
            Dim kp As New KeyPair()
            Dim hash As Byte() = MakeMessageHash("der prefix test")
            Dim sig As EcdsaSignature = EcdsaSigner.Sign(hash, kp)
            Dim der As Byte() = sig.ToDer()
            Assert.AreEqual(CByte(&H30), der(0), "DER signature must start with 0x30 (SEQUENCE)")
        End Sub

        <TestMethod>
        Public Sub Sign_KnownPrivateKey_VerifiesCorrectly()
            Dim kp As KeyPair = KeyPair.FromHex(KnownPrivateKeyHex)
            Dim hash As Byte() = MakeMessageHash("known key test")
            Dim sig As EcdsaSignature = EcdsaSigner.Sign(hash, kp)
            Assert.IsTrue(EcdsaSigner.Verify(hash, sig, kp.PublicKey))
        End Sub

    End Class

End Namespace
