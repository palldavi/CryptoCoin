Namespace CryptoCoin.Networking

    ''' <summary>
    ''' Ping/Pong message used for connection keepalive and latency measurement.
    ''' A Ping message contains a random nonce; the receiver must respond with
    ''' a Pong message containing the same nonce.
    ''' </summary>
    Public Class PingPongMessage

        ''' <summary>
        ''' The random nonce value. The pong response must echo this value.
        ''' </summary>
        Public Property Nonce As ULong

        ''' <summary>
        ''' Whether this is a Ping (True) or Pong (False) message.
        ''' </summary>
        Public Property IsPing As Boolean

        ''' <summary>
        ''' The timestamp when this ping was created (for local latency tracking).
        ''' </summary>
        Public Property CreatedAt As DateTime

        ''' <summary>
        ''' Creates a new PingPongMessage with a random nonce.
        ''' </summary>
        Public Sub New()
            Dim rng As New Random()
            Dim buffer(7) As Byte
            rng.NextBytes(buffer)
            Nonce = BitConverter.ToUInt64(buffer, 0)
            IsPing = True
            CreatedAt = DateTime.UtcNow
        End Sub

        ''' <summary>
        ''' Creates a PingPongMessage with the specified nonce.
        ''' </summary>
        ''' <param name="nonce">The nonce value.</param>
        ''' <param name="isPing">True for Ping, False for Pong.</param>
        Public Sub New(nonce As ULong, isPing As Boolean)
            Me.Nonce = nonce
            Me.IsPing = isPing
            Me.CreatedAt = DateTime.UtcNow
        End Sub

        ''' <summary>
        ''' Creates a Ping message with a new random nonce.
        ''' </summary>
        ''' <returns>A new Ping message.</returns>
        Public Shared Function CreatePing() As PingPongMessage
            Dim msg As New PingPongMessage()
            msg.IsPing = True
            Return msg
        End Function

        ''' <summary>
        ''' Creates a Pong response for the given Ping message.
        ''' </summary>
        ''' <param name="ping">The Ping message to respond to.</param>
        ''' <returns>A Pong message with the same nonce.</returns>
        Public Shared Function CreatePong(ping As PingPongMessage) As PingPongMessage
            If ping Is Nothing Then
                Throw New ArgumentNullException(NameOf(ping))
            End If
            Return New PingPongMessage(ping.Nonce, False)
        End Function

        ''' <summary>
        ''' Creates a Pong response for the given nonce value.
        ''' </summary>
        ''' <param name="nonce">The nonce from the received Ping.</param>
        ''' <returns>A Pong message with the specified nonce.</returns>
        Public Shared Function CreatePongFromNonce(nonce As ULong) As PingPongMessage
            Return New PingPongMessage(nonce, False)
        End Function

        ''' <summary>
        ''' Verifies that a Pong response matches this Ping's nonce.
        ''' </summary>
        ''' <param name="pong">The Pong message to verify.</param>
        ''' <returns>True if the nonces match.</returns>
        Public Function VerifyPong(pong As PingPongMessage) As Boolean
            If pong Is Nothing Then Return False
            Return pong.Nonce = Me.Nonce
        End Function

        ''' <summary>
        ''' Calculates the round-trip latency between this Ping and the current time.
        ''' </summary>
        ''' <returns>The elapsed time since this Ping was created.</returns>
        Public Function GetElapsedTime() As TimeSpan
            Return DateTime.UtcNow - CreatedAt
        End Function

        ''' <summary>
        ''' Serializes the ping/pong message to a byte array payload.
        ''' </summary>
        ''' <returns>The serialized payload (8 bytes containing the nonce).</returns>
        Public Function Serialize() As Byte()
            Return BitConverter.GetBytes(Nonce)
        End Function

        ''' <summary>
        ''' Deserializes a ping/pong message from a byte array payload.
        ''' </summary>
        ''' <param name="data">The payload bytes (8 bytes for the nonce).</param>
        ''' <param name="isPing">Whether this is a Ping or Pong message.</param>
        ''' <returns>A populated PingPongMessage instance.</returns>
        Public Shared Function Deserialize(data As Byte(), isPing As Boolean) As PingPongMessage
            If data Is Nothing OrElse data.Length < 8 Then
                Throw New ArgumentException("Ping/Pong message payload must be at least 8 bytes.")
            End If

            Dim msg As New PingPongMessage()
            msg.Nonce = BitConverter.ToUInt64(data, 0)
            msg.IsPing = isPing
            Return msg
        End Function

        ''' <summary>
        ''' Wraps this message in a NetworkMessage for transmission.
        ''' Uses "ping" or "pong" command based on the IsPing property.
        ''' </summary>
        ''' <returns>A NetworkMessage with the appropriate command.</returns>
        Public Function ToNetworkMessage() As NetworkMessage
            Dim command As String = If(IsPing, NetworkCommands.Ping, NetworkCommands.Pong)
            Return New NetworkMessage(command, Serialize())
        End Function

        ''' <summary>
        ''' Checks whether the nonce is non-zero (valid for latency measurement).
        ''' </summary>
        ''' <returns>True if the nonce is non-zero.</returns>
        Public Function HasValidNonce() As Boolean
            Return Nonce <> 0UL
        End Function

        Public Overrides Function ToString() As String
            Dim msgType As String = If(IsPing, "Ping", "Pong")
            Return $"PingPongMessage({msgType}, Nonce={Nonce})"
        End Function

    End Class

End Namespace
