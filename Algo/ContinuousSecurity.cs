namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Непрерывный инструмент (как правило, фьючерс), содержащий в себе инструменты, подверженные экспирации.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.ContinuousSecurityKey)]
	[DescriptionLoc(LocalizedStrings.Str696Key)]
	public class ContinuousSecurity : BasketSecurity
	{
		/// <summary>
		/// Интерфейс, описывающий коллекцию внутренних инструментов <see cref="ContinuousSecurity.ExpirationJumps"/>.
		/// </summary>
		public interface IExpirationJumpList : ISynchronizedCollection<KeyValuePair<Security, DateTimeOffset>>, IDictionary<Security, DateTimeOffset>
		{
			/// <summary>
			/// Получить инструмент для заданного времени экспирации.
			/// </summary>
			/// <param name="time">Время экспирации.</param>
			/// <returns>Инструмент.</returns>
			Security this[DateTimeOffset time] { get; }

			/// <summary>
			/// Получить первый по экспирации инструмент.
			/// </summary>
			/// <returns>Первый инструмент. Если <see cref="ExpirationJumps"/> пустой, то будет возвращено null.</returns>
			Security FirstSecurity { get; }

			/// <summary>
			/// Получить последний по экспирации инструмент.
			/// </summary>
			/// <returns>Последний инструмент. Если <see cref="ExpirationJumps"/> пустой, то будет возвращено null.</returns>
			Security LastSecurity { get; }

			/// <summary>
			/// Получить следующий инструмент.
			/// </summary>
			/// <param name="security">Инструмент.</param>
			/// <returns>Следующий инструмент. Если <paramref name="security"/> является последним инструментом, то будет возвращено null.</returns>
			Security GetNextSecurity(Security security);

			/// <summary>
			/// Получить предыдущий инструмент.
			/// </summary>
			/// <param name="security">Инструмент.</param>
			/// <returns>Предыдущий инструмент. Если <paramref name="security"/> является первым инструментом, то будет возвращено null.</returns>
			Security GetPrevSecurity(Security security);
			
			/// <summary>
			/// Получить диапазон действия внутреннего инструмента.
			/// </summary>
			/// <param name="security">Внутренний инструмент.</param>
			/// <returns>Диапазон действия.</returns>
			Range<DateTimeOffset> GetActivityRange(Security security);
		}

		private sealed class ExpirationJumpsDictionary : SynchronizedPairSet<Security, DateTimeOffset>, IExpirationJumpList
		{
			private readonly SortedDictionary<Range<DateTimeOffset>, Security> _expirationRanges;
			private IEnumerator<KeyValuePair<Range<DateTimeOffset>, Security>> _enumerator;
			private KeyValuePair<Range<DateTimeOffset>, Security>? _current;

			public ExpirationJumpsDictionary()
			{
				Func<Range<DateTimeOffset>, Range<DateTimeOffset>, int> comparer = (r1, r2) => r1.Max.CompareTo(r2.Max);
				_expirationRanges = new SortedDictionary<Range<DateTimeOffset>, Security>(comparer.ToComparer());

				InnerSecurities = ArrayHelper<Security>.EmptyArray;
			}

			public Security[] InnerSecurities { get; private set; }

			public override void Add(Security key, DateTimeOffset value)
			{
				lock (SyncRoot)
				{
					base.Add(key, value);
					RefreshRanges();
				}
			}

			public override bool Remove(Security key)
			{
				lock (SyncRoot)
				{
					if (base.Remove(key))
					{
						RefreshRanges();
						return true;
					}
				}
				
				return false;
			}

			public override void Clear()
			{
				lock (SyncRoot)
				{
					base.Clear();

					_expirationRanges.Clear();
					_current = null;
					InnerSecurities = ArrayHelper<Security>.EmptyArray;

					DisposeEnumerator();
				}
			}

			private void RefreshRanges()
			{
				var prevTime = DateTimeOffset.MinValue;

				_expirationRanges.Clear();

				foreach (var pair in this.OrderBy(kv => kv.Value).ToArray())
				{
					_expirationRanges.Add(new Range<DateTimeOffset>(prevTime, pair.Value), pair.Key);
					prevTime = pair.Value + TimeSpan.FromTicks(1);
				}

				InnerSecurities = _expirationRanges.Values.ToArray();

				DisposeEnumerator();

				_enumerator = ((IEnumerable<KeyValuePair<Range<DateTimeOffset>, Security>>)new CircularBuffer<KeyValuePair<Range<DateTimeOffset>, Security>>(_expirationRanges)).GetEnumerator();

				MoveNext();
			}

			private void MoveNext()
			{
				if (_enumerator.MoveNext())
					_current = _enumerator.Current;
				else
					_current = null;
			}

			public Security GetSecurity(DateTimeOffset marketTime)
			{
				lock (SyncRoot)
				{
					Security security = null;

					while (_current != null)
					{
						var kv = _current.Value;

						// зациклилось
						if (kv.Value == security)
							break;

						if (kv.Key.Contains(marketTime))
							return kv.Value;

						if (security == null)
							security = _current.Value.Value;

						MoveNext();
					}

					return null;
				}
			}

			private void DisposeEnumerator()
			{
				if (_enumerator != null)
					_enumerator.Dispose();
			}

			Security IExpirationJumpList.FirstSecurity
			{
				get { return GetSecurity(DateTimeOffset.MinValue); }
			}

			Security IExpirationJumpList.LastSecurity
			{
				get { return _expirationRanges.LastOrDefault().Value; }
			}

			Security IExpirationJumpList.GetNextSecurity(Security security)
			{
				lock (SyncRoot)
				{
					if (!ContainsKey(security))
						throw new ArgumentException(LocalizedStrings.Str697Params.Put(security));

					var index = InnerSecurities.IndexOf(security);
					return index == InnerSecurities.Length - 1 ? null : InnerSecurities[index + 1];
				}
			}

			Security IExpirationJumpList.GetPrevSecurity(Security security)
			{
				lock (SyncRoot)
				{
					if (!ContainsKey(security))
						throw new ArgumentException(LocalizedStrings.Str697Params.Put(security));

					var index = InnerSecurities.IndexOf(security);
					return index == 0 ? null : InnerSecurities[index - 1];
				}
			}

			Range<DateTimeOffset> IExpirationJumpList.GetActivityRange(Security security)
			{
				if (security == null)
					throw new ArgumentNullException("security");

				lock (SyncRoot)
				{
					return (from expirationRange in _expirationRanges
							where expirationRange.Value == security
							select expirationRange.Key).First();
				}
			}
		}

		/// <summary>
		/// Создать <see cref="ContinuousSecurity"/>.
		/// </summary>
		public ContinuousSecurity()
		{
			Type = SecurityTypes.Future;
			_expirationJumps = new ExpirationJumpsDictionary();
		}

		private readonly ExpirationJumpsDictionary _expirationJumps;

		/// <summary>
		/// Инструменты и даты перехода, при наступлении которых происходит переход на следующий инструмент.
		/// </summary>
		[Browsable(false)]
		public IExpirationJumpList ExpirationJumps
		{
			get { return _expirationJumps; }
		}

		/// <summary>
		/// Инструменты, из которых создана данная корзина.
		/// </summary>
		[Browsable(false)]
		public override IEnumerable<Security> InnerSecurities
		{
			get { return _expirationJumps.InnerSecurities; }
		}

		///// <summary>
		///// Проверить, используется ли указанный инструмент в настоящее время.
		///// </summary>
		///// <param name="security">Инструмент, который необходимо проверить.</param>
		///// <returns><see langword="true"/>, если указанный инструмент используется в настоящее время, иначе, <see langword="false"/>.</returns>
		//public override bool Contains(Security security)
		//{
		//	var innerSecurity = GetSecurity();
		//	var basket = innerSecurity as BasketSecurity;

		//	if (basket == null)
		//		return innerSecurity == security;

		//	return basket.Contains(security);
		//}

		///// <summary>
		///// Получить инструмент, который торгуется в текущий момент (текущее время вычисляется через метод <see cref="TraderHelper.GetMarketTime"/>).
		///// </summary>
		///// <returns>Инструмент. Если не существует инструмента для указанного времени, то будет возвращено null.</returns>
		//public Security GetSecurity()
		//{
		//	return GetSecurity(this.GetMarketTime());
		//}

		/// <summary>
		/// Получить инструмент, который торгуется для указанного биржевого времени.
		/// </summary>
		/// <param name="marketTime">Биржевое время.</param>
		/// <returns>Инструмент. Если не существует инструмента для указанного времени, то будет возвращено null.</returns>
		public Security GetSecurity(DateTimeOffset marketTime)
		{
			return _expirationJumps.GetSecurity(marketTime);
		}

		/// <summary>
		/// Сдвинуть экпирацию у внутренних инструментов <see cref="ExpirationJumps"/> на размер, равный <paramref name="offset"/>.
		/// </summary>
		/// <param name="offset">Размер сдвига экспирации.</param>
		public void Offset(TimeSpan offset)
		{
			lock (_expirationJumps.SyncRoot)
			{
				var dict = new PairSet<Security, DateTimeOffset>();

				foreach (var security in InnerSecurities)
				{
					if (security.ExpiryDate == null)
						throw new InvalidOperationException(LocalizedStrings.Str698Params.Put(security.Id));

					var expiryDate = (DateTimeOffset)security.ExpiryDate + offset;

					if (expiryDate > security.ExpiryDate)
						throw new ArgumentOutOfRangeException("offset", offset, LocalizedStrings.Str699Params.Put(security.Id, expiryDate, security.ExpiryDate));

					dict.Add(security, expiryDate);
				}

				_expirationJumps.Clear();
				_expirationJumps.AddRange(dict);
			}
		}
	}
}