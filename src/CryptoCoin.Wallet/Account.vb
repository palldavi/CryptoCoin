Imports System.Collections.Generic
Imports CryptoCoin.Cryptography

Namespace CryptoCoin.Wallet

    ''' <summary>
    ''' Represents a wallet account with HD key derivation path.
    ''' Each account manages external (receiving) and internal (change) address chains
    ''' following the BIP44 standard: m/44'/999'/account'/chain/index.
    ''' </summary>
    Public Class Account

        Private ReadOnly _externalAddresses As List(Of DerivedAddress)
        Private ReadOnly _internalAddresses As List(Of DerivedAddress)
        Private ReadOnly _syncLock As New Object()

        ''' <summary>
        ''' The account index in the HD derivation path.
        ''' </summary>
        Public ReadOnly Property AccountIndex As Integer

        ''' <summary>
        ''' A user-friendly name for this account.
        ''' </summary>
        Public Property Name As String

        ''' <summary>
        ''' The extended private key for this account level (m/44'/999'/account').
        ''' </summary>
        Public ReadOnly Property AccountKey As HdKeyDerivation.ExtendedKey

        ''' <summary>
        ''' The gap limit for address discovery.
        ''' </summary>
        Public Property GapLimit As Integer = 20

        ''' <summary>
        ''' The next unused external (receiving) address index.
        ''' </summary>
        Public Property NextExternalIndex As Integer = 0

        ''' <summary>
        ''' The next unused internal (change) address index.
        ''' </summary>
        Public Property NextInternalIndex As Integer = 0

        ''' <summary>
        ''' The wallet configuration for this account.
        ''' </summary>
        Public ReadOnly Property Config As WalletConfig

        ''' <summary>
        ''' Creates a new account from an account-level extended key.
        ''' </summary>
        ''' <param name="accountKey">The extended key at the account level (m/44'/999'/account').</param>
        ''' <param name="accountIndex">The account index.</param>
        ''' <param name="config">The wallet configuration.</param>
        Public Sub New(accountKey As HdKeyDerivation.ExtendedKey, accountIndex As Integer, config As WalletConfig)
            If accountKey Is Nothing Then Throw New ArgumentNullException(NameOf(accountKey))
            If config Is Nothing Then Throw New ArgumentNullException(NameOf(config))
            If accountIndex < 0 Then Throw New ArgumentOutOfRangeException(NameOf(accountIndex))

            Me.AccountKey = accountKey
            Me.AccountIndex = accountIndex
            Me.Config = config
            Me.GapLimit = config.GapLimit
            Me.Name = $"Account {accountIndex}"

            _externalAddresses = New List(Of DerivedAddress)()
            _internalAddresses = New List(Of DerivedAddress)()
        End Sub

        ''' <summary>
        ''' Gets the next unused receiving (external) address.
        ''' Generates a new address if needed.
        ''' </summary>
        ''' <returns>A fresh receiving address string.</returns>
        Public Function GetReceivingAddress() As String
            SyncLock _syncLock
                ' Ensure we have an address at the current index
                EnsureExternalAddress(NextExternalIndex)
                Dim addr As DerivedAddress = _externalAddresses(NextExternalIndex)
                Return addr.Address
            End SyncLock
        End Function

        ''' <summary>
        ''' Gets the next unused change (internal) address.
        ''' </summary>
        ''' <returns>A fresh change address string.</returns>
        Public Function GetChangeAddress() As String
            SyncLock _syncLock
                EnsureInternalAddress(NextInternalIndex)
                Dim addr As DerivedAddress = _internalAddresses(NextInternalIndex)
                Return addr.Address
            End SyncLock
        End Function

        ''' <summary>
        ''' Marks the current receiving address as used and advances to the next index.
        ''' </summary>
        Public Sub AdvanceReceivingAddress()
            SyncLock _syncLock
                NextExternalIndex += 1
                EnsureExternalAddress(NextExternalIndex)
            End SyncLock
        End Sub

        ''' <summary>
        ''' Marks the current change address as used and advances to the next index.
        ''' </summary>
        Public Sub AdvanceChangeAddress()
            SyncLock _syncLock
                NextInternalIndex += 1
                EnsureInternalAddress(NextInternalIndex)
            End SyncLock
        End Sub

        ''' <summary>
        ''' Gets all generated external (receiving) addresses.
        ''' </summary>
        Public Function GetExternalAddresses() As List(Of DerivedAddress)
            SyncLock _syncLock
                Return New List(Of DerivedAddress)(_externalAddresses)
            End SyncLock
        End Function

        ''' <summary>
        ''' Gets all generated internal (change) addresses.
        ''' </summary>
        Public Function GetInternalAddresses() As List(Of DerivedAddress)
            SyncLock _syncLock
                Return New List(Of DerivedAddress)(_internalAddresses)
            End SyncLock
        End Function

        ''' <summary>
        ''' Checks whether the given address belongs to this account (external or internal chain).
        ''' </summary>
        ''' <param name="address">The address to check.</param>
        ''' <returns>True if the address belongs to this account.</returns>
        Public Function ContainsAddress(address As String) As Boolean
            If String.IsNullOrEmpty(address) Then Return False

            SyncLock _syncLock
                For Each derived As Object In _externalAddresses
                    If String.Equals(derived.Address, address, StringComparison.OrdinalIgnoreCase) Then
                        Return True
                    End If
                Next
                For Each derived As Object In _internalAddresses
                    If String.Equals(derived.Address, address, StringComparison.OrdinalIgnoreCase) Then
                        Return True
                    End If
                Next
            End SyncLock

            Return False
        End Function

        ''' <summary>
        ''' Gets the key pair for a specific address in this account.
        ''' </summary>
        ''' <param name="address">The address to get the key pair for.</param>
        ''' <returns>The key pair, or Nothing if the address is not found.</returns>
        Public Function GetKeyPairForAddress(address As String) As KeyPair
            If String.IsNullOrEmpty(address) Then Return Nothing

            SyncLock _syncLock
                For Each derived As Object In _externalAddresses
                    If String.Equals(derived.Address, address, StringComparison.OrdinalIgnoreCase) Then
                        Return DeriveKeyPair(0, derived.Index)
                    End If
                Next
                For Each derived As Object In _internalAddresses
                    If String.Equals(derived.Address, address, StringComparison.OrdinalIgnoreCase) Then
                        Return DeriveKeyPair(1, derived.Index)
                    End If
                Next
            End SyncLock

            Return Nothing
        End Function

        ''' <summary>
        ''' Discovers used addresses by scanning up to the gap limit.
        ''' Returns the number of addresses with activity found.
        ''' </summary>
        ''' <param name="isUsedCheck">A function that returns True if an address has been used on-chain.</param>
        ''' <returns>The number of used addresses discovered.</returns>
        Public Function DiscoverAddresses(isUsedCheck As Func(Of String, Boolean)) As Integer
            If isUsedCheck Is Nothing Then Throw New ArgumentNullException(NameOf(isUsedCheck))

            Dim discoveredCount As Integer = 0

            ' Scan external chain
            Dim consecutiveUnused As Integer = 0
            Dim index As Integer = 0
            While consecutiveUnused < GapLimit
                EnsureExternalAddress(index)
                Dim addr As DerivedAddress = _externalAddresses(index)
                If isUsedCheck(addr.Address) Then
                    addr.IsUsed = True
                    consecutiveUnused = 0
                    discoveredCount += 1
                    NextExternalIndex = index + 1
                Else
                    consecutiveUnused += 1
                End If
                index += 1
            End While

            ' Scan internal chain
            consecutiveUnused = 0
            index = 0
            While consecutiveUnused < GapLimit
                EnsureInternalAddress(index)
                Dim addr As DerivedAddress = _internalAddresses(index)
                If isUsedCheck(addr.Address) Then
                    addr.IsUsed = True
                    consecutiveUnused = 0
                    discoveredCount += 1
                    NextInternalIndex = index + 1
                Else
                    consecutiveUnused += 1
                End If
                index += 1
            End While

            Return discoveredCount
        End Function

        ''' <summary>
        ''' Gets the BIP44 derivation path for this account.
        ''' </summary>
        Public Function GetDerivationPath() As String
            Return $"m/44'/{Config.CoinType}'/{AccountIndex}'"
        End Function

        Private Sub EnsureExternalAddress(index As Integer)
            While _externalAddresses.Count <= index
                Dim newIndex As Integer = _externalAddresses.Count
                Dim address As String = DeriveAddress(0, newIndex)
                Dim derived As New DerivedAddress()
                derived.Address = address
                derived.Index = newIndex
                derived.Chain = AddressChain.External
                derived.IsUsed = False
                _externalAddresses.Add(derived)
            End While
        End Sub

        Private Sub EnsureInternalAddress(index As Integer)
            While _internalAddresses.Count <= index
                Dim newIndex As Integer = _internalAddresses.Count
                Dim address As String = DeriveAddress(1, newIndex)
                Dim derived As New DerivedAddress()
                derived.Address = address
                derived.Index = newIndex
                derived.Chain = AddressChain.Internal
                derived.IsUsed = False
                _internalAddresses.Add(derived)
            End While
        End Sub

        Private Function DeriveAddress(chain As Integer, index As Integer) As String
            Dim chainKey As HdKeyDerivation.ExtendedKey = HdKeyDerivation.DeriveChild(AccountKey, CUInt(chain))
            Dim childKey As HdKeyDerivation.ExtendedKey = HdKeyDerivation.DeriveChild(chainKey, CUInt(index))
            Dim keyPair As KeyPair = childKey.GetKeyPair()
            Return AddressEncoder.FromKeyPair(keyPair, Config.AddressVersion)
        End Function

        Private Function DeriveKeyPair(chain As Integer, index As Integer) As KeyPair
            Dim chainKey As HdKeyDerivation.ExtendedKey = HdKeyDerivation.DeriveChild(AccountKey, CUInt(chain))
            Dim childKey As HdKeyDerivation.ExtendedKey = HdKeyDerivation.DeriveChild(chainKey, CUInt(index))
            Return childKey.GetKeyPair()
        End Function

        Public Overrides Function ToString() As String
            Return $"{Name} (Index={AccountIndex}, External={NextExternalIndex}, Internal={NextInternalIndex})"
        End Function

    End Class

    ''' <summary>
    ''' Represents a derived address with its metadata.
    ''' </summary>
    Public Class DerivedAddress

        ''' <summary>The derived CryptoCoin address string.</summary>
        Public Property Address As String

        ''' <summary>The derivation index within the chain.</summary>
        Public Property Index As Integer

        ''' <summary>Whether this is an external or internal (change) address.</summary>
        Public Property Chain As AddressChain

        ''' <summary>Whether this address has been used in a transaction.</summary>
        Public Property IsUsed As Boolean

        Public Overrides Function ToString() As String
            Dim chainStr As String = If(Chain = AddressChain.External, "ext", "int")
            Return $"{chainStr}/{Index}: {Address} (used={IsUsed})"
        End Function

    End Class

    ''' <summary>
    ''' Indicates the address chain type in BIP44 derivation.
    ''' </summary>
    Public Enum AddressChain
        ''' <summary>External chain (index 0) for receiving addresses.</summary>
        External = 0
        ''' <summary>Internal chain (index 1) for change addresses.</summary>
        Internal = 1
    End Enum

End Namespace
