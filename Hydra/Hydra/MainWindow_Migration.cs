#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.HydraPublic
File: MainWindow_Migration.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
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

		private void Execute(string query)
		{
			((Database)_entityRegistry.Storage)
				.GetCommand(Query.Execute(query), null, new FieldList(), new FieldList(), false)
				.ExecuteNonQuery(new SerializationItemCollection());
		}

		private void CheckDatabase()
		{
			if (_entityRegistry.Version.Compare(HydraEntityRegistry.LatestVersion) == 0)
				return;

			var database = (Database)_entityRegistry.Storage;

			var conStrBuilder = new DbConnectionStringBuilder { ConnectionString = database.ConnectionString };

			var path = (string)conStrBuilder.Cast<KeyValuePair<string, object>>().ToDictionary(StringComparer.InvariantCultureIgnoreCase).TryGetValue("Data Source");

			if (path == null)
				throw new InvalidOperationException(LocalizedStrings.Str2895);

			File.Copy(path, "{0}.bak.{1:yyyyMMdd}".Put(path, DateTime.Now), true);

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
				Execute(@"
					update HydraTaskSecurity
					set
						[Security] = 'ALL@ALL'
					where
						[Security] LIKE 'ALL@%'");

				_entityRegistry.Version = new Version(2, 8);
			}

			if (_entityRegistry.Version.Compare(new Version(2, 8)) == 0)
			{
				Execute(@"
					update HydraTaskSecurity
					set
						[CandleSeries] = replace([CandleSeries], '<From>01/01/0001 00:00:00</From>', '<From>01/01/0001 00:00:00 +00:00</From>')");

				Execute(@"
					update HydraTaskSecurity
					set
						[CandleSeries] = replace([CandleSeries], '<To>9999-12-31T23:59:59.9999999</To>', '<To>12/31/9999 23:59:59 +00:00</To>')");

				_entityRegistry.Version = new Version(2, 9);
			}

			if (_entityRegistry.Version.Compare(new Version(2, 9)) == 0)
			{
				Execute(@"
					update HydraTaskSettings
					set
						[ExtensionInfo] = replace([ExtensionInfo], '<From>01/01/0001 00:00:00</From>', '<From>01/01/0001 00:00:00 +00:00</From>')");

				_entityRegistry.Version = new Version(2, 10);
			}

			if (_entityRegistry.Version.Compare(new Version(2, 10)) == 0)
			{
				Execute(@"
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

				_entityRegistry.Version = new Version(2, 11);
			}

			if (_entityRegistry.Version.Compare(new Version(2, 11)) == 0)
			{
				UpdateDatabaseWalMode();

				_entityRegistry.Version = new Version(2, 12);
			}

			if (_entityRegistry.Version.Compare(new Version(2, 12)) == 0)
			{
				Execute(@"
						alter table [HydraTaskSecurity] add column ExecutionCount integer;
						alter table [HydraTaskSecurity] add column ExecutionLastTime time;");

				_entityRegistry.Version = new Version(2, 13);
			}

			if (_entityRegistry.Version.Compare(new Version(2, 13)) == 0)
			{
				Execute("update [HydraTaskSecurity] set ExecutionCount = 0 where ExecutionCount is null");
				Execute("update [HydraTaskSecurity] set CandleCount = 0 where CandleCount is null");

				_entityRegistry.Version = new Version(2, 14);
			}

			if (_entityRegistry.Version.Compare(new Version(2, 14)) == 0)
			{
				Execute(@"
					alter table [Security] RENAME TO tmp;
					create table [Security] (
						[Id] varchar NOT NULL, [Name] varchar, [Code] varchar NOT NULL, [Class] varchar, [ShortName] varchar,
						[PriceStep] real, [VolumeStep] real, [Multiplier] real, [Decimals] integer,
						[Type] integer, [ExpiryDate] varchar, [SettlementDate] varchar, [ExtensionInfo] text,
						[Currency] integer, [Board] varchar NOT NULL, [UnderlyingSecurityId] varchar, [Strike] real,
						[OptionType] integer, [BinaryOptionType] varchar, [Bloomberg] varchar, [Cusip] varchar,
						[Isin] varchar, [IQfeed] varchar, [Ric] varchar, [Sedol] varchar, [InteractiveBrokers] integer,
						[Plaza] varchar);
					insert into [Security] select * from tmp;
					drop table tmp;");

				_entityRegistry.Version = new Version(2, 15);
			}

			if (_entityRegistry.Version.Compare(new Version(2, 15)) == 0)
			{
				Execute(@"
					alter table [ExchangeBoard] add column TimeZone varchar;
					update [ExchangeBoard]
					set
						TimeZone = (select [TimeZoneInfo] from [Exchange] where [Name] = [ExchangeBoard].[Exchange])");

				_entityRegistry.Version = new Version(2, 16);
			}

			if (_entityRegistry.Version.Compare(new Version(2, 16)) == 0)
			{
				Execute(@"
					alter table [HydraTaskSecurity] RENAME TO tmp;
					CREATE TABLE HydraTaskSecurity (Id integer NOT NULL PRIMARY KEY AUTOINCREMENT,
						Security varchar NOT NULL,DataTypes varchar NOT NULL,
						CandleSeries text NULL,Settings binary NOT NULL,
						TradeCount integer NOT NULL,TradeLastTime time,
						DepthCount integer NOT NULL,DepthLastTime time,
						OrderLogCount integer NOT NULL,OrderLogLastTime time,
						Level1Count integer NOT NULL,Level1LastTime time,
						CandleCount integer NOT NULL,CandleLastTime time,
						ExecutionCount integer NOT NULL,ExecutionLastTime time);
					insert into [HydraTaskSecurity] select * from tmp;
					drop table tmp;

					update HydraTaskSecurity set DataTypes = '<DataTypeArray />'");

				_entityRegistry.Version = new Version(2, 17);
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

			try
			{
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