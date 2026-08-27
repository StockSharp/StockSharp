namespace StockSharp.Samples.Avalonia;

using System;
using System.IO;

using Ecng.IO;
using Ecng.Serialization;

using StockSharp.Configuration;

internal sealed class JsonSampleConnectorSettingsStore(
	IFileSystem fileSystem,
	string connectorFile = "ConnectorFile.json",
	string windowFile = "ConnectorWindow.json") : ISampleConnectorSettingsStore
{
	private readonly IFileSystem _fileSystem = fileSystem
		?? throw new ArgumentNullException(nameof(fileSystem));

	public SettingsStorage LoadConnector() => Load(connectorFile);

	public SettingsStorage LoadWindow() => Load(windowFile);

	public void SaveConnector(SettingsStorage settings) => Save(settings, connectorFile);

	public void SaveWindow(SettingsStorage settings) => Save(settings, windowFile);

	private SettingsStorage Load(string fileName)
		=> _fileSystem.FileExists(fileName)
			? fileName.Deserialize<SettingsStorage>(_fileSystem)
			: null;

	private void Save(SettingsStorage settings, string fileName)
	{
		ArgumentNullException.ThrowIfNull(settings);
		settings.Serialize(_fileSystem, fileName);
	}
}
