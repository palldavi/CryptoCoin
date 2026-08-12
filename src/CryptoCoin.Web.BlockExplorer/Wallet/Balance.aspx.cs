using System;
using System.Web.UI;

namespace CryptoCoin.Web.BlockExplorer.Wallet
{
    public partial class Balance : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // Pre-fill from query string so links from Address page work
            if (!IsPostBack && !string.IsNullOrEmpty(Request.QueryString["address"]))
            {
                txtAddress.Text = Request.QueryString["address"];
                LookupBalance();
            }
        }

        protected void btnLookup_Click(object sender, EventArgs e) => LookupBalance();

        private void LookupBalance()
        {
            string address = txtAddress.Text.Trim();
            if (string.IsNullOrEmpty(address)) return;

            var response = WcfWalletClient.Call(
                ch => ch.GetBalance(address),
                defaultValue: null);

            if (response == null)
            {
                pnlError.Visible = true;
                litError.Text    = " WCF wallet service is unavailable. Make sure the node is running with --wcf 8090.";
                return;
            }

            pnlResult.Visible    = true;
            litAddress.Text      = response.Address;
            litConfirmed.Text    = ExplorerApiClient.FormatCrc(response.ConfirmedBalance);
            litUnconfirmed.Text  = ExplorerApiClient.FormatCrc(response.UnconfirmedBalance);
            litTotal.Text        = ExplorerApiClient.FormatCrc(response.TotalBalance);
        }
    }
}
