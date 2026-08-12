using System;
using System.Web.UI;

namespace CryptoCoin.Web.BlockExplorer
{
    public partial class Address : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;

            string address = Request.QueryString["address"];
            if (string.IsNullOrEmpty(address))
            {
                pnlError.Visible = true;
                litErrorDetail.Text = " No address supplied.";
                return;
            }

            var data = ExplorerApiClient.GetAddress(address);

            if (data == null || data["error"] != null)
            {
                pnlError.Visible = true;
                litErrorDetail.Text = " " + (data?["error"]?.ToString() ?? "Could not reach Explorer API.");
                return;
            }

            pnlAddress.Visible = true;
            Title = $"Address {ExplorerApiClient.ShortHash(address)}";

            litAddress.Text = address;

            long balance = data["balance"] != null ? (long)data["balance"] : 0;
            litBalance.Text = ExplorerApiClient.FormatCrc(balance);

            litTxCount.Text = data["txCount"]?.ToString() ?? "0";
        }
    }
}
