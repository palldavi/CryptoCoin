Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports CryptoCoin.Cryptography
Imports System.Text

Namespace CryptoCoin.Tests.Cryptography

    <TestClass>
    Public Class HashUtilTests

        ' ── SHA-256 ──────────────────────────────────────────────────────────

        <TestMethod>
        Public Sub Sha256_EmptyBytes_ReturnsKnownHash()
            Dim result As Byte() = HashUtil.Sha256(New Byte() {})
            Assert.AreEqual(Sha256EmptyHex, HashUtil.ToHex(result))
        End Sub

        <TestMethod>
        Public Sub Sha256_KnownInput_ReturnsCorrectHash()
            Dim input As Byte() = Encoding.UTF8.GetBytes("abc")
            Dim result As Byte() = HashUtil.Sha256(input)
            Assert.AreEqual(Sha256AbcHex, HashUtil.ToHex(result))
        End Sub

        <TestMethod>
        Public Sub Sha256_StringOverload_MatchesBytesOverload()
            Dim fromString As Byte() = HashUtil.Sha256("hello")
            Dim fromBytes As Byte() = HashUtil.Sha256(Encoding.UTF8.GetBytes("hello"))
            AssertBytesEqual(fromBytes, fromString)
        End Sub

        <TestMethod>
        Public Sub Sha256_Returns32Bytes()
            Dim result As Byte() = HashUtil.Sha256(New Byte() {1, 2, 3})
            Assert.AreEqual(32, result.Length)
        End Sub

        <TestMethod>
        <ExpectedException(GetType(ArgumentNullException))>
        Public Sub Sha256_NullInput_Throws()
            HashUtil.Sha256(CType(Nothing, Byte()))
        End Sub

        ' ── Double SHA-256 ───────────────────────────────────────────────────

        <TestMethod>
        Public Sub DoubleSha256_EmptyBytes_ReturnsKnownHash()
            Dim result As Byte() = HashUtil.DoubleSha256(New Byte() {})
            Assert.AreEqual(DoubleSha256EmptyHex, HashUtil.ToHex(result))
        End Sub

        <TestMethod>
        Public Sub DoubleSha256_IsDifferentFromSingleSha256()
            Dim data As Byte() = Encoding.UTF8.GetBytes("test")
            Dim singleHash As Byte() = HashUtil.Sha256(data)
            Dim doubled As Byte() = HashUtil.DoubleSha256(data)
            Assert.IsFalse(HashUtil.ConstantTimeEquals(singleHash, doubled))
        End Sub

        <TestMethod>
        Public Sub DoubleSha256_EqualsManualDoubleHash()
            Dim data As Byte() = Encoding.UTF8.GetBytes("cryptocoin")
            Dim manual As Byte() = HashUtil.Sha256(HashUtil.Sha256(data))
            Dim direct As Byte() = HashUtil.DoubleSha256(data)
            AssertBytesEqual(manual, direct)
        End Sub

        ' ── SHA-512 ──────────────────────────────────────────────────────────

        <TestMethod>
        Public Sub Sha512_Returns64Bytes()
            Dim result As Byte() = HashUtil.Sha512(New Byte() {1, 2, 3})
            Assert.AreEqual(64, result.Length)
        End Sub

        <TestMethod>
        Public Sub Sha512_DifferentFromSha256()
            Dim data As Byte() = Encoding.UTF8.GetBytes("test")
            Dim sha256 As Byte() = HashUtil.Sha256(data)
            Dim sha512 As Byte() = HashUtil.Sha512(data)
            Assert.AreNotEqual(sha256.Length, sha512.Length)
        End Sub

        ' ── Hex encoding ─────────────────────────────────────────────────────

        <TestMethod>
        Public Sub ToHex_KnownBytes_ReturnsCorrectString()
            Dim data As Byte() = {&HDE, &HAD, &HBE, &HEF}
            Assert.AreEqual("deadbeef", HashUtil.ToHex(data))
        End Sub

        <TestMethod>
        Public Sub FromHex_KnownString_ReturnsCorrectBytes()
            Dim result As Byte() = HashUtil.FromHex("deadbeef")
            AssertBytesEqual(New Byte() {&HDE, &HAD, &HBE, &HEF}, result)
        End Sub

        <TestMethod>
        Public Sub ToHex_FromHex_Roundtrip()
            Dim original As Byte() = {1, 2, 3, 255, 128, 0, 64}
            Dim hex As String = HashUtil.ToHex(original)
            Dim decoded As Byte() = HashUtil.FromHex(hex)
            AssertBytesEqual(original, decoded)
        End Sub

        <TestMethod>
        Public Sub ToHex_EmptyArray_ReturnsEmptyString()
            Assert.AreEqual("", HashUtil.ToHex(New Byte() {}))
        End Sub

        <TestMethod>
        <ExpectedException(GetType(ArgumentException))>
        Public Sub FromHex_OddLengthString_Throws()
            HashUtil.FromHex("abc")
        End Sub

        <TestMethod>
        Public Sub FromHex_UppercaseInput_Works()
            Dim result As Byte() = HashUtil.FromHex("DEADBEEF")
            AssertBytesEqual(New Byte() {&HDE, &HAD, &HBE, &HEF}, result)
        End Sub

        ' ── Checksum ─────────────────────────────────────────────────────────

        <TestMethod>
        Public Sub Checksum_Returns4Bytes()
            Dim result As Byte() = HashUtil.Checksum(New Byte() {1, 2, 3})
            Assert.AreEqual(4, result.Length)
        End Sub

        <TestMethod>
        Public Sub Checksum_SameInputSameOutput()
            Dim data As Byte() = {10, 20, 30}
            Dim c1 As Byte() = HashUtil.Checksum(data)
            Dim c2 As Byte() = HashUtil.Checksum(data)
            AssertBytesEqual(c1, c2)
        End Sub

        <TestMethod>
        Public Sub Checksum_EqualsFirstFourBytesOfDoubleSha256()
            Dim data As Byte() = {1, 2, 3, 4}
            Dim fullHash As Byte() = HashUtil.DoubleSha256(data)
            Dim checksum As Byte() = HashUtil.Checksum(data)
            For i As Integer = 0 To 3
                Assert.AreEqual(fullHash(i), checksum(i))
            Next
        End Sub

        ' ── ConstantTimeEquals ───────────────────────────────────────────────

        <TestMethod>
        Public Sub ConstantTimeEquals_EqualArrays_ReturnsTrue()
            Dim a As Byte() = {1, 2, 3}
            Dim b As Byte() = {1, 2, 3}
            Assert.IsTrue(HashUtil.ConstantTimeEquals(a, b))
        End Sub

        <TestMethod>
        Public Sub ConstantTimeEquals_DifferentArrays_ReturnsFalse()
            Dim a As Byte() = {1, 2, 3}
            Dim b As Byte() = {1, 2, 4}
            Assert.IsFalse(HashUtil.ConstantTimeEquals(a, b))
        End Sub

        <TestMethod>
        Public Sub ConstantTimeEquals_DifferentLengths_ReturnsFalse()
            Dim a As Byte() = {1, 2, 3}
            Dim b As Byte() = {1, 2}
            Assert.IsFalse(HashUtil.ConstantTimeEquals(a, b))
        End Sub

        <TestMethod>
        Public Sub ConstantTimeEquals_NullInputs_ReturnsFalse()
            Assert.IsFalse(HashUtil.ConstantTimeEquals(Nothing, New Byte() {1}))
            Assert.IsFalse(HashUtil.ConstantTimeEquals(New Byte() {1}, Nothing))
        End Sub

        <TestMethod>
        Public Sub ConstantTimeEquals_EmptyArrays_ReturnsTrue()
            Assert.IsTrue(HashUtil.ConstantTimeEquals(New Byte() {}, New Byte() {}))
        End Sub

        ' ── Hash160 ──────────────────────────────────────────────────────────

        <TestMethod>
        Public Sub Hash160_Returns20Bytes()
            Dim result As Byte() = HashUtil.Hash160(New Byte() {1, 2, 3})
            Assert.AreEqual(20, result.Length)
        End Sub

        <TestMethod>
        Public Sub Hash160_DifferentInputsDifferentOutputs()
            Dim h1 As Byte() = HashUtil.Hash160(New Byte() {1})
            Dim h2 As Byte() = HashUtil.Hash160(New Byte() {2})
            Assert.IsFalse(HashUtil.ConstantTimeEquals(h1, h2))
        End Sub

    End Class

End Namespace
