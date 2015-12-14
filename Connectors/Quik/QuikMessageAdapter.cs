#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Quik.QuikPublic
File: QuikMessageAdapter.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Quik
{
	using System;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Localization;

	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// Базовый адаптер сообщений для Quik.
	/// </summary>
	[TargetPlatform(Languages.Russian)]
	[CategoryLoc(LocalizedStrings.RussiaKey)]
	[Icon("Quik_logo.png")]
	[Doc("http://stocksharp.com/doc/html/c338d4b4-ba54-4671-9206-976c07ef655e.htm")]
	public abstract class QuikMessageAdapter : MessageAdapter
	{
		/// <summary>
		/// Инициализировать <see cref="QuikMessageAdapter"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Генератор идентификаторов транзакций.</param>
		protected QuikMessageAdapter(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
			SecurityClassInfo.FillDefault();
		}

		internal Func<QuikTerminal> GetTerminal;

		//internal QuikTerminal GetTerminal()
		//{
		//	var terminal = SessionHolder.Terminal;

		//	if (terminal == null)
		//		throw new InvalidOperationException(LocalizedStrings.Str1710);

		//	return terminal;
		//}
	}
}