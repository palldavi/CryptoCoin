Imports System
Imports System.Collections.Generic

Namespace CryptoCoin.Cryptography

    ''' <summary>
    ''' Implements a Merkle tree for efficient transaction verification.
    ''' Used to compute the Merkle root included in block headers.
    ''' </summary>
    Public Class MerkleTree

        Private ReadOnly _leaves As List(Of Byte())
        Private _root As Byte()
        Private _levels As List(Of List(Of Byte()))

        ''' <summary>
        ''' Gets the Merkle root hash.
        ''' </summary>
        Public ReadOnly Property Root As Byte()
            Get
                If _root Is Nothing Then
                    ComputeRoot()
                End If
                Return _root
            End Get
        End Property

        ''' <summary>
        ''' Gets all levels of the tree (bottom to top).
        ''' </summary>
        Public ReadOnly Property Levels As List(Of List(Of Byte()))
            Get
                If _levels Is Nothing Then
                    ComputeRoot()
                End If
                Return _levels
            End Get
        End Property

        ''' <summary>
        ''' Gets the number of leaves in the tree.
        ''' </summary>
        Public ReadOnly Property LeafCount As Integer
            Get
                Return _leaves.Count
            End Get
        End Property

        ''' <summary>
        ''' Creates a new Merkle tree from a list of data items (typically transaction hashes).
        ''' </summary>
        Public Sub New(leaves As IEnumerable(Of Byte()))
            If leaves Is Nothing Then Throw New ArgumentNullException(NameOf(leaves))
            _leaves = New List(Of Byte())(leaves)
            If _leaves.Count = 0 Then
                Throw New ArgumentException("Merkle tree requires at least one leaf.", NameOf(leaves))
            End If
        End Sub

        ''' <summary>
        ''' Creates a Merkle tree from transaction ID hex strings.
        ''' </summary>
        Public Shared Function FromHexStrings(hexHashes As IEnumerable(Of String)) As MerkleTree
            Dim leaves As New List(Of Byte())()
            For Each hex As String In hexHashes
                leaves.Add(HashUtil.FromHex(hex))
            Next
            Return New MerkleTree(leaves)
        End Function

        ''' <summary>
        ''' Computes the Merkle root by hashing pairs of nodes up the tree.
        ''' </summary>
        Private Sub ComputeRoot()
            _levels = New List(Of List(Of Byte()))()

            Dim currentLevel As New List(Of Byte())(_leaves)
            _levels.Add(New List(Of Byte())(currentLevel))

            While currentLevel.Count > 1
                Dim nextLevel As New List(Of Byte())()

                ' If odd number of nodes, duplicate the last one
                If currentLevel.Count Mod 2 <> 0 Then
                    currentLevel.Add(currentLevel(currentLevel.Count - 1))
                End If

                For i As Integer = 0 To currentLevel.Count - 1 Step 2
                    Dim combined(currentLevel(i).Length + currentLevel(i + 1).Length - 1) As Byte
                    Array.Copy(currentLevel(i), 0, combined, 0, currentLevel(i).Length)
                    Array.Copy(currentLevel(i + 1), 0, combined, currentLevel(i).Length, currentLevel(i + 1).Length)
                    nextLevel.Add(HashUtil.DoubleSha256(combined))
                Next

                _levels.Add(New List(Of Byte())(nextLevel))
                currentLevel = nextLevel
            End While

            _root = currentLevel(0)
        End Sub

        ''' <summary>
        ''' Generates a Merkle proof (authentication path) for a leaf at the given index.
        ''' </summary>
        Public Function GetProof(leafIndex As Integer) As MerkleProof
            If leafIndex < 0 OrElse leafIndex >= _leaves.Count Then
                Throw New ArgumentOutOfRangeException(NameOf(leafIndex))
            End If

            If _levels Is Nothing Then ComputeRoot()

            Dim proof As New List(Of MerkleProofNode)()
            Dim index As Integer = leafIndex

            For level As Integer = 0 To _levels.Count - 2
                Dim currentLevelNodes As List(Of Byte()) = _levels(level)

                ' Duplicate last if odd
                Dim nodes As New List(Of Byte())(currentLevelNodes)
                If nodes.Count Mod 2 <> 0 Then
                    nodes.Add(nodes(nodes.Count - 1))
                End If

                Dim isRight As Boolean = (index Mod 2 = 0)
                Dim siblingIndex As Integer = If(isRight, index + 1, index - 1)

                If siblingIndex < nodes.Count Then
                    proof.Add(New MerkleProofNode(nodes(siblingIndex), Not isRight))
                End If

                index = index \ 2
            Next

            Return New MerkleProof(_leaves(leafIndex), proof)
        End Function

        ''' <summary>
        ''' Verifies a Merkle proof against the expected root.
        ''' </summary>
        Public Shared Function VerifyProof(proof As MerkleProof, expectedRoot As Byte()) As Boolean
            If proof Is Nothing OrElse expectedRoot Is Nothing Then Return False

            Dim current As Byte() = proof.LeafHash

            For Each node As MerkleProofNode In proof.Nodes
                Dim combined As Byte()
                If node.IsLeft Then
                    combined = New Byte(node.Hash.Length + current.Length - 1) {}
                    Array.Copy(node.Hash, 0, combined, 0, node.Hash.Length)
                    Array.Copy(current, 0, combined, node.Hash.Length, current.Length)
                Else
                    combined = New Byte(current.Length + node.Hash.Length - 1) {}
                    Array.Copy(current, 0, combined, 0, current.Length)
                    Array.Copy(node.Hash, 0, combined, current.Length, node.Hash.Length)
                End If
                current = HashUtil.DoubleSha256(combined)
            Next

            Return HashUtil.ConstantTimeEquals(current, expectedRoot)
        End Function

        ''' <summary>
        ''' Computes the Merkle root directly from a list of hashes without building the full tree.
        ''' </summary>
        Public Shared Function ComputeRootDirect(hashes As List(Of Byte())) As Byte()
            If hashes Is Nothing OrElse hashes.Count = 0 Then
                Throw New ArgumentException("At least one hash required.", NameOf(hashes))
            End If

            Dim tree As New MerkleTree(hashes)
            Return tree.Root
        End Function

    End Class

    ''' <summary>
    ''' Represents a Merkle proof (authentication path from leaf to root).
    ''' </summary>
    Public Class MerkleProof

        Public ReadOnly Property LeafHash As Byte()
        Public ReadOnly Property Nodes As List(Of MerkleProofNode)

        Public Sub New(leafHash As Byte(), nodes As List(Of MerkleProofNode))
            Me.LeafHash = leafHash
            Me.Nodes = nodes
        End Sub

    End Class

    ''' <summary>
    ''' A single node in a Merkle proof path.
    ''' </summary>
    Public Class MerkleProofNode

        Public ReadOnly Property Hash As Byte()
        Public ReadOnly Property IsLeft As Boolean

        Public Sub New(hash As Byte(), isLeft As Boolean)
            Me.Hash = hash
            Me.IsLeft = isLeft
        End Sub

    End Class

End Namespace
