namespace StockSharp.Algo.Storages.Binary.Snapshot
{
	using System;
	using System.Runtime.InteropServices;

	using Ecng.Common;
	using Ecng.Interop;

	using StockSharp.Messages;

	/// <summary>
	/// Implementation of <see cref="ISnapshotSerializer{TKey,TMessage}"/> in binary format for <see cref="Level1ChangeMessage"/>.
	/// </summary>
	public class Level1BinarySnapshotSerializer : ISnapshotSerializer<SecurityId, Level1ChangeMessage>
	{
		private const int _snapshotSize = 1024 * 10; // 10kb

		[StructLayout(LayoutKind.Sequential, Pack = 1, Size = _snapshotSize, CharSet = CharSet.Unicode)]
		private struct Level1Snapshot
		{
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
			public string SecurityId;

			public long LastChangeServerTime;
			public long LastChangeLocalTime;

			public long LastTradeTime;
			public decimal LastTradePrice;
			public decimal LastTradeVolume;
			public sbyte LastTradeOrigin;
			public sbyte LastTradeUpDown;
			public long LastTradeId;

			public decimal BestBidPrice;
			public decimal BestAskPrice;
			public decimal BestBidVolume;
			public decimal BestAskVolume;

			public decimal BidsVolume;
			public decimal AsksVolume;

			public int BidsCount;
			public int AsksCount;

			public decimal HighBidPrice;
			public decimal LowAskPrice;

			public decimal OpenPrice;
			public decimal HighPrice;
			public decimal LowPrice;
			public decimal ClosePrice;
			public decimal Volume;

			public decimal StepPrice;
			public decimal OI;

			public decimal MinPrice;
			public decimal MaxPrice;

			public decimal MarginBuy;
			public decimal MarginSell;

			public sbyte State;

			public decimal IV;
			public decimal HV;
			public decimal TheorPrice;
			public decimal Delta;
			public decimal Gamma;
			public decimal Vega;
			public decimal Theta;
			public decimal Rho;

			public decimal AveragePrice;
			public decimal SettlementPrice;
			public decimal Change;
			public decimal AccruedCouponIncome;
			public decimal Yield;
			public decimal VWAP;

			public int TradesCount;

			public decimal Beta;
			public decimal AverageTrueRange;
			public decimal Duration;
			public decimal Turnover;
			public decimal SpreadMiddle;

			//public decimal PriceEarnings;
			//public decimal ForwardPriceEarnings;
			//public decimal PriceEarningsGrowth;
			//public decimal PriceSales;
			//public decimal PriceBook;
			//public decimal PriceCash;
			//public decimal PriceFreeCash;
			//public decimal Payout;

			//public decimal SharesOutstanding;
			//public decimal SharesFloat;
			//public decimal FloatShort;
			//public decimal ShortRatio;

			//public decimal ReturnOnAssets;
			//public decimal ReturnOnEquity;
			//public decimal ReturnOnInvestment;
			//public decimal CurrentRatio;
			//public decimal QuickRatio;
		}

		Version ISnapshotSerializer<SecurityId, Level1ChangeMessage>.Version { get; } = new Version(1, 0);

		string ISnapshotSerializer<SecurityId, Level1ChangeMessage>.FileName => "level1_snapshot.bin";

		int ISnapshotSerializer<SecurityId, Level1ChangeMessage>.GetSnapshotSize(Version version) => _snapshotSize;

		void ISnapshotSerializer<SecurityId, Level1ChangeMessage>.Serialize(Version version, Level1ChangeMessage message, byte[] buffer)
		{
			if (version == null)
				throw new ArgumentNullException(nameof(version));

			if (message == null)
				throw new ArgumentNullException(nameof(message));

			var snapshot = new Level1Snapshot
			{
				SecurityId = message.SecurityId.ToStringId(),
				LastChangeServerTime = message.ServerTime.To<long>(),
				LastChangeLocalTime = message.LocalTime.To<long>(),

				LastTradeUpDown = -1,
				LastTradeOrigin = -1,
				State = -1,
			};

			foreach (var change in message.Changes)
			{
				switch (change.Key)
				{
					case Level1Fields.OpenPrice:
						snapshot.OpenPrice = (decimal)change.Value;
						break;
					case Level1Fields.HighPrice:
						snapshot.HighPrice = (decimal)change.Value;
						break;
					case Level1Fields.LowPrice:
						snapshot.LowPrice = (decimal)change.Value;
						break;
					case Level1Fields.ClosePrice:
						snapshot.ClosePrice = (decimal)change.Value;
						break;
					case Level1Fields.StepPrice:
						snapshot.StepPrice = (decimal)change.Value;
						break;
					case Level1Fields.ImpliedVolatility:
						snapshot.IV = (decimal)change.Value;
						break;
					case Level1Fields.TheorPrice:
						snapshot.TheorPrice = (decimal)change.Value;
						break;
					case Level1Fields.OpenInterest:
						snapshot.OI = (decimal)change.Value;
						break;
					case Level1Fields.MinPrice:
						snapshot.MinPrice = (decimal)change.Value;
						break;
					case Level1Fields.MaxPrice:
						snapshot.MaxPrice = (decimal)change.Value;
						break;
					case Level1Fields.BidsVolume:
						snapshot.BidsVolume = (decimal)change.Value;
						break;
					case Level1Fields.BidsCount:
						snapshot.BidsCount = (int)change.Value;
						break;
					case Level1Fields.AsksVolume:
						snapshot.AsksVolume = (decimal)change.Value;
						break;
					case Level1Fields.AsksCount:
						snapshot.AsksCount = (int)change.Value;
						break;
					case Level1Fields.HistoricalVolatility:
						snapshot.HV = (decimal)change.Value;
						break;
					case Level1Fields.Delta:
						snapshot.Delta = (decimal)change.Value;
						break;
					case Level1Fields.Gamma:
						snapshot.Gamma = (decimal)change.Value;
						break;
					case Level1Fields.Vega:
						snapshot.Vega = (decimal)change.Value;
						break;
					case Level1Fields.Theta:
						snapshot.Theta = (decimal)change.Value;
						break;
					case Level1Fields.MarginBuy:
						snapshot.MarginBuy = (decimal)change.Value;
						break;
					case Level1Fields.MarginSell:
						snapshot.MarginSell = (decimal)change.Value;
						break;
					case Level1Fields.State:
						snapshot.State = (sbyte)(SecurityStates)change.Value;
						break;
					case Level1Fields.LastTradePrice:
						snapshot.LastTradePrice = (decimal)change.Value;
						break;
					case Level1Fields.LastTradeVolume:
						snapshot.LastTradeVolume = (decimal)change.Value;
						break;
					case Level1Fields.Volume:
						snapshot.Volume = (decimal)change.Value;
						break;
					case Level1Fields.AveragePrice:
						snapshot.AveragePrice = (decimal)change.Value;
						break;
					case Level1Fields.SettlementPrice:
						snapshot.SettlementPrice = (decimal)change.Value;
						break;
					case Level1Fields.Change:
						snapshot.Change = (decimal)change.Value;
						break;
					case Level1Fields.BestBidPrice:
						snapshot.BestBidPrice = (decimal)change.Value;
						break;
					case Level1Fields.BestBidVolume:
						snapshot.BestBidVolume = (decimal)change.Value;
						break;
					case Level1Fields.BestAskPrice:
						snapshot.BestAskPrice = (decimal)change.Value;
						break;
					case Level1Fields.BestAskVolume:
						snapshot.BestAskVolume = (decimal)change.Value;
						break;
					case Level1Fields.Rho:
						snapshot.Rho = (decimal)change.Value;
						break;
					case Level1Fields.AccruedCouponIncome:
						snapshot.AccruedCouponIncome = (decimal)change.Value;
						break;
					case Level1Fields.HighBidPrice:
						snapshot.HighBidPrice = (decimal)change.Value;
						break;
					case Level1Fields.LowAskPrice:
						snapshot.LowAskPrice = (decimal)change.Value;
						break;
					case Level1Fields.Yield:
						snapshot.Yield = (decimal)change.Value;
						break;
					case Level1Fields.LastTradeTime:
						snapshot.LastTradeTime = change.Value.To<long>();
						break;
					case Level1Fields.TradesCount:
						snapshot.TradesCount = (int)change.Value;
						break;
					case Level1Fields.VWAP:
						snapshot.VWAP = (decimal)change.Value;
						break;
					case Level1Fields.LastTradeId:
						snapshot.LastTradeId = (long)change.Value;
						break;
					case Level1Fields.LastTradeUpDown:
						snapshot.LastTradeUpDown = (sbyte)((bool)change.Value == false ? 0 : 1);
						break;
					case Level1Fields.LastTradeOrigin:
						snapshot.LastTradeOrigin = (sbyte)(Sides)change.Value;
						break;
					case Level1Fields.Beta:
						snapshot.Beta = (decimal)change.Value;
						break;
					case Level1Fields.AverageTrueRange:
						snapshot.AverageTrueRange = (decimal)change.Value;
						break;
					case Level1Fields.HistoricalVolatilityMonth:
						snapshot.Beta = (decimal)change.Value;
						break;
					case Level1Fields.Duration:
						snapshot.Duration = (decimal)change.Value;
						break;
					case Level1Fields.Turnover:
						snapshot.Turnover = (decimal)change.Value;
						break;
					case Level1Fields.SpreadMiddle:
						snapshot.SpreadMiddle = (decimal)change.Value;
						break;
				}
			}

			var ptr = snapshot.StructToPtr();
			Marshal.Copy(ptr, buffer, 0, _snapshotSize);
			Marshal.FreeHGlobal(ptr);
		}

		Level1ChangeMessage ISnapshotSerializer<SecurityId, Level1ChangeMessage>.Deserialize(Version version, byte[] buffer)
		{
			if (version == null)
				throw new ArgumentNullException(nameof(version));

			// Pin the managed memory while, copy it out the data, then unpin it
			using (var handle = new GCHandle<byte[]>(buffer, GCHandleType.Pinned))
			{
				var snapshot = (Level1Snapshot)Marshal.PtrToStructure(handle.Value.AddrOfPinnedObject(), typeof(Level1Snapshot));

				var level1Msg = new Level1ChangeMessage
				{
					SecurityId = snapshot.SecurityId.ToSecurityId(),
					ServerTime = snapshot.LastChangeServerTime.To<DateTimeOffset>(),
					LocalTime = snapshot.LastChangeLocalTime.To<DateTimeOffset>(),
				};

				level1Msg
					.TryAdd(Level1Fields.LastTradePrice, snapshot.LastTradePrice)
					.TryAdd(Level1Fields.LastTradeVolume, snapshot.LastTradeVolume)
					.TryAdd(Level1Fields.LastTradeId, snapshot.LastTradeId)

					.TryAdd(Level1Fields.BestBidPrice, snapshot.BestBidPrice)
					.TryAdd(Level1Fields.BestAskPrice, snapshot.BestAskPrice)

					.TryAdd(Level1Fields.BestBidVolume, snapshot.BestBidVolume)
					.TryAdd(Level1Fields.BestAskVolume, snapshot.BestAskVolume)

					.TryAdd(Level1Fields.BidsVolume, snapshot.BidsVolume)
					.TryAdd(Level1Fields.AsksVolume, snapshot.AsksVolume)

					.Add(Level1Fields.BidsCount, snapshot.BidsCount)
					.Add(Level1Fields.AsksCount, snapshot.AsksCount)

					.TryAdd(Level1Fields.HighBidPrice, snapshot.HighBidPrice)
					.TryAdd(Level1Fields.LowAskPrice, snapshot.LowAskPrice)

					.TryAdd(Level1Fields.OpenPrice, snapshot.OpenPrice)
					.TryAdd(Level1Fields.HighPrice, snapshot.HighPrice)
					.TryAdd(Level1Fields.LowPrice, snapshot.LowPrice)
					.TryAdd(Level1Fields.ClosePrice, snapshot.ClosePrice)
					.TryAdd(Level1Fields.Volume, snapshot.Volume)

					.TryAdd(Level1Fields.StepPrice, snapshot.StepPrice)
					.TryAdd(Level1Fields.OpenInterest, snapshot.OI)

					.TryAdd(Level1Fields.MinPrice, snapshot.MinPrice)
					.TryAdd(Level1Fields.MaxPrice, snapshot.MaxPrice)

					.TryAdd(Level1Fields.MarginBuy, snapshot.MarginBuy)
					.TryAdd(Level1Fields.MarginSell, snapshot.MarginSell)

					.TryAdd(Level1Fields.ImpliedVolatility, snapshot.IV)
					.TryAdd(Level1Fields.HistoricalVolatility, snapshot.HV)
					.TryAdd(Level1Fields.TheorPrice, snapshot.TheorPrice)
					.TryAdd(Level1Fields.Delta, snapshot.Delta)
					.TryAdd(Level1Fields.Gamma, snapshot.Gamma)
					.TryAdd(Level1Fields.Vega, snapshot.Vega)
					.TryAdd(Level1Fields.Theta, snapshot.Theta)
					.TryAdd(Level1Fields.Rho, snapshot.Rho)

					.TryAdd(Level1Fields.AveragePrice, snapshot.AveragePrice)
					.TryAdd(Level1Fields.SettlementPrice, snapshot.SettlementPrice)
					.TryAdd(Level1Fields.Change, snapshot.Change)
					.TryAdd(Level1Fields.AccruedCouponIncome, snapshot.AccruedCouponIncome)
					.TryAdd(Level1Fields.Yield, snapshot.Yield)
					.TryAdd(Level1Fields.VWAP, snapshot.VWAP)
								
					.Add(Level1Fields.TradesCount, snapshot.TradesCount)
								
					.TryAdd(Level1Fields.Beta, snapshot.Beta)
					.TryAdd(Level1Fields.AverageTrueRange, snapshot.AverageTrueRange)
					.TryAdd(Level1Fields.Duration, snapshot.Duration)
					.TryAdd(Level1Fields.Turnover, snapshot.Turnover)
					.TryAdd(Level1Fields.SpreadMiddle, snapshot.SpreadMiddle);

				if (snapshot.LastTradeTime > 0)
					level1Msg.Add(Level1Fields.LastTradeTime, snapshot.LastTradeTime.To<DateTimeOffset>());

				if (snapshot.LastTradeUpDown != -1)
					level1Msg.Add(Level1Fields.LastTradeUpDown, snapshot.LastTradeUpDown == 0);

				if (snapshot.LastTradeOrigin != -1)
					level1Msg.Add(Level1Fields.LastTradeOrigin, (Sides)snapshot.LastTradeOrigin);

				if (snapshot.State != -1)
					level1Msg.Add(Level1Fields.State, (SecurityStates)snapshot.State);

				return level1Msg;
			}
		}

		SecurityId ISnapshotSerializer<SecurityId, Level1ChangeMessage>.GetKey(Level1ChangeMessage message)
		{
			return message.SecurityId;
		}

		void ISnapshotSerializer<SecurityId, Level1ChangeMessage>.Update(Level1ChangeMessage message, Level1ChangeMessage changes)
		{
			var lastTradeFound = false;
			var bestBidFound = false;
			var bestAskFound = false;

			foreach (var pair in changes.Changes)
			{
				var field = pair.Key;

				if (!lastTradeFound)
				{
					if (field.IsLastTradeField())
					{
						message.Changes.Remove(Level1Fields.LastTradeUpDown);
						message.Changes.Remove(Level1Fields.LastTradeTime);
						message.Changes.Remove(Level1Fields.LastTradeId);
						message.Changes.Remove(Level1Fields.LastTradeOrigin);
						message.Changes.Remove(Level1Fields.LastTradePrice);
						message.Changes.Remove(Level1Fields.LastTradeVolume);

						lastTradeFound = true;
					}
				}

				if (!bestBidFound)
				{
					if (field.IsBestBidField())
					{
						message.Changes.Remove(Level1Fields.BestBidPrice);
						message.Changes.Remove(Level1Fields.BestBidTime);
						message.Changes.Remove(Level1Fields.BestBidVolume);

						bestBidFound = true;
					}
				}

				if (!bestAskFound)
				{
					if (field.IsBestAskField())
					{
						message.Changes.Remove(Level1Fields.BestAskPrice);
						message.Changes.Remove(Level1Fields.BestAskTime);
						message.Changes.Remove(Level1Fields.BestAskVolume);

						bestAskFound = true;
					}
				}

				message.Changes[pair.Key] = pair.Value;
			}

			message.LocalTime = changes.LocalTime;
			message.ServerTime = changes.ServerTime;
		}

		DataType ISnapshotSerializer<SecurityId, Level1ChangeMessage>.DataType => DataType.Level1;
	}
}