# StockSharp.Configuration

`StockSharp.Configuration` is a .NET Standard library that centralizes various configuration facilities used by the StockSharp trading platform. It provides classes for storing application paths, loading settings, managing user credentials, and constructing message adapters.
The assembly is titled **S#.Configuration** and described as "Configuration components."


## Features

- **System paths** – the `Paths` class defines all important directories and files used by StockSharp. Paths are initialized from `PathsHolder` and configuration files, as shown below:
  ```csharp
  var companyPath = PathsHolder.CompanyPath ?? ConfigManager.TryGet<string>("companyPath");
  CompanyPath = companyPath.IsEmpty() ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "StockSharp") : companyPath.ToFullPathIfNeed();
  CredentialsFile = Path.Combine(CompanyPath, $"credentials{DefaultSettingsExt}");
  PlatformConfigurationFile = Path.Combine(AppDataPath, $"platform_config{DefaultSettingsExt}");
  ```
  【F:Configuration/Paths.cs†L14-L26】
- **Start‑up settings** – `AppStartSettings` stores language preference and online/offline mode and can be loaded or saved from the platform configuration file:
  ```csharp
  public class AppStartSettings : IPersistable
  {
      public string Language { get; set; } = LocalizedStrings.ActiveLanguage;
      public bool Online { get; set; } = true;
      public static AppStartSettings TryLoad()
      {
          var configFile = Paths.PlatformConfigurationFile;
          if (configFile.IsEmptyOrWhiteSpace() || !configFile.IsConfigExists())
              return null;
          return configFile.Deserialize<SettingsStorage>()?.Load<AppStartSettings>();
      }
  }
  ```
  【F:Configuration/AppStartSettings.cs†L4-L41】
- **Credentials management** – the `ICredentialsProvider` interface allows loading, saving and deleting of `ServerCredentials`. The default implementation persists credentials in `Paths.CredentialsFile`:
  ```csharp
  public interface ICredentialsProvider
  {
      bool TryLoad(out ServerCredentials credentials);
      void Save(ServerCredentials credentials, bool keepSecret);
      void Delete();
  }
  ```
  【F:Configuration/ICredentialsProvider.cs†L4-L25】
  ```csharp
  class DefaultCredentialsProvider : ICredentialsProvider
  {
      private ServerCredentials _credentials;
      bool ICredentialsProvider.TryLoad(out ServerCredentials credentials)
      {
          lock (this)
          {
              if(_credentials != null)
              {
                  credentials = _credentials.Clone();
                  return credentials.CanAutoLogin();
              }
              var file = Paths.CredentialsFile;
              credentials = null;
              if (file.IsConfigExists())
              {
                  credentials = new ServerCredentials();
                  credentials.LoadIfNotNull(file.Deserialize<SettingsStorage>());
                  _credentials = credentials.Clone();
              }
              return credentials?.CanAutoLogin() == true;
          }
      }
  }
  ```
  【F:Configuration/DefaultCredentialsProvider.cs†L3-L38】
- **Invariant serialization** – `InvariantCultureSerializer` saves configuration files using the invariant culture and UTF‑8 encoding, enabling culture‑independent settings:
  ```csharp
  public static class InvariantCultureSerializer
  {
      public static void SerializeInvariant(this SettingsStorage settings, string fileName, bool bom = true)
      {
          if (settings is null)
              throw new ArgumentNullException(nameof(settings));
          Do.Invariant(() => settings.Serialize(fileName, bom));
      }
  }
  ```
  【F:Configuration/InvariantCultureSerializer.cs†L3-L23】
- **Message adapter discovery** – `InMemoryMessageAdapterProvider` scans all local assemblies for `IMessageAdapter` implementations and can create adapters on demand:
  ```csharp
  public class InMemoryMessageAdapterProvider : IMessageAdapterProvider
  {
      public InMemoryMessageAdapterProvider(IEnumerable<IMessageAdapter> currentAdapters, Type transportAdapter = null)
      {
          CurrentAdapters = currentAdapters ?? throw new ArgumentNullException(nameof(currentAdapters));
          var idGenerator = new IncrementalIdGenerator();
          PossibleAdapters = [.. GetAdapters().Select(t =>
          {
              try
              {
                  return t.CreateAdapter(idGenerator);
              }
              catch (Exception ex)
              {
                  ex.LogError();
                  return null;
              }
          }).WhereNotNull()];
      }
  }
  ```
  【F:Configuration/InMemoryMessageAdapterProvider.cs†L5-L51】
- **Utility helpers** – the `Paths` class also exposes methods for serialization, backup management and building links to StockSharp documentation and store pages.


## Usage

1. Configure global paths before accessing `Paths` by setting `PathsHolder.CompanyPath` and `PathsHolder.AppDataPath` if your application uses custom directories.
2. Load or create application start settings with `AppStartSettings.TryLoad` and save them using `TrySave`.
3. Implement `ICredentialsProvider` or use `DefaultCredentialsProvider` to persist server credentials securely.
4. Use `InvariantCultureSerializer` when you need stable serialization irrespective of system locale.
5. Create an instance of `InMemoryMessageAdapterProvider` to dynamically discover available message adapters.

