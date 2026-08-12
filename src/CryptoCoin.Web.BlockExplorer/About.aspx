<%@ Page Title="About" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="About.aspx.cs" Inherits="CryptoCoin.Web.BlockExplorer.About" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

    <div class="page-title">
        <span class="icon">&#9432;</span> About CryptoCoin Explorer
    </div>

    <div class="explorer-panel">
        <div class="panel-header"><span>What is this?</span></div>
        <div style="padding:1.25rem; line-height:1.7;">
            <p>
                <strong style="color:var(--cc-accent);">CryptoCoin (CRC)</strong> is an educational
                cryptocurrency implementation in VB.NET / .NET Framework 4.8, demonstrating how a
                real blockchain works end-to-end — from elliptic curve cryptography and proof-of-work
                mining through to a wallet, block explorer API, and smart contract VM.
            </p>
            <p>This Block Explorer is a read-only web front-end for the
                <code>CryptoCoin.Explorer</code> REST API. It lets you browse blocks,
                inspect transactions, look up addresses, and monitor the mempool in real time.
            </p>
        </div>
    </div>

    <div class="explorer-panel">
        <div class="panel-header"><span>API Endpoints</span></div>
        <table class="explorer-table" aria-label="API endpoints">
            <thead>
                <tr>
                    <th scope="col">Endpoint</th>
                    <th scope="col">Description</th>
                </tr>
            </thead>
            <tbody>
                <tr><td class="hash">GET /api/status</td><td>Node status (height, best hash, mempool)</td></tr>
                <tr><td class="hash">GET /api/network</td><td>Network info (difficulty, hash rate, coin name)</td></tr>
                <tr><td class="hash">GET /api/blocks/latest</td><td>Last 10 blocks</td></tr>
                <tr><td class="hash">GET /api/blocks/height/{n}</td><td>Block at height n</td></tr>
                <tr><td class="hash">GET /api/block/{hash}</td><td>Block by hash</td></tr>
                <tr><td class="hash">GET /api/tx/{txid}</td><td>Transaction by ID</td></tr>
                <tr><td class="hash">GET /api/mempool</td><td>Mempool contents and top transactions</td></tr>
                <tr><td class="hash">GET /api/address/{address}</td><td>Address info and transaction count</td></tr>
            </tbody>
        </table>
    </div>

    <div class="explorer-panel">
        <div class="panel-header"><span>Coin Parameters</span></div>
        <table class="explorer-table" aria-label="Coin parameters">
            <thead>
                <tr><th scope="col">Parameter</th><th scope="col">Value</th></tr>
            </thead>
            <tbody>
                <tr><td>Ticker</td><td>CRC</td></tr>
                <tr><td>Max Supply</td><td>21,000,000 CRC</td></tr>
                <tr><td>Initial Block Reward</td><td>50 CRC</td></tr>
                <tr><td>Halving Interval</td><td>210,000 blocks</td></tr>
                <tr><td>Target Block Time</td><td>2 minutes</td></tr>
                <tr><td>Difficulty Adjustment</td><td>Every 1,008 blocks</td></tr>
                <tr><td>Curve</td><td>secp256k1</td></tr>
                <tr><td>Hash Algorithm</td><td>Double SHA-256</td></tr>
            </tbody>
        </table>
    </div>

</asp:Content>
