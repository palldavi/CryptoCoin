<%@ Page Title="Create Wallet" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Create.aspx.cs" Inherits="CryptoCoin.Web.BlockExplorer.Wallet.Create" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

    <div class="page-title">
        <span class="icon">&#9679;</span> Create New Wallet
    </div>

    <asp:Panel ID="pnlForm" runat="server">
        <div class="explorer-panel" style="max-width:600px;">
            <div class="panel-header"><span>New HD Wallet</span></div>
            <div style="padding:1.25rem;">
                <p style="color:var(--cc-muted); margin-bottom:1.25rem;">
                    Creates a new BIP44 HD wallet with a fresh 12-word mnemonic phrase.
                    The wallet is backed by the <code>IWalletService</code> WCF service.
                </p>
                <div style="margin-bottom:1rem;">
                    <label style="display:block; color:var(--cc-muted); font-size:0.8rem; margin-bottom:0.35rem;">
                        Wallet Name
                    </label>
                    <asp:TextBox ID="txtName" runat="server" CssClass="form-control"
                        placeholder="My CRC Wallet" style="max-width:none;" />
                </div>
                <div style="margin-bottom:1.25rem;">
                    <label style="display:block; color:var(--cc-muted); font-size:0.8rem; margin-bottom:0.35rem;">
                        Passphrase (optional)
                    </label>
                    <asp:TextBox ID="txtPassphrase" runat="server" TextMode="Password"
                        CssClass="form-control" placeholder="Leave blank for no passphrase"
                        style="max-width:none;" />
                </div>
                <asp:Button ID="btnCreate" runat="server" Text="Create Wallet"
                    CssClass="btn-search" OnClick="btnCreate_Click" />
            </div>
        </div>
    </asp:Panel>

    <asp:Panel ID="pnlError" runat="server" CssClass="explorer-alert" Visible="false">
        <strong>Could not create wallet.</strong>
        <asp:Literal ID="litError" runat="server" />
    </asp:Panel>

    <asp:Panel ID="pnlResult" runat="server" Visible="false">
        <div class="explorer-panel" style="max-width:700px;">
            <div class="panel-header"><span>Wallet Created</span></div>
            <div class="detail-grid">
                <div class="detail-row">
                    <div class="dk">Wallet ID</div>
                    <div class="dv"><code class="hash-full"><asp:Literal ID="litWalletId" runat="server" /></code></div>
                </div>
                <div class="detail-row">
                    <div class="dk">First Address</div>
                    <div class="dv">
                        <a class="hash" href='<%# "~/Address?address=" + litFirstAddress.Text %>' runat="server">
                            <asp:Literal ID="litFirstAddress" runat="server" />
                        </a>
                    </div>
                </div>
            </div>
        </div>

        <div class="explorer-panel" style="max-width:700px; margin-top:1rem;">
            <div class="panel-header">
                <span>&#9888; Recovery Phrase — Write this down and keep it safe</span>
            </div>
            <div style="padding:1.25rem;">
                <div style="background:var(--cc-dark); border:1px solid var(--cc-border);
                            border-radius:6px; padding:1rem; font-family:monospace;
                            font-size:1rem; letter-spacing:0.05em; color:var(--cc-accent);
                            word-spacing:0.5em; line-height:2;">
                    <asp:Literal ID="litMnemonic" runat="server" />
                </div>
                <p style="color:var(--cc-red); font-size:0.8rem; margin-top:0.75rem;">
                    This phrase will not be shown again. Anyone with this phrase can access your wallet.
                </p>
            </div>
        </div>
    </asp:Panel>

</asp:Content>
