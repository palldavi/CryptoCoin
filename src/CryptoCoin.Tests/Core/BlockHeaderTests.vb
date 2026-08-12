Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports CryptoCoin.Core
Imports CryptoCoin.Cryptography

Namespace CryptoCoin.Tests.Core

    <TestClass>
    Public Class BlockHeaderTests

        Private Function MakeHeader() As BlockHeader
            Dim h As New BlockHeader()
            h.Version = 1
            h.PreviousBlockHash = New String("0"c, 64)
            h.MerkleRoot = New String("0"c, 64)
            h.Timestamp = 1735689600L
            h.Bits = DifficultyCalculator.MinDifficultyBits
            h.Nonce = 0
            h.Height = 0
            Return h
        End Function

        <TestMethod>
        Public Sub ComputeHash_Returns64CharHexString()
            Dim h As BlockHeader = MakeHeader()
            Dim hash As String = h.ComputeHash()
            Assert.AreEqual(64, hash.Length)
        End Sub

        <TestMethod>
        Public Sub ComputeHash_SameHeader_SameHash()
            Dim h As BlockHeader = MakeHeader()
            Assert.AreEqual(h.ComputeHash(), h.ComputeHash())
        End Sub

        <TestMethod>
        Public Sub ComputeHash_DifferentNonce_DifferentHash()
            Dim h1 As BlockHeader = MakeHeader()
            Dim h2 As BlockHeader = MakeHeader()
            h2.Nonce = 1
            Assert.AreNotEqual(h1.ComputeHash(), h2.ComputeHash())
        End Sub

        <TestMethod>
        Public Sub Serialize_Returns80Bytes()
            Dim h As BlockHeader = MakeHeader()
            Dim bytes As Byte() = h.Serialize()
            Assert.AreEqual(80, bytes.Length)
        End Sub

        <TestMethod>
        Public Sub Serialize_Deserialize_Roundtrip()
            Dim original As BlockHeader = MakeHeader()
            original.Nonce = 42
            original.Timestamp = 1735689600L
            Dim bytes As Byte() = original.Serialize()
            Dim restored As BlockHeader = BlockHeader.Deserialize(bytes)
            Assert.AreEqual(original.Version, restored.Version)
            Assert.AreEqual(original.Nonce, restored.Nonce)
            Assert.AreEqual(original.Timestamp, restored.Timestamp)
            Assert.AreEqual(original.Bits, restored.Bits)
        End Sub

        <TestMethod>
        Public Sub MeetsTarget_MinDifficulty_EasilyMet()
            ' With minimum difficulty, the target is very large so most hashes meet it.
            ' Verify by checking that the all-zeros hash meets the target.
            Dim h As BlockHeader = MakeHeader()
            h.Bits = DifficultyCalculator.MinDifficultyBits
            ' The all-zeros hash definitely meets any target
            Dim zeroHash(31) As Byte
            Assert.IsTrue(DifficultyCalculator.MeetsTarget(zeroHash, DifficultyCalculator.MinDifficultyBits))
        End Sub

        <TestMethod>
        Public Sub GetTarget_Returns32Bytes()
            Dim h As BlockHeader = MakeHeader()
            Dim target As Byte() = h.GetTarget()
            Assert.AreEqual(32, target.Length)
        End Sub

        <TestMethod>
        Public Sub Version_DefaultIs1()
            Dim h As New BlockHeader()
            Assert.AreEqual(1, h.Version)
        End Sub

    End Class

End Namespace
