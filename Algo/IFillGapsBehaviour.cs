namespace StockSharp.Algo;

using System;
using System.Linq;

using Ecng.Collections;
using Ecng.Common;

using StockSharp.Algo.Storages;
using StockSharp.Messages;
using DataType = Messages.DataType;

/// <summary>
/// Interface describes missed historical data (gaps) dates.
/// </summary>
public interface IFillGapsBehaviour
{
	/// <summary>
	/// Try get next date without data.
	/// </summary>
	/// <param name="secId"><see cref="SecurityId"/></param>
	/// <param name="dataType"><see cref="DataType"/></param>
	/// <param name="from"><see cref="MarketDataMessage.From"/></param>
	/// <param name="to"><see cref="MarketDataMessage.To"/></param>
	/// <param name="fillGaps"><see cref="FillGapsDays"/></param>
	/// <returns>Operation result. <see langword="null"/> no any gaps.</returns>
	(DateTime? gapStart, DateTime? gapEnd) TryGetNextGap(SecurityId secId, DataType dataType, DateTime from, DateTime to, FillGapsDays fillGaps);
}

/// <summary>
/// Implementation is <see cref="IFillGapsBehaviour"/> uses storage information.
/// </summary>
public class StorageFillGapsBehaviour : IFillGapsBehaviour
{
	private readonly IMarketDataDrive _drive;
	private readonly StorageFormats _format;

	/// <summary>
	/// Initializes a new instance of the <see cref="StorageFillGapsBehaviour"/>.
	/// </summary>
	/// <param name="drive"><see cref="IMarketDataDrive"/></param>
	/// <param name="format"><see cref="StorageFormats"/></param>
	public StorageFillGapsBehaviour(IMarketDataDrive drive, StorageFormats format)
    {
		_drive = drive ?? throw new ArgumentNullException(nameof(drive));
		_format = format;
	}

	(DateTime?, DateTime?) IFillGapsBehaviour.TryGetNextGap(SecurityId secId, DataType dataType, DateTime from, DateTime to, FillGapsDays fillGaps)
	{
		if (from >= to)
			return default;

		var existing = _drive.GetStorageDrive(secId, dataType, _format).Dates.Where(d => from <= d || d <= to).ToSet();

		DateTime? gapStart = null;
		DateTime? gapEnd = null;

		if (existing.Count > 0)
		{
			foreach (var required in from.Date.Range(to.Date, TimeSpan.FromDays(1)).Where(d => fillGaps == FillGapsDays.All || !d.DayOfWeek.IsWeekend()))
			{
				if (existing.Remove(required))
				{
					if (gapEnd is not null)
						break;

					continue;
				}

				gapStart ??= required;

				if (existing.Count > 0)
				{
					gapEnd = required.EndOfDay();

					if (fillGaps != FillGapsDays.All && required.DayOfWeek == DayOfWeek.Friday)
						break;
				}
				else
				{
					gapEnd = to;
					break;
				}
			}
		}
		else
		{
			gapStart = from;
			gapEnd = to;
		}

		return (gapStart, gapEnd);
	}
}