import clr

# Add references to required assemblies
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.BusinessEntities")

from StockSharp.Messages import ICandleMessage
from StockSharp.Algo.Indicators import IndicatorHelper, IIndicator, IIndicatorValue
from System import Decimal, DateTimeOffset

def get_first_value(indicator):
    """
    Get the first value of the indicator.
    :param indicator: The indicator (IIndicator).
    :return: The first value as Decimal.
    """
    return IndicatorHelper.GetFirstValue(indicator)

def get_nullable_first_value(indicator):
    """
    Get the first value of the indicator, or None if the indicator has no values.
    :param indicator: The indicator (IIndicator).
    :return: The first value as Decimal? (nullable).
    """
    return IndicatorHelper.GetNullableFirstValue(indicator)

def get_current_value(indicator):
    """
    Get the current value of the indicator.
    :param indicator: The indicator (IIndicator).
    :return: The current value as Decimal.
    """
    return IndicatorHelper.GetCurrentValue(indicator)

def get_nullable_current_value(indicator):
    """
    Get the current value of the indicator, or None if the indicator has no values.
    :param indicator: The indicator (IIndicator).
    :return: The current value as Decimal? (nullable).
    """
    return IndicatorHelper.GetNullableCurrentValue(indicator)

def get_value(indicator, index):
    """
    Get the indicator value by index (0 - last value).
    :param indicator: The indicator (IIndicator).
    :param index: The index of the value.
    :return: The value as Decimal.
    """
    return IndicatorHelper.GetValue(indicator, index)

def get_nullable_value(indicator, index):
    """
    Get the indicator value by index (0 - last value), or None if the value is not available.
    :param indicator: The indicator (IIndicator).
    :param index: The index of the value.
    :return: The value as Decimal? (nullable).
    """
    return IndicatorHelper.GetNullableValue(indicator, index)

def process_value(indicator, value, time, is_final=True):
    """
    Process the indicator with a numeric value.
    :param indicator: The indicator (IIndicator).
    :param value: The numeric value (Decimal).
    :param time: The time of the value (DateTimeOffset).
    :param is_final: Whether the value is final (default is True).
    :return: The new value of the indicator (IIndicatorValue).
    """
    return IndicatorHelper.Process(indicator, value, time, is_final)

def process_candle(indicator, candle):
    """
    Process the indicator with a candle.
    :param indicator: The indicator (IIndicator).
    :param candle: The candle (ICandleMessage).
    :return: The new value of the indicator (IIndicatorValue).
    """
    return IndicatorHelper.Process(indicator, candle)

def to_decimal(indicator_value):
    """
    Convert IIndicatorValue to Decimal.
    :param indicator_value: The indicator value (IIndicatorValue).
    :return: The value as Decimal.
    """
    return IndicatorHelper.ToDecimal(indicator_value)

def to_candle(indicator_value):
    """
    Convert IIndicatorValue to ICandleMessage.
    :param indicator_value: The indicator value (IIndicatorValue).
    :return: The candle (ICandleMessage).
    """
    return IndicatorHelper.ToCandle(indicator_value)

def create_empty_value(indicator, time):
    """
    Create an empty IIndicatorValue.
    :param indicator: The indicator (IIndicator).
    :param time: The time of the value (DateTimeOffset).
    :return: The empty indicator value (IIndicatorValue).
    """
    return IndicatorHelper.CreateEmptyValue(indicator, time)

def process_float(indicator, value, time, is_final=True):
    """
    Process the indicator with a numeric value.
    :param indicator: The indicator (IIndicator).
    :param value: The numeric value (Decimal).
    :param time: The time of the value (DateTimeOffset).
    :param is_final: Whether the value is final (default is True).
    :return: The new value of the indicator (IIndicatorValue).
    """
    return process_value(indicator, Decimal(value), time, is_final)