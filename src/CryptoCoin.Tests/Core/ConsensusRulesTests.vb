Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports CryptoCoin.Core

Namespace CryptoCoin.Tests.Core

    <TestClass>
    Public Class ConsensusRulesTests

        Private ReadOnly _params As ChainParameters = ChainParameters.Mainnet()
        Private ReadOnly _rules As ConsensusRules

        Public Sub New()
            _rules = New ConsensusRules(_params)
        End Sub

        <TestMethod>
        Public Sub GetBlockReward_Height0_Returns50CRC()
            Dim reward As Long = _rules.GetBlockReward(0)
            Assert.AreEqual(_params.InitialBlockReward, reward)
        End Sub

        <TestMethod>
        Public Sub GetBlockReward_AfterFirstHalving_Returns25CRC()
            Dim reward As Long = _rules.GetBlockReward(_params.HalvingInterval)
            Assert.AreEqual(_params.InitialBlockReward \ 2, reward)
        End Sub

        <TestMethod>
        Public Sub GetBlockReward_AfterSecondHalving_Returns12_5CRC()
            Dim reward As Long = _rules.GetBlockReward(_params.HalvingInterval * 2)
            Assert.AreEqual(_params.InitialBlockReward \ 4, reward)
        End Sub

        <TestMethod>
        Public Sub GetBlockReward_After64Halvings_ReturnsZero()
            Dim reward As Long = _rules.GetBlockReward(_params.HalvingInterval * 64)
            Assert.AreEqual(0L, reward)
        End Sub

        <TestMethod>
        Public Sub ValidateCoinbaseReward_ExactReward_ReturnsTrue()
            Dim reward As Long = _rules.GetBlockReward(0)
            Assert.IsTrue(_rules.ValidateCoinbaseReward(0, reward, 0))
        End Sub

        <TestMethod>
        Public Sub ValidateCoinbaseReward_RewardPlusFees_ReturnsTrue()
            Dim reward As Long = _rules.GetBlockReward(0)
            Dim fees As Long = 100000L
            Assert.IsTrue(_rules.ValidateCoinbaseReward(0, reward + fees, fees))
        End Sub

        <TestMethod>
        Public Sub ValidateCoinbaseReward_TooMuch_ReturnsFalse()
            Dim reward As Long = _rules.GetBlockReward(0)
            Assert.IsFalse(_rules.ValidateCoinbaseReward(0, reward + 1, 0))
        End Sub

        <TestMethod>
        Public Sub IsCoinbaseMature_BelowMaturity_ReturnsFalse()
            Assert.IsFalse(_rules.IsCoinbaseMature(0, _params.CoinbaseMaturity - 1))
        End Sub

        <TestMethod>
        Public Sub IsCoinbaseMature_AtMaturity_ReturnsTrue()
            Assert.IsTrue(_rules.IsCoinbaseMature(0, _params.CoinbaseMaturity))
        End Sub

        <TestMethod>
        Public Sub IsCoinbaseMature_AboveMaturity_ReturnsTrue()
            Assert.IsTrue(_rules.IsCoinbaseMature(0, _params.CoinbaseMaturity + 100))
        End Sub

        <TestMethod>
        Public Sub GetMinimumFee_100Bytes_Returns100Satoshis()
            Dim fee As Long = _rules.GetMinimumFee(100)
            Assert.AreEqual(100L * _params.MinFeePerByte, fee)
        End Sub

        <TestMethod>
        Public Sub ValidateFee_AboveMinimum_ReturnsTrue()
            Dim minFee As Long = _rules.GetMinimumFee(250)
            Assert.IsTrue(_rules.ValidateFee(minFee + 1, 250))
        End Sub

        <TestMethod>
        Public Sub ValidateFee_BelowMinimum_ReturnsFalse()
            Dim minFee As Long = _rules.GetMinimumFee(250)
            Assert.IsFalse(_rules.ValidateFee(minFee - 1, 250))
        End Sub

        <TestMethod>
        Public Sub GetTotalSupplyAtHeight_Zero_ReturnsFirstBlockReward()
            Dim supply As Long = _rules.GetTotalSupplyAtHeight(0)
            Assert.AreEqual(_params.InitialBlockReward, supply)
        End Sub

        <TestMethod>
        Public Sub GetTotalSupplyAtHeight_NeverExceedsMaxSupply()
            Dim supply As Long = _rules.GetTotalSupplyAtHeight(10000000)
            Assert.IsTrue(supply <= _params.MaxSupply)
        End Sub

        <TestMethod>
        Public Sub ValidateMedianTimePast_NewTimestamp_ReturnsTrue()
            Dim timestamps As New List(Of Long)() From {100, 200, 300, 400, 500}
            Assert.IsTrue(_rules.ValidateMedianTimePast(600, timestamps))
        End Sub

        <TestMethod>
        Public Sub ValidateMedianTimePast_OldTimestamp_ReturnsFalse()
            Dim timestamps As New List(Of Long)() From {100, 200, 300, 400, 500, 600, 700, 800, 900, 1000, 1100}
            ' Median of 11 values is the 6th = 600; timestamp must be > 600
            Assert.IsFalse(_rules.ValidateMedianTimePast(500, timestamps))
        End Sub

    End Class

End Namespace
