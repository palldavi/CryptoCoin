# CryptoCoin

A full-stack cryptocurrency implementation in **VB.NET / .NET Framework 4.8**. CryptoCoin is an imaginary coin (ticker: **CRC**) built to demonstrate how a real cryptocurrency works end-to-end — from elliptic curve cryptography and proof-of-work mining through to a wallet CLI, block explorer API, smart contract VM, WCF services, and SQLite persistence.

Modernize this solution to .NET 10 with AWS Transform:
[Modernize .NET with the AWS Transform conversational AI assistant for Visual Studio](https://aws.amazon.com/blogs/dotnet/introducing-the-aws-transform-conversational-ai-assistant-for-visual-studio/)

> **This is an educational/demonstration project.** CRC has no real-world value.

---

## Requirements

- Visual Studio 2026 or 2022 (Community edition is fine)
- .NET Framework 4.8 (pre-installed on Windows 10/11)
- NuGet packages restore automatically on build — no manual install needed

**NuGet packages used:**

| Project | Packages |
|---------|----------|
| `CryptoCoin.Node` | log4net 2.0.15, Castle.Windsor 5.1.2, EnterpriseLibrary.Logging 6.0.1304 |
| `CryptoCoin.Core` | Newtonsoft.Json 13.0.3 |
| `CryptoCoin.Sdk` | Newtonsoft.Json 13.0.3 |
| `CryptoCoin.Persistence` | System.Data.SQLite.Core 1.0.118.0 |
| `CryptoCoin.Web.BlockExplorer` | Bootstrap 5.2.3, jQuery 3.7.0, Newtonsoft.Json 13.0.3 |

---

## Building

Open `CryptoCoin.sln` in Visual Studio and press **Ctrl+Shift+B** to build all projects.

Or from the command line using the VS MSBuild:

```
"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" CryptoCoin.sln /p:Configuration=Debug
```

All 16 projects should build with 0 errors.

---

## Running the Demo

The quickest way to see CryptoCoin in action is the **Demo** console app.

**In Visual Studio:**
1. Right-click `CryptoCoin.Demo` in Solution Explorer → Set as Startup Project
2. Press **F5**

**From the command line:**
```
src\CryptoCoin.Demo\bin\Debug\CryptoCoin.Demo.exe
```

**What the demo shows:**

```
=== CryptoCoin Demo ===

1. Generating 12-word mnemonic phrase...
   Phrase: carpet canvas case cash can call cannon casino axis about card cash

2. Deriving seed from mnemonic...
   Seed (first 32 bytes): 220cb8b99475ba77...

3. Generating master HD key...
   Master key serialized: xprv9s21ZrQH143K4...

4. Deriving address at m/44'/999'/0'/0/0...
   Address: CJTZijYXJ4n3XisgX2jVioSWtcThfL31PC
   Public Key: 02772152aed12ae90f07fe70...

5. Signing a message...
   Message: Hello CryptoCoin!
   Signature R: 7e7129269450dc3f...
   Signature S: 71fe6bac4845bd5a...

6. Verifying signature...
   Valid: True

7. Building Merkle tree from 4 transaction hashes...
   Merkle Root: ef58d462072a7f0a...

8. Base58Check encoding demo...
   Encoded: 2KsWBN6srbqoTtjT...
   Roundtrip OK: True

9. Generating 5 addresses from HD wallet...
   m/44'/999'/0'/0/0 -> CJTZijYXJ4n3XisgX2jVioSWtcThfL31PC
   m/44'/999'/0'/0/1 -> CXr7snrSRD4MJNhtvBedYTmmVFWRiaTJAK
   ...
```

---

## Running the Full Node

The node starts a blockchain, mempool, optional miner, JSON-RPC server, Explorer API, WCF services, and structured logging — all in one process.

```
src\CryptoCoin.Node\bin\Debug\CryptoCoin.Node.exe [options]
```

**Options:**

| Flag | Description |
|------|-------------|
| `--testnet` | Use testnet parameters (faster blocks) |
| `--regtest` | Use regtest parameters (instant blocks, for local dev) |
| `--rpcport 8332` | RPC server port (default: 8332) |
| `--mine <address>` | Start mining and send rewards to this address |
| `--explorer <port>` | Start the Explorer REST API on the given port (e.g. 8080) |
| `--wcf <port>` | Start WCF services on the given port (e.g. 8090) |
| `--wcfkey <key>` | API key for WCF service authentication (default: `cryptocoin-demo-key`) |
| `--datadir <path>` | Data directory for logs and SQLite database (default: `data`) |
| `--persist` | Enable SQLite blockchain persistence (chain survives restarts) |
| `--no-persist` | Disable persistence — chain resets on every restart (default) |

**Example — full local regtest setup with all features:**
```
CryptoCoin.Node.exe --regtest --mine CJTZijYXJ4n3XisgX2jVioSWtcThfL31PC --explorer 8080 --wcf 8090
```

**Example — with persistence enabled:**
```
CryptoCoin.Node.exe --regtest --mine CJTZijYXJ4n3XisgX2jVioSWtcThfL31PC --explorer 8080 --persist
```

**Example — query the node via RPC (PowerShell):**
```powershell
Invoke-RestMethod -Method POST -Uri http://localhost:8332/ `
  -ContentType "application/json" `
  -Body '{"method":"getblockcount","params":[],"id":1}'
```

**Available RPC methods:**

| Method | Description |
|--------|-------------|
| `getblockcount` | Current chain height |
| `getbestblockhash` | Hash of the tip block |
| `getblock` | Block details by hash |
| `getblockbyheight` | Block details by height |
| `getmempoolinfo` | Mempool size, bytes, fees |
| `getmininginfo` | Hash rate, blocks mined, difficulty |
| `getdifficulty` | Current difficulty ratio |
| `startmining` | Start mining to an address |
| `stopmining` | Stop the miner |

**Logging:**

The node writes structured logs to `data/logs/` using two frameworks simultaneously:
- `node.log` — log4net rolling file (max 10 MB, 5 backups)
- `node-entlib.log` — Enterprise Library flat file

---

## Running the Block Explorer API

The Explorer REST API is built into the node — start it with the `--explorer <port>` flag (see above). It shares the node's live blockchain directly with no inter-process communication.

**Explorer API Endpoints** (when `--explorer <port>` is used):

| Endpoint | Description |
|----------|-------------|
| `GET /api/status` | Node status (height, best hash, mempool) |
| `GET /api/network` | Network info (difficulty, hash rate, coin name) |
| `GET /api/blocks/latest` | Last 10 blocks |
| `GET /api/blocks/height/{n}` | Block at height n |
| `GET /api/block/{hash}` | Block by hash |
| `GET /api/tx/{txid}` | Transaction by ID (checks mempool + chain) |
| `GET /api/mempool` | Mempool contents and top transactions |
| `GET /api/address/{address}` | Address info and transaction count |

**Example:**
```
curl http://localhost:8080/api/status
curl http://localhost:8080/api/blocks/height/0
curl http://localhost:8080/api/network
```

> **Note:** `CryptoCoin.Explorer.exe` can also be run as a standalone process with `--port 8080`. This is useful for testing the API independently, but it will only show the genesis block without a connected node.

---

## Running the WCF Services

The node exposes two WCF services when started with `--wcf <port>`:

| Service | URL | Description |
|---------|-----|-------------|
| `IBlockchainService` | `http://localhost:8090/cryptocoin/blockchain` | Block, network, and mempool queries |
| `IWalletService` | `http://localhost:8090/cryptocoin/wallet` | Wallet creation and balance queries |

Both services use `BasicHttpBinding` and require an API key in a custom SOAP header.

**Security:** Every call must include an `ApiKey` header in the `http://cryptocoin.services/2024/security` namespace. The default key is `cryptocoin-demo-key` — change it with `--wcfkey <key>`.

The web project's `WcfBlockchainClient.cs` shows how to create a typed channel with the API key behavior attached. The key and endpoint URL are configured in `Web.config`:

```xml
<appSettings>
  <add key="WcfBlockchainServiceUrl" value="http://localhost:8090/cryptocoin/blockchain" />
  <add key="WcfApiKey" value="cryptocoin-demo-key" />
</appSettings>
```

**Modernisation note:** On .NET 10, WCF is replaced by CoreWCF (open source port) or rewritten as gRPC / minimal API endpoints. The custom API key header is replaced by ASP.NET Core authentication middleware.

---

## Running the Block Explorer Web UI

The web client is an ASP.NET 4.7.2 Web Forms application that provides a browser-based front-end for the Explorer REST API built into the node.

**Prerequisites:** The Node must be running with at least `--explorer 8080` before launching the web client.

**In Visual Studio:**
1. Set `CryptoCoin.Node` debug arguments to:
   ```
   --regtest --mine CJTZijYXJ4n3XisgX2jVioSWtcThfL31PC --explorer 8080 --wcf 8090
   ```
2. Set multiple startup projects: `CryptoCoin.Node` → **Start**, `CryptoCoin.Web.BlockExplorer` → **Start**, all others → **None**
3. Press **F5** — the node and web client launch together

**Default URL:** `https://localhost:44301/`

**Pages:**

| Page | URL | Description |
|------|-----|-------------|
| Dashboard | `/` | Network stat cards, latest 10 blocks, network info panel |
| Blocks | `/Blocks` | Full latest blocks list with timestamps |
| Block detail | `/Block?hash={hash}` | All block header fields, transaction list, prev/next navigation |
| Block detail | `/Block?height={n}` | Same as above, by height |
| Transaction | `/Transaction?txid={id}` | Status, confirmations, inputs, outputs, total value |
| Address | `/Address?address={addr}` | Balance and transaction count |
| Mempool | `/Mempool` | Pending transactions, fees, top transactions by fee rate |
| About | `/About` | Coin parameters and API endpoint reference |

**Global search bar** (in the navbar) accepts:
- A 64-character hex string → routes to Block or Transaction detail
- An integer → routes to Block by height
- Anything else → routes to Address lookup

**Configuration** (`Web.config`):

```xml
<appSettings>
  <add key="ExplorerBaseUrl" value="http://localhost:8080" />
  <add key="WcfBlockchainServiceUrl" value="http://localhost:8090/cryptocoin/blockchain" />
  <add key="WcfWalletServiceUrl" value="http://localhost:8090/cryptocoin/wallet" />
  <add key="WcfApiKey" value="cryptocoin-demo-key" />
</appSettings>
```

**ASMX Price Service:** Browse to `https://localhost:44301/PriceService.asmx` to see the service description page and test the three web methods (`GetPrice`, `GetPriceHistory`, `GetMarketStats`) directly.

**Wallet pages** (accessible via the Wallet dropdown in the navbar):

| Page | URL | Description |
|------|-----|-------------|
| Create Wallet | `/Wallet/Create` | Generate a new HD wallet and display the mnemonic phrase |
| Check Balance | `/Wallet/Balance` | Look up balance for any CRC address via `IWalletService` WCF |
| New Address | `/Wallet/NewAddress` | Generate the next receiving address for a wallet |
| Send CRC | `/Wallet/Send` | Broadcast a transaction via `IWalletService` WCF |

---

## Running the Wallet CLI

The wallet CLI lets you create wallets, generate addresses, check balances, and send transactions.

```
src\CryptoCoin.WalletCli\bin\Debug\CryptoCoin.WalletCli.exe
```

**Available commands** (type `help` at the prompt):

| Command | Description |
|---------|-------------|
| `create` | Create a new HD wallet with a fresh mnemonic |
| `restore` | Restore a wallet from a 12/24-word mnemonic phrase |
| `balance` | Show confirmed and unconfirmed balance |
| `address` | Generate a new receiving address |
| `send <address> <amount>` | Send CRC to an address |
| `history` | Show transaction history |
| `backup` | Export wallet backup |
| `exit` | Quit |

---

## Project Structure

```
CryptoCoin/
├── CryptoCoin.sln
├── lib/
│   └── SQLite/                        # SQLite native binaries (x64/x86)
└── src/
    ├── CryptoCoin.Cryptography/       # Core crypto primitives
    ├── CryptoCoin.Core/               # Blockchain engine (+ Newtonsoft.Json)
    ├── CryptoCoin.Transactions/       # UTXO transaction model
    ├── CryptoCoin.Networking/         # P2P network layer
    ├── CryptoCoin.Mining/             # Proof-of-work miner
    ├── CryptoCoin.Wallet/             # HD wallet library
    ├── CryptoCoin.Sdk/                # Developer SDK / RPC client (+ Newtonsoft.Json)
    ├── CryptoCoin.Contracts/          # Smart contract VM
    ├── CryptoCoin.Node/               # Full node (+ log4net, Castle.Windsor, EntLib)
    ├── CryptoCoin.Explorer/           # Block explorer REST API
    ├── CryptoCoin.Persistence/        # SQLite blockchain persistence
    ├── CryptoCoin.Services/           # WCF service contracts and implementations
    ├── CryptoCoin.WalletCli/          # Command-line wallet app
    ├── CryptoCoin.Demo/               # Interactive demo (start here)
    ├── CryptoCoin.Tests/              # Unit tests
    └── CryptoCoin.Web.BlockExplorer/  # Block explorer web UI (ASP.NET 4.7.2)
```

---

## Project Descriptions

### CryptoCoin.Cryptography
The foundation of everything. Contains:
- **ECDSA** signing and verification on the **secp256k1** curve (same as Bitcoin)
- **SHA-256**, double SHA-256, **RIPEMD-160**, HMAC-SHA512
- **Base58Check** encoding for addresses
- **BIP32 HD key derivation** — derive unlimited child keys from a single master seed
- **BIP39 Mnemonic** phrases — 12/24-word human-readable wallet backups
- **Merkle tree** construction and proof verification
- Cryptographically secure random number generation

No external dependencies — all algorithms implemented from scratch in VB.NET.

### CryptoCoin.Core
The blockchain engine:
- **Block** and **BlockHeader** structures with serialization
- **Blockchain** class — manages the chain, validates and adds blocks, handles reorganizations
- **Proof-of-work** difficulty calculation and adjustment (retargets every 1008 blocks)
- **Orphan pool** — holds blocks whose parent hasn't arrived yet
- **Chain state** — tracks the best chain tip and block index
- **Consensus rules** — coinbase maturity, block rewards, halving schedule
- **Genesis block** creation
- Binary serialization helpers (BufferReader/BufferWriter, VarInt)
- NuGet: **Newtonsoft.Json 13.0.3**

### CryptoCoin.Transactions
The UTXO (Unspent Transaction Output) model:
- **Transaction**, **TransactionInput**, **TransactionOutput** structures
- **UTXO set** — tracks all spendable outputs
- **Script engine** — P2PKH, P2SH, multisig, OP_RETURN scripts with a full interpreter
- **TransactionBuilder** — fluent API for constructing and signing transactions
- **TransactionValidator** — validates transactions against UTXO state and consensus rules
- **Mempool** — holds unconfirmed transactions, prioritized by fee rate
- **Coin selection** — largest-first and exact-match algorithms
- **Fee estimator** — estimates fees based on recent mempool data

### CryptoCoin.Networking
The P2P network layer:
- **TCP server** — accepts inbound peer connections
- **Peer manager** — tracks connected peers, enforces connection limits (max 125)
- **Message protocol** — version handshake, inventory, getblocks, getdata, ping/pong
- **Peer discovery** — DNS seeds, address exchange, exponential backoff
- **Ban manager** — tracks misbehaving peers and bans them automatically
- **Stratum-compatible** message framing for pool connections

### CryptoCoin.Mining
Proof-of-work mining:
- **Multi-threaded miner** — uses all CPU cores, each thread searches a nonce range
- **Block assembler** — selects transactions from the mempool by fee rate, creates coinbase
- **Hash rate monitor** — tracks hashes/second with a sliding window
- **Mining pool** — distributes work to multiple workers, proportional (PROP) payouts
- **Stratum protocol** — TCP server for pool workers (JSON-RPC over TCP)
- **Share validator** — validates submitted shares, detects duplicates

### CryptoCoin.Wallet
HD wallet library:
- **WalletManager** — creates/restores/loads wallets, manages multiple accounts
- **Account** — BIP44 derivation (m/44'/999'/account'/chain/index), gap limit scanning
- **KeyStore** — AES-256 encrypted private key storage, PBKDF2 key derivation
- **BalanceTracker** — confirmed, unconfirmed, and immature (coinbase) balances
- **TransactionHistory** — records sent/received transactions with confirmations
- **AddressBook** — contact management with labels
- **WalletBackup** — encrypted backup export/import, mnemonic verification
- **PaymentRequest** — URI format: `cryptocoin:address?amount=X&label=Y`

### CryptoCoin.Sdk
Developer SDK for building applications on CryptoCoin:
- **CryptoCoinClient** — typed wrapper around the node's JSON-RPC API
- **RpcClient** — low-level HTTP client with retry logic and error handling
- **TransactionRequestBuilder** — fluent builder for creating send requests
- Strongly-typed models: BlockInfo, TransactionInfo, NetworkInfo, WalletInfo
- Custom **RpcException** for error handling
- NuGet: **Newtonsoft.Json 13.0.3**

### CryptoCoin.Contracts
A stack-based smart contract virtual machine:
- **VirtualMachine** — executes contract bytecode with gas metering
- **ContractOpCodes** — full opcode set (arithmetic, logic, crypto, storage, control flow)
- **ContractStorage** — persistent key-value state for each contract
- **ContractExecutor** — manages execution context, gas limits, error handling
- **ContractCompiler** — compiles a simple contract language to VM bytecode
- **GasCalculator** — per-opcode gas costs
- **ContractDeployer** — deploys contracts to the blockchain
- **Standard contracts** — ERC20-like token contract, multisig wallet contract

### CryptoCoin.Node
The full node application that ties everything together:
- Starts a **Blockchain**, **Mempool**, **BlockAssembler**, and **Miner**
- Exposes a **JSON-RPC server** (HTTP on port 8332 by default)
- Hosts the **Explorer REST API** in-process (optional, `--explorer`)
- Hosts **WCF services** in-process (optional, `--wcf`) — `IBlockchainService` and `IWalletService`
- **Castle.Windsor** IoC container wires up all node services
- **NodeLogger** writes structured logs to both log4net and Enterprise Library simultaneously
- **SyncManager** for syncing with peers
- **BlockProcessor** for validating and applying incoming blocks
- Supports mainnet, testnet, and regtest networks
- NuGet: **log4net 2.0.15**, **Castle.Windsor 5.1.2**, **EnterpriseLibrary.Logging 6.0.1304**

### CryptoCoin.Explorer
A block explorer REST API server:
- HTTP server built on `HttpListener` (no web framework needed)
- Endpoints for blocks, transactions, addresses, mempool, and network stats
- Returns JSON responses, CORS-enabled for browser access
- **Primarily used embedded inside `CryptoCoin.Node`** via the `--explorer <port>` flag, sharing the live blockchain directly
- Can also run standalone (`CryptoCoin.Explorer.exe --port 8080`) for testing, but will only show the genesis block without a connected node

### CryptoCoin.Persistence
SQLite-backed blockchain persistence:
- **SqliteBlockStore** — extends `BlockStore`, persists full block BLOBs to `blockchain.db`
- **SqliteChainState** — extends `ChainState`, persists block index and chain tip
- **Database** — manages the SQLite connection and schema (WAL mode for performance)
- **PersistenceFactory** — detects existing database and resumes the chain on restart
- Enabled with `--persist` flag; data stored in `data/<network>/blockchain.db`
- NuGet: **System.Data.SQLite.Core 1.0.118.0** (no .NET Core counterpart — replaced by `Microsoft.Data.Sqlite` on .NET 10)

### CryptoCoin.Services
WCF service contracts and implementations:
- **IBlockchainService** — `GetBlockCount`, `GetBestBlockHash`, `GetBlock`, `GetBlockByHeight`, `GetLatestBlocks`, `GetNetworkStatus`, `GetMempool`
- **IWalletService** — `CreateWallet`, `GetBalance`, `GetNewAddress`, `SendTransaction`
- **ApiKeyHeader** — custom SOAP header carrying a shared secret key
- **ApiKeyServiceInspector** / **ApiKeyClientInspector** — WCF message inspectors that validate/inject the key on every call
- **BlockchainServiceImpl** — backed directly by the live `Blockchain` and `Mempool` instances
- **NodeServiceHost** — self-hosted `BasicHttpBinding` on a configurable port
- All data contracts use `[DataContract]` / `[DataMember]` attributes

### CryptoCoin.WalletCli
A command-line wallet application:
- Interactive REPL with commands for all wallet operations
- Create wallets, restore from mnemonic, generate addresses, send CRC
- Displays transaction history and balance breakdown
- Wallet files are AES-256 encrypted on disk

### CryptoCoin.Tests
MSTest unit and integration test suite covering all major subsystems:
- **Cryptography** — SHA-256, ECDSA sign/verify, Base58, HD keys, Mnemonic, Merkle tree, addresses
- **Core** — Blockchain genesis, block retrieval, difficulty, consensus rules, injected-store constructor
- **Transactions** — Coinbase creation, serialization, UTXO set, mempool, script engine, coin selection
- **Mining** — Miner integration tests
- **Wallet** — WalletManager, KeyStore encryption
- **Persistence** — SqliteBlockStore and SqliteChainState round-trip tests against a real temp database
- **Services** — BlockchainServiceImpl tested directly (no WCF channel), ApiKeyHeader serialization

### CryptoCoin.Demo
The best place to start. A self-contained console app that demonstrates:
- Mnemonic generation and seed derivation
- HD key derivation and address generation
- ECDSA signing and verification
- Merkle tree construction
- Base58Check encoding

No setup required — just build and run.

### CryptoCoin.Web.BlockExplorer
A browser-based block explorer built on **ASP.NET 4.7.2 Web Forms** (C#). Consumes the Explorer REST API and WCF services from the node:
- **Dashboard** — live network stat cards (including CRC/USD price), latest blocks table, and network info panel
- **Block detail** — full header fields, transaction list, and prev/next block navigation
- **Transaction detail** — confirmation status, input/output counts, total value
- **Address lookup** — balance and transaction count
- **Mempool viewer** — pending transactions ranked by fee rate
- **Global search** — routes hashes, heights, and addresses from the navbar search bar
- **Wallet pages** — Create wallet, Check balance, New address, Send CRC (all via `IWalletService` WCF)
- **WcfBlockchainClient** / **WcfWalletClient** — typed WCF channel proxies with API key behavior
- **PriceService.asmx** — ASMX web service providing mock CRC/USD price data; consumed by the dashboard via jQuery `$.ajax` (no .NET Core equivalent — replaced by minimal API on .NET 10)
- Responsive Bootstrap 5 layout with a custom dark theme

---

## Coin Parameters

| Parameter | Value |
|-----------|-------|
| Ticker | CRC |
| Max supply | 21,000,000 CRC |
| Initial block reward | 50 CRC |
| Halving interval | 210,000 blocks |
| Target block time | 2 minutes |
| Difficulty adjustment | Every 1,008 blocks |
| Address prefix | C (mainnet) |
| Coin type (BIP44) | 999 |
| Curve | secp256k1 |
| Hash algorithm | Double SHA-256 |

---

## Architecture Overview

```
                    ┌──────────────────────────────────────────────────────┐
                    │                  CryptoCoin.Node                      │
                    │  ┌──────────────────────────────────────────────────┐ │
                    │  │   Blockchain  │  Mempool  │  Miner               │ │
                    │  │   (optionally persisted via CryptoCoin.Persistence)│ │
                    │  └──────────────────────────────────────────────────┘ │
                    │  ┌────────────┐ ┌──────────────┐ ┌─────────────────┐ │
                    │  │ RPC Server │ │ Explorer API │ │  WCF Services   │ │
                    │  │ port 8332  │ │ port 8080    │ │  port 8090      │ │
                    │  └────────────┘ └──────┬───────┘ └────────┬────────┘ │
                    │  Castle.Windsor IoC  log4net + EntLib logging         │
                    └─────────────────────── │ ──────────────── │ ──────────┘
                                             │                  │
                              ┌──────────────▼──────────────────▼──────────┐
                              │         CryptoCoin.Web.BlockExplorer        │
                              │  (ASP.NET 4.7.2 Web Forms browser UI)       │
                              │  ExplorerApiClient (REST) + WcfBlockchainClient│
                              └─────────────────────────────────────────────┘

   ┌─────────────┐  ┌────────────────┐  ┌────────────────┐  ┌──────────────┐
   │   Core      │  │  Transactions  │  │  Cryptography  │  │   Mining     │
   └─────────────┘  └────────────────┘  └────────────────┘  └──────────────┘

   ┌─────────────┐  ┌────────────────┐  ┌────────────────┐  ┌──────────────┐
   │   Wallet    │  │      SDK       │  │   Contracts    │  │  Networking  │
   └─────────────┘  └────────────────┘  └────────────────┘  └──────────────┘
```

---

## License

This project is provided for educational purposes. Do whatever you like with it.
