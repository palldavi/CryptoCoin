Imports System.Collections.Generic
Imports CryptoCoin.Cryptography

Namespace CryptoCoin.Wallet

    ''' <summary>
    ''' Manages a contact address book for the wallet.
    ''' Stores labeled addresses for easy identification and reuse.
    ''' </summary>
    Public Class AddressBook

        Private ReadOnly _contacts As Dictionary(Of String, AddressBookEntry)
        Private ReadOnly _syncLock As New Object()

        ''' <summary>
        ''' Gets the number of contacts in the address book.
        ''' </summary>
        Public ReadOnly Property Count As Integer
            Get
                SyncLock _syncLock
                    Return _contacts.Count
                End SyncLock
            End Get
        End Property

        ''' <summary>
        ''' Creates a new empty address book.
        ''' </summary>
        Public Sub New()
            _contacts = New Dictionary(Of String, AddressBookEntry)(StringComparer.OrdinalIgnoreCase)
        End Sub

        ''' <summary>
        ''' Adds a new contact to the address book.
        ''' </summary>
        ''' <param name="address">The CryptoCoin address.</param>
        ''' <param name="label">A human-readable label for the contact.</param>
        ''' <param name="notes">Optional notes about the contact.</param>
        ''' <exception cref="ArgumentException">Thrown if the address is invalid or already exists.</exception>
        Public Sub AddContact(address As String, label As String, Optional notes As String = "")
            If String.IsNullOrEmpty(address) Then Throw New ArgumentNullException(NameOf(address))
            If String.IsNullOrEmpty(label) Then Throw New ArgumentNullException(NameOf(label))

            If Not AddressEncoder.IsValid(address) Then
                Throw New ArgumentException("Invalid CryptoCoin address.", NameOf(address))
            End If

            SyncLock _syncLock
                If _contacts.ContainsKey(address) Then
                    Throw New ArgumentException($"Address '{address}' already exists in the address book.", NameOf(address))
                End If

                Dim entry As New AddressBookEntry()
                entry.Address = address
                entry.Label = label
                entry.Notes = If(notes, String.Empty)
                entry.DateAdded = DateTime.UtcNow

                _contacts.Add(address, entry)
            End SyncLock
        End Sub

        ''' <summary>
        ''' Removes a contact from the address book.
        ''' </summary>
        ''' <param name="address">The address to remove.</param>
        ''' <returns>True if the contact was removed; false if not found.</returns>
        Public Function RemoveContact(address As String) As Boolean
            If String.IsNullOrEmpty(address) Then Return False

            SyncLock _syncLock
                Return _contacts.Remove(address)
            End SyncLock
        End Function

        ''' <summary>
        ''' Updates the label and notes for an existing contact.
        ''' </summary>
        ''' <param name="address">The address to update.</param>
        ''' <param name="newLabel">The new label.</param>
        ''' <param name="newNotes">The new notes.</param>
        ''' <returns>True if the contact was updated; false if not found.</returns>
        Public Function UpdateContact(address As String, newLabel As String, Optional newNotes As String = Nothing) As Boolean
            If String.IsNullOrEmpty(address) Then Return False
            If String.IsNullOrEmpty(newLabel) Then Throw New ArgumentNullException(NameOf(newLabel))

            SyncLock _syncLock
                Dim entry As AddressBookEntry = Nothing
                If Not _contacts.TryGetValue(address, entry) Then Return False

                entry.Label = newLabel
                If newNotes IsNot Nothing Then
                    entry.Notes = newNotes
                End If
                Return True
            End SyncLock
        End Function

        ''' <summary>
        ''' Gets a contact by address.
        ''' </summary>
        ''' <param name="address">The address to look up.</param>
        ''' <returns>The address book entry, or Nothing if not found.</returns>
        Public Function GetContact(address As String) As AddressBookEntry
            If String.IsNullOrEmpty(address) Then Return Nothing

            SyncLock _syncLock
                Dim entry As AddressBookEntry = Nothing
                _contacts.TryGetValue(address, entry)
                Return entry
            End SyncLock
        End Function

        ''' <summary>
        ''' Searches contacts by label (case-insensitive partial match).
        ''' </summary>
        ''' <param name="searchTerm">The search term to match against labels.</param>
        ''' <returns>A list of matching contacts.</returns>
        Public Function SearchByLabel(searchTerm As String) As List(Of AddressBookEntry)
            If String.IsNullOrEmpty(searchTerm) Then Return GetAllContacts()

            Dim results As New List(Of AddressBookEntry)()

            SyncLock _syncLock
                For Each entry As Object In _contacts.Values
                    If entry.Label.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0 Then
                        results.Add(entry)
                    End If
                Next
            End SyncLock

            Return results
        End Function

        ''' <summary>
        ''' Gets the label for an address, or an empty string if not found.
        ''' </summary>
        ''' <param name="address">The address to look up.</param>
        ''' <returns>The label or empty string.</returns>
        Public Function GetLabel(address As String) As String
            Dim entry As AddressBookEntry = GetContact(address)
            If entry IsNot Nothing Then Return entry.Label
            Return String.Empty
        End Function

        ''' <summary>
        ''' Checks whether an address exists in the address book.
        ''' </summary>
        ''' <param name="address">The address to check.</param>
        ''' <returns>True if the address is in the book.</returns>
        Public Function Contains(address As String) As Boolean
            If String.IsNullOrEmpty(address) Then Return False

            SyncLock _syncLock
                Return _contacts.ContainsKey(address)
            End SyncLock
        End Function

        ''' <summary>
        ''' Gets all contacts in the address book.
        ''' </summary>
        ''' <returns>A list of all address book entries.</returns>
        Public Function GetAllContacts() As List(Of AddressBookEntry)
            SyncLock _syncLock
                Return New List(Of AddressBookEntry)(_contacts.Values)
            End SyncLock
        End Function

        ''' <summary>
        ''' Removes all contacts from the address book.
        ''' </summary>
        Public Sub Clear()
            SyncLock _syncLock
                _contacts.Clear()
            End SyncLock
        End Sub

    End Class

    ''' <summary>
    ''' Represents a single entry in the wallet address book.
    ''' </summary>
    Public Class AddressBookEntry

        ''' <summary>
        ''' The CryptoCoin address.
        ''' </summary>
        Public Property Address As String

        ''' <summary>
        ''' A human-readable label for this contact.
        ''' </summary>
        Public Property Label As String

        ''' <summary>
        ''' Optional notes about this contact.
        ''' </summary>
        Public Property Notes As String = String.Empty

        ''' <summary>
        ''' The date this contact was added.
        ''' </summary>
        Public Property DateAdded As DateTime

        Public Overrides Function ToString() As String
            Return $"{Label} ({Address})"
        End Function

    End Class

End Namespace
