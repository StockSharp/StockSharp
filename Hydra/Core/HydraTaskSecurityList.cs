namespace StockSharp.Hydra.Core
{
	using Ecng.Collections;
	using Ecng.Data;
	using Ecng.Data.Sql;
	using Ecng.Serialization;

	using StockSharp.Algo.Storages;

	/// <summary>
	/// Коллекция <see cref="HydraTaskSecurityList"/>.
	/// </summary>
	public class HydraTaskSecurityList : BaseStorageEntityList<HydraTaskSecurity>
	{
		/// <summary>
		/// Создать <see cref="HydraTaskSecurityList"/>.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		/// <param name="settings">Настройки.</param>
		public HydraTaskSecurityList(IStorage storage, HydraTaskSettings settings)
			: base(storage)
		{
			CountQuery = Query
				.Select("count(*)")
				.From(Schema)
				.Where()
				.Equals(Schema.Fields["Settings"]);

			ReadAllQuery = Query
				.Select(Schema)
				.From(Schema)
				.Where()
				.Equals(Schema.Fields["Settings"]);

			ClearQuery = Query
				.Delete()
				.From(Schema)
				.Where()
				.Equals(Schema.Fields["Settings"]);

			UpdateQuery = Query
				.Update(Schema)
				.Set(Schema.Fields["DataTypes"])
				.Where()
				.Equals(Schema.Fields["Id"]);

			RemoveQuery = Query
				.Delete()
				.From(Schema)
				.Where()
				.Equals(Schema.Fields["Id"]);

			AddFilter(Schema.Fields["Settings"], settings, () => settings.Id);
		}

		// TODO

		/// <summary>
		/// Обновить инструмент.
		/// </summary>
		/// <param name="entity">Инструмент.</param>
		protected override void OnUpdate(HydraTaskSecurity entity)
		{
			var excludeFields = new FieldList(Schema.Fields["Id"], Schema.Fields["Settings"], Schema.Fields["Security"]);

			var valueFields = new FieldList(Schema.Fields);
			valueFields.RemoveRange(excludeFields);

			var db = (Database)Storage;
			db.Update(entity, new FieldList(Schema.Fields["Id"]), valueFields);
		}

		/// <summary>
		/// Удалить инструмент.
		/// </summary>
		/// <param name="entity">Инструмент.</param>
		protected override void OnRemove(HydraTaskSecurity entity)
		{
			var db = (Database)Storage;
			db.Delete(db.GetCommand(RemoveQuery, Schema, new FieldList(), new FieldList()), new SerializationItemCollection
			{
				new SerializationItem(Schema.Fields["Id"], entity.Id),
			});
		}
	}
}