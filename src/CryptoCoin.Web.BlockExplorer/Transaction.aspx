<%@ Page Title="Transaction" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Transaction.aspx.cs" Inherits="CryptoCoin.Web.BlockExplorer.Transaction" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

    <div class="page-title">
        <span class="icon">&#8646;</span> Transaction
    </div>

    <asp:Panel ID="pnlError" runat="server" CssClass="explorer-alert" Visible="false">
        <strong>Transaction not found.</strong>
        <asp:Literal ID="litErrorDetail" runat="server" />
    </asp:Panel>

    <asp:Panel ID="pnlTx" runat="server" Visible="false">
        <div class="explorer-panel">
            <div class="panel-header"><span>Transaction Details</span></div>
            <div class="detail-grid">
                <div class="detail-row">
                    <div class="dk">TXID</div>
                    <div class="dv"><code class="hash-full"><asp:Literal ID="litTxId" runat="server" /></code></div>
                </div>
                <div class="detail-row">
                    <div class="dk">Status</div>
                    <div class="dv"><asp:Literal ID="litStatus" runat="server" /></div>
                </div>
                <div class="detail-row">
                    <div class="dk">Block</div>
                    <div class="dv"><asp:Literal ID="litBlock" runat="server" /></div>
                </div>
                <div class="detail-row">
                    <div class="dk">Confirmations</div>
                    <div class="dv"><asp:Literal ID="litConfirmations" runat="server" /></div>
                </div>
                <div class="detail-row">
                    <div class="dk">Type</div>
                    <div class="dv"><asp:Literal ID="litType" runat="server" /></div>
                </div>
                <div class="detail-row">
                    <div class="dk">Inputs</div>
                    <div class="dv"><asp:Literal ID="litInputCount" runat="server" /></div>
                </div>
                <div class="detail-row">
                    <div class="dk">Outputs</div>
                    <div class="dv"><asp:Literal ID="litOutputCount" runat="server" /></div>
                </div>
                <div class="detail-row">
                    <div class="dk">Total Output</div>
                    <div class="dv"><asp:Literal ID="litTotalOutput" runat="server" /></div>
                </div>
                <div class="detail-row">
                    <div class="dk">Size</div>
                    <div class="dv"><asp:Literal ID="litSize" runat="server" /></div>
                </div>
            </div>
        </div>
    </asp:Panel>

</asp:Content>
