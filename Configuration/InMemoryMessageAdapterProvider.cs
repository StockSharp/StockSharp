namespace StockSharp.Configuration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;

	using StockSharp.Messages;

	/// <summary>
	/// In memory configuration message adapter's provider.
	/// </summary>
	public class InMemoryMessageAdapterProvider : IMessageAdapterProvider
	{
		/// <summary>
		/// Initialize <see cref="InMemoryMessageAdapterProvider"/>.
		/// </summary>
		/// <param name="currentAdapters">All currently available adapters.</param>
		public InMemoryMessageAdapterProvider(IEnumerable<IMessageAdapter> currentAdapters)
		{
			var idGenerator = new IncrementalIdGenerator();

			PossibleAdapters = Extensions.Adapters.Select(type => type.CreateInstance<IMessageAdapter>(idGenerator)).ToArray();
			CurrentAdapters = currentAdapters ?? throw new ArgumentNullException(nameof(currentAdapters));
		}

		/// <inheritdoc />
		public IEnumerable<IMessageAdapter> CurrentAdapters { get; }

		/// <inheritdoc />
		public IEnumerable<IMessageAdapter> PossibleAdapters { get; }
	}
}