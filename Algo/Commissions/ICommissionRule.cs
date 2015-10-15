namespace StockSharp.Algo.Commissions
{
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// The commission calculating rule interface.
	/// </summary>
	public interface ICommissionRule : IPersistable
	{
		/// <summary>
		/// Header.
		/// </summary>
		string Title { get; }

		/// <summary>
		/// Total commission.
		/// </summary>
		decimal Commission { get; }

		/// <summary>
		/// Commission value.
		/// </summary>
		Unit Value { get; }

		/// <summary>
		/// To reset the state.
		/// </summary>
		void Reset();

		/// <summary>
		/// To calculate commission.
		/// </summary>
		/// <param name="message">The message containing the information about the order or own trade.</param>
		/// <returns>The commission. If the commission can not be calculated then <see langword="null" /> will be returned.</returns>
		decimal? ProcessExecution(ExecutionMessage message);
	}
}