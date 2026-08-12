Imports CryptoCoin.Core
Imports CryptoCoin.Transactions
Imports CryptoCoin.Mining

Namespace CryptoCoin.Node

    Module Program

        Sub Main(args As String())
            Console.WriteLine("CryptoCoin Node v1.0")
            Console.WriteLine("====================")

            Dim config As New NodeConfig()
            config.Network = "mainnet"
            config.RpcPort = 8332
            config.MinerAddress = ""

            ' Parse command line args
            For i As Integer = 0 To args.Length - 1
                Dim arg As String = args(i)
                If arg = "--testnet" Then
                    config.Network = "testnet"
                ElseIf arg = "--regtest" Then
                    config.Network = "regtest"
                ElseIf arg = "--rpcport" AndAlso i + 1 < args.Length Then
                    config.RpcPort = Integer.Parse(args(i + 1))
                    i += 1
                ElseIf arg = "--explorer" AndAlso i + 1 < args.Length Then
                    config.ExplorerPort = Integer.Parse(args(i + 1))
                    i += 1
                ElseIf arg = "--datadir" AndAlso i + 1 < args.Length Then
                    config.DataDir = args(i + 1)
                    i += 1
                ElseIf arg = "--no-persist" Then
                    config.Persist = False
                ElseIf arg = "--wcf" AndAlso i + 1 < args.Length Then
                    config.WcfPort = Integer.Parse(args(i + 1))
                    i += 1
                ElseIf arg = "--wcfkey" AndAlso i + 1 < args.Length Then
                    config.WcfApiKey = args(i + 1)
                    i += 1
                ElseIf arg = "--mine" AndAlso i + 1 < args.Length Then
                    config.MinerAddress = args(i + 1)
                    i += 1
                End If
            Next

            Dim service As New NodeService(config)
            service.Start()

            Console.WriteLine("Node running. Press Q to quit.")
            While True
                Dim key As ConsoleKeyInfo = Console.ReadKey(True)
                If key.Key = ConsoleKey.Q Then Exit While
            End While

            service.Stop()
            Console.WriteLine("Node stopped.")
        End Sub

    End Module

End Namespace
