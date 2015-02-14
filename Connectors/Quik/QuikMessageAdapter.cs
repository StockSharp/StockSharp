namespace StockSharp.Quik
{
	using System;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Базовый адаптер сообщений для Quik.
	/// </summary>
	public abstract class QuikMessageAdapter : MessageAdapter<QuikSessionHolder>
	{
		/// <summary>
		/// Инициализировать <see cref="QuikMessageAdapter"/>.
		/// </summary>
		/// <param name="type">Тип адаптера.</param>
		/// <param name="sessionHolder">Контейнер для сессии.</param>
		protected QuikMessageAdapter(MessageAdapterTypes type, QuikSessionHolder sessionHolder)
			: base(type, sessionHolder)
		{
		}

		internal QuikTerminal GetTerminal()
		{
			var terminal = SessionHolder.Terminal;

			if (terminal == null)
				throw new InvalidOperationException(LocalizedStrings.Str1710);

			return terminal;
		}
	}
}