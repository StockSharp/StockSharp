#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Services.StudioPublic
File: StudioSecurityProvider.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.Services
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Configuration;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.Studio.Core;

	sealed class StudioSecurityProvider : ISecurityProvider
	{
		private readonly CachedSynchronizedDictionary<string, Security> _securities = new CachedSynchronizedDictionary<string, Security>();

		private readonly IStudioEntityRegistry _registry;

		public StudioSecurityProvider()
		{
			_registry = ConfigManager.GetService<IStudioEntityRegistry>();
		}

		IEnumerable<Security> ISecurityProvider.Lookup(Security criteria)
		{
			if (criteria.IsLookupAll())
				return _securities.CachedValues;

			return _registry
				.Securities
				.Lookup(criteria)
				.Select(security => _securities.SafeAdd(security.Id, id => security))
				.ToArray();
		}

		object ISecurityProvider.GetNativeId(Security security)
		{
			return null;
		}

		int ISecurityProvider.Count
		{
			get { return _securities.Count; }
		}

		event Action<IEnumerable<Security>> ISecurityProvider.Added
		{
			add { }
			remove { }
		}

		event Action<IEnumerable<Security>> ISecurityProvider.Removed
		{
			add { }
			remove { }
		}

		event Action ISecurityProvider.Cleared
		{
			add { }
			remove { }
		}

		void IDisposable.Dispose()
		{
		}
	}
}