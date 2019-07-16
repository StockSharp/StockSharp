namespace StockSharp.Configuration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Security;

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
			CurrentAdapters = currentAdapters ?? throw new ArgumentNullException(nameof(currentAdapters));

			var idGenerator = new IncrementalIdGenerator();
			PossibleAdapters = Extensions.Adapters.Select(type => type.CreateAdapter(idGenerator)).ToArray();
		}

		/// <inheritdoc />
		public virtual IEnumerable<IMessageAdapter> CurrentAdapters { get; }

		/// <inheritdoc />
		public virtual IEnumerable<IMessageAdapter> PossibleAdapters { get; }

		/// <inheritdoc />
		public virtual IEnumerable<IMessageAdapter> CreateStockSharpAdapters(IdGenerator transactionIdGenerator, string login, SecureString password) => Enumerable.Empty<IMessageAdapter>();
	}
}