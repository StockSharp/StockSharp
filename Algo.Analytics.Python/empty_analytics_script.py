import clr

# Подключаем необходимые сборки .NET
clr.AddReference("System")
clr.AddReference("System.Core")
clr.AddReference("System.Threading")
clr.AddReference("System.Threading.Tasks")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Analytics")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Storages")
clr.AddReference("StockSharp.Logging")

from System.Threading.Tasks import Task
from StockSharp.Algo.Analytics import IAnalyticsScript

# The empty analytic strategy.
class empty_analytics_script(IAnalyticsScript):
    def Run(self, logs, panel, securities, from_date, to_date, storage, drive, format, time_frame, cancellation_token):
        if not securities
            logs.AddWarningLog("No instruments.")
            return Task.CompletedTask

        # !! add logic here

        return Task.CompletedTask
