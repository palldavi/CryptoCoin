Imports CryptoCoin.Cryptography

Namespace CryptoCoin.Core.Serialization

    ''' <summary>
    ''' Binary reader for deserializing blockchain data from byte arrays.
    ''' Reads values in little-endian format as used by the protocol.
    ''' </summary>
    Public Class BufferReader

        Private ReadOnly _data As Byte()
        Private _offset As Integer

        ''' <summary>
        ''' Gets the current read position.
        ''' </summary>
        Public ReadOnly Property Position As Integer
            Get
                Return _offset
            End Get
        End Property

        ''' <summary>
        ''' Gets the total length of the buffer.
        ''' </summary>
        Public ReadOnly Property Length As Integer
            Get
                Return _data.Length
            End Get
        End Property

        ''' <summary>
        ''' Gets the number of bytes remaining to read.
        ''' </summary>
        Public ReadOnly Property Remaining As Integer
            Get
                Return _data.Length - _offset
            End Get
        End Property

        Public Sub New(data As Byte())
            If data Is Nothing Then Throw New ArgumentNullException(NameOf(data))
            _data = data
            _offset = 0
        End Sub

        Public Sub New(data As Byte(), offset As Integer)
            If data Is Nothing Then Throw New ArgumentNullException(NameOf(data))
            _data = data
            _offset = offset
        End Sub

        Public Function ReadByte() As Byte
            EnsureAvailable(1)
            Dim value As Byte = _data(_offset)
            _offset += 1
            Return value
        End Function

        Public Function ReadInt16() As Short
            EnsureAvailable(2)
            Dim value As Short = BitConverter.ToInt16(_data, _offset)
            _offset += 2
            Return value
        End Function

        Public Function ReadUInt16() As UShort
            EnsureAvailable(2)
            Dim value As UShort = BitConverter.ToUInt16(_data, _offset)
            _offset += 2
            Return value
        End Function

        Public Function ReadInt32() As Integer
            EnsureAvailable(4)
            Dim value As Integer = BitConverter.ToInt32(_data, _offset)
            _offset += 4
            Return value
        End Function

        Public Function ReadUInt32() As UInteger
            EnsureAvailable(4)
            Dim value As UInteger = BitConverter.ToUInt32(_data, _offset)
            _offset += 4
            Return value
        End Function

        Public Function ReadInt64() As Long
            EnsureAvailable(8)
            Dim value As Long = BitConverter.ToInt64(_data, _offset)
            _offset += 8
            Return value
        End Function

        Public Function ReadUInt64() As ULong
            EnsureAvailable(8)
            Dim value As ULong = BitConverter.ToUInt64(_data, _offset)
            _offset += 8
            Return value
        End Function

        Public Function ReadBytes(count As Integer) As Byte()
            EnsureAvailable(count)
            Dim result(count - 1) As Byte
            Array.Copy(_data, _offset, result, 0, count)
            _offset += count
            Return result
        End Function

        Public Function ReadVarInt() As Long
            Return VarInt.Decode(_data, _offset)
        End Function

        Public Function ReadHash() As Byte()
            Return ReadBytes(32)
        End Function

        Public Function ReadHashAsHex() As String
            Dim hash As Byte() = ReadHash()
            Return HashUtil.ToHex(hash)
        End Function

        Public Function ReadVarBytes() As Byte()
            Dim length As Integer = CInt(ReadVarInt())
            Return ReadBytes(length)
        End Function

        Public Function ReadString() As String
            Dim bytes As Byte() = ReadVarBytes()
            Return System.Text.Encoding.UTF8.GetString(bytes)
        End Function

        Public Sub Skip(count As Integer)
            EnsureAvailable(count)
            _offset += count
        End Sub

        Public Sub Seek(position As Integer)
            If position < 0 OrElse position > _data.Length Then
                Throw New ArgumentOutOfRangeException(NameOf(position))
            End If
            _offset = position
        End Sub

        Private Sub EnsureAvailable(count As Integer)
            If _offset + count > _data.Length Then
                Throw New InvalidOperationException(
                    $"Buffer underflow: need {count} bytes at offset {_offset}, but only {_data.Length - _offset} available.")
            End If
        End Sub

    End Class

End Namespace
