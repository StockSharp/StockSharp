namespace StockSharp.Algo.Strategies
{
	using Ecng.Common;

	using StockSharp.Algo.Storages;
	using StockSharp.Messages;

	/// <summary>
	/// The message adapter, automatically calculating position.
	/// </summary>
	public class StrategyPositionMessageAdapter : MessageAdapterWrapper
	{
		private readonly StrategyPositionManager _positionManager;
		private readonly StorageBuffer _buffer;

		/// <summary>
		/// Initializes a new instance of the <see cref="StrategyPositionMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">The adapter, to which messages will be directed.</param>
		/// <param name="byOrders">To calculate the position on realized volume for orders (<see langword="true" />) or by trades (<see langword="false" />).</param>
		/// <param name="buffer">Storage buffer.</param>
		public StrategyPositionMessageAdapter(IMessageAdapter innerAdapter, bool byOrders, StorageBuffer buffer)
			: base(innerAdapter)
		{
			_positionManager = new StrategyPositionManager(byOrders) { Parent = this };
			_buffer = buffer;
		}

		/// <inheritdoc />
		protected override bool OnSendInMessage(Message message)
		{
			_positionManager.ProcessMessage(message);
			return base.OnSendInMessage(message);
		}

		/// <inheritdoc />
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			PositionChangeMessage change = null;

			if (message.Type != MessageTypes.Reset)
				change = _positionManager.ProcessMessage(message);

			base.OnInnerAdapterNewOutMessage(message);

			if (change != null)
			{
				_buffer?.ProcessOutMessage(change);
				base.OnInnerAdapterNewOutMessage(change);
			}
		}

		/// <summary>
		/// Create a copy of <see cref="StrategyPositionMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone() => new StrategyPositionMessageAdapter(InnerAdapter.TypedClone(), _positionManager.ByOrders, _buffer);
	}
}