namespace StockSharp.Algo.Risk
{
	using Ecng.Serialization;

	using StockSharp.Messages;

	/// <summary>
	/// Интерфейс, описывающий риск-правило.
	/// </summary>
	public interface IRiskRule : IPersistable
	{
		/// <summary>
		/// Заголовок.
		/// </summary>
		string Title { get; }

		/// <summary>
		/// Действие.
		/// </summary>
		RiskActions Action { get; set; }

		/// <summary>
		/// Сбросить состояние.
		/// </summary>
		void Reset();

		/// <summary>
		/// Обработать торговое сообщение.
		/// </summary>
		/// <param name="message">Торговое сообщение.</param>
		/// <returns><see langword="true"/>, если правило активировалось, иначе, <see langword="false"/>.</returns>
		bool ProcessMessage(Message message);
	}
}