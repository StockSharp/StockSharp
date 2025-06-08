import clr

clr.AddReference("System.Drawing")
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from System.Drawing import Color
from StockSharp.Messages import UnitTypes
from StockSharp.Messages import Unit
from StockSharp.Messages import DataType
from StockSharp.Messages import ICandleMessage
from StockSharp.Messages import CandleStates
from StockSharp.Messages import Sides
from StockSharp.Algo.Indicators import SMA
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Strategies import ISubscriptionHandler

class sma_strategy(Strategy):
    """
    Sample strategy demonstrating the work with SMA indicators.
    
    See more examples: https://github.com/StockSharp/AlgoTrading
    """
    def __init__(self):
        super(sma_strategy, self).__init__()
        self._isShortLessThenLong = None

        # Initialize strategy parameters
        self._candleTypeParam = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle type", "Candle type for strategy calculation.", "General")

        self._long = self.Param("Long", 80)
        self._short = self.Param("Short", 30)

        self._takeValue = self.Param("TakeValue", Unit(0, UnitTypes.Absolute))
        self._stopValue = self.Param("StopValue", Unit(2, UnitTypes.Percent))

    @property
    def CandleType(self):
        return self._candleTypeParam.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleTypeParam.Value = value

    @property
    def Long(self):
        return self._long.Value

    @Long.setter
    def Long(self, value):
        self._long.Value = value

    @property
    def Short(self):
        return self._short.Value

    @Short.setter
    def Short(self, value):
        self._short.Value = value

    @property
    def TakeValue(self):
        return self._takeValue.Value

    @TakeValue.setter
    def TakeValue(self, value):
        self._takeValue.Value = value

    @property
    def StopValue(self):
        return self._stopValue.Value

    @StopValue.setter
    def StopValue(self, value):
        self._stopValue.Value = value

    def OnReseted(self):
        """
        Resets internal state when strategy is reset.
        """
        super(sma_strategy, self).OnReseted()
        self._isShortLessThenLong = None

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, charting, and protection.
        
        :param time: The time when the strategy started.
        """
        super(sma_strategy, self).OnStarted(time)

        # Create indicators
        longSma = SMA()
        longSma.Length = self.Long
        shortSma = SMA()
        shortSma.Length = self.Short

        # Bind candles set and indicators
        subscription = self.SubscribeCandles(self.CandleType)
        # Bind indicators to the candles and start processing
        subscription.Bind(longSma, shortSma, self.OnProcess).Start()

        # Configure chart if GUI is available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, shortSma, Color.Coral)
            self.DrawIndicator(area, longSma)
            self.DrawOwnTrades(area)

        # Configure position protection
        self.StartProtection(self.TakeValue, self.StopValue)

    def OnProcess(self, candle, longValue, shortValue):
        """
        Processes each finished candle, logs information, and executes trading logic on SMA crossing.
        
        :param candle: The processed candle message.
        :param longValue: The current value of the long SMA.
        :param shortValue: The current value of the short SMA.
        """
        self.LogInfo("New candle {0}: {6} {1};{2};{3};{4}; volume {5}", candle.OpenTime, candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice, candle.TotalVolume, candle.SecurityId)

        # If candle is not finished, do nothing
        if candle.State != CandleStates.Finished:
            return

        # Determine if short SMA is less than long SMA
        isShortLessThenLong = shortValue < longValue

        if self._isShortLessThenLong is None:
            self._isShortLessThenLong = isShortLessThenLong
        elif self._isShortLessThenLong != isShortLessThenLong:
            # Crossing happened
            direction = Sides.Sell if isShortLessThenLong else Sides.Buy

            # Calculate volume for opening position or reverting
            volume = self.Volume if self.Position == 0 else Math.Min(Math.Abs(self.Position), self.Volume) * 2

            # Get price step (default to 1 if not set)
            priceStep = self.GetSecurity().PriceStep or 1

            # Calculate order price with offset
            price = candle.ClosePrice + (priceStep if direction == Sides.Buy else -priceStep)

            if direction == Sides.Buy:
                self.BuyLimit(price, volume)
            else:
                self.SellLimit(price, volume)

            # Update state
            self._isShortLessThenLong = isShortLessThenLong

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return sma_strategy()