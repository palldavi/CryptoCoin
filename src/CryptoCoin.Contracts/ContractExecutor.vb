' ===============================================================================
' CryptoCoin.Contracts - ContractExecutor.vb
' Executes contract calls, manages gas, and handles execution errors.
' ===============================================================================

Imports System
Imports System.Collections.Generic
Imports CryptoCoin.Cryptography

Namespace CryptoCoin.Contracts

    ''' <summary>
    ''' Executes smart contract calls by setting up the VM environment,
    ''' managing gas allocation, and handling execution results.
    ''' </summary>
    Public Class ContractExecutor

        Private ReadOnly _contracts As Dictionary(Of String, Contract)
        Private ReadOnly _gasCalculator As GasCalculator
        Private Const DefaultGasLimit As Long = 1000000
        Private Const MaxCallDepth As Integer = 64

        ''' <summary>Event raised when a contract execution completes.</summary>
        Public Event ExecutionCompleted As EventHandler(Of ContractExecutionEventArgs)

        ''' <summary>Event raised when a contract emits a log event.</summary>
        Public Event LogEmitted As EventHandler(Of ContractLogEventArgs)

        ''' <summary>
        ''' Initializes a new ContractExecutor.
        ''' </summary>
        Public Sub New()
            _contracts = New Dictionary(Of String, Contract)(StringComparer.OrdinalIgnoreCase)
            _gasCalculator = New GasCalculator()
        End Sub

        ''' <summary>
        ''' Registers a deployed contract with the executor.
        ''' </summary>
        ''' <param name="contract">The contract to register.</param>
        Public Sub RegisterContract(contract As Contract)
            If contract Is Nothing Then Throw New ArgumentNullException(NameOf(contract))
            Dim addressHex As String = contract.AddressHex
            _contracts(addressHex) = contract
        End Sub

        ''' <summary>
        ''' Executes a contract call with the specified parameters.
        ''' </summary>
        ''' <param name="contractAddress">The address of the contract to call.</param>
        ''' <param name="caller">The address of the caller.</param>
        ''' <param name="callData">The call data (function selector + encoded arguments).</param>
        ''' <param name="value">The CRC value sent with the call (in satoshis).</param>
        ''' <param name="gasLimit">The maximum gas allowed for this call.</param>
        ''' <returns>The execution result.</returns>
        Public Function ExecuteCall(contractAddress As Byte(), caller As Byte(),
                                    callData As Byte(), value As Long,
                                    Optional gasLimit As Long = DefaultGasLimit) As ExecutionResult
            Return ExecuteCallInternal(contractAddress, caller, callData, value, gasLimit, 0)
        End Function

        ''' <summary>
        ''' Internal execution method that tracks call depth for re-entrancy protection.
        ''' </summary>
        Private Function ExecuteCallInternal(contractAddress As Byte(), caller As Byte(),
                                             callData As Byte(), value As Long,
                                             gasLimit As Long, depth As Integer) As ExecutionResult
            ' Check call depth
            If depth >= MaxCallDepth Then
                Return New ExecutionResult(False, Nothing, 0, "Maximum call depth exceeded")
            End If

            ' Find the contract
            Dim addressHex As String = HashUtil.ToHex(contractAddress)
            Dim contract As Contract = Nothing
            If Not _contracts.TryGetValue(addressHex, contract) Then
                Return New ExecutionResult(False, Nothing, 0, $"Contract not found: {addressHex}")
            End If

            ' Check contract is active
            If Not contract.IsActive Then
                Return New ExecutionResult(False, Nothing, 0, "Contract is not active")
            End If

            ' Validate gas limit
            If gasLimit <= 0 Then gasLimit = DefaultGasLimit

            ' Create execution context
            Dim context As New ExecutionContext() With {
                .Caller = caller,
                .Value = value,
                .ContractAddress = contractAddress,
                .BlockNumber = GetCurrentBlockNumber(),
                .Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                .CallData = callData
            }

            ' Create snapshot for potential rollback
            contract.Storage.CreateSnapshot()

            ' Transfer value to contract
            If value > 0 Then
                contract.Balance += value
            End If

            Try
                ' Create and configure VM
                Dim vm As New VirtualMachine(contract.Storage, gasLimit)
                vm.Context = context

                ' Execute the contract code
                Dim result As ExecutionResult = vm.Execute(contract.Code)

                If result.Success Then
                    ' Commit storage changes
                    contract.Storage.Commit()
                Else
                    ' Rollback storage changes
                    contract.Storage.Rollback()
                    ' Refund value on failure
                    If value > 0 Then
                        contract.Balance -= value
                    End If
                End If

                ' Raise completion event
                RaiseEvent ExecutionCompleted(Me, New ContractExecutionEventArgs() With {
                    .ContractAddress = addressHex,
                    .Success = result.Success,
                    .GasUsed = result.GasUsed,
                    .ErrorMessage = result.ErrorMessage
                })

                Return result

            Catch ex As Exception
                ' Rollback on any unhandled exception
                contract.Storage.Rollback()
                If value > 0 Then
                    contract.Balance -= value
                End If

                Return New ExecutionResult(False, Nothing, gasLimit, $"Execution failed: {ex.Message}")
            End Try
        End Function

        ''' <summary>
        ''' Executes a read-only (view) call that does not modify state.
        ''' </summary>
        ''' <param name="contractAddress">The contract address to call.</param>
        ''' <param name="callData">The call data.</param>
        ''' <returns>The execution result (state changes are discarded).</returns>
        Public Function ExecuteViewCall(contractAddress As Byte(), callData As Byte()) As ExecutionResult
            Dim addressHex As String = HashUtil.ToHex(contractAddress)
            Dim contract As Contract = Nothing
            If Not _contracts.TryGetValue(addressHex, contract) Then
                Return New ExecutionResult(False, Nothing, 0, "Contract not found")
            End If

            ' Create snapshot (will always rollback for view calls)
            contract.Storage.CreateSnapshot()

            Try
                Dim context As New ExecutionContext() With {
                    .Caller = New Byte(19) {},
                    .Value = 0,
                    .ContractAddress = contractAddress,
                    .BlockNumber = GetCurrentBlockNumber(),
                    .Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    .CallData = callData
                }

                Dim vm As New VirtualMachine(contract.Storage, DefaultGasLimit)
                vm.Context = context

                Dim result As ExecutionResult = vm.Execute(contract.Code)

                ' Always rollback for view calls
                contract.Storage.Rollback()

                Return result
            Catch ex As Exception
                contract.Storage.Rollback()
                Return New ExecutionResult(False, Nothing, 0, ex.Message)
            End Try
        End Function

        ''' <summary>
        ''' Estimates the gas required for a contract call.
        ''' </summary>
        ''' <param name="contractAddress">The contract address.</param>
        ''' <param name="callData">The call data.</param>
        ''' <returns>The estimated gas cost.</returns>
        Public Function EstimateGas(contractAddress As Byte(), callData As Byte()) As Long
            Dim result As ExecutionResult = ExecuteViewCall(contractAddress, callData)
            If result.Success Then
                ' Add 20% buffer to the actual gas used
                Return CLng(result.GasUsed * 1.2)
            End If
            Return DefaultGasLimit
        End Function

        ''' <summary>
        ''' Gets a registered contract by address.
        ''' </summary>
        ''' <param name="addressHex">The contract address as hex string.</param>
        ''' <returns>The contract, or Nothing if not found.</returns>
        Public Function GetContract(addressHex As String) As Contract
            Dim contract As Contract = Nothing
            _contracts.TryGetValue(addressHex, contract)
            Return contract
        End Function

        ''' <summary>
        ''' Gets the current block number (placeholder for integration).
        ''' </summary>
        Private Function GetCurrentBlockNumber() As Long
            Return 0 ' Would be provided by blockchain context in production
        End Function

    End Class

    ''' <summary>
    ''' Event arguments for contract execution completion.
    ''' </summary>
    Public Class ContractExecutionEventArgs
        Inherits EventArgs

        Public Property ContractAddress As String
        Public Property Success As Boolean
        Public Property GasUsed As Long
        Public Property ErrorMessage As String
    End Class

    ''' <summary>
    ''' Event arguments for contract log emissions.
    ''' </summary>
    Public Class ContractLogEventArgs
        Inherits EventArgs

        Public Property ContractAddress As String
        Public Property Topics As List(Of Byte())
        Public Property Data As Byte()
    End Class

End Namespace
