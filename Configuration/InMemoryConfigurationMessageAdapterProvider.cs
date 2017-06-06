namespace StockSharp.Configuration
{
	using System.Collections.Generic;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Messages;
	using StockSharp.Xaml.Diagram.Elements;

	/// <summary>
	/// In memory configuration message adapter's provider.
	/// </summary>
	public class InMemoryConfigurationMessageAdapterProvider : IConfigurationMessageAdapterProvider
	{
		private readonly CachedSynchronizedList<IMessageAdapter> _adapters = new CachedSynchronizedList<IMessageAdapter>();

		/// <summary>
		/// Initialize <see cref="InMemoryConfigurationMessageAdapterProvider"/>.
		/// </summary>
		public InMemoryConfigurationMessageAdapterProvider()
		{
			var idGenerator = new IncrementalIdGenerator();

			foreach (var type in Extensions.Adapters)
				_adapters.Add(type.CreateInstance<IMessageAdapter>(idGenerator));
		}

		#region Implementation of IConfigurationMessageAdapterProvider

		IEnumerable<IMessageAdapter> IConfigurationMessageAdapterProvider.Adapters => _adapters.Cache;

		#endregion
	}
}