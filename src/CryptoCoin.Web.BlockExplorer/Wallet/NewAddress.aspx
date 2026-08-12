<%@ Page Title="New Address" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="NewAddress.aspx.cs" Inherits="CryptoCoin.Web.BlockExplorer.Wallet.NewAddress" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

    <div class="page-title">
        <span class="icon">&#43;</span> Generate New Address
    </div>

    <div class="explorer-panel" style="max-width:600px; margin-bottom:1.5rem;">
        <div class="panel-header"><span>New Receiving Address</span></div>
        <div style="padding:1.25rem;">
            <div style="margin-bottom:1rem;">
                <label style="display:block; color:var(--cc-muted); font-size:0.8rem; margin-bottom:0.35rem;">Wallet ID</label>
                <asp:TextBox ID="txtWalletId" runat="server" CssClass="form-control"
                    placeholder="Wallet ID from Create Wallet" style="max-width:none;" />
            </div>
            <asp:Button ID="btnGenerate" runat="server" Text="Generate Address"
                CssClass="btn-search" OnClick="btnGenerate_Click" />
        </div>
    </div>

    <asp:Panel ID="pnlError" runat="server" CssClass="explorer-alert" Visible="false">
        <strong>Could not generate address.</strong>
        <asp:Literal ID="litError" runat="server" />
    </asp:Panel>

    <asp:Panel ID="pnlResult" runat="server" Visible="false">
        <div class="explorer-panel" style="max-width:600px;">
            <div class="panel-header"><span>New Address Generated</span></div>
            <div class="detail-grid">
                <div class="detail-row">
                    <div class="dk">Address</div>
                    <div class="dv"><code class="hash-full"><asp:Literal ID="litAddress" runat="server" /></code></div>
                </div>
                <div class="detail-row">
                    <div class="dk">Derivation Path</div>
                    <div class="dv" style="font-family:monospace; color:var(--cc-muted);">
                        <asp:Literal ID="litPath" runat="server" />
                    </div>
                </div>
                <div class="detail-row">
                    <div class="dk">Actions</div>
                    <div class="dv">
                        <a class="hash" href='<%# "~/Wallet/Balance?address=" + litAddress.Text %>' runat="server">
                            Check balance &rarr;
                        </a>
                    </div>
                </div>
            </div>
        </div>
    </asp:Panel>

</asp:Content>
