Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports CryptoCoin.Transactions
Imports CryptoCoin.Cryptography

Namespace CryptoCoin.Tests.Transactions

    <TestClass>
    Public Class UtxoSetTests

        Private Function MakeOutput(value As Long) As TransactionOutput
            Dim kp As New KeyPair()
            Dim address As String = AddressEncoder.FromKeyPair(kp)
            Dim script As Byte() = CryptoCoin.Transactions.Script.StandardScripts.CreateP2PKHOutput(address)
            Return New TransactionOutput(value, script)
        End Function

        Private Function MakeTxHash(n As Integer) As String
            Return MakeHashHex(n)
        End Function

        <TestMethod>
        Public Sub New_IsEmpty()
            Dim utxos As New UtxoSet()
            Assert.AreEqual(0, utxos.Count)
        End Sub

        <TestMethod>
        Public Sub Add_IncreasesCount()
            Dim utxos As New UtxoSet()
            utxos.Add(MakeTxHash(1), 0, MakeOutput(1000L), 1, False)
            Assert.AreEqual(1, utxos.Count)
        End Sub

        <TestMethod>
        Public Sub Contains_AfterAdd_ReturnsTrue()
            Dim utxos As New UtxoSet()
            Dim txHash As String = MakeTxHash(1)
            utxos.Add(txHash, 0, MakeOutput(1000L), 1, False)
            Assert.IsTrue(utxos.Contains(txHash, 0))
        End Sub

        <TestMethod>
        Public Sub Contains_NotAdded_ReturnsFalse()
            Dim utxos As New UtxoSet()
            Assert.IsFalse(utxos.Contains(MakeTxHash(99), 0))
        End Sub

        <TestMethod>
        Public Sub Get_AfterAdd_ReturnsEntry()
            Dim utxos As New UtxoSet()
            Dim txHash As String = MakeTxHash(1)
            Dim output As TransactionOutput = MakeOutput(5000L)
            utxos.Add(txHash, 0, output, 10, False)
            Dim entry As UtxoEntry = utxos.Get(txHash, 0)
            Assert.IsNotNull(entry)
            Assert.AreEqual(5000L, entry.Value)
            Assert.AreEqual(10, entry.BlockHeight)
        End Sub

        <TestMethod>
        Public Sub Get_NotAdded_ReturnsNothing()
            Dim utxos As New UtxoSet()
            Dim entry As UtxoEntry = utxos.Get(MakeTxHash(99), 0)
            Assert.IsNull(entry)
        End Sub

        <TestMethod>
        Public Sub Spend_RemovesEntry()
            Dim utxos As New UtxoSet()
            Dim txHash As String = MakeTxHash(1)
            utxos.Add(txHash, 0, MakeOutput(1000L), 1, False)
            utxos.Spend(txHash, 0)
            Assert.IsFalse(utxos.Contains(txHash, 0))
        End Sub

        <TestMethod>
        Public Sub Spend_ReturnsSpentEntry()
            Dim utxos As New UtxoSet()
            Dim txHash As String = MakeTxHash(1)
            utxos.Add(txHash, 0, MakeOutput(2500L), 5, False)
            Dim spent As UtxoEntry = utxos.Spend(txHash, 0)
            Assert.IsNotNull(spent)
            Assert.AreEqual(2500L, spent.Value)
        End Sub

        <TestMethod>
        Public Sub Spend_NonExistent_ReturnsNothing()
            Dim utxos As New UtxoSet()
            Dim result As UtxoEntry = utxos.Spend(MakeTxHash(99), 0)
            Assert.IsNull(result)
        End Sub

        <TestMethod>
        Public Sub TotalValue_SumsAllUtxos()
            Dim utxos As New UtxoSet()
            utxos.Add(MakeTxHash(1), 0, MakeOutput(1000L), 1, False)
            utxos.Add(MakeTxHash(2), 0, MakeOutput(2000L), 1, False)
            utxos.Add(MakeTxHash(3), 0, MakeOutput(3000L), 1, False)
            Assert.AreEqual(6000L, utxos.TotalValue)
        End Sub

        <TestMethod>
        Public Sub Clear_RemovesAllEntries()
            Dim utxos As New UtxoSet()
            utxos.Add(MakeTxHash(1), 0, MakeOutput(1000L), 1, False)
            utxos.Add(MakeTxHash(2), 0, MakeOutput(2000L), 1, False)
            utxos.Clear()
            Assert.AreEqual(0, utxos.Count)
        End Sub

        <TestMethod>
        Public Sub Contains_ByOutPoint_Works()
            Dim utxos As New UtxoSet()
            Dim txHash As String = MakeTxHash(1)
            utxos.Add(txHash, 0, MakeOutput(1000L), 1, False)
            Dim outpoint As New OutPoint(txHash, 0)
            Assert.IsTrue(utxos.Contains(outpoint))
        End Sub

        <TestMethod>
        Public Sub ApplyTransaction_CoinbaseTx_AddsOutputs()
            Dim utxos As New UtxoSet()
            Dim kp As New KeyPair()
            Dim address As String = AddressEncoder.FromKeyPair(kp)
            Dim tx As Transaction = Transaction.CreateCoinbase(1, 5000000000L, address)
            utxos.ApplyTransaction(tx, 1)
            Assert.AreEqual(1, utxos.Count)
        End Sub

    End Class

End Namespace
