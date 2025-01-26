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
from numpy_extensions import nx

# The analytic script, calculating distribution of the biggest volume by hours and shows its in 3D chart.
class chart3d_script(IAnalyticsScript):
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
            logs.LogWarning("No instruments.")
            return Task.CompletedTask

        x = []  # X labels for instruments
        y = []  # Y labels for hours

        # Fill Y labels with hours 0 to 23
        for h in range(24):
            y.append(str(h))

        # Create a 2D array for Z values with dimensions: (number of securities) x (number of hours)
        z = [[0.0 for _ in range(len(y))] for _ in range(len(securities))]

        for i in range(securities.Length):
            # Stop calculation if user cancels script execution
            if cancellation_token.IsCancellationRequested:
                break

            security = securities[i]

            # Fill X labels with security identifiers
            x.append(to_string_id(security))

            # Get candle storage for current security
            candle_storage = get_tf_candle_storage(storage, security, time_frame, drive, format)

            # Get available dates for the specified period
            dates = get_dates(candle_storage, from_date, to_date)

            if len(dates) == 0:
                logs.LogWarning("no data")
                return Task.CompletedTask

            # Grouping candles by opening time (truncated to the nearest hour) and summing volumes
            candles = load_tf_candles(candle_storage, from_date, to_date)
            by_hours = {}
            for candle in candles:
                # Truncate TimeOfDay to the nearest hour
                tod = candle.OpenTime.TimeOfDay
                truncated = TimeSpan.FromHours(int(tod.TotalHours))
                hour = truncated.Hours
                # Sum volumes for the hour
                by_hours[hour] = by_hours.get(hour, 0) + candle.TotalVolume

            # Fill Z values for current security
            for hour, volume in by_hours.items():
                # Set volume at position [i, hour] in the 2D array
                # Ensure hour is within the range of y labels
                
                if hour < len(y):
                    z[i][hour] = float(volume)
                    
        # Draw the 3D chart using panel
        panel.Draw3D(x, y, nx.to2darray(z), "Instruments", "Hours", "Volume")

        return Task.CompletedTask
