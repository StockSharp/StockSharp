namespace StockSharp.Algo.Positions
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Messages;

	/// <summary>
	/// The position calculation manager.
	/// </summary>
	public class PositionManager : IPositionManager
	{
		private readonly Dictionary<long, Tuple<Sides, decimal>> _byOrderPositions = new Dictionary<long, Tuple<Sides, decimal>>();

		/// <summary>
		/// Initializes a new instance of the <see cref="PositionManager"/>.
		/// </summary>
		/// <param name="byOrders">To calculate the position on realized volume for orders (<see langword="true" />) or by trades (<see langword="false" />).</param>
		public PositionManager(bool byOrders)
		{
			ByOrders = byOrders;
		}

		/// <summary>
		/// To calculate the position on realized volume for orders (<see langword="true" />) or by trades (<see langword="false" />).
		/// </summary>
		public bool ByOrders { get; private set; }

		/// <summary>
		/// The position aggregate value.
		/// </summary>
		public decimal Position { get; set; }

		private readonly CachedSynchronizedDictionary<Tuple<SecurityId, string>, decimal> _positions = new CachedSynchronizedDictionary<Tuple<SecurityId, string>, decimal>();

		/// <summary>
		/// Positions, grouped by instruments and portfolios.
		/// </summary>
		public IEnumerable<KeyValuePair<Tuple<SecurityId, string>, decimal>> Positions
		{
			get { return _positions.CachedPairs; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				lock (_positions.SyncRoot)
				{
					_positions.Clear();
					_positions.AddRange(value);

					Position = value.Sum(p => p.Value);
				}
			}
		}

		/// <summary>
		/// The event of new position occurrence in <see cref="IPositionManager.Positions"/>.
		/// </summary>
		public event Action<KeyValuePair<Tuple<SecurityId, string>, decimal>> NewPosition;

		/// <summary>
		/// The event of position change in <see cref="IPositionManager.Positions"/>.
		/// </summary>
		public event Action<KeyValuePair<Tuple<SecurityId, string>, decimal>> PositionChanged;

		/// <summary>
		/// To null position.
		/// </summary>
		public virtual void Reset()
		{
			Positions = Enumerable.Empty<KeyValuePair<Tuple<SecurityId, string>, decimal>>();
		}

		/// <summary>
		/// To calculate position.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <returns>The position by order or trade.</returns>
		public decimal? ProcessMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
				{
					Reset();
					break;
				}

				case MessageTypes.Execution:
				{
					var execMsg = (ExecutionMessage)message;
					var key = Tuple.Create(execMsg.SecurityId, execMsg.PortfolioName);

					switch (execMsg.ExecutionType)
					{
						case ExecutionTypes.Order:
						{
							if (!ByOrders)
								return 0;

							var orderId = execMsg.OriginalTransactionId;
							var newPosition = execMsg.GetPosition();

							bool? isNew = null;
							decimal position;

							lock (_positions.SyncRoot)
							{
								Tuple<Sides, decimal> oldPosition;

								if (_byOrderPositions.TryGetValue(orderId, out oldPosition))
								{
									if (newPosition != oldPosition.Item2)
									{
										_byOrderPositions[orderId] = Tuple.Create(execMsg.Side, newPosition);
										isNew = false;
									}

									position = newPosition - oldPosition.Item2;
								}
								else
								{
									_byOrderPositions.Add(orderId, Tuple.Create(execMsg.Side, newPosition));
									position = newPosition;
									isNew = true;
								}

								_positions[key] = _positions.TryGetValue(key) + position;
								Position += position;
							}

							if (isNew == true)
								NewPosition.SafeInvoke(new KeyValuePair<Tuple<SecurityId, string>, decimal>(key, position));
							else if (isNew == false)
								PositionChanged.SafeInvoke(new KeyValuePair<Tuple<SecurityId, string>, decimal>(key, position));

							return position;
						}
						case ExecutionTypes.Trade:
						{
							if (ByOrders)
								return 0;

							var position = execMsg.GetPosition();

							if (position == 0)
								break;

							bool isNew;

							lock (_positions.SyncRoot)
							{
								decimal prev;
								isNew = _positions.TryGetValue(key, out prev);
								_positions[key] = prev + position;
								Position += position;
							}

							if (isNew)
								NewPosition.SafeInvoke(new KeyValuePair<Tuple<SecurityId, string>, decimal>(key, position));
							else
								PositionChanged.SafeInvoke(new KeyValuePair<Tuple<SecurityId, string>, decimal>(key, position));

							return position;
						}
					}

					break;
				}
			}

			return null;
		}
	}
}