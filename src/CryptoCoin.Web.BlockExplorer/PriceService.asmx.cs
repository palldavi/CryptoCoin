using System;
using System.Web.Services;
using System.Web.Script.Services;

namespace CryptoCoin.Web.BlockExplorer
{
    /// <summary>
    /// ASMX web service providing mock CRC/USD price data for the Block Explorer dashboard.
    ///
    /// Modernisation note: ASMX has no equivalent in .NET Core — it was replaced entirely
    /// by ASP.NET Web API (now minimal API). On .NET 10 this would be a single
    /// app.MapGet("/api/price", ...) endpoint returning JSON directly.
    ///
    /// The [ScriptService] attribute enables JSON responses consumed by jQuery $.ajax calls,
    /// which is the classic ASP.NET AJAX pattern for ASMX services.
    /// </summary>
    [WebService(Namespace = "http://cryptocoin.services/price",
                Name = "CryptoCoin Price Service",
                Description = "Provides mock CRC/USD price and market data for the Block Explorer.")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ScriptService]
    public class PriceService : WebService
    {
        // Simulated base price — in a real implementation this would come from
        // an exchange API or an on-chain oracle contract.
        private static readonly Random _rng = new Random();
        private static double _basePrice = 0.0042;
        private static DateTime _lastUpdate = DateTime.UtcNow;

        /// <summary>
        /// Returns the current simulated CRC/USD spot price.
        /// The price drifts slightly on each call to simulate market movement.
        /// </summary>
        [WebMethod(Description = "Returns the current CRC/USD spot price.")]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public PriceData GetPrice()
        {
            UpdatePrice();
            return new PriceData
            {
                Symbol        = "CRC",
                PriceUsd      = Math.Round(_basePrice, 6),
                Change24h     = Math.Round((_rng.NextDouble() * 10) - 5, 2),  // -5% to +5%
                Volume24hUsd  = Math.Round(_rng.NextDouble() * 50000, 2),
                LastUpdated   = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };
        }

        /// <summary>
        /// Returns simulated 7-day price history as an array of daily closing prices.
        /// </summary>
        [WebMethod(Description = "Returns 7-day simulated CRC/USD price history.")]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public PriceHistory GetPriceHistory()
        {
            var history = new double[7];
            double price = _basePrice * (1 + (_rng.NextDouble() * 0.2 - 0.1));
            for (int i = 6; i >= 0; i--)
            {
                history[i] = Math.Round(price, 6);
                price *= (1 + (_rng.NextDouble() * 0.06 - 0.03));  // ±3% daily drift
            }
            return new PriceHistory
            {
                Symbol  = "CRC",
                Days    = 7,
                Prices  = history,
                Labels  = GetDayLabels(7)
            };
        }

        /// <summary>
        /// Returns simulated market statistics for CRC.
        /// </summary>
        [WebMethod(Description = "Returns CRC market statistics including market cap and circulating supply.")]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public MarketStats GetMarketStats()
        {
            UpdatePrice();
            const long circulatingSupply = 1_050_000L;  // fictional circulating supply
            return new MarketStats
            {
                Symbol             = "CRC",
                PriceUsd           = Math.Round(_basePrice, 6),
                MarketCapUsd       = Math.Round(_basePrice * circulatingSupply, 2),
                CirculatingSupply  = circulatingSupply,
                MaxSupply          = 21_000_000L,
                AllTimeHighUsd     = 0.0089,
                AllTimeLowUsd      = 0.0001
            };
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static void UpdatePrice()
        {
            // Drift the price slightly every 30 seconds
            if ((DateTime.UtcNow - _lastUpdate).TotalSeconds > 30)
            {
                _basePrice *= (1 + (_rng.NextDouble() * 0.02 - 0.01));  // ±1%
                _basePrice  = Math.Max(0.0001, Math.Min(0.01, _basePrice));
                _lastUpdate = DateTime.UtcNow;
            }
        }

        private static string[] GetDayLabels(int days)
        {
            var labels = new string[days];
            for (int i = days - 1; i >= 0; i--)
                labels[days - 1 - i] = DateTime.UtcNow.AddDays(-i).ToString("MMM dd");
            return labels;
        }
    }

    // ── Response types ────────────────────────────────────────────────────────

    public class PriceData
    {
        public string Symbol       { get; set; }
        public double PriceUsd     { get; set; }
        public double Change24h    { get; set; }
        public double Volume24hUsd { get; set; }
        public string LastUpdated  { get; set; }
    }

    public class PriceHistory
    {
        public string   Symbol { get; set; }
        public int      Days   { get; set; }
        public double[] Prices { get; set; }
        public string[] Labels { get; set; }
    }

    public class MarketStats
    {
        public string Symbol            { get; set; }
        public double PriceUsd          { get; set; }
        public double MarketCapUsd      { get; set; }
        public long   CirculatingSupply { get; set; }
        public long   MaxSupply         { get; set; }
        public double AllTimeHighUsd    { get; set; }
        public double AllTimeLowUsd     { get; set; }
    }
}
