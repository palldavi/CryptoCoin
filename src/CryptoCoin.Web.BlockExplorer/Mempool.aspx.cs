using System;
using System.Web.UI;
using Newtonsoft.Json.Linq;

namespace CryptoCoin.Web.BlockExplorer
{
    public partial class Mempool : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;

            var data = ExplorerApiClient.GetMempool();

            if (data == null)
            {
                pnlError.Visible = true;
                return;
            }

            litSize.Text  = data["size"]?.ToString()  ?? "0";
            litBytes.Text = data["bytes"] != null ? $"{data["bytes"]:N0} bytes" : "—";

            long fees = data["fees"] != null ? (long)data["fees"] : 0;
            litFees.Text = ExplorerApiClient.FormatCrc(fees);

            var txs = data["transactions"] as JArray;
            if (txs != null && txs.Count > 0)
            {
                rptTxs.DataSource = txs;
                rptTxs.DataBind();
            }
            else
            {
                pnlEmpty.Visible = true;
            }
        }
    }
}
