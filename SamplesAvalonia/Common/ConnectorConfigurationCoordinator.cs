namespace StockSharp.Samples.Avalonia;

using System;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Serialization;

using StockSharp.Algo;

internal interface ISampleConnectorConfigurationDialog
{
	Task<SampleConnectorConfigurationResult> EditAsync(
		BasketMessageAdapter workingAdapter,
		SettingsStorage windowSettings,
		bool autoConnect,
		CancellationToken cancellationToken);
}

internal interface ISampleConnectorSettingsStore
{
	SettingsStorage LoadConnector();

	SettingsStorage LoadWindow();

	void SaveConnector(SettingsStorage settings);

	void SaveWindow(SettingsStorage settings);
}

internal readonly record struct SampleConnectorConfigurationResult(
	bool IsAccepted,
	bool AutoConnect,
	SettingsStorage WindowSettings);

/// <summary>
/// Applies connector edits transactionally: the live basket changes only after acceptance.
/// </summary>
internal sealed class ConnectorConfigurationCoordinator(
	Connector liveConnector,
	ISampleConnectorSettingsStore settingsStore)
{
	private readonly SemaphoreSlim _configureGate = new(1, 1);
	private readonly Connector _liveConnector = liveConnector
		?? throw new ArgumentNullException(nameof(liveConnector));
	private readonly ISampleConnectorSettingsStore _settingsStore = settingsStore
		?? throw new ArgumentNullException(nameof(settingsStore));

	private BasketMessageAdapter LiveAdapter => _liveConnector.Adapter;

	public bool AutoConnect { get; private set; }

	public void Load()
	{
		if (_settingsStore.LoadConnector() is { } settings)
		{
			AutoConnect = settings.GetValue(nameof(AutoConnect), false);
			_liveConnector.Load(settings);
		}
	}

	public async Task<bool> ConfigureAsync(
		ISampleConnectorConfigurationDialog dialog,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(dialog);
		cancellationToken.ThrowIfCancellationRequested();

		if (!await _configureGate.WaitAsync(0, cancellationToken))
			return false;

		try
		{
			using var workingAdapter = (BasketMessageAdapter)LiveAdapter.Clone();
			var result = await dialog.EditAsync(
				workingAdapter,
				_settingsStore.LoadWindow(),
				AutoConnect,
				cancellationToken);
			cancellationToken.ThrowIfCancellationRequested();

			if (result.WindowSettings is { } windowSettings)
				_settingsStore.SaveWindow(windowSettings);

			if (!result.IsAccepted)
				return false;

			var liveAdapterSettings = LiveAdapter.Save();
			try
			{
				LiveAdapter.Load(workingAdapter.Save());
			}
			catch (Exception applyError)
			{
				try
				{
					LiveAdapter.Load(liveAdapterSettings);
				}
				catch (Exception rollbackError)
				{
					throw new AggregateException(
						"Connector adapter settings could not be applied or rolled back.",
						applyError,
						rollbackError);
				}

				throw;
			}

			AutoConnect = result.AutoConnect;
			var connectorSettings = _liveConnector.Save();
			connectorSettings.SetValue(nameof(AutoConnect), AutoConnect);
			_settingsStore.SaveConnector(connectorSettings);
			return true;
		}
		finally
		{
			_configureGate.Release();
		}
	}
}
