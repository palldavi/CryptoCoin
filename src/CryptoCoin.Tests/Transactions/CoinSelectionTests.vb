Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports CryptoCoin.Transactions
Imports CryptoCoin.Cryptography

Namespace CryptoCoin.Tests.Transactions

    <TestClass>
    Public Class CoinSelectionTests

        Private Function MakeUtxo(value As Long, index As Integer) As UtxoEntry
            Dim kp As New KeyPair()
            Dim address As String = AddressEncoder.FromKeyPair(kp)
            Dim script As Byte() = CryptoCoin.Transactions.Script.StandardScripts.CreateP2PKHOutput(address)
            Dim output As New TransactionOutput(value, script)
            Return New UtxoEntry(output, 1, False, MakeHashHex(index), 0)
        End Function

        <TestMethod>
        Public Sub SelectCoins_SufficientFunds_Succeeds()
            Dim utxos As New List(Of UtxoEntry)()
            utxos.Add(MakeUtxo(1000000L, 1))
            utxos.Add(MakeUtxo(2000000L, 2))
            utxos.Add(MakeUtxo(3000000L, 3))
            Dim result As CoinSelectionResult = CoinSelection.SelectCoins(utxos, 500000L, 10L)
            Assert.IsTrue(result.Success, $"Selection should succeed: {result.ErrorMessage}")
        End Sub

        <TestMethod>
        Public Sub SelectCoins_InsufficientFunds_Fails()
            Dim utxos As New List(Of UtxoEntry)()
            utxos.Add(MakeUtxo(100L, 1))
            Dim result As CoinSelectionResult = CoinSelection.SelectCoins(utxos, 1000000L, 10L)
            Assert.IsFalse(result.Success)
            Assert.IsFalse(String.IsNullOrEmpty(result.ErrorMessage))
        End Sub

        <TestMethod>
        Public Sub SelectCoins_EmptyUtxos_Fails()
            Dim result As CoinSelectionResult = CoinSelection.SelectCoins(New List(Of UtxoEntry)(), 1000L, 10L)
            Assert.IsFalse(result.Success)
        End Sub

        <TestMethod>
        Public Sub SelectCoins_SelectedTotalCoversTarget()
            Dim utxos As New List(Of UtxoEntry)()
            For i As Integer = 1 To 5
                utxos.Add(MakeUtxo(CLng(i) * 100000L, i))
            Next
            Dim target As Long = 250000L
            Dim result As CoinSelectionResult = CoinSelection.SelectCoins(utxos, target, 1L)
            Assert.IsTrue(result.Success)
            Assert.IsTrue(result.TotalInput >= target + result.Fee,
                "Selected inputs must cover target + fee")
        End Sub

        <TestMethod>
        Public Sub SelectCoins_SelectedUtxosNotEmpty()
            Dim utxos As New List(Of UtxoEntry)()
            utxos.Add(MakeUtxo(5000000L, 1))
            Dim result As CoinSelectionResult = CoinSelection.SelectCoins(utxos, 1000000L, 1L)
            Assert.IsTrue(result.Success)
            Assert.IsTrue(result.SelectedUtxos.Count > 0)
        End Sub

        <TestMethod>
        Public Sub SelectCoins_FeeIsPositive()
            Dim utxos As New List(Of UtxoEntry)()
            utxos.Add(MakeUtxo(5000000L, 1))
            Dim result As CoinSelectionResult = CoinSelection.SelectCoins(utxos, 1000000L, 10L)
            Assert.IsTrue(result.Success)
            Assert.IsTrue(result.Fee > 0)
        End Sub

        <TestMethod>
        Public Sub EstimateSize_1Input2Outputs_ReturnsReasonableSize()
            Dim size As Integer = CoinSelection.EstimateSize(1, 2)
            ' 10 + 148 + 68 = 226 bytes typical
            Assert.IsTrue(size > 100 AndAlso size < 500,
                $"Estimated size {size} should be between 100 and 500 bytes")
        End Sub

        <TestMethod>
        Public Sub EstimateSize_MoreInputs_LargerSize()
            Dim size1 As Integer = CoinSelection.EstimateSize(1, 2)
            Dim size5 As Integer = CoinSelection.EstimateSize(5, 2)
            Assert.IsTrue(size5 > size1)
        End Sub

    End Class

End Namespace
