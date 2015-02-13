namespace StockSharp.InteractiveBrokers
{
	using StockSharp.Messages;

	/// <summary>
	/// Результат фильтра сканера, запускаемого через <see cref="IBTrader.SubscribeScanner"/>.
	/// </summary>
	public class ScannerResult
	{
		/// <summary>
		/// Идентификатор инструмента.
		/// </summary>
		public SecurityId SecurityId { get; set; }
		
		/// <summary>
		/// Ранк.
		/// </summary>
		public int Rank { get; set; }
		
		/// <summary>
		/// Значение запроса.
		/// </summary>
		public string Distance { get; set; }

		/// <summary>
		/// Значение запроса.
		/// </summary>
		public string Benchmark { get; set; }

		/// <summary>
		/// Значение запроса.
		/// </summary>
		public string Projection { get; set; }

		/// <summary>
		/// Описание комбинированного инструмента.
		/// </summary>
		public string Legs { get; set; }
	}
}