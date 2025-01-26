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

        for security in securities:
            # stop calculation if user cancel script execution
            if cancellation_token.IsCancellationRequested:
                break

            # get candle storage
            candle_storage = get_tf_candle_storage(storage, security, time_frame, drive, format)
            all_candles = load_tf_candles(candle_storage, from_date, to_date)

            if len(all_candles) > 0:
                # first orders by volume desc will be our biggest candle
                big_price_candle = max(all_candles, key=lambda c: get_length(c))
                big_vol_candle = max(all_candles, key=lambda c: c.TotalVolume)

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
            "prices",
            [c.OpenTime for c in big_vol_candles],
            [get_middle_price(c) for c in big_price_candles],
            [c.TotalVolume for c in big_vol_candles]
        )

        return Task.CompletedTask
