Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports CryptoCoin.Transactions
Imports CryptoCoin.Transactions.Script
Imports CryptoCoin.Cryptography

Namespace CryptoCoin.Tests.Transactions

    <TestClass>
    Public Class ScriptTests

        <TestMethod>
        Public Sub CreateP2PKHOutput_Returns25Bytes()
            Dim kp As New KeyPair()
            Dim address As String = AddressEncoder.FromKeyPair(kp)
            Dim script As Byte() = StandardScripts.CreateP2PKHOutput(address)
            Assert.AreEqual(25, script.Length)
        End Sub

        <TestMethod>
        Public Sub CreateP2PKHOutput_StartsWithOpDupOpHash160()
            Dim kp As New KeyPair()
            Dim address As String = AddressEncoder.FromKeyPair(kp)
            Dim script As Byte() = StandardScripts.CreateP2PKHOutput(address)
            Assert.AreEqual(OpCodes.OP_DUP, script(0))
            Assert.AreEqual(OpCodes.OP_HASH160, script(1))
            Assert.AreEqual(CByte(20), script(2))
        End Sub

        <TestMethod>
        Public Sub CreateP2PKHOutput_EndsWithEqualVerifyCheckSig()
            Dim kp As New KeyPair()
            Dim address As String = AddressEncoder.FromKeyPair(kp)
            Dim script As Byte() = StandardScripts.CreateP2PKHOutput(address)
            Assert.AreEqual(OpCodes.OP_EQUALVERIFY, script(23))
            Assert.AreEqual(OpCodes.OP_CHECKSIG, script(24))
        End Sub

        <TestMethod>
        Public Sub GetOutputType_P2PKHScript_ReturnsP2PKH()
            Dim kp As New KeyPair()
            Dim address As String = AddressEncoder.FromKeyPair(kp)
            Dim script As Byte() = StandardScripts.CreateP2PKHOutput(address)
            Assert.AreEqual(ScriptOutputType.P2PKH, StandardScripts.GetOutputType(script))
        End Sub

        <TestMethod>
        Public Sub GetOutputType_P2SHScript_ReturnsP2SH()
            Dim scriptHash(19) As Byte
            Dim script As Byte() = StandardScripts.CreateP2SHOutput(scriptHash)
            Assert.AreEqual(ScriptOutputType.P2SH, StandardScripts.GetOutputType(script))
        End Sub

        <TestMethod>
        Public Sub GetOutputType_NullDataScript_ReturnsNullData()
            Dim data As Byte() = {1, 2, 3, 4}
            Dim script As Byte() = StandardScripts.CreateNullDataOutput(data)
            Assert.AreEqual(ScriptOutputType.NullData, StandardScripts.GetOutputType(script))
        End Sub

        <TestMethod>
        Public Sub GetOutputType_EmptyScript_ReturnsNonStandard()
            Assert.AreEqual(ScriptOutputType.NonStandard, StandardScripts.GetOutputType(New Byte() {}))
        End Sub

        <TestMethod>
        Public Sub CreateP2SHOutput_Returns23Bytes()
            Dim scriptHash(19) As Byte
            Dim script As Byte() = StandardScripts.CreateP2SHOutput(scriptHash)
            Assert.AreEqual(23, script.Length)
        End Sub

        <TestMethod>
        Public Sub CreateNullDataOutput_StartsWithOpReturn()
            Dim script As Byte() = StandardScripts.CreateNullDataOutput(New Byte() {1, 2, 3})
            Assert.AreEqual(OpCodes.OP_RETURN, script(0))
        End Sub

        <TestMethod>
        <ExpectedException(GetType(ArgumentException))>
        Public Sub CreateNullDataOutput_Over80Bytes_Throws()
            Dim data(80) As Byte
            StandardScripts.CreateNullDataOutput(data)
        End Sub

        <TestMethod>
        Public Sub ExtractP2PKHHash_ReturnsCorrectHash()
            Dim kp As New KeyPair()
            Dim address As String = AddressEncoder.FromKeyPair(kp)
            Dim script As Byte() = StandardScripts.CreateP2PKHOutput(address)
            Dim extracted As Byte() = StandardScripts.ExtractP2PKHHash(script)
            Dim expected As Byte() = AddressEncoder.GetHash160(address)
            AssertBytesEqual(expected, extracted)
        End Sub

        <TestMethod>
        Public Sub ScriptBuilder_PushData_ProducesCorrectLength()
            Dim builder As New ScriptBuilder()
            Dim data(9) As Byte ' 10 bytes
            builder.PushData(data)
            Dim result As Byte() = builder.ToBytes()
            ' 1 byte length prefix + 10 bytes data = 11
            Assert.AreEqual(11, result.Length)
        End Sub

        <TestMethod>
        Public Sub ScriptBuilder_AddOp_AddsOpcode()
            Dim builder As New ScriptBuilder()
            builder.AddOp(OpCodes.OP_DUP)
            builder.AddOp(OpCodes.OP_HASH160)
            Dim result As Byte() = builder.ToBytes()
            Assert.AreEqual(2, result.Length)
            Assert.AreEqual(OpCodes.OP_DUP, result(0))
            Assert.AreEqual(OpCodes.OP_HASH160, result(1))
        End Sub

        <TestMethod>
        Public Sub OpCodes_GetName_ReturnsReadableName()
            Assert.AreEqual("OP_DUP", OpCodes.GetName(OpCodes.OP_DUP))
            Assert.AreEqual("OP_HASH160", OpCodes.GetName(OpCodes.OP_HASH160))
            Assert.AreEqual("OP_CHECKSIG", OpCodes.GetName(OpCodes.OP_CHECKSIG))
            Assert.AreEqual("OP_RETURN", OpCodes.GetName(OpCodes.OP_RETURN))
        End Sub

    End Class

End Namespace
