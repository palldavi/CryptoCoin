Imports System
Imports System.Collections.Generic
Imports System.Text

Namespace CryptoCoin.Cryptography

    ''' <summary>
    ''' BIP39 mnemonic phrase generation and validation for CryptoCoin wallets.
    ''' Generates 12-24 word recovery phrases from entropy.
    ''' </summary>
    Public Class Mnemonic

        Private Shared ReadOnly WordList As String() = GetEnglishWordList()

        ''' <summary>
        ''' The mnemonic phrase as a string of space-separated words.
        ''' </summary>
        Public ReadOnly Property Phrase As String

        ''' <summary>
        ''' The individual words of the mnemonic.
        ''' </summary>
        Public ReadOnly Property Words As String()

        ''' <summary>
        ''' The entropy bytes used to generate this mnemonic.
        ''' </summary>
        Public ReadOnly Property Entropy As Byte()

        ''' <summary>
        ''' Generates a new random mnemonic with the specified word count.
        ''' </summary>
        ''' <param name="wordCount">Number of words (12, 15, 18, 21, or 24).</param>
        Public Sub New(Optional wordCount As Integer = 12)
            Dim entropyBits As Integer
            Select Case wordCount
                Case 12 : entropyBits = 128
                Case 15 : entropyBits = 160
                Case 18 : entropyBits = 192
                Case 21 : entropyBits = 224
                Case 24 : entropyBits = 256
                Case Else
                    Throw New ArgumentException("Word count must be 12, 15, 18, 21, or 24.", NameOf(wordCount))
            End Select

            _Entropy = SecureRandom.GetBytes(entropyBits \ 8)
            _Words = EntropyToWords(_Entropy)
            _Phrase = String.Join(" ", _Words)
        End Sub

        ''' <summary>
        ''' Creates a mnemonic from an existing phrase.
        ''' </summary>
        Public Sub New(phrase As String)
            If String.IsNullOrWhiteSpace(phrase) Then Throw New ArgumentNullException(NameOf(phrase))

            _Phrase = phrase.Trim().ToLowerInvariant()
            _Words = _Phrase.Split(" "c)

            If Not IsValidWordCount(_Words.Length) Then
                Throw New ArgumentException("Invalid mnemonic word count.", NameOf(phrase))
            End If

            ' Validate words exist in wordlist
            For Each word As String In _Words
                If Array.IndexOf(WordList, word) < 0 Then
                    Throw New ArgumentException($"Invalid mnemonic word: '{word}'", NameOf(phrase))
                End If
            Next

            _Entropy = WordsToEntropy(_Words)
        End Sub

        ''' <summary>
        ''' Creates a mnemonic from entropy bytes.
        ''' </summary>
        Public Sub New(entropy As Byte())
            If entropy Is Nothing Then Throw New ArgumentNullException(NameOf(entropy))
            If Not IsValidEntropyLength(entropy.Length) Then
                Throw New ArgumentException("Invalid entropy length.", NameOf(entropy))
            End If

            _Entropy = CType(entropy.Clone(), Byte())
            _Words = EntropyToWords(_Entropy)
            _Phrase = String.Join(" ", _Words)
        End Sub

        ''' <summary>
        ''' Derives the BIP39 seed from this mnemonic.
        ''' </summary>
        Public Function ToSeed(Optional passphrase As String = "") As Byte()
            Return Pbkdf2Deriver.DeriveBip39Seed(_Phrase, passphrase)
        End Function

        ''' <summary>
        ''' Validates a mnemonic phrase including checksum verification.
        ''' </summary>
        Public Shared Function IsValid(phrase As String) As Boolean
            If String.IsNullOrWhiteSpace(phrase) Then Return False

            Try
                Dim m As New Mnemonic(phrase)
                Return VerifyChecksum(m.Words)
            Catch
                Return False
            End Try
        End Function

        Private Shared Function EntropyToWords(entropy As Byte()) As String()
            ' Compute checksum (first CS bits of SHA-256)
            Dim hash As Byte() = HashUtil.Sha256(entropy)
            Dim checksumBits As Integer = entropy.Length \ 4 ' CS = ENT / 32

            ' Convert entropy + checksum to bit string
            Dim bits As New StringBuilder()
            For Each b As Byte In entropy
                bits.Append(Convert.ToString(b, 2).PadLeft(8, "0"c))
            Next
            For i As Integer = 0 To checksumBits - 1
                Dim bitIndex As Integer = 7 - (i Mod 8)
                Dim byteIndex As Integer = i \ 8
                Dim bit As Integer = (hash(byteIndex) >> bitIndex) And 1
                bits.Append(bit.ToString())
            Next

            ' Split into 11-bit groups
            Dim totalBits As Integer = entropy.Length * 8 + checksumBits
            Dim wordCount As Integer = totalBits \ 11
            Dim words(wordCount - 1) As String

            For i As Integer = 0 To wordCount - 1
                Dim segment As String = bits.ToString(i * 11, 11)
                Dim index As Integer = Convert.ToInt32(segment, 2)
                words(i) = WordList(index)
            Next

            Return words
        End Function

        Private Shared Function WordsToEntropy(words As String()) As Byte()
            ' Convert words to bit string
            Dim bits As New StringBuilder()
            For Each word As String In words
                Dim index As Integer = Array.IndexOf(WordList, word)
                bits.Append(Convert.ToString(index, 2).PadLeft(11, "0"c))
            Next

            ' Split into entropy and checksum
            Dim totalBits As Integer = words.Length * 11
            Dim checksumBits As Integer = totalBits \ 33
            Dim entropyBits As Integer = totalBits - checksumBits

            Dim entropyBytes(entropyBits \ 8 - 1) As Byte
            For i As Integer = 0 To entropyBytes.Length - 1
                Dim segment As String = bits.ToString(i * 8, 8)
                entropyBytes(i) = Convert.ToByte(segment, 2)
            Next

            Return entropyBytes
        End Function

        Private Shared Function VerifyChecksum(words As String()) As Boolean
            Try
                Dim entropy As Byte() = WordsToEntropy(words)
                Dim expectedWords As String() = EntropyToWords(entropy)
                Return String.Join(" ", words) = String.Join(" ", expectedWords)
            Catch
                Return False
            End Try
        End Function

        Private Shared Function IsValidWordCount(count As Integer) As Boolean
            Return count = 12 OrElse count = 15 OrElse count = 18 OrElse count = 21 OrElse count = 24
        End Function

        Private Shared Function IsValidEntropyLength(length As Integer) As Boolean
            Return length = 16 OrElse length = 20 OrElse length = 24 OrElse length = 28 OrElse length = 32
        End Function

        ''' <summary>
        ''' Returns a subset of the BIP39 English word list (2048 words).
        ''' In production, this would load from a resource file.
        ''' </summary>
        Private Shared Function GetEnglishWordList() As String()
            ' BIP39 English word list (first 256 words shown, full list would be 2048)
            ' In a real implementation, this would be loaded from an embedded resource
            Dim words As New List(Of String)()
            words.AddRange({"abandon", "ability", "able", "about", "above", "absent", "absorb", "abstract",
                           "absurd", "abuse", "access", "accident", "account", "accuse", "achieve", "acid",
                           "acoustic", "acquire", "across", "act", "action", "actor", "actress", "actual",
                           "adapt", "add", "addict", "address", "adjust", "admit", "adult", "advance",
                           "advice", "aerobic", "affair", "afford", "afraid", "again", "age", "agent",
                           "agree", "ahead", "aim", "air", "airport", "aisle", "alarm", "album",
                           "alcohol", "alert", "alien", "all", "alley", "allow", "almost", "alone",
                           "alpha", "already", "also", "alter", "always", "amateur", "amazing", "among",
                           "amount", "amused", "analyst", "anchor", "ancient", "anger", "angle", "angry",
                           "animal", "ankle", "announce", "annual", "another", "answer", "antenna", "antique",
                           "anxiety", "any", "apart", "apology", "appear", "apple", "approve", "april",
                           "arch", "arctic", "area", "arena", "argue", "arm", "armed", "armor",
                           "army", "around", "arrange", "arrest", "arrive", "arrow", "art", "artefact",
                           "artist", "artwork", "ask", "aspect", "assault", "asset", "assist", "assume",
                           "asthma", "athlete", "atom", "attack", "attend", "attitude", "attract", "auction",
                           "audit", "august", "aunt", "author", "auto", "autumn", "average", "avocado",
                           "avoid", "awake", "aware", "awesome", "awful", "awkward", "axis", "baby",
                           "bachelor", "bacon", "badge", "bag", "balance", "balcony", "ball", "bamboo",
                           "banana", "banner", "bar", "barely", "bargain", "barrel", "base", "basic",
                           "basket", "battle", "beach", "bean", "beauty", "because", "become", "beef",
                           "before", "begin", "behave", "behind", "believe", "below", "belt", "bench",
                           "benefit", "best", "betray", "better", "between", "beyond", "bicycle", "bid",
                           "bike", "bind", "biology", "bird", "birth", "bitter", "black", "blade",
                           "blame", "blanket", "blast", "bleak", "bless", "blind", "blood", "blossom",
                           "blow", "blue", "blur", "blush", "board", "boat", "body", "boil",
                           "bomb", "bone", "bonus", "book", "boost", "border", "boring", "borrow",
                           "boss", "bottom", "bounce", "box", "boy", "bracket", "brain", "brand",
                           "brass", "brave", "bread", "breeze", "brick", "bridge", "brief", "bright",
                           "bring", "brisk", "broccoli", "broken", "bronze", "broom", "brother", "brown",
                           "brush", "bubble", "buddy", "budget", "buffalo", "build", "bulb", "bulk",
                           "bullet", "bundle", "bunny", "burden", "burger", "burst", "bus", "business",
                           "busy", "butter", "buyer", "buzz", "cabbage", "cabin", "cable", "cactus"})

            ' Pad to 2048 words with generated words for demo purposes
            Dim baseWords As String() = {"cage", "cake", "call", "calm", "camera", "camp", "can", "canal",
                                         "cancel", "candy", "cannon", "canoe", "canvas", "canyon", "capable", "capital",
                                         "captain", "car", "carbon", "card", "cargo", "carpet", "carry", "cart",
                                         "case", "cash", "casino", "castle", "casual", "cat", "catalog", "catch"}
            While words.Count < 2048
                For Each w As String In baseWords
                    If words.Count >= 2048 Then Exit For
                    Dim suffix As String = (words.Count \ 32).ToString()
                    words.Add(w & suffix)
                Next
            End While

            Return words.ToArray()
        End Function

        Public Overrides Function ToString() As String
            Return _Phrase
        End Function

    End Class

End Namespace
