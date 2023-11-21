#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: WorkingTimePeriod.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.Serialization;
	using System.Xml.Serialization;
	using System.ComponentModel.DataAnnotations;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Serialization;
	using Ecng.Collections;

	using StockSharp.Localization;

	/// <summary>
	/// Schedule validity period.
	/// </summary>
	[Serializable]
	[DataContract]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ScheduleKey,
		Description = LocalizedStrings.ScheduleValidityPeriodKey)]
	public class WorkingTimePeriod : Cloneable<WorkingTimePeriod>, IPersistable
	{
		/// <summary>
		/// Schedule expiration date.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.TillKey,
			Description = LocalizedStrings.WorkingTimeTillKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public DateTime Till { get; set; }

		private List<Range<TimeSpan>> _times = new();

		/// <summary>
		/// Work schedule within day.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.ScheduleKey,
			Description = LocalizedStrings.WorkScheduleDayKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public List<Range<TimeSpan>> Times
		{
			get => _times;
			set => _times = value ?? throw new ArgumentNullException(nameof(value));
		}

		private IDictionary<DayOfWeek, Range<TimeSpan>[]> _specialDays = new Dictionary<DayOfWeek, Range<TimeSpan>[]>();

		/// <summary>
		/// Work schedule for days with different from <see cref="Times"/> schedules.
		/// </summary>
		[XmlIgnore]
		public IDictionary<DayOfWeek, Range<TimeSpan>[]> SpecialDays
		{
			get => _specialDays;
			set => _specialDays = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <summary>
		/// Create a copy of <see cref="WorkingTimePeriod"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override WorkingTimePeriod Clone()
		{
			return new WorkingTimePeriod
			{
				Till = Till,
				Times = Times.Select(t => t.Clone()).ToList(),
				SpecialDays = SpecialDays.ToDictionary(p => p.Key, p => p.Value.ToArray()),
			};
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Load(SettingsStorage storage)
		{
			Times = storage.GetValue<IEnumerable<SettingsStorage>>(nameof(Times)).Select(s => s.ToRange<TimeSpan>()).ToList();
			Till = storage.GetValue<DateTime>(nameof(Till));
			SpecialDays = storage.GetValue<IEnumerable<SettingsStorage>>(nameof(SpecialDays)).Select(s =>
				new KeyValuePair<DayOfWeek, Range<TimeSpan>[]>(
					s.GetValue<DayOfWeek>("Day"),
					s.GetValue<IEnumerable<SettingsStorage>>("Periods").Select(s1 => s1.ToRange<TimeSpan>()).ToArray()))
			.ToDictionary();
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(Times), Times.Select(r => r.ToStorage()).ToArray());
			storage.SetValue(nameof(Till), Till);
			storage.SetValue(nameof(SpecialDays), SpecialDays.Select(p => new SettingsStorage()
				.Set("Day", p.Key)
				.Set("Periods", p.Value.Select(r => r.ToStorage()).ToArray())
			).ToArray());
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return Times.Select(t => t.ToString()).JoinComma();
		}
	}
}