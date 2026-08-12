' ============================================================================
' CryptoCoin.Core - MerkleTree.vb
' Full Merkle tree implementation for transaction hash verification.
' ============================================================================

Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Security.Cryptography
Imports System.Text

Namespace CryptoCoin.Core

    ''' <summary>
    ''' Represents a single node in a Merkle tree.
    ''' Each node contains a hash and references to its children and parent.
    ''' </summary>
    Public Class MerkleNode
        ''' <summary>
        ''' Gets or sets the hash value of this node.
        ''' </summary>
        Public Property Hash As String

        ''' <summary>
        ''' Gets or sets the left child node.
        ''' </summary>
        Public Property Left As MerkleNode

        ''' <summary>
        ''' Gets or sets the right child node.
        ''' </summary>
        Public Property Right As MerkleNode

        ''' <summary>
        ''' Gets or sets the parent node.
        ''' </summary>
        Public Property Parent As MerkleNode

        ''' <summary>
        ''' Gets whether this node is a leaf node (has no children).
        ''' </summary>
        Public ReadOnly Property IsLeaf As Boolean
            Get
                Return Left Is Nothing AndAlso Right Is Nothing
            End Get
        End Property

        ''' <summary>
        ''' Gets whether this node is the root node (has no parent).
        ''' </summary>
        Public ReadOnly Property IsRoot As Boolean
            Get
                Return Parent Is Nothing
            End Get
        End Property

        ''' <summary>
        ''' Gets whether this node is a left child of its parent.
        ''' </summary>
        Public ReadOnly Property IsLeftChild As Boolean
            Get
                If Parent Is Nothing Then Return False
                Return Parent.Left Is Me
            End Get
        End Property

        ''' <summary>
        ''' Gets whether this node is a right child of its parent.
        ''' </summary>
        Public ReadOnly Property IsRightChild As Boolean
            Get
                If Parent Is Nothing Then Return False
                Return Parent.Right Is Me
            End Get
        End Property

        ''' <summary>
        ''' Gets the depth of this node in the tree (0 for root).
        ''' </summary>
        Public ReadOnly Property Depth As Integer
            Get
                Dim d = 0
                Dim current = Parent
                While current IsNot Nothing
                    d += 1
                    current = current.Parent
                End While
                Return d
            End Get
        End Property

        ''' <summary>
        ''' Creates a new leaf node with the specified hash.
        ''' </summary>
        ''' <param name="hash">The hash value for this leaf.</param>
        Public Sub New(hash As String)
            Me.Hash = If(hash, String.Empty)
            Me.Left = Nothing
            Me.Right = Nothing
            Me.Parent = Nothing
        End Sub

        ''' <summary>
        ''' Creates a new internal node with the specified children.
        ''' </summary>
        ''' <param name="left">The left child node.</param>
        ''' <param name="right">The right child node.</param>
        Public Sub New(left As MerkleNode, right As MerkleNode)
            Me.Left = left
            Me.Right = right
            If left IsNot Nothing Then left.Parent = Me
            If right IsNot Nothing Then right.Parent = Me
            Me.Hash = ComputeParentHash(
                If(left IsNot Nothing, left.Hash, String.Empty),
                If(right IsNot Nothing, right.Hash, String.Empty))
        End Sub

        ''' <summary>
        ''' Computes the parent hash from two child hashes using double SHA-256.
        ''' </summary>
        Private Shared Function ComputeParentHash(leftHash As String, rightHash As String) As String
            Dim combined = leftHash & rightHash
            Using sha256 As System.Security.Cryptography.SHA256 = System.Security.Cryptography.SHA256.Create()
                Dim bytes = Encoding.UTF8.GetBytes(combined)
                Dim firstHash = sha256.ComputeHash(bytes)
                Dim secondHash = sha256.ComputeHash(firstHash)
                Dim sb As New StringBuilder(secondHash.Length * 2)
                For Each b As Byte In secondHash
                    sb.Append(b.ToString("x2"))
                Next
                Return sb.ToString()
            End Using
        End Function

        ''' <summary>
        ''' Returns a string representation of this node.
        ''' </summary>
        Public Overrides Function ToString() As String
            Dim nodeType = If(IsLeaf, "Leaf", If(IsRoot, "Root", "Internal"))
            Dim shortHash = If(Hash IsNot Nothing AndAlso Hash.Length > 8, Hash.Substring(0, 8) & "...", Hash)
            Return $"MerkleNode({nodeType}, {shortHash})"
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return If(Hash IsNot Nothing, Hash.GetHashCode(), 0)
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            Dim other = TryCast(obj, MerkleNode)
            If other Is Nothing Then Return False
            Return String.Equals(Hash, other.Hash, StringComparison.OrdinalIgnoreCase)
        End Function
    End Class

    ''' <summary>
    ''' Represents a proof that a specific leaf exists in a Merkle tree.
    ''' Contains the path of hashes from the leaf to the root.
    ''' </summary>
    Public Class MerkleProof
        ''' <summary>
        ''' Gets the leaf hash being proven.
        ''' </summary>
        Public ReadOnly Property LeafHash As String

        ''' <summary>
        ''' Gets the Merkle root hash.
        ''' </summary>
        Public ReadOnly Property RootHash As String

        ''' <summary>
        ''' Gets the proof path - list of (hash, isLeft) pairs from leaf to root.
        ''' </summary>
        Public ReadOnly Property Path As IReadOnlyList(Of ProofStep)

        ''' <summary>
        ''' Gets the index of the leaf in the tree.
        ''' </summary>
        Public ReadOnly Property LeafIndex As Integer

        Public Sub New(leafHash As String, rootHash As String, path As List(Of ProofStep), leafIndex As Integer)
            Me.LeafHash = leafHash
            Me.RootHash = rootHash
            Me.Path = If(path, New List(Of ProofStep)()).AsReadOnly()
            Me.LeafIndex = leafIndex
        End Sub

        ''' <summary>
        ''' Verifies this proof against the expected root hash.
        ''' </summary>
        ''' <returns>True if the proof is valid.</returns>
        Public Function Verify() As Boolean
            Return MerkleTree.VerifyProof(Me)
        End Function

        Public Overrides Function ToString() As String
            Dim sb As New StringBuilder()
            sb.AppendFormat("MerkleProof(Leaf={0}, Steps={1})", 
                           If(LeafHash IsNot Nothing AndAlso LeafHash.Length > 8, LeafHash.Substring(0, 8) & "...", LeafHash),
                           Path.Count)
            Return sb.ToString()
        End Function
    End Class

    ''' <summary>
    ''' Represents a single step in a Merkle proof path.
    ''' </summary>
    Public Class ProofStep
        ''' <summary>
        ''' Gets the sibling hash at this level.
        ''' </summary>
        Public ReadOnly Property Hash As String

        ''' <summary>
        ''' Gets whether this sibling is on the left side.
        ''' If true, the sibling hash goes on the left when computing the parent.
        ''' </summary>
        Public ReadOnly Property IsLeft As Boolean

        Public Sub New(hash As String, isLeft As Boolean)
            Me.Hash = hash
            Me.IsLeft = isLeft
        End Sub

        Public Overrides Function ToString() As String
            Dim side = If(IsLeft, "L", "R")
            Dim shortHash = If(Hash IsNot Nothing AndAlso Hash.Length > 8, Hash.Substring(0, 8) & "...", Hash)
            Return $"[{side}] {shortHash}"
        End Function
    End Class

    ''' <summary>
    ''' Full Merkle tree implementation for computing and verifying transaction hashes.
    ''' Supports building trees, generating proofs, and verifying inclusion.
    ''' </summary>
    Public Class MerkleTree
        Private _root As MerkleNode
        Private ReadOnly _leaves As List(Of MerkleNode)
        Private ReadOnly _allNodes As List(Of List(Of MerkleNode))

        ''' <summary>
        ''' Gets the root node of the Merkle tree.
        ''' </summary>
        Public ReadOnly Property Root As MerkleNode
            Get
                Return _root
            End Get
        End Property

        ''' <summary>
        ''' Gets the Merkle root hash.
        ''' </summary>
        Public ReadOnly Property RootHash As String
            Get
                If _root Is Nothing Then Return String.Empty
                Return _root.Hash
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
        ''' Gets the height of the tree (number of levels).
        ''' </summary>
        Public ReadOnly Property TreeHeight As Integer
            Get
                Return _allNodes.Count
            End Get
        End Property

        ''' <summary>
        ''' Gets the leaf hashes.
        ''' </summary>
        Public ReadOnly Property LeafHashes As IReadOnlyList(Of String)
            Get
                Return _leaves.Select(Function(n) n.Hash).ToList().AsReadOnly()
            End Get
        End Property

        ''' <summary>
        ''' Creates a new empty Merkle tree.
        ''' </summary>
        Public Sub New()
            _leaves = New List(Of MerkleNode)()
            _allNodes = New List(Of List(Of MerkleNode))()
        End Sub

        ''' <summary>
        ''' Creates a Merkle tree from a list of transaction hashes.
        ''' </summary>
        ''' <param name="transactionHashes">The transaction hashes to build the tree from.</param>
        Public Sub New(transactionHashes As IEnumerable(Of String))
            _leaves = New List(Of MerkleNode)()
            _allNodes = New List(Of List(Of MerkleNode))()

            If transactionHashes Is Nothing Then
                Throw New ArgumentNullException(NameOf(transactionHashes))
            End If

            BuildTree(transactionHashes.ToList())
        End Sub

        ''' <summary>
        ''' Builds the Merkle tree from a list of hashes.
        ''' If the number of leaves is odd, the last leaf is duplicated.
        ''' </summary>
        ''' <param name="hashes">The leaf hashes.</param>
        Public Sub BuildTree(hashes As List(Of String))
            If hashes Is Nothing OrElse hashes.Count = 0 Then
                Throw New ArgumentException("Cannot build Merkle tree from empty hash list.", NameOf(hashes))
            End If

            _leaves.Clear()
            _allNodes.Clear()

            ' Handle single hash case
            If hashes.Count = 1 Then
                Dim leaf As New MerkleNode(hashes(0))
                _leaves.Add(leaf)
                _allNodes.Add(New List(Of MerkleNode)() From {leaf})
                _root = leaf
                Return
            End If

            ' Create leaf nodes
            For Each hash As String In hashes
                _leaves.Add(New MerkleNode(hash))
            Next

            ' Build tree bottom-up
            Dim currentLevel As New List(Of MerkleNode)(_leaves)
            _allNodes.Add(New List(Of MerkleNode)(currentLevel))

            While currentLevel.Count > 1
                Dim nextLevel As New List(Of MerkleNode)()

                ' If odd number of nodes, duplicate the last one
                If currentLevel.Count Mod 2 <> 0 Then
                    Dim duplicateNode As New MerkleNode(currentLevel(currentLevel.Count - 1).Hash)
                    currentLevel.Add(duplicateNode)
                End If

                ' Pair up nodes and create parents
                For i = 0 To currentLevel.Count - 1 Step 2
                    Dim leftNode = currentLevel(i)
                    Dim rightNode = currentLevel(i + 1)
                    Dim parentNode As New MerkleNode(leftNode, rightNode)
                    nextLevel.Add(parentNode)
                Next

                _allNodes.Add(New List(Of MerkleNode)(nextLevel))
                currentLevel = nextLevel
            End While

            _root = currentLevel(0)
        End Sub

        ''' <summary>
        ''' Computes the Merkle root from a list of transaction hashes without building the full tree.
        ''' More memory-efficient for large transaction sets.
        ''' </summary>
        ''' <param name="hashes">The transaction hashes.</param>
        ''' <returns>The Merkle root hash.</returns>
        Public Shared Function ComputeRoot(hashes As IList(Of String)) As String
            If hashes Is Nothing OrElse hashes.Count = 0 Then
                Return String.Empty
            End If

            If hashes.Count = 1 Then
                Return hashes(0)
            End If

            Dim currentLevel As New List(Of String)(hashes)

            While currentLevel.Count > 1
                Dim nextLevel As New List(Of String)()

                ' Duplicate last if odd
                If currentLevel.Count Mod 2 <> 0 Then
                    currentLevel.Add(currentLevel(currentLevel.Count - 1))
                End If

                For i = 0 To currentLevel.Count - 1 Step 2
                    Dim combined = currentLevel(i) & currentLevel(i + 1)
                    nextLevel.Add(DoubleHash(combined))
                Next

                currentLevel = nextLevel
            End While

            Return currentLevel(0)
        End Function

        ''' <summary>
        ''' Generates a Merkle proof for a specific leaf hash.
        ''' The proof contains the path of sibling hashes needed to reconstruct the root.
        ''' </summary>
        ''' <param name="leafHash">The leaf hash to generate a proof for.</param>
        ''' <returns>A MerkleProof if the leaf exists; otherwise Nothing.</returns>
        Public Function GenerateProof(leafHash As String) As MerkleProof
            If String.IsNullOrEmpty(leafHash) Then
                Throw New ArgumentNullException(NameOf(leafHash))
            End If

            If _root Is Nothing OrElse _leaves.Count = 0 Then
                Return Nothing
            End If

            ' Find the leaf node
            Dim leafIndex = -1
            For i = 0 To _leaves.Count - 1
                If String.Equals(_leaves(i).Hash, leafHash, StringComparison.OrdinalIgnoreCase) Then
                    leafIndex = i
                    Exit For
                End If
            Next

            If leafIndex < 0 Then
                Return Nothing ' Leaf not found
            End If

            ' Build proof path from leaf to root
            Dim path As New List(Of ProofStep)()
            Dim currentNode = _leaves(leafIndex)

            While currentNode.Parent IsNot Nothing
                Dim parent = currentNode.Parent
                If currentNode.IsLeftChild Then
                    ' Sibling is on the right
                    If parent.Right IsNot Nothing Then
                        path.Add(New ProofStep(parent.Right.Hash, False))
                    End If
                Else
                    ' Sibling is on the left
                    If parent.Left IsNot Nothing Then
                        path.Add(New ProofStep(parent.Left.Hash, True))
                    End If
                End If
                currentNode = parent
            End While

            Return New MerkleProof(leafHash, RootHash, path, leafIndex)
        End Function

        ''' <summary>
        ''' Generates a Merkle proof for a leaf at a specific index.
        ''' </summary>
        ''' <param name="leafIndex">The zero-based index of the leaf.</param>
        ''' <returns>A MerkleProof for the specified leaf.</returns>
        Public Function GenerateProofByIndex(leafIndex As Integer) As MerkleProof
            If leafIndex < 0 OrElse leafIndex >= _leaves.Count Then
                Throw New ArgumentOutOfRangeException(NameOf(leafIndex),
                    $"Leaf index must be between 0 and {_leaves.Count - 1}.")
            End If

            Return GenerateProof(_leaves(leafIndex).Hash)
        End Function

        ''' <summary>
        ''' Verifies a Merkle proof by recomputing the root hash from the leaf and proof path.
        ''' </summary>
        ''' <param name="proof">The Merkle proof to verify.</param>
        ''' <returns>True if the proof is valid and produces the expected root hash.</returns>
        Public Shared Function VerifyProof(proof As MerkleProof) As Boolean
            If proof Is Nothing Then Return False
            If String.IsNullOrEmpty(proof.LeafHash) Then Return False
            If String.IsNullOrEmpty(proof.RootHash) Then Return False

            Try
                Dim currentHash = proof.LeafHash

                For Each proofStep As ProofStep In proof.Path
                    If proofStep.IsLeft Then
                        ' Sibling is on the left, so it goes first
                        currentHash = DoubleHash(proofStep.Hash & currentHash)
                    Else
                        ' Sibling is on the right, so current goes first
                        currentHash = DoubleHash(currentHash & proofStep.Hash)
                    End If
                Next

                Return String.Equals(currentHash, proof.RootHash, StringComparison.OrdinalIgnoreCase)
            Catch ex As Exception
                Return False
            End Try
        End Function

        ''' <summary>
        ''' Verifies that a specific hash is included in the tree with the given root.
        ''' </summary>
        ''' <param name="leafHash">The leaf hash to verify.</param>
        ''' <param name="expectedRoot">The expected Merkle root.</param>
        ''' <param name="proofPath">The proof path.</param>
        ''' <returns>True if the leaf is included in the tree.</returns>
        Public Shared Function VerifyInclusion(leafHash As String, expectedRoot As String,
                                               proofPath As IList(Of ProofStep)) As Boolean
            If String.IsNullOrEmpty(leafHash) OrElse String.IsNullOrEmpty(expectedRoot) Then
                Return False
            End If

            Dim proof As New MerkleProof(leafHash, expectedRoot, New List(Of ProofStep)(proofPath), 0)
            Return VerifyProof(proof)
        End Function

        ''' <summary>
        ''' Checks if a specific hash exists as a leaf in this tree.
        ''' </summary>
        ''' <param name="hash">The hash to search for.</param>
        ''' <returns>True if the hash exists as a leaf.</returns>
        Public Function ContainsLeaf(hash As String) As Boolean
            If String.IsNullOrEmpty(hash) Then Return False
            Return _leaves.Any(Function(n) String.Equals(n.Hash, hash, StringComparison.OrdinalIgnoreCase))
        End Function

        ''' <summary>
        ''' Gets the index of a leaf hash in the tree.
        ''' </summary>
        ''' <param name="hash">The hash to find.</param>
        ''' <returns>The zero-based index, or -1 if not found.</returns>
        Public Function GetLeafIndex(hash As String) As Integer
            If String.IsNullOrEmpty(hash) Then Return -1
            For i = 0 To _leaves.Count - 1
                If String.Equals(_leaves(i).Hash, hash, StringComparison.OrdinalIgnoreCase) Then
                    Return i
                End If
            Next
            Return -1
        End Function

        ''' <summary>
        ''' Adds a new leaf to the tree and rebuilds it.
        ''' </summary>
        ''' <param name="hash">The hash to add.</param>
        Public Sub AddLeaf(hash As String)
            If String.IsNullOrEmpty(hash) Then
                Throw New ArgumentNullException(NameOf(hash))
            End If

            Dim allHashes = _leaves.Select(Function(n) n.Hash).ToList()
            allHashes.Add(hash)
            BuildTree(allHashes)
        End Sub

        ''' <summary>
        ''' Returns a visual representation of the Merkle tree structure.
        ''' </summary>
        Public Overrides Function ToString() As String
            If _root Is Nothing Then Return "MerkleTree(empty)"

            Dim sb As New StringBuilder()
            sb.AppendLine($"MerkleTree(Leaves={LeafCount}, Height={TreeHeight})")
            sb.AppendLine($"Root: {If(RootHash.Length > 16, RootHash.Substring(0, 16) & "...", RootHash)}")
            sb.AppendLine()

            ' Print tree level by level
            For level = _allNodes.Count - 1 To 0 Step -1
                Dim nodes = _allNodes(level)
                Dim levelName = If(level = _allNodes.Count - 1, "Root", If(level = 0, "Leaves", $"Level {level}"))
                sb.AppendFormat("  {0}: ", levelName)

                For Each node As MerkleNode In nodes
                    Dim shortHash = If(node.Hash IsNot Nothing AndAlso node.Hash.Length > 8,
                                       node.Hash.Substring(0, 8), node.Hash)
                    sb.AppendFormat("[{0}] ", shortHash)
                Next
                sb.AppendLine()
            Next

            Return sb.ToString()
        End Function

        ''' <summary>
        ''' Returns a detailed tree visualization with connecting lines.
        ''' </summary>
        Public Function ToDetailedString() As String
            If _root Is Nothing Then Return "Empty tree"

            Dim sb As New StringBuilder()
            sb.AppendLine("╔══════════════════════════════════════════╗")
            sb.AppendLine("║           MERKLE TREE                    ║")
            sb.AppendLine("╠══════════════════════════════════════════╣")
            sb.AppendFormat("║ Leaves: {0,-5} Height: {1,-5}            ║", LeafCount, TreeHeight)
            sb.AppendLine()
            sb.AppendFormat("║ Root:   {0}  ║", If(RootHash.Length > 30, RootHash.Substring(0, 30) & "...", RootHash))
            sb.AppendLine()
            sb.AppendLine("╠══════════════════════════════════════════╣")

            PrintNode(sb, _root, "", True)

            sb.AppendLine("╚══════════════════════════════════════════╝")
            Return sb.ToString()
        End Function

        ''' <summary>
        ''' Recursively prints a node and its children.
        ''' </summary>
        Private Sub PrintNode(sb As StringBuilder, node As MerkleNode, prefix As String, isLast As Boolean)
            If node Is Nothing Then Return

            Dim connector = If(isLast, "└── ", "├── ")
            Dim shortHash = If(node.Hash IsNot Nothing AndAlso node.Hash.Length > 12,
                               node.Hash.Substring(0, 12) & "...", node.Hash)
            Dim nodeType = If(node.IsLeaf, " [LEAF]", "")

            sb.AppendFormat("║ {0}{1}{2}{3}", prefix, connector, shortHash, nodeType)
            sb.AppendLine()

            Dim childPrefix = prefix & If(isLast, "    ", "│   ")

            If node.Left IsNot Nothing Then
                PrintNode(sb, node.Left, childPrefix, node.Right Is Nothing)
            End If
            If node.Right IsNot Nothing Then
                PrintNode(sb, node.Right, childPrefix, True)
            End If
        End Sub

        ''' <summary>
        ''' Computes a double SHA-256 hash of the input string.
        ''' </summary>
        Private Shared Function DoubleHash(input As String) As String
            Using sha256 As System.Security.Cryptography.SHA256 = System.Security.Cryptography.SHA256.Create()
                Dim bytes = Encoding.UTF8.GetBytes(input)
                Dim firstHash = sha256.ComputeHash(bytes)
                Dim secondHash = sha256.ComputeHash(firstHash)
                Dim sb As New StringBuilder(secondHash.Length * 2)
                For Each b As Byte In secondHash
                    sb.Append(b.ToString("x2"))
                Next
                Return sb.ToString()
            End Using
        End Function

        ''' <summary>
        ''' Gets all nodes at a specific level of the tree.
        ''' </summary>
        ''' <param name="level">The level (0 = leaves, TreeHeight-1 = root).</param>
        ''' <returns>List of nodes at the specified level.</returns>
        Public Function GetNodesAtLevel(level As Integer) As IReadOnlyList(Of MerkleNode)
            If level < 0 OrElse level >= _allNodes.Count Then
                Return New List(Of MerkleNode)().AsReadOnly()
            End If
            Return _allNodes(level).AsReadOnly()
        End Function

        ''' <summary>
        ''' Validates the internal consistency of the tree by recomputing all hashes.
        ''' </summary>
        ''' <returns>True if all internal hashes are consistent.</returns>
        Public Function ValidateTree() As Boolean
            If _root Is Nothing Then Return _leaves.Count = 0
            Return ValidateNode(_root)
        End Function

        ''' <summary>
        ''' Recursively validates a node and its children.
        ''' </summary>
        Private Function ValidateNode(node As MerkleNode) As Boolean
            If node Is Nothing Then Return True
            If node.IsLeaf Then Return Not String.IsNullOrEmpty(node.Hash)

            ' Validate children first
            If Not ValidateNode(node.Left) Then Return False
            If Not ValidateNode(node.Right) Then Return False

            ' Verify this node's hash matches its children
            Dim leftHash = If(node.Left IsNot Nothing, node.Left.Hash, String.Empty)
            Dim rightHash = If(node.Right IsNot Nothing, node.Right.Hash, String.Empty)
            Dim expectedHash = DoubleHash(leftHash & rightHash)

            Return String.Equals(node.Hash, expectedHash, StringComparison.OrdinalIgnoreCase)
        End Function
    End Class

End Namespace