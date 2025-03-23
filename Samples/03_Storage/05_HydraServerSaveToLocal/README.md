# Hydra Server Mode Data Retrieval Example

## Overview

This program illustrates how to connect to Hydra operating in server mode and retrieve various types of market data using the StockSharp framework. It includes setting up remote data retrieval, storing it locally, and displaying some of the data to the user.

## Detailed Code Walkthrough

### Logging Setup

```csharp
var logger = new LogManager();
logger.Listeners.Add(new ConsoleLogListener());
```

- **`LogManager`**: Creates a logging system for tracking operations.
- **`ConsoleLogListener`**: Configures logging to output to the console.

### Storage Setup

```csharp
var storageRegistry = new StorageRegistry();
var registry = new CsvEntityRegistry(Path.Combine(Directory.GetCurrentDirectory(), "Storage"));
var securityStorage = (ISecurityStorage)registry.Securities;
```

- **`StorageRegistry`**: Creates a registry for managing various storage mechanisms.
- **`CsvEntityRegistry`**: Sets up a registry for storing entities in CSV format in the "Storage" directory.
- **`securityStorage`**: Interface for accessing security information stored in the registry.

### Configuring Remote Data Access

```csharp
var remoteDrive = new RemoteMarketDataDrive(RemoteMarketDataDrive.DefaultAddress, new FixMessageAdapter(new IncrementalIdGenerator()))
{
    Logs = logger.Application,

    Credentials =
    {
        Email = "hydra_user",

        //
        // required for non anonymous access
        //
        //Password = "hydra_user".To<SecureString>()
    },

    //
    // uncomment to enable binary mode
    //
    //IsBinaryEnabled = true,
};
```

- **`RemoteMarketDataDrive`**: Sets up a connection to the remote Hydra server using the default address and FIX messaging for communication.
- **`Logs`**: Connects the logger to track application-level events.
- **`Credentials`**: Configures for anonymous access by default (password is commented out).
- **Binary Mode Option**: Commented out but available if needed for improved performance.

### Security Identification

```csharp
var secId = new SecurityId
{
    SecurityCode = "BTCUSDT",
    BoardCode = BoardCodes.BinanceFut,
};
```

- **`SecurityId`**: Identifies the specific market instrument to retrieve (BTCUSDT futures on Binance).
- **`BoardCodes.BinanceFut`**: Uses the predefined board code for Binance Futures.

### Downloading and Storing Security Information

```csharp
var exchangeInfoProvider = new InMemoryExchangeInfoProvider();
remoteDrive.LookupSecurities(new() { SecurityId = secId }, registry.Securities,
    s => securityStorage.Save(s.ToSecurity(exchangeInfoProvider), false), () => false,
    (c, t) => Console.WriteLine($"Downloaded [{c}]/[{t}]"));

var securities = securityStorage.LookupAll();

foreach (var sec in securities)
{
    Console.WriteLine(sec);
}

Console.ReadLine();
```

- **`InMemoryExchangeInfoProvider`**: Provides exchange information for converting messages to securities.
- **LookupSecurities**: Retrieves specific security information (BTCUSDT) from the remote Hydra instance.
- Progress tracking shows download counts in the console.
- Displays all retrieved securities to the console.

### Setup for Historical Data Retrieval

```csharp
var startDate = DateTime.Now.AddDays(-30);
var endDate = DateTime.Now;

const string pathHistory = "Storage";
pathHistory.SafeDeleteDir();

var localDrive = new LocalMarketDataDrive(pathHistory);

const StorageFormats format = StorageFormats.Binary;
```

- **Date Range**: Sets up to retrieve the last 30 days of data.
- **Storage Directory**: Clears and resets the "Storage" directory.
- **LocalMarketDataDrive**: Configures the local storage location.
- **Storage Format**: Sets binary format for storing retrieved data.

### Retrieving and Storing Market Data

```csharp
foreach (var dataType in remoteDrive.GetAvailableDataTypes(secId, format))
{
    var localStorage = storageRegistry.GetStorage(secId, dataType.MessageType, dataType.Arg, localDrive, format);
    var remoteStorage = remoteDrive.GetStorageDrive(secId, dataType, format);

    Console.WriteLine($"Remote {dataType}: {remoteStorage.Dates.FirstOrDefault()}-{remoteStorage.Dates.LastOrDefault()}");

    var dates = remoteStorage.Dates.Where(date => date >= startDate && date <= endDate).ToList();

    foreach (var dateTime in dates)
    {
        using (var stream = remoteStorage.LoadStream(dateTime))
        {
            if (stream == Stream.Null)
                continue;

            localStorage.Drive.SaveStream(dateTime, stream);
        }

        Console.WriteLine($"{dataType}={dateTime}");

        var localStor = localStorage.Load(dateTime);
        foreach (var marketDate in localStor.Take(100))
        {
            Console.WriteLine(marketDate);
        }
    }
}
```

- **GetAvailableDataTypes**: Discovers all available data types for the specified security.
- Displays available date ranges for each data type.
- Filters dates to match the specified 30-day range.
- Downloads each data file as a stream and saves it locally.
- Loads and displays the first 100 entries from each stored data file.

## Key Features

- **Anonymous Access**: Configured by default for anonymous access to Hydra.
- **Specific Security Focus**: Retrieves data for BTCUSDT on Binance Futures.
- **Console Logging**: Provides detailed operation logging.
- **Binary Storage Format**: Uses efficient binary format for data storage.
- **Incremental Download**: Retrieves data for specific dates within a 30-day range.
- **Data Verification**: Displays retrieved data for verification purposes.

## Conclusion

This example demonstrates how to connect to Hydra in server mode, retrieve cryptocurrency market data (specifically BTCUSDT futures), and store it locally for further analysis. The program uses anonymous access by default and retrieves a 30-day history of available data types. This setup is ideal for applications requiring automated data retrieval and storage management in cryptocurrency markets analysis. The code can be adapted for different securities, date ranges, or authentication methods as needed.