import clr

# Add .NET references
clr.AddReference("StockSharp.Algo.Analytics")

from StockSharp.Algo.Analytics import IAnalyticsScript

# The empty analytic strategy.
class empty_analytics_script(IAnalyticsScript):
    def Run(self, logs, panel, securities, from_date, to_date, storage, drive, format, time_frame, cancellation_token):
        if not securities:
            logs.AddWarningLog("No instruments.")
            return Task.CompletedTask

        # !! add logic here

        return Task.CompletedTask
