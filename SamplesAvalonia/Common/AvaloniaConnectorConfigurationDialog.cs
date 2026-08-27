namespace StockSharp.Samples.Avalonia;

using System;
using System.Threading;
using System.Threading.Tasks;

using global::Avalonia.Controls;
using global::Avalonia.Threading;

using Ecng.Serialization;

using StockSharp.Algo;
using StockSharp.Configuration;
using StockSharp.Messages;
using StockSharp.Xaml.Windows.Avalonia;

using MessageConnectorInfo = StockSharp.Messages.ConnectorInfo;

internal sealed class AvaloniaConnectorConfigurationDialog(
	Window owner,
	IMessageAdapterProvider adapterProvider) : ISampleConnectorConfigurationDialog
{
	private readonly Window _owner = owner ?? throw new ArgumentNullException(nameof(owner));
	private readonly IMessageAdapterProvider _adapterProvider = adapterProvider
		?? throw new ArgumentNullException(nameof(adapterProvider));

	public async Task<SampleConnectorConfigurationResult> EditAsync(
		BasketMessageAdapter workingAdapter,
		SettingsStorage windowSettings,
		bool autoConnect,
		CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(workingAdapter);
		cancellationToken.ThrowIfCancellationRequested();

		using var window = new ConnectorWindow
		{
			Adapter = workingAdapter,
			AutoConnect = autoConnect,
		};

		if (windowSettings is not null)
			window.Load(windowSettings);

		foreach (var adapter in _adapterProvider.PossibleAdapters)
			window.ConnectorsInfo.Add(new MessageConnectorInfo(adapter));

		using var cancellationRegistration = cancellationToken.Register(
			static state => Dispatcher.UIThread.Post(() => ((ConnectorWindow)state).Close(false)),
			window);
		var accepted = await window.ShowDialog<bool>(_owner);
		cancellationToken.ThrowIfCancellationRequested();

		var savedWindowSettings = new SettingsStorage();
		window.Save(savedWindowSettings);
		return new(accepted, window.AutoConnect, savedWindowSettings);
	}
}
