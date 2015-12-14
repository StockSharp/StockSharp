#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Algo
File: ContinuousSecurity.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
	/// Continuous security (generally, a futures contract), containing expirable securities.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.ContinuousSecurityKey)]
	[DescriptionLoc(LocalizedStrings.Str696Key)]
	public class ContinuousSecurity : BasketSecurity
	{
		/// <summary>
		/// The interface describing the internal instruments collection <see cref="ContinuousSecurity.ExpirationJumps"/>.
		/// </summary>
		public interface IExpirationJumpList : ISynchronizedCollection<KeyValuePair<Security, DateTimeOffset>>, IDictionary<Security, DateTimeOffset>
		{
			/// <summary>
			/// To get the instrument for the specified expiration time.
			/// </summary>
			/// <param name="time">The expiration time.</param>
			/// <returns>Security.</returns>
			Security this[DateTimeOffset time] { get; }

			/// <summary>
			/// To get the first instrument by expiration.
			/// </summary>
			/// <returns>The first instrument. If the <see cref="ContinuousSecurity.ExpirationJumps"/> is empty, the <see langword="null" /> will be returned.</returns>
			Security FirstSecurity { get; }

			/// <summary>
			/// To get the last instrument by expiration.
			/// </summary>
			/// <returns>The last instrument. If the <see cref="ContinuousSecurity.ExpirationJumps"/> is empty, the <see langword="null" /> will be returned.</returns>
			Security LastSecurity { get; }

			/// <summary>
			/// To get the next instrument.
			/// </summary>
			/// <param name="security">Security.</param>
			/// <returns>The next instrument. If the <paramref name="security" /> is the last instrument then <see langword="null" /> will be returned.</returns>
			Security GetNextSecurity(Security security);

			/// <summary>
			/// To get the previous instrument.
			/// </summary>
			/// <param name="security">Security.</param>
			/// <returns>The previous instrument. If the <paramref name="security" /> is the first instrument then <see langword="null" /> will be returned.</returns>
			Security GetPrevSecurity(Security security);
			
			/// <summary>
			/// To get the range of operation of the internal instrument.
			/// </summary>
			/// <param name="security">The internal instrument.</param>
			/// <returns>The range of operation.</returns>
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

				InnerSecurities = ArrayHelper.Empty<Security>();
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
					InnerSecurities = ArrayHelper.Empty<Security>();

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

			Security IExpirationJumpList.FirstSecurity => GetSecurity(DateTimeOffset.MinValue);

			Security IExpirationJumpList.LastSecurity => _expirationRanges.LastOrDefault().Value;

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
					throw new ArgumentNullException(nameof(security));

				lock (SyncRoot)
				{
					return (from expirationRange in _expirationRanges
							where expirationRange.Value == security
							select expirationRange.Key).First();
				}
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ContinuousSecurity"/>.
		/// </summary>
		public ContinuousSecurity()
		{
			Type = SecurityTypes.Future;
			_expirationJumps = new ExpirationJumpsDictionary();
		}

		private readonly ExpirationJumpsDictionary _expirationJumps;

		/// <summary>
		/// Instruments and dates of transition at which the transition to the next instrument takes place.
		/// </summary>
		[Browsable(false)]
		public IExpirationJumpList ExpirationJumps => _expirationJumps;

		/// <summary>
		/// Instruments, from which this basket is created.
		/// </summary>
		[Browsable(false)]
		public override IEnumerable<Security> InnerSecurities => _expirationJumps.InnerSecurities;

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
		///// <returns>Инструмент. Если не существует инструмента для указанного времени, то будет возвращено <see langword="null"/>.</returns>
		//public Security GetSecurity()
		//{
		//	return GetSecurity(this.GetMarketTime());
		//}

		/// <summary>
		/// To get the instrument that trades for the specified exchange time.
		/// </summary>
		/// <param name="marketTime">The exchange time.</param>
		/// <returns>The instrument. If there is no instrument for the specified time then the <see langword="null" /> will be returned.</returns>
		public Security GetSecurity(DateTimeOffset marketTime)
		{
			return _expirationJumps.GetSecurity(marketTime);
		}

		/// <summary>
		/// To shift the expiration of internal instruments <see cref="ContinuousSecurity.ExpirationJumps"/> to a size equas to <paramref name="offset" />.
		/// </summary>
		/// <param name="offset">The size of the expiration shift.</param>
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
						throw new ArgumentOutOfRangeException(nameof(offset), offset, LocalizedStrings.Str699Params.Put(security.Id, expiryDate, security.ExpiryDate));

					dict.Add(security, expiryDate);
				}

				_expirationJumps.Clear();
				_expirationJumps.AddRange(dict);
			}
		}
	}
}