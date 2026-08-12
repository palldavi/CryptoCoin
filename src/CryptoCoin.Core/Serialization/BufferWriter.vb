Imports System.IO
Imports CryptoCoin.Cryptography

Namespace CryptoCoin.Core.Serialization

    ''' <summary>
    ''' Binary writer for serializing blockchain data to byte arrays.
    ''' Writes values in little-endian format as used by the protocol.
    ''' </summary>
    Public Class BufferWriter

        Private ReadOnly _stream As MemoryStream

        ''' <summary>
        ''' Gets the current write position (number of bytes written).
        ''' </summary>
        Public ReadOnly Property Position As Integer
            Get
                Return CInt(_stream.Position)
            End Get
        End Property

        Public Sub New()
            _stream = New MemoryStream()
        End Sub

        Public Sub New(capacity As Integer)
            _stream = New MemoryStream(capacity)
        End Sub

        Public Sub WriteByte(value As Byte)
            _stream.WriteByte(value)
        End Sub

        Public Sub WriteInt16(value As Short)
            Dim bytes As Byte() = BitConverter.GetBytes(value)
            _stream.Write(bytes, 0, 2)
        End Sub

        Public Sub WriteUInt16(value As UShort)
            Dim bytes As Byte() = BitConverter.GetBytes(value)
            _stream.Write(bytes, 0, 2)
        End Sub

        Public Sub WriteInt32(value As Integer)
            Dim bytes As Byte() = BitConverter.GetBytes(value)
            _stream.Write(bytes, 0, 4)
        End Sub

        Public Sub WriteUInt32(value As UInteger)
            Dim bytes As Byte() = BitConverter.GetBytes(value)
            _stream.Write(bytes, 0, 4)
        End Sub

        Public Sub WriteInt64(value As Long)
            Dim bytes As Byte() = BitConverter.GetBytes(value)
            _stream.Write(bytes, 0, 8)
        End Sub

        Public Sub WriteUInt64(value As ULong)
            Dim bytes As Byte() = BitConverter.GetBytes(value)
            _stream.Write(bytes, 0, 8)
        End Sub

        Public Sub WriteBytes(data As Byte())
            If data Is Nothing Then Throw New ArgumentNullException(NameOf(data))
            _stream.Write(data, 0, data.Length)
        End Sub

        Public Sub WriteVarInt(value As Long)
            Dim encoded As Byte() = VarInt.Encode(value)
            _stream.Write(encoded, 0, encoded.Length)
        End Sub

        Public Sub WriteHash(hash As Byte())
            If hash Is Nothing OrElse hash.Length <> 32 Then
                Throw New ArgumentException("Hash must be 32 bytes.")
            End If
            _stream.Write(hash, 0, 32)
        End Sub

        Public Sub WriteHashFromHex(hex As String)
            Dim hash As Byte() = HashUtil.FromHex(hex)
            If hash.Length <> 32 Then
                Throw New ArgumentException("Hash hex must represent 32 bytes.")
            End If
            _stream.Write(hash, 0, 32)
        End Sub

        Public Sub WriteVarBytes(data As Byte())
            If data Is Nothing Then
                WriteVarInt(0)
            Else
                WriteVarInt(data.Length)
                _stream.Write(data, 0, data.Length)
            End If
        End Sub

        Public Sub WriteString(value As String)
            If value Is Nothing Then
                WriteVarInt(0)
            Else
                Dim bytes As Byte() = System.Text.Encoding.UTF8.GetBytes(value)
                WriteVarBytes(bytes)
            End If
        End Sub

        ''' <summary>
        ''' Gets the written data as a byte array.
        ''' </summary>
        Public Function ToArray() As Byte()
            Return _stream.ToArray()
        End Function

        ''' <summary>
        ''' Resets the writer to the beginning.
        ''' </summary>
        Public Sub Reset()
            _stream.SetLength(0)
            _stream.Position = 0
        End Sub

    End Class

End Namespace
