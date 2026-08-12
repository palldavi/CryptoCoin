Imports CryptoCoin.Core
Imports CryptoCoin.Cryptography

Namespace CryptoCoin.Mining

    ''' <summary>
    ''' Validates mining shares submitted by pool workers.
    ''' </summary>
    Public Class ShareValidator

        Private ReadOnly _shareDifficulty As UInteger
        Private ReadOnly _recentShares As New HashSet(Of String)()
        Private ReadOnly _syncLock As New Object()

        Public Sub New(shareDifficulty As UInteger)
            _shareDifficulty = shareDifficulty
        End Sub

        ''' <summary>
        ''' Validates a submitted share.
        ''' </summary>
        Public Function ValidateShare(headerBytes As Byte(), nonce As UInteger, jobId As String) As ShareValidationResult
            ' Compute hash with the given nonce
            ' Replace nonce bytes in header (bytes 76-79)
            Dim headerCopy(headerBytes.Length - 1) As Byte
            Array.Copy(headerBytes, headerCopy, headerBytes.Length)
            Dim nonceBytes As Byte() = BitConverter.GetBytes(nonce)
            Array.Copy(nonceBytes, 0, headerCopy, 76, 4)

            Dim hash As Byte() = HashUtil.DoubleSha256(headerCopy)
            Dim hashHex As String = HashUtil.ToHex(hash)

            ' Check for duplicate share
            Dim shareKey As String = $"{jobId}:{nonce}"
            SyncLock _syncLock
                If _recentShares.Contains(shareKey) Then
                    Return New ShareValidationResult(False, "Duplicate share.", hashHex)
                End If
                _recentShares.Add(shareKey)

                ' Limit memory usage
                If _recentShares.Count > 100000 Then
                    _recentShares.Clear()
                End If
            End SyncLock

            ' Check if meets share difficulty
            If Not DifficultyCalculator.MeetsTarget(hash, _shareDifficulty) Then
                Return New ShareValidationResult(False, "Does not meet share difficulty.", hashHex)
            End If

            Return New ShareValidationResult(True, "Valid share.", hashHex)
        End Function

        ''' <summary>
        ''' Checks if a share also meets the network difficulty (block found).
        ''' </summary>
        Public Function IsBlockSolution(hash As Byte(), networkDifficulty As UInteger) As Boolean
            Return DifficultyCalculator.MeetsTarget(hash, networkDifficulty)
        End Function

        ''' <summary>
        ''' Clears the duplicate share cache.
        ''' </summary>
        Public Sub ClearCache()
            SyncLock _syncLock
                _recentShares.Clear()
            End SyncLock
        End Sub

    End Class

    Public Class ShareValidationResult
        Public ReadOnly Property IsValid As Boolean
        Public ReadOnly Property Message As String
        Public ReadOnly Property Hash As String

        Public Sub New(isValid As Boolean, message As String, hash As String)
            Me.IsValid = isValid
            Me.Message = message
            Me.Hash = hash
        End Sub
    End Class

End Namespace
