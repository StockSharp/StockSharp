#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.Core.CorePublic
File: DatabaseConnectionCache.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.Core
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Data;
	using Ecng.Data.Providers;
	using Ecng.Serialization;
	using Ecng.Xaml.Database;

	/// <summary>
	/// Кэш <see cref="DatabaseConnectionPair"/>.
	/// </summary>
	public class DatabaseConnectionCache : IPersistable
	{
		private readonly CachedSynchronizedSet<DatabaseConnectionPair> _connections = new CachedSynchronizedSet<DatabaseConnectionPair>();

		private DatabaseConnectionCache()
		{
		}

		private static readonly Lazy<DatabaseConnectionCache> _instance = new Lazy<DatabaseConnectionCache>(() => new DatabaseConnectionCache());

		/// <summary>
		/// Кэш.
		/// </summary>
		public static DatabaseConnectionCache Instance => _instance.Value;

		/// <summary>
		/// Список всех подключений.
		/// </summary>
		public IEnumerable<DatabaseConnectionPair> AllConnections => _connections.Cache;

		/// <summary>
		/// Событие создания нового подключения.
		/// </summary>
		public event Action<DatabaseConnectionPair> NewConnectionCreated;

		/// <summary>
		/// Получить подключение к базе данных.
		/// </summary>
		/// <param name="provider">Провайдер баз данных.</param>
		/// <param name="connectionString">Строка подключения.</param>
		/// <returns>Подключение к базе данных</returns>
		public DatabaseConnectionPair GetConnection(DatabaseProvider provider, string connectionString)
		{
			var connection = AllConnections.FirstOrDefault(p => p.Provider == provider && p.ConnectionString.CompareIgnoreCase(connectionString));

			if (connection == null)
			{
				connection = new DatabaseConnectionPair { Provider = provider, ConnectionString = connectionString };
				AddConnection(connection);
			}

			return connection;
		}

		/// <summary>
		/// Добавить новое подключение к базе данных.
		/// </summary>
		/// <param name="connection">Новое подключение.</param>
		public void AddConnection(DatabaseConnectionPair connection)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));

			_connections.Add(connection);
			NewConnectionCreated.SafeInvoke(connection);
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public void Load(SettingsStorage storage)
		{
			var connections = storage
				.GetValue<IEnumerable<SettingsStorage>>("Connections")
				.Select(s =>
				{
					var providerName = s.GetValue<string>("Provider");
					var provider = DatabaseProviderRegistry.Providers.FirstOrDefault(p => p.Name.CompareIgnoreCase(providerName));

					return provider == null
						? null
						: new DatabaseConnectionPair
						{
							Provider = provider,
							ConnectionString = s.GetValue<string>("ConnectionString")
						};
				})
				.Where(p => p != null)
				.ToArray();

			lock (_connections.SyncRoot)
				_connections.AddRange(connections);
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue("Connections", AllConnections.Select(pair =>
			{
				var conStorage = new SettingsStorage
				{
					["Provider"] = pair.Provider.Name,
					["ConnectionString"] = pair.ConnectionString
				};
				return conStorage;
			}).ToArray());
		}
	}
}