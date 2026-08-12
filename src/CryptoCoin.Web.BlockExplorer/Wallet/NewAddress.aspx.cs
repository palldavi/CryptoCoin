using System;
using System.Web.UI;

namespace CryptoCoin.Web.BlockExplorer.Wallet
{
    public partial class NewAddress : Page
    {
        protected void Page_Load(object sender, EventArgs e) { }

        protected void btnGenerate_Click(object sender, EventArgs e)
        {
            string walletId = txtWalletId.Text.Trim();
            if (string.IsNullOrEmpty(walletId))
            {
                pnlError.Visible = true;
                litError.Text    = " Please enter a Wallet ID.";
                return;
            }

            var response = WcfWalletClient.Call(
                ch => ch.GetNewAddress(walletId),
                defaultValue: null);

            if (response == null)
            {
                pnlError.Visible = true;
                litError.Text    = " WCF wallet service is unavailable. Make sure the node is running with --wcf 8090.";
                return;
            }

            pnlResult.Visible = true;
            litAddress.Text   = response.Address;
            litPath.Text      = response.DerivationPath;
        }
    }
}
