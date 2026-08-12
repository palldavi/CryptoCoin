<%@ Page Title="Blocks" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Blocks.aspx.cs" Inherits="CryptoCoin.Web.BlockExplorer.Blocks" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

    <div class="page-title">
        <span class="icon">&#9632;</span> Latest Blocks
    </div>

    <asp:Panel ID="pnlError" runat="server" CssClass="explorer-alert" Visible="false">
        <strong>Could not load blocks.</strong> Make sure the Explorer API is running.
    </asp:Panel>

    <div class="explorer-panel">
        <div class="panel-header">
            <span>Blocks</span>
            <span><asp:Literal ID="litCount" runat="server" /></span>
        </div>
        <asp:Repeater ID="rptBlocks" runat="server">
            <HeaderTemplate>
                <table class="explorer-table" aria-label="Blocks">
                    <thead>
                        <tr>
                            <th scope="col">Height</th>
                            <th scope="col">Hash</th>
                            <th scope="col">Merkle Root</th>
                            <th scope="col">Txs</th>
                            <th scope="col">Size</th>
                            <th scope="col">Time</th>
                        </tr>
                    </thead>
                    <tbody>
            </HeaderTemplate>
            <ItemTemplate>
                <tr>
                    <td>
                        <a href='<%# ResolveUrl("~/Block?height=" + Eval("height")) %>' class="hash">
                            <%# Eval("height") %>
                        </a>
                    </td>
                    <td>
                        <a href='<%# ResolveUrl("~/Block?hash=" + Eval("hash")) %>'
                           class="hash" title='<%# Eval("hash") %>'>
                            <%# CryptoCoin.Web.BlockExplorer.ExplorerApiClient.ShortHash(Eval("hash")?.ToString()) %>
                        </a>
                    </td>
                    <td>
                        <span class="hash" title='<%# Eval("merkleRoot") %>'>
                            <%# CryptoCoin.Web.BlockExplorer.ExplorerApiClient.ShortHash(Eval("merkleRoot")?.ToString()) %>
                        </span>
                    </td>
                    <td><%# Eval("txcount") %></td>
                    <td><%# Eval("size") %> B</td>
                    <td><%# CryptoCoin.Web.BlockExplorer.ExplorerApiClient.FormatTimestamp(Convert.ToInt64(Eval("timestamp"))) %></td>
                </tr>
            </ItemTemplate>
            <FooterTemplate>
                    </tbody>
                </table>
            </FooterTemplate>
        </asp:Repeater>
    </div>

</asp:Content>
