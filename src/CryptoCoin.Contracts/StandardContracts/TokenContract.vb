' ===============================================================================
' CryptoCoin.Contracts - StandardContracts\TokenContract.vb
' ERC20-like fungible token contract implementation.
' ===============================================================================

Imports System
Imports System.Collections.Generic
Imports System.Numerics
Imports System.Text
Imports CryptoCoin.Cryptography

Namespace CryptoCoin.Contracts.StandardContracts

    ''' <summary>
    ''' Implements an ERC20-like fungible token contract for the CryptoCoin platform.
    ''' Supports transfer, approve, transferFrom, and balance queries.
    ''' </summary>
    ''' <remarks>
    ''' Storage layout:
    '''   "name"              -> Token name (string)
    '''   "symbol"            -> Token symbol (string)
    '''   "decimals"          -> Decimal places (byte)
    '''   "totalSupply"       -> Total supply (BigInteger)
    '''   "balance:{address}" -> Balance for address (BigInteger)
    '''   "allowance:{owner}:{spender}" -> Allowance (BigInteger)
    '''   "owner"             -> Contract owner address
    ''' </remarks>
    Public Class TokenContract

        Private ReadOnly _storage As ContractStorage
        Private ReadOnly _contractAddress As Byte()

        ' Function selectors (first 4 bytes of SHA-256 of function signature)
        Private Shared ReadOnly FuncTransfer As Byte() = Contract.GetFunctionSelector("transfer(address,uint256)")
        Private Shared ReadOnly FuncApprove As Byte() = Contract.GetFunctionSelector("approve(address,uint256)")
        Private Shared ReadOnly FuncTransferFrom As Byte() = Contract.GetFunctionSelector("transferFrom(address,address,uint256)")
        Private Shared ReadOnly FuncBalanceOf As Byte() = Contract.GetFunctionSelector("balanceOf(address)")
        Private Shared ReadOnly FuncAllowance As Byte() = Contract.GetFunctionSelector("allowance(address,address)")
        Private Shared ReadOnly FuncTotalSupply As Byte() = Contract.GetFunctionSelector("totalSupply()")
        Private Shared ReadOnly FuncMint As Byte() = Contract.GetFunctionSelector("mint(address,uint256)")
        Private Shared ReadOnly FuncBurn As Byte() = Contract.GetFunctionSelector("burn(uint256)")

        ''' <summary>Gets the token name.</summary>
        Public ReadOnly Property Name As String
            Get
                Dim data As Byte() = _storage.GetByString("name")
                If data Is Nothing Then Return String.Empty
                Return Encoding.UTF8.GetString(data)
            End Get
        End Property

        ''' <summary>Gets the token symbol.</summary>
        Public ReadOnly Property Symbol As String
            Get
                Dim data As Byte() = _storage.GetByString("symbol")
                If data Is Nothing Then Return String.Empty
                Return Encoding.UTF8.GetString(data)
            End Get
        End Property

        ''' <summary>Gets the number of decimal places.</summary>
        Public ReadOnly Property Decimals As Byte
            Get
                Dim data As Byte() = _storage.GetByString("decimals")
                If data Is Nothing OrElse data.Length = 0 Then Return 18
                Return data(0)
            End Get
        End Property

        ''' <summary>Gets the total token supply.</summary>
        Public ReadOnly Property TotalSupply As BigInteger
            Get
                Return GetStorageInteger("totalSupply")
            End Get
        End Property

        ''' <summary>
        ''' Initializes a new TokenContract with existing storage.
        ''' </summary>
        ''' <param name="storage">The contract storage instance.</param>
        ''' <param name="contractAddress">The contract's address.</param>
        Public Sub New(storage As ContractStorage, contractAddress As Byte())
            _storage = storage
            _contractAddress = contractAddress
        End Sub

        ''' <summary>
        ''' Initializes the token contract with name, symbol, decimals, and initial supply.
        ''' </summary>
        ''' <param name="name">The token name.</param>
        ''' <param name="symbol">The token symbol.</param>
        ''' <param name="decimals">The number of decimal places.</param>
        ''' <param name="initialSupply">The initial token supply.</param>
        ''' <param name="owner">The address of the token owner/creator.</param>
        Public Sub Initialize(name As String, symbol As String, decimals As Byte,
                              initialSupply As BigInteger, owner As Byte())
            _storage.PutByString("name", Encoding.UTF8.GetBytes(name))
            _storage.PutByString("symbol", Encoding.UTF8.GetBytes(symbol))
            _storage.PutByString("decimals", New Byte() {decimals})
            SetStorageInteger("totalSupply", initialSupply)
            _storage.PutByString("owner", owner)

            ' Assign initial supply to owner
            Dim ownerKey As String = $"balance:{BytesToHex(owner)}"
            SetStorageInteger(ownerKey, initialSupply)
        End Sub

        ''' <summary>
        ''' Gets the token balance of the specified address.
        ''' </summary>
        ''' <param name="address">The address to query.</param>
        ''' <returns>The token balance.</returns>
        Public Function BalanceOf(address As Byte()) As BigInteger
            Dim key As String = $"balance:{BytesToHex(address)}"
            Return GetStorageInteger(key)
        End Function

        ''' <summary>
        ''' Transfers tokens from the caller to the specified recipient.
        ''' </summary>
        ''' <param name="caller">The sender's address.</param>
        ''' <param name="recipient">The recipient's address.</param>
        ''' <param name="amount">The amount to transfer.</param>
        ''' <returns>True if the transfer succeeded; otherwise, False.</returns>
        Public Function Transfer(caller As Byte(), recipient As Byte(), amount As BigInteger) As Boolean
            If amount <= BigInteger.Zero Then Return False
            If caller Is Nothing OrElse recipient Is Nothing Then Return False

            Dim senderKey As String = $"balance:{BytesToHex(caller)}"
            Dim recipientKey As String = $"balance:{BytesToHex(recipient)}"

            Dim senderBalance As BigInteger = GetStorageInteger(senderKey)
            If senderBalance < amount Then Return False

            ' Update balances
            SetStorageInteger(senderKey, senderBalance - amount)
            Dim recipientBalance As BigInteger = GetStorageInteger(recipientKey)
            SetStorageInteger(recipientKey, recipientBalance + amount)

            Return True
        End Function

        ''' <summary>
        ''' Approves a spender to transfer tokens on behalf of the caller.
        ''' </summary>
        ''' <param name="owner">The token owner's address.</param>
        ''' <param name="spender">The spender's address.</param>
        ''' <param name="amount">The approved amount.</param>
        ''' <returns>True if the approval succeeded.</returns>
        Public Function Approve(owner As Byte(), spender As Byte(), amount As BigInteger) As Boolean
            If owner Is Nothing OrElse spender Is Nothing Then Return False
            If amount < BigInteger.Zero Then Return False

            Dim key As String = $"allowance:{BytesToHex(owner)}:{BytesToHex(spender)}"
            SetStorageInteger(key, amount)

            Return True
        End Function

        ''' <summary>
        ''' Transfers tokens from one address to another using an allowance.
        ''' </summary>
        ''' <param name="spender">The address executing the transfer.</param>
        ''' <param name="from">The address to transfer from.</param>
        ''' <param name="to">The address to transfer to.</param>
        ''' <param name="amount">The amount to transfer.</param>
        ''' <returns>True if the transfer succeeded; otherwise, False.</returns>
        Public Function TransferFrom(spender As Byte(), from As Byte(), [to] As Byte(), amount As BigInteger) As Boolean
            If amount <= BigInteger.Zero Then Return False

            ' Check allowance
            Dim allowanceKey As String = $"allowance:{BytesToHex(from)}:{BytesToHex(spender)}"
            Dim currentAllowance As BigInteger = GetStorageInteger(allowanceKey)
            If currentAllowance < amount Then Return False

            ' Check balance
            Dim fromKey As String = $"balance:{BytesToHex(from)}"
            Dim fromBalance As BigInteger = GetStorageInteger(fromKey)
            If fromBalance < amount Then Return False

            ' Update balances and allowance
            SetStorageInteger(fromKey, fromBalance - amount)
            Dim toKey As String = $"balance:{BytesToHex([to])}"
            Dim toBalance As BigInteger = GetStorageInteger(toKey)
            SetStorageInteger(toKey, toBalance + amount)
            SetStorageInteger(allowanceKey, currentAllowance - amount)

            Return True
        End Function

        ''' <summary>
        ''' Gets the allowance for a spender on behalf of an owner.
        ''' </summary>
        Public Function GetAllowance(owner As Byte(), spender As Byte()) As BigInteger
            Dim key As String = $"allowance:{BytesToHex(owner)}:{BytesToHex(spender)}"
            Return GetStorageInteger(key)
        End Function

        ''' <summary>
        ''' Mints new tokens to the specified address (owner only).
        ''' </summary>
        ''' <param name="caller">The caller's address (must be owner).</param>
        ''' <param name="recipient">The address to mint tokens to.</param>
        ''' <param name="amount">The amount to mint.</param>
        ''' <returns>True if minting succeeded; otherwise, False.</returns>
        Public Function Mint(caller As Byte(), recipient As Byte(), amount As BigInteger) As Boolean
            If Not IsOwner(caller) Then Return False
            If amount <= BigInteger.Zero Then Return False

            ' Increase total supply
            Dim supply As BigInteger = GetStorageInteger("totalSupply")
            SetStorageInteger("totalSupply", supply + amount)

            ' Add to recipient balance
            Dim key As String = $"balance:{BytesToHex(recipient)}"
            Dim balance As BigInteger = GetStorageInteger(key)
            SetStorageInteger(key, balance + amount)

            Return True
        End Function

        ''' <summary>
        ''' Burns tokens from the caller's balance.
        ''' </summary>
        ''' <param name="caller">The caller's address.</param>
        ''' <param name="amount">The amount to burn.</param>
        ''' <returns>True if burning succeeded; otherwise, False.</returns>
        Public Function Burn(caller As Byte(), amount As BigInteger) As Boolean
            If amount <= BigInteger.Zero Then Return False

            Dim key As String = $"balance:{BytesToHex(caller)}"
            Dim balance As BigInteger = GetStorageInteger(key)
            If balance < amount Then Return False

            ' Decrease balance and total supply
            SetStorageInteger(key, balance - amount)
            Dim supply As BigInteger = GetStorageInteger("totalSupply")
            SetStorageInteger("totalSupply", supply - amount)

            Return True
        End Function

        ''' <summary>
        ''' Checks if the specified address is the contract owner.
        ''' </summary>
        Private Function IsOwner(address As Byte()) As Boolean
            Dim ownerData As Byte() = _storage.GetByString("owner")
            If ownerData Is Nothing OrElse address Is Nothing Then Return False
            If ownerData.Length <> address.Length Then Return False
            For i As Integer = 0 To ownerData.Length - 1
                If ownerData(i) <> address(i) Then Return False
            Next
            Return True
        End Function

        Private Function GetStorageInteger(key As String) As BigInteger
            Dim data As Byte() = _storage.GetByString(key)
            If data Is Nothing OrElse data.Length = 0 Then Return BigInteger.Zero
            Return New BigInteger(data)
        End Function

        Private Sub SetStorageInteger(key As String, value As BigInteger)
            _storage.PutByString(key, value.ToByteArray())
        End Sub

        Private Function BytesToHex(data As Byte()) As String
            If data Is Nothing Then Return String.Empty
            Return BitConverter.ToString(data).Replace("-", "").ToLowerInvariant()
        End Function

    End Class

End Namespace
