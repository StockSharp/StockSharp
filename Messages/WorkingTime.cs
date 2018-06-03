#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: WorkingTime.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Runtime.Serialization;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Work mode (time, holidays etc.).
	/// </summary>
	[Serializable]
	[System.Runtime.Serialization.DataContract]
	[DisplayNameLoc(LocalizedStrings.Str184Key)]
	[DescriptionLoc(LocalizedStrings.Str408Key)]
	[TypeConverter(typeof(ExpandableObjectConverter))]
	public class WorkingTime : Cloneable<WorkingTime>, IPersistable
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="WorkingTime"/>.
		/// </summary>
		public WorkingTime()
		{
        }

		private List<WorkingTimePeriod> _periods = new List<WorkingTimePeriod>();

		/// <summary>
		/// Schedule validity periods.
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.Str409Key)]
		[DescriptionLoc(LocalizedStrings.Str410Key)]
		public List<WorkingTimePeriod> Periods
		{
			get => _periods;
			set => _periods = value ?? throw new ArgumentNullException(nameof(value));
		}

		private List<DateTime> _specialWorkingDays = new List<DateTime>();

		/// <summary>
		/// Working days, falling on Saturday and Sunday.
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.Str411Key)]
		[DescriptionLoc(LocalizedStrings.Str412Key)]
		public List<DateTime> SpecialWorkingDays
		{
			get => _specialWorkingDays;
			set => _specialWorkingDays = CheckDates(value);
		}

		private List<DateTime> _specialHolidays = new List<DateTime>();

		/// <summary>
		/// Holidays that fall on workdays.
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.Str413Key)]
		[DescriptionLoc(LocalizedStrings.Str414Key)]
		public List<DateTime> SpecialHolidays
		{
			get => _specialHolidays;
			set => _specialHolidays = CheckDates(value);
		}

		private bool _checkDates = true;

		private List<DateTime> CheckDates(List<DateTime> dates)
		{
			if (!_checkDates)
				return dates;

			if (dates == null)
				throw new ArgumentNullException(nameof(dates));

			var dupDate = dates.GroupBy(d => d).FirstOrDefault(g => g.Count() > 1);

			if (dupDate != null)
				throw new ArgumentException(LocalizedStrings.Str415Params.Put(dupDate.Key), nameof(dates));

			return dates;
		}

		/// <summary>
		/// Create a copy of <see cref="WorkingTime"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override WorkingTime Clone()
		{
			var clone = new WorkingTime
			{
				_checkDates = false,
				Periods = Periods.Select(t => t.Clone()).ToList(),
				SpecialWorkingDays = SpecialWorkingDays.ToList(),
				SpecialHolidays = SpecialHolidays.ToList()
			};

			clone._checkDates = true;

			return clone;
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Load(SettingsStorage storage)
		{
			Periods = storage.GetValue<IEnumerable<SettingsStorage>>(nameof(Periods)).Select(s => s.Load<WorkingTimePeriod>()).ToList();
			SpecialWorkingDays = storage.GetValue<List<DateTime>>(nameof(SpecialWorkingDays));
			SpecialHolidays = storage.GetValue<List<DateTime>>(nameof(SpecialHolidays));
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(Periods), Periods.Select(p => p.Save()).ToArray());
			storage.SetValue(nameof(SpecialWorkingDays), SpecialWorkingDays);
			storage.SetValue(nameof(SpecialHolidays), SpecialHolidays);
		}

		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
		/// </returns>
		public override string ToString()
		{
			return Periods.Select(p => p.ToString()).Join(",");
		}
	}
}