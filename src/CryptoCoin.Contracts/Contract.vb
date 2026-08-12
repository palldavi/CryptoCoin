' ===============================================================================
' CryptoCoin.Contracts - Contract.vb
' Represents a deployed smart contract with address, code, and storage state.
' ===============================================================================

Imports System
Imports CryptoCoin.Cryptography

Namespace CryptoCoin.Contracts

    ''' <summary>
    ''' Represents a deployed smart contract on the CryptoCoin blockchain.
    ''' Contains the contract's address, compiled bytecode, storage state, and metadata.
    ''' </summary>
    Public Class Contract

        ''' <summary>Gets or sets the unique contract address (20 bytes).</summary>
        Public Property Address As Byte()

        ''' <summary>Gets or sets the compiled bytecode of the contract.</summary>
        Public Property Code As Byte()

        ''' <summary>Gets or sets the contract's persistent storage.</summary>
        Public Property Storage As ContractStorage

        ''' <summary>Gets or sets the address of the contract creator.</summary>
        Public Property Creator As Byte()

        ''' <summary>Gets or sets the block height at which the contract was deployed.</summary>
        Public Property DeployedAtBlock As Long

        ''' <summary>Gets or sets the transaction hash of the deployment transaction.</summary>
        Public Property DeploymentTxHash As Byte()

        ''' <summary>Gets or sets the contract's CRC balance in satoshis.</summary>
        Public Property Balance As Long

        ''' <summary>Gets or sets the contract version number.</summary>
        Public Property Version As Integer

        ''' <summary>Gets or sets whether the contract is active (not self-destructed).</summary>
        Public Property IsActive As Boolean

        ''' <summary>Gets or sets the contract's ABI (Application Binary Interface) descriptor.</summary>
        Public Property Abi As String

        ''' <summary>Gets the contract address as a hex string.</summary>
        Public ReadOnly Property AddressHex As String
            Get
                If Address Is Nothing Then Return String.Empty
                Return HashUtil.ToHex(Address)
            End Get
        End Property

        ''' <summary>Gets the size of the contract bytecode in bytes.</summary>
        Public ReadOnly Property CodeSize As Integer
            Get
                If Code Is Nothing Then Return 0
                Return Code.Length
            End Get
        End Property

        ''' <summary>
        ''' Initializes a new empty Contract instance.
        ''' </summary>
        Public Sub New()
            Storage = New ContractStorage()
            IsActive = True
            Version = 1
            Balance = 0
        End Sub

        ''' <summary>
        ''' Initializes a new Contract with the specified address and code.
        ''' </summary>
        ''' <param name="address">The contract address (20 bytes).</param>
        ''' <param name="code">The compiled bytecode.</param>
        Public Sub New(address As Byte(), code As Byte())
            Me.New()
            Me.Address = address
            Me.Code = code
        End Sub

        ''' <summary>
        ''' Generates a contract address from the creator address and nonce.
        ''' Uses SHA-256 hash of (creator + nonce) truncated to 20 bytes.
        ''' </summary>
        ''' <param name="creatorAddress">The address of the contract creator.</param>
        ''' <param name="nonce">The creator's transaction nonce.</param>
        ''' <returns>A 20-byte contract address.</returns>
        Public Shared Function GenerateAddress(creatorAddress As Byte(), nonce As Long) As Byte()
            ' Combine creator address and nonce
            Dim nonceBytes As Byte() = BitConverter.GetBytes(nonce)
            Dim combined(creatorAddress.Length + nonceBytes.Length - 1) As Byte
            Array.Copy(creatorAddress, 0, combined, 0, creatorAddress.Length)
            Array.Copy(nonceBytes, 0, combined, creatorAddress.Length, nonceBytes.Length)

            ' Hash and take first 20 bytes
            Dim hash As Byte() = HashUtil.Sha256(combined)
            Dim contractAddress(19) As Byte
            Array.Copy(hash, 0, contractAddress, 0, 20)

            Return contractAddress
        End Function

        ''' <summary>
        ''' Gets the function selector (first 4 bytes of SHA-256 hash of function signature).
        ''' </summary>
        ''' <param name="functionSignature">The function signature (e.g., "transfer(address,uint256)").</param>
        ''' <returns>A 4-byte function selector.</returns>
        Public Shared Function GetFunctionSelector(functionSignature As String) As Byte()
            Dim sigBytes As Byte() = System.Text.Encoding.UTF8.GetBytes(functionSignature)
            Dim hash As Byte() = HashUtil.Sha256(sigBytes)
            Dim selector(3) As Byte
            Array.Copy(hash, 0, selector, 0, 4)
            Return selector
        End Function

        ''' <summary>
        ''' Serializes the contract to a byte array for storage.
        ''' </summary>
        ''' <returns>The serialized contract bytes.</returns>
        Public Function Serialize() As Byte()
            ' Simple serialization: address length + address + code length + code
            Dim addressLen As Integer = If(Address IsNot Nothing, Address.Length, 0)
            Dim codeLen As Integer = If(Code IsNot Nothing, Code.Length, 0)

            Dim result(4 + addressLen + 4 + codeLen + 8 + 1 - 1) As Byte
            Dim offset As Integer = 0

            ' Address
            Array.Copy(BitConverter.GetBytes(addressLen), 0, result, offset, 4)
            offset += 4
            If addressLen > 0 Then
                Array.Copy(Address, 0, result, offset, addressLen)
                offset += addressLen
            End If

            ' Code
            Array.Copy(BitConverter.GetBytes(codeLen), 0, result, offset, 4)
            offset += 4
            If codeLen > 0 Then
                Array.Copy(Code, 0, result, offset, codeLen)
                offset += codeLen
            End If

            ' Balance
            Array.Copy(BitConverter.GetBytes(Balance), 0, result, offset, 8)
            offset += 8

            ' IsActive
            result(offset) = If(IsActive, CByte(1), CByte(0))

            Return result
        End Function

        ''' <summary>
        ''' Returns a string representation of the contract.
        ''' </summary>
        Public Overrides Function ToString() As String
            Return $"Contract[{AddressHex.Substring(0, 8)}...] Code={CodeSize}B Active={IsActive}"
        End Function

    End Class

End Namespace
