using StockSharp.BusinessEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockSharp.Terminal.Fakes
{
	public class FakeSecurityProvider : ISecurityProvider
	{
		public int Count
		{
			get
			{
				return 0;
			}
		}

		public event Action<IEnumerable<Security>> Added;
		public event Action Cleared;
		public event Action<IEnumerable<Security>> Removed;

		public void Dispose()
		{

		}

		public object GetNativeId(Security security)
		{
			return new { };
		}

		public IEnumerable<Security> Lookup(Security criteria)
		{
			return new List<Security>();
		}
	}
}
