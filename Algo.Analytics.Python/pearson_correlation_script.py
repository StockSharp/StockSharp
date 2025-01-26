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
from numpy_extensions import nx

clr.AddReference("NumpyDotNet")
from NumpyDotNet import np

# The analytic script, calculating Pearson correlation by specified securities.
class pearson_correlation_script(IAnalyticsScript):
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
        if not securities:
            logs.LogWarning("No instruments.")
            return Task.CompletedTask

        closes = []

        for security in securities:
            # stop calculation if user cancel script execution
            if cancellation_token.IsCancellationRequested:
                break

            # get candle storage
            candle_storage = get_tf_candle_storage(storage, security, time_frame, drive, format)

            # get closing prices
            prices = [float(c.ClosePrice) for c in load_tf_candles(candle_storage, from_date, to_date)]

            if len(prices) == 0:
                logs.LogWarning("No data for {0}", security)
                return Task.CompletedTask

            closes.append(prices)

        # all arrays must be the same length, so truncate longer ones
        min_length = min(len(arr) for arr in closes)
        closes = [arr[:min_length] for arr in closes]
        
        # convert list or array into 2D array
        array2d = nx.to2darray(closes)
        
        # calculating correlation using NumSharp
        np_array = np.array(array2d)
        matrix = np.corrcoef(np_array)

        # displaying result into heatmap
        ids = [to_string_id(s) for s in securities]
        panel.DrawHeatmap(ids, ids, nx.tosystemarray(matrix))

        return Task.CompletedTask
