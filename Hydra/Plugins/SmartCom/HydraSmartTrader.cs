namespace Ecng.Trading.Hydra.Smart
{
	using System;
	using System.Net;

	using Ecng.Trading.Hydra.Core;
	using Ecng.Trading.Smart;

	class HydraSmartTrader : SmartTrader
	{
		private readonly SecurityStorage _securityStorage;

		public HydraSmartTrader(SecurityStorage securityStorage, string login, string password, IPAddress address)
			: base(login, password, address)
		{
			if (securityStorage == null)
				throw new ArgumentNullException("securityStorage");

			_securityStorage = securityStorage;
		}
	}
}