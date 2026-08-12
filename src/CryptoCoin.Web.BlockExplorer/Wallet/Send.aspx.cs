using System;
using System.Web.UI;

namespace CryptoCoin.Web.BlockExplorer.Wallet
{
    public partial class Send : Page
    {
        protected void Page_Load(object sender, EventArgs e) { }

        protected void btnSend_Click(object sender, EventArgs e)
        {
            if (!double.TryParse(txtAmount.Text, out double amount) || amount <= 0)
            {
                pnlError.Visible = true;
                litError.Text    = " Please enter a valid amount.";
                return;
            }

            double fee = 0.00001;
            double.TryParse(txtFee.Text, out fee);

            var request = new SendTransactionRequest
            {
                FromAddress    = txtFrom.Text.Trim(),
                ToAddress      = txtTo.Text.Trim(),
                AmountSatoshis = (long)(amount * 100_000_000),
                FeeSatoshis    = (long)(fee    * 100_000_000)
            };

            var response = WcfWalletClient.Call(
                ch => ch.SendTransaction(request),
                defaultValue: null);

            if (response == null)
            {
                pnlError.Visible = true;
                litError.Text    = " WCF wallet service is unavailable. Make sure the node is running with --wcf 8090.";
                return;
            }

            if (!response.Success)
            {
                pnlError.Visible = true;
                litError.Text    = " " + response.ErrorMessage;
                return;
            }

            pnlForm.Visible    = false;
            pnlSuccess.Visible = true;
            litTxId.Text       = response.TxId ?? "pending";
        }
    }
}
