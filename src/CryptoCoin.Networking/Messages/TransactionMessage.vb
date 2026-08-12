Imports CryptoCoin.Cryptography

Namespace CryptoCoin.Networking

    ''' <summary>
    ''' Network message containing a single transaction.
    ''' Sent to relay unconfirmed transactions to peers or in response to GetData requests.
    ''' </summary>
    Public Class TransactionMessage

        ''' <summary>
        ''' The transaction version number.
        ''' </summary>
        Public Property Version As Integer

        ''' <summary>
        ''' The number of inputs in the transaction.
        ''' </summary>
        Public Property InputCount As Integer

        ''' <summary>
        ''' The number of outputs in the transaction.
        ''' </summary>
        Public Property OutputCount As Integer

        ''' <summary>
        ''' The raw serialized transaction data.
        ''' </summary>
        Public Property RawData As Byte()

        ''' <summary>
        ''' The lock time for the transaction (block height or Unix timestamp).
        ''' </summary>
        Public Property LockTime As UInteger

        ''' <summary>
        ''' The computed transaction ID (double SHA-256 of the serialized data).
        ''' </summary>
        Public ReadOnly Property TransactionId As String
            Get
                If RawData Is Nothing OrElse RawData.Length = 0 Then Return String.Empty
                Dim hash As Byte() = HashUtil.DoubleSha256(RawData)
                Return HashUtil.ToHex(hash)
            End Get
        End Property

        ''' <summary>
        ''' The size of the transaction in bytes.
        ''' </summary>
        Public ReadOnly Property Size As Integer
            Get
                If RawData Is Nothing Then Return 0
                Return RawData.Length
            End Get
        End Property

        ''' <summary>
        ''' Whether this transaction appears to be a coinbase transaction.
        ''' </summary>
        Public ReadOnly Property IsCoinbase As Boolean
            Get
                Return InputCount = 1 AndAlso Version >= 1 AndAlso HasCoinbaseMarker()
            End Get
        End Property

        ''' <summary>
        ''' Creates a new empty TransactionMessage.
        ''' </summary>
        Public Sub New()
            Version = 1
            InputCount = 0
            OutputCount = 0
            RawData = Array.Empty(Of Byte)()
            LockTime = 0UI
        End Sub

        ''' <summary>
        ''' Creates a TransactionMessage from raw serialized transaction data.
        ''' </summary>
        ''' <param name="rawTransactionData">The complete serialized transaction.</param>
        Public Sub New(rawTransactionData As Byte())
            If rawTransactionData Is Nothing Then
                Throw New ArgumentNullException(NameOf(rawTransactionData))
            End If
            RawData = rawTransactionData
            ParseHeader()
        End Sub

        ''' <summary>
        ''' Serializes the transaction message to a byte array payload.
        ''' </summary>
        ''' <returns>The serialized payload bytes.</returns>
        Public Function Serialize() As Byte()
            If RawData IsNot Nothing AndAlso RawData.Length > 0 Then
                Dim result(RawData.Length - 1) As Byte
                Array.Copy(RawData, result, RawData.Length)
                Return result
            End If

            ' Build minimal transaction structure
            Dim parts As New List(Of Byte())()

            ' Version (4 bytes)
            parts.Add(BitConverter.GetBytes(Version))

            ' Input count (varint)
            parts.Add(EncodeVarInt(InputCount))

            ' Output count (varint)
            parts.Add(EncodeVarInt(OutputCount))

            ' Lock time (4 bytes)
            parts.Add(BitConverter.GetBytes(LockTime))

            Dim totalSize As Integer = 0
            For Each p As Object In parts
                totalSize += p.Length
            Next

            Dim buffer(totalSize - 1) As Byte
            Dim offset As Integer = 0
            For Each p As Object In parts
                Array.Copy(p, 0, buffer, offset, p.Length)
                offset += p.Length
            Next

            Return buffer
        End Function

        ''' <summary>
        ''' Deserializes a transaction message from a byte array payload.
        ''' </summary>
        ''' <param name="data">The payload bytes to deserialize.</param>
        ''' <returns>A populated TransactionMessage instance.</returns>
        Public Shared Function Deserialize(data As Byte()) As TransactionMessage
            If data Is Nothing OrElse data.Length < 10 Then
                Throw New ArgumentException("Transaction message payload too short.")
            End If

            Dim msg As New TransactionMessage()
            msg.RawData = New Byte(data.Length - 1) {}
            Array.Copy(data, msg.RawData, data.Length)
            msg.ParseHeader()

            Return msg
        End Function

        ''' <summary>
        ''' Validates the basic structure of the transaction message.
        ''' </summary>
        ''' <returns>True if the message structure appears valid.</returns>
        Public Function ValidateStructure() As Boolean
            ' Must have raw data
            If RawData Is Nothing OrElse RawData.Length < 10 Then Return False

            ' Must have at least one input and one output
            If InputCount <= 0 Then Return False
            If OutputCount <= 0 Then Return False

            ' Size sanity check (max 1 MB for a single transaction)
            If RawData.Length > 1048576 Then Return False

            Return True
        End Function

        ''' <summary>
        ''' Wraps this transaction message in a NetworkMessage for transmission.
        ''' </summary>
        ''' <returns>A NetworkMessage with the "tx" command.</returns>
        Public Function ToNetworkMessage() As NetworkMessage
            Return New NetworkMessage(NetworkCommands.Tx, Serialize())
        End Function

        ''' <summary>
        ''' Parses the version and input/output counts from the raw data header.
        ''' </summary>
        Private Sub ParseHeader()
            If RawData Is Nothing OrElse RawData.Length < 5 Then Return

            Dim offset As Integer = 0

            ' Version (4 bytes)
            Version = BitConverter.ToInt32(RawData, offset)
            offset += 4

            ' Input count (varint)
            Dim inCount As Integer = 0
            Dim varIntSize As Integer = DecodeVarInt(RawData, offset, inCount)
            InputCount = inCount
            offset += varIntSize

            ' We need to skip past inputs to find output count
            ' For now, estimate from remaining data
            If offset < RawData.Length Then
                ' Try to read output count after skipping inputs
                ' This is a simplified parse - full parsing would walk each input
                OutputCount = EstimateOutputCount()
            End If

            ' Lock time is last 4 bytes
            If RawData.Length >= 4 Then
                LockTime = BitConverter.ToUInt32(RawData, RawData.Length - 4)
            End If
        End Sub

        ''' <summary>
        ''' Estimates the output count from the raw data structure.
        ''' </summary>
        Private Function EstimateOutputCount() As Integer
            ' Simplified estimation - in a real implementation this would
            ' fully parse the transaction structure
            If RawData.Length < 50 Then Return 1
            Return Math.Max(1, (RawData.Length - 10) \ 34)
        End Function

        ''' <summary>
        ''' Checks if the transaction has a coinbase marker in the first input.
        ''' </summary>
        Private Function HasCoinbaseMarker() As Boolean
            If RawData Is Nothing OrElse RawData.Length < 41 Then Return False
            ' Coinbase transactions have a null previous output hash (32 zero bytes)
            ' starting at offset 5 (after version + varint input count)
            Dim offset As Integer = 5
            For i As Integer = 0 To 31
                If offset + i >= RawData.Length Then Return False
                If RawData(offset + i) <> 0 Then Return False
            Next
            Return True
        End Function

        Private Shared Function EncodeVarInt(value As Integer) As Byte()
            If value < 253 Then
                Return New Byte() {CByte(value)}
            ElseIf value <= &HFFFF Then
                Dim result(2) As Byte
                result(0) = 253
                Array.Copy(BitConverter.GetBytes(CUShort(value)), 0, result, 1, 2)
                Return result
            Else
                Dim result(4) As Byte
                result(0) = 254
                Array.Copy(BitConverter.GetBytes(CUInt(value)), 0, result, 1, 4)
                Return result
            End If
        End Function

        Private Shared Function DecodeVarInt(data As Byte(), offset As Integer, ByRef value As Integer) As Integer
            If offset >= data.Length Then
                value = 0
                Return 1
            End If
            If data(offset) < 253 Then
                value = CInt(data(offset))
                Return 1
            ElseIf data(offset) = 253 AndAlso offset + 2 < data.Length Then
                value = CInt(BitConverter.ToUInt16(data, offset + 1))
                Return 3
            ElseIf data(offset) = 254 AndAlso offset + 4 < data.Length Then
                value = CInt(BitConverter.ToUInt32(data, offset + 1))
                Return 5
            Else
                value = 0
                Return 1
            End If
        End Function

        Public Overrides Function ToString() As String
            Return $"TransactionMessage(TxId={TransactionId.Substring(0, Math.Min(16, TransactionId.Length))}..., Size={Size})"
        End Function

    End Class

End Namespace
