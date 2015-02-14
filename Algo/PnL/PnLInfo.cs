namespace StockSharp.Algo.PnL
{
	using System;

	using StockSharp.Messages;

	using StockSharp.Localization;

	/// <summary>
	/// Информация о сделке, закрытый ее объем и ее прибыльность.
	/// </summary>
	public class PnLInfo
	{
		/// <summary>
		/// Создать <see cref="PnLInfo"/>.
		/// </summary>
		/// <param name="trade">Собственная сделка.</param>
		/// <param name="closedVolume">Объем позиции, который был закрыт собственной сделкой.</param>
		/// <param name="pnL">Реализованная данной сделкой прибыль.</param>
		public PnLInfo(ExecutionMessage trade, decimal closedVolume, decimal pnL)
		{
			if (trade == null)
				throw new ArgumentNullException("trade");

			if (closedVolume < 0)
				throw new ArgumentOutOfRangeException("closedVolume", closedVolume, LocalizedStrings.Str946);

			Trade = trade;
			ClosedVolume = closedVolume;
			PnL = pnL;
		}

		/// <summary>
		/// Собственная сделка.
		/// </summary>
		public ExecutionMessage Trade { get; private set; }

		/// <summary>
		/// Объем позиции, который был закрыт собственной сделкой.
		/// </summary>
		/// <remarks>Например, в стратегии позиция была 2, Сделка на -5 контрактов. Закрытая позиция 2.</remarks>
		public decimal ClosedVolume { get; private set; }

		/// <summary>
		/// Реализованная данной сделкой прибыль.
		/// </summary>
		public decimal PnL { get; private set; }
	}
}