Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports CryptoCoin.Transactions
Imports CryptoCoin.Cryptography

Namespace CryptoCoin.Tests.Transactions

    <TestClass>
    Public Class MempoolTests

        Private Function MakeTx(heightSeed As Integer) As Transaction
            Dim kp As New KeyPair()
            Dim address As String = AddressEncoder.FromKeyPair(kp)
            Return Transaction.CreateCoinbase(heightSeed, 5000000000L, address)
        End Function

        <TestMethod>
        Public Sub New_IsEmpty()
            Dim pool As New Mempool()
            Assert.AreEqual(0, pool.Count)
        End Sub

        <TestMethod>
        Public Sub Add_NonCoinbase_IncreasesCount()
            Dim pool As New Mempool()
            Dim tx As New Transaction()
            tx.Inputs.Add(New TransactionInput() With {
                .PreviousOutput = New OutPoint(MakeHashHex(1), 0)
            })
            tx.Outputs.Add(New TransactionOutput(1000L, New Byte() {}))
            Dim added As Boolean = pool.Add(tx, 100L)
            Assert.IsTrue(added)
            Assert.AreEqual(1, pool.Count)
        End Sub

        <TestMethod>
        Public Sub Add_Coinbase_ReturnsFalse()
            Dim pool As New Mempool()
            Dim kp As New KeyPair()
            Dim tx As Transaction = Transaction.CreateCoinbase(1, 5000000000L, AddressEncoder.FromKeyPair(kp))
            Dim added As Boolean = pool.Add(tx, 0L)
            Assert.IsFalse(added)
            Assert.AreEqual(0, pool.Count)
        End Sub

        <TestMethod>
        Public Sub Add_DuplicateTx_ReturnsFalse()
            Dim pool As New Mempool()
            Dim tx As New Transaction()
            tx.Inputs.Add(New TransactionInput() With {
                .PreviousOutput = New OutPoint(MakeHashHex(1), 0)
            })
            tx.Outputs.Add(New TransactionOutput(1000L, New Byte() {}))
            pool.Add(tx, 100L)
            Dim addedAgain As Boolean = pool.Add(tx, 100L)
            Assert.IsFalse(addedAgain)
            Assert.AreEqual(1, pool.Count)
        End Sub

        <TestMethod>
        Public Sub Contains_AfterAdd_ReturnsTrue()
            Dim pool As New Mempool()
            Dim tx As New Transaction()
            tx.Inputs.Add(New TransactionInput() With {
                .PreviousOutput = New OutPoint(MakeHashHex(1), 0)
            })
            tx.Outputs.Add(New TransactionOutput(1000L, New Byte() {}))
            pool.Add(tx, 100L)
            Assert.IsTrue(pool.Contains(tx.TxId))
        End Sub

        <TestMethod>
        Public Sub Contains_NotAdded_ReturnsFalse()
            Dim pool As New Mempool()
            Assert.IsFalse(pool.Contains(MakeHashHex(99)))
        End Sub

        <TestMethod>
        Public Sub Get_AfterAdd_ReturnsTx()
            Dim pool As New Mempool()
            Dim tx As New Transaction()
            tx.Inputs.Add(New TransactionInput() With {
                .PreviousOutput = New OutPoint(MakeHashHex(1), 0)
            })
            tx.Outputs.Add(New TransactionOutput(1000L, New Byte() {}))
            pool.Add(tx, 100L)
            Dim retrieved As Transaction = pool.Get(tx.TxId)
            Assert.IsNotNull(retrieved)
            Assert.AreEqual(tx.TxId, retrieved.TxId)
        End Sub

        <TestMethod>
        Public Sub Remove_DecreasesCount()
            Dim pool As New Mempool()
            Dim tx As New Transaction()
            tx.Inputs.Add(New TransactionInput() With {
                .PreviousOutput = New OutPoint(MakeHashHex(1), 0)
            })
            tx.Outputs.Add(New TransactionOutput(1000L, New Byte() {}))
            pool.Add(tx, 100L)
            pool.Remove(tx.TxId)
            Assert.AreEqual(0, pool.Count)
        End Sub

        <TestMethod>
        Public Sub RemoveAll_RemovesMultiple()
            Dim pool As New Mempool()
            Dim txIds As New List(Of String)()
            For i As Integer = 1 To 3
                Dim tx As New Transaction()
                tx.Inputs.Add(New TransactionInput() With {
                    .PreviousOutput = New OutPoint(MakeHashHex(i), 0)
                })
                tx.Outputs.Add(New TransactionOutput(1000L, New Byte() {}))
                pool.Add(tx, 100L)
                txIds.Add(tx.TxId)
            Next
            pool.RemoveAll(txIds)
            Assert.AreEqual(0, pool.Count)
        End Sub

        <TestMethod>
        Public Sub TotalFees_SumsAllFees()
            Dim pool As New Mempool()
            For i As Integer = 1 To 3
                Dim tx As New Transaction()
                tx.Inputs.Add(New TransactionInput() With {
                    .PreviousOutput = New OutPoint(MakeHashHex(i), 0)
                })
                tx.Outputs.Add(New TransactionOutput(1000L, New Byte() {}))
                pool.Add(tx, 1000L)
            Next
            Assert.AreEqual(3000L, pool.TotalFees)
        End Sub

        <TestMethod>
        Public Sub GetByFeeRate_ReturnsSortedByFeeRateDescending()
            Dim pool As New Mempool()
            For i As Integer = 1 To 3
                Dim tx As New Transaction()
                tx.Inputs.Add(New TransactionInput() With {
                    .PreviousOutput = New OutPoint(MakeHashHex(i), 0)
                })
                tx.Outputs.Add(New TransactionOutput(1000L, New Byte() {}))
                pool.Add(tx, CLng(i) * 1000L) ' Different fees
            Next
            Dim entries As List(Of MempoolEntry) = pool.GetByFeeRate()
            Assert.AreEqual(3, entries.Count)
            ' Should be sorted highest fee rate first
            For i As Integer = 0 To entries.Count - 2
                Assert.IsTrue(entries(i).FeeRate >= entries(i + 1).FeeRate)
            Next
        End Sub

        <TestMethod>
        Public Sub Clear_EmptiesPool()
            Dim pool As New Mempool()
            Dim tx As New Transaction()
            tx.Inputs.Add(New TransactionInput() With {
                .PreviousOutput = New OutPoint(MakeHashHex(1), 0)
            })
            tx.Outputs.Add(New TransactionOutput(1000L, New Byte() {}))
            pool.Add(tx, 100L)
            pool.Clear()
            Assert.AreEqual(0, pool.Count)
        End Sub

    End Class

End Namespace
