namespace StockSharp.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;

	using Ecng.Xaml;
	using Ecng.Common;
	using Ecng.Collections;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.Localization;

	/// <summary>
	/// Окно для создания или редактирования <see cref="ContinuousSecurity"/>.
	/// </summary>
	public partial class ContinuousSecurityWindow : ISecurityWindow
	{
		/// <summary>
		/// Создать <see cref="ContinuousSecurityWindow"/>.
		/// </summary>
		public ContinuousSecurityWindow()
		{
			InitializeComponent();

			SecurityId.Security = new ContinuousSecurity
			{
				ExtensionInfo = new Dictionary<object, object>()
			};
		}

		private Func<string, string> _validateId = id => null;

		/// <summary>
		/// Обработчик, проверяющий доступность введенного идентификатора для <see cref="Security"/>.
		/// </summary>
		public Func<string, string> ValidateId
		{
			get { return _validateId; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_validateId = value;
			}
		}

		Security ISecurityWindow.Security
		{
			get { return ContinuousSecurity; }
			set { ContinuousSecurity = (ContinuousSecurity)value; }
		}

		/// <summary>
		/// Поставщик информации об инструментах.
		/// </summary>
		public ISecurityProvider SecurityProvider { get; set; }

		/// <summary>
		/// Непрерывный инструмент.
		/// </summary>
		public ContinuousSecurity ContinuousSecurity
		{
			get { return (ContinuousSecurity)SecurityId.Security; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				SecurityId.Security = value;

				JumpsGrid.Jumps.Clear();
				JumpsGrid.Jumps.AddRange(value.ExpirationJumps.Select(p => new SecurityJump
				{
					Security = p.Key,
					Date = p.Value.UtcDateTime
				}));

				Title += " " + value.Id;
			}
		}

		private void AddRow_Click(object sender, RoutedEventArgs e)
		{
			JumpsGrid.Jumps.Add(new SecurityJump());
		}

		private void RemoveRow_Click(object sender, RoutedEventArgs e)
		{
			JumpsGrid.Jumps.RemoveRange(JumpsGrid.SelectedJumps);
		}

		private void OK_Click(object sender, RoutedEventArgs e)
		{
			var continuousSecurity = ContinuousSecurity;

			var errorMsg = _validateId(SecurityId.Text) ?? SecurityId.ValidateId() ?? JumpsGrid.Validate();

			if (!errorMsg.IsEmpty())
			{
				new MessageBoxBuilder()
					.Owner(this)
					.Error()
					.Text(errorMsg)
					.Show();

				return;
			}

			continuousSecurity.Id = SecurityId.Text;

			var underlyingSecurity = JumpsGrid.Jumps[0].Security;
			continuousSecurity.Board = underlyingSecurity.Board;
			continuousSecurity.PriceStep = underlyingSecurity.PriceStep;
			continuousSecurity.Type = underlyingSecurity.Type;

			continuousSecurity.ExpirationJumps.Clear();
			continuousSecurity.ExpirationJumps.AddRange(JumpsGrid.Jumps.Select(j => new KeyValuePair<Security, DateTimeOffset>(j.Security, j.Date)));

			DialogResult = true;
			Close();
		}

		private void SecurityId_TextChanged(object sender, TextChangedEventArgs e)
		{
			EnableSave();
			Auto.IsEnabled = !SecurityId.Text.IsEmpty();
		}

		private void EnableSave()
		{
			Ok.IsEnabled = !SecurityId.Text.IsEmpty() && !JumpsGrid.Jumps.IsEmpty();
		}

		private void JumpsGrid_OnJumpSelected(SecurityJump jump)
		{
			RemoveRow.IsEnabled = JumpsGrid.SelectedJump != null;
		}

		private void JumpsGrid_OnChanged()
		{
			EnableSave();
		}

		private void Auto_Click(object sender, RoutedEventArgs e)
		{
			var newSecurities = Enumerable.Empty<Security>();

			var split = SecurityId.Text.Split(new[] {'@'});

			if (split.Any() && split[0] != string.Empty)
				newSecurities = ContinuousSecurity.GetFortsJumps(SecurityProvider, split[0], DateTime.Today - TimeSpan.FromTicks(TimeHelper.TicksPerYear * 10), DateTime.Today, false);

			if (!newSecurities.IsEmpty())
			{
				JumpsGrid.Jumps.Clear();
				JumpsGrid.Jumps.AddRange(newSecurities.Select(s => new SecurityJump
				{
					Security = s,
					Date = s.ExpiryDate != null ? s.ExpiryDate.Value.UtcDateTime : DateTime.UtcNow.Date
				}));

				new MessageBoxBuilder()
					.Owner(this)
					.Text(LocalizedStrings.Str1455)
					.Show();
			}
			else
			{
				new MessageBoxBuilder()
					.Owner(this)
					.Error()
					.Text(LocalizedStrings.Str1456)
					.Show();
			}
		}
	}
}