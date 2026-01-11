import clr

# Add .NET references
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo.Analytics")
clr.AddReference("Ecng.Drawing")

from Ecng.Drawing import DrawStyles
from System import TimeSpan
from System.Threading.Tasks import Task
from StockSharp.Algo.Analytics import IAnalyticsScript
from storage_extensions import *
from candle_extensions import *
from chart_extensions import *
from indicator_extensions import *

# The analytic script, shows biggest candle (by volume and by length) for specified securities.
class biggest_candle_script(IAnalyticsScript):
    def Run(self, logs, panel, securities, from_date, to_date, storage, drive, format, time_frame, cancellation_token):
        if not securities:
            logs.LogWarning("No instruments.")
            return Task.CompletedTask

        price_chart = create_3d_chart(panel, datetime, float, float)
        vol_chart = create_3d_chart(panel, datetime, float, float)

        big_price_candles = []
        big_vol_candles = []

        for idx, security in enumerate(securities):
            # stop calculation if user cancel script execution
            if cancellation_token.IsCancellationRequested:
                break

            logs.LogInfo("Processing {0} of {1}: {2}...", idx + 1, len(securities), security)

            # get candle storage
            candle_storage = get_candle_storage(storage, security, time_frame, drive, format)

            # find biggest candles by iterating through stream
            big_price_candle = None
            big_vol_candle = None
            max_length = 0
            max_volume = 0
            prev_date = None

            for candle in iter_candles(candle_storage, from_date, to_date, cancellation_token):
                # Log date change
                curr_date = candle.OpenTime.Date
                if curr_date != prev_date:
                    prev_date = curr_date
                    logs.LogInfo("  {0}...", curr_date.ToString("yyyy-MM-dd"))

                length = get_length(candle)
                if big_price_candle is None or length > max_length:
                    max_length = length
                    big_price_candle = candle

                if big_vol_candle is None or candle.TotalVolume > max_volume:
                    max_volume = candle.TotalVolume
                    big_vol_candle = candle

            if big_price_candle is not None:
                big_price_candles.append(big_price_candle)

            if big_vol_candle is not None:
                big_vol_candles.append(big_vol_candle)

        # draw series on chart
        price_chart.Append(
            "prices",
            [c.OpenTime for c in big_price_candles],
            [get_middle_price(c) for c in big_price_candles],
            [get_length(c) for c in big_price_candles]
        )

        vol_chart.Append(
            "volumes",
            [c.OpenTime for c in big_vol_candles],
            [c.TotalVolume for c in big_vol_candles],
            [c.TotalVolume for c in big_vol_candles]
        )

        return Task.CompletedTask
