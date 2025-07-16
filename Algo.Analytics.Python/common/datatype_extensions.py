import clr

# Add references to required assemblies
clr.AddReference("StockSharp.Messages")

from StockSharp.Messages import Extensions
from System import TimeSpan

def tf(minutes):
    return Extensions.TimeFrame(TimeSpan.FromMinutes(minutes))

