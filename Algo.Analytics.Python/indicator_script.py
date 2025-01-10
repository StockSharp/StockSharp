import clr

# Add .NET references
clr.AddReference("System")
clr.AddReference("System.Core")
clr.AddReference("System.Threading")
clr.AddReference("System.Threading.Tasks")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Analytics")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Storages")
clr.AddReference("StockSharp.Logging")
clr.AddReference("StockSharp.Messages")

from System import DateTime, TimeSpan
from System.Threading.Tasks import Task
from StockSharp.Algo.Analytics import IAnalyticsScript
from StockSharp.Algo.Indicators import RateOfChange

# The analytic script, using indicator ROC.
class indicator_script(IAnalyticsScript):
    def Run(self, logs, panel, securities, from_date, to_date, storage, drive, format, time_frame, cancellation_token):
        if not securities
            logs.AddWarningLog("No instruments.")
            return Task.CompletedTask

        # creating 2 panes for candles and indicator series
        candle_chart = panel.CreateChart[object, object]()
        indicator_chart = panel.CreateChart[object, object]()

        for security in securities:
            # stop calculation if user cancel script execution
            if cancellation_token.IsCancellationRequested:
                break

            candles_series = {}
            indicator_series = {}

            # creating ROC
            roc = RateOfChange()

            # get candle storage
            candle_storage = storage.GetTimeFrameCandleMessageStorage(security, time_frame, drive, format)

            for candle in candle_storage.Load(from_date, to_date):
                # fill series
                candles_series[candle.OpenTime] = candle.ClosePrice
                indicator_series[candle.OpenTime] = roc.Process(candle).ToDecimal()

            # draw series on chart
            candle_chart.Append(
                f"{security} (close)",
                list(candles_series.keys()),
                list(candles_series.values())
            )
            indicator_chart.Append(
                f"{security} (ROC)",
                list(indicator_series.keys()),
                list(indicator_series.values())
            )

        return Task.CompletedTask
