Namespace CryptoCoin.Cryptography

    ''' <summary>
    ''' Managed implementation of the RIPEMD-160 hash algorithm.
    ''' Used for generating CryptoCoin addresses (Hash160 = SHA256 + RIPEMD160).
    ''' </summary>
    Public Class Ripemd160Hasher

        Private Const BlockSize As Integer = 64
        Private Const DigestSize As Integer = 20

        Private _h0 As UInteger = &H67452301UI
        Private _h1 As UInteger = &HEFCDAB89UI
        Private _h2 As UInteger = &H98BADCFEUI
        Private _h3 As UInteger = &H10325476UI
        Private _h4 As UInteger = &HC3D2E1F0UI

        ''' <summary>
        ''' Computes the RIPEMD-160 hash of the given data.
        ''' </summary>
        Public Function ComputeHash(data As Byte()) As Byte()
            If data Is Nothing Then Throw New ArgumentNullException(NameOf(data))

            ' Reset state
            _h0 = &H67452301UI
            _h1 = &HEFCDAB89UI
            _h2 = &H98BADCFEUI
            _h3 = &H10325476UI
            _h4 = &HC3D2E1F0UI

            ' Pre-processing: adding padding bits
            Dim msgLen As Long = data.Length
            Dim bitLen As Long = msgLen * 8

            ' Calculate padded length
            Dim paddedLen As Integer = CInt(((msgLen + 8) \ 64 + 1) * 64)
            Dim padded(paddedLen - 1) As Byte
            Array.Copy(data, padded, CInt(msgLen))
            padded(CInt(msgLen)) = &H80

            ' Append original length in bits as 64-bit little-endian
            Dim lenBytes As Byte() = BitConverter.GetBytes(bitLen)
            Array.Copy(lenBytes, 0, padded, paddedLen - 8, 8)

            ' Process each 512-bit block
            For i As Integer = 0 To paddedLen - 1 Step BlockSize
                ProcessBlock(padded, i)
            Next

            ' Produce the final hash value (little-endian)
            Dim hash(DigestSize - 1) As Byte
            WriteUInt32LE(_h0, hash, 0)
            WriteUInt32LE(_h1, hash, 4)
            WriteUInt32LE(_h2, hash, 8)
            WriteUInt32LE(_h3, hash, 12)
            WriteUInt32LE(_h4, hash, 16)

            Return hash
        End Function

        Private Sub ProcessBlock(data As Byte(), offset As Integer)
            Dim x(15) As UInteger
            For i As Integer = 0 To 15
                x(i) = ReadUInt32LE(data, offset + i * 4)
            Next

            Dim al As UInteger = _h0
            Dim bl As UInteger = _h1
            Dim cl As UInteger = _h2
            Dim dl As UInteger = _h3
            Dim el As UInteger = _h4

            Dim ar As UInteger = _h0
            Dim br As UInteger = _h1
            Dim cr As UInteger = _h2
            Dim dr As UInteger = _h3
            Dim er As UInteger = _h4

            ' Left rounds
            Dim rl As Integer() = {
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15,
                7, 4, 13, 1, 10, 6, 15, 3, 12, 0, 9, 5, 2, 14, 11, 8,
                3, 10, 14, 4, 9, 15, 8, 1, 2, 7, 0, 6, 13, 11, 5, 12,
                1, 9, 11, 10, 0, 8, 12, 4, 13, 3, 7, 15, 14, 5, 6, 2,
                4, 0, 5, 9, 7, 12, 2, 10, 14, 1, 3, 8, 11, 6, 15, 13
            }

            Dim sl As Integer() = {
                11, 14, 15, 12, 5, 8, 7, 9, 11, 13, 14, 15, 6, 7, 9, 8,
                7, 6, 8, 13, 11, 9, 7, 15, 7, 12, 15, 9, 11, 7, 13, 12,
                11, 13, 6, 7, 14, 9, 13, 15, 14, 8, 13, 6, 5, 12, 7, 5,
                11, 12, 14, 15, 14, 15, 9, 8, 9, 14, 5, 6, 8, 6, 5, 12,
                9, 15, 5, 11, 6, 8, 13, 12, 5, 12, 13, 14, 11, 8, 5, 6
            }

            ' Right rounds
            Dim rr As Integer() = {
                5, 14, 7, 0, 9, 2, 11, 4, 13, 6, 15, 8, 1, 10, 3, 12,
                6, 11, 3, 7, 0, 13, 5, 10, 14, 15, 8, 12, 4, 9, 1, 2,
                15, 5, 1, 3, 7, 14, 6, 9, 11, 8, 12, 2, 10, 0, 4, 13,
                8, 6, 4, 1, 3, 11, 15, 0, 5, 12, 2, 13, 9, 7, 10, 14,
                12, 15, 10, 4, 1, 5, 8, 7, 6, 2, 13, 14, 0, 3, 9, 11
            }

            Dim sr As Integer() = {
                8, 9, 9, 11, 13, 15, 15, 5, 7, 7, 8, 11, 14, 14, 12, 6,
                9, 13, 15, 7, 12, 8, 9, 11, 7, 7, 12, 7, 6, 15, 13, 11,
                9, 7, 15, 11, 8, 6, 6, 14, 12, 13, 5, 14, 13, 13, 7, 5,
                15, 5, 8, 11, 14, 14, 6, 14, 6, 9, 12, 9, 12, 5, 15, 8,
                8, 5, 12, 9, 12, 5, 14, 6, 8, 13, 6, 5, 15, 13, 11, 11
            }

            For j As Integer = 0 To 79
                Dim round As Integer = j \ 16
                Dim fl, fr As UInteger
                Dim kl, kr As UInteger

                Select Case round
                    Case 0
                        fl = F1(bl, cl, dl) : kl = &H0UI
                        fr = F5(br, cr, dr) : kr = &H50A28BE6UI
                    Case 1
                        fl = F2(bl, cl, dl) : kl = &H5A827999UI
                        fr = F4(br, cr, dr) : kr = &H5C4DD124UI
                    Case 2
                        fl = F3(bl, cl, dl) : kl = &H6ED9EBA1UI
                        fr = F3(br, cr, dr) : kr = &H6D703EF3UI
                    Case 3
                        fl = F4(bl, cl, dl) : kl = &H8F1BBCDCUI
                        fr = F2(br, cr, dr) : kr = &H7A6D76E9UI
                    Case Else
                        fl = F5(bl, cl, dl) : kl = &HA953FD4EUI
                        fr = F1(br, cr, dr) : kr = &H0UI
                End Select

                Dim tl As UInteger = RotateLeft(al + fl + x(rl(j)) + kl, sl(j)) + el
                al = el
                el = dl
                dl = RotateLeft(cl, 10)
                cl = bl
                bl = tl

                Dim tr As UInteger = RotateLeft(ar + fr + x(rr(j)) + kr, sr(j)) + er
                ar = er
                er = dr
                dr = RotateLeft(cr, 10)
                cr = br
                br = tr
            Next

            Dim t As UInteger = _h1 + cl + dr
            _h1 = _h2 + dl + er
            _h2 = _h3 + el + ar
            _h3 = _h4 + al + br
            _h4 = _h0 + bl + cr
            _h0 = t
        End Sub

        Private Shared Function F1(x As UInteger, y As UInteger, z As UInteger) As UInteger
            Return x Xor y Xor z
        End Function

        Private Shared Function F2(x As UInteger, y As UInteger, z As UInteger) As UInteger
            Return (x And y) Or (Not x And z)
        End Function

        Private Shared Function F3(x As UInteger, y As UInteger, z As UInteger) As UInteger
            Return (x Or Not y) Xor z
        End Function

        Private Shared Function F4(x As UInteger, y As UInteger, z As UInteger) As UInteger
            Return (x And z) Or (y And Not z)
        End Function

        Private Shared Function F5(x As UInteger, y As UInteger, z As UInteger) As UInteger
            Return x Xor (y Or Not z)
        End Function

        Private Shared Function RotateLeft(value As UInteger, bits As Integer) As UInteger
            Return (value << bits) Or (value >> (32 - bits))
        End Function

        Private Shared Function ReadUInt32LE(data As Byte(), offset As Integer) As UInteger
            Return CUInt(data(offset)) Or
                   (CUInt(data(offset + 1)) << 8) Or
                   (CUInt(data(offset + 2)) << 16) Or
                   (CUInt(data(offset + 3)) << 24)
        End Function

        Private Shared Sub WriteUInt32LE(value As UInteger, data As Byte(), offset As Integer)
            data(offset) = CByte(value And &HFFUI)
            data(offset + 1) = CByte((value >> 8) And &HFFUI)
            data(offset + 2) = CByte((value >> 16) And &HFFUI)
            data(offset + 3) = CByte((value >> 24) And &HFFUI)
        End Sub

    End Class

End Namespace
