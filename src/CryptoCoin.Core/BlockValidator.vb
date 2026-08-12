Namespace CryptoCoin.Core

    ''' <summary>
    ''' Validates blocks against the CryptoCoin consensus rules.
    ''' Performs both structural and contextual validation.
    ''' </summary>
    Public Class BlockValidator

        Private ReadOnly _params As ChainParameters

        Public Sub New(params As ChainParameters)
            If params Is Nothing Then Throw New ArgumentNullException(NameOf(params))
            _params = params
        End Sub

        ''' <summary>
        ''' Performs full validation of a block.
        ''' </summary>
        Public Function Validate(block As Block, chainState As ChainState) As BlockValidationResult
            Dim result As New BlockValidationResult()

            ' Structural validation
            ValidateStructure(block, result)
            If Not result.IsValid Then Return result

            ' Contextual validation
            ValidateContext(block, chainState, result)

            Return result
        End Function

        ''' <summary>
        ''' Validates block structure (can be done without chain context).
        ''' </summary>
        Public Sub ValidateStructure(block As Block, result As BlockValidationResult)
            ' Header must exist
            If block.Header Is Nothing Then
                result.AddError("Block header is missing.")
                Return
            End If

            ' Must have at least one transaction (coinbase)
            If block.TransactionIds Is Nothing OrElse block.TransactionIds.Count = 0 Then
                result.AddError("Block must contain at least one transaction.")
                Return
            End If

            ' Check max transactions
            If block.TransactionIds.Count > _params.MaxTransactionsPerBlock Then
                result.AddError($"Too many transactions: {block.TransactionIds.Count} > {_params.MaxTransactionsPerBlock}")
            End If

            ' Check block size
            If block.Size > _params.MaxBlockSize Then
                result.AddError($"Block too large: {block.Size} > {_params.MaxBlockSize}")
            End If

            ' Validate Merkle root
            If Not block.ValidateMerkleRoot() Then
                result.AddError("Merkle root does not match transactions.")
            End If

            ' Validate proof-of-work
            If Not block.Header.MeetsTarget() Then
                result.AddError("Block hash does not meet difficulty target.")
            End If

            ' Check for duplicate transaction IDs
            Dim seen As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            For Each txId As String In block.TransactionIds
                If Not seen.Add(txId) Then
                    result.AddError($"Duplicate transaction ID: {txId}")
                End If
            Next

            ' Validate timestamp is not too old (median time past)
            ' This would require chain context in full implementation
        End Sub

        ''' <summary>
        ''' Validates block in the context of the current chain.
        ''' </summary>
        Public Sub ValidateContext(block As Block, chainState As ChainState, result As BlockValidationResult)
            If chainState Is Nothing Then Return

            ' Check previous block exists
            Dim parentIndex As BlockIndex = chainState.GetIndex(block.Header.PreviousBlockHash)
            If parentIndex Is Nothing Then
                result.AddError("Previous block not found in chain.")
                Return
            End If

            ' Validate height
            Dim expectedHeight As Integer = parentIndex.Height + 1
            If block.Header.Height <> expectedHeight Then
                result.AddError($"Invalid height: expected {expectedHeight}, got {block.Header.Height}")
            End If

            ' Validate timestamp is after parent
            If block.Header.Timestamp <= parentIndex.Timestamp Then
                result.AddError("Block timestamp must be after parent block.")
            End If

            ' Validate timestamp is not too far in the future
            Dim currentTime As Long = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            If block.Header.Timestamp > currentTime + _params.MaxTimeDriftSeconds Then
                result.AddError("Block timestamp too far in the future.")
            End If

            ' Validate difficulty
            ' (In full implementation, would check against calculated next difficulty)
        End Sub

        ''' <summary>
        ''' Validates just the block header (lightweight validation for headers-first sync).
        ''' </summary>
        Public Function ValidateHeader(header As BlockHeader) As BlockValidationResult
            Dim result As New BlockValidationResult()

            If header Is Nothing Then
                result.AddError("Header is null.")
                Return result
            End If

            ' Check version
            If header.Version < 1 Then
                result.AddError("Invalid block version.")
            End If

            ' Check proof-of-work
            If Not header.MeetsTarget() Then
                result.AddError("Header does not meet difficulty target.")
            End If

            ' Check timestamp
            Dim currentTime As Long = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            If header.Timestamp > currentTime + _params.MaxTimeDriftSeconds Then
                result.AddError("Header timestamp too far in the future.")
            End If

            Return result
        End Function

    End Class

End Namespace
