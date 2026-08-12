Imports CryptoCoin.Cryptography

Module Program

    Sub Main()
        Console.WriteLine("=== CryptoCoin Demo ===")
        Console.WriteLine()

        ' 1. Generate a mnemonic recovery phrase
        Console.WriteLine("1. Generating 12-word mnemonic phrase...")
        Dim mnemonic As New Mnemonic(12)
        Console.WriteLine($"   Phrase: {mnemonic.Phrase}")
        Console.WriteLine()

        ' 2. Derive seed from mnemonic
        Console.WriteLine("2. Deriving seed from mnemonic...")
        Dim seed As Byte() = mnemonic.ToSeed()
        Console.WriteLine($"   Seed (first 32 bytes): {HashUtil.ToHex(seed).Substring(0, 64)}...")
        Console.WriteLine()

        ' 3. Generate master HD key
        Console.WriteLine("3. Generating master HD key...")
        Dim masterKey As HdKeyDerivation.ExtendedKey = HdKeyDerivation.MasterKeyFromSeed(seed)
        Console.WriteLine($"   Master key serialized: {masterKey.Serialize().Substring(0, 40)}...")
        Console.WriteLine()

        ' 4. Derive a CryptoCoin address (m/44'/999'/0'/0/0)
        Console.WriteLine("4. Deriving address at m/44'/999'/0'/0/0...")
        Dim childKey As HdKeyDerivation.ExtendedKey = HdKeyDerivation.DerivePath(masterKey, "m/44'/999'/0'/0/0")
        Dim keyPair As KeyPair = childKey.GetKeyPair()
        Dim address As String = AddressEncoder.FromKeyPair(keyPair)
        Console.WriteLine($"   Address: {address}")
        Console.WriteLine($"   Public Key: {HashUtil.ToHex(keyPair.CompressedPublicKey)}")
        Console.WriteLine()

        ' 5. Sign and verify a message
        Console.WriteLine("5. Signing a message...")
        Dim message As String = "Hello CryptoCoin!"
        Dim messageHash As Byte() = HashUtil.DoubleSha256(System.Text.Encoding.UTF8.GetBytes(message))
        Dim signature As EcdsaSignature = EcdsaSigner.Sign(messageHash, keyPair)
        Console.WriteLine($"   Message: {message}")
        Console.WriteLine($"   Signature R: {HashUtil.ToHex(signature.R.ToByteArrayFixed(32)).Substring(0, 32)}...")
        Console.WriteLine($"   Signature S: {HashUtil.ToHex(signature.S.ToByteArrayFixed(32)).Substring(0, 32)}...")
        Console.WriteLine()

        ' 6. Verify the signature
        Console.WriteLine("6. Verifying signature...")
        Dim isValid As Boolean = EcdsaSigner.Verify(messageHash, signature, keyPair.PublicKey)
        Console.WriteLine($"   Valid: {isValid}")
        Console.WriteLine()

        ' 7. Demonstrate Merkle tree
        Console.WriteLine("7. Building Merkle tree from 4 transaction hashes...")
        Dim txHashes As New List(Of String)()
        For i As Integer = 1 To 4
            txHashes.Add(HashUtil.ToHex(HashUtil.DoubleSha256(System.Text.Encoding.UTF8.GetBytes($"tx{i}"))))
        Next
        Dim tree As New MerkleTree(txHashes.Select(Function(h) HashUtil.FromHex(h)))
        Console.WriteLine($"   Merkle Root: {HashUtil.ToHex(tree.Root)}")
        Console.WriteLine()

        ' 8. Base58Check encoding
        Console.WriteLine("8. Base58Check encoding demo...")
        Dim testData As Byte() = HashUtil.Sha256("CryptoCoin is awesome")
        Dim encoded As String = Base58Encoder.EncodeCheck(testData)
        Console.WriteLine($"   Encoded: {encoded}")
        Dim decoded As Byte() = Base58Encoder.DecodeCheck(encoded)
        Console.WriteLine($"   Roundtrip OK: {HashUtil.ConstantTimeEquals(testData, decoded)}")
        Console.WriteLine()

        ' 9. Generate multiple addresses
        Console.WriteLine("9. Generating 5 addresses from HD wallet...")
        For i As Integer = 0 To 4
            Dim path As String = $"m/44'/999'/0'/0/{i}"
            Dim ck As HdKeyDerivation.ExtendedKey = HdKeyDerivation.DerivePath(masterKey, path)
            Dim kp As KeyPair = ck.GetKeyPair()
            Dim addr As String = AddressEncoder.FromKeyPair(kp)
            Console.WriteLine($"   {path} -> {addr}")
        Next
        Console.WriteLine()

        Console.WriteLine("=== Demo Complete ===")
        Console.WriteLine("Press any key to exit...")
        Console.ReadKey()
    End Sub

End Module
