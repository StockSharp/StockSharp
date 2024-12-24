namespace StockSharp.Algo;

/// <summary>
/// Continuous security (generally, a futures contract), containing expirable securities.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.ContinuousSecurityKey,
	Description = LocalizedStrings.ContinuousSecurityDescKey)]
public abstract class ContinuousSecurity : BasketSecurity
{
}

/// <summary>
/// Basket securities codes.
/// </summary>
public static class BasketCodes
{
	/// <summary>
	/// <see cref="ExpirationContinuousSecurity"/>
	/// </summary>
	public const string ExpirationContinuous = "CE";

	/// <summary>
	/// <see cref="VolumeContinuousSecurity"/>
	/// </summary>
	public const string VolumeContinuous = "CV";
}

/// <summary>
/// Rollover by expiration date continuous security.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.ContinuousSecurityKey,
	Description = LocalizedStrings.ContinuousSecurityDescKey)]
[BasketCode(BasketCodes.ExpirationContinuous)]
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

			InnerSecurities = [];
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
				InnerSecurities = [];

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

			InnerSecurities = [.. _expirationRanges.Values];

			DisposeEnumerator();

			_enumerator = new CircularBuffer<KeyValuePair<Range<DateTimeOffset>, SecurityId>>(_expirationRanges.Count, [.. _expirationRanges]).GetEnumerator();

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

					if (security == default)
						security = _current.Value.Value;

					MoveNext();
				}

				return default;
			}
		}

		private void DisposeEnumerator() => _enumerator?.Dispose();

		SecurityId IExpirationJumpList.FirstSecurity => GetSecurity(DateTimeOffset.MinValue);

		SecurityId IExpirationJumpList.LastSecurity => _expirationRanges.LastOrDefault().Value;

		SecurityId? IExpirationJumpList.GetNextSecurity(SecurityId security)
		{
			lock (SyncRoot)
			{
				if (!ContainsKey(security))
					throw new ArgumentException(LocalizedStrings.NotInternalSecurity.Put(security));

				var index = InnerSecurities.IndexOf(security);
				return index == InnerSecurities.Length - 1 ? null : InnerSecurities[index + 1];
			}
		}

		SecurityId? IExpirationJumpList.GetPrevSecurity(SecurityId security)
		{
			lock (SyncRoot)
			{
				if (!ContainsKey(security))
					throw new ArgumentException(LocalizedStrings.NotInternalSecurity.Put(security));

				var index = InnerSecurities.IndexOf(security);
				return index == 0 ? null : InnerSecurities[index - 1];
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
			return _expirationJumps.Select(j => $"{j.Key.ToStringId()}={j.Value.UtcDateTime.ToString(_dateFormat)}").JoinComma();
	}

	/// <inheritdoc />
	protected override void FromSerializedString(string text)
	{
		lock (_expirationJumps.SyncRoot)
		{
			_expirationJumps.Clear();

			if (text.IsEmpty())
				return;

			_expirationJumps.AddRange(text.SplitByComma().Select(p =>
			{
				var parts = p.SplitByEqual();
				return new KeyValuePair<SecurityId, DateTimeOffset>(parts[0].ToSecurityId(), parts[1].ToDateTime(_dateFormat).UtcKind());
			}));
		}
	}
}

/// <summary>
/// Rollover by volume continuous security.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.ContinuousSecurityKey,
	Description = LocalizedStrings.ContinuousSecurityDescKey)]
[BasketCode(BasketCodes.VolumeContinuous)]
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
	public SynchronizedList<SecurityId> InnerSecurities { get; } = [];

	/// <summary>
	/// Use open interest for <see cref="VolumeLevel"/>.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.OIKey,
		Description = LocalizedStrings.OpenInterestDescKey,
		GroupName = LocalizedStrings.ContinuousSecurityKey,
		Order = 1)]
	public bool IsOpenInterest { get; set; }

	private Unit _volumeLevel = new();

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
			return $"{IsOpenInterest},{VolumeLevel}," + InnerSecurities.Select(id => id.ToStringId()).JoinComma();
		}
	}

	/// <inheritdoc />
	protected override void FromSerializedString(string text)
	{
		var parts = text.SplitByComma();

		IsOpenInterest = parts[0].To<bool>();
		VolumeLevel = parts[1].ToUnit();

		lock (InnerSecurities.SyncRoot)
		{
			InnerSecurities.Clear();
			InnerSecurities.AddRange(parts.Skip(2).Select(p => p.ToSecurityId()));
		}
	}
}