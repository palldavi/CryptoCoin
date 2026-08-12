' ===============================================================================
' CryptoCoin.Contracts - GasCalculator.vb
' Calculates gas costs for VM operations and storage access.
' ===============================================================================

Imports System

Namespace CryptoCoin.Contracts

    ''' <summary>
    ''' Calculates gas costs for virtual machine operations.
    ''' Different operations have different costs based on their computational complexity
    ''' and resource usage (CPU, memory, storage, I/O).
    ''' </summary>
    Public Class GasCalculator

        ' Base costs for operation categories
        Private Const GasZero As Long = 0
        Private Const GasBase As Long = 2
        Private Const GasVeryLow As Long = 3
        Private Const GasLow As Long = 5
        Private Const GasMid As Long = 8
        Private Const GasHigh As Long = 10
        Private Const GasExtCode As Long = 700
        Private Const GasSLoad As Long = 200
        Private Const GasSStoreSet As Long = 20000
        Private Const GasSStoreReset As Long = 5000
        Private Const GasSha256 As Long = 60
        Private Const GasRipemd160 As Long = 600
        Private Const GasEcRecover As Long = 3000
        Private Const GasCall As Long = 700
        Private Const GasLog As Long = 375
        Private Const GasLogTopic As Long = 375
        Private Const GasMemory As Long = 3
        Private Const GasCreate As Long = 32000

        ''' <summary>
        ''' Gets the gas cost for a specific opcode.
        ''' </summary>
        ''' <param name="opCode">The opcode to get the cost for.</param>
        ''' <returns>The gas cost in gas units.</returns>
        Public Function GetCost(opCode As ContractOpCode) As Long
            Select Case opCode
                ' Free operations
                Case ContractOpCode.NOP, ContractOpCode.JUMPDEST
                    Return GasZero

                ' Very low cost (stack manipulation)
                Case ContractOpCode.POP, ContractOpCode.DUP, ContractOpCode.SWAP,
                     ContractOpCode.PUSH1, ContractOpCode.PUSH4, ContractOpCode.PUSH8,
                     ContractOpCode.PUSH32, ContractOpCode.PUSH
                    Return GasVeryLow

                ' Low cost (simple arithmetic)
                Case ContractOpCode.ADD, ContractOpCode.[SUB], ContractOpCode.LT,
                     ContractOpCode.GT, ContractOpCode.EQ, ContractOpCode.NEQ,
                     ContractOpCode.LTE, ContractOpCode.GTE, ContractOpCode.ISZERO,
                     ContractOpCode.[AND], ContractOpCode.[OR], ContractOpCode.[XOR],
                     ContractOpCode.[NOT], ContractOpCode.SHL, ContractOpCode.SHR,
                     ContractOpCode.NEG, ContractOpCode.ABS, ContractOpCode.INC,
                     ContractOpCode.DEC, ContractOpCode.ROT, ContractOpCode.DUPN
                    Return GasLow

                ' Medium cost (multiplication, division)
                Case ContractOpCode.MUL, ContractOpCode.DIV, ContractOpCode.[MOD]
                    Return GasMid

                ' High cost (exponentiation)
                Case ContractOpCode.EXP
                    Return GasHigh

                ' Cryptographic operations
                Case ContractOpCode.SHA256, ContractOpCode.HASH256
                    Return GasSha256
                Case ContractOpCode.RIPEMD160, ContractOpCode.HASH160
                    Return GasRipemd160
                Case ContractOpCode.CHECKSIG
                    Return GasEcRecover

                ' Storage operations
                Case ContractOpCode.SLOAD
                    Return GasSLoad
                Case ContractOpCode.SSTORE
                    Return GasSStoreSet ' Actual cost depends on whether setting or clearing

                ' Memory operations
                Case ContractOpCode.MLOAD, ContractOpCode.MSTORE
                    Return GasVeryLow
                Case ContractOpCode.MSIZE
                    Return GasBase

                ' Control flow
                Case ContractOpCode.JUMP, ContractOpCode.JUMPI
                    Return GasMid
                Case ContractOpCode.[RETURN], ContractOpCode.REVERT, ContractOpCode.HALT
                    Return GasZero
                Case ContractOpCode.[CALL], ContractOpCode.DELEGATECALL
                    Return GasCall

                ' Environment
                Case ContractOpCode.CALLER, ContractOpCode.CALLVALUE,
                     ContractOpCode.ADDRESS, ContractOpCode.BLOCKNUMBER,
                     ContractOpCode.TIMESTAMP, ContractOpCode.GAS,
                     ContractOpCode.CALLDATASIZE, ContractOpCode.CALLDATALOAD,
                     ContractOpCode.BALANCE
                    Return GasBase

                ' Log operations
                Case ContractOpCode.LOG0
                    Return GasLog
                Case ContractOpCode.LOG1
                    Return GasLog + GasLogTopic
                Case ContractOpCode.LOG2
                    Return GasLog + (GasLogTopic * 2)
                Case ContractOpCode.LOG3
                    Return GasLog + (GasLogTopic * 3)

                Case Else
                    Return GasBase
            End Select
        End Function

        ''' <summary>
        ''' Calculates the additional gas cost for storage operations based on value size.
        ''' </summary>
        ''' <param name="value">The value being stored.</param>
        ''' <returns>Additional gas cost for the storage operation.</returns>
        Public Function GetStorageCost(value As Byte()) As Long
            If value Is Nothing OrElse value.Length = 0 Then
                ' Clearing storage (refund scenario)
                Return GasSStoreReset
            End If

            ' Setting new storage value
            Return GasSStoreSet + (CLng(value.Length) * GasMemory)
        End Function

        ''' <summary>
        ''' Calculates the gas cost for memory expansion.
        ''' </summary>
        ''' <param name="currentSize">The current memory size in words.</param>
        ''' <param name="newSize">The new required memory size in words.</param>
        ''' <returns>The gas cost for memory expansion.</returns>
        Public Function GetMemoryExpansionCost(currentSize As Integer, newSize As Integer) As Long
            If newSize <= currentSize Then Return 0

            Dim oldCost As Long = MemoryCost(currentSize)
            Dim newCost As Long = MemoryCost(newSize)
            Return newCost - oldCost
        End Function

        ''' <summary>
        ''' Calculates the gas cost for deploying a new contract.
        ''' </summary>
        ''' <param name="codeSize">The size of the contract code in bytes.</param>
        ''' <returns>The total deployment gas cost.</returns>
        Public Function GetDeploymentCost(codeSize As Integer) As Long
            Return GasCreate + (CLng(codeSize) * 200L)
        End Function

        ''' <summary>
        ''' Calculates the memory cost formula: cost = words * 3 + words^2 / 512.
        ''' </summary>
        Private Function MemoryCost(words As Integer) As Long
            Return (CLng(words) * GasMemory) + (CLng(words) * CLng(words) \ 512L)
        End Function

    End Class

End Namespace
