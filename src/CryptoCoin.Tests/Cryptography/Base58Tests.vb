Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports CryptoCoin.Cryptography

Namespace CryptoCoin.Tests.Cryptography

    <TestClass>
    Public Class Base58Tests

        ' ── Encode / Decode ──────────────────────────────────────────────────

        <TestMethod>
        Public Sub Encode_SingleZeroByte_ReturnsOne()
            ' Leading zero bytes encode as '1' characters
            Dim result As String = Base58Encoder.Encode(New Byte() {0})
            Assert.AreEqual("1", result)
        End Sub

        <TestMethod>
        Public Sub Encode_EmptyArray_ReturnsEmptyString()
            Dim result As String = Base58Encoder.Encode(New Byte() {})
            Assert.AreEqual("", result)
        End Sub

        <TestMethod>
        Public Sub Encode_Decode_Roundtrip()
            Dim original As Byte() = {1, 2, 3, 4, 5, 100, 200, 255}
            Dim encoded As String = Base58Encoder.Encode(original)
            Dim decoded As Byte() = Base58Encoder.Decode(encoded)
            AssertBytesEqual(original, decoded)
        End Sub

        <TestMethod>
        Public Sub Encode_LeadingZeros_PreservedAsOnes()
            Dim data As Byte() = {0, 0, 1, 2, 3}
            Dim encoded As String = Base58Encoder.Encode(data)
            Assert.IsTrue(encoded.StartsWith("11"), "Leading zeros should encode as '1' characters")
        End Sub

        <TestMethod>
        Public Sub Decode_LeadingOnes_PreservedAsZeroBytes()
            Dim encoded As String = "11abc"
            Dim decoded As Byte() = Base58Encoder.Decode(encoded)
            Assert.AreEqual(CByte(0), decoded(0))
            Assert.AreEqual(CByte(0), decoded(1))
        End Sub

        <TestMethod>
        <ExpectedException(GetType(FormatException))>
        Public Sub Decode_InvalidCharacter_Throws()
            ' '0', 'O', 'I', 'l' are not in the Base58 alphabet
            Base58Encoder.Decode("0abc")
        End Sub

        <TestMethod>
        Public Sub Encode_OnlyUsesValidAlphabet()
            Dim data As Byte() = HashUtil.Sha256(New Byte() {42})
            Dim encoded As String = Base58Encoder.Encode(data)
            Dim invalidChars As String = "0OIl"
            For Each c As Char In encoded
                Assert.IsFalse(invalidChars.Contains(c),
                    $"Encoded string contains invalid Base58 character: '{c}'")
            Next
        End Sub

        ' ── EncodeCheck / DecodeCheck ────────────────────────────────────────

        <TestMethod>
        Public Sub EncodeCheck_DecodeCheck_Roundtrip()
            Dim data As Byte() = {&H1C, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20}
            Dim encoded As String = Base58Encoder.EncodeCheck(data)
            Dim decoded As Byte() = Base58Encoder.DecodeCheck(encoded)
            AssertBytesEqual(data, decoded)
        End Sub

        <TestMethod>
        <ExpectedException(GetType(FormatException))>
        Public Sub DecodeCheck_TamperedData_ThrowsChecksumMismatch()
            Dim data As Byte() = {&H1C, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20}
            Dim encoded As String = Base58Encoder.EncodeCheck(data)
            ' Flip a character in the middle to corrupt the checksum
            Dim chars As Char() = encoded.ToCharArray()
            chars(5) = If(chars(5) = "A"c, "B"c, "A"c)
            Base58Encoder.DecodeCheck(New String(chars))
        End Sub

        <TestMethod>
        Public Sub EncodeCheck_LongerThanPlainEncode()
            ' EncodeCheck appends 4 checksum bytes, so encoded string should be longer
            Dim data As Byte() = {1, 2, 3, 4, 5}
            Dim plain As String = Base58Encoder.Encode(data)
            Dim withCheck As String = Base58Encoder.EncodeCheck(data)
            Assert.IsTrue(withCheck.Length > plain.Length)
        End Sub

        <TestMethod>
        Public Sub EncodeCheck_SameInputSameOutput()
            Dim data As Byte() = {&H1C, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120, 130, 140, 150, 160, 170, 180, 190, 200}
            Dim e1 As String = Base58Encoder.EncodeCheck(data)
            Dim e2 As String = Base58Encoder.EncodeCheck(data)
            Assert.AreEqual(e1, e2)
        End Sub

    End Class

End Namespace
