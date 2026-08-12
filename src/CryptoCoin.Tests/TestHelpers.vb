Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports CryptoCoin.Cryptography

''' <summary>
''' Shared helpers and constants used across all test classes.
''' </summary>
Module TestHelpers

    ' A known-good private key (32 bytes, all 0x01 except last byte = 0x01)
    Public ReadOnly KnownPrivateKeyHex As String =
        "0000000000000000000000000000000000000000000000000000000000000001"

    ' Known SHA-256 of empty string
    Public ReadOnly Sha256EmptyHex As String =
        "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"

    ' Known SHA-256 of "abc" (correct value)
    Public ReadOnly Sha256AbcHex As String =
        "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad"

    ' Known double-SHA-256 of empty bytes
    Public ReadOnly DoubleSha256EmptyHex As String =
        "5df6e0e2761359d30a8275058e299fcc0381534545f55cf43e41983f5d4c9456"

    ' A fixed 12-word mnemonic phrase using words from the wordlist
    Public ReadOnly KnownMnemonicPhrase As String =
        "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about"

    ''' <summary>
    ''' Asserts that two byte arrays are equal.
    ''' </summary>
    Public Sub AssertBytesEqual(expected As Byte(), actual As Byte(), Optional message As String = "")
        Assert.IsNotNull(actual, "Byte array should not be null. " & message)
        Assert.AreEqual(expected.Length, actual.Length,
            $"Byte array length mismatch. Expected {expected.Length}, got {actual.Length}. {message}")
        For i As Integer = 0 To expected.Length - 1
            Assert.AreEqual(expected(i), actual(i),
                $"Byte mismatch at index {i}. Expected 0x{expected(i):X2}, got 0x{actual(i):X2}. {message}")
        Next
    End Sub

    ''' <summary>
    ''' Creates a deterministic 32-byte hash from a seed integer (for test data).
    ''' </summary>
    Public Function MakeHash(seed As Integer) As Byte()
        Dim data(31) As Byte
        For i As Integer = 0 To 31
            data(i) = CByte((seed + i) And &HFF)
        Next
        Return HashUtil.DoubleSha256(data)
    End Function

    ''' <summary>
    ''' Creates a deterministic hex hash string from a seed integer.
    ''' </summary>
    Public Function MakeHashHex(seed As Integer) As String
        Return HashUtil.ToHex(MakeHash(seed))
    End Function

End Module
