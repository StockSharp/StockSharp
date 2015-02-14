namespace StockSharp.Logging
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Интерфейс для отслеживания количества активных объектов конктретного типа.
	/// </summary>
	public interface IMemoryStatisticsValue
	{
		/// <summary>
		/// Название.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Количество активных объектов.
		/// </summary>
		int ObjectCount { get; }

		/// <summary>
		/// Очистить активные объекты.
		/// </summary>
		/// <param name="resetCounter">Очищать ли счетчик объектов.</param>
		void Clear(bool resetCounter = false);
	}

	/// <summary>
	/// Класс для отслеживания количества активных объектов конктретного типа.
	/// </summary>
	/// <typeparam name="T">Тип объекта.</typeparam>
	public class MemoryStatisticsValue<T> : IPersistable, IMemoryStatisticsValue
	{
		//private readonly MemoryStatistics _parent;
		private readonly CachedSynchronizedSet<T> _objects = new CachedSynchronizedSet<T>();

		/// <summary>
		/// Создать <see cref="MemoryStatisticsValue{T}"/>.
		/// </summary>
		public MemoryStatisticsValue(string name/*, MemoryStatistics parent*/)
		{
			if (name.IsEmpty())
				throw new ArgumentNullException("name");

			//if (parent == null)
			//	throw new ArgumentNullException("parent");

			Name = name;
			//_parent = parent;
		}

		/// <summary>
		/// Активные объекты.
		/// </summary>
		public T[] Objects
		{
			get { return _objects.Cache; }
		}

		/// <summary>
		/// Название.
		/// </summary>
		public string Name { get; private set; }

		private int _objectCount;

		/// <summary>
		/// Количество активных объектов.
		/// </summary>
		public int ObjectCount
		{
			get { return _objectCount; }
		}

		/// <summary>
		/// Проверять, что удаляется ранее удаленный объект.
		/// </summary>
		public bool ThrowOnRemoveDeleted { get; set; }

		/// <summary>
		/// Логировать создание и удаление объектов. По умолчанию выключено.
		/// </summary>
		public bool IsTraceObjects { get; set; }

		private bool _isObjectTracking;

		/// <summary>
		/// Включено ли хранение объектов, доступных через <see cref="Objects"/>. По-умолчанию, выключено.
		/// </summary>
		public bool IsObjectTracking
		{
			get { return _isObjectTracking; }
			set
			{
				if (!_isObjectTracking && value)
					Clear();

				_isObjectTracking = value;
			}
		}

		/// <summary>
		/// Добавить новый объект.
		/// </summary>
		/// <param name="obj">Новый объект.</param>
		public void Add(T obj)
		{
			Interlocked.Increment(ref _objectCount);

			//if (IsTraceObjects)
			//	_parent.AddDebugLog("Создан({1}) {0} типа {2}.", obj, ObjectCount, obj.GetType().Name);

			if (!IsObjectTracking)
				return;

			if (!_objects.TryAdd(obj))
				throw new ArgumentException(LocalizedStrings.ObjectWasAlreadyAdded.Put(obj));
		}

		/// <summary>
		/// Добавить новые объекты.
		/// </summary>
		/// <param name="objects">Активные объекты.</param>
		public void Add(IEnumerable<T> objects)
		{
			var count = objects.Count();

			Interlocked.Add(ref _objectCount, count);

			if (!IsObjectTracking)
				return;

			var obj = objects.FirstOrDefault(o => !_objects.TryAdd(o));

			if (!obj.IsNull())
				throw new ArgumentException(LocalizedStrings.ObjectWasAlreadyAdded.Put(obj));
		}

		/// <summary>
		/// Удалить активный объект.
		/// </summary>
		/// <param name="obj">Активный объект.</param>
		public void Remove(T obj)
		{
			Interlocked.Decrement(ref _objectCount);

			//if (IsTraceObjects)
			//	_parent.AddDebugLog("Удален({1}) {0} типа {2}.", obj, ObjectCount, obj.GetType().Name);

			if (!IsObjectTracking)
				return;

			var found = _objects.Remove(obj);

			if (ThrowOnRemoveDeleted && !found)
				throw new ArgumentException(LocalizedStrings.ObjectWasAlreadyDeleted.Put(obj));
		}

		/// <summary>
		/// Изменить <see cref="ObjectCount"/>, уменьшив его на количество удаленных объектов.
		/// </summary>
		/// <param name="count">Количество удаленных объектов.</param>
		public void Remove(int count)
		{
			Interlocked.Exchange(ref _objectCount, _objectCount - count);
		}

		/// <summary>
		/// Удалить активные объекты.
		/// </summary>
		/// <param name="objects">Активные объекты.</param>
		public void Remove(IEnumerable<T> objects)
		{
			Remove(objects.Count());

			//if (IsTraceObjects)
			//	_parent.AddDebugLog("Удаление {0} объектов типа {1}.", count, typeof(T).Name);

			if (!IsObjectTracking)
				return;

			var hasDeleted = false;

			foreach (var obj in objects)
			{
				if (!_objects.Remove(obj))
					hasDeleted = true;
			}

			if (ThrowOnRemoveDeleted && hasDeleted)
				throw new ArgumentException(LocalizedStrings.SomeObjectWasDeleted);
		}

		/// <summary>
		/// Очистить активные объекты <see cref="Objects"/>.
		/// </summary>
		/// <param name="resetCounter">Очищать ли счетчик объектов.</param>
		public void Clear(bool resetCounter = false)
		{
			if (resetCounter)
				_objectCount = 0;

			if (!IsObjectTracking)
				return;

			// TODO: WeakReference?
			_objects.Clear();
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public void Load(SettingsStorage storage)
		{
			IsObjectTracking = storage.GetValue<bool>("IsObjectTracking");
			ThrowOnRemoveDeleted = storage.GetValue<bool>("ThrowOnRemoveDeleted");
			IsTraceObjects = storage.GetValue<bool>("IsTraceObjects");
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue("IsObjectTracking", IsObjectTracking);
			storage.SetValue("ThrowOnRemoveDeleted", ThrowOnRemoveDeleted);
			storage.SetValue("IsTraceObjects", IsTraceObjects);
		}
	}
}