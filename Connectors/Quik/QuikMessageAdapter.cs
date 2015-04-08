namespace StockSharp.Quik
{
	using System;

	using Ecng.Common;
	using Ecng.Localization;

	using StockSharp.Messages;

	/// <summary>
	/// Базовый адаптер сообщений для Quik.
	/// </summary>
	[TargetPlatform(Languages.Russian)]
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