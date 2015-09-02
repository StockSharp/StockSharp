namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.Serialization;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Serialization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	using StockSharp.Localization;

	/// <summary>
	/// Work mode (time, holidays etc.).
	/// </summary>
	[Serializable]
	[System.Runtime.Serialization.DataContract]
	[DisplayNameLoc(LocalizedStrings.Str184Key)]
	[DescriptionLoc(LocalizedStrings.Str408Key)]
	[ExpandableObject]
	public class WorkingTime : Cloneable<WorkingTime>, IPersistable
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="WorkingTime"/>.
		/// </summary>
		public WorkingTime()
		{
		}

		private WorkingTimePeriod[] _periods = ArrayHelper.Empty<WorkingTimePeriod>();

		/// <summary>
		/// Schedule validity periods.
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.Str409Key)]
		[DescriptionLoc(LocalizedStrings.Str410Key)]
		public WorkingTimePeriod[] Periods
		{
			get { return _periods; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_periods = value;
			}
		}

		private DateTime[] _specialWorkingDays = ArrayHelper.Empty<DateTime>();

		/// <summary>
		/// Working days, falling on Saturday and Sunday.
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.Str411Key)]
		[DescriptionLoc(LocalizedStrings.Str412Key)]
		public DateTime[] SpecialWorkingDays
		{
			get { return _specialWorkingDays; }
			set { _specialWorkingDays = CheckDates(value); }
		}

		private DateTime[] _specialHolidays = ArrayHelper.Empty<DateTime>();

		/// <summary>
		/// Holidays that fall on workdays.
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.Str413Key)]
		[DescriptionLoc(LocalizedStrings.Str414Key)]
		public DateTime[] SpecialHolidays
		{
			get { return _specialHolidays; }
			set { _specialHolidays = CheckDates(value); }
		}

		private bool _checkDates = true;

		private DateTime[] CheckDates(DateTime[] dates)
		{
			if (!_checkDates)
				return dates;

			if (dates == null)
				throw new ArgumentNullException("dates");

			var dupDate = dates.GroupBy(d => d).FirstOrDefault(g => g.Count() > 1);

			if (dupDate != null)
				throw new ArgumentException(LocalizedStrings.Str415Params.Put(dupDate.Key), "dates");

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
				Periods = Periods.Select(t => t.Clone()).ToArray(),
				SpecialWorkingDays = SpecialWorkingDays.ToArray(),
				SpecialHolidays = SpecialHolidays.ToArray()
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
			if (storage.ContainsKey("Times"))
			{
				// TODO Удалить через несколько версий
	
				Periods = new[]
				{
					new WorkingTimePeriod
					{
						Till = DateTime.MaxValue,
						Times = storage.GetValue<Range<TimeSpan>[]>("Times")
					}
				};
			}
			else
				Periods = storage.GetValue<IEnumerable<SettingsStorage>>("Periods").Select(s => s.Load<WorkingTimePeriod>()).ToArray();
			
			SpecialWorkingDays = storage.GetValue<DateTime[]>("SpecialWorkingDays");
			SpecialHolidays = storage.GetValue<DateTime[]>("SpecialHolidays");
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue("Periods", Periods.Select(p => p.Save()).ToArray());
			storage.SetValue("SpecialWorkingDays", SpecialWorkingDays);
			storage.SetValue("SpecialHolidays", SpecialHolidays);
		}
	}
}