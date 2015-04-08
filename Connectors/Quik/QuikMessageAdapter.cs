namespace StockSharp.Quik
{
	using System;
	using System.ComponentModel;

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

		/// <summary>
		/// Являются ли подключения адаптеров независимыми друг от друга.
		/// </summary>
		[Browsable(false)]
		public override bool IsAdaptersIndependent
		{
			get { return true; }
		}

		/// <summary>
		/// Объединять обработчики входящих сообщений для адаптеров.
		/// </summary>
		[Browsable(false)]
		public override bool JoinInProcessors
		{
			get { return false; }
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