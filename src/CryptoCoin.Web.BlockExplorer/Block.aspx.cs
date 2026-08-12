using System;
using System.Web.UI;
using Newtonsoft.Json.Linq;

namespace CryptoCoin.Web.BlockExplorer
{
    public partial class Block : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;

            JObject block = null;

            string hash   = Request.QueryString["hash"];
            string heightQ = Request.QueryString["height"];

            if (!string.IsNullOrEmpty(hash))
            {
                block = ExplorerApiClient.GetBlock(hash);
            }
            else if (!string.IsNullOrEmpty(heightQ) && int.TryParse(heightQ, out int h))
            {
                block = ExplorerApiClient.GetBlockByHeight(h);
            }

            if (block == null || block["error"] != null)
            {
                pnlError.Visible = true;
                litErrorDetail.Text = block?["error"]?.ToString() ?? "No hash or height supplied.";
                return;
            }

            pnlBlock.Visible = true;

            int blockHeight = block["height"] != null ? (int)block["height"] : 0;
            string blockHash = block["hash"]?.ToString() ?? "";

            litPageTitle.Text  = $"#{blockHeight}";
            litHeight.Text     = blockHeight.ToString("N0");
            litHash.Text       = blockHash;
            litMerkleRoot.Text = block["merkleRoot"]?.ToString() ?? "—";
            litTxCount.Text    = block["txcount"]?.ToString() ?? "0";
            litSize.Text       = $"{block["size"]} bytes";
            litBits.Text       = block["bits"]?.ToString() ?? "—";
            litNonce.Text      = block["nonce"]?.ToString() ?? "—";

            long ts = block["timestamp"] != null ? (long)block["timestamp"] : 0;
            litTimestamp.Text = $"{ExplorerApiClient.FormatTimestamp(ts)} ({ExplorerApiClient.TimeAgo(ts)})";

            string prevHash = block["previousHash"]?.ToString() ?? "";
            litPrevHash.Text = ExplorerApiClient.ShortHash(prevHash);
            if (!string.IsNullOrEmpty(prevHash) && prevHash != new string('0', 64))
                aPrevHash.HRef = ResolveUrl("~/Block?hash=" + prevHash);
            else
                aPrevHash.HRef = "#";

            // Prev / Next navigation
            if (blockHeight > 0)
            {
                lnkPrev.NavigateUrl = ResolveUrl($"~/Block?height={blockHeight - 1}");
                lnkPrev.Visible = true;
            }
            else
            {
                lnkPrev.Visible = false;
            }

            // Check if a next block exists
            var nextBlock = ExplorerApiClient.GetBlockByHeight(blockHeight + 1);
            if (nextBlock != null && nextBlock["error"] == null)
            {
                lnkNext.NavigateUrl = ResolveUrl($"~/Block?height={blockHeight + 1}");
                lnkNext.Visible = true;
            }
            else
            {
                lnkNext.Visible = false;
            }

            // Transaction list
            var txArray = block["transactions"] as JArray;
            if (txArray != null && txArray.Count > 0)
            {
                var txIds = new System.Collections.Generic.List<string>();
                foreach (var t in txArray)
                    txIds.Add(t.ToString());
                rptTxIds.DataSource = txIds;
                rptTxIds.DataBind();
            }
        }
    }
}
