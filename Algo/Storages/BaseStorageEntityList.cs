namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Data;
	using Ecng.Serialization;

	using MoreLinq;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// Базовый класс для представления в виде списка торговых объектов, хранящихся в базе данных.
	/// </summary>
	/// <typeparam name="T">Тип торгового объекта (например, <see cref="Security"/> или <see cref="MyTrade"/>).</typeparam>
	public abstract class BaseStorageEntityList<T> : HierarchicalPageLoadList<T>, IStorageEntityList<T>, ICollectionEx<T>
		where T : class
	{
		private readonly SyncObject _syncRoot = new SyncObject();

		/// <summary>
		/// Объект синхронизации.
		/// </summary>
		public SyncObject SyncRoot { get { return _syncRoot; } }

		/// <summary>
		/// Поле, обозначающее время.
		/// </summary>
		protected virtual Field TimeField
		{
			get { return Schema.Fields["Time"]; }
		}

		/// <summary>
		/// Инициализировать <see cref="BaseStorageEntityList{T}"/>.
		/// </summary>
		/// <param name="storage">Специальный интерфейс для прямого доступа к хранилищу.</param>
		protected BaseStorageEntityList(IStorage storage)
			: base(storage)
		{
		}

		/// <summary>
		/// Добавить торговый объект в коллекцию.
		/// </summary>
		/// <param name="entity">Торговый объект.</param>
		public override void Add(T entity)
		{
			if (entity == null)
				throw new ArgumentNullException("entity");

			base.Add(entity);
		}

		/// <summary>
		/// Удалить торговый объект из коллекции.
		/// </summary>
		/// <returns>
		/// <see langword="true"/>, если элемент был удален. Иначе, <see langword="false"/>.
		/// </returns>
		/// <param name="entity">Торговый объект.</param>
		public override bool Remove(T entity)
		{
			if (entity == null)
				throw new ArgumentNullException("entity");

			return base.Remove(entity);
		}

		/// <summary>
		/// Сохранить торговый объект.
		/// </summary>
		/// <param name="entity">Торговый объект.</param>
		public override void Save(T entity)
		{
			if (entity == null)
				throw new ArgumentNullException("entity");

			lock (_syncRoot)
				base.Save(entity);
		}

		/// <summary>
		/// Загрузить последние созданные данные.
		/// </summary>
		/// <param name="count">Количество запрашиваемых данных.</param>
		/// <returns>Диапазон данных.</returns>
		public virtual IEnumerable<T> ReadLasts(int count)
		{
			return ReadLasts(count, TimeField);
		}

		/// <summary>
		/// Вызывается при добавлении элемента в хранилище.
		/// </summary>
		/// <param name="entity">Торговый объект.</param>
		protected override void OnAdd(T entity)
		{
			lock (SyncRoot)
				base.OnAdd(entity);
		}

		/// <summary>
		/// Вызывается при удалении всех элементов в хранилище.
		/// </summary>
		protected override void OnClear()
		{
			lock (SyncRoot)
				base.OnClear();
		}

		/// <summary>
		/// Вызывается при получении количества элементов в хранилище.
		/// </summary>
		/// <returns>Количество элементов в хранилище.</returns>
		protected override long OnGetCount()
		{
			lock (SyncRoot)
				return base.OnGetCount();
		}

		/// <summary>
		/// Вызывается при выборке элементов из хранилища.
		/// </summary>
		/// <param name="startIndex">Индекс первого элемента.</param>
		/// <param name="count">Число элементов.</param>
		/// <param name="orderBy">Условие сортировки.</param>
		/// <param name="direction">Направление сортировки.</param>
		/// <returns>Набор элементов.</returns>
		protected override IEnumerable<T> OnGetGroup(long startIndex, long count, Field orderBy, ListSortDirection direction)
		{
			lock (SyncRoot)
				return base.OnGetGroup(startIndex, count, orderBy, direction);
		}

		/// <summary>
		/// Вызывается при удалении элемента из хранилища.
		/// </summary>
		/// <param name="entity">Элемент.</param>
		protected override void OnRemove(T entity)
		{
			lock (SyncRoot)
				base.OnRemove(entity);
		}

		/// <summary>
		/// Вызывается при обновлении элемента в хранилище.
		/// </summary>
		/// <param name="entity">Элемент.</param>
		protected override void OnUpdate(T entity)
		{
			lock (SyncRoot)
				base.OnUpdate(entity);
		}

		/// <summary>
		/// Добавить элементы.
		/// </summary>
		/// <param name="items">Новые элементы.</param>
		public void AddRange(IEnumerable<T> items)
		{
			items.ForEach(Add);
		}

		/// <summary>
		/// Удалить элементы.
		/// </summary>
		/// <param name="items">Элементы, которые необходимо удалить.</param>
		/// <returns>Удаленные элементы.</returns>
		public IEnumerable<T> RemoveRange(IEnumerable<T> items)
		{
			return CollectionHelper.RemoveRange(this, items);
		}

		/// <summary>
		/// Удалить элементы.
		/// </summary>
		/// <param name="index">Индекс, начиная с которого необходимо удалить элементы.</param>
		/// <param name="count">Количество удаляемых элементов.</param>
		/// <returns>Количество удаленных элементов.</returns>
		public int RemoveRange(int index, int count)
		{
			throw new NotSupportedException();
		}
	}
}