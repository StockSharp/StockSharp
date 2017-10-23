namespace StockSharp.Community
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Backtesting market-data types.
	/// </summary>
	[DataContract]
	public enum StrategyBacktestMarketData
	{
		/// <summary>
		/// Ticks.
		/// </summary>
		[EnumMember]
		Ticks,

		/// <summary>
		/// Order books.
		/// </summary>
		[EnumMember]
		OrderBook,

		/// <summary>
		/// Full order log.
		/// </summary>
		[EnumMember]
		OrderLog,
	}

	/// <summary>
	/// Backtest iteration settings.
	/// </summary>
	[DataContract]
	public class StrategyBacktestIteration
	{
		/// <summary>
		/// Identifier.
		/// </summary>
		[DataMember]
		public long Id { get; set; }

		/// <summary>
		/// The backtesting identifier.
		/// </summary>
		[DataMember]
		public long BacktestId { get; set; }

		/// <summary>
		/// Begin time.
		/// </summary>
		[DataMember]
		public DateTime Begin { get; set; }

		/// <summary>
		/// End time.
		/// </summary>
		[DataMember]
		public DateTime End { get; set; }
	}

	/// <summary>
	/// Backtesting session.
	/// </summary>
	[DataContract]
	public class StrategyBacktest
	{
		/// <summary>
		/// Identifier.
		/// </summary>
		[DataMember]
		public long Id { get; set; }

		/// <summary>
		/// The strategy identifier.
		/// </summary>
		[DataMember]
		public long StrategyId { get; set; }

		/// <summary>
		/// Backtesting market-data type.
		/// </summary>
		[DataMember]
		public StrategyBacktestMarketData MarketData { get; set; }

		/// <summary>
		/// The security identifier.
		/// </summary>
		[DataMember]
		public string SecurityId { get; set; }

		/// <summary>
		/// Iterations.
		/// </summary>
		[DataMember]
		public StrategyBacktestIteration[] Iterations { get; set; }

		/// <summary>
		/// Max machine to allocate. If <see langword="null"/> the allocation will be automatically.
		/// </summary>
		[DataMember]
		public int? MachineCount { get; set; }

		/// <summary>
		/// The maximum possible amount of money to spend.
		/// </summary>
		[DataMember]
		public decimal MaxAmount { get; set; }

		/// <summary>
		/// Is the <see cref="MaxAmount"/> in USD.
		/// </summary>
		[DataMember]
		public bool IsMaxAmountUsd { get; set; }

		/// <summary>
		/// Date of creation.
		/// </summary>
		[DataMember]
		public DateTime CreationDate { get; set; }
	}
}