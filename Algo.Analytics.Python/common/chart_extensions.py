import clr
from System import Decimal, DateTimeOffset
from datetime import datetime

# Add references to required assemblies
clr.AddReference("StockSharp.Algo.Analytics")

from StockSharp.Algo.Analytics import IAnalyticsPanel

def create_chart(panel, x_type, y_type):
    """
    Create a chart with type checking and conversion.
    :param panel: The analytics panel (IAnalyticsPanel).
    :param x_type: The type of the X-axis (e.g., float, datetime).
    :param y_type: The type of the Y-axis (e.g., float, datetime).
    :return: The created chart.
    """
    # Convert Python types to .NET types
    if x_type == float:
        x_type = Decimal
    elif x_type == datetime:
        x_type = DateTimeOffset

    if y_type == float:
        y_type = Decimal
    elif y_type == datetime:
        y_type = DateTimeOffset

    # Create the chart with the correct types
    return panel.CreateChart[x_type, y_type]()

def create_3d_chart(panel, x_type, y_type, z_type):
    """
    Create a 3D chart with type checking and conversion.
    :param panel: The analytics panel (IAnalyticsPanel).
    :param x_type: The type of the X-axis (e.g., float, datetime).
    :param y_type: The type of the Y-axis (e.g., float, datetime).
    :param z_type: The type of the Z-axis (e.g., float, datetime).
    :return: The created 3D chart.
    """
    # Convert Python types to .NET types
    if x_type == float:
        x_type = Decimal
    elif x_type == datetime:
        x_type = DateTimeOffset

    if y_type == float:
        y_type = Decimal
    elif y_type == datetime:
        y_type = DateTimeOffset

    if z_type == float:
        z_type = Decimal
    elif z_type == datetime:
        z_type = DateTimeOffset

    # Create the 3D chart with the correct types
    return panel.CreateChart[x_type, y_type, z_type]()