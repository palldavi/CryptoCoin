' ===============================================================================
' CryptoCoin.Contracts - ContractOpCodes.vb
' VM opcode definitions for the CryptoCoin smart contract virtual machine.
' ===============================================================================

Imports System

Namespace CryptoCoin.Contracts

    ''' <summary>
    ''' Defines the opcodes for the CryptoCoin smart contract virtual machine.
    ''' Each opcode represents a single instruction that the VM can execute.
    ''' </summary>
    Public Enum ContractOpCode As Byte

        ' --- Stack Operations (0x00 - 0x0F) ---

        ''' <summary>No operation. Does nothing.</summary>
        NOP = &H0

        ''' <summary>Push the next N bytes onto the stack.</summary>
        PUSH = &H1

        ''' <summary>Push a single byte value onto the stack.</summary>
        PUSH1 = &H2

        ''' <summary>Push a 4-byte integer onto the stack.</summary>
        PUSH4 = &H3

        ''' <summary>Push an 8-byte long onto the stack.</summary>
        PUSH8 = &H4

        ''' <summary>Push a 32-byte value onto the stack.</summary>
        PUSH32 = &H5

        ''' <summary>Remove the top item from the stack.</summary>
        POP = &H6

        ''' <summary>Duplicate the top stack item.</summary>
        DUP = &H7

        ''' <summary>Swap the top two stack items.</summary>
        SWAP = &H8

        ''' <summary>Rotate the top three stack items.</summary>
        ROT = &H9

        ''' <summary>Duplicate the Nth item from the top of the stack.</summary>
        DUPN = &HA

        ' --- Arithmetic Operations (0x10 - 0x1F) ---

        ''' <summary>Add top two stack items.</summary>
        ADD = &H10

        ''' <summary>Subtract top item from second item.</summary>
        [SUB] = &H11

        ''' <summary>Multiply top two stack items.</summary>
        MUL = &H12

        ''' <summary>Divide second item by top item.</summary>
        DIV = &H13

        ''' <summary>Modulo: second item mod top item.</summary>
        [MOD] = &H14

        ''' <summary>Negate the top stack item.</summary>
        NEG = &H15

        ''' <summary>Absolute value of top stack item.</summary>
        ABS = &H16

        ''' <summary>Increment top stack item by 1.</summary>
        INC = &H17

        ''' <summary>Decrement top stack item by 1.</summary>
        DEC = &H18

        ''' <summary>Exponentiation: second item raised to top item power.</summary>
        EXP = &H19

        ' --- Comparison Operations (0x20 - 0x2F) ---

        ''' <summary>Check if top two items are equal. Push 1 if true, 0 if false.</summary>
        EQ = &H20

        ''' <summary>Check if top two items are not equal.</summary>
        NEQ = &H21

        ''' <summary>Check if second item is less than top item.</summary>
        LT = &H22

        ''' <summary>Check if second item is greater than top item.</summary>
        GT = &H23

        ''' <summary>Check if second item is less than or equal to top item.</summary>
        LTE = &H24

        ''' <summary>Check if second item is greater than or equal to top item.</summary>
        GTE = &H25

        ''' <summary>Check if top item is zero.</summary>
        ISZERO = &H26

        ' --- Logic Operations (0x30 - 0x3F) ---

        ''' <summary>Bitwise AND of top two items.</summary>
        [AND] = &H30

        ''' <summary>Bitwise OR of top two items.</summary>
        [OR] = &H31

        ''' <summary>Bitwise XOR of top two items.</summary>
        [XOR] = &H32

        ''' <summary>Bitwise NOT of top item.</summary>
        [NOT] = &H33

        ''' <summary>Shift left: second item shifted by top item bits.</summary>
        SHL = &H34

        ''' <summary>Shift right: second item shifted by top item bits.</summary>
        SHR = &H35

        ' --- Cryptographic Operations (0x40 - 0x4F) ---

        ''' <summary>SHA-256 hash of top stack item.</summary>
        SHA256 = &H40

        ''' <summary>Double SHA-256 hash (SHA256d) of top stack item.</summary>
        HASH256 = &H41

        ''' <summary>RIPEMD-160 hash of top stack item.</summary>
        RIPEMD160 = &H42

        ''' <summary>SHA-256 followed by RIPEMD-160 (HASH160).</summary>
        HASH160 = &H43

        ''' <summary>Verify ECDSA signature. Pushes 1 if valid, 0 if not.</summary>
        CHECKSIG = &H44

        ' --- Storage Operations (0x50 - 0x5F) ---

        ''' <summary>Load value from contract storage by key.</summary>
        SLOAD = &H50

        ''' <summary>Store value to contract storage by key.</summary>
        SSTORE = &H51

        ''' <summary>Load value from memory by offset.</summary>
        MLOAD = &H52

        ''' <summary>Store value to memory at offset.</summary>
        MSTORE = &H53

        ''' <summary>Get the size of memory.</summary>
        MSIZE = &H54

        ' --- Control Flow (0x60 - 0x6F) ---

        ''' <summary>Unconditional jump to address.</summary>
        JUMP = &H60

        ''' <summary>Conditional jump: jump if top item is non-zero.</summary>
        JUMPI = &H61

        ''' <summary>Mark a valid jump destination.</summary>
        JUMPDEST = &H62

        ''' <summary>Return from execution with data.</summary>
        [RETURN] = &H63

        ''' <summary>Revert execution and undo state changes.</summary>
        REVERT = &H64

        ''' <summary>Halt execution (invalid/stop).</summary>
        HALT = &H65

        ''' <summary>Call another contract.</summary>
        [CALL] = &H66

        ''' <summary>Delegate call to another contract (preserves caller context).</summary>
        DELEGATECALL = &H67

        ' --- Environment Operations (0x70 - 0x7F) ---

        ''' <summary>Get the caller's address.</summary>
        CALLER = &H70

        ''' <summary>Get the value sent with the call.</summary>
        CALLVALUE = &H71

        ''' <summary>Get the current contract's address.</summary>
        ADDRESS = &H72

        ''' <summary>Get the current block number.</summary>
        BLOCKNUMBER = &H73

        ''' <summary>Get the current block timestamp.</summary>
        TIMESTAMP = &H74

        ''' <summary>Get the balance of an address.</summary>
        BALANCE = &H75

        ''' <summary>Get the size of the call data.</summary>
        CALLDATASIZE = &H76

        ''' <summary>Load call data at offset.</summary>
        CALLDATALOAD = &H77

        ''' <summary>Get the remaining gas.</summary>
        GAS = &H78

        ' --- Event/Log Operations (0x80 - 0x8F) ---

        ''' <summary>Emit a log event with no topics.</summary>
        LOG0 = &H80

        ''' <summary>Emit a log event with one topic.</summary>
        LOG1 = &H81

        ''' <summary>Emit a log event with two topics.</summary>
        LOG2 = &H82

        ''' <summary>Emit a log event with three topics.</summary>
        LOG3 = &H83

    End Enum

    ''' <summary>
    ''' Provides utility methods for working with contract opcodes.
    ''' </summary>
    Public NotInheritable Class OpCodeInfo

        Private Sub New()
        End Sub

        ''' <summary>
        ''' Gets the human-readable name of an opcode.
        ''' </summary>
        ''' <param name="opCode">The opcode to get the name for.</param>
        ''' <returns>The opcode name as a string.</returns>
        Public Shared Function GetName(opCode As ContractOpCode) As String
            Return [Enum].GetName(GetType(ContractOpCode), opCode)
        End Function

        ''' <summary>
        ''' Gets the number of stack items consumed by an opcode.
        ''' </summary>
        ''' <param name="opCode">The opcode to check.</param>
        ''' <returns>The number of items popped from the stack.</returns>
        Public Shared Function GetStackInputs(opCode As ContractOpCode) As Integer
            Select Case opCode
                Case ContractOpCode.NOP, ContractOpCode.PUSH, ContractOpCode.PUSH1,
                     ContractOpCode.PUSH4, ContractOpCode.PUSH8, ContractOpCode.PUSH32,
                     ContractOpCode.CALLER, ContractOpCode.CALLVALUE, ContractOpCode.ADDRESS,
                     ContractOpCode.BLOCKNUMBER, ContractOpCode.TIMESTAMP, ContractOpCode.GAS,
                     ContractOpCode.CALLDATASIZE, ContractOpCode.MSIZE
                    Return 0
                Case ContractOpCode.POP, ContractOpCode.DUP, ContractOpCode.NEG,
                     ContractOpCode.ABS, ContractOpCode.INC, ContractOpCode.DEC,
                     ContractOpCode.[NOT], ContractOpCode.ISZERO, ContractOpCode.SHA256,
                     ContractOpCode.HASH256, ContractOpCode.RIPEMD160, ContractOpCode.HASH160,
                     ContractOpCode.JUMP, ContractOpCode.SLOAD, ContractOpCode.MLOAD,
                     ContractOpCode.BALANCE, ContractOpCode.CALLDATALOAD,
                     ContractOpCode.[RETURN], ContractOpCode.REVERT
                    Return 1
                Case ContractOpCode.ADD, ContractOpCode.[SUB], ContractOpCode.MUL,
                     ContractOpCode.DIV, ContractOpCode.[MOD], ContractOpCode.EXP,
                     ContractOpCode.EQ, ContractOpCode.NEQ, ContractOpCode.LT,
                     ContractOpCode.GT, ContractOpCode.LTE, ContractOpCode.GTE,
                     ContractOpCode.[AND], ContractOpCode.[OR], ContractOpCode.[XOR],
                     ContractOpCode.SHL, ContractOpCode.SHR, ContractOpCode.SWAP,
                     ContractOpCode.JUMPI, ContractOpCode.SSTORE, ContractOpCode.MSTORE
                    Return 2
                Case ContractOpCode.ROT, ContractOpCode.CHECKSIG
                    Return 3
                Case Else
                    Return 0
            End Select
        End Function

        ''' <summary>
        ''' Gets the number of stack items produced by an opcode.
        ''' </summary>
        ''' <param name="opCode">The opcode to check.</param>
        ''' <returns>The number of items pushed onto the stack.</returns>
        Public Shared Function GetStackOutputs(opCode As ContractOpCode) As Integer
            Select Case opCode
                Case ContractOpCode.POP, ContractOpCode.SSTORE, ContractOpCode.MSTORE,
                     ContractOpCode.JUMP, ContractOpCode.JUMPI, ContractOpCode.HALT,
                     ContractOpCode.[RETURN], ContractOpCode.REVERT, ContractOpCode.NOP,
                     ContractOpCode.LOG0, ContractOpCode.LOG1, ContractOpCode.LOG2, ContractOpCode.LOG3
                    Return 0
                Case ContractOpCode.DUP
                    Return 2
                Case Else
                    Return 1
            End Select
        End Function

    End Class

End Namespace
