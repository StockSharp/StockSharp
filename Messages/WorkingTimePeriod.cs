namespace StockSharp.Messages
{
	using System;
	using System.Linq;
	using System.Runtime.Serialization;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Период действия расписания.
	/// </summary>
	[Serializable]
	[System.Runtime.Serialization.DataContract]
	[DisplayNameLoc(LocalizedStrings.Str416Key)]
	[DescriptionLoc(LocalizedStrings.Str417Key)]
	public class WorkingTimePeriod : Cloneable<WorkingTimePeriod>, IPersistable
	{
		/// <summary>
		/// Дата окончания действия расписания.
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.Str418Key)]
		[DescriptionLoc(LocalizedStrings.Str419Key)]
		public DateTime Till { get; set; }

		private Range<TimeSpan>[] _times = new Range<TimeSpan>[0];

		/// <summary>
		/// Расписание работы внутри дня.
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.Str416Key)]
		[DescriptionLoc(LocalizedStrings.Str420Key)]
		public Range<TimeSpan>[] Times
		{
			get { return _times; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_times = value;
			}
		}

		/// <summary>
		/// Создать копию объекта <see cref="WorkingTimePeriod"/>.
		/// </summary>
		/// <returns>Копия объекта.</returns>
		public override WorkingTimePeriod Clone()
		{
			return new WorkingTimePeriod
			{
				Till = Till,
				Times = Times.Select(t => t.Clone()).ToArray(),
			};
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public void Load(SettingsStorage storage)
		{
			Times = storage.GetValue<Range<TimeSpan>[]>("Times");
			Till = storage.GetValue<DateTime>("Till");
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue("Times", Times);
			storage.SetValue("Till", Till);
		}
	}
}