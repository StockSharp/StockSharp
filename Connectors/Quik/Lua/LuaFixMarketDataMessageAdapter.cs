namespace StockSharp.Quik.Lua
{
	using System.Security;

	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Algo;
	using StockSharp.Fix;
	using StockSharp.Messages;

	/// <summary>
	/// Адаптер сообщений для Quik LUA FIX.
	/// </summary>
	[Icon("Quik_logo.png")]
	[Doc("http://stocksharp.com/doc/html/769f74c8-6f8e-4312-a867-3dc6e8482636.htm")]
	public class LuaFixMarketDataMessageAdapter : FixMessageAdapter
	{
		/// <summary>
		/// Создать <see cref="LuaFixMarketDataMessageAdapter"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Генератор идентификаторов транзакций.</param>
		public LuaFixMarketDataMessageAdapter(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
			this.RemoveTransactionalSupport();

			Login = "quik";
			Password = "quik".To<SecureString>();
			Address = QuikTrader.DefaultLuaAddress;
			TargetCompId = "StockSharpMD";
			SenderCompId = "quik";
			//ExchangeBoard = ExchangeBoard.Forts;
			Version = FixVersions.Fix44_Lua;
			RequestAllSecurities = true;
			MarketData = FixMarketData.MarketData;
			TimeZone = TimeHelper.Moscow;
		}
	}
}