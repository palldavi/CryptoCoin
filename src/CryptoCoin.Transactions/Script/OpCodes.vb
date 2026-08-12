Namespace CryptoCoin.Transactions.Script

    ''' <summary>
    ''' Defines the opcodes used in CryptoCoin's scripting language.
    ''' Based on Bitcoin's Script with a subset of operations.
    ''' </summary>
    Public NotInheritable Class OpCodes

        Private Sub New()
        End Sub

        ' Constants
        Public Const OP_0 As Byte = &H0
        Public Const OP_FALSE As Byte = &H0
        Public Const OP_PUSHDATA1 As Byte = &H4C
        Public Const OP_PUSHDATA2 As Byte = &H4D
        Public Const OP_PUSHDATA4 As Byte = &H4E
        Public Const OP_1NEGATE As Byte = &H4F
        Public Const OP_1 As Byte = &H51
        Public Const OP_TRUE As Byte = &H51
        Public Const OP_2 As Byte = &H52
        Public Const OP_3 As Byte = &H53
        Public Const OP_4 As Byte = &H54
        Public Const OP_5 As Byte = &H55
        Public Const OP_6 As Byte = &H56
        Public Const OP_7 As Byte = &H57
        Public Const OP_8 As Byte = &H58
        Public Const OP_9 As Byte = &H59
        Public Const OP_10 As Byte = &H5A
        Public Const OP_11 As Byte = &H5B
        Public Const OP_12 As Byte = &H5C
        Public Const OP_13 As Byte = &H5D
        Public Const OP_14 As Byte = &H5E
        Public Const OP_15 As Byte = &H5F
        Public Const OP_16 As Byte = &H60

        ' Flow control
        Public Const OP_NOP As Byte = &H61
        Public Const OP_IF As Byte = &H63
        Public Const OP_NOTIF As Byte = &H64
        Public Const OP_ELSE As Byte = &H67
        Public Const OP_ENDIF As Byte = &H68
        Public Const OP_VERIFY As Byte = &H69
        Public Const OP_RETURN As Byte = &H6A

        ' Stack
        Public Const OP_TOALTSTACK As Byte = &H6B
        Public Const OP_FROMALTSTACK As Byte = &H6C
        Public Const OP_IFDUP As Byte = &H73
        Public Const OP_DEPTH As Byte = &H74
        Public Const OP_DROP As Byte = &H75
        Public Const OP_DUP As Byte = &H76
        Public Const OP_NIP As Byte = &H77
        Public Const OP_OVER As Byte = &H78
        Public Const OP_PICK As Byte = &H79
        Public Const OP_ROLL As Byte = &H7A
        Public Const OP_ROT As Byte = &H7B
        Public Const OP_SWAP As Byte = &H7C
        Public Const OP_TUCK As Byte = &H7D
        Public Const OP_2DROP As Byte = &H6D
        Public Const OP_2DUP As Byte = &H6E
        Public Const OP_3DUP As Byte = &H6F
        Public Const OP_2OVER As Byte = &H70
        Public Const OP_2ROT As Byte = &H71
        Public Const OP_2SWAP As Byte = &H72

        ' Splice
        Public Const OP_SIZE As Byte = &H82

        ' Bitwise logic
        Public Const OP_EQUAL As Byte = &H87
        Public Const OP_EQUALVERIFY As Byte = &H88

        ' Arithmetic
        Public Const OP_1ADD As Byte = &H8B
        Public Const OP_1SUB As Byte = &H8C
        Public Const OP_NEGATE As Byte = &H8F
        Public Const OP_ABS As Byte = &H90
        Public Const OP_NOT As Byte = &H91
        Public Const OP_0NOTEQUAL As Byte = &H92
        Public Const OP_ADD As Byte = &H93
        Public Const OP_SUB As Byte = &H94
        Public Const OP_BOOLAND As Byte = &H9A
        Public Const OP_BOOLOR As Byte = &H9B
        Public Const OP_NUMEQUAL As Byte = &H9C
        Public Const OP_NUMEQUALVERIFY As Byte = &H9D
        Public Const OP_NUMNOTEQUAL As Byte = &H9E
        Public Const OP_LESSTHAN As Byte = &H9F
        Public Const OP_GREATERTHAN As Byte = &HA0
        Public Const OP_LESSTHANOREQUAL As Byte = &HA1
        Public Const OP_GREATERTHANOREQUAL As Byte = &HA2
        Public Const OP_MIN As Byte = &HA3
        Public Const OP_MAX As Byte = &HA4
        Public Const OP_WITHIN As Byte = &HA5

        ' Crypto
        Public Const OP_RIPEMD160 As Byte = &HA6
        Public Const OP_SHA1 As Byte = &HA7
        Public Const OP_SHA256 As Byte = &HA8
        Public Const OP_HASH160 As Byte = &HA9
        Public Const OP_HASH256 As Byte = &HAA
        Public Const OP_CODESEPARATOR As Byte = &HAB
        Public Const OP_CHECKSIG As Byte = &HAC
        Public Const OP_CHECKSIGVERIFY As Byte = &HAD
        Public Const OP_CHECKMULTISIG As Byte = &HAE
        Public Const OP_CHECKMULTISIGVERIFY As Byte = &HAF

        ' Lock time
        Public Const OP_CHECKLOCKTIMEVERIFY As Byte = &HB1
        Public Const OP_CHECKSEQUENCEVERIFY As Byte = &HB2

        ''' <summary>
        ''' Gets the name of an opcode.
        ''' </summary>
        Public Shared Function GetName(opcode As Byte) As String
            Select Case opcode
                Case OP_0 : Return "OP_0"
                Case OP_1NEGATE : Return "OP_1NEGATE"
                Case OP_NOP : Return "OP_NOP"
                Case OP_IF : Return "OP_IF"
                Case OP_NOTIF : Return "OP_NOTIF"
                Case OP_ELSE : Return "OP_ELSE"
                Case OP_ENDIF : Return "OP_ENDIF"
                Case OP_VERIFY : Return "OP_VERIFY"
                Case OP_RETURN : Return "OP_RETURN"
                Case OP_DUP : Return "OP_DUP"
                Case OP_DROP : Return "OP_DROP"
                Case OP_SWAP : Return "OP_SWAP"
                Case OP_EQUAL : Return "OP_EQUAL"
                Case OP_EQUALVERIFY : Return "OP_EQUALVERIFY"
                Case OP_HASH160 : Return "OP_HASH160"
                Case OP_HASH256 : Return "OP_HASH256"
                Case OP_CHECKSIG : Return "OP_CHECKSIG"
                Case OP_CHECKMULTISIG : Return "OP_CHECKMULTISIG"
                Case OP_ADD : Return "OP_ADD"
                Case OP_SUB : Return "OP_SUB"
                Case Else
                    If opcode >= OP_1 AndAlso opcode <= OP_16 Then
                        Return $"OP_{opcode - OP_1 + 1}"
                    End If
                    If opcode >= 1 AndAlso opcode <= 75 Then
                        Return $"PUSH_{opcode}"
                    End If
                    Return $"OP_UNKNOWN_{opcode:X2}"
            End Select
        End Function

    End Class

End Namespace
