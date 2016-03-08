#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Positions.Algo
File: PositionManager.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
		public bool ByOrders { get; }

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
					throw new ArgumentNullException(nameof(value));

				lock (_positions.SyncRoot)
				{
					_byOrderPositions.Clear();
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

					if (ByOrders && execMsg.HasOrderInfo())
					{
						var orderId = execMsg.OriginalTransactionId;
						var newPosition = execMsg.GetPosition();

						bool isNew;
						decimal position;

						lock (_positions.SyncRoot)
						{
							decimal prev;
							isNew = _positions.TryGetValue(key, out prev);

							Tuple<Sides, decimal> oldPosition;

							if (_byOrderPositions.TryGetValue(orderId, out oldPosition))
							{
								if (newPosition != oldPosition.Item2)
									_byOrderPositions[orderId] = Tuple.Create(execMsg.Side, newPosition);

								position = newPosition - oldPosition.Item2;
							}
							else
							{
								_byOrderPositions.Add(orderId, Tuple.Create(execMsg.Side, newPosition));
								position = newPosition;
							}

							_positions[key] = prev + position;
							Position += position;
						}

						if (isNew)
							NewPosition?.Invoke(new KeyValuePair<Tuple<SecurityId, string>, decimal>(key, Position));
						else
							PositionChanged?.Invoke(new KeyValuePair<Tuple<SecurityId, string>, decimal>(key, Position));

						return position;
					}

					if (!ByOrders && execMsg.HasTradeInfo())
					{
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
							NewPosition?.Invoke(new KeyValuePair<Tuple<SecurityId, string>, decimal>(key, Position));
						else
							PositionChanged?.Invoke(new KeyValuePair<Tuple<SecurityId, string>, decimal>(key, Position));

						return position;
					}

					break;
				}
			}

			return null;
		}
	}
}