<%@ Page Title="Wallet Balance" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Balance.aspx.cs" Inherits="CryptoCoin.Web.BlockExplorer.Wallet.Balance" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

    <div class="page-title">
        <span class="icon">&#9673;</span> Wallet Balance
    </div>

    <div class="explorer-panel" style="max-width:600px; margin-bottom:1.5rem;">
        <div class="panel-header"><span>Look Up Balance</span></div>
        <div style="padding:1.25rem; display:flex; gap:0.75rem;">
            <asp:TextBox ID="txtAddress" runat="server" CssClass="form-control"
                placeholder="Enter a CRC address…"
                style="max-width:none; flex:1;" />
            <asp:Button ID="btnLookup" runat="server" Text="Check"
                CssClass="btn-search" OnClick="btnLookup_Click" />
        </div>
    </div>

    <asp:Panel ID="pnlError" runat="server" CssClass="explorer-alert" Visible="false">
        <strong>Could not retrieve balance.</strong>
        <asp:Literal ID="litError" runat="server" />
    </asp:Panel>

    <asp:Panel ID="pnlResult" runat="server" Visible="false">
        <div class="explorer-panel" style="max-width:600px;">
            <div class="panel-header"><span>Balance Details</span></div>
            <div class="detail-grid">
                <div class="detail-row">
                    <div class="dk">Address</div>
                    <div class="dv"><code class="hash-full"><asp:Literal ID="litAddress" runat="server" /></code></div>
                </div>
                <div class="detail-row">
                    <div class="dk">Confirmed</div>
                    <div class="dv" style="font-size:1.2rem; font-weight:700; color:var(--cc-green);">
                        <asp:Literal ID="litConfirmed" runat="server" />
                    </div>
                </div>
                <div class="detail-row">
                    <div class="dk">Unconfirmed</div>
                    <div class="dv" style="color:var(--cc-accent);">
                        <asp:Literal ID="litUnconfirmed" runat="server" />
                    </div>
                </div>
                <div class="detail-row">
                    <div class="dk">Total</div>
                    <div class="dv" style="font-size:1.2rem; font-weight:700; color:var(--cc-accent);">
                        <asp:Literal ID="litTotal" runat="server" />
                    </div>
                </div>
            </div>
        </div>
    </asp:Panel>

</asp:Content>
