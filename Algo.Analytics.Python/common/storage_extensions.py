import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System.Threading import CancellationToken
from StockSharp.Algo.Storages import StorageHelper
from StockSharp.Messages import Extensions
from StockSharp.Messages import TimeFrameCandleMessage
from StockSharp.Messages import CandleMessage

def to_string_id(security_id):
    """
    Convert SecurityId to a string representation.
    :param security_id: The SecurityId object.
    :return: String representation of the SecurityId.
    """
    return Extensions.ToStringId(security_id)

def get_dates(storage, from_date, to_date, cancellation_token=None):
    """
    Helper function to get dates from storage using async API.
    :param storage: The storage object.
    :param from_date: The start date of the range.
    :param to_date: The end date of the range.
    :param cancellation_token: Optional cancellation token.
    :return: A list of dates within the specified range.
    """
    if cancellation_token is None:
        cancellation_token = CancellationToken()
    async_enumerable = storage.GetDatesAsync()
    enumerator = async_enumerable.GetAsyncEnumerator(cancellation_token)
    result = []
    try:
        while enumerator.MoveNextAsync().GetAwaiter().GetResult():
            d = enumerator.Current
            if d >= from_date and d <= to_date:
                result.append(d)
    finally:
        enumerator.DisposeAsync().GetAwaiter().GetResult()
    return result

def load_tf_candles(storage, from_date, to_date, cancellation_token=None):
    """
    Helper function to load CandleMessage from storage.
    :param storage: The storage object.
    :param from_date: The start date of the range.
    :param to_date: The end date of the range.
    :param cancellation_token: Optional cancellation token.
    :return: A list of candles within the specified date range.
    """
    return load_async_enumerable(storage, CandleMessage, from_date, to_date, cancellation_token)

def load_range(storage, message_type, from_date, to_date, cancellation_token=None):
    """
    Helper function to load messages from storage.
    :param storage: The storage object.
    :param message_type: The type of the message (e.g., CandleMessage).
    :param from_date: The start date of the range.
    :param to_date: The end date of the range.
    :param cancellation_token: Optional cancellation token.
    :return: A list of messages within the specified date range.
    """
    return load_async_enumerable(storage, message_type, from_date, to_date, cancellation_token)

def load_async_enumerable(storage, message_type, from_date, to_date, cancellation_token=None):
    """
    Helper function to iterate over IAsyncEnumerable from LoadAsync.
    :param storage: The storage object.
    :param message_type: The .NET type for generic method (e.g., CandleMessage).
    :param from_date: The start date of the range.
    :param to_date: The end date of the range.
    :param cancellation_token: Optional cancellation token.
    :return: A list of messages within the specified date range.
    """
    # Use generator and convert to list for backward compatibility
    return list(iter_async_enumerable(storage, message_type, from_date, to_date, cancellation_token))

def iter_async_enumerable(storage, message_type, from_date, to_date, cancellation_token=None):
    """
    Generator to iterate over IAsyncEnumerable from LoadAsync without loading all into memory.
    :param storage: The storage object.
    :param message_type: The .NET type for generic method (e.g., CandleMessage).
    :param from_date: The start date of the range.
    :param to_date: The end date of the range.
    :param cancellation_token: Optional cancellation token.
    :yields: Messages one by one.
    """
    if cancellation_token is None:
        cancellation_token = CancellationToken()

    # Get IAsyncEnumerable from LoadAsync<T> extension method with explicit type
    async_enumerable = StorageHelper.LoadAsync[message_type](storage, from_date, to_date)

    # Iterate over IAsyncEnumerable by blocking on each MoveNextAsync
    enumerator = async_enumerable.GetAsyncEnumerator(cancellation_token)
    try:
        while enumerator.MoveNextAsync().GetAwaiter().GetResult():
            yield enumerator.Current
    finally:
        enumerator.DisposeAsync().GetAwaiter().GetResult()

def iter_candles(storage, from_date, to_date, cancellation_token=None):
    """
    Generator to iterate over candles without loading all into memory.
    :param storage: The candle storage object.
    :param from_date: The start date of the range.
    :param to_date: The end date of the range.
    :param cancellation_token: Optional cancellation token.
    :yields: CandleMessage one by one.
    """
    return iter_async_enumerable(storage, CandleMessage, from_date, to_date, cancellation_token)

def get_candle_storage(registry, security, data_type, drive, format):
    """
    Helper function to get CandleMessage storage.
    :param registry: The storage registry.
    :param security: The security object.
    :param data_type: The data type for candles.
    :param drive: The market data drive.
    :param format: The storage format.
    :return: The storage for TimeFrameCandleMessage.
    """
    return registry.GetCandleMessageStorage(security, data_type, drive, format)


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