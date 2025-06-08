import clr
import random

clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo")

from StockSharp.Algo.Indicators import BaseIndicator, DecimalIndicatorValue
from indicator_extensions import *

class empty_indicator(BaseIndicator):
    """
    Sample indicator demonstrating saving and loading parameters.

    Doc https://doc.stocksharp.com/topics/designer/strategies/using_code/python/create_own_indicator.html
    
    Changes input price on +20% or -20%.
    """
    def __init__(self):
        super(empty_indicator, self).__init__()
        self._change = 20
        self._counter = 0
        self._isFormed = False

    @property
    def Change(self) -> int:
        return self._change

    @Change.setter
    def Change(self, value):
        self._change = value
        self.Reset()

    def CalcIsFormed(self):
        """Determines if the indicator has received sufficient inputs to be considered formed."""
        return self._isFormed

    def Reset(self):
        """Resets the indicator's state and internal counters."""
        super(empty_indicator, self).Reset()
        self._isFormed = False
        self._counter = 0

    def OnProcess(self, input):
        """
        Processes the incoming indicator value and applies a random change.
        
        :param input: The incoming indicator value.
        :return: A new DecimalIndicatorValue after applying changes.
        """
        # Every 10th call, try to return an empty value
        if random.randint(0, 10) == 0:
            return DecimalIndicatorValue(self, input.Time)

        if self._counter == 5:
            # For example, our indicator needs 5 inputs to become formed
            self._isFormed = True
        self._counter += 1

        value = to_decimal(input)

        # Apply random change of +/- _change percent to the current value
        value += value * random.randint(-self._change, self._change) / 100.0

        result = DecimalIndicatorValue(self, value, input.Time)
        # Mark value as final based on a random decision
        result.IsFinal = bool(random.getrandbits(1))
        return result

    def Load(self, storage):
        """
        Loads the indicator parameters from persistent storage.
        
        :param storage: The settings storage to load from.
        """
        super(empty_indicator, self).Load(storage)
        self.Change = storage.GetValue("Change", self.Change)

    def Save(self, storage):
        """
        Saves the indicator parameters to persistent storage.
        
        :param storage: The settings storage to save to.
        """
        super(empty_indicator, self).Save(storage)
        storage.SetValue("Change", self.Change)

    def __str__(self):
        return f"Change: {self.Change}"

    def ToString(self):
        return str(self)
