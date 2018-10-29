#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Candles.Algo
File: CandleSeries.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Candles
{
	using System;
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Configuration;
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;
	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// Candles series.
	/// </summary>
	public class CandleSeries : NotifiableObject, IPersistable
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CandleSeries"/>.
		/// </summary>
		public CandleSeries()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CandleSeries"/>.
		/// </summary>
		/// <param name="candleType">The candle type.</param>
		/// <param name="security">The instrument to be used for candles formation.</param>
		/// <param name="arg">The candle formation parameter. For example, for <see cref="TimeFrameCandle"/> this value is <see cref="TimeFrameCandle.TimeFrame"/>.</param>
		public CandleSeries(Type candleType, Security security, object arg)
		{
			if (!candleType.IsCandle())
				throw new ArgumentOutOfRangeException(nameof(candleType), candleType, LocalizedStrings.WrongCandleType);

			_security = security ?? throw new ArgumentNullException(nameof(security));
			_candleType = candleType ?? throw new ArgumentNullException(nameof(candleType));
			_arg = arg ?? throw new ArgumentNullException(nameof(arg));
			WorkingTime = security.Board?.WorkingTime;
		}

		private Security _security;

		/// <summary>
		/// The instrument to be used for candles formation.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.SecurityKey,
			Description = LocalizedStrings.SecurityKey + LocalizedStrings.Dot,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 0)]
		public virtual Security Security
		{
			get => _security;
			set
			{
				_security = value;
				NotifyChanged(nameof(Security));
			}
		}

		private Type _candleType;

		/// <summary>
		/// The candle type.
		/// </summary>
		[Browsable(false)]
		public virtual Type CandleType
		{
			get => _candleType;
			set
			{
				NotifyChanging(nameof(CandleType));
				_candleType = value;
				NotifyChanged(nameof(CandleType));
			}
		}

		private object _arg;

		/// <summary>
		/// The candle formation parameter. For example, for <see cref="TimeFrameCandle"/> this value is <see cref="TimeFrameCandle.TimeFrame"/>.
		/// </summary>
		[Browsable(false)]
		public virtual object Arg
		{
			get => _arg;
			set
			{
				NotifyChanging(nameof(Arg));
				_arg = value;
				NotifyChanged(nameof(Arg));
			}
		}

		/// <summary>
		/// The time boundary, within which candles for give series shall be translated.
		/// </summary>
		[Browsable(false)]
		public WorkingTime WorkingTime { get; set; }

		/// <summary>
		/// To perform the calculation <see cref="Candle.PriceLevels"/>. By default, it is disabled.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.VolumeProfileKey,
			Description = LocalizedStrings.VolumeProfileCalcKey,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 2)]
		public bool IsCalcVolumeProfile { get; set; }

		/// <summary>
		/// The initial date from which you need to get data.
		/// </summary>
		[Nullable]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str343Key,
			Description = LocalizedStrings.Str344Key,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 3)]
		public DateTimeOffset? From { get; set; }

		/// <summary>
		/// The final date by which you need to get data.
		/// </summary>
		[Nullable]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str345Key,
			Description = LocalizedStrings.Str346Key,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 4)]
		public DateTimeOffset? To { get; set; }

		/// <summary>
		/// Allow build candles from smaller timeframe.
		/// </summary>
		/// <remarks>
		/// Available only for <see cref="TimeFrameCandle"/>.
		/// </remarks>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.SmallerTimeFrameKey,
			Description = LocalizedStrings.SmallerTimeFrameDescKey,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 5)]
		public bool AllowBuildFromSmallerTimeFrame { get; set; } = true;

		/// <summary>
		/// Use only the regular trading hours for which data will be requested.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.RegularHoursKey,
			Description = LocalizedStrings.RegularTradingHoursKey,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 6)]
		public bool IsRegularTradingHours { get; set; }

		/// <summary>
		/// Market-data count.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.CountKey,
			Description = LocalizedStrings.CandlesCountKey,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 7)]
		public long? Count { get; set; }

		/// <summary>
		/// Build mode.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.ModeKey,
			Description = LocalizedStrings.BuildModeKey,
			GroupName = LocalizedStrings.BuildKey,
			Order = 20)]
		public MarketDataBuildModes BuildCandlesMode { get; set; }

		/// <summary>
		/// Which market-data type is used as a source value.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str213Key,
			Description = LocalizedStrings.CandlesBuildSourceKey,
			GroupName = LocalizedStrings.BuildKey,
			Order = 21)]
		public MarketDataTypes? BuildCandlesFrom { get; set; }

		/// <summary>
		/// Extra info for the <see cref="BuildCandlesFrom"/>.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str748Key,
			Description = LocalizedStrings.Level1FieldKey,
			GroupName = LocalizedStrings.BuildKey,
			Order = 22)]
		public Level1Fields? BuildCandlesField { get; set; }

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return CandleType?.Name + "_" + Security + "_" + TraderHelper.CandleArgToFolderName(Arg);
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Load(SettingsStorage storage)
		{
			var secProvider = ConfigManager.TryGetService<ISecurityProvider>();
			if (secProvider != null)
			{
				var securityId = storage.GetValue<string>(nameof(SecurityId));

				if (!securityId.IsEmpty())
					Security = secProvider.LookupById(securityId);
			}

			CandleType = storage.GetValue(nameof(CandleType), CandleType);
			Arg = storage.GetValue(nameof(Arg), Arg);
			From = storage.GetValue(nameof(From), From);
			To = storage.GetValue(nameof(To), To);
			WorkingTime = storage.GetValue(nameof(WorkingTime), WorkingTime);

			IsCalcVolumeProfile = storage.GetValue(nameof(IsCalcVolumeProfile), IsCalcVolumeProfile);

			BuildCandlesMode = storage.GetValue(nameof(BuildCandlesMode), BuildCandlesMode);
			BuildCandlesFrom = storage.GetValue(nameof(BuildCandlesFrom), BuildCandlesFrom);
			BuildCandlesField = storage.GetValue(nameof(BuildCandlesField), BuildCandlesField);
			AllowBuildFromSmallerTimeFrame = storage.GetValue(nameof(AllowBuildFromSmallerTimeFrame), AllowBuildFromSmallerTimeFrame);
			IsRegularTradingHours = storage.GetValue(nameof(IsRegularTradingHours), IsRegularTradingHours);
			Count = storage.GetValue(nameof(Count), Count);
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Save(SettingsStorage storage)
		{
			if (Security != null)
				storage.SetValue(nameof(SecurityId), Security.Id);

			if (CandleType != null)
				storage.SetValue(nameof(CandleType), CandleType.GetTypeName(false));

			if (Arg != null)
				storage.SetValue(nameof(Arg), Arg);

			storage.SetValue(nameof(From), From);
			storage.SetValue(nameof(To), To);

			if (WorkingTime != null)
				storage.SetValue(nameof(WorkingTime), WorkingTime);

			storage.SetValue(nameof(IsCalcVolumeProfile), IsCalcVolumeProfile);

			storage.SetValue(nameof(BuildCandlesMode), BuildCandlesMode);
			storage.SetValue(nameof(BuildCandlesFrom), BuildCandlesFrom);
			storage.SetValue(nameof(BuildCandlesField), BuildCandlesField);
			storage.SetValue(nameof(AllowBuildFromSmallerTimeFrame), AllowBuildFromSmallerTimeFrame);
			storage.SetValue(nameof(IsRegularTradingHours), IsRegularTradingHours);
			storage.SetValue(nameof(Count), Count);
		}
	}
}