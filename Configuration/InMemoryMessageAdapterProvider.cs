namespace StockSharp.Configuration
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using System.Security;

	using Ecng.Common;
	using Ecng.Reflection;
	using Ecng.Configuration;

	using StockSharp.Logging;
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
			PossibleAdapters = GetAdapters().Select(t =>
			{
				try
				{
					return t.CreateAdapter(idGenerator);
				}
				catch (Exception ex)
				{
					ex.LogError();
					return null;
				}
			}).Where(a => a != null).ToArray();
		}

		/// <inheritdoc />
		public virtual IEnumerable<IMessageAdapter> CurrentAdapters { get; }

		/// <inheritdoc />
		public virtual IEnumerable<IMessageAdapter> PossibleAdapters { get; }

		private static readonly HashSet<string> _exceptions = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase)
		{
			"StockSharp.Alerts",
			"StockSharp.Algo",
			"StockSharp.Algo.History",
			"StockSharp.Algo.Strategies",
			"StockSharp.BusinessEntities",
			"StockSharp.Community",
			"StockSharp.Configuration",
			"StockSharp.Licensing",
			"StockSharp.Localization",
			"StockSharp.Logging",
			"StockSharp.Messages",
			"StockSharp.Xaml",
			"StockSharp.Xaml.CodeEditor",
			"StockSharp.Xaml.Charting",
			"StockSharp.Xaml.Diagram",
			"StockSharp.Studio.Core",
			"StockSharp.Studio.Controls",
			"StockSharp.QuikLua",
			"StockSharp.QuikLua32",
		};
		
		/// <summary>
		/// Get all available adapters.
		/// </summary>
		/// <returns>All available adapters.</returns>
		protected virtual IEnumerable<Type> GetAdapters()
		{
			var adapters = new List<Type>();

			try
			{
				var assemblies = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.dll").Where(p =>
				{
					var name = Path.GetFileNameWithoutExtension(p);
					return !_exceptions.Contains(name) && name.StartsWithIgnoreCase("StockSharp.");
				});

				foreach (var assembly in assemblies)
				{
					if (!assembly.IsAssembly())
						continue;

					try
					{
						var asm = Assembly.Load(AssemblyName.GetAssemblyName(assembly));

						adapters.AddRange(asm
							.GetTypes()
							.Where(t => typeof(IMessageAdapter).IsAssignableFrom(t) && t.IsPublic && !t.IsAbstract && !t.IsObsolete() && t.IsBrowsable() && !t.Name.EndsWith("Dialect"))
							.ToArray());
					}
					catch (Exception e)
					{
						e.LogError();
					}
				}
			}
			catch (Exception e)
			{
				e.LogError();
			}

			return adapters;
		}

		/// <inheritdoc />
		public virtual IEnumerable<IMessageAdapter> CreateStockSharpAdapters(IdGenerator transactionIdGenerator, string login, SecureString password) => Enumerable.Empty<IMessageAdapter>();

		/// <inheritdoc />
		public virtual IMessageAdapter CreateTransportAdapter(IdGenerator transactionIdGenerator)
		{
			var type = ConfigManager.TryGet<Type>("transportAdapter");

			if (type is null)
				throw new NotSupportedException();

			return type.CreateInstance<IMessageAdapter>(transactionIdGenerator);
		}
	}
}