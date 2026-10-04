namespace StockSharp.Samples;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Ecng.IO;
using Ecng.Logging;
using Ecng.Serialization;

using StockSharp.Configuration;
using StockSharp.Xaml;
using StockSharp.Xaml.Shared.ViewModels;

internal sealed class JsonSampleConnectorSettingsStore(
	IFileSystem fileSystem,
	string connectorFile = "ConnectorFile.json",
	string windowFile = "ConnectorWindow.json") : IConnectorSettingsStore
{
	private readonly IFileSystem _fileSystem = fileSystem
		?? throw new ArgumentNullException(nameof(fileSystem));

	private SettingsStorage _connector;
	private SettingsStorage _window;
	private bool _isLoaded;
	private readonly Lock _writeLock = new();
	private Task _writes = Task.CompletedTask;

	/// <summary>
	/// Reads the stored files. The synchronous members of <see cref="IConnectorSettingsStore"/> return what was read here.
	/// </summary>
	public async Task LoadAsync(CancellationToken cancellationToken)
	{
		_connector = await ReadAsync(connectorFile, cancellationToken);
		_window = await ReadAsync(windowFile, cancellationToken);
		_isLoaded = true;
	}

	public SettingsStorage LoadConnector() => EnsureLoaded(_connector);

	public SettingsStorage LoadWindow() => EnsureLoaded(_window);

	public void SaveConnector(SettingsStorage settings) => _connector = Enqueue(settings, connectorFile);

	public void SaveWindow(SettingsStorage settings) => _window = Enqueue(settings, windowFile);

	private SettingsStorage EnsureLoaded(SettingsStorage settings)
		=> _isLoaded ? settings : throw new InvalidOperationException($"{nameof(LoadAsync)} must be called first.");

	private async Task<SettingsStorage> ReadAsync(string fileName, CancellationToken cancellationToken)
		=> _fileSystem.FileExists(fileName)
			? await fileName.DeserializeAsync<SettingsStorage>(_fileSystem, cancellationToken)
			: null;

	// IConnectorSettingsStore saves synchronously, so the files are written in the background, one after another.
	private SettingsStorage Enqueue(SettingsStorage settings, string fileName)
	{
		ArgumentNullException.ThrowIfNull(settings);

		using (_writeLock.EnterScope())
			_writes = WriteAfterAsync(_writes, settings, fileName);

		return settings;
	}

	private async Task WriteAfterAsync(Task previous, SettingsStorage settings, string fileName)
	{
		await previous;

		try
		{
			await settings.SerializeAsync(_fileSystem, fileName, true, default);
		}
		catch (Exception ex)
		{
			ex.LogError();
		}
	}
}
