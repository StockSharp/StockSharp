namespace StockSharp.Sterling
{
	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// Реализация интерфейса <see cref="IConnector"/> для взаимодействия с терминалом Sterling.
	/// </summary>
	public class SterlingTrader : Connector
    {
		/// <summary>
		/// Создать <see cref="SterlingTrader"/>.
		/// </summary>
		public SterlingTrader()
		{
			SessionHolder = new SterlingSessionHolder(TransactionIdGenerator);

			ApplyMessageProcessor(MessageDirections.In, true, true);
			ApplyMessageProcessor(MessageDirections.Out, true, true);
		}
    }
}