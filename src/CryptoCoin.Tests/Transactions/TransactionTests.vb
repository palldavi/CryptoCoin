Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports CryptoCoin.Transactions
Imports CryptoCoin.Cryptography

Namespace CryptoCoin.Tests.Transactions

    <TestClass>
    Public Class TransactionTests

        Private Function MakeCoinbase(height As Integer, reward As Long) As Transaction
            Dim kp As New KeyPair()
            Dim address As String = AddressEncoder.FromKeyPair(kp)
            Return Transaction.CreateCoinbase(height, reward, address)
        End Function

        <TestMethod>
        Public Sub CreateCoinbase_HasOneInput()
            Dim tx As Transaction = MakeCoinbase(1, 5000000000L)
            Assert.AreEqual(1, tx.Inputs.Count)
        End Sub

        <TestMethod>
        Public Sub CreateCoinbase_HasOneOutput()
            Dim tx As Transaction = MakeCoinbase(1, 5000000000L)
            Assert.AreEqual(1, tx.Outputs.Count)
        End Sub

        <TestMethod>
        Public Sub CreateCoinbase_IsCoinbaseIsTrue()
            Dim tx As Transaction = MakeCoinbase(1, 5000000000L)
            Assert.IsTrue(tx.IsCoinbase)
        End Sub

        <TestMethod>
        Public Sub CreateCoinbase_OutputValueMatchesReward()
            Dim reward As Long = 5000000000L
            Dim tx As Transaction = MakeCoinbase(1, reward)
            Assert.AreEqual(reward, tx.TotalOutputValue)
        End Sub

        <TestMethod>
        Public Sub TxId_Is64CharHexString()
            Dim tx As Transaction = MakeCoinbase(1, 5000000000L)
            Assert.AreEqual(64, tx.TxId.Length)
        End Sub

        <TestMethod>
        Public Sub TxId_IsDeterministic()
            Dim kp As New KeyPair()
            Dim address As String = AddressEncoder.FromKeyPair(kp)
            Dim tx As Transaction = Transaction.CreateCoinbase(1, 5000000000L, address)
            Assert.AreEqual(tx.TxId, tx.TxId)
        End Sub

        <TestMethod>
        Public Sub TxId_DifferentTransactions_DifferentIds()
            Dim tx1 As Transaction = MakeCoinbase(1, 5000000000L)
            Dim tx2 As Transaction = MakeCoinbase(2, 5000000000L)
            Assert.AreNotEqual(tx1.TxId, tx2.TxId)
        End Sub

        <TestMethod>
        Public Sub Serialize_Deserialize_Roundtrip()
            Dim tx As Transaction = MakeCoinbase(1, 5000000000L)
            Dim bytes As Byte() = tx.Serialize()
            Dim restored As Transaction = Transaction.Deserialize(bytes)
            Assert.AreEqual(tx.TxId, restored.TxId)
            Assert.AreEqual(tx.Inputs.Count, restored.Inputs.Count)
            Assert.AreEqual(tx.Outputs.Count, restored.Outputs.Count)
        End Sub

        <TestMethod>
        Public Sub Serialize_ProducesNonEmptyBytes()
            Dim tx As Transaction = MakeCoinbase(1, 5000000000L)
            Dim bytes As Byte() = tx.Serialize()
            Assert.IsTrue(bytes.Length > 0)
        End Sub

        <TestMethod>
        Public Sub Size_IsPositive()
            Dim tx As Transaction = MakeCoinbase(1, 5000000000L)
            Assert.IsTrue(tx.Size > 0)
        End Sub

        <TestMethod>
        Public Sub Version_DefaultIs1()
            Dim tx As New Transaction()
            Assert.AreEqual(1, tx.Version)
        End Sub

        <TestMethod>
        Public Sub IsCoinbase_RegularTransaction_ReturnsFalse()
            Dim tx As New Transaction()
            Dim input As New TransactionInput()
            input.PreviousOutput = New OutPoint("abcd" & New String("0"c, 60), 0)
            tx.Inputs.Add(input)
            Assert.IsFalse(tx.IsCoinbase)
        End Sub

        <TestMethod>
        Public Sub TotalOutputValue_SumsAllOutputs()
            Dim tx As New Transaction()
            tx.Outputs.Add(New TransactionOutput(1000L, New Byte() {}))
            tx.Outputs.Add(New TransactionOutput(2000L, New Byte() {}))
            tx.Outputs.Add(New TransactionOutput(3000L, New Byte() {}))
            Assert.AreEqual(6000L, tx.TotalOutputValue)
        End Sub

    End Class

End Namespace
