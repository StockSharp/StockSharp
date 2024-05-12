# Hydra Server Mode Data Retrieval Example

## Overview

This program illustrates how to connect to Hydra operating in server mode and retrieve various types of market data using the StockSharp framework. It includes setting up remote data retrieval, storing it locally, and displaying some of the data to the user.

## Detailed Code Walkthrough

### Initial Setup

```csharp
var storageRegistry = new StorageRegistry();
var registry = new CsvEntityRegistry(Path.Combine(Directory.GetCurrentDirectory(), "Storage"));
var securityStorage = (ISecurityStorage)registry.Securities;
```

- **`StorageRegistry`** and **`CsvEntityRegistry`**: Setup local storage mechanisms. 
- **`securityStorage`**: Interface for accessing security information stored in CSV format.

### Configuring Remote Data Access

```csharp
var remoteDrive = new RemoteMarketDataDrive(RemoteMarketDataDrive.DefaultAddress, new FixMessageAdapter(new IncrementalIdGenerator()))
{
    Credentials = { Email = "hydra_user", Password = "hydra_user".To<SecureString>() }
};
```

- **`RemoteMarketDataDrive`**: Sets up a connection to the remote Hydra server using the default address and FIX messaging for communication.
- **`Credentials`**: Configures login details.

### Downloading and Storing Security Information

```csharp
var exchangeInfoProvider = new InMemoryExchangeInfoProvider();
remoteDrive.LookupSecurities(Extensions.LookupAllCriteriaMessage, registry.Securities,
    s => securityStorage.Save(s.ToSecurity(exchangeInfoProvider), false), () => false,
    (c, t) => Console.WriteLine($"Downloaded [{c}]/[{t}]"));

var securities = securityStorage.LookupAll();
foreach (var sec in securities)
{
    Console.WriteLine(sec);
}
Console.ReadLine();
```

- Retrieves all securities from the remote Hydra instance and stores them locally.
- **`LookupSecurities`**: Fetches security information and saves it using the provided callback functions.

### Setup for Data Retrieval

```csharp
var secId = new SecurityId { SecurityCode = "BTCUSD_PERP", BoardCode = "BNBCN" };
var startDate = DateTime.Now.AddDays(-30);
var endDate = DateTime.Now;

const string pathHistory = "Storage";
pathHistory.SafeDeleteDir();

var localDrive = new LocalMarketDataDrive(pathHistory);
const StorageFormats format = StorageFormats.Binary;
```

- **`SecurityId`**: Identifies the specific market data to retrieve.
- **`LocalMarketDataDrive`**: Configures a local storage drive.

### Retrieving and Storing Data

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
Console.ReadLine();
```

- **`GetAvailableDataTypes`**: Retrieves available data types from the remote server.
- Retrieves data for each type within the specified date range, saves it locally, and prints a subset to the console.

## Conclusion

This example provides a comprehensive method to connect to Hydra in server mode, retrieve various market data types, and store them locally for further use. This setup is ideal for applications requiring automated data retrieval and storage management in financial markets analysis. Adjust the code as necessary to align with specific requirements or server configurations.