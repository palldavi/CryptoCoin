<%@ Page Title="Block" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Block.aspx.cs" Inherits="CryptoCoin.Web.BlockExplorer.Block" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

    <div class="page-title">
        <span class="icon">&#9632;</span>
        Block <asp:Literal ID="litPageTitle" runat="server" />
    </div>

    <asp:Panel ID="pnlError" runat="server" CssClass="explorer-alert" Visible="false">
        <strong>Block not found.</strong>
        <asp:Literal ID="litErrorDetail" runat="server" />
    </asp:Panel>

    <asp:Panel ID="pnlBlock" runat="server" Visible="false">

        <div class="explorer-panel" style="margin-bottom:1.5rem;">
            <div class="panel-header"><span>Block Details</span></div>
            <div class="detail-grid">
                <div class="detail-row">
                    <div class="dk">Height</div>
                    <div class="dv"><asp:Literal ID="litHeight" runat="server" /></div>
                </div>
                <div class="detail-row">
                    <div class="dk">Hash</div>
                    <div class="dv"><code class="hash-full"><asp:Literal ID="litHash" runat="server" /></code></div>
                </div>
                <div class="detail-row">
                    <div class="dk">Previous Hash</div>
                    <div class="dv">
                        <a class="hash" id="aPrevHash" runat="server" href="#"><asp:Literal ID="litPrevHash" runat="server" /></a>
                    </div>
                </div>
                <div class="detail-row">
                    <div class="dk">Merkle Root</div>
                    <div class="dv"><code class="hash-full"><asp:Literal ID="litMerkleRoot" runat="server" /></code></div>
                </div>
                <div class="detail-row">
                    <div class="dk">Timestamp</div>
                    <div class="dv"><asp:Literal ID="litTimestamp" runat="server" /></div>
                </div>
                <div class="detail-row">
                    <div class="dk">Transactions</div>
                    <div class="dv"><asp:Literal ID="litTxCount" runat="server" /></div>
                </div>
                <div class="detail-row">
                    <div class="dk">Size</div>
                    <div class="dv"><asp:Literal ID="litSize" runat="server" /></div>
                </div>
                <div class="detail-row">
                    <div class="dk">Bits</div>
                    <div class="dv"><asp:Literal ID="litBits" runat="server" /></div>
                </div>
                <div class="detail-row">
                    <div class="dk">Nonce</div>
                    <div class="dv"><asp:Literal ID="litNonce" runat="server" /></div>
                </div>
            </div>
        </div>

        <div class="explorer-panel">
            <div class="panel-header"><span>Transactions</span></div>
            <asp:Repeater ID="rptTxIds" runat="server">
                <HeaderTemplate>
                    <table class="explorer-table" aria-label="Block transactions">
                        <thead>
                            <tr><th scope="col">#</th><th scope="col">Transaction ID</th></tr>
                        </thead>
                        <tbody>
                </HeaderTemplate>
                <ItemTemplate>
                    <tr>
                        <td style="color:var(--cc-muted);"><%# Container.ItemIndex + 1 %></td>
                        <td>
                            <a href='<%# ResolveUrl("~/Transaction?txid=" + Container.DataItem.ToString()) %>'
                               class="hash"><%# Container.DataItem %></a>
                        </td>
                    </tr>
                </ItemTemplate>
                <FooterTemplate>
                        </tbody>
                    </table>
                </FooterTemplate>
            </asp:Repeater>
        </div>

        <div style="margin-top:1rem; display:flex; gap:1rem;">
            <asp:HyperLink ID="lnkPrev" runat="server" CssClass="btn-search" style="padding:0.4rem 1rem;">
                &larr; Previous Block
            </asp:HyperLink>
            <asp:HyperLink ID="lnkNext" runat="server" CssClass="btn-search" style="padding:0.4rem 1rem;">
                Next Block &rarr;
            </asp:HyperLink>
        </div>

    </asp:Panel>

</asp:Content>
