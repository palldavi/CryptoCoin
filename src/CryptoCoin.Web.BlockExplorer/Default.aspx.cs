using System;
using System.Web.UI;
using Newtonsoft.Json.Linq;

namespace CryptoCoin.Web.BlockExplorer
{
    public partial class _Default : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;

            var status  = ExplorerApiClient.GetStatus();
            var network = ExplorerApiClient.GetNetwork();
            var blocks  = ExplorerApiClient.GetLatestBlocks();

            if (status == null && network == null)
            {
                pnlError.Visible = true;
                litApiUrl.Text   = System.Configuration.ConfigurationManager
                                         .AppSettings["ExplorerBaseUrl"] ?? "http://localhost:8080";
                return;
            }

            // ── Stat cards ──────────────────────────────────────────────────
            if (status != null)
            {
                litHeight.Text   = status["height"]?.ToString()       ?? "—";
                litMempool.Text  = status["mempoolCount"]?.ToString()  ?? "—";
                litBestHash.Text = ExplorerApiClient.ShortHash(status["bestBlockHash"]?.ToString());
                litBestTime.Text = ExplorerApiClient.TimeAgo(
                    status["bestBlockTime"] != null ? (long)status["bestBlockTime"] : 0);
            }

            if (network != null)
            {
                litDifficulty.Text = network["difficulty"] != null
                    ? $"{(double)network["difficulty"]:N4}" : "—";
                litHashRate.Text = network["hashRate"] != null
                    ? ExplorerApiClient.FormatHashRate((double)network["hashRate"]) : "—";
            }

            // ── Latest blocks table ─────────────────────────────────────────
            if (blocks != null && blocks.Count > 0)
            {
                rptBlocks.DataSource = blocks;
                rptBlocks.DataBind();
            }
            else
            {
                pnlNoBlocks.Visible = true;
            }

            // ── Network info panel ──────────────────────────────────────────
            if (network != null)
            {
                pnlNetwork.Visible = true;

                litCoinName.Text    = network["coin"]?.ToString()     ?? "CryptoCoin";
                litCoinSymbol.Text  = network["symbol"]?.ToString()   ?? "CRC";
                litNetHeight.Text   = network["height"]?.ToString()   ?? "—";
                litBlockCount.Text  = network["blockCount"]?.ToString() ?? "—";
                litNetMempool.Text  = network["mempoolSize"]?.ToString() ?? "0";

                litNetDifficulty.Text = network["difficulty"] != null
                    ? $"{(double)network["difficulty"]:N6}" : "—";
                litNetHashRate.Text = network["hashRate"] != null
                    ? ExplorerApiClient.FormatHashRate((double)network["hashRate"]) : "—";
            }
        }
    }
}
