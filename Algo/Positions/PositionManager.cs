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

		private SecurityId? _securityId;

		/// <summary>
		/// The security for which <see cref="Position"/> will be calculated.
		/// </summary>
		public SecurityId? SecurityId
		{
			get => _securityId;
			set
			{
				_securityId = value;

				lock (_positions.SyncRoot)
				{
					UpdatePositionValue(_positions);
				}
			}
		}

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
			get => _positions.CachedPairs;
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				lock (_positions.SyncRoot)
				{
					_byOrderPositions.Clear();

					_positions.Clear();
					_positions.AddRange(value);

					UpdatePositionValue(value);
				}
			}
		}

		private void UpdatePositionValue(IEnumerable<KeyValuePair<Tuple<SecurityId, string>, decimal>> positions)
		{
			var secId = SecurityId;

			Position = secId == null
				? positions.Sum(p => p.Value)
				: positions.Where(p => p.Key.Item1 == secId.Value).Sum(p => p.Value);
		}

		/// <summary>
		/// The event of new position occurrence in <see cref="IPositionManager.Positions"/>.
		/// </summary>
		public event Action<Tuple<SecurityId, string>, decimal> NewPosition;

		/// <summary>
		/// The event of position change in <see cref="IPositionManager.Positions"/>.
		/// </summary>
		public event Action<Tuple<SecurityId, string>, decimal> PositionChanged;

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
						var newPosition = execMsg.GetPosition(true);

						if (newPosition == null)
							break;

						bool isNew;
						decimal diff;
						decimal abs;

						lock (_positions.SyncRoot)
						{
							isNew = !_positions.TryGetValue(key, out var prev);

							if (_byOrderPositions.TryGetValue(orderId, out var oldPosition))
							{
								if (newPosition.Value != oldPosition.Item2)
									_byOrderPositions[orderId] = Tuple.Create(execMsg.Side, newPosition.Value);

								diff = newPosition.Value - oldPosition.Item2;
							}
							else
							{
								_byOrderPositions.Add(orderId, Tuple.Create(execMsg.Side, newPosition.Value));
								diff = newPosition.Value;
							}

							abs = prev + diff;

							_positions[key] = abs;

							if (SecurityId == null || SecurityId.Value == execMsg.SecurityId)
								Position += diff;
						}

						if (isNew)
							NewPosition?.Invoke(key, abs);
						else
							PositionChanged?.Invoke(key, abs);

						return diff;
					}

					if (!ByOrders && execMsg.HasTradeInfo())
					{
						var diff = execMsg.GetPosition(false);

						if (diff == null || diff == 0)
							break;

						bool isNew;
						decimal abs;

						lock (_positions.SyncRoot)
						{
							isNew = !_positions.TryGetValue(key, out var prev);
							abs = prev + diff.Value;

							_positions[key] = abs;

							if (SecurityId == null || SecurityId.Value == execMsg.SecurityId)
								Position += diff.Value;
						}

						if (isNew)
							NewPosition?.Invoke(key, abs);
						else
							PositionChanged?.Invoke(key, abs);

						return diff;
					}

					break;
				}
			}

			return null;
		}
	}
}