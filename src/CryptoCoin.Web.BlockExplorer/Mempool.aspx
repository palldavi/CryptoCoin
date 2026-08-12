<%@ Page Title="Mempool" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Mempool.aspx.cs" Inherits="CryptoCoin.Web.BlockExplorer.Mempool" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

    <div class="page-title">
        <span class="icon">&#9203;</span> Mempool
    </div>

    <asp:Panel ID="pnlError" runat="server" CssClass="explorer-alert" Visible="false">
        <strong>Could not load mempool.</strong> Make sure the Explorer API is running.
    </asp:Panel>

    <div class="stat-cards" style="margin-bottom:1.5rem;">
        <div class="stat-card">
            <div class="label">Pending Transactions</div>
            <div class="value"><asp:Literal ID="litSize" runat="server">—</asp:Literal></div>
        </div>
        <div class="stat-card">
            <div class="label">Total Size</div>
            <div class="value small"><asp:Literal ID="litBytes" runat="server">—</asp:Literal></div>
        </div>
        <div class="stat-card">
            <div class="label">Total Fees</div>
            <div class="value small"><asp:Literal ID="litFees" runat="server">—</asp:Literal></div>
        </div>
    </div>

    <div class="explorer-panel">
        <div class="panel-header">
            <span>Top Transactions by Fee Rate</span>
        </div>
        <asp:Panel ID="pnlEmpty" runat="server" Visible="false">
            <div style="padding:1.25rem; color:var(--cc-muted);">Mempool is empty.</div>
        </asp:Panel>
        <asp:Repeater ID="rptTxs" runat="server">
            <HeaderTemplate>
                <table class="explorer-table" aria-label="Mempool transactions">
                    <thead>
                        <tr>
                            <th scope="col">TXID</th>
                            <th scope="col">Fee</th>
                            <th scope="col">Size</th>
                            <th scope="col">Fee Rate</th>
                        </tr>
                    </thead>
                    <tbody>
            </HeaderTemplate>
            <ItemTemplate>
                <tr>
                    <td>
                        <a href='<%# ResolveUrl("~/Transaction?txid=" + Eval("txid")) %>'
                           class="hash" title='<%# Eval("txid") %>'>
                            <%# CryptoCoin.Web.BlockExplorer.ExplorerApiClient.ShortHash(Eval("txid")?.ToString()) %>
                        </a>
                    </td>
                    <td><%# CryptoCoin.Web.BlockExplorer.ExplorerApiClient.FormatCrc(Convert.ToInt64(Eval("fee"))) %></td>
                    <td><%# Eval("size") %> B</td>
                    <td><%# $"{Eval("feeRate"):N2}" %> sat/B</td>
                </tr>
            </ItemTemplate>
            <FooterTemplate>
                    </tbody>
                </table>
            </FooterTemplate>
        </asp:Repeater>
    </div>

</asp:Content>
