namespace StockSharp.Samples.Avalonia;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

using global::Avalonia.Controls;

using Ecng.ComponentModel;

using StockSharp.Algo;
using StockSharp.Configuration;
using StockSharp.Messages;

/// <summary>
/// Owns the connector and the shared transactional settings workflow for a sample window.
/// </summary>
internal sealed class SampleConnectorContext : IDisposable
{
	private readonly IMessageAdapterProvider _adapterProvider;
	private readonly bool _ownsAdapterProvider;
	private readonly IMessageAdapter[] _ownedAdapterCatalog = [];
	private readonly ConnectorConfigurationCoordinator _configuration;
	private readonly CancellationTokenSource _lifetimeCancellation = new();
	private bool _disposed;

	public SampleConnectorContext()
		: this(new Connector())
	{
	}

	/// <summary>
	/// Creates a sample context that owns <paramref name="connector"/> and disposes it with the context.
	/// </summary>
	/// <param name="connector">Connector whose lifetime is transferred to this context.</param>
	public SampleConnectorContext(Connector connector)
	{
		Connector = connector ?? throw new ArgumentNullException(nameof(connector));

		try
		{
			_adapterProvider = ServicesRegistry.TryAdapterProvider;
			if (_adapterProvider is null)
			{
				_adapterProvider = new InMemoryMessageAdapterProvider(Connector.Adapter.InnerAdapters);
				_ownsAdapterProvider = true;
				_ownedAdapterCatalog = [.. _adapterProvider.PossibleAdapters
					.Distinct<IMessageAdapter>(ReferenceEqualityComparer.Instance)];
			}

			_configuration = new(
				Connector,
				new JsonSampleConnectorSettingsStore(Paths.FileSystem));
			_configuration.Load();
		}
		catch (Exception initializationError)
		{
			var errors = new List<Exception> { initializationError };
			TryRelease(Connector.Dispose, errors);
			ReleaseOwnedAdapterCatalog(errors);
			TryRelease(_lifetimeCancellation.Dispose, errors);

			if (errors.Count > 1)
				throw new AggregateException(
					"The sample connector context failed to initialize and release its connector.",
					errors);

			ExceptionDispatchInfo.Capture(initializationError).Throw();
			throw;
		}
	}

	/// <summary>Connector owned by this context.</summary>
	public Connector Connector { get; }

	public bool AutoConnect => _configuration.AutoConnect;

	public async Task<bool> ConfigureAsync(Window owner, CancellationToken cancellationToken = default)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);

		if (Connector.ConnectionState != ConnectionStates.Disconnected)
			return false;

		using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
			cancellationToken,
			_lifetimeCancellation.Token);
		return await _configuration.ConfigureAsync(
			new AvaloniaConnectorConfigurationDialog(owner, _adapterProvider),
			linkedCancellation.Token);
	}

	public void Dispose()
	{
		if (_disposed)
			return;

		_disposed = true;
		var errors = new List<Exception>();
		TryRelease(_lifetimeCancellation.Cancel, errors);
		TryRelease(Connector.Dispose, errors);
		ReleaseOwnedAdapterCatalog(errors);
		TryRelease(_lifetimeCancellation.Dispose, errors);

		if (errors.Count == 1)
			ExceptionDispatchInfo.Capture(errors[0]).Throw();
		if (errors.Count > 1)
			throw new AggregateException("One or more sample connector resources failed to dispose.", errors);
	}

	private void ReleaseOwnedAdapterCatalog(ICollection<Exception> errors)
	{
		if (!_ownsAdapterProvider)
			return;

		foreach (var adapter in _ownedAdapterCatalog)
			TryRelease(adapter.Dispose, errors);
	}

	private static void TryRelease(Action release, ICollection<Exception> errors)
	{
		try
		{
			release();
		}
		catch (Exception error)
		{
			errors.Add(error);
		}
	}
}
