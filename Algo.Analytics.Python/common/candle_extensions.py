import clr

# Add references to required assemblies
clr.AddReference("StockSharp.Algo")

from StockSharp.Algo.Candles import CandleHelper

def get_length(candle):
    """
    Get the length of the candle (difference between High and Low).
    :param candle: Candle (object implementing ICandleMessage).
    :return: Length of the candle.
    """
    return CandleHelper.GetLength(candle)

def get_body(candle):
    """
    Get the body of the candle (difference between Open and Close).
    :param candle: Candle (object implementing ICandleMessage).
    :return: Body of the candle.
    """
    return CandleHelper.GetBody(candle)

def get_top_shadow(candle):
    """
    Get the length of the upper shadow of the candle.
    :param candle: Candle (object implementing ICandleMessage).
    :return: Length of the upper shadow.
    """
    return CandleHelper.GetTopShadow(candle)

def get_bottom_shadow(candle):
    """
    Get the length of the lower shadow of the candle.
    :param candle: Candle (object implementing ICandleMessage).
    :return: Length of the lower shadow.
    """
    return CandleHelper.GetBottomShadow(candle)

def get_middle_price(candle, price_step=None):
    """
    Get the middle price of the candle.
    :param candle: Candle (object implementing ICandleMessage).
    :param price_step: Price step (optional).
    :return: Middle price of the candle.
    """
    return CandleHelper.GetMiddlePrice(candle, price_step)