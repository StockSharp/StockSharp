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
	using System.ComponentModel.DataAnnotations;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Continuous security (generally, a futures contract), containing expirable securities.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.ContinuousSecurityKey)]
	[DescriptionLoc(LocalizedStrings.Str696Key)]
	public abstract class ContinuousSecurity : BasketSecurity
	{
	}

	/// <summary>
	/// Rollover by expiration date continuous security.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.ContinuousSecurityKey)]
	[DescriptionLoc(LocalizedStrings.Str696Key)]
	[BasketCode("CE")]
	public class ExpirationContinuousSecurity : ContinuousSecurity
	{
		/// <summary>
		/// The interface describing the internal instruments collection <see cref="ExpirationJumps"/>.
		/// </summary>
		public interface IExpirationJumpList : ISynchronizedCollection<KeyValuePair<SecurityId, DateTimeOffset>>, IDictionary<SecurityId, DateTimeOffset>
		{
			/// <summary>
			/// To get the instrument for the specified expiration time.
			/// </summary>
			/// <param name="time">The expiration time.</param>
			/// <returns>Security.</returns>
			SecurityId this[DateTimeOffset time] { get; }

			/// <summary>
			/// To get the first instrument by expiration.
			/// </summary>
			/// <returns>The first instrument. If the <see cref="ExpirationJumps"/> is empty, the <see langword="null" /> will be returned.</returns>
			SecurityId FirstSecurity { get; }

			/// <summary>
			/// To get the last instrument by expiration.
			/// </summary>
			/// <returns>The last instrument. If the <see cref="ExpirationJumps"/> is empty, the <see langword="null" /> will be returned.</returns>
			SecurityId LastSecurity { get; }

			/// <summary>
			/// To get the next instrument.
			/// </summary>
			/// <param name="security">Security.</param>
			/// <returns>The next instrument. If the <paramref name="security" /> is the last instrument then <see langword="null" /> will be returned.</returns>
			SecurityId? GetNextSecurity(SecurityId security);

			/// <summary>
			/// To get the previous instrument.
			/// </summary>
			/// <param name="security">Security.</param>
			/// <returns>The previous instrument. If the <paramref name="security" /> is the first instrument then <see langword="null" /> will be returned.</returns>
			SecurityId? GetPrevSecurity(SecurityId security);
			
			/// <summary>
			/// To get the range of operation of the internal instrument.
			/// </summary>
			/// <param name="security">The internal instrument.</param>
			/// <returns>The range of operation.</returns>
			Range<DateTimeOffset> GetActivityRange(SecurityId security);
		}

		private sealed class ExpirationJumpsDictionary : SynchronizedPairSet<SecurityId, DateTimeOffset>, IExpirationJumpList
		{
			private readonly SortedDictionary<Range<DateTimeOffset>, SecurityId> _expirationRanges;
			private IEnumerator<KeyValuePair<Range<DateTimeOffset>, SecurityId>> _enumerator;
			private KeyValuePair<Range<DateTimeOffset>, SecurityId>? _current;

			public ExpirationJumpsDictionary()
			{
				Func<Range<DateTimeOffset>, Range<DateTimeOffset>, int> comparer = (r1, r2) => r1.Max.CompareTo(r2.Max);
				_expirationRanges = new SortedDictionary<Range<DateTimeOffset>, SecurityId>(comparer.ToComparer());

				InnerSecurities = ArrayHelper.Empty<SecurityId>();
			}

			public SecurityId[] InnerSecurities { get; private set; }

			public override void Add(SecurityId key, DateTimeOffset value)
			{
				lock (SyncRoot)
				{
					base.Add(key, value);
					RefreshRanges();
				}
			}

			public override bool Remove(SecurityId key)
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
					InnerSecurities = ArrayHelper.Empty<SecurityId>();

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

				_enumerator = ((IEnumerable<KeyValuePair<Range<DateTimeOffset>, SecurityId>>)new CircularBuffer<KeyValuePair<Range<DateTimeOffset>, SecurityId>>(_expirationRanges)).GetEnumerator();

				MoveNext();
			}

			private void MoveNext()
			{
				if (_enumerator.MoveNext())
					_current = _enumerator.Current;
				else
					_current = null;
			}

			public SecurityId GetSecurity(DateTimeOffset marketTime)
			{
				lock (SyncRoot)
				{
					var security = default(SecurityId);

					while (_current != null)
					{
						var kv = _current.Value;

						// зациклилось
						if (kv.Value == security)
							break;

						if (kv.Key.Contains(marketTime))
							return kv.Value;

						if (security.IsDefault())
							security = _current.Value.Value;

						MoveNext();
					}

					return default(SecurityId);
				}
			}

			private void DisposeEnumerator()
			{
				_enumerator?.Dispose();
			}

			SecurityId IExpirationJumpList.FirstSecurity => GetSecurity(DateTimeOffset.MinValue);

			SecurityId IExpirationJumpList.LastSecurity => _expirationRanges.LastOrDefault().Value;

			SecurityId? IExpirationJumpList.GetNextSecurity(SecurityId security)
			{
				lock (SyncRoot)
				{
					if (!ContainsKey(security))
						throw new ArgumentException(LocalizedStrings.Str697Params.Put(security));

					var index = InnerSecurities.IndexOf(security);
					return index == InnerSecurities.Length - 1 ? (SecurityId?)null : InnerSecurities[index + 1];
				}
			}

			SecurityId? IExpirationJumpList.GetPrevSecurity(SecurityId security)
			{
				lock (SyncRoot)
				{
					if (!ContainsKey(security))
						throw new ArgumentException(LocalizedStrings.Str697Params.Put(security));

					var index = InnerSecurities.IndexOf(security);
					return index == 0 ? (SecurityId?)null : InnerSecurities[index - 1];
				}
			}

			Range<DateTimeOffset> IExpirationJumpList.GetActivityRange(SecurityId security)
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
		/// Initializes a new instance of the <see cref="ExpirationContinuousSecurity"/>.
		/// </summary>
		public ExpirationContinuousSecurity()
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

		/// <inheritdoc />
		[Browsable(false)]
		public override IEnumerable<SecurityId> InnerSecurityIds => _expirationJumps.InnerSecurities;

		/// <summary>
		/// To get the instrument that trades for the specified exchange time.
		/// </summary>
		/// <param name="marketTime">The exchange time.</param>
		/// <returns>The instrument. If there is no instrument for the specified time then the <see langword="null" /> will be returned.</returns>
		public SecurityId GetSecurity(DateTimeOffset marketTime)
		{
			return _expirationJumps.GetSecurity(marketTime);
		}

		private const string _dateFormat = "yyyyMMddHHmmss";

		/// <inheritdoc />
		protected override string ToSerializedString()
		{
			lock (_expirationJumps.SyncRoot)
				return _expirationJumps.Select(j => $"{j.Key.ToStringId()}={j.Value.UtcDateTime.ToString(_dateFormat)}").Join(",");
		}

		/// <inheritdoc />
		protected override void FromSerializedString(string text)
		{
			lock (_expirationJumps.SyncRoot)
			{
				_expirationJumps.Clear();

				if (text.IsEmpty())
					return;

				_expirationJumps.AddRange(text.Split(",").Select(p =>
				{
					var parts = p.Split("=");
					return new KeyValuePair<SecurityId, DateTimeOffset>(parts[0].ToSecurityId(), parts[1].ToDateTime(_dateFormat).ChangeKind(DateTimeKind.Utc));
				}));
			}
		}

		///// <summary>
		///// To shift the expiration of internal instruments <see cref="ExpirationJumps"/> to a size equas to <paramref name="offset" />.
		///// </summary>
		///// <param name="offset">The size of the expiration shift.</param>
		//public void Offset(TimeSpan offset)
		//{
		//	lock (_expirationJumps.SyncRoot)
		//	{
		//		var dict = new PairSet<Security, DateTimeOffset>();

		//		foreach (var security in InnerSecurityIds)
		//		{
		//			if (security.ExpiryDate == null)
		//				throw new InvalidOperationException(LocalizedStrings.Str698Params.Put(security.Id));

		//			var expiryDate = (DateTimeOffset)security.ExpiryDate + offset;

		//			if (expiryDate > security.ExpiryDate)
		//				throw new ArgumentOutOfRangeException(nameof(offset), offset, LocalizedStrings.Str699Params.Put(security.Id, expiryDate, security.ExpiryDate));

		//			dict.Add(security, expiryDate);
		//		}

		//		_expirationJumps.Clear();
		//		_expirationJumps.AddRange(dict);
		//	}
		//}
	}

	/// <summary>
	/// Rollover by volume continuous security.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.ContinuousSecurityKey)]
	[DescriptionLoc(LocalizedStrings.Str696Key)]
	[BasketCode("CV")]
	public class VolumeContinuousSecurity : ContinuousSecurity
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="VolumeContinuousSecurity"/>.
		/// </summary>
		public VolumeContinuousSecurity()
		{
			Type = SecurityTypes.Future;
		}

		/// <summary>
		/// Instruments rollover by volume.
		/// </summary>
		public SynchronizedList<SecurityId> InnerSecurities { get; } = new SynchronizedList<SecurityId>();

		/// <summary>
		/// Use open interest for <see cref="VolumeLevel"/>.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.OIKey,
			Description = LocalizedStrings.OpenInterestKey,
			GroupName = LocalizedStrings.ContinuousSecurityKey,
			Order = 1)]
		public bool IsOpenInterest { get; set; }

		private Unit _volumeLevel = new Unit();

		/// <summary>
		/// Volume trigger causes switch to the next contract.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.VolumeKey,
			Description = LocalizedStrings.VolumeTriggerKey,
			GroupName = LocalizedStrings.ContinuousSecurityKey,
			Order = 0)]
		public Unit VolumeLevel
		{
			get => _volumeLevel;
			set => _volumeLevel = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <inheritdoc />
		[Browsable(false)]
		public override IEnumerable<SecurityId> InnerSecurityIds => InnerSecurities;

		/// <inheritdoc />
		protected override string ToSerializedString()
		{
			lock (InnerSecurities.SyncRoot)
			{
				return $"{IsOpenInterest},{VolumeLevel}," + InnerSecurities.Select(id => id.ToStringId()).Join(",");
			}
		}

		/// <inheritdoc />
		protected override void FromSerializedString(string text)
		{
			var parts = text.Split(",");

			IsOpenInterest = parts[0].To<bool>();
			VolumeLevel = parts[1].ToUnit();

			lock (InnerSecurities.SyncRoot)
			{
				InnerSecurities.Clear();
				InnerSecurities.AddRange(parts.Skip(2).Select(p => p.ToSecurityId()));
			}
		}
	}
}