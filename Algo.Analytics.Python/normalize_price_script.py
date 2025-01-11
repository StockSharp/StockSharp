import clr

# Add .NET references
clr.AddReference("StockSharp.Algo.Analytics")

from StockSharp.Algo.Analytics import IAnalyticsScript

# The analytic script, normalize securities close prices and shows on same chart.
class normalize_price_script(IAnalyticsScript):
    def Run(self, logs, panel, securities, from_date, to_date, storage, drive, format, time_frame, cancellation_token):
        if not securities:
            logs.AddWarningLog("No instruments.")
            return Task.CompletedTask

        chart = panel.CreateChart[object, object]()

        for security in securities:
            # stop calculation if user cancel script execution
            if cancellation_token.IsCancellationRequested:
                break

            series = {}

            # get candle storage
            candle_storage = storage.GetTimeFrameCandleMessageStorage(security, time_frame, drive, format)

            first_close = None

            for candle in candle_storage.Load(from_date, to_date):
                if first_close is None:
                    first_close = candle.ClosePrice

                # normalize close prices by dividing on first close
                series[candle.OpenTime] = candle.ClosePrice / first_close

            # draw series on chart
            chart.Append(security.ToStringId(), list(series.keys()), list(series.values()))

        return Task.CompletedTask
