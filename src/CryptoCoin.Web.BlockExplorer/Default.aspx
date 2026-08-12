<%@ Page Title="Dashboard" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="CryptoCoin.Web.BlockExplorer._Default" %>

<asp:Content ID="BannerSection" ContentPlaceHolderID="BannerContent" runat="server">
    <div class="site-banner-wrap">
        <img src="<%: ResolveUrl("~/Content/images/CryptoCoin-banner.png") %>"
             alt="CryptoCoin Block Explorer"
             class="site-banner-img" />
    </div>
</asp:Content>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

    <asp:Panel ID="pnlError" runat="server" CssClass="explorer-alert" Visible="false">
        <strong>Explorer API unavailable.</strong>
        Make sure <code>CryptoCoin.Explorer.exe</code> is running on
        <asp:Literal ID="litApiUrl" runat="server" />.
    </asp:Panel>

    <%-- ── Network stat cards ── --%>
    <div class="stat-cards" role="region" aria-label="Network statistics">
        <div class="stat-card">
            <div class="label">Block Height</div>
            <div class="value"><asp:Literal ID="litHeight" runat="server">—</asp:Literal></div>
        </div>
        <div class="stat-card">
            <div class="label">Difficulty</div>
            <div class="value small"><asp:Literal ID="litDifficulty" runat="server">—</asp:Literal></div>
        </div>
        <div class="stat-card">
            <div class="label">Hash Rate</div>
            <div class="value small"><asp:Literal ID="litHashRate" runat="server">—</asp:Literal></div>
        </div>
        <div class="stat-card">
            <div class="label">Mempool Txs</div>
            <div class="value"><asp:Literal ID="litMempool" runat="server">—</asp:Literal></div>
        </div>
        <div class="stat-card">
            <div class="label">Best Block</div>
            <div class="value small"><asp:Literal ID="litBestHash" runat="server">—</asp:Literal></div>
        </div>
        <div class="stat-card">
            <div class="label">Best Block Time</div>
            <div class="value small"><asp:Literal ID="litBestTime" runat="server">—</asp:Literal></div>
        </div>
        <div class="stat-card" id="priceCard">
            <div class="label">CRC / USD</div>
            <div class="value small" id="priceValue">—</div>
            <div style="font-size:0.75rem; margin-top:0.25rem;" id="priceChange"></div>
        </div>
    </div>

    <%-- ── Latest blocks panel ── --%>
    <div class="explorer-panel" role="region" aria-label="Latest blocks">
        <div class="panel-header">
            <span>Latest Blocks</span>
            <a runat="server" href="~/Blocks">View all &rarr;</a>
        </div>
        <asp:Panel ID="pnlNoBlocks" runat="server" Visible="false">
            <div style="padding:1rem 1.25rem; color:var(--cc-muted);">No blocks available.</div>
        </asp:Panel>
        <asp:Repeater ID="rptBlocks" runat="server">
            <HeaderTemplate>
                <table class="explorer-table" aria-label="Latest blocks">
                    <thead>
                        <tr>
                            <th scope="col">Height</th>
                            <th scope="col">Hash</th>
                            <th scope="col">Txs</th>
                            <th scope="col">Size</th>
                            <th scope="col">Age</th>
                        </tr>
                    </thead>
                    <tbody>
            </HeaderTemplate>
            <ItemTemplate>
                <tr>
                    <td>
                        <a href='<%# ResolveUrl("~/Block?height=" + Eval("height")) %>'
                           class="hash"><%# Eval("height") %></a>
                    </td>
                    <td>
                        <a href='<%# ResolveUrl("~/Block?hash=" + Eval("hash")) %>'
                           class="hash" title='<%# Eval("hash") %>'>
                            <%# CryptoCoin.Web.BlockExplorer.ExplorerApiClient.ShortHash(Eval("hash")?.ToString()) %>
                        </a>
                    </td>
                    <td><%# Eval("txcount") %></td>
                    <td><%# Eval("size") %> B</td>
                    <td><%# CryptoCoin.Web.BlockExplorer.ExplorerApiClient.TimeAgo(Convert.ToInt64(Eval("timestamp"))) %></td>
                </tr>
            </ItemTemplate>
            <FooterTemplate>
                    </tbody>
                </table>
            </FooterTemplate>
        </asp:Repeater>
    </div>

    <%-- ── Network info panel ── --%>
    <asp:Panel ID="pnlNetwork" runat="server" Visible="false">
        <div class="explorer-panel" role="region" aria-label="Network information">
            <div class="panel-header"><span>Network Information</span></div>
            <div class="detail-grid">
                <div class="detail-row">
                    <div class="dk">Coin</div>
                    <div class="dv"><asp:Literal ID="litCoinName" runat="server" /></div>
                </div>
                <div class="detail-row">
                    <div class="dk">Symbol</div>
                    <div class="dv"><asp:Literal ID="litCoinSymbol" runat="server" /></div>
                </div>
                <div class="detail-row">
                    <div class="dk">Block Height</div>
                    <div class="dv"><asp:Literal ID="litNetHeight" runat="server" /></div>
                </div>
                <div class="detail-row">
                    <div class="dk">Total Blocks</div>
                    <div class="dv"><asp:Literal ID="litBlockCount" runat="server" /></div>
                </div>
                <div class="detail-row">
                    <div class="dk">Difficulty</div>
                    <div class="dv"><asp:Literal ID="litNetDifficulty" runat="server" /></div>
                </div>
                <div class="detail-row">
                    <div class="dk">Estimated Hash Rate</div>
                    <div class="dv"><asp:Literal ID="litNetHashRate" runat="server" /></div>
                </div>
                <div class="detail-row">
                    <div class="dk">Mempool Transactions</div>
                    <div class="dv"><asp:Literal ID="litNetMempool" runat="server" /></div>
                </div>
                <div class="detail-row">
                    <div class="dk">Max Supply</div>
                    <div class="dv">21,000,000 CRC</div>
                </div>
                <div class="detail-row">
                    <div class="dk">Block Reward</div>
                    <div class="dv">50 CRC (halves every 210,000 blocks)</div>
                </div>
                <div class="detail-row">
                    <div class="dk">Target Block Time</div>
                    <div class="dv">2 minutes</div>
                </div>
                <div class="detail-row">
                    <div class="dk">Algorithm</div>
                    <div class="dv">Double SHA-256 (secp256k1)</div>
                </div>
            </div>
        </div>
    </asp:Panel>

    <%-- ── CRC Price via ASMX service ── --%>
    <%-- Calls PriceService.asmx using the classic ASP.NET AJAX / jQuery pattern.
         Modernisation note: on .NET 10 this would be a fetch() call to a minimal API endpoint. --%>
    <script type="text/javascript">
        (function loadCrcPrice() {
            $.ajax({
                type: "POST",
                url: '<%: ResolveUrl("~/PriceService.asmx/GetPrice") %>',
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function (response) {
                    var d = response.d;
                    if (!d) return;
                    $("#priceValue").text("$" + d.PriceUsd.toFixed(6));
                    var change = d.Change24h;
                    var sign   = change >= 0 ? "+" : "";
                    var color  = change >= 0 ? "var(--cc-green)" : "var(--cc-red)";
                    $("#priceChange").html(
                        '<span style="color:' + color + '">' + sign + change.toFixed(2) + '% (24h)</span>'
                    );
                },
                error: function () {
                    $("#priceValue").text("unavailable");
                }
            });
        })();
    </script>

</asp:Content>
