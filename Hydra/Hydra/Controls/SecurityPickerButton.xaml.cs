namespace StockSharp.Hydra.Controls
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Configuration;
	using Ecng.Xaml;
	using Ecng.Common;

	using StockSharp.BusinessEntities;
	using StockSharp.Xaml;

	public partial class SecurityPickerButton
	{
		private readonly object _contentTemplate;

		public SecurityPickerButton()
		{
			InitializeComponent();
			_contentTemplate = Content;
		}

		public Security SelectedSecurity
		{
			get { return SelectedSecurities.FirstOrDefault(); }
			set
			{
				SelectedSecurities = value == null ? Enumerable.Empty<Security>() : new[] { value };
			}
		}

		private IEnumerable<Security> _selectedSecurities = Enumerable.Empty<Security>();

		public IEnumerable<Security> SelectedSecurities
		{
			get { return _selectedSecurities; }
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				_selectedSecurities = value;
				Content = value.IsEmpty() ? _contentTemplate : value.Select(s => s.Id).Join(", ");
				SecuritySelected.SafeInvoke();
			}
		}

		public event Action SecuritySelected;

		protected override void OnClick()
		{
			base.OnClick();

			var wnd = new SecurityPickerWindow
			{
				SecurityProvider = ConfigManager.GetService<ISecurityProvider>(),
				//SelectionMode = DataGridSelectionMode.Single
			};
			wnd.ExcludeSecurities.Add(Core.Extensions.GetAllSecurity());
			wnd.SelectedSecurities.AddRange(SelectedSecurities);

			if (!wnd.ShowModal(this))
				return;

			SelectedSecurities = wnd.SelectedSecurities.ToArray();
		}
	}
}