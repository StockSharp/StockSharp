import clr

# Add .NET references
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo.Analytics")
clr.AddReference("Ecng.Drawing")

from Ecng.Drawing import DrawStyles
from System import Double, TimeSpan
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
        cancellation_token.ThrowIfCancellationRequested()

        if not securities:
            logs.LogWarning("No instruments.")
            return Task.CompletedTask

        closes = []

        for idx, security in enumerate(securities):
            cancellation_token.ThrowIfCancellationRequested()

            logs.LogInfo("Processing {0} of {1}: {2}...", idx + 1, len(securities), security)
            cancellation_token.ThrowIfCancellationRequested()

            # get candle storage
            candle_storage = get_candle_storage(storage, security, time_frame, drive, format)

            # get closing prices
            prices_by_time = {}
            prev_date = None

            candles = iter_candles(candle_storage, from_date, to_date, cancellation_token)
            try:
                for candle in candles:
                    cancellation_token.ThrowIfCancellationRequested()

                    # Log date change
                    curr_date = candle.OpenTime.Date
                    if curr_date != prev_date:
                        prev_date = curr_date
                        logs.LogInfo("  {0}...", curr_date.ToString("yyyy-MM-dd"))
                        cancellation_token.ThrowIfCancellationRequested()

                    prices_by_time[candle.OpenTime] = float(candle.ClosePrice)
            finally:
                candles.close()

            if len(prices_by_time) == 0:
                logs.LogWarning("No data for {0}", security)
                return Task.CompletedTask

            closes.append(prices_by_time)

        # Pair only candles that describe the same market moment. Positional truncation
        # correlates unrelated prices as soon as either instrument has a gap.
        common_times = sorted(
            time for time in closes[0]
            if all(time in series for series in closes[1:])
        )

        if not common_times:
            logs.LogWarning("The instruments have no candles at the same time.")
            return Task.CompletedTask

        paired_closes = [
            [series[time] for time in common_times]
            for series in closes
        ]

        # convert list or array into 2D array
        array2d = nx.to2darray(paired_closes)

        # calculating correlation using NumSharp
        np_array = np.array(array2d)
        matrix = np.corrcoef(np_array)

        # Pearson correlation is undefined when either series has zero deviation.
        # NumpyDotNet reports infinity for that input, so mark the affected row and
        # column as NaN explicitly and keep the matrix symmetric.
        matrix_array = nx.tosystemarray(matrix)
        for row, values in enumerate(paired_closes):
            if all(value == values[0] for value in values[1:]):
                for col in range(len(paired_closes)):
                    matrix_array[row, col] = Double.NaN
                    matrix_array[col, row] = Double.NaN

        # displaying result into heatmap
        ids = [to_string_id(s) for s in securities]
        panel.DrawHeatmap(ids, ids, matrix_array)

        return Task.CompletedTask
