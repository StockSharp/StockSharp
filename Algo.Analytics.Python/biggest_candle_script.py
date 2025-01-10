import clr

# Add .NET references
clr.AddReference("System")
clr.AddReference("System.Core")
clr.AddReference("System.Threading")
clr.AddReference("System.Threading.Tasks")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Analytics")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Storages")
clr.AddReference("StockSharp.Logging")
clr.AddReference("StockSharp.Messages")

from System import DateTime, TimeSpan
from System.Threading.Tasks import Task
from StockSharp.Algo.Analytics import IAnalyticsScript

# The analytic script, shows biggest candle (by volume and by length) for specified securities.
class biggest_candle_script(IAnalyticsScript):
    def Run(self, logs, panel, securities, from_date, to_date, storage, drive, format, time_frame, cancellation_token):
        if not securities
            logs.AddWarningLog("No instruments.")
            return Task.CompletedTask

        price_chart = panel.CreateChart[object, object, object]()
        vol_chart = panel.CreateChart[object, object, object]()

        big_price_candles = []
        big_vol_candles = []

        for security in securities:
            # stop calculation if user cancel script execution
            if cancellation_token.IsCancellationRequested:
                break

            # get candle storage
            candle_storage = storage.GetTimeFrameCandleMessageStorage(security, time_frame, drive, format)
            all_candles = list(candle_storage.Load(from_date, to_date))

            if len(all_candles) > 0:
                # first orders by volume desc will be our biggest candle
                big_price_candle = max(all_candles, key=lambda c: c.GetLength())
                big_vol_candle = max(all_candles, key=lambda c: c.TotalVolume)

                if big_price_candle is not None:
                    big_price_candles.append(big_price_candle)

                if big_vol_candle is not None:
                    big_vol_candles.append(big_vol_candle)

        # draw series on chart
        price_chart.Append(
            "prices",
            [c.OpenTime for c in big_price_candles],
            [c.GetMiddlePrice(None) for c in big_price_candles],
            [c.GetLength() for c in big_price_candles]
        )

        vol_chart.Append(
            "prices",
            [c.OpenTime for c in big_vol_candles],
            [c.GetMiddlePrice(None) for c in big_price_candles],
            [c.TotalVolume for c in big_vol_candles]
        )

        return Task.CompletedTask
