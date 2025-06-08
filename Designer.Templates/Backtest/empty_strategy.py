import clr

clr.AddReference("StockSharp.Algo")

from StockSharp.Algo.Strategies import Strategy

class empty_strategy(Strategy):
    """
    Empty strategy.

    See more examples: https://github.com/StockSharp/AlgoTrading
    """
    def __init__(self):
        super(empty_strategy, self).__init__()
        # Initialize strategy parameter with default value 80
        self._intParam = self.Param("IntParam", 80)

    @property
    def int_param(self):
        """
        Gets or sets the integer parameter value.
        """
        return self._intParam.Value

    @int_param.setter
    def int_param(self, value):
        """
        Sets the integer parameter value.
        """
        self._intParam.Value = value

    def OnStarted(self, time):
        """
        Called when the strategy is started.

        Logs the start event and calls the base implementation.
        
        :param time: The time when the strategy started.
        """
        # Log information when the strategy starts
        self.LogInfo("OnStarted")
        super(empty_strategy, self).OnStarted(time)

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return empty_strategy()
