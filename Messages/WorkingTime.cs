namespace StockSharp.Messages;

/// <summary>
/// Work schedule (time, holidays etc.).
/// </summary>
[Serializable]
[DataContract]
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.WorkScheduleKey,
	Description = LocalizedStrings.WorkScheduleDescKey)]
public class WorkingTime : IPersistable
{
	/// <summary>
	/// Initializes a new instance of the <see cref="WorkingTime"/>.
	/// </summary>
	public WorkingTime()
	{
        }

	/// <summary>
	/// Is enabled.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ActiveKey,
		Description = LocalizedStrings.TaskOnKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 0)]
	public bool IsEnabled { get; set; }

	private List<WorkingTimePeriod> _periods = [];

	/// <summary>
	/// Schedule validity periods.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PeriodsKey,
		Description = LocalizedStrings.PeriodsDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 1)]
	public List<WorkingTimePeriod> Periods
	{
		get => _periods;
		set => _periods = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <summary>
	/// Working days, falling on Saturday and Sunday.
	/// </summary>
	[XmlIgnore]
	[Browsable(false)]
	public DateTime[] SpecialWorkingDays
	{
		get => [.. _specialDays.Where(p => p.Value.Length > 0).Select(p => p.Key)];
		set
		{
			//_specialWorkingDays = CheckDates(value);

			foreach (var day in CheckDates(value))
			{
				var period = this.GetPeriod(day);

				_specialDays[day] = period?.Times.ToArray() ?? [new Range<TimeSpan>(new TimeSpan(9, 0, 0), new TimeSpan(16, 0, 0))];
			}
		}
	}

	/// <summary>
	/// Holidays that fall on workdays.
	/// </summary>
	[XmlIgnore]
	[Browsable(false)]
	public DateTime[] SpecialHolidays
	{
		get => [.. _specialDays.Where(p => p.Value.Length == 0).Select(p => p.Key)];
		set
		{
			foreach (var day in CheckDates(value))
				_specialDays[day] = [];
		}
	}

	private IDictionary<DateTime, Range<TimeSpan>[]> _specialDays = new Dictionary<DateTime, Range<TimeSpan>[]>();

	/// <summary>
	/// Special working days and holidays.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SpecialDaysKey,
		Description = LocalizedStrings.SpecialDaysDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 2)]
	[XmlIgnore]
	public IDictionary<DateTime, Range<TimeSpan>[]> SpecialDays
	{
		get => _specialDays;
		set => _specialDays = value ?? throw new ArgumentNullException(nameof(value));
	}

	private bool _checkDates = true;

	private DateTime[] CheckDates(DateTime[] dates)
	{
		if (!_checkDates)
			return dates;

		if (dates is null)
			throw new ArgumentNullException(nameof(dates));

		var dupDate = dates.GroupBy(d => d).FirstOrDefault(g => g.Count() > 1);

		if (dupDate != null)
			throw new ArgumentException(LocalizedStrings.HasDuplicates.Put(dupDate.Key), nameof(dates));

		return dates;
	}

	/// <summary>
	/// Load settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public void Load(SettingsStorage storage)
	{
		_checkDates = false;

		try
		{
			IsEnabled = storage.GetValue(nameof(IsEnabled), IsEnabled);
			Periods = [.. storage.GetValue<IEnumerable<SettingsStorage>>(nameof(Periods)).Select(s => s.Load<WorkingTimePeriod>())];

			if (storage.ContainsKey(nameof(SpecialDays)))
			{
				SpecialDays.Clear();
				SpecialDays.AddRange(storage
					.GetValue<IEnumerable<SettingsStorage>>(nameof(SpecialDays))
					.Select(s => new KeyValuePair<DateTime, Range<TimeSpan>[]>
					(
						s.GetValue<DateTime>("Day"),
						[.. s.GetValue<IEnumerable<SettingsStorage>>("Periods").Select(s1 => s1.ToRange<TimeSpan>())]
					))
				);
			}
			else
			{
				SpecialWorkingDays = [.. storage.GetValue<List<DateTime>>(nameof(SpecialWorkingDays))];
				SpecialHolidays = [.. storage.GetValue<List<DateTime>>(nameof(SpecialHolidays))];
			}
		}
		finally
		{
			_checkDates = true;
		}
	}

	/// <summary>
	/// Save settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public void Save(SettingsStorage storage)
	{
		storage.SetValue(nameof(IsEnabled), IsEnabled);
		storage.SetValue(nameof(Periods), Periods.Select(p => p.Save()).ToArray());
		storage.SetValue(nameof(SpecialDays), SpecialDays.Select(p => new SettingsStorage()
			.Set("Day", p.Key)
			.Set("Periods", p.Value.Select(p1 => p1.ToStorage()).ToArray())
		).ToArray());
	}

	/// <inheritdoc />
	public override string ToString() => Periods.Select(p => p.ToString()).JoinComma();
}