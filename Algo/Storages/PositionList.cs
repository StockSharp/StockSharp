namespace StockSharp.Algo.Storages
{
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// Класс для представления в виде списка позиций, хранящихся во внешнем хранилище.
	/// </summary>
	public class PositionList : BaseStorageEntityList<Position>
	{
		/// <summary>
		/// Создать <see cref="PositionList"/>.
		/// </summary>
		/// <param name="storage">Специальный интерфейс для прямого доступа к хранилищу.</param>
		public PositionList(IStorage storage)
			: base(storage)
		{
		}

		/// <summary>
		/// Получить данных из сущности для создания.
		/// </summary>
		/// <param name="entity">Сущность.</param>
		/// <returns>Данные для создания.</returns>
		protected override SerializationItemCollection GetOverridedAddSource(Position entity)
		{
			return CreateSource(entity);
		}

		/// <summary>
		/// Получить данных из сущности для удаления.
		/// </summary>
		/// <param name="entity">Сущность.</param>
		/// <returns>Данные для удаления.</returns>
		protected override SerializationItemCollection GetOverridedRemoveSource(Position entity)
		{
			return CreateSource(entity);
		}

		/// <summary>
		/// Загрузить позицию.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="portfolio">Портфель.</param>
		/// <returns>Позиция.</returns>
		public Position ReadBySecurityAndPortfolio(Security security, Portfolio portfolio)
		{
			return Read(CreateSource(security, portfolio));
		}

		/// <summary>
		/// Сохранить торговый объект.
		/// </summary>
		/// <param name="entity">Торговый объект.</param>
		public override void Save(Position entity)
		{
			if (ReadBySecurityAndPortfolio(entity.Security, entity.Portfolio) == null)
				Add(entity);
			else
				Update(entity);
		}

		private static SerializationItemCollection CreateSource(Position position)
		{
			return CreateSource(position.Security, position.Portfolio);
		}

		private static SerializationItemCollection CreateSource(Security security, Portfolio portfolio)
		{
			return new SerializationItemCollection
			{
				new SerializationItem<string>(new VoidField<string>("Security"), security.Id),
				new SerializationItem<string>(new VoidField<string>("Portfolio"), portfolio.Name)
			};
		}
	}
}