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
	using System.Linq;
	using System.Runtime.Serialization;

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
		/// <summary>
		/// Schedule expiration date.
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.Str418Key)]
		[DescriptionLoc(LocalizedStrings.Str419Key)]
		public DateTime Till { get; set; }

		private Range<TimeSpan>[] _times = new Range<TimeSpan>[0];

		/// <summary>
		/// Work schedule within day.
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
					throw new ArgumentNullException(nameof(value));

				_times = value;
			}
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
				Times = Times.Select(t => t.Clone()).ToArray(),
			};
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Load(SettingsStorage storage)
		{
			Times = storage.GetValue<Range<TimeSpan>[]>(nameof(Times));
			Till = storage.GetValue<DateTime>(nameof(Till));
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(Times), Times);
			storage.SetValue(nameof(Till), Till);
		}
	}
}