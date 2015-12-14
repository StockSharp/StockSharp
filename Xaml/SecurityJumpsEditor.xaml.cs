#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: SecurityJumpsEditor.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
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
	/// Rollover description for the instrument.
	/// </summary>
	public sealed class SecurityJump
	{
		private Security _security;

		/// <summary>
		/// Security.
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
		/// Rollover date.
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
		/// The rollover change event.
		/// </summary>
		public event Action Changed;
	}

	/// <summary>
	/// Graphical component for editing of rollovers between tools.
	/// </summary>
	public partial class SecurityJumpsEditor
	{
		private readonly ObservableCollection<SecurityJump> _jumps = new ObservableCollection<SecurityJump>();

		/// <summary>
		/// Rollovers.
		/// </summary>
		public IList<SecurityJump> Jumps => _jumps;

		/// <summary>
		/// Selected rollover.
		/// </summary>
		public SecurityJump SelectedJump => (SecurityJump)JumpsGrid.SelectedItem;

		/// <summary>
		/// Selected rollovers.
		/// </summary>
		public IEnumerable<SecurityJump> SelectedJumps => JumpsGrid.SelectedItems.Cast<SecurityJump>().ToArray();

		/// <summary>
		/// The rollover change event.
		/// </summary>
		public event Action Changed;

		/// <summary>
		/// The rollover change event.
		/// </summary>
		public event Action<SecurityJump> JumpSelected;

		/// <summary>
		/// Initializes a new instance of the <see cref="SecurityJumpsEditor"/>.
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
		/// To check for proper input.
		/// </summary>
		/// <returns>Error detais.</returns>
		public string Validate()
		{
			if (!_jumps.Any())
				return LocalizedStrings.Str1449;

			if (_jumps.Any(j => j.Security == null))
				return LocalizedStrings.Str1450;

			if (_jumps.Any(j => j.Security is BasketSecurity))
				return LocalizedStrings.Str1451;

			if (_jumps.Any(j => j.Date.IsDefault()))
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