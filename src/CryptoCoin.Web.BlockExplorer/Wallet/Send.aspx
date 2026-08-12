<%@ Page Title="Send CRC" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Send.aspx.cs" Inherits="CryptoCoin.Web.BlockExplorer.Wallet.Send" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

    <div class="page-title">
        <span class="icon">&#8594;</span> Send CRC
    </div>

    <asp:Panel ID="pnlForm" runat="server">
        <div class="explorer-panel" style="max-width:600px;">
            <div class="panel-header"><span>Send Transaction</span></div>
            <div style="padding:1.25rem;">
                <p style="color:var(--cc-muted); margin-bottom:1.25rem; font-size:0.875rem;">
                    Broadcasts a transaction via the <code>IWalletService</code> WCF service.
                    The current implementation returns a stub response — wire into
                    <code>WalletManager</code> for real transactions.
                </p>
                <div style="margin-bottom:1rem;">
                    <label style="display:block; color:var(--cc-muted); font-size:0.8rem; margin-bottom:0.35rem;">From Address</label>
                    <asp:TextBox ID="txtFrom" runat="server" CssClass="form-control"
                        placeholder="Your CRC address" style="max-width:none;" />
                </div>
                <div style="margin-bottom:1rem;">
                    <label style="display:block; color:var(--cc-muted); font-size:0.8rem; margin-bottom:0.35rem;">To Address</label>
                    <asp:TextBox ID="txtTo" runat="server" CssClass="form-control"
                        placeholder="Recipient CRC address" style="max-width:none;" />
                </div>
                <div style="margin-bottom:1rem; display:grid; grid-template-columns:1fr 1fr; gap:1rem;">
                    <div>
                        <label style="display:block; color:var(--cc-muted); font-size:0.8rem; margin-bottom:0.35rem;">Amount (CRC)</label>
                        <asp:TextBox ID="txtAmount" runat="server" CssClass="form-control"
                            placeholder="0.00000000" style="max-width:none;" />
                    </div>
                    <div>
                        <label style="display:block; color:var(--cc-muted); font-size:0.8rem; margin-bottom:0.35rem;">Fee (CRC)</label>
                        <asp:TextBox ID="txtFee" runat="server" CssClass="form-control"
                            placeholder="0.00001000" style="max-width:none;" />
                    </div>
                </div>
                <asp:Button ID="btnSend" runat="server" Text="Send Transaction"
                    CssClass="btn-search" OnClick="btnSend_Click" />
            </div>
        </div>
    </asp:Panel>

    <asp:Panel ID="pnlError" runat="server" CssClass="explorer-alert" Visible="false">
        <strong>Transaction failed.</strong>
        <asp:Literal ID="litError" runat="server" />
    </asp:Panel>

    <asp:Panel ID="pnlSuccess" runat="server" Visible="false">
        <div class="explorer-panel" style="max-width:600px;">
            <div class="panel-header"><span>Transaction Submitted</span></div>
            <div class="detail-grid">
                <div class="detail-row">
                    <div class="dk">Transaction ID</div>
                    <div class="dv">
                        <a class="hash" href='<%# "~/Transaction?txid=" + litTxId.Text %>' runat="server">
                            <asp:Literal ID="litTxId" runat="server" />
                        </a>
                    </div>
                </div>
                <div class="detail-row">
                    <div class="dk">Status</div>
                    <div class="dv"><span class="badge-mempool">Pending</span></div>
                </div>
            </div>
        </div>
    </asp:Panel>

</asp:Content>
