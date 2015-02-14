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
			SessionHolder = new CQGSessionHolder(TransactionIdGenerator);

			ApplyMessageProcessor(MessageDirections.In, true, true);
			ApplyMessageProcessor(MessageDirections.Out, true, true);
		}
    }
}