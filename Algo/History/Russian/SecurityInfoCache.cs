namespace StockSharp.Algo.History.Russian
{
	using System;
	using System.Globalization;
	using System.Linq;
	using System.Xml.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.IO;

	using StockSharp.Algo.Properties;

	static class SecurityInfoCache
	{
		private static readonly SynchronizedDictionary<string, CachedSynchronizedList<SecurityInfo>> _securitiesByCode = new SynchronizedDictionary<string, CachedSynchronizedList<SecurityInfo>>(StringComparer.InvariantCultureIgnoreCase);
		private static readonly SynchronizedDictionary<string, SecurityInfo> _securitiesByShortName = new SynchronizedDictionary<string, SecurityInfo>(StringComparer.InvariantCultureIgnoreCase);

		static SecurityInfoCache()
		{
			foreach (var stream in Resources.Securities.Unzip())
			{
				var root = XDocument.Load(stream).Root;

				if (root == null)
					throw new InvalidOperationException();

				var securities = CultureInfo.InvariantCulture.DoInCulture(() =>
					root.Elements().Select(
						    elem => new SecurityInfo
						    {
							    Board = elem.GetAttributeValue<string>("board"),
							    Multiplier = elem.GetAttributeValue<decimal?>("multiplier"),
							    Decimals = elem.GetAttributeValue<int?>("decimals"),
							    PriceStep = elem.GetAttributeValue<decimal?>("priceStep"),
							    Code = elem.GetAttributeValue<string>("code"),
							    ShortName = elem.GetAttributeValue<string>("shortName"),
							    Name = elem.GetAttributeValue<string>("name"),
							    Isin = elem.GetAttributeValue<string>("isin"),
							    Asset = elem.GetAttributeValue<string>("asset"),
							    Type = elem.GetAttributeValue<string>("type"),
							    Currency = elem.GetAttributeValue<string>("currency"),
							    IssueSize = elem.GetAttributeValue<decimal?>("issueSize"),
							    IssueDate = elem.GetAttributeValue<string>("issueDate").TryToDateTime("yyyyMMdd"),
							    LastDate = elem.GetAttributeValue<string>("lastDate").TryToDateTime("yyyyMMdd"),
						    })
					    .ToArray());

				foreach (var info in securities)
				{
					_securitiesByCode.SafeAdd(info.Code).Add(info);
				}

				foreach (var info in securities)
				{
					if (!info.ShortName.IsEmpty())
						_securitiesByShortName.TryAdd(info.ShortName, info);
					else if (!info.Name.IsEmpty())
						_securitiesByShortName.TryAdd(info.Name, info);
				}
			}
		}

		public static SecurityInfo[] TryGetByCode(string code)
		{
			return _securitiesByCode.TryGetValue(code)?.Cache;// ?? ArrayHelper.Empty<SecurityInfo>();
		}

		public static SecurityInfo[] TryGetByCodeAndBoard(string code, string board)
		{
			return TryGetByCode(code)?.Where(i => i.Board.CompareIgnoreCase(board)).ToArray();
		}

		public static SecurityInfo TryGetByShortName(string shortName)
		{
			return _securitiesByShortName.TryGetValue(shortName);
		}

		public static void Add(SecurityInfo info)
		{
			if (info == null)
				throw new ArgumentNullException(nameof(info));

			_securitiesByCode.SafeAdd(info.Code).Add(info);

			if (!info.ShortName.IsEmpty())
				_securitiesByShortName.TryAdd(info.ShortName, info);

			// NOTE
			//CultureInfo.InvariantCulture.DoInCulture(() => System.Diagnostics.Debug.WriteLine($"	<security code=\"{info.Code.Encode()}\" name=\"{info.Name.Encode()}\" shortName=\"{info.ShortName.Encode()}\" board=\"{info.Board}\" priceStep=\"{info.PriceStep}\" decimals=\"{info.Decimals}\" multiplier=\"{info.Multiplier}\" isin=\"{info.Isin}\" type=\"{info.Type}\" asset=\"{info.Asset.Encode()}\" currency=\"{info.Currency}\" issueSize=\"{info.IssueSize}\" issueDate=\"{info.IssueDate:yyyyMMdd}\" lastDate=\"{info.LastDate:yyyyMMdd}\" />"));
		}

		//private static string Encode(this string value)
		//{
		//	return value?.Replace("&", "&amp;").Replace("\"", "&quot;");
		//}
	}
}