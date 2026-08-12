Namespace CryptoCoin.Core

    ''' <summary>
    ''' Defines and enforces the consensus rules for the CryptoCoin network.
    ''' All nodes must agree on these rules for the network to function.
    ''' </summary>
    Public Class ConsensusRules

        Private ReadOnly _params As ChainParameters

        Public Sub New(params As ChainParameters)
            If params Is Nothing Then Throw New ArgumentNullException(NameOf(params))
            _params = params
        End Sub

        ''' <summary>
        ''' Validates that a coinbase transaction reward is correct for the given height.
        ''' </summary>
        Public Function ValidateCoinbaseReward(height As Integer, actualReward As Long, totalFees As Long) As Boolean
            Dim expectedReward As Long = _params.GetBlockReward(height)
            Dim maxAllowed As Long = expectedReward + totalFees
            Return actualReward <= maxAllowed
        End Function

        ''' <summary>
        ''' Checks if a coinbase transaction output is mature enough to spend.
        ''' </summary>
        Public Function IsCoinbaseMature(coinbaseHeight As Integer, currentHeight As Integer) As Boolean
            Return (currentHeight - coinbaseHeight) >= _params.CoinbaseMaturity
        End Function

        ''' <summary>
        ''' Validates the block reward for a given height.
        ''' </summary>
        Public Function GetBlockReward(height As Integer) As Long
            Return _params.GetBlockReward(height)
        End Function

        ''' <summary>
        ''' Calculates the minimum transaction fee based on transaction size.
        ''' </summary>
        Public Function GetMinimumFee(transactionSizeBytes As Integer) As Long
            Return CLng(transactionSizeBytes) * _params.MinFeePerByte
        End Function

        ''' <summary>
        ''' Validates that a transaction fee meets the minimum requirement.
        ''' </summary>
        Public Function ValidateFee(fee As Long, transactionSizeBytes As Integer) As Boolean
            Return fee >= GetMinimumFee(transactionSizeBytes)
        End Function

        ''' <summary>
        ''' Gets the maximum allowed block size.
        ''' </summary>
        Public Function GetMaxBlockSize() As Integer
            Return _params.MaxBlockSize
        End Function

        ''' <summary>
        ''' Gets the maximum number of signature operations allowed per block.
        ''' </summary>
        Public Function GetMaxSigOps() As Integer
            Return _params.MaxSigOpsPerBlock
        End Function

        ''' <summary>
        ''' Validates the median time past rule.
        ''' A block's timestamp must be greater than the median of the last 11 blocks.
        ''' </summary>
        Public Function ValidateMedianTimePast(blockTimestamp As Long, previousTimestamps As List(Of Long)) As Boolean
            If previousTimestamps Is Nothing OrElse previousTimestamps.Count = 0 Then
                Return True
            End If

            ' Get last 11 timestamps (or fewer if chain is shorter)
            Dim count As Integer = Math.Min(11, previousTimestamps.Count)
            Dim recent As New List(Of Long)()
            For i As Integer = previousTimestamps.Count - count To previousTimestamps.Count - 1
                recent.Add(previousTimestamps(i))
            Next

            recent.Sort()
            Dim median As Long = recent(recent.Count \ 2)

            Return blockTimestamp > median
        End Function

        ''' <summary>
        ''' Checks if a transaction is a valid coinbase transaction.
        ''' </summary>
        Public Function IsCoinbaseTransaction(txInputCount As Integer, hasPreviousOutput As Boolean) As Boolean
            ' Coinbase has exactly one input with no previous output reference
            Return txInputCount = 1 AndAlso Not hasPreviousOutput
        End Function

        ''' <summary>
        ''' Gets the total supply at a given height.
        ''' </summary>
        Public Function GetTotalSupplyAtHeight(height As Integer) As Long
            Dim total As Long = 0
            Dim reward As Long = _params.InitialBlockReward
            Dim h As Integer = 0

            While h <= height AndAlso reward > 0
                Dim blocksInEra As Integer = Math.Min(_params.HalvingInterval, height - h + 1)
                total += reward * blocksInEra
                h += _params.HalvingInterval
                reward >>= 1
            End While

            Return Math.Min(total, _params.MaxSupply)
        End Function

    End Class

End Namespace
