import clr
import json
from datetime import datetime
from urllib.parse import urlencode
from urllib.request import Request, urlopen

# Add .NET references
clr.AddReference("StockSharp.Algo.Analytics")
clr.AddReference("Ecng.Drawing")

from Ecng.Drawing import DrawStyles
from System import DateTime, Environment
from System.Threading.Tasks import Task
from StockSharp.Algo.Analytics import IAnalyticsScript
from chart_extensions import *


FXMACRODATA_API_BASE_URL = "https://fxmacrodata.com/api/v1"
DEFAULT_PAIR = "EURUSD"


def split_currency_pair(pair):
    normalized = pair.replace("/", "").replace("-", "").replace("_", "").upper()
    if len(normalized) != 6:
        raise ValueError("currency pair must look like 'EURUSD' or 'EUR/USD'")
    return normalized[:3], normalized[3:]


def first_present(row, keys):
    for key in keys:
        value = row.get(key)
        if value is not None:
            return value
    return None


def api_date(value):
    if hasattr(value, "ToString"):
        return value.ToString("yyyy-MM-dd")
    return value.strftime("%Y-%m-%d")


def selected_pair(securities):
    if securities:
        letters = "".join(ch for ch in str(securities[0]).upper() if ch.isalpha())
        if len(letters) >= 6:
            return letters[:6]
    return DEFAULT_PAIR


def build_fxmacrodata_request(pair, start_date, end_date):
    base, quote = split_currency_pair(pair)
    query = urlencode({"start_date": start_date, "end_date": end_date})
    url = f"{FXMACRODATA_API_BASE_URL}/forex/{base}/{quote}?{query}"
    headers = {}

    api_key = Environment.GetEnvironmentVariable("FXMACRODATA_API_KEY")
    if not api_key:
        api_key = Environment.GetEnvironmentVariable("FXMD_API_KEY")
    if api_key:
        headers["X-API-Key"] = api_key

    return Request(url, headers=headers)


def fetch_fxmacrodata_rates(pair, start_date, end_date):
    request = build_fxmacrodata_request(pair, start_date, end_date)
    response = urlopen(request, timeout=30)
    try:
        payload = json.loads(response.read().decode("utf-8"))
    finally:
        response.close()

    rows = payload.get("data") if isinstance(payload, dict) else payload
    if not rows:
        return []

    rates = []
    for row in rows:
        date_value = first_present(row, ("date", "time", "timestamp", "datetime"))
        rate = first_present(row, ("value", "val", "rate", "close", "fx_rate"))
        if date_value is None or rate is None:
            continue
        rates.append((DateTime.Parse(str(date_value)), float(rate)))

    rates.sort(key=lambda item: item[0])
    return rates


# Analytics script that downloads FXMacroData daily FX spot history and plots it.
class fxmacrodata_forex_script(IAnalyticsScript):
    def Run(
        self,
        logs,
        panel,
        securities,
        from_date,
        to_date,
        storage,
        drive,
        format,
        time_frame,
        cancellation_token
    ):
        pair = selected_pair(securities)
        start_date = api_date(from_date)
        end_date = api_date(to_date)
        logs.LogInfo("Loading FXMacroData {0} from {1} to {2}...", pair, start_date, end_date)

        if cancellation_token.IsCancellationRequested:
            return Task.CompletedTask

        try:
            rates = fetch_fxmacrodata_rates(pair, start_date, end_date)
        except Exception as ex:
            logs.LogWarning("FXMacroData request failed: {0}", ex)
            return Task.CompletedTask

        if len(rates) == 0:
            logs.LogWarning("No FXMacroData rates returned for {0}.", pair)
            return Task.CompletedTask

        dates = [item[0] for item in rates]
        values = [item[1] for item in rates]

        chart = create_chart(panel, datetime, float)
        chart.Append(f"FXMacroData {pair}", dates, values, DrawStyles.DashedLine)

        logs.LogInfo("Loaded {0} FXMacroData observations for {1}.", len(rates), pair)
        return Task.CompletedTask
