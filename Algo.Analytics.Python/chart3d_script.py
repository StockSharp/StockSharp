import clr

# Add .NET references
clr.AddReference("StockSharp.Algo.Analytics")

from StockSharp.Algo.Analytics import IAnalyticsScript

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
            logs.AddWarningLog("No instruments.")
            return Task.CompletedTask

        x = []  # X labels for instruments
        y = []  # Y labels for hours

        # Fill Y labels with hours 0 to 23
        for h in range(24):
            y.append(str(h))

        # Create a 2D array for Z values with dimensions: (number of securities) x (number of hours)
        z = Array.CreateInstance(Double, securities.Length, len(y))

        for i in range(securities.Length):
            # Stop calculation if user cancels script execution
            if cancellation_token.IsCancellationRequested:
                break

            security = securities[i]

            # Fill X labels with security identifiers
            x.append(security.ToStringId())

            # Get candle storage for current security
            candle_storage = storage.GetTimeFrameCandleMessageStorage(security, time_frame, drive, format)

            # Get available dates for the specified period
            dates = list(candle_storage.GetDates(from_date, to_date))

            if len(dates) == 0:
                logs.AddWarningLog("no data")
                return Task.CompletedTask

            # Grouping candles by opening time (truncated to the nearest hour) and summing volumes
            candles = list(candle_storage.Load(from_date, to_date))
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
                    z[i, hour] = float(volume)

        # Draw the 3D chart using panel
        panel.Draw3D(x, y, z, "Instruments", "Hours", "Volume")

        return Task.CompletedTask
