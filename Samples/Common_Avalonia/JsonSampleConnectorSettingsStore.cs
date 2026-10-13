namespace StockSharp.Samples;

using System;
using System.Threading;
using System.Threading.Tasks;

using Ecng.IO;
using Ecng.Serialization;

using StockSharp.Configuration;
using StockSharp.Xaml.Shared.ViewModels;

internal sealed class JsonSampleConnectorSettingsStore(
	IFileSystem fileSystem,
	string connectorFile = "ConnectorFile.json",
	string windowFile = "ConnectorWindow.json") : IConnectorSettingsStore
{
	private readonly IFileSystem _fileSystem = fileSystem
		?? throw new ArgumentNullException(nameof(fileSystem));

	public ValueTask<SettingsStorage> LoadConnectorAsync(CancellationToken cancellationToken)
		=> ReadAsync(connectorFile, cancellationToken);

	public ValueTask<SettingsStorage> LoadWindowAsync(CancellationToken cancellationToken)
		=> ReadAsync(windowFile, cancellationToken);

	public ValueTask SaveConnectorAsync(SettingsStorage settings, CancellationToken cancellationToken)
		=> WriteAsync(settings, connectorFile, cancellationToken);

	public ValueTask SaveWindowAsync(SettingsStorage settings, CancellationToken cancellationToken)
		=> WriteAsync(settings, windowFile, cancellationToken);

	private async ValueTask<SettingsStorage> ReadAsync(string fileName, CancellationToken cancellationToken)
		=> _fileSystem.FileExists(fileName)
			? await fileName.DeserializeAsync<SettingsStorage>(_fileSystem, cancellationToken)
			: null;

	private async ValueTask WriteAsync(SettingsStorage settings, string fileName, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(settings);

		await settings.SerializeAsync(_fileSystem, fileName, true, cancellationToken);
	}
}
