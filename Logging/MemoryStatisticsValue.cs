#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Logging.Logging
File: MemoryStatisticsValue.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
	/// The interface for tracking the number of active objects of the particular type.
	/// </summary>
	public interface IMemoryStatisticsValue
	{
		/// <summary>
		/// Name.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// The number of active objects.
		/// </summary>
		int ObjectCount { get; }

		/// <summary>
		/// To clear active objects.
		/// </summary>
		/// <param name="resetCounter">Whether to clear the objects counter.</param>
		void Clear(bool resetCounter = false);
	}

	/// <summary>
	/// The class for tracking the number of active objects of the particular type.
	/// </summary>
	/// <typeparam name="T">The object type.</typeparam>
	public class MemoryStatisticsValue<T> : IPersistable, IMemoryStatisticsValue
	{
		//private readonly MemoryStatistics _parent;
		private readonly CachedSynchronizedSet<T> _objects = new CachedSynchronizedSet<T>();

		/// <summary>
		/// Initializes a new instance of the <see cref="MemoryStatisticsValue{T}"/>.
		/// </summary>
		/// <param name="name">Name.</param>
		public MemoryStatisticsValue(string name/*, MemoryStatistics parent*/)
		{
			if (name.IsEmpty())
				throw new ArgumentNullException(nameof(name));

			//if (parent == null)
			//	throw new ArgumentNullException(nameof(parent));

			Name = name;
			//_parent = parent;
		}

		/// <summary>
		/// Active objects.
		/// </summary>
		public T[] Objects => _objects.Cache;

		/// <summary>
		/// Name.
		/// </summary>
		public string Name { get; }

		private int _objectCount;

		/// <summary>
		/// The number of active objects.
		/// </summary>
		public int ObjectCount => _objectCount;

		/// <summary>
		/// To check that they are going to delete a previously deleted object.
		/// </summary>
		public bool ThrowOnRemoveDeleted { get; set; }

		/// <summary>
		/// To log the objects creating and deletion. The default is off.
		/// </summary>
		public bool IsTraceObjects { get; set; }

		private bool _isObjectTracking;

		/// <summary>
		/// Whether the storage of objects available through <see cref="Objects"/> is on. The default is off.
		/// </summary>
		public bool IsObjectTracking
		{
			get => _isObjectTracking;
			set
			{
				if (!_isObjectTracking && value)
					Clear();

				_isObjectTracking = value;
			}
		}

		/// <summary>
		/// To add a new object.
		/// </summary>
		/// <param name="obj">The new object.</param>
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
		/// To add new objects.
		/// </summary>
		/// <param name="objects">Active objects.</param>
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
		/// To delete the active object.
		/// </summary>
		/// <param name="obj">The active object.</param>
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
		/// To change <see cref="ObjectCount"/>, reducing it by the number of deleted objects.
		/// </summary>
		/// <param name="count">The number of deleted objects.</param>
		public void Remove(int count)
		{
			Interlocked.Exchange(ref _objectCount, _objectCount - count);
		}

		/// <summary>
		/// To delete active objects.
		/// </summary>
		/// <param name="objects">Active objects.</param>
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
		/// To clear active objects <see cref="ObjectCount"/>.
		/// </summary>
		/// <param name="resetCounter">Whether to clear the objects counter.</param>
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
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Load(SettingsStorage storage)
		{
			IsObjectTracking = storage.GetValue<bool>(nameof(IsObjectTracking));
			ThrowOnRemoveDeleted = storage.GetValue<bool>(nameof(ThrowOnRemoveDeleted));
			IsTraceObjects = storage.GetValue<bool>(nameof(IsTraceObjects));
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(IsObjectTracking), IsObjectTracking);
			storage.SetValue(nameof(ThrowOnRemoveDeleted), ThrowOnRemoveDeleted);
			storage.SetValue(nameof(IsTraceObjects), IsTraceObjects);
		}
	}
}