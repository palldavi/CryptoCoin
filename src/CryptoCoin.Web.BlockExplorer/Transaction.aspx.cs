using System;
using System.Web.UI;
using Newtonsoft.Json.Linq;

namespace CryptoCoin.Web.BlockExplorer
{
    public partial class Transaction : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;

            string txid = Request.QueryString["txid"];
            if (string.IsNullOrEmpty(txid))
            {
                pnlError.Visible = true;
                litErrorDetail.Text = " No transaction ID supplied.";
                return;
            }

            var tx = ExplorerApiClient.GetTransaction(txid);

            if (tx == null || tx["error"] != null)
            {
                pnlError.Visible = true;
                litErrorDetail.Text = " " + (tx?["error"]?.ToString() ?? "Could not reach Explorer API.");
                return;
            }

            pnlTx.Visible = true;
            Title = $"Tx {ExplorerApiClient.ShortHash(txid)}";

            litTxId.Text = txid;

            bool inMempool = tx["inMempool"] != null && (bool)tx["inMempool"];
            litStatus.Text = inMempool
                ? "<span class=\"badge-mempool\">Unconfirmed (mempool)</span>"
                : "<span class=\"badge-confirmed\">Confirmed</span>";

            int blockHeight = tx["blockHeight"] != null ? (int)tx["blockHeight"] : -1;
            if (blockHeight >= 0)
                litBlock.Text = $"<a href=\"{ResolveUrl("~/Block?height=" + blockHeight)}\" class=\"hash\">#{blockHeight}</a>";
            else
                litBlock.Text = "—";

            int confs = tx["confirmations"] != null ? (int)tx["confirmations"] : 0;
            litConfirmations.Text = confs.ToString("N0");

            bool isCoinbase = tx["coinbase"] != null && (bool)tx["coinbase"];
            litType.Text = isCoinbase
                ? "<span class=\"badge-coinbase\">Coinbase</span>"
                : "Standard";

            litInputCount.Text  = tx["inputCount"]?.ToString()  ?? "—";
            litOutputCount.Text = tx["outputCount"]?.ToString() ?? "—";

            long totalOut = tx["totalOutput"] != null ? (long)tx["totalOutput"] : 0;
            litTotalOutput.Text = ExplorerApiClient.FormatCrc(totalOut);

            litSize.Text = tx["size"] != null ? $"{tx["size"]} bytes" : "—";
        }
    }
}
