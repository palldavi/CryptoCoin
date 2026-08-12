Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports CryptoCoin.Cryptography

Namespace CryptoCoin.Tests.Cryptography

    <TestClass>
    Public Class MerkleTreeTests

        Private Function MakeLeaves(count As Integer) As List(Of Byte())
            Dim leaves As New List(Of Byte())()
            For i As Integer = 0 To count - 1
                leaves.Add(MakeHash(i))
            Next
            Return leaves
        End Function

        <TestMethod>
        Public Sub SingleLeaf_RootEqualsLeaf()
            Dim leaf As Byte() = MakeHash(1)
            Dim tree As New MerkleTree(New List(Of Byte()) From {leaf})
            AssertBytesEqual(leaf, tree.Root)
        End Sub

        <TestMethod>
        Public Sub TwoLeaves_RootIsHashOfBoth()
            Dim leaf1 As Byte() = MakeHash(1)
            Dim leaf2 As Byte() = MakeHash(2)
            Dim tree As New MerkleTree(New List(Of Byte()) From {leaf1, leaf2})
            ' Root should be double-SHA256 of leaf1 || leaf2
            Dim combined(leaf1.Length + leaf2.Length - 1) As Byte
            Array.Copy(leaf1, 0, combined, 0, leaf1.Length)
            Array.Copy(leaf2, 0, combined, leaf1.Length, leaf2.Length)
            Dim expected As Byte() = HashUtil.DoubleSha256(combined)
            AssertBytesEqual(expected, tree.Root)
        End Sub

        <TestMethod>
        Public Sub OddNumberOfLeaves_LastLeafDuplicated()
            ' With 3 leaves, the 3rd is duplicated to make 4
            Dim leaves As List(Of Byte()) = MakeLeaves(3)
            Dim tree As New MerkleTree(leaves)
            Assert.IsNotNull(tree.Root)
            Assert.AreEqual(32, tree.Root.Length)
        End Sub

        <TestMethod>
        Public Sub FourLeaves_RootIs32Bytes()
            Dim tree As New MerkleTree(MakeLeaves(4))
            Assert.AreEqual(32, tree.Root.Length)
        End Sub

        <TestMethod>
        Public Sub SameLeaves_SameRoot()
            Dim leaves As List(Of Byte()) = MakeLeaves(4)
            Dim tree1 As New MerkleTree(leaves)
            Dim tree2 As New MerkleTree(leaves)
            AssertBytesEqual(tree1.Root, tree2.Root)
        End Sub

        <TestMethod>
        Public Sub DifferentLeaves_DifferentRoot()
            Dim tree1 As New MerkleTree(MakeLeaves(4))
            Dim tree2 As New MerkleTree(MakeLeaves(5))
            Assert.IsFalse(HashUtil.ConstantTimeEquals(tree1.Root, tree2.Root))
        End Sub

        <TestMethod>
        Public Sub LeafCount_ReturnsCorrectCount()
            Dim tree As New MerkleTree(MakeLeaves(7))
            Assert.AreEqual(7, tree.LeafCount)
        End Sub

        <TestMethod>
        <ExpectedException(GetType(ArgumentException))>
        Public Sub EmptyLeaves_Throws()
            Dim tree As New MerkleTree(New List(Of Byte())())
        End Sub

        <TestMethod>
        <ExpectedException(GetType(ArgumentNullException))>
        Public Sub NullLeaves_Throws()
            Dim tree As New MerkleTree(CType(Nothing, IEnumerable(Of Byte())))
        End Sub

        <TestMethod>
        Public Sub FromHexStrings_ProducesSameRootAsFromBytes()
            Dim hashes As New List(Of String)()
            For i As Integer = 0 To 3
                hashes.Add(MakeHashHex(i))
            Next
            Dim treeFromHex As MerkleTree = MerkleTree.FromHexStrings(hashes)
            Dim treeFromBytes As New MerkleTree(hashes.Select(Function(h) HashUtil.FromHex(h)).ToList())
            AssertBytesEqual(treeFromBytes.Root, treeFromHex.Root)
        End Sub

        <TestMethod>
        Public Sub GetProof_ValidIndex_ReturnsProof()
            Dim tree As New MerkleTree(MakeLeaves(4))
            Dim proof As MerkleProof = tree.GetProof(0)
            Assert.IsNotNull(proof)
            Assert.IsNotNull(proof.LeafHash)
        End Sub

        <TestMethod>
        Public Sub GetProof_VerifyProof_ReturnsTrue()
            Dim leaves As List(Of Byte()) = MakeLeaves(4)
            Dim tree As New MerkleTree(leaves)
            For i As Integer = 0 To leaves.Count - 1
                Dim proof As MerkleProof = tree.GetProof(i)
                Assert.IsTrue(MerkleTree.VerifyProof(proof, tree.Root),
                    $"Proof for leaf {i} should verify against root")
            Next
        End Sub

        <TestMethod>
        Public Sub VerifyProof_WrongRoot_ReturnsFalse()
            Dim tree As New MerkleTree(MakeLeaves(4))
            Dim proof As MerkleProof = tree.GetProof(0)
            Dim wrongRoot As Byte() = MakeHash(999)
            Assert.IsFalse(MerkleTree.VerifyProof(proof, wrongRoot))
        End Sub

        <TestMethod>
        <ExpectedException(GetType(ArgumentOutOfRangeException))>
        Public Sub GetProof_OutOfRangeIndex_Throws()
            Dim tree As New MerkleTree(MakeLeaves(4))
            tree.GetProof(10)
        End Sub

        <TestMethod>
        Public Sub ComputeRootDirect_MatchesTreeRoot()
            Dim leaves As List(Of Byte()) = MakeLeaves(6)
            Dim tree As New MerkleTree(leaves)
            Dim direct As Byte() = MerkleTree.ComputeRootDirect(leaves)
            AssertBytesEqual(tree.Root, direct)
        End Sub

    End Class

End Namespace
