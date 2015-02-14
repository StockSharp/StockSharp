namespace StockSharp.OpenECry
{
	using System;

	/// <summary>
	/// Исключение, генерируемое реализацией <see cref="OECTrader"/> в случае возникновения ошибок.
	/// </summary>
	public sealed class OpenECryException : ApplicationException
	{
		/// <summary>
		/// Создать <see cref="OpenECryException"/>.
		/// </summary>
		/// <param name="msg">Текст сообщения.</param>
		/// <param name="inner">Внутреннее исключение.</param>
		internal OpenECryException(string msg, Exception inner = null)
			: base(msg, inner)
		{
		}
	}
}