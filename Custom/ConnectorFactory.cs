using Ecng.Serialization;
using StockSharp.Algo;
using StockSharp.Configuration;

namespace Custom;

public static class ConnectorFactory
{
    private static Connector? connector;
    private const string ConnectorFile = @"C:\Users\Woife\AppData\Roaming\Microsoft\UserSecrets\00000000-0000-0000-0000-000000000000\ConnectorFile.json";

    public static Connector GetConnector(KnownTraders knownTraders)
    {
        if (connector == null)
        {
            connector = new Connector();
            connector.Connected += Connector_Connected;
        }

        if (File.Exists(ConnectorFile))
        {
            connector.Load(ConnectorFile.Deserialize<SettingsStorage>());
        }

        connector.Connect();

        return connector;


    }

    private static void Connector_Connected()
    {
        connector.LookupSecurities(StockSharp.Messages.Extensions.LookupAllCriteriaMessage);
    }
}