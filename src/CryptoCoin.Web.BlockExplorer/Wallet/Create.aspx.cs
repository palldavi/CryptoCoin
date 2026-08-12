using System;
using System.Web.UI;

namespace CryptoCoin.Web.BlockExplorer.Wallet
{
    public partial class Create : Page
    {
        protected void Page_Load(object sender, EventArgs e) { }

        protected void btnCreate_Click(object sender, EventArgs e)
        {
            var request = new CreateWalletRequest
            {
                WalletName = txtName.Text.Trim(),
                Passphrase = txtPassphrase.Text
            };

            var response = WcfWalletClient.Call(
                ch => ch.CreateWallet(request),
                defaultValue: null);

            if (response == null)
            {
                pnlError.Visible  = true;
                litError.Text     = " WCF wallet service is unavailable. Make sure the node is running with --wcf 8090.";
                return;
            }

            if (!response.Success)
            {
                pnlError.Visible = true;
                litError.Text    = " " + response.ErrorMessage;
                return;
            }

            pnlForm.Visible    = false;
            pnlResult.Visible  = true;
            litWalletId.Text   = response.WalletId;
            litFirstAddress.Text = response.FirstAddress;
            litMnemonic.Text   = response.MnemonicPhrase;
        }
    }
}
