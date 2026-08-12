Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports CryptoCoin.Cryptography

Namespace CryptoCoin.Tests.Cryptography

    <TestClass>
    Public Class MnemonicTests

        <TestMethod>
        Public Sub New_Default_Generates12Words()
            Dim m As New Mnemonic()
            Assert.AreEqual(12, m.Words.Length)
        End Sub

        <TestMethod>
        Public Sub New_24Words_Generates24Words()
            Dim m As New Mnemonic(24)
            Assert.AreEqual(24, m.Words.Length)
        End Sub

        <TestMethod>
        Public Sub New_AllValidWordCounts_Succeed()
            For Each count As Integer In {12, 15, 18, 21, 24}
                Dim m As New Mnemonic(count)
                Assert.AreEqual(count, m.Words.Length, $"Expected {count} words")
            Next
        End Sub

        <TestMethod>
        <ExpectedException(GetType(ArgumentException))>
        Public Sub New_InvalidWordCount_Throws()
            Dim m As New Mnemonic(13)
        End Sub

        <TestMethod>
        Public Sub Phrase_ContainsSpaceSeparatedWords()
            Dim m As New Mnemonic(12)
            Dim parts As String() = m.Phrase.Split(" "c)
            Assert.AreEqual(12, parts.Length)
        End Sub

        <TestMethod>
        Public Sub Entropy_Is16BytesFor12Words()
            Dim m As New Mnemonic(12)
            Assert.AreEqual(16, m.Entropy.Length)
        End Sub

        <TestMethod>
        Public Sub Entropy_Is32BytesFor24Words()
            Dim m As New Mnemonic(24)
            Assert.AreEqual(32, m.Entropy.Length)
        End Sub

        <TestMethod>
        Public Sub New_FromPhrase_RoundtripsEntropy()
            Dim m1 As New Mnemonic(12)
            Dim m2 As New Mnemonic(m1.Phrase)
            AssertBytesEqual(m1.Entropy, m2.Entropy)
        End Sub

        <TestMethod>
        Public Sub New_FromEntropy_RoundtripsPhrase()
            Dim m1 As New Mnemonic(12)
            Dim m2 As New Mnemonic(m1.Entropy)
            Assert.AreEqual(m1.Phrase, m2.Phrase)
        End Sub

        <TestMethod>
        Public Sub IsValid_ValidPhrase_ReturnsTrue()
            Dim m As New Mnemonic(12)
            Assert.IsTrue(Mnemonic.IsValid(m.Phrase))
        End Sub

        <TestMethod>
        Public Sub IsValid_EmptyString_ReturnsFalse()
            Assert.IsFalse(Mnemonic.IsValid(""))
        End Sub

        <TestMethod>
        Public Sub IsValid_WrongWordCount_ReturnsFalse()
            Assert.IsFalse(Mnemonic.IsValid("abandon abandon abandon"))
        End Sub

        <TestMethod>
        Public Sub ToSeed_Returns64Bytes()
            Dim m As New Mnemonic(12)
            Dim seed As Byte() = m.ToSeed()
            Assert.AreEqual(64, seed.Length)
        End Sub

        <TestMethod>
        Public Sub ToSeed_SamePhraseProducesSameSeed()
            Dim m1 As New Mnemonic(12)
            Dim m2 As New Mnemonic(m1.Phrase)
            AssertBytesEqual(m1.ToSeed(), m2.ToSeed())
        End Sub

        <TestMethod>
        Public Sub ToSeed_DifferentPassphrase_DifferentSeed()
            Dim m As New Mnemonic(12)
            Dim seed1 As Byte() = m.ToSeed("")
            Dim seed2 As Byte() = m.ToSeed("passphrase")
            Assert.IsFalse(HashUtil.ConstantTimeEquals(seed1, seed2))
        End Sub

        <TestMethod>
        Public Sub TwoRandomMnemonics_HaveDifferentPhrases()
            Dim m1 As New Mnemonic(12)
            Dim m2 As New Mnemonic(12)
            Assert.AreNotEqual(m1.Phrase, m2.Phrase)
        End Sub

        <TestMethod>
        <ExpectedException(GetType(ArgumentException))>
        Public Sub New_InvalidWord_Throws()
            Dim m As New Mnemonic("notaword notaword notaword notaword notaword notaword notaword notaword notaword notaword notaword notaword")
        End Sub

    End Class

End Namespace
