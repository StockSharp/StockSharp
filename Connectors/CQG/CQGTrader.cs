namespace StockSharp.CQG
{
	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// Реализация интерфейса <see cref="IConnector"/> для взаимодействия с системой CQG.
	/// </summary>
	public class CQGTrader : Connector
    {
		/// <summary>
		/// Создать <see cref="CQGTrader"/>.
		/// </summary>
		public CQGTrader()
		{
			var sessionHolder = new CQGSessionHolder(TransactionIdGenerator);
			SessionHolder = sessionHolder;

			TransactionAdapter = MarketDataAdapter = new CQGMessageAdapter(sessionHolder);

			ApplyMessageProcessor(MessageDirections.In, true, true);
			ApplyMessageProcessor(MessageDirections.Out, true, true);
		}
    }
}