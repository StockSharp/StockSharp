namespace StockSharp.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Collections.Specialized;
	using System.Linq;
	using System.Windows.Controls;

	using Ecng.Common;

	using MoreLinq;

	using StockSharp.BusinessEntities;
	using StockSharp.Localization;

	/// <summary>
	/// Описание перехода для инструмента.
	/// </summary>
	public sealed class SecurityJump
	{
		private Security _security;

		/// <summary>
		/// Инструмент.
		/// </summary>
		public Security Security
		{
			get { return _security; }
			set
			{
				if (Security == value)
					return;

				_security = value;
				Changed.SafeInvoke();
			}
		}

		private DateTime _date;

		/// <summary>
		/// Дата перехода.
		/// </summary>
		public DateTime Date
		{
			get { return _date; }
			set
			{
				if (Date == value)
					return;

				_date = value;
				Changed.SafeInvoke();
			}
		}

		/// <summary>
		/// Событие изменения перехода.
		/// </summary>
		public event Action Changed;
	}

	/// <summary>
	/// Графический компонент для редактирования переходов между инструментами.
	/// </summary>
	public partial class SecurityJumpsEditor
	{
		private readonly ObservableCollection<SecurityJump> _jumps = new ObservableCollection<SecurityJump>();

		/// <summary>
		/// Переходы.
		/// </summary>
		public IList<SecurityJump> Jumps
		{
			get { return _jumps; }
		}

		/// <summary>
		/// Выбранный переход.
		/// </summary>
		public SecurityJump SelectedJump
		{
			get { return (SecurityJump)JumpsGrid.SelectedItem; }
		}

		/// <summary>
		/// Выбранные переходы.
		/// </summary>
		public IEnumerable<SecurityJump> SelectedJumps
		{
			get { return JumpsGrid.SelectedItems.Cast<SecurityJump>().ToArray(); }
		}

		/// <summary>
		/// Событие изменения перехода.
		/// </summary>
		public event Action Changed;

		/// <summary>
		/// Событие изменения перехода.
		/// </summary>
		public event Action<SecurityJump> JumpSelected;

		/// <summary>
		/// Создать <see cref="SecurityJumpsEditor"/>.
		/// </summary>
		public SecurityJumpsEditor()
		{
			InitializeComponent();

			_jumps.CollectionChanged += JumpsOnCollectionChanged;

			JumpsGrid.ItemsSource = _jumps;
		}

		private void JumpsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.NewItems != null && e.NewItems.Count > 0)
				e.NewItems.Cast<SecurityJump>().ForEach(j => j.Changed += OnJumpChanged);

			if (e.OldItems != null && e.OldItems.Count > 0)
				e.OldItems.Cast<SecurityJump>().ForEach(j => j.Changed -= OnJumpChanged);

			Changed.SafeInvoke();
		}

		/// <summary>
		/// Проверить правильность ввода данных.
		/// </summary>
		/// <returns>Описание ошибки.</returns>
		public string Validate()
		{
			if (!_jumps.Any())
				return LocalizedStrings.Str1449;

			if (_jumps.Any(j => j.Security == null))
				return LocalizedStrings.Str1450;

			if (_jumps.Any(j => j.Security is BasketSecurity))
				return LocalizedStrings.Str1451;

			if (_jumps.Any(j => j.Date == null))
				return LocalizedStrings.Str1452;

			if (_jumps.GroupBy(j => j.Security).Any(g => g.Count() > 1))
				return LocalizedStrings.Str1453;

			if (_jumps.GroupBy(j => j.Date).Any(g => g.Count() > 1))
				return (LocalizedStrings.Str1454);

			return null;
		}

		private void OnJumpChanged()
		{
			Changed.SafeInvoke();
		}

		private void JumpsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			JumpSelected.SafeInvoke(SelectedJump);
		}
	}
}