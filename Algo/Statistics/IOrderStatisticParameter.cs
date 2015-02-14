namespace StockSharp.Algo.Statistics
{
	using System;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;
	using StockSharp.Localization;

	/// <summary>
	/// Интерфейс, описывающий параметр статистики, рассчитывающийся на основе заявков.
	/// </summary>
	public interface IOrderStatisticParameter
	{
		/// <summary>
		/// Добавить в параметр информацию о новой заявке.
		/// </summary>
		/// <param name="order">Новая заявка.</param>
		void New(Order order);

		/// <summary>
		/// Добавить в параметр информацию об изменившейся заявке.
		/// </summary>
		/// <param name="order">Изменившаяся заявка.</param>
		void Changed(Order order);

		/// <summary>
		/// Добавить в параметр информацию об ошибке регистрации заявки.
		/// </summary>
		/// <param name="fail">Ошибка регистрации заявки.</param>
		void RegisterFailed(OrderFail fail);
	
		/// <summary>
		/// Добавить в параметр информацию об ошибке отмены заявки.
		/// </summary>
		/// <param name="fail">Ошибка отмены заявки.</param>
		void CancelFailed(OrderFail fail);
	}

	/// <summary>
	/// Базовый параметр статистики, рассчитывающийся на основе заявков.
	/// </summary>
	/// <typeparam name="TValue">Тип значения параметра.</typeparam>
	public abstract class BaseOrderStatisticParameter<TValue> : BaseStatisticParameter<TValue>, IOrderStatisticParameter
		where TValue : IComparable<TValue>
	{
		/// <summary>
		/// Инициализировать <see cref="BaseOrderStatisticParameter{TValue}"/>.
		/// </summary>
		protected BaseOrderStatisticParameter()
		{
		}

		/// <summary>
		/// Добавить в параметр информацию о новой заявке.
		/// </summary>
		/// <param name="order">Новая заявка.</param>
		public virtual void New(Order order)
		{
		}

		/// <summary>
		/// Добавить в параметр информацию об изменившейся заявке.
		/// </summary>
		/// <param name="order">Изменившаяся заявка.</param>
		public virtual void Changed(Order order)
		{
		}

		/// <summary>
		/// Добавить в параметр информацию об ошибке регистрации заявки.
		/// </summary>
		/// <param name="fail">Ошибка регистрации заявки.</param>
		public virtual void RegisterFailed(OrderFail fail)
		{
		}

		/// <summary>
		/// Добавить в параметр информацию об ошибке отмены заявки.
		/// </summary>
		/// <param name="fail">Ошибка отмены заявки.</param>
		public virtual void CancelFailed(OrderFail fail)
		{
		}
	}

	/// <summary>
	/// Максимальное значение задержки регистрации заявки.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str947Key)]
	[DescriptionLoc(LocalizedStrings.Str948Key)]
	[CategoryLoc(LocalizedStrings.OrdersKey)]
	public class MaxLatencyRegistrationParameter : BaseOrderStatisticParameter<TimeSpan>
	{
		/// <summary>
		/// Добавить в параметр информацию о новой заявке.
		/// </summary>
		/// <param name="order">Новая заявка.</param>
		public override void New(Order order)
		{
			if (order.LatencyRegistration != null)
				Value = Value.Max(order.LatencyRegistration.Value);
		}
	}

	/// <summary>
	/// Максимальное значение задержки отмены заявки.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str950Key)]
	[DescriptionLoc(LocalizedStrings.Str951Key)]
	[CategoryLoc(LocalizedStrings.OrdersKey)]
	public class MaxLatencyCancellationParameter : BaseOrderStatisticParameter<TimeSpan>
	{
		/// <summary>
		/// Добавить в параметр информацию об изменившейся заявке.
		/// </summary>
		/// <param name="order">Изменившаяся заявка.</param>
		public override void Changed(Order order)
		{
			if (order.LatencyCancellation != null)
				Value = Value.Max(order.LatencyCancellation.Value);
		}
	}

	/// <summary>
	/// Минимальное значение задержки регистрации заявки.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str952Key)]
	[DescriptionLoc(LocalizedStrings.Str953Key)]
	[CategoryLoc(LocalizedStrings.OrdersKey)]
	public class MinLatencyRegistrationParameter : BaseOrderStatisticParameter<TimeSpan>
	{
		private bool _initialized;

		/// <summary>
		/// Добавить в параметр информацию о новой заявке.
		/// </summary>
		/// <param name="order">Новая заявка.</param>
		public override void New(Order order)
		{
			if (order.LatencyRegistration == null)
				return;

			if (!_initialized)
			{
				Value = order.LatencyRegistration.Value;
				_initialized = true;
			}
			else
				Value = Value.Min(order.LatencyRegistration.Value);
		}

		/// <summary>
		/// Загрузить состояние параметра статистики.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Load(SettingsStorage storage)
		{
			_initialized = storage.GetValue<bool>("Initialized");
			base.Load(storage);
		}

		/// <summary>
		/// Сохранить состояние параметра статистики.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Save(SettingsStorage storage)
		{
			storage.SetValue("Initialized", _initialized);
			base.Save(storage);
		}
	}

	/// <summary>
	/// Минимальное значение задержки отмены заявки.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str954Key)]
	[DescriptionLoc(LocalizedStrings.Str955Key)]
	[CategoryLoc(LocalizedStrings.OrdersKey)]
	public class MinLatencyCancellationParameter : BaseOrderStatisticParameter<TimeSpan>
	{
		private bool _initialized;

		/// <summary>
		/// Добавить в параметр информацию об изменившейся заявке.
		/// </summary>
		/// <param name="order">Изменившаяся заявка.</param>
		public override void Changed(Order order)
		{
			if (order.LatencyCancellation == null)
				return;

			if (!_initialized)
			{
				Value = order.LatencyCancellation.Value;
				_initialized = true;
			}
			else
				Value = Value.Min(order.LatencyCancellation.Value);
		}

		/// <summary>
		/// Загрузить состояние параметра статистики.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Load(SettingsStorage storage)
		{
			_initialized = storage.GetValue<bool>("Initialized");
			base.Load(storage);
		}

		/// <summary>
		/// Сохранить состояние параметра статистики.
		/// </summary>
		/// <param name="storage">Хранилище.</param>
		public override void Save(SettingsStorage storage)
		{
			storage.SetValue("Initialized", _initialized);
			base.Save(storage);
		}
	}

	/// <summary>
	/// Общее количество заявок.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str956Key)]
	[DescriptionLoc(LocalizedStrings.Str957Key)]
	[CategoryLoc(LocalizedStrings.OrdersKey)]
	public class OrderCountParameter : BaseOrderStatisticParameter<int>
	{
		/// <summary>
		/// Добавить в параметр информацию о новой заявке.
		/// </summary>
		/// <param name="order">Новая заявка.</param>
		public override void New(Order order)
		{
			Value++;
		}
	}
}