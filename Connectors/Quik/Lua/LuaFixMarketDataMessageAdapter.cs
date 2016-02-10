#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Quik.Lua.QuikPublic
File: LuaFixMarketDataMessageAdapter.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Quik.Lua
{
	using System;
	using System.IO;
	using System.Security;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Localization;

	using StockSharp.Fix;
	using StockSharp.Fix.Dialects;
	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// Адаптер сообщений для Quik LUA FIX.
	/// </summary>
	[Icon("Quik_logo.png")]
	[DisplayNameLoc("Quik LUA. Market data")]
	[Doc("http://stocksharp.com/doc/html/769f74c8-6f8e-4312-a867-3dc6e8482636.htm")]
	[TargetPlatform(Languages.Russian)]
	[CategoryLoc(LocalizedStrings.RussiaKey)]
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
			//Version = FixVersions.Fix44_Lua;
			RequestAllSecurities = true;
			//MarketData = FixMarketData.MarketData;
			//TimeZone = TimeHelper.Moscow;
		}

		/// <summary>
		/// Create FIX protocol dialect.
		/// </summary>
		/// <param name="stream">Stream.</param>
		/// <param name="idGenerator">Sequence id generator.</param>
		/// <returns>The dialect.</returns>
		protected override IFixDialect CreateDialect(Stream stream, IncrementalIdGenerator idGenerator)
		{
			return new QuikLuaDialect(SenderCompId, TargetCompId, stream, Encoding, idGenerator, HeartbeatInterval, IsResetCounter, Login, Password, () => { throw new NotSupportedException(); });
		}
	}
}