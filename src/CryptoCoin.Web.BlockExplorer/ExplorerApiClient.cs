using System;
using System.Net;
using System.IO;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CryptoCoin.Web.BlockExplorer
{
    /// <summary>
    /// Thin HTTP client that calls the CryptoCoin Explorer REST API.
    /// The base URL is read from Web.config key "ExplorerBaseUrl".
    /// </summary>
    public static class ExplorerApiClient
    {
        private static readonly string BaseUrl =
            System.Configuration.ConfigurationManager.AppSettings["ExplorerBaseUrl"]
            ?? "http://localhost:8080";

        // ── raw fetch ────────────────────────────────────────────────────────

        /// <summary>Fetches a URL and returns the raw JSON string, or null on error.</summary>
        public static string Fetch(string relativeUrl)
        {
            try
            {
                var url = BaseUrl.TrimEnd('/') + relativeUrl;
                var req = (HttpWebRequest)WebRequest.Create(url);
                req.Timeout = 5000;
                req.Method = "GET";
                using (var resp = (HttpWebResponse)req.GetResponse())
                using (var sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                    return sr.ReadToEnd();
            }
            catch (WebException ex) when (ex.Response != null)
            {
                // Read the error body so callers can surface a useful message
                try
                {
                    using (var sr = new StreamReader(ex.Response.GetResponseStream(), Encoding.UTF8))
                        return sr.ReadToEnd();
                }
                catch { return null; }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Fetches and parses a URL into a JObject, or null on error.</summary>
        public static JObject FetchObject(string relativeUrl)
        {
            var json = Fetch(relativeUrl);
            if (string.IsNullOrEmpty(json)) return null;
            try { return JObject.Parse(json); }
            catch { return null; }
        }

        /// <summary>Fetches and parses a URL into a JArray, or null on error.</summary>
        public static JArray FetchArray(string relativeUrl)
        {
            var json = Fetch(relativeUrl);
            if (string.IsNullOrEmpty(json)) return null;
            try { return JArray.Parse(json); }
            catch { return null; }
        }

        // ── convenience helpers ──────────────────────────────────────────────

        public static JObject GetStatus()       => FetchObject("/api/status");
        public static JObject GetNetwork()      => FetchObject("/api/network");
        public static JArray  GetLatestBlocks() => FetchArray("/api/blocks/latest");
        public static JObject GetBlock(string hash)    => FetchObject($"/api/block/{hash}");
        public static JObject GetBlockByHeight(int h)  => FetchObject($"/api/blocks/height/{h}");
        public static JObject GetTransaction(string id) => FetchObject($"/api/tx/{id}");
        public static JObject GetAddress(string addr)   => FetchObject($"/api/address/{addr}");
        public static JObject GetMempool()      => FetchObject("/api/mempool");

        // ── formatting helpers ───────────────────────────────────────────────

        /// <summary>Converts a Unix timestamp (seconds) to a local DateTime string.</summary>
        public static string FormatTimestamp(long unixSeconds)
        {
            if (unixSeconds <= 0) return "—";
            var dt = DateTimeOffset.FromUnixTimeSeconds(unixSeconds).LocalDateTime;
            return dt.ToString("yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>Returns a human-readable age string (e.g. "3 min ago").</summary>
        public static string TimeAgo(long unixSeconds)
        {
            if (unixSeconds <= 0) return "—";
            var dt = DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime;
            var diff = DateTime.UtcNow - dt;
            if (diff.TotalSeconds < 60)  return $"{(int)diff.TotalSeconds}s ago";
            if (diff.TotalMinutes < 60)  return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalHours < 24)    return $"{(int)diff.TotalHours}h ago";
            return $"{(int)diff.TotalDays}d ago";
        }

        /// <summary>Formats a satoshi value as CRC (divide by 1e8).</summary>
        public static string FormatCrc(long satoshis)
        {
            return (satoshis / 100_000_000.0).ToString("N8") + " CRC";
        }

        /// <summary>Truncates a hash for display (first 8 + … + last 8 chars).</summary>
        public static string ShortHash(string hash)
        {
            if (string.IsNullOrEmpty(hash) || hash.Length <= 20) return hash ?? "—";
            return hash.Substring(0, 10) + "…" + hash.Substring(hash.Length - 8);
        }

        /// <summary>Formats a hash rate in H/s, KH/s, MH/s, or GH/s.</summary>
        public static string FormatHashRate(double hps)
        {
            if (hps >= 1_000_000_000) return $"{hps / 1_000_000_000:N2} GH/s";
            if (hps >= 1_000_000)     return $"{hps / 1_000_000:N2} MH/s";
            if (hps >= 1_000)         return $"{hps / 1_000:N2} KH/s";
            return $"{hps:N0} H/s";
        }
    }
}
