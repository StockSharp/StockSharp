namespace StockSharp.Quik
{
	using System;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Localization;

	using StockSharp.Algo;
	using StockSharp.Messages;

	/// <summary>
	/// Базовый адаптер сообщений для Quik.
	/// </summary>
	[TargetPlatform(Languages.Russian)]
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