using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Xml;

namespace CryptoCoin.Web.BlockExplorer
{
    // ── Data contracts (mirror of CryptoCoin.Services.DataContracts) ────────
    // Defined here so the web project has no compile-time dependency on the
    // Services assembly — only a runtime WCF channel is needed.

    [System.Runtime.Serialization.DataContract(Namespace = "http://cryptocoin.services/2024/blockchain")]
    public class BlockData
    {
        [System.Runtime.Serialization.DataMember(Order = 1)]  public string Hash { get; set; }
        [System.Runtime.Serialization.DataMember(Order = 2)]  public int Height { get; set; }
        [System.Runtime.Serialization.DataMember(Order = 3)]  public string PreviousHash { get; set; }
        [System.Runtime.Serialization.DataMember(Order = 4)]  public string MerkleRoot { get; set; }
        [System.Runtime.Serialization.DataMember(Order = 5)]  public long Timestamp { get; set; }
        [System.Runtime.Serialization.DataMember(Order = 6)]  public long Bits { get; set; }
        [System.Runtime.Serialization.DataMember(Order = 7)]  public long Nonce { get; set; }
        [System.Runtime.Serialization.DataMember(Order = 8)]  public int TransactionCount { get; set; }
        [System.Runtime.Serialization.DataMember(Order = 9)]  public int Size { get; set; }
        [System.Runtime.Serialization.DataMember(Order = 10)] public List<string> TransactionIds { get; set; }
        public BlockData() { TransactionIds = new List<string>(); }
    }

    [System.Runtime.Serialization.DataContract(Namespace = "http://cryptocoin.services/2024/blockchain")]
    public class BlockListData
    {
        [System.Runtime.Serialization.DataMember(Order = 1)] public List<BlockData> Blocks { get; set; }
        [System.Runtime.Serialization.DataMember(Order = 2)] public int TotalCount { get; set; }
        public BlockListData() { Blocks = new List<BlockData>(); }
    }

    [System.Runtime.Serialization.DataContract(Namespace = "http://cryptocoin.services/2024/blockchain")]
    public class NetworkStatusData
    {
        [System.Runtime.Serialization.DataMember(Order = 1)]  public int Height { get; set; }
        [System.Runtime.Serialization.DataMember(Order = 2)]  public string BestBlockHash { get; set; }
        [System.Runtime.Serialization.DataMember(Order = 3)]  public long BestBlockTime { get; set; }
        [System.Runtime.Serialization.DataMember(Order = 4)]  public int BlockCount { get; set; }
        [System.Runtime.Serialization.DataMember(Order = 5)]  public int MempoolCount { get; set; }
        [System.Runtime.Serialization.DataMember(Order = 6)]  public long MempoolBytes { get; set; }
        [System.Runtime.Serialization.DataMember(Order = 7)]  public string CoinName { get; set; }
        [System.Runtime.Serialization.DataMember(Order = 8)]  public string CoinSymbol { get; set; }
        [System.Runtime.Serialization.DataMember(Order = 9)]  public double Difficulty { get; set; }
        [System.Runtime.Serialization.DataMember(Order = 10)] public double HashRate { get; set; }
    }

    [System.Runtime.Serialization.DataContract(Namespace = "http://cryptocoin.services/2024/blockchain")]
    public class MempoolData
    {
        [System.Runtime.Serialization.DataMember(Order = 1)] public int TransactionCount { get; set; }
        [System.Runtime.Serialization.DataMember(Order = 2)] public long TotalBytes { get; set; }
        [System.Runtime.Serialization.DataMember(Order = 3)] public long TotalFees { get; set; }
        [System.Runtime.Serialization.DataMember(Order = 4)] public List<MempoolEntryData> Transactions { get; set; }
        public MempoolData() { Transactions = new List<MempoolEntryData>(); }
    }

    [System.Runtime.Serialization.DataContract(Namespace = "http://cryptocoin.services/2024/blockchain")]
    public class MempoolEntryData
    {
        [System.Runtime.Serialization.DataMember(Order = 1)] public string TxId { get; set; }
        [System.Runtime.Serialization.DataMember(Order = 2)] public long Fee { get; set; }
        [System.Runtime.Serialization.DataMember(Order = 3)] public int Size { get; set; }
        [System.Runtime.Serialization.DataMember(Order = 4)] public double FeeRate { get; set; }
    }

    // ── Service contract (mirror of IBlockchainService) ──────────────────────

    [ServiceContract(Namespace = "http://cryptocoin.services/2024/blockchain",
                     Name = "IBlockchainService")]
    public interface IBlockchainServiceChannel
    {
        [OperationContract] int GetBlockCount();
        [OperationContract] string GetBestBlockHash();
        [OperationContract] BlockData GetBlock(string hash);
        [OperationContract] BlockData GetBlockByHeight(int height);
        [OperationContract] BlockListData GetLatestBlocks();
        [OperationContract] NetworkStatusData GetNetworkStatus();
        [OperationContract] MempoolData GetMempool();
    }

    // ── API key client inspector ─────────────────────────────────────────────

    /// <summary>
    /// Adds the ApiKey SOAP header to every outbound WCF request.
    /// Mirrors CryptoCoin.Services.Security.ApiKeyClientInspector.
    /// </summary>
    internal class WcfApiKeyInspector : IClientMessageInspector
    {
        private const string HeaderName = "ApiKey";
        private const string HeaderNs   = "http://cryptocoin.services/2024/security";
        private readonly string _key;

        public WcfApiKeyInspector(string key) { _key = key; }

        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            var header = MessageHeader.CreateHeader(HeaderName, HeaderNs, _key);
            request.Headers.Add(header);
            return null;
        }

        public void AfterReceiveReply(ref Message reply, object correlationState) { }
    }

    internal class WcfApiKeyBehavior : IEndpointBehavior
    {
        private readonly string _key;
        public WcfApiKeyBehavior(string key) { _key = key; }
        public void AddBindingParameters(ServiceEndpoint ep, BindingParameterCollection bp) { }
        public void ApplyDispatchBehavior(ServiceEndpoint ep, EndpointDispatcher ed) { }
        public void Validate(ServiceEndpoint ep) { }
        public void ApplyClientBehavior(ServiceEndpoint ep, ClientRuntime cr)
            => cr.ClientMessageInspectors.Add(new WcfApiKeyInspector(_key));
    }

    // ── WCF client factory ───────────────────────────────────────────────────

    /// <summary>
    /// Creates a WCF channel to IBlockchainService with the API key behavior attached.
    /// The endpoint URL and API key are read from Web.config appSettings:
    ///   WcfBlockchainServiceUrl  (default: http://localhost:8090/cryptocoin/blockchain)
    ///   WcfApiKey                (default: cryptocoin-demo-key)
    /// </summary>
    public static class WcfBlockchainClient
    {
        private static readonly string ServiceUrl =
            System.Configuration.ConfigurationManager.AppSettings["WcfBlockchainServiceUrl"]
            ?? "http://localhost:8090/cryptocoin/blockchain";

        private static readonly string ApiKey =
            System.Configuration.ConfigurationManager.AppSettings["WcfApiKey"]
            ?? "cryptocoin-demo-key";

        /// <summary>
        /// Creates a channel, executes the action, and disposes the channel.
        /// Returns the default value on any fault or communication exception.
        /// </summary>
        public static T Call<T>(Func<IBlockchainServiceChannel, T> action, T defaultValue = default)
        {
            var binding  = new BasicHttpBinding { MaxReceivedMessageSize = 10 * 1024 * 1024 };
            var endpoint = new EndpointAddress(ServiceUrl);
            var factory  = new ChannelFactory<IBlockchainServiceChannel>(binding, endpoint);
            factory.Endpoint.Behaviors.Add(new WcfApiKeyBehavior(ApiKey));

            IBlockchainServiceChannel channel = null;
            try
            {
                channel = factory.CreateChannel();
                var result = action(channel);
                ((IClientChannel)channel).Close();
                factory.Close();
                return result;
            }
            catch (FaultException ex)
            {
                System.Diagnostics.Trace.TraceError($"[WCF] FaultException: {ex.Message}");
                ((IClientChannel)channel)?.Abort();
                factory.Abort();
                return defaultValue;
            }
            catch (CommunicationException ex)
            {
                System.Diagnostics.Trace.TraceError($"[WCF] CommunicationException: {ex.Message}");
                ((IClientChannel)channel)?.Abort();
                factory.Abort();
                return defaultValue;
            }
            catch (TimeoutException ex)
            {
                System.Diagnostics.Trace.TraceError($"[WCF] TimeoutException: {ex.Message}");
                ((IClientChannel)channel)?.Abort();
                factory.Abort();
                return defaultValue;
            }
        }
    }
}
