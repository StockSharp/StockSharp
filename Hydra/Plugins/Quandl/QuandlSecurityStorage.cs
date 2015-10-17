namespace StockSharp.Hydra.Quandl
{
	using System;
	using System.Collections.Generic;

	using Ecng.Collections;

	using StockSharp.Algo.History;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Hydra.Core;

	class QuandlSecurityStorage : NativeIdSecurityStorage<Tuple<string, string>>
	{
		private class KeyComparer : IEqualityComparer<Tuple<string, string>>
		{
			public bool Equals(Tuple<string, string> x, Tuple<string, string> y)
			{
				return StringComparer.InvariantCultureIgnoreCase.Equals(x.Item1, y.Item1)
					   && StringComparer.InvariantCultureIgnoreCase.Equals(x.Item2, y.Item2);
			}

			public int GetHashCode(Tuple<string, string> obj)
			{
				return obj.GetHashCode();
			}
		}

		public QuandlSecurityStorage(IEntityRegistry entityRegistry)
			: base(entityRegistry, new KeyComparer())
		{
		}

		protected override Tuple<string, string> CreateNativeId(Security security)
		{
			var sourceCode = (string)security.ExtensionInfo.TryGetValue(QuandlHistorySource.SourceCodeField);
			var secCode = (string)security.ExtensionInfo.TryGetValue(QuandlHistorySource.SecurityCodeField);

			return Tuple.Create(sourceCode, secCode);
		}
	}
}