namespace StockSharp.Algo.Candles
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	class IndexCandleBuilder
	{
		[DebuggerDisplay("{OpenTime} - {CloseTime}")]
		private sealed class CandleBuffer
		{
			private int _counter;
			private readonly bool _isSparseBuffer;

			public CandleBuffer(DateTimeOffset openTime, DateTimeOffset closeTime, int maxCandleCount, bool isSparseBuffer)
			{
				OpenTime = openTime;
				CloseTime = closeTime;
				Candles = new Candle[maxCandleCount];
				_counter = maxCandleCount;
				_isSparseBuffer = isSparseBuffer;
			}

			public DateTimeOffset OpenTime { get; private set; }
			public DateTimeOffset CloseTime { get; private set; }
			public Candle[] Candles { get; private set; }

			public bool IsFilled
			{
				get { return _counter <= 0; }
			}

			public void AddCandle(int securityIndex, Candle candle)
			{
				if (candle == null)
					throw new ArgumentNullException("candle");

				if (Candles[securityIndex] != null)
				{
					if (_isSparseBuffer)
						return;
					
					throw new ArgumentException(LocalizedStrings.Str654Params.Put(candle.OpenTime), "candle");
				}

				Candles[securityIndex] = candle;

				_counter--;

				if (_isSparseBuffer)
				{
					if (candle.OpenTime < OpenTime)
					{
						OpenTime = candle.OpenTime;
						OpenTime = Candles.Where(c => c != null).Min(c => c.OpenTime);
					}

					if (candle.CloseTime > CloseTime)
					{
						CloseTime = candle.CloseTime;
						CloseTime = Candles.Where(c => c != null).Max(c => c.CloseTime);
					}
				}
			}

			public void Fill(CandleBuffer prevBuffer)
			{
				if (prevBuffer == null)
					throw new ArgumentNullException("prevBuffer");

				for (var i = 0; i < Candles.Length; i++)
				{
					if (Candles[i] == null && prevBuffer.Candles[i] != null)
					{
						Candles[i] = CreateFilledCandle(prevBuffer.Candles[i]);
						_counter--;
					}

					//if (Candles[i] == null)
					//	throw new ArgumentException("Предыдущий буфер так же не содержит свечи.", "prevBuffer");
				}
			}

			private Candle CreateFilledCandle(Candle candle)
			{
				if (candle == null)
					throw new ArgumentNullException("candle");

				var filledCandle = candle.GetType().CreateInstance<Candle>();

				filledCandle.Series = candle.Series;
				filledCandle.Security = candle.Security;
				filledCandle.Arg = CloneArg(candle.Arg, candle.Security);
				filledCandle.OpenTime = candle.OpenTime;
				filledCandle.CloseTime = candle.CloseTime;

				//filledCandle.TotalVolume = candle.TotalVolume;
				//filledCandle.TotalPrice = candle.TotalPrice;
				filledCandle.OpenPrice = candle.ClosePrice;
				//filledCandle.OpenVolume = candle.CloseVolume;
				filledCandle.ClosePrice = candle.ClosePrice;
				//filledCandle.CloseVolume = candle.CloseVolume;
				filledCandle.HighPrice = candle.ClosePrice;
				//filledCandle.HighVolume = candle.CloseVolume;
				filledCandle.LowPrice = candle.ClosePrice;
				//filledCandle.LowVolume = candle.CloseVolume;

				filledCandle.State = CandleStates.Finished;

				return filledCandle;
			}
		}

		private readonly IndexSecurity _security;
		private readonly CachedSynchronizedOrderedDictionary<DateTimeOffset, CandleBuffer> _buffers = new CachedSynchronizedOrderedDictionary<DateTimeOffset, CandleBuffer>();
		private CandleBuffer _lastProcessBuffer;
		private readonly SynchronizedDictionary<Security, int> _securityIndecies = new SynchronizedDictionary<Security, int>();
		private readonly SynchronizedDictionary<Security, Security> _securityToSecurity = new SynchronizedDictionary<Security, Security>();
		private readonly int _bufferSize;
		private CandleBuffer _sparseBuffer1;
		private CandleBuffer _sparseBuffer2;

		public IndexCandleBuilder(IndexSecurity security)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			_security = security;
			FillSecurityIndecies(_security);
			_bufferSize = _securityIndecies.Values.Distinct().Count();
		}

		private void FillSecurityIndecies(BasketSecurity basketSecurity)
		{
			var index = 0;

			foreach (var security in basketSecurity.InnerSecurities)
			{
				_securityIndecies[security] = index;
				_securityToSecurity[security] = security;

				index++;
			}
		}

		public void Reset()
		{
			_lastProcessBuffer = null;
			_buffers.Clear();
		}

		public IEnumerable<Candle> ProcessCandle(Candle candle)
		{
			return GetFormedBuffers(candle)
				.Select(buffer =>
				{
					var openPrice = TryCalculate(buffer, c => c.OpenPrice);

					if (openPrice == null)
						return null;

					var indexCandle = candle.GetType().CreateInstance<Candle>();

					indexCandle.Security = _security;
					indexCandle.Arg = CloneArg(candle.Arg, _security);
					indexCandle.OpenTime = buffer.OpenTime;
					indexCandle.CloseTime = buffer.CloseTime;

					indexCandle.TotalVolume = Calculate(buffer, c => c.TotalVolume);
					indexCandle.TotalPrice = Calculate(buffer, c => c.TotalPrice);
					indexCandle.OpenPrice = (decimal)openPrice;
					indexCandle.OpenVolume = Calculate(buffer, c => c.OpenVolume);
					indexCandle.ClosePrice = Calculate(buffer, c => c.ClosePrice);
					indexCandle.CloseVolume = Calculate(buffer, c => c.CloseVolume);
					indexCandle.HighPrice = Calculate(buffer, c => c.HighPrice);
					indexCandle.HighVolume = Calculate(buffer, c => c.HighVolume);
					indexCandle.LowPrice = Calculate(buffer, c => c.LowPrice);
					indexCandle.LowVolume = Calculate(buffer, c => c.LowVolume);

					// если некоторые свечи имеют неполные данные, то и индекс будет таким же неполным
					if (indexCandle.OpenPrice == 0 || indexCandle.HighPrice == 0 || indexCandle.LowPrice == 0 || indexCandle.ClosePrice == 0)
					{
						var nonZeroPrice = indexCandle.OpenPrice;

						if (nonZeroPrice == 0)
							nonZeroPrice = indexCandle.HighPrice;

						if (nonZeroPrice == 0)
							nonZeroPrice = indexCandle.LowPrice;

						if (nonZeroPrice == 0)
							nonZeroPrice = indexCandle.LowPrice;

						if (nonZeroPrice != 0)
						{
							if (indexCandle.OpenPrice == 0)
								indexCandle.OpenPrice = nonZeroPrice;

							if (indexCandle.HighPrice == 0)
								indexCandle.HighPrice = nonZeroPrice;

							if (indexCandle.LowPrice == 0)
								indexCandle.LowPrice = nonZeroPrice;

							if (indexCandle.ClosePrice == 0)
								indexCandle.ClosePrice = nonZeroPrice;
						}
					}

					if (indexCandle.HighPrice < indexCandle.LowPrice)
					{
						var high = indexCandle.HighPrice;

						indexCandle.HighPrice = indexCandle.LowPrice;
						indexCandle.LowPrice = high;
					}

					if (indexCandle.OpenPrice > indexCandle.HighPrice)
						indexCandle.HighPrice = indexCandle.OpenPrice;
					else if (indexCandle.OpenPrice < indexCandle.LowPrice)
						indexCandle.LowPrice = indexCandle.OpenPrice;

					if (indexCandle.ClosePrice > indexCandle.HighPrice)
						indexCandle.HighPrice = indexCandle.ClosePrice;
					else if (indexCandle.ClosePrice < indexCandle.LowPrice)
						indexCandle.LowPrice = indexCandle.ClosePrice;

					indexCandle.State = CandleStates.Finished;

					return indexCandle;
				})
				.Where(c => c != null);
		}

		private IEnumerable<CandleBuffer> GetFormedBuffers(Candle candle)
		{
			var buffers = new List<CandleBuffer>();

			lock (_buffers.SyncRoot)
			{
				var buffer = _buffers.SafeAdd(candle.OpenTime, key => new CandleBuffer(candle.OpenTime, candle.CloseTime, _bufferSize, false));
				buffer.AddCandle(_securityIndecies[candle.Series.Security], candle);

				if (!buffer.IsFilled)
				{
					return Enumerable.Empty<CandleBuffer>();

					// mika
					// заполняем "размазанные" буфера, чтобы определить, что первее наступит,
					// заполнится ли полностью одна из свечек,
					// или мы сможем достроить спред из "размазанных" буферов

					// TODO пока убрал, нужно больше тестов
					if (_sparseBuffer1 == null)
						_sparseBuffer1 = new CandleBuffer(candle.OpenTime, candle.CloseTime, _bufferSize, true);

					if (!_sparseBuffer1.IsFilled)
						_sparseBuffer1.AddCandle(_securityIndecies[candle.Security], candle);
					else
					{
						if (_sparseBuffer2 == null)
							_sparseBuffer2 = new CandleBuffer(candle.OpenTime, candle.CloseTime, _bufferSize, true);

						_sparseBuffer2.AddCandle(_securityIndecies[candle.Security], candle);

						// если первая свеча будет построена по размазанному буферу, то разница между временем эти буферов
						// должна гарантировать, что между ними 
						//if (_lastProcessBuffer == null && _sparseBuffer2.IsFilled && (_sparseBuffer1.CloseTime > _sparseBuffer2.OpenTime))
						//	return Enumerable.Empty<CandleBuffer>();
					}

					if (_sparseBuffer2 == null || !_sparseBuffer2.IsFilled)
						return Enumerable.Empty<CandleBuffer>();
				}

				// mika
				// далее идет обработка 4-рех ситуаций
				//
 				// 1. первая свеча оказалась заполненой полностью, и мы просто удаляем начальные буфера
				// так как они уже никогда не заполняться.
				//
				// 2. первая свеча оказалось размазанной, тогда так же удаляем хвосты, но при этом формируем
				// из одной размазанной свечи N (= равное кол-ву инструментов) дозаполненных.
				//
				// 3. появилась заполненная полностью свеча, и тогда дозаполняем промежутки с _lastProcessBuffer
				//
				// 4. появилась размазанная свеча, и тогда дозаполняем промежутки с _lastProcessBuffer + формируем
				// из одной размазанной свечи N (= равное кол-ву инструментов) дозаполненных.

				var firstTimeBuffer = _lastProcessBuffer == null;

				// последней буфер, до которого (включительно) можно сформировать спред по текущим данным
				var lastBuffer = buffer.IsFilled ? buffer : _buffers[_sparseBuffer2.OpenTime];

				var deleteKeys = new List<DateTimeOffset>();

				foreach (var time in _buffers.CachedKeys)
				{
					if (time >= lastBuffer.OpenTime)
						break;

					var curr = _buffers[time];

					if (firstTimeBuffer)
					{
						if (!buffer.IsFilled)
						{
							if (_lastProcessBuffer == null)
								_lastProcessBuffer = curr;
							else
							{
								_lastProcessBuffer.Fill(curr);
								_lastProcessBuffer = curr;
							}
						}
					}
					else
					{
						curr.Fill(_lastProcessBuffer);

						if (!curr.IsFilled)
							throw new InvalidOperationException(LocalizedStrings.Str655);

						_lastProcessBuffer = curr;
						buffers.Add(curr);
					}

					deleteKeys.Add(time);
				}

				if (!buffer.IsFilled)
					lastBuffer.Fill(_lastProcessBuffer);

				if (!lastBuffer.IsFilled)
					throw new InvalidOperationException(LocalizedStrings.Str656);

				deleteKeys.Add(lastBuffer.OpenTime);

				_lastProcessBuffer = lastBuffer;
				buffers.Add(lastBuffer);

				_sparseBuffer1 = _sparseBuffer2;
				_sparseBuffer2 = null;

				deleteKeys.ForEach(k => _buffers.Remove(k));
			}

			return buffers;
		}

		private decimal? TryCalculate(CandleBuffer buffer, Func<Candle, decimal> getPart)
		{
			return _security.Calculate(buffer.Candles.ToDictionary(c => _securityToSecurity[c.Series.Security], getPart));
		}

		private decimal Calculate(CandleBuffer buffer, Func<Candle, decimal> getPart)
		{
			var calculate = TryCalculate(buffer, getPart);

			if (calculate == null)
				throw new InvalidOperationException(LocalizedStrings.Str657);

			return (decimal)calculate;
		}

		private static object CloneArg(object arg, Security security)
		{
			if (arg == null)
				throw new ArgumentNullException("arg");

			if (security == null)
				throw new ArgumentNullException("security");

			var clone = arg;
			clone.DoIf<object, ICloneable>(c => clone = c.Clone());
			clone.DoIf<object, Unit>(u => u.SetSecurity(security));
			return clone;
		}
	}
}