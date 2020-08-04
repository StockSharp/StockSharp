namespace StockSharp.Algo.Strategies.Messages
{
	using StockSharp.Messages;

	/// <summary>
	/// Strategy data types.
	/// </summary>
	public static class StrategyDataType
	{
		/// <summary>
		/// <see cref="StrategyInfoMessage"/>.
		/// </summary>
		public static DataType Info { get; } = DataType.Create(typeof(StrategyInfoMessage), null).Immutable();
		
		/// <summary>
		/// <see cref="StrategyStateMessage"/>.
		/// </summary>
		public static DataType State { get; } = DataType.Create(typeof(StrategyStateMessage), null).Immutable();
	}
}