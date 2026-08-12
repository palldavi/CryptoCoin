Imports CryptoCoin.Cryptography

Namespace CryptoCoin.Transactions.Script

    ''' <summary>
    ''' Creates and identifies standard transaction scripts.
    ''' </summary>
    Public NotInheritable Class StandardScripts

        Private Sub New()
        End Sub

        ''' <summary>
        ''' Creates a P2PKH (Pay-to-Public-Key-Hash) output script.
        ''' Format: OP_DUP OP_HASH160 [20-byte-hash] OP_EQUALVERIFY OP_CHECKSIG
        ''' </summary>
        Public Shared Function CreateP2PKHOutput(address As String) As Byte()
            Dim hash160 As Byte() = AddressEncoder.GetHash160(address)
            Return CreateP2PKHOutputFromHash(hash160)
        End Function

        ''' <summary>
        ''' Creates a P2PKH output script from a Hash160 directly.
        ''' </summary>
        Public Shared Function CreateP2PKHOutputFromHash(hash160 As Byte()) As Byte()
            If hash160 Is Nothing OrElse hash160.Length <> 20 Then
                Throw New ArgumentException("Hash160 must be 20 bytes.")
            End If

            Dim script(24) As Byte
            script(0) = OpCodes.OP_DUP
            script(1) = OpCodes.OP_HASH160
            script(2) = 20 ' Push 20 bytes
            Array.Copy(hash160, 0, script, 3, 20)
            script(23) = OpCodes.OP_EQUALVERIFY
            script(24) = OpCodes.OP_CHECKSIG
            Return script
        End Function

        ''' <summary>
        ''' Creates a P2PKH input script (scriptSig).
        ''' Format: [signature] [public-key]
        ''' </summary>
        Public Shared Function CreateP2PKHInput(signature As Byte(), publicKey As Byte()) As Byte()
            Dim builder As New ScriptBuilder()
            builder.PushData(signature)
            builder.PushData(publicKey)
            Return builder.ToBytes()
        End Function

        ''' <summary>
        ''' Creates a P2SH (Pay-to-Script-Hash) output script.
        ''' Format: OP_HASH160 [20-byte-hash] OP_EQUAL
        ''' </summary>
        Public Shared Function CreateP2SHOutput(scriptHash As Byte()) As Byte()
            If scriptHash Is Nothing OrElse scriptHash.Length <> 20 Then
                Throw New ArgumentException("Script hash must be 20 bytes.")
            End If

            Dim script(22) As Byte
            script(0) = OpCodes.OP_HASH160
            script(1) = 20
            Array.Copy(scriptHash, 0, script, 2, 20)
            script(22) = OpCodes.OP_EQUAL
            Return script
        End Function

        ''' <summary>
        ''' Creates a P2PK (Pay-to-Public-Key) output script.
        ''' Format: [public-key] OP_CHECKSIG
        ''' </summary>
        Public Shared Function CreateP2PKOutput(publicKey As Byte()) As Byte()
            If publicKey Is Nothing Then Throw New ArgumentNullException(NameOf(publicKey))

            Dim script(publicKey.Length + 1) As Byte
            script(0) = CByte(publicKey.Length)
            Array.Copy(publicKey, 0, script, 1, publicKey.Length)
            script(publicKey.Length + 1) = OpCodes.OP_CHECKSIG
            Return script
        End Function

        ''' <summary>
        ''' Creates a multisig output script.
        ''' Format: OP_M [pubkey1] [pubkey2] ... OP_N OP_CHECKMULTISIG
        ''' </summary>
        Public Shared Function CreateMultiSigOutput(required As Integer, publicKeys As List(Of Byte())) As Byte()
            If publicKeys Is Nothing OrElse publicKeys.Count = 0 Then
                Throw New ArgumentException("At least one public key required.")
            End If
            If required < 1 OrElse required > publicKeys.Count Then
                Throw New ArgumentException("Required signatures must be between 1 and number of keys.")
            End If

            Dim builder As New ScriptBuilder()
            builder.AddOp(CByte(OpCodes.OP_1 + required - 1))
            For Each key As Byte() In publicKeys
                builder.PushData(key)
            Next
            builder.AddOp(CByte(OpCodes.OP_1 + publicKeys.Count - 1))
            builder.AddOp(OpCodes.OP_CHECKMULTISIG)
            Return builder.ToBytes()
        End Function

        ''' <summary>
        ''' Creates an OP_RETURN (null data) output script for embedding data.
        ''' </summary>
        Public Shared Function CreateNullDataOutput(data As Byte()) As Byte()
            If data Is Nothing Then data = New Byte() {}
            If data.Length > 80 Then
                Throw New ArgumentException("OP_RETURN data cannot exceed 80 bytes.")
            End If

            Dim builder As New ScriptBuilder()
            builder.AddOp(OpCodes.OP_RETURN)
            If data.Length > 0 Then
                builder.PushData(data)
            End If
            Return builder.ToBytes()
        End Function

        ''' <summary>
        ''' Determines the type of an output script.
        ''' </summary>
        Public Shared Function GetOutputType(scriptPubKey As Byte()) As ScriptOutputType
            If scriptPubKey Is Nothing OrElse scriptPubKey.Length = 0 Then
                Return ScriptOutputType.NonStandard
            End If

            ' P2PKH: OP_DUP OP_HASH160 <20> [hash] OP_EQUALVERIFY OP_CHECKSIG
            If scriptPubKey.Length = 25 AndAlso
               scriptPubKey(0) = OpCodes.OP_DUP AndAlso
               scriptPubKey(1) = OpCodes.OP_HASH160 AndAlso
               scriptPubKey(2) = 20 AndAlso
               scriptPubKey(23) = OpCodes.OP_EQUALVERIFY AndAlso
               scriptPubKey(24) = OpCodes.OP_CHECKSIG Then
                Return ScriptOutputType.P2PKH
            End If

            ' P2SH: OP_HASH160 <20> [hash] OP_EQUAL
            If scriptPubKey.Length = 23 AndAlso
               scriptPubKey(0) = OpCodes.OP_HASH160 AndAlso
               scriptPubKey(1) = 20 AndAlso
               scriptPubKey(22) = OpCodes.OP_EQUAL Then
                Return ScriptOutputType.P2SH
            End If

            ' P2PK: <33 or 65> [pubkey] OP_CHECKSIG
            If (scriptPubKey.Length = 35 OrElse scriptPubKey.Length = 67) AndAlso
               scriptPubKey(scriptPubKey.Length - 1) = OpCodes.OP_CHECKSIG Then
                Return ScriptOutputType.P2PK
            End If

            ' OP_RETURN
            If scriptPubKey(0) = OpCodes.OP_RETURN Then
                Return ScriptOutputType.NullData
            End If

            Return ScriptOutputType.NonStandard
        End Function

        ''' <summary>
        ''' Extracts the address hash from a P2PKH script.
        ''' </summary>
        Public Shared Function ExtractP2PKHHash(scriptPubKey As Byte()) As Byte()
            If GetOutputType(scriptPubKey) <> ScriptOutputType.P2PKH Then Return Nothing
            Dim hash(19) As Byte
            Array.Copy(scriptPubKey, 3, hash, 0, 20)
            Return hash
        End Function

    End Class

End Namespace
