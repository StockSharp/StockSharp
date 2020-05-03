#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Testing.Algo
File: BaseEmulationConnector.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Testing
{
    using System;
    using System.Linq;

    using Ecng.Serialization;

	using StockSharp.Algo.Storages;

    /// <summary>
    /// The base connection of emulation.
    /// </summary>
    public abstract class BaseEmulationConnector : Connector
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="BaseEmulationConnector"/>.
		/// </summary>
		/// <param name="emulationAdapter">Emulation message adapter.</param>
		/// <param name="applyHeartbeat">Apply on/off heartbeat mode for the specified adapter.</param>
		public BaseEmulationConnector(EmulationMessageAdapter emulationAdapter, bool applyHeartbeat)
		{
			Adapter.InnerAdapters.Add(emulationAdapter ?? throw new ArgumentNullException(nameof(emulationAdapter)));
			Adapter.ApplyHeartbeat(EmulationAdapter, applyHeartbeat);

			TimeChange = false;

			EntityFactory = new StorageEntityFactory(emulationAdapter.Emulator.SecurityProvider, emulationAdapter.Emulator.PortfolioProvider);
		}

		/// <summary>
		/// The adapter, executing messages in <see cref="IMarketEmulator"/>.
		/// </summary>
		public EmulationMessageAdapter EmulationAdapter => (EmulationMessageAdapter)Adapter.InnerAdapters.First();

		private void SendInGeneratorMessage(MarketDataGenerator generator, bool isSubscribe)
		{
			if (generator == null)
				throw new ArgumentNullException(nameof(generator));

			SendInMessage(new GeneratorMessage
			{
				TransactionId = TransactionIdGenerator.GetNextId(),
				IsSubscribe = isSubscribe,
				SecurityId = generator.SecurityId,
				Generator = generator,
				DataType2 = generator.DataType,
			});
		}

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			if (EmulationAdapter.OwnInnerAdapter)
				EmulationAdapter.Load(storage.GetValue<SettingsStorage>(nameof(EmulationAdapter)));

			base.Load(storage);
		}

		/// <inheritdoc />
		public override void Save(SettingsStorage storage)
		{
			if (EmulationAdapter.OwnInnerAdapter)
				storage.SetValue(nameof(EmulationAdapter), EmulationAdapter.Save());

			base.Save(storage);
		}

		/// <inheritdoc />
		protected override void DisposeManaged()
		{
			if (EmulationAdapter.OwnInnerAdapter)
				EmulationAdapter.Dispose();

			base.DisposeManaged();
		}

		/// <summary>
		/// To register the trades generator.
		/// </summary>
		/// <param name="generator">The trades generator.</param>
		[Obsolete("Uses custom adapter implementation.")]
		public void RegisterTrades(TradeGenerator generator)
		{
			SendInGeneratorMessage(generator, true);
		}

		/// <summary>
		/// To delete the trades generator, registered earlier through <see cref="RegisterTrades"/>.
		/// </summary>
		/// <param name="generator">The trades generator.</param>
		[Obsolete("Uses custom adapter implementation.")]
		public void UnRegisterTrades(TradeGenerator generator)
		{
			SendInGeneratorMessage(generator, false);
		}

		/// <summary>
		/// To register the order books generator.
		/// </summary>
		/// <param name="generator">The order books generator.</param>
		[Obsolete("Uses custom adapter implementation.")]
		public void RegisterMarketDepth(MarketDepthGenerator generator)
		{
			SendInGeneratorMessage(generator, true);
		}

		/// <summary>
		/// To delete the order books generator, earlier registered through <see cref="RegisterMarketDepth"/>.
		/// </summary>
		/// <param name="generator">The order books generator.</param>
		[Obsolete("Uses custom adapter implementation.")]
		public void UnRegisterMarketDepth(MarketDepthGenerator generator)
		{
			SendInGeneratorMessage(generator, false);
		}

		/// <summary>
		/// To register the orders log generator.
		/// </summary>
		/// <param name="generator">The orders log generator.</param>
		[Obsolete("Uses custom adapter implementation.")]
		public void RegisterOrderLog(OrderLogGenerator generator)
		{
			SendInGeneratorMessage(generator, true);
		}

		/// <summary>
		/// To delete the orders log generator, earlier registered through <see cref="RegisterOrderLog"/>.
		/// </summary>
		/// <param name="generator">The orders log generator.</param>
		[Obsolete("Uses custom adapter implementation.")]
		public void UnRegisterOrderLog(OrderLogGenerator generator)
		{
			SendInGeneratorMessage(generator, false);
		}
	}
}