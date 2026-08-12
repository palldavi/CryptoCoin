Imports System.Threading
Imports CryptoCoin.Core
Imports CryptoCoin.Cryptography

Namespace CryptoCoin.Mining

    ''' <summary>
    ''' The proof-of-work miner that searches for valid block hashes.
    ''' Supports multi-threaded mining with cancellation.
    ''' </summary>
    Public Class Miner

        Private ReadOnly _blockchain As Blockchain
        Private ReadOnly _assembler As BlockAssembler
        Private ReadOnly _monitor As HashRateMonitor
        Private _cancellationSource As CancellationTokenSource
        Private _isMining As Boolean
        Private _minerAddress As String
        Private _threadCount As Integer

        ''' <summary>
        ''' Gets whether the miner is currently running.
        ''' </summary>
        Public ReadOnly Property IsMining As Boolean
            Get
                Return _isMining
            End Get
        End Property

        ''' <summary>
        ''' Gets the current hash rate in hashes per second.
        ''' </summary>
        Public ReadOnly Property HashRate As Double
            Get
                Return _monitor.CurrentHashRate
            End Get
        End Property

        ''' <summary>
        ''' Gets the total number of hashes computed.
        ''' </summary>
        Public ReadOnly Property TotalHashes As Long
            Get
                Return _monitor.TotalHashes
            End Get
        End Property

        ''' <summary>
        ''' Gets the number of blocks mined.
        ''' </summary>
        Public Property BlocksMined As Integer

        ''' <summary>
        ''' Event raised when a valid block is found.
        ''' </summary>
        Public Event BlockFound As EventHandler(Of BlockFoundEventArgs)

        ''' <summary>
        ''' Event raised periodically with mining status updates.
        ''' </summary>
        Public Event StatusUpdate As EventHandler(Of MiningStatusEventArgs)

        Public Sub New(blockchain As Blockchain, assembler As BlockAssembler)
            If blockchain Is Nothing Then Throw New ArgumentNullException(NameOf(blockchain))
            If assembler Is Nothing Then Throw New ArgumentNullException(NameOf(assembler))
            _blockchain = blockchain
            _assembler = assembler
            _monitor = New HashRateMonitor()
            _threadCount = Environment.ProcessorCount
        End Sub

        ''' <summary>
        ''' Starts mining with the given miner address.
        ''' </summary>
        Public Sub Start(minerAddress As String, Optional threadCount As Integer = 0)
            If _isMining Then Return
            If String.IsNullOrEmpty(minerAddress) Then Throw New ArgumentNullException(NameOf(minerAddress))

            _minerAddress = minerAddress
            _threadCount = If(threadCount > 0, threadCount, Environment.ProcessorCount)
            _cancellationSource = New CancellationTokenSource()
            _isMining = True
            _monitor.Reset()

            ' Start mining threads
            For i As Integer = 0 To _threadCount - 1
                Dim threadIndex As Integer = i
                Dim thread As New Thread(Sub() MineLoop(threadIndex, _cancellationSource.Token))
                thread.IsBackground = True
                thread.Name = $"Miner-{i}"
                thread.Start()
            Next
        End Sub

        ''' <summary>
        ''' Stops mining.
        ''' </summary>
        Public Sub [Stop]()
            If Not _isMining Then Return
            _cancellationSource?.Cancel()
            _isMining = False
        End Sub

        ''' <summary>
        ''' The main mining loop for each thread.
        ''' </summary>
        Private Sub MineLoop(threadIndex As Integer, token As CancellationToken)
            Try
                While Not token.IsCancellationRequested
                    ' Assemble a candidate block
                    Dim job As MiningJob = _assembler.CreateJob(_minerAddress, _blockchain)
                    If job Is Nothing Then
                        Thread.Sleep(1000)
                        Continue While
                    End If

                    ' Assign nonce range to this thread
                    Dim nonceStart As UInteger = CUInt(threadIndex) * (UInteger.MaxValue \ CUInt(_threadCount))
                    Dim nonceEnd As UInteger = CUInt(threadIndex + 1) * (UInteger.MaxValue \ CUInt(_threadCount))
                    If threadIndex = _threadCount - 1 Then nonceEnd = UInteger.MaxValue

                    ' Mine
                    Dim result As MiningResult = MineBlock(job, nonceStart, nonceEnd, token)

                    If result IsNot Nothing AndAlso result.Found Then
                        ' Valid block found
                        job.Block.Header.Nonce = result.Nonce
                        BlocksMined += 1

                        ' Submit to blockchain
                        Dim validationResult = _blockchain.AddBlock(job.Block)

                        RaiseEvent BlockFound(Me, New BlockFoundEventArgs(job.Block, result.HashesComputed))
                    End If
                End While
            Catch ex As OperationCanceledException
                ' Normal cancellation
            End Try
        End Sub

        ''' <summary>
        ''' Mines a single block by iterating through nonces.
        ''' </summary>
        Private Function MineBlock(job As MiningJob, nonceStart As UInteger, nonceEnd As UInteger, token As CancellationToken) As MiningResult
            Dim header As BlockHeader = job.Block.Header
            Dim hashCount As Long = 0
            Dim reportInterval As Integer = 100000

            Dim nonce As UInteger = nonceStart
            While nonce < nonceEnd
                If token.IsCancellationRequested Then Return Nothing

                header.Nonce = nonce
                hashCount += 1

                If header.MeetsTarget() Then
                    _monitor.AddHashes(hashCount)
                    Return New MiningResult(True, nonce, hashCount)
                End If

                ' Periodic reporting
                If hashCount Mod reportInterval = 0 Then
                    _monitor.AddHashes(CLng(reportInterval))
                    hashCount -= reportInterval

                    RaiseEvent StatusUpdate(Me, New MiningStatusEventArgs(
                        _monitor.CurrentHashRate, _monitor.TotalHashes, job.Block.Height))
                End If

                nonce += 1UI
            End While

            _monitor.AddHashes(hashCount)
            Return New MiningResult(False, 0, hashCount)
        End Function

        ''' <summary>
        ''' Mines a single block synchronously (for testing/regtest).
        ''' </summary>
        Public Function MineSingleBlock(minerAddress As String) As Block
            Dim job As MiningJob = _assembler.CreateJob(minerAddress, _blockchain)
            If job Is Nothing Then Return Nothing

            Dim header As BlockHeader = job.Block.Header
            Dim nonce As UInteger = 0

            While nonce < UInteger.MaxValue
                header.Nonce = nonce
                If header.MeetsTarget() Then
                    _blockchain.AddBlock(job.Block)
                    BlocksMined += 1
                    Return job.Block
                End If
                nonce += 1UI
            End While

            Return Nothing
        End Function

    End Class

    Public Class MiningResult
        Public ReadOnly Property Found As Boolean
        Public ReadOnly Property Nonce As UInteger
        Public ReadOnly Property HashesComputed As Long

        Public Sub New(found As Boolean, nonce As UInteger, hashes As Long)
            Me.Found = found
            Me.Nonce = nonce
            Me.HashesComputed = hashes
        End Sub
    End Class

    Public Class BlockFoundEventArgs
        Inherits EventArgs
        Public ReadOnly Property Block As Block
        Public ReadOnly Property HashesComputed As Long

        Public Sub New(block As Block, hashes As Long)
            Me.Block = block
            Me.HashesComputed = hashes
        End Sub
    End Class

    Public Class MiningStatusEventArgs
        Inherits EventArgs
        Public ReadOnly Property HashRate As Double
        Public ReadOnly Property TotalHashes As Long
        Public ReadOnly Property CurrentHeight As Integer

        Public Sub New(hashRate As Double, totalHashes As Long, height As Integer)
            Me.HashRate = hashRate
            Me.TotalHashes = totalHashes
            Me.CurrentHeight = height
        End Sub
    End Class

End Namespace
