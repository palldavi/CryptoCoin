Imports System.Numerics
Imports CryptoCoin.Cryptography

Namespace CryptoCoin.Core

    ''' <summary>
    ''' Calculates and adjusts the proof-of-work difficulty target.
    ''' Difficulty adjusts every N blocks to maintain target block time.
    ''' </summary>
    Public NotInheritable Class DifficultyCalculator

        Private Sub New()
        End Sub

        ''' <summary>
        ''' Minimum difficulty bits (easiest difficulty, used for genesis and regtest).
        ''' </summary>
        Public Const MinDifficultyBits As UInteger = &H1F00FFFFUI

        ''' <summary>
        ''' Maximum difficulty (hardest possible target = 1).
        ''' </summary>
        Public Shared ReadOnly MaxTarget As BigInteger = BigInteger.Parse("00000000FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", Globalization.NumberStyles.HexNumber)

        ''' <summary>
        ''' Calculates the next difficulty target based on the actual time taken for the last period.
        ''' </summary>
        Public Shared Function CalculateNextTarget(currentBits As UInteger, actualTimespan As Long, params As ChainParameters) As UInteger
            ' Clamp the timespan to prevent extreme adjustments
            Dim clampedTimespan As Long = actualTimespan
            If clampedTimespan < params.MinAdjustmentTimespan Then
                clampedTimespan = params.MinAdjustmentTimespan
            End If
            If clampedTimespan > params.MaxAdjustmentTimespan Then
                clampedTimespan = params.MaxAdjustmentTimespan
            End If

            ' Calculate expected timespan
            Dim expectedTimespan As Long = CLng(params.TargetBlockTimeSeconds) * params.DifficultyAdjustmentInterval

            ' Get current target
            Dim currentTarget As BigInteger = BitsToTarget(currentBits)

            ' New target = current target * actual time / expected time
            Dim newTarget As BigInteger = BigInteger.Divide(currentTarget * clampedTimespan, New BigInteger(expectedTimespan))

            ' Ensure we don't exceed the maximum (easiest) target
            If newTarget > MaxTarget Then
                newTarget = MaxTarget
            End If

            Return TargetToBits(newTarget)
        End Function

        ''' <summary>
        ''' Converts compact bits format to a full target value.
        ''' </summary>
        Public Shared Function BitsToTarget(bits As UInteger) As BigInteger
            Dim exponent As Integer = CInt(bits >> 24)
            Dim coefficient As BigInteger = New BigInteger(bits And &HFFFFFFUI)

            If exponent <= 3 Then
                coefficient >>= (8 * (3 - exponent))
            Else
                coefficient <<= (8 * (exponent - 3))
            End If

            Return coefficient
        End Function

        ''' <summary>
        ''' Converts a full target value to compact bits format.
        ''' </summary>
        Public Shared Function TargetToBits(target As BigInteger) As UInteger
            If target = BigInteger.Zero Then Return 0

            ' Get byte representation
            Dim bytes As Byte() = target.ToByteArray()
            ' Remove trailing zeros (BigInteger is little-endian)
            Dim size As Integer = bytes.Length
            While size > 1 AndAlso bytes(size - 1) = 0
                size -= 1
            End While

            ' Handle sign byte
            If (bytes(size - 1) And &H80) <> 0 Then
                size += 1
            End If

            Dim compact As UInteger
            If size <= 3 Then
                compact = CUInt(target) << (8 * (3 - size))
            Else
                Dim shifted As BigInteger = target >> (8 * (size - 3))
                compact = CUInt(shifted And &HFFFFFFUI)
            End If

            compact = compact Or (CUInt(size) << 24)
            Return compact
        End Function

        ''' <summary>
        ''' Calculates the work represented by a given difficulty target.
        ''' Work = 2^256 / (target + 1)
        ''' </summary>
        Public Shared Function GetBlockWork(bits As UInteger) As BigInteger
            Dim target As BigInteger = BitsToTarget(bits)
            If target = BigInteger.Zero Then Return BigInteger.Zero

            ' work = 2^256 / (target + 1)
            Dim maxVal As BigInteger = BigInteger.One << 256
            Return BigInteger.Divide(maxVal, target + BigInteger.One)
        End Function

        ''' <summary>
        ''' Calculates the difficulty ratio relative to the minimum difficulty.
        ''' </summary>
        Public Shared Function GetDifficultyRatio(bits As UInteger) As Double
            Dim target As BigInteger = BitsToTarget(bits)
            If target = BigInteger.Zero Then Return Double.MaxValue

            Dim minTarget As BigInteger = BitsToTarget(MinDifficultyBits)
            ' difficulty = minTarget / target
            Return CDbl(minTarget) / CDbl(target)
        End Function

        ''' <summary>
        ''' Estimates the hash rate based on difficulty and block time.
        ''' </summary>
        Public Shared Function EstimateHashRate(bits As UInteger, blockTimeSeconds As Integer) As Double
            Dim difficulty As Double = GetDifficultyRatio(bits)
            ' hashrate = difficulty * 2^32 / blockTime
            Return difficulty * Math.Pow(2, 32) / blockTimeSeconds
        End Function

        ''' <summary>
        ''' Checks if a hash meets the given difficulty target.
        ''' </summary>
        Public Shared Function MeetsTarget(hash As Byte(), bits As UInteger) As Boolean
            Dim target As BigInteger = BitsToTarget(bits)
            Dim hashValue As BigInteger = FromByteArrayUnsigned(hash)
            Return hashValue <= target
        End Function

    End Class

End Namespace
