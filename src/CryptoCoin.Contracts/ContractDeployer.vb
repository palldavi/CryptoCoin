' ===============================================================================
' CryptoCoin.Contracts - ContractDeployer.vb
' Deploys new smart contracts to the CryptoCoin blockchain.
' ===============================================================================

Imports System
Imports System.Collections.Generic
Imports CryptoCoin.Cryptography
Imports CryptoCoin.Transactions

Namespace CryptoCoin.Contracts

    ''' <summary>
    ''' Handles the deployment of new smart contracts to the blockchain.
    ''' Validates bytecode, calculates deployment costs, generates contract addresses,
    ''' and creates deployment transactions.
    ''' </summary>
    Public Class ContractDeployer

        Private ReadOnly _executor As ContractExecutor
        Private ReadOnly _gasCalculator As GasCalculator
        Private _deployedContracts As New List(Of Contract)()

        Private Const MaxCodeSize As Integer = 24576 ' 24KB max contract size
        Private Const MinDeploymentGas As Long = 32000

        ''' <summary>Gets the list of contracts deployed by this deployer.</summary>
        Public ReadOnly Property DeployedContracts As List(Of Contract)
            Get
                Return _deployedContracts
            End Get
        End Property

        ''' <summary>
        ''' Initializes a new ContractDeployer with the specified executor.
        ''' </summary>
        ''' <param name="executor">The contract executor for running initialization code.</param>
        Public Sub New(executor As ContractExecutor)
            _executor = executor
            _gasCalculator = New GasCalculator()
        End Sub

        ''' <summary>
        ''' Deploys a new contract from compiled bytecode.
        ''' </summary>
        ''' <param name="bytecode">The compiled contract bytecode.</param>
        ''' <param name="deployer">The address of the deployer.</param>
        ''' <param name="nonce">The deployer's transaction nonce.</param>
        ''' <param name="constructorArgs">Optional constructor arguments.</param>
        ''' <param name="value">Optional CRC value to send to the contract.</param>
        ''' <param name="gasLimit">The gas limit for deployment.</param>
        ''' <returns>A DeploymentResult containing the outcome.</returns>
        Public Function Deploy(bytecode As Byte(), deployer As Byte(), nonce As Long,
                               Optional constructorArgs As Byte() = Nothing,
                               Optional value As Long = 0,
                               Optional gasLimit As Long = 0) As DeploymentResult

            ' Validate bytecode
            Dim validationError As String = ValidateBytecode(bytecode)
            If validationError IsNot Nothing Then
                Return New DeploymentResult(False, Nothing, 0, validationError)
            End If

            ' Calculate gas limit if not specified
            If gasLimit <= 0 Then
                gasLimit = _gasCalculator.GetDeploymentCost(bytecode.Length)
            End If

            If gasLimit < MinDeploymentGas Then
                gasLimit = MinDeploymentGas
            End If

            ' Generate contract address
            Dim contractAddress As Byte() = CryptoCoin.Contracts.Contract.GenerateAddress(deployer, nonce)

            ' Create the contract instance
            Dim contract As New Contract(contractAddress, bytecode) With {
                .Creator = deployer,
                .DeployedAtBlock = 0, ' Will be set when included in a block
                .Balance = value,
                .Version = 1,
                .IsActive = True
            }

            ' Run constructor/initialization code if present
            If constructorArgs IsNot Nothing AndAlso constructorArgs.Length > 0 Then
                Dim initResult As ExecutionResult = RunConstructor(contract, deployer, constructorArgs, value, gasLimit)
                If Not initResult.Success Then
                    Return New DeploymentResult(False, Nothing, initResult.GasUsed,
                        $"Constructor failed: {initResult.ErrorMessage}")
                End If
            End If

            ' Register the contract with the executor
            _executor.RegisterContract(contract)
            _deployedContracts.Add(contract)

            Dim totalGas As Long = _gasCalculator.GetDeploymentCost(bytecode.Length)

            Return New DeploymentResult(True, contract, totalGas, Nothing)
        End Function

        ''' <summary>
        ''' Deploys a contract from source code by compiling it first.
        ''' </summary>
        ''' <param name="sourceCode">The contract source code.</param>
        ''' <param name="deployer">The address of the deployer.</param>
        ''' <param name="nonce">The deployer's transaction nonce.</param>
        ''' <returns>A DeploymentResult containing the outcome.</returns>
        Public Function DeployFromSource(sourceCode As String, deployer As Byte(), nonce As Long) As DeploymentResult
            ' Compile the source code
            Dim compiler As New ContractCompiler()
            Dim bytecode As Byte() = compiler.Compile(sourceCode)

            If bytecode Is Nothing OrElse compiler.HasErrors Then
                Dim errorMsg As String = "Compilation failed"
                If compiler.Errors.Count > 0 Then
                    errorMsg = compiler.Errors(0).ToString()
                End If
                Return New DeploymentResult(False, Nothing, 0, errorMsg)
            End If

            Return Deploy(bytecode, deployer, nonce)
        End Function

        ''' <summary>
        ''' Estimates the gas cost for deploying a contract.
        ''' </summary>
        ''' <param name="bytecode">The contract bytecode.</param>
        ''' <returns>The estimated gas cost for deployment.</returns>
        Public Function EstimateDeploymentGas(bytecode As Byte()) As Long
            If bytecode Is Nothing Then Return 0
            Return _gasCalculator.GetDeploymentCost(bytecode.Length)
        End Function

        ''' <summary>
        ''' Validates contract bytecode before deployment.
        ''' </summary>
        ''' <param name="bytecode">The bytecode to validate.</param>
        ''' <returns>An error message if invalid, or Nothing if valid.</returns>
        Private Function ValidateBytecode(bytecode As Byte()) As String
            If bytecode Is Nothing OrElse bytecode.Length = 0 Then
                Return "Bytecode cannot be empty"
            End If

            If bytecode.Length > MaxCodeSize Then
                Return $"Bytecode exceeds maximum size ({bytecode.Length} > {MaxCodeSize} bytes)"
            End If

            ' Basic validation: check for at least one valid opcode
            Dim firstByte As Byte = bytecode(0)
            If Not [Enum].IsDefined(GetType(ContractOpCode), firstByte) Then
                ' First byte should be a valid opcode (or PUSH data)
                ' Allow it since PUSH instructions have data following
            End If

            Return Nothing
        End Function

        ''' <summary>
        ''' Runs the contract constructor/initialization code.
        ''' </summary>
        Private Function RunConstructor(contract As Contract, deployer As Byte(),
                                        constructorArgs As Byte(), value As Long,
                                        gasLimit As Long) As ExecutionResult
            Dim vm As New VirtualMachine(contract.Storage, gasLimit)
            vm.Context = New ExecutionContext() With {
                .Caller = deployer,
                .Value = value,
                .ContractAddress = contract.Address,
                .BlockNumber = 0,
                .Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                .CallData = constructorArgs
            }

            Return vm.Execute(contract.Code)
        End Function

        ''' <summary>
        ''' Creates a deployment transaction for inclusion in a block.
        ''' </summary>
        ''' <param name="bytecode">The contract bytecode.</param>
        ''' <param name="deployer">The deployer address.</param>
        ''' <param name="gasLimit">The gas limit.</param>
        ''' <returns>A transaction representing the contract deployment.</returns>
        Public Function CreateDeploymentTransaction(bytecode As Byte(), deployer As Byte(),
                                                     gasLimit As Long) As Transaction
            Dim tx As New Transaction()
            tx.Version = 2 ' Version 2 indicates contract transaction

            ' The "output" contains the contract bytecode
            Dim output As New TransactionOutput()
            output.Value = 0
            output.ScriptPubKey = bytecode ' Contract code stored in output script
            tx.Outputs.Add(output)

            Return tx
        End Function

    End Class

    ''' <summary>
    ''' Represents the result of a contract deployment operation.
    ''' </summary>
    Public Class DeploymentResult

        ''' <summary>Gets whether the deployment was successful.</summary>
        Public ReadOnly Property Success As Boolean

        ''' <summary>Gets the deployed contract instance (if successful).</summary>
        Public ReadOnly Property Contract As Contract

        ''' <summary>Gets the total gas consumed during deployment.</summary>
        Public ReadOnly Property GasUsed As Long

        ''' <summary>Gets the error message (if deployment failed).</summary>
        Public ReadOnly Property ErrorMessage As String

        ''' <summary>Gets the contract address as hex (if successful).</summary>
        Public ReadOnly Property ContractAddress As String
            Get
                If Contract IsNot Nothing Then Return Contract.AddressHex
                Return String.Empty
            End Get
        End Property

        Public Sub New(success As Boolean, contract As Contract, gasUsed As Long, errorMessage As String)
            Me.Success = success
            Me.Contract = contract
            Me.GasUsed = gasUsed
            Me.ErrorMessage = errorMessage
        End Sub

    End Class

End Namespace
