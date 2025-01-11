import clr

# Add .NET references
clr.AddReference("StockSharp.Algo.Analytics")

from StockSharp.Algo.Analytics import IAnalyticsScript

# The analytic script, shows chart drawing possibilities.
class chart_draw_script(IAnalyticsScript):
    def Run(
        self,
        logs,
        panel,
        securities,
        from_date,
        to_date,
        storage,
        drive,
        format,
        time_frame,
        cancellation_token
    ):
        # Check if there are no instruments
        if not securities:
            logs.AddWarningLog("No instruments.")
            return Task.CompletedTask

        # Create charts using the panel
        line_chart = panel.CreateChart[object, object]()
        histogram_chart = panel.CreateChart[object, object]()

        for security in securities:
            # Stop calculation if user cancels script execution
            if cancellation_token.IsCancellationRequested:
                break

            candles_series = {}
            vols_series = {}

            # Get candle storage for the current security
            candle_storage = storage.GetTimeFrameCandleMessageStorage(security, time_frame, drive, format)

            for candle in candle_storage.Load(from_date, to_date):
                # Fill series with closing prices and volumes
                candles_series[candle.OpenTime] = candle.ClosePrice
                vols_series[candle.OpenTime] = candle.TotalVolume

            # Draw series on line chart with dashed line style
            line_chart.Append(
                f"{security} (close)",
                list(candles_series.keys()),
                list(candles_series.values()),
                DrawStyles.DashedLine
            )

            # Draw series on histogram chart with histogram style
            histogram_chart.Append(
                f"{security} (vol)",
                list(vols_series.keys()),
                list(vols_series.values()),
                DrawStyles.Histogram
            )

        return Task.CompletedTask
