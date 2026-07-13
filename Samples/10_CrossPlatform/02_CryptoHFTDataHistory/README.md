# CryptoHFTData historical market data

This cross-platform console sample downloads one hour of KAVAUSDT trades and
order-book updates through `CryptoHFTDataMessageAdapter` and prints the first ten
native StockSharp messages of each type.

Run it from the repository root:

```bash
dotnet run --project Samples/10_CrossPlatform/02_CryptoHFTDataHistory
```

No credentials are required for the rate-limited free tier. Set
`CRYPTOHFTDATA_API_KEY` to use account-level limits.
