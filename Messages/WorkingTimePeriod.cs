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

	using DevExpress.Mvvm.DataAnnotations;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Schedule validity period.
	/// </summary>
	[Serializable]
	[System.Runtime.Serialization.DataContract]
	[DisplayNameLoc(LocalizedStrings.Str416Key)]
	[DescriptionLoc(LocalizedStrings.Str417Key)]
	public class WorkingTimePeriod : Cloneable<WorkingTimePeriod>, IPersistable
	{
		sealed class TimeSpanRangeInitializer : NewItemInstanceInitializerAttribute
		{
			public TimeSpanRangeInitializer()
				: base(typeof(Range<TimeSpan>), "item")
			{
			}

			public override object CreateInstance()
			{
				return new Range<TimeSpan>(TimeSpan.Zero, TimeSpan.Zero);
			}
		}

		/// <summary>
		/// Schedule expiration date.
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.Str418Key)]
		[DescriptionLoc(LocalizedStrings.Str419Key)]
		public DateTime Till { get; set; }
		
		private List<Range<TimeSpan>> _times = new List<Range<TimeSpan>>();

		/// <summary>
		/// Work schedule within day.
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.Str416Key)]
		[DescriptionLoc(LocalizedStrings.Str420Key)]
		[TimeSpanRangeInitializer]
		public List<Range<TimeSpan>> Times
		{
			get => _times;
			set => _times = value ?? throw new ArgumentNullException(nameof(value));
		}

		private IDictionary<DayOfWeek, Range<TimeSpan>[]> _specialDays = new Dictionary<DayOfWeek, Range<TimeSpan>[]>();

		/// <summary>
		/// Work schedule for days with different from <see cref="Times"/> schedules.
		/// </summary>
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
			Times = storage.GetValue<List<Range<TimeSpan>>>(nameof(Times));
			Till = storage.GetValue<DateTime>(nameof(Till));

			if (storage.ContainsKey(nameof(SpecialDays)))
				SpecialDays = storage.GetValue<IDictionary<DayOfWeek, Range<TimeSpan>[]>>(nameof(SpecialDays));
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(Times), Times);
			storage.SetValue(nameof(Till), Till);
			storage.SetValue(nameof(SpecialDays), SpecialDays);
		}

		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
		/// </returns>
		public override string ToString()
		{
			return Times.Select(t => t.ToString()).Join(",");
		}
	}
}