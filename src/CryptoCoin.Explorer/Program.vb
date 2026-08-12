Imports CryptoCoin.Core
Imports CryptoCoin.Transactions

Namespace CryptoCoin.Explorer

    Module Program

        Sub Main(args As String())
            Console.WriteLine("CryptoCoin Block Explorer v1.0")
            Console.WriteLine("==============================")

            Dim port As Integer = 8080
            Dim network As String = "mainnet"
            Dim nodeUrl As String = Nothing   ' e.g. http://localhost:8332/

            ' Parse command line args
            For i As Integer = 0 To args.Length - 1
                Dim arg As String = args(i)
                If arg = "--port" AndAlso i + 1 < args.Length Then
                    port = Integer.Parse(args(i + 1))
                    i += 1
                ElseIf arg = "--testnet" Then
                    network = "testnet"
                ElseIf arg = "--regtest" Then
                    network = "regtest"
                ElseIf arg = "--nodeurl" AndAlso i + 1 < args.Length Then
                    nodeUrl = args(i + 1).Replace("localhost", "127.0.0.1")
                    i += 1
                End If
            Next

            ' Chain parameters (used for coin name/symbol in network responses)
            Dim params As ChainParameters
            Select Case network
                Case "testnet" : params = ChainParameters.Testnet()
                Case "regtest" : params = ChainParameters.Regtest()
                Case Else      : params = ChainParameters.Mainnet()
            End Select

            ' Create a blank local chain (used as fallback when no node is connected)
            Dim blockchain As New Blockchain(params)
            Dim mempool As New Mempool()

            ' Optional proxy to a live node
            Dim proxy As NodeProxy = Nothing
            If Not String.IsNullOrEmpty(nodeUrl) Then
                proxy = New NodeProxy(nodeUrl)
                Console.WriteLine($"Connecting to node at {nodeUrl}")
            End If

            ' Start explorer server
            Dim server As New ExplorerServer(port, blockchain, mempool, proxy, params)
            server.Start()

            If proxy IsNot Nothing Then
                Console.WriteLine($"Explorer running on http://localhost:{port}/ (proxying node at {nodeUrl})")
            Else
                Console.WriteLine($"Explorer running on http://localhost:{port}/ (standalone mode)")
            End If
            Console.WriteLine("Press Q to quit.")

            While True
                Dim key As ConsoleKeyInfo = Console.ReadKey(True)
                If key.Key = ConsoleKey.Q Then Exit While
            End While

            server.Stop()
            Console.WriteLine("Explorer stopped.")
        End Sub

    End Module

End Namespace
