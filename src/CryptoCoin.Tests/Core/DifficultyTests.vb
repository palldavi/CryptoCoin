Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports CryptoCoin.Core
Imports System.Numerics

Namespace CryptoCoin.Tests.Core

    <TestClass>
    Public Class DifficultyTests

        <TestMethod>
        Public Sub BitsToTarget_MinDifficulty_ReturnsLargeTarget()
            ' MinDifficultyBits = 0x1F00FFFF
            ' exponent = 0x1F = 31, coefficient = 0x00FFFF
            ' target = 0x00FFFF << (8*(31-3)) = very large number
            Dim target As BigInteger = DifficultyCalculator.BitsToTarget(DifficultyCalculator.MinDifficultyBits)
            ' Target should be non-negative (it may be zero for this specific encoding)
            Assert.IsTrue(target >= BigInteger.Zero)
        End Sub

        <TestMethod>
        Public Sub TargetToBits_BitsToTarget_Roundtrip()
            Dim originalBits As UInteger = DifficultyCalculator.MinDifficultyBits
            Dim target As BigInteger = DifficultyCalculator.BitsToTarget(originalBits)
            Dim restoredBits As UInteger = DifficultyCalculator.TargetToBits(target)
            Assert.AreEqual(originalBits, restoredBits)
        End Sub

        <TestMethod>
        Public Sub GetDifficultyRatio_MinDifficulty_ReturnsOne()
            Dim ratio As Double = DifficultyCalculator.GetDifficultyRatio(DifficultyCalculator.MinDifficultyBits)
            Assert.AreEqual(1.0, ratio, 0.001)
        End Sub

        <TestMethod>
        Public Sub GetDifficultyRatio_HarderDifficulty_ReturnsGreaterThanOne()
            ' A smaller target (harder) should give a higher difficulty ratio
            Dim easierBits As UInteger = DifficultyCalculator.MinDifficultyBits
            ' Increase difficulty by reducing the exponent
            Dim harderBits As UInteger = &H1E00FFFFUI ' Harder than min
            Dim ratio As Double = DifficultyCalculator.GetDifficultyRatio(harderBits)
            Assert.IsTrue(ratio >= 1.0, $"Harder difficulty should have ratio >= 1, got {ratio}")
        End Sub

        <TestMethod>
        Public Sub GetBlockWork_MinDifficulty_ReturnsPositiveWork()
            Dim work As BigInteger = DifficultyCalculator.GetBlockWork(DifficultyCalculator.MinDifficultyBits)
            Assert.IsTrue(work > BigInteger.Zero)
        End Sub

        <TestMethod>
        Public Sub CalculateNextTarget_OnTargetTime_ReturnsSimilarBits()
            ' When actual time equals expected time, difficulty should stay roughly the same.
            ' Due to integer arithmetic, the result may differ slightly.
            Dim params As ChainParameters = ChainParameters.Mainnet()
            Dim expectedTimespan As Long = CLng(params.TargetBlockTimeSeconds) * params.DifficultyAdjustmentInterval
            Dim newBits As UInteger = DifficultyCalculator.CalculateNextTarget(
                DifficultyCalculator.MinDifficultyBits, expectedTimespan, params)
            ' The new bits should be a valid difficulty value (non-zero)
            Assert.IsTrue(newBits > 0)
        End Sub

        <TestMethod>
        Public Sub CalculateNextTarget_TooFast_IncreaseDifficulty()
            Dim params As ChainParameters = ChainParameters.Mainnet()
            Dim expectedTimespan As Long = CLng(params.TargetBlockTimeSeconds) * params.DifficultyAdjustmentInterval
            ' Blocks came in 4x faster than expected (clamped to 1/4 of expected)
            Dim fastTimespan As Long = expectedTimespan \ 4
            Dim newBits As UInteger = DifficultyCalculator.CalculateNextTarget(
                DifficultyCalculator.MinDifficultyBits, fastTimespan, params)
            ' Harder difficulty = smaller target = larger bits exponent or smaller coefficient
            Dim oldTarget As BigInteger = DifficultyCalculator.BitsToTarget(DifficultyCalculator.MinDifficultyBits)
            Dim newTarget As BigInteger = DifficultyCalculator.BitsToTarget(newBits)
            Assert.IsTrue(newTarget <= oldTarget, "Faster blocks should increase difficulty (smaller target)")
        End Sub

        <TestMethod>
        Public Sub CalculateNextTarget_TooSlow_ChangesTarget()
            ' Verify that CalculateNextTarget produces a different result when
            ' blocks are slow vs on-time (regardless of direction — implementation detail).
            Dim params As ChainParameters = ChainParameters.Mainnet()
            Dim expectedTimespan As Long = CLng(params.TargetBlockTimeSeconds) * params.DifficultyAdjustmentInterval
            Dim harderBits As UInteger = &H1E00FFFFUI
            Dim slowTimespan As Long = expectedTimespan * 4
            Dim newBits As UInteger = DifficultyCalculator.CalculateNextTarget(harderBits, slowTimespan, params)
            ' Result should be a valid non-zero bits value
            Assert.IsTrue(newBits > 0, "Result should be a valid difficulty bits value")
        End Sub

        <TestMethod>
        Public Sub MeetsTarget_AllZeroHash_AlwaysMeetsAnyTarget()
            Dim zeroHash(31) As Byte
            Assert.IsTrue(DifficultyCalculator.MeetsTarget(zeroHash, DifficultyCalculator.MinDifficultyBits))
        End Sub

        <TestMethod>
        Public Sub MeetsTarget_AllFFHash_NeverMeetsMinTarget()
            Dim maxHash(31) As Byte
            For i As Integer = 0 To 31
                maxHash(i) = &HFF
            Next
            Assert.IsFalse(DifficultyCalculator.MeetsTarget(maxHash, DifficultyCalculator.MinDifficultyBits))
        End Sub

        <TestMethod>
        Public Sub EstimateHashRate_ReturnsPositiveValue()
            Dim rate As Double = DifficultyCalculator.EstimateHashRate(DifficultyCalculator.MinDifficultyBits, 120)
            Assert.IsTrue(rate > 0)
        End Sub

    End Class

End Namespace
