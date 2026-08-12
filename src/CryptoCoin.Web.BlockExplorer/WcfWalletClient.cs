using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace CryptoCoin.Web.BlockExplorer
{
    // ── Wallet data contracts (mirror of CryptoCoin.Services.DataContracts) ──

    [System.Runtime.Serialization.DataContract(Namespace = "http://cryptocoin.services/2024/wallet")]
    public class CreateWalletRequest
    {
        [System.Runtime.Serialization.DataMember(Order = 1)] public string WalletName { get; set; }
        [System.Runtime.Serialization.DataMember(Order = 2)] public string Passphrase { get; set; }
    }

    [System.Runtime.Serialization.DataContract(Namespace = "http://cryptocoin.services/2024/wallet")]
    public class CreateWalletResponse
    {
        [System.Runtime.Serialization.DataMember(Order = 1)] public string WalletId { get; set; }
        [System.Runtime.Serialization.DataMember(Order = 2)] public string MnemonicPhrase { get; set; }
        [System.Runtime.Serialization.DataMember(Order = 3)] public string FirstAddress { get; set; }
        [System.Runtime.Serialization.DataMember(Order = 4)] public bool Success { get; set; }
        [System.Runtime.Serialization.DataMember(Order = 5)] public string ErrorMessage { get; set; }
    }

    [System.Runtime.Serialization.DataContract(Namespace = "http://cryptocoin.services/2024/wallet")]
    public class BalanceResponse
    {
        [System.Runtime.Serialization.DataMember(Order = 1)] public string Address { get; set; }
        [System.Runtime.Serialization.DataMember(Order = 2)] public long ConfirmedBalance { get; set; }
        [System.Runtime.Serialization.DataMember(Order = 3)] public long UnconfirmedBalance { get; set; }
        [System.Runtime.Serialization.DataMember(Order = 4)] public long TotalBalance { get; set; }
    }

    [System.Runtime.Serialization.DataContract(Namespace = "http://cryptocoin.services/2024/wallet")]
    public class NewAddressResponse
    {
        [System.Runtime.Serialization.DataMember(Order = 1)] public string Address { get; set; }
        [System.Runtime.Serialization.DataMember(Order = 2)] public string DerivationPath { get; set; }
    }

    [System.Runtime.Serialization.DataContract(Namespace = "http://cryptocoin.services/2024/wallet")]
    public class SendTransactionRequest
    {
        [System.Runtime.Serialization.DataMember(Order = 1)] public string FromAddress { get; set; }
        [System.Runtime.Serialization.DataMember(Order = 2)] public string ToAddress { get; set; }
        [System.Runtime.Serialization.DataMember(Order = 3)] public long AmountSatoshis { get; set; }
        [System.Runtime.Serialization.DataMember(Order = 4)] public long FeeSatoshis { get; set; }
    }

    [System.Runtime.Serialization.DataContract(Namespace = "http://cryptocoin.services/2024/wallet")]
    public class SendTransactionResponse
    {
        [System.Runtime.Serialization.DataMember(Order = 1)] public string TxId { get; set; }
        [System.Runtime.Serialization.DataMember(Order = 2)] public bool Success { get; set; }
        [System.Runtime.Serialization.DataMember(Order = 3)] public string ErrorMessage { get; set; }
    }

    // ── Service contract (mirror of IWalletService) ───────────────────────────

    [ServiceContract(Namespace = "http://cryptocoin.services/2024/wallet",
                     Name = "IWalletService")]
    public interface IWalletServiceChannel
    {
        [OperationContract] CreateWalletResponse CreateWallet(CreateWalletRequest request);
        [OperationContract] BalanceResponse GetBalance(string address);
        [OperationContract] NewAddressResponse GetNewAddress(string walletId);
        [OperationContract] SendTransactionResponse SendTransaction(SendTransactionRequest request);
    }

    // ── WCF wallet client factory ─────────────────────────────────────────────

    /// <summary>
    /// Creates a WCF channel to IWalletService with the API key behavior attached.
    /// Endpoint URL and API key are read from Web.config:
    ///   WcfWalletServiceUrl  (default: http://localhost:8090/cryptocoin/wallet)
    ///   WcfApiKey            (default: cryptocoin-demo-key)
    /// </summary>
    public static class WcfWalletClient
    {
        private static readonly string ServiceUrl =
            System.Configuration.ConfigurationManager.AppSettings["WcfWalletServiceUrl"]
            ?? "http://localhost:8090/cryptocoin/wallet";

        private static readonly string ApiKey =
            System.Configuration.ConfigurationManager.AppSettings["WcfApiKey"]
            ?? "cryptocoin-demo-key";

        public static T Call<T>(Func<IWalletServiceChannel, T> action, T defaultValue = default)
        {
            var binding  = new BasicHttpBinding();
            var endpoint = new EndpointAddress(ServiceUrl);
            var factory  = new ChannelFactory<IWalletServiceChannel>(binding, endpoint);
            factory.Endpoint.Behaviors.Add(new WcfApiKeyBehavior(ApiKey));

            IWalletServiceChannel channel = null;
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
                System.Diagnostics.Trace.TraceError($"[WCF Wallet] FaultException: {ex.Message}");
                ((IClientChannel)channel)?.Abort();
                factory.Abort();
                return defaultValue;
            }
            catch (CommunicationException ex)
            {
                System.Diagnostics.Trace.TraceError($"[WCF Wallet] CommunicationException: {ex.Message}");
                ((IClientChannel)channel)?.Abort();
                factory.Abort();
                return defaultValue;
            }
            catch (TimeoutException ex)
            {
                System.Diagnostics.Trace.TraceError($"[WCF Wallet] TimeoutException: {ex.Message}");
                ((IClientChannel)channel)?.Abort();
                factory.Abort();
                return defaultValue;
            }
        }
    }
}
