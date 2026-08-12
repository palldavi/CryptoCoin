using System;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace CryptoCoin.Web.BlockExplorer
{
    public partial class SiteMaster : MasterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
        }

        protected void btnSearch_Click(object sender, EventArgs e)
        {
            string query = txtSearch.Text.Trim();
            if (string.IsNullOrEmpty(query)) return;

            // 64-char hex → block hash or tx id; try block first, fall back to tx
            if (query.Length == 64 && IsHex(query))
            {
                // Attempt block lookup; if the API returns an error object the Block page handles it
                Response.Redirect("~/Block?hash=" + HttpUtility.UrlEncode(query));
                return;
            }

            // Pure integer → block height
            if (int.TryParse(query, out int height))
            {
                Response.Redirect("~/Block?height=" + height);
                return;
            }

            // Anything else → treat as an address
            Response.Redirect("~/Address?address=" + HttpUtility.UrlEncode(query));
        }

        private static bool IsHex(string s)
        {
            foreach (char c in s)
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
                    return false;
            return true;
        }
    }
}