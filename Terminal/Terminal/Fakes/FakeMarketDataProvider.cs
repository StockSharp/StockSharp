using StockSharp.BusinessEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StockSharp.Messages;

namespace StockSharp.Terminal.Fakes
{
	public class FakeMarketDataProvider : IMarketDataProvider
	{
		public event Action<Security, IEnumerable<KeyValuePair<Level1Fields, object>>, DateTimeOffset, DateTimeOffset> ValuesChanged;

		public IEnumerable<Level1Fields> GetLevel1Fields(Security security)
		{
			return new List<Level1Fields>();
		}

		public MarketDepth GetMarketDepth(Security security)
		{
			return new MarketDepth(security);
		}

		public object GetSecurityValue(Security security, Level1Fields field)
		{
			return new { };
		}
	}
}
