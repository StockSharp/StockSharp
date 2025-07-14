import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from StockSharp.Algo.Storages import StorageHelper
from StockSharp.Messages import Extensions
from StockSharp.Messages import TimeFrameCandleMessage

def to_string_id(security_id):
    """
    Convert SecurityId to a string representation.
    :param security_id: The SecurityId object.
    :return: String representation of the SecurityId.
    """
    return Extensions.ToStringId(security_id)

def get_dates(storage, from_date, to_date):
    """
    Helper function to mimic the GetDates extension method.
    :param storage: The storage object.
    :param from_date: The start date of the range.
    :param to_date: The end date of the range.
    :return: A list of dates within the specified range.
    """
    # Call the GetDates extension method from StorageHelper.GetDates
    return list(StorageHelper.GetDates(storage, from_date, to_date))

def load_tf_candles(storage, from_date, to_date):
    return load_range(storage, TimeFrameCandleMessage, from_date, to_date)

def load_range(storage, message_type, from_date, to_date):
    """
    Helper function to mimic the Load extension method.
    :param storage: The storage object.
    :param message_type: The type of the message (e.g., TimeFrameCandleMessage).
    :param from_date: The start date of the range.
    :param to_date: The end date of the range.
    :return: A list of candles within the specified date range.
    """
    # Get the generic method Load<TMessage> from StorageHelper
    load_method = StorageHelper.Load[message_type]
    
    # Call the Load method with the specified type
    return list(load_method(storage, from_date, to_date))

def get_tf_candle_storage(registry, security, time_frame, drive, format):
    """
    Helper function to get TimeFrameCandleMessage storage.
    :param registry: The storage registry.
    :param security: The security object.
    :param time_frame: The time frame for candles.
    :param drive: The market data drive.
    :param format: The storage format.
    :return: The storage for TimeFrameCandleMessage.
    """
    return registry.GetCandleMessageStorage(TimeFrameCandleMessage, security, time_frame, drive, format)

def get_tick_candle_storage(registry, security, tick_count, drive, format):
    """
    Helper function to get TickCandleMessage storage.
    :param registry: The storage registry.
    :param security: The security object.
    :param tick_count: The number of ticks per candle.
    :param drive: The market data drive.
    :param format: The storage format.
    :return: The storage for TickCandleMessage.
    """
    return registry.GetCandleMessageStorage(TickCandleMessage, security, tick_count, drive, format)

def get_volume_candle_storage(registry, security, volume, drive, format):
    """
    Helper function to get VolumeCandleMessage storage.
    :param registry: The storage registry.
    :param security: The security object.
    :param volume: The volume per candle.
    :param drive: The market data drive.
    :param format: The storage format.
    :return: The storage for VolumeCandleMessage.
    """
    return registry.GetCandleMessageStorage(VolumeCandleMessage, security, volume, drive, format)

def get_range_candle_storage(registry, security, range, drive, format):
    """
    Helper function to get RangeCandleMessage storage.
    :param registry: The storage registry.
    :param security: The security object.
    :param range: The range per candle.
    :param drive: The market data drive.
    :param format: The storage format.
    :return: The storage for RangeCandleMessage.
    """
    return registry.GetCandleMessageStorage(RangeCandleMessage, security, range, drive, format)

def get_pnf_candle_storage(registry, security, box_size, reversal_amount, drive, format):
    """
    Helper function to get PnFCandleMessage storage.
    :param registry: The storage registry.
    :param security: The security object.
    :param box_size: The box size for Point and Figure candles.
    :param reversal_amount: The reversal amount for Point and Figure candles.
    :param drive: The market data drive.
    :param format: The storage format.
    :return: The storage for PnFCandleMessage.
    """
    return registry.GetCandleMessageStorage(PnFCandleMessage, security, (box_size, reversal_amount), drive, format)

def get_renko_candle_storage(registry, security, brick_size, drive, format):
    """
    Helper function to get RenkoCandleMessage storage.
    :param registry: The storage registry.
    :param security: The security object.
    :param brick_size: The brick size for Renko candles.
    :param drive: The market data drive.
    :param format: The storage format.
    :return: The storage for RenkoCandleMessage.
    """
    return registry.GetCandleMessageStorage(RenkoCandleMessage, security, brick_size, drive, format)

def get_heikin_ashi_candle_storage(registry, security, time_frame, drive, format):
    """
    Helper function to get HeikinAshiCandleMessage storage.
    :param registry: The storage registry.
    :param security: The security object.
    :param time_frame: The time frame for candles.
    :param drive: The market data drive.
    :param format: The storage format.
    :return: The storage for HeikinAshiCandleMessage.
    """
    return registry.GetCandleMessageStorage(HeikinAshiCandleMessage, security, time_frame, drive, format)