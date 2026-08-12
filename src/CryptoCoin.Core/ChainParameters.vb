Namespace CryptoCoin.Core

    ''' <summary>
    ''' Defines the consensus parameters for the CryptoCoin blockchain.
    ''' These parameters control block timing, rewards, difficulty adjustment, etc.
    ''' </summary>
    Public Class ChainParameters

        ''' <summary>
        ''' Target time between blocks in seconds (2 minutes).
        ''' </summary>
        Public Property TargetBlockTimeSeconds As Integer = 120

        ''' <summary>
        ''' Number of blocks between difficulty adjustments.
        ''' </summary>
        Public Property DifficultyAdjustmentInterval As Integer = 1008

        ''' <summary>
        ''' Maximum allowed time for a difficulty adjustment period (4x target).
        ''' </summary>
        Public ReadOnly Property MaxAdjustmentTimespan As Long
            Get
                Return CLng(TargetBlockTimeSeconds) * DifficultyAdjustmentInterval * 4
            End Get
        End Property

        ''' <summary>
        ''' Minimum allowed time for a difficulty adjustment period (1/4 target).
        ''' </summary>
        Public ReadOnly Property MinAdjustmentTimespan As Long
            Get
                Return CLng(TargetBlockTimeSeconds) * DifficultyAdjustmentInterval \ 4
            End Get
        End Property

        ''' <summary>
        ''' Initial block reward in satoshis (50 CRC).
        ''' </summary>
        Public Property InitialBlockReward As Long = 5000000000L

        ''' <summary>
        ''' Number of blocks between reward halvings.
        ''' </summary>
        Public Property HalvingInterval As Integer = 210000

        ''' <summary>
        ''' Maximum total supply in satoshis (21 million CRC).
        ''' </summary>
        Public Property MaxSupply As Long = 2100000000000000L

        ''' <summary>
        ''' Maximum block size in bytes (1 MB).
        ''' </summary>
        Public Property MaxBlockSize As Integer = 1048576

        ''' <summary>
        ''' Maximum number of transactions per block.
        ''' </summary>
        Public Property MaxTransactionsPerBlock As Integer = 5000

        ''' <summary>
        ''' Minimum transaction fee in satoshis per byte.
        ''' </summary>
        Public Property MinFeePerByte As Long = 1

        ''' <summary>
        ''' Maximum allowed block timestamp drift from network time (2 hours).
        ''' </summary>
        Public Property MaxTimeDriftSeconds As Integer = 7200

        ''' <summary>
        ''' Number of confirmations required for coinbase maturity.
        ''' </summary>
        Public Property CoinbaseMaturity As Integer = 100

        ''' <summary>
        ''' Maximum number of signature operations per block.
        ''' </summary>
        Public Property MaxSigOpsPerBlock As Integer = 20000

        ''' <summary>
        ''' The genesis block hash (set after genesis block creation).
        ''' </summary>
        Public Property GenesisBlockHash As String = ""

        ''' <summary>
        ''' Network magic bytes for message identification.
        ''' </summary>
        Public Property NetworkMagic As Byte() = New Byte() {&HCC, &HC0, &H1A, &HCC}

        ''' <summary>
        ''' Default network port.
        ''' </summary>
        Public Property DefaultPort As Integer = 8333

        ''' <summary>
        ''' Protocol version.
        ''' </summary>
        Public Property ProtocolVersion As Integer = 1

        ''' <summary>
        ''' Coin ticker symbol.
        ''' </summary>
        Public Property CoinSymbol As String = "CRC"

        ''' <summary>
        ''' Coin name.
        ''' </summary>
        Public Property CoinName As String = "CryptoCoin"

        ''' <summary>
        ''' Number of decimal places (satoshis = 10^8).
        ''' </summary>
        Public Property DecimalPlaces As Integer = 8

        ''' <summary>
        ''' One full coin in satoshis.
        ''' </summary>
        Public ReadOnly Property OneCoin As Long
            Get
                Return CLng(Math.Pow(10, DecimalPlaces))
            End Get
        End Property

        ''' <summary>
        ''' Gets the block reward for a given block height.
        ''' </summary>
        Public Function GetBlockReward(height As Integer) As Long
            Dim halvings As Integer = height \ HalvingInterval
            If halvings >= 64 Then Return 0
            Return InitialBlockReward >> halvings
        End Function

        ''' <summary>
        ''' Creates the default mainnet parameters.
        ''' </summary>
        Public Shared Function Mainnet() As ChainParameters
            Return New ChainParameters()
        End Function

        ''' <summary>
        ''' Creates testnet parameters with faster block times.
        ''' </summary>
        Public Shared Function Testnet() As ChainParameters
            Dim p As New ChainParameters()
            p.TargetBlockTimeSeconds = 30
            p.DifficultyAdjustmentInterval = 504
            p.DefaultPort = 18333
            p.NetworkMagic = New Byte() {&HAC, &HCC, &H0A, &HAE}
            Return p
        End Function

        ''' <summary>
        ''' Creates regtest parameters for local development.
        ''' </summary>
        Public Shared Function Regtest() As ChainParameters
            Dim p As New ChainParameters()
            p.TargetBlockTimeSeconds = 1
            p.DifficultyAdjustmentInterval = 10
            p.DefaultPort = 28333
            p.CoinbaseMaturity = 1
            p.NetworkMagic = New Byte() {&HBE, &HAB, &HAC, &HA0}
            Return p
        End Function

    End Class

End Namespace
