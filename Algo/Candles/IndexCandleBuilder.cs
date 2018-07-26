#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Candles.Algo
File: IndexSeriesBuilder.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
	using StockSharp.Logging;

	class IndexCandleBuilder
	{
		[DebuggerDisplay("{OpenTime} - {CloseTime}")]
		private sealed class CandleBuffer
		{
			private readonly Type _candleType;
			private int _counter;
			private readonly bool _isSparseBuffer;

			public CandleBuffer(Type candleType, DateTimeOffset openTime, DateTimeOffset closeTime, int maxCandleCount, bool isSparseBuffer)
			{
				_candleType = candleType;

				OpenTime = openTime;
				CloseTime = closeTime;
				Candles = new Candle[maxCandleCount];
				_counter = maxCandleCount;
				_isSparseBuffer = isSparseBuffer;
			}

			public DateTimeOffset OpenTime { get; private set; }
			public DateTimeOffset CloseTime { get; private set; }
			public Candle[] Candles { get; }

			public bool IsFilled => _counter <= 0;

			public void AddCandle(int securityIndex, Candle candle)
			{
				if (candle == null)
					throw new ArgumentNullException(nameof(candle));

				if (Candles[securityIndex] != null)
				{
					if (_isSparseBuffer)
						return;
					
					throw new ArgumentException(LocalizedStrings.Str654Params.Put(candle.OpenTime), nameof(candle));
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
					throw new ArgumentNullException(nameof(prevBuffer));

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
					throw new ArgumentNullException(nameof(candle));

				var filledCandle = _candleType.CreateInstance<Candle>();

				//filledCandle.Series = candle.Series;
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
		private readonly Type _candleType;
		private readonly bool _ignoreErrors;
		private readonly CachedSynchronizedOrderedDictionary<DateTimeOffset, CandleBuffer> _buffers = new CachedSynchronizedOrderedDictionary<DateTimeOffset, CandleBuffer>();
		private CandleBuffer _lastProcessBuffer;
		private readonly SynchronizedDictionary<SecurityId, int> _securityIndecies = new SynchronizedDictionary<SecurityId, int>();
		private readonly int _bufferSize;
		private CandleBuffer _sparseBuffer1;
		private CandleBuffer _sparseBuffer2;

		public IndexCandleBuilder(IndexSecurity security, Type candleType, bool ignoreErrors)
		{
			_security = security ?? throw new ArgumentNullException(nameof(security));
			_candleType = candleType ?? throw new ArgumentNullException(nameof(candleType));
			_ignoreErrors = ignoreErrors;
			FillSecurityIndecies(_security);
			_bufferSize = _securityIndecies.Values.Distinct().Count();
		}

		private void FillSecurityIndecies(BasketSecurity basketSecurity)
		{
			var index = 0;

			foreach (var security in basketSecurity.InnerSecurityIds)
			{
				_securityIndecies[security] = index;

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
					var indexCandle = candle.GetType().CreateInstance<Candle>();

					indexCandle.Security = _security;
					indexCandle.Arg = CloneArg(candle.Arg, _security);
					indexCandle.OpenTime = buffer.OpenTime;
					indexCandle.CloseTime = buffer.CloseTime;

					try
					{
						indexCandle.OpenPrice = Calculate(buffer, true, c => c.OpenPrice);
						indexCandle.ClosePrice = Calculate(buffer, true, c => c.ClosePrice);
						indexCandle.HighPrice = Calculate(buffer, true, c => c.HighPrice);
						indexCandle.LowPrice = Calculate(buffer, true, c => c.LowPrice);

						if (_security.CalculateExtended)
						{
							indexCandle.TotalVolume = Calculate(buffer, false, c => c.TotalVolume);

							indexCandle.TotalPrice = Calculate(buffer, true, c => c.TotalPrice);
							indexCandle.OpenVolume = Calculate(buffer, false, c => c.OpenVolume ?? 0);
							indexCandle.CloseVolume = Calculate(buffer, false, c => c.CloseVolume ?? 0);
							indexCandle.HighVolume = Calculate(buffer, false, c => c.HighVolume ?? 0);
							indexCandle.LowVolume = Calculate(buffer, false, c => c.LowVolume ?? 0);
						}
					}
					catch (ArithmeticException ex)
					{
						if (!_ignoreErrors)
							throw;

						ex.LogError();
						return null;
					}

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
				var buffer = _buffers.SafeAdd(candle.OpenTime, key => new CandleBuffer(_candleType, candle.OpenTime, candle.CloseTime, _bufferSize, false));
				var secId = candle.Security.ToSecurityId();
				buffer.AddCandle(_securityIndecies[secId], candle);

				if (!buffer.IsFilled)
				{
					return Enumerable.Empty<CandleBuffer>();

					// mika
					// заполняем "размазанные" буфера, чтобы определить, что первее наступит,
					// заполнится ли полностью одна из свечек,
					// или мы сможем достроить спред из "размазанных" буферов

					// TODO пока убрал, нужно больше тестов
					if (_sparseBuffer1 == null)
						_sparseBuffer1 = new CandleBuffer(_candleType, candle.OpenTime, candle.CloseTime, _bufferSize, true);

					if (!_sparseBuffer1.IsFilled)
						_sparseBuffer1.AddCandle(_securityIndecies[secId], candle);
					else
					{
						if (_sparseBuffer2 == null)
							_sparseBuffer2 = new CandleBuffer(_candleType, candle.OpenTime, candle.CloseTime, _bufferSize, true);

						_sparseBuffer2.AddCandle(_securityIndecies[secId], candle);

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

		private decimal Calculate(CandleBuffer buffer, bool isPrice, Func<Candle, decimal> getPart)
		{
			var values = buffer.Candles.Select(getPart).ToArray();

			try
			{
				return _security.Calculate(values, isPrice);
			}
			catch (ArithmeticException excp)
			{
				throw new ArithmeticException(LocalizedStrings.BuildIndexError.Put(_security, _security.InnerSecurityIds.Zip(values, (s, v) => "{0}: {1}".Put(s, v)).Join(", ")), excp);
			}
		}

		internal static object CloneArg(object arg, Security security)
		{
			if (arg == null)
				throw new ArgumentNullException(nameof(arg));

			if (security == null)
				throw new ArgumentNullException(nameof(security));

			var clone = arg;

			if (clone is ICloneable cloneable)
				clone = cloneable.Clone();

			if (clone is Unit unit)
				unit.SetSecurity(security);

			return clone;
		}
	}
}