using System.Reflection;
using Newtonsoft.Json;
namespace SciTrader.Model
{

    [Obfuscation(Feature = "renaming", ApplyToMembers = true)]
    internal class Account
    {
        [JsonProperty("account_no")]
        public string AccountCode { get; set; }

        [JsonProperty("account_name")]
        public string AccountName { get; set; }

        [JsonProperty("account_type")]
        public string AccountType { get; set; }

    }
}