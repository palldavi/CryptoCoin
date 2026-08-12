<%@ Page Title="Address" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Address.aspx.cs" Inherits="CryptoCoin.Web.BlockExplorer.Address" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

    <div class="page-title">
        <span class="icon">&#9673;</span> Address
    </div>

    <asp:Panel ID="pnlError" runat="server" CssClass="explorer-alert" Visible="false">
        <strong>Address not found.</strong>
        <asp:Literal ID="litErrorDetail" runat="server" />
    </asp:Panel>

    <asp:Panel ID="pnlAddress" runat="server" Visible="false">
        <div class="explorer-panel">
            <div class="panel-header"><span>Address Details</span></div>
            <div class="detail-grid">
                <div class="detail-row">
                    <div class="dk">Address</div>
                    <div class="dv"><code class="hash-full"><asp:Literal ID="litAddress" runat="server" /></code></div>
                </div>
                <div class="detail-row">
                    <div class="dk">Balance</div>
                    <div class="dv" style="font-size:1.2rem; font-weight:700; color:var(--cc-accent);">
                        <asp:Literal ID="litBalance" runat="server" />
                    </div>
                </div>
                <div class="detail-row">
                    <div class="dk">Transactions</div>
                    <div class="dv"><asp:Literal ID="litTxCount" runat="server" /></div>
                </div>
            </div>
        </div>
    </asp:Panel>

</asp:Content>
