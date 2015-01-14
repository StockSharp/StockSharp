namespace StockSharp.Hydra
{
	using System;
	using System.Collections.Generic;
	using System.Data.Common;
	using System.IO;
	using System.Linq;
	using System.Windows.Input;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Data;
	using Ecng.Data.Sql;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using StockSharp.BusinessEntities;
	using StockSharp.Hydra.Core;
	using StockSharp.Logging;
	using StockSharp.Localization;

	partial class MainWindow
	{
		private void Execute(Query query, Schema schema)
		{
			var database = (Database)_entityRegistry.Storage;
			database.Execute(database.GetCommand(query, schema, new FieldList(), new FieldList()), new SerializationItemCollection(), false);
		}

		private void UpdateDatabaseWalMode()
		{
			var database = (Database)_entityRegistry.Storage;
			var walQuery = Query.Execute("PRAGMA journal_mode=WAL;");
			var walCmd = database.GetCommand(walQuery, null, new FieldList(), new FieldList(), false);
			database.Execute(walCmd, new SerializationItemCollection(), false);
		}

		private void CheckDatabase()
		{
			if (_entityRegistry.Version.Compare(HydraEntityRegistry.LatestVersion) == 0)
				return;

			var database = (Database)_entityRegistry.Storage;

			if (_entityRegistry.Version.Compare(new Version(2, 5)) == 0)
			{
				var schema = typeof(Security).GetSchema();

				var binaryOptType = Query
					.AlterTable(schema)
					.AddColumn(schema.Fields["BinaryOptionType"]);

				Execute(binaryOptType, schema);

				var multiplier = Query
					.AlterTable(schema)
					.AddColumn(schema.Fields["Multiplier"]);

				Execute(multiplier, schema);

				var update = Query
					.Update(schema)
					.Set(new SetPart("Multiplier", "VolumeStep"));

				Execute(update, schema);

				_entityRegistry.Version = new Version(2, 6);
			}
			
			if (_entityRegistry.Version.Compare(new Version(2, 6)) == 0)
			{
				var schema = typeof(News).GetSchema();

				var localTime = Query
					.AlterTable(schema)
					.AddColumn(schema.Fields["LocalTime"]);

				Execute(localTime, schema);

				var serverTime = Query
					.AlterTable(schema)
					.AddColumn(schema.Fields["ServerTime"]);

				Execute(serverTime, schema);

				_entityRegistry.Version = new Version(2, 7);
			}

			if (_entityRegistry.Version.Compare(new Version(2, 7)) == 0)
			{
				var query = Query
					.Execute(@"
					update HydraTaskSecurity
					set
						[Security] = 'ALL@ALL'
					where
						[Security] LIKE 'ALL@%'");

				var cmd = database.GetCommand(query, null, new FieldList(), new FieldList(), false);
				cmd.ExecuteNonQuery(new SerializationItemCollection());

				_entityRegistry.Version = new Version(2, 8);
			}

			if (_entityRegistry.Version.Compare(new Version(2, 8)) == 0)
			{
				var queryFrom = Query
					.Execute(@"
					update HydraTaskSecurity
					set
						[CandleSeries] = replace([CandleSeries], '<From>01/01/0001 00:00:00</From>', '<From>01/01/0001 00:00:00 +00:00</From>')");

				var cmdFrom = database.GetCommand(queryFrom, null, new FieldList(), new FieldList(), false);
				cmdFrom.ExecuteNonQuery(new SerializationItemCollection());

				var queryTo = Query
					.Execute(@"
					update HydraTaskSecurity
					set
						[CandleSeries] = replace([CandleSeries], '<To>9999-12-31T23:59:59.9999999</To>', '<To>12/31/9999 23:59:59 +00:00</To>')");

				var cmdTo = database.GetCommand(queryTo, null, new FieldList(), new FieldList(), false);
				cmdTo.ExecuteNonQuery(new SerializationItemCollection());

				_entityRegistry.Version = new Version(2, 9);
			}

			if (_entityRegistry.Version.Compare(new Version(2, 9)) == 0)
			{
				var query = Query
					.Execute(@"
					update HydraTaskSettings
					set
						[ExtensionInfo] = replace([ExtensionInfo], '<From>01/01/0001 00:00:00</From>', '<From>01/01/0001 00:00:00 +00:00</From>')");

				database
					.GetCommand(query, null, new FieldList(), new FieldList(), false)
					.ExecuteNonQuery(new SerializationItemCollection());

				_entityRegistry.Version = new Version(2, 10);
			}

			if (_entityRegistry.Version.Compare(new Version(2, 10)) == 0)
			{
				var query = Query
					.Execute(@"
					alter table [Security] RENAME TO tmp;
					create table [Security] (
						[Id] varchar NOT NULL, [Name] varchar, [Code] varchar NOT NULL, [Class] varchar, [ShortName] varchar,
						[PriceStep] real NOT NULL, [VolumeStep] real NOT NULL, [Multiplier] real NOT NULL, [Decimals] integer NOT NULL,
						[Type] integer, [ExpiryDate] varchar, [SettlementDate] varchar, [ExtensionInfo] text,
						[Currency] integer, [Board] varchar NOT NULL, [UnderlyingSecurityId] varchar, [Strike] real,
						[OptionType] integer, [BinaryOptionType] varchar, [Bloomberg] varchar, [Cusip] varchar,
						[Isin] varchar, [IQfeed] varchar, [Ric] varchar, [Sedol] varchar, [InteractiveBrokers] integer,
						[Plaza] varchar);
					insert into [Security] select * from tmp;
					drop table tmp;");

				database
					.GetCommand(query, null, new FieldList(), new FieldList(), false)
					.ExecuteNonQuery(new SerializationItemCollection());

				_entityRegistry.Version = new Version(2, 11);
			}

			if (_entityRegistry.Version.Compare(new Version(2, 11)) == 0)
			{
				UpdateDatabaseWalMode();

				_entityRegistry.Version = new Version(2, 12);
			}

			if (_entityRegistry.Version.Compare(new Version(2, 12)) == 0)
			{
				var query = Query
					.Execute(@"
							alter table [HydraTaskSecurity] add column ExecutionCount integer;
							alter table [HydraTaskSecurity] add column ExecutionLastTime time;");

				database
					.GetCommand(query, null, new FieldList(), new FieldList(), false)
					.ExecuteNonQuery(new SerializationItemCollection());

				_entityRegistry.Version = new Version(2, 13);
			}

			if (_entityRegistry.Version.Compare(new Version(2, 13)) == 0)
			{
				database
					.GetCommand(Query
					.Execute("update [HydraTaskSecurity] set ExecutionCount = 0 where ExecutionCount is null"),
						null, new FieldList(), new FieldList(), false)
					.ExecuteNonQuery(new SerializationItemCollection());

				database
					.GetCommand(Query
					.Execute("update [HydraTaskSecurity] set CandleCount = 0 where CandleCount is null"),
						null, new FieldList(), new FieldList(), false)
					.ExecuteNonQuery(new SerializationItemCollection());

				_entityRegistry.Version = new Version(2, 14);
				return;
			}

			GuiDispatcher.GlobalDispatcher.AddSyncAction(() =>
			{
				Mouse.OverrideCursor = null;

				new MessageBoxBuilder()
					.Text(LocalizedStrings.Str3628Params.Put(_entityRegistry.Version, TypeHelper.ApplicationName, HydraEntityRegistry.LatestVersion))
					.Warning()
					.Owner(this)
					.Show();
			});

			var conStrBuilder = new DbConnectionStringBuilder { ConnectionString = database.ConnectionString };

			try
			{
				var path = (string)conStrBuilder.Cast<KeyValuePair<string, object>>().ToDictionary(StringComparer.InvariantCultureIgnoreCase).TryGetValue("Data Source");

				if (path == null)
					throw new InvalidOperationException(LocalizedStrings.Str2895);

				var targetPath = "{0}.bak.{1:yyyyMMdd}".Put(path, DateTime.Now);

				if (File.Exists(targetPath))
					File.Delete(targetPath);

				File.Move(path, targetPath);
				File.WriteAllBytes(path, Properties.Resources.StockSharp);

				// обнуляем настройки, так как БД перезаписана на новую
				_entityRegistry.Settings = new HydraSettingsRegistry();
				_entityRegistry.Version = HydraEntityRegistry.LatestVersion;
			}
			catch (Exception ex)
			{
				ex.LogError();

				GuiDispatcher.GlobalDispatcher.AddSyncAction(() =>
				{
					new MessageBoxBuilder()
						.Text(LocalizedStrings.Str2896)
						.Warning()
						.Owner(this)
						.Show();

					Close();
				});
			}
		}
	}
}