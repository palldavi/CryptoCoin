using System;
using System.Web.UI;

namespace CryptoCoin.Web.BlockExplorer
{
    public partial class Blocks : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;

            var blocks = ExplorerApiClient.GetLatestBlocks();

            if (blocks == null)
            {
                pnlError.Visible = true;
                return;
            }

            litCount.Text = $"{blocks.Count} blocks shown";
            rptBlocks.DataSource = blocks;
            rptBlocks.DataBind();
        }
    }
}
