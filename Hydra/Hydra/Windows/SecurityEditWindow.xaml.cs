namespace StockSharp.Hydra.Windows
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Xaml;

	using StockSharp.Algo;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Localization;

	public partial class SecurityEditWindow
	{
		private readonly IEntityRegistry _entityRegistry;

		public SecurityEditWindow()
		{
			InitializeComponent();

			if (this.IsDesignMode())
				return;

			_entityRegistry = ConfigManager.GetService<IEntityRegistry>();

			ExchangeCtrl.ExchangeInfoProvider = ConfigManager.GetService<IExchangeInfoProvider>();
		}

		private bool CanEditCommonInfo
		{
			set
			{
				Code.IsEnabled = value;
				SecName.IsEnabled = value;
			}
		}

		private Security _security;

		public Security Security
		{
			get
			{
				return _security;
			}
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_security = value;

				Code.Text = value.Code;
				SecName.Text = value.Name;
				VolumeStep.Value = value.VolumeStep;
				PriceStep.Value = value.PriceStep;
				Decimals.Value = value.Decimals;
				ExchangeCtrl.SelectedBoard = value.Board;
				TypeCtrl.SelectedType = value.Type;

				CanEditCommonInfo = true;

				Title = _security.Id.IsEmpty() ? LocalizedStrings.Str2921 : LocalizedStrings.Str2922;
				Code.IsReadOnly = !_security.Code.IsEmpty();
				ExchangeCtrl.IsEnabled = _security.Board == null;
			}
		}

		private IEnumerable<Security> _securities;
	
		public IEnumerable<Security> Securities
		{
			get { return _securities; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				if (value.IsEmpty())
					throw new ArgumentOutOfRangeException();

				if (value.Count() == 1)
				{
					Security = value.First();
					return;
				}

				_securities = value;

				ExchangeCtrl.SelectedBoard = null;
				TypeCtrl.SelectedType = null;

				CanEditCommonInfo = false;

				var firstSecurity = value.First();

				VolumeStep.Value = value.All(s => s.VolumeStep == firstSecurity.VolumeStep) ? firstSecurity.VolumeStep : null;
				PriceStep.Value = value.All(s => s.PriceStep == firstSecurity.PriceStep) ? firstSecurity.PriceStep : null;
				Decimals.Value = value.All(s => s.Decimals == firstSecurity.Decimals) ? firstSecurity.Decimals : null;
				ExchangeCtrl.SelectedBoard = value.All(s => s.Board == firstSecurity.Board) ? firstSecurity.Board : null;
				TypeCtrl.SelectedType = value.All(s => s.Type == firstSecurity.Type) ? firstSecurity.Type : null;
			}
		}

		private void ShowError(string text)
		{
			new MessageBoxBuilder()
					.Text(text)
					.Owner(this)
					.Warning()
					.Show();
		}

		private void OkClick(object sender, RoutedEventArgs e)
		{
			var volumeStep = VolumeStep.Value;
			var priceStep = PriceStep.Value;
			var decimals = Decimals.Value;
			var type = TypeCtrl.SelectedType;

			if (Securities == null)
			{
				if (Code.Text.IsEmpty())
				{
					ShowError(LocalizedStrings.Str2923);
					return;
				}

				if (decimals == null)
				{
					ShowError(LocalizedStrings.DecimalsNotFilled);
					return;
				}

				if (priceStep == null || priceStep == 0)
				{
					ShowError(LocalizedStrings.Str2925);
					return;
				}

				if (volumeStep == null || volumeStep == 0)
				{
					ShowError(LocalizedStrings.Str2924);
					return;
				}

				if (ExchangeCtrl.SelectedBoard == null)
				{
					ShowError(LocalizedStrings.Str2926);
					return;
				}

				var security = Security;

				security.Code = Code.Text;

				if (!SecName.Text.IsEmpty())
					security.Name = SecName.Text;

				security.VolumeStep = volumeStep;
				security.PriceStep = priceStep;
				security.Decimals = decimals;
				security.Board = ExchangeCtrl.SelectedBoard;
				security.Type = TypeCtrl.SelectedType;

				if (security.Id.IsEmpty())
				{
					var id = new SecurityIdGenerator().GenerateId(security.Code, security.Board);

					var isExist = _entityRegistry.Securities.ReadById(id) != null;

					if (isExist)
					{
						new MessageBoxBuilder()
							.Text(LocalizedStrings.Str2927Params.Put(id))
							.Owner(this)
							.Warning()
							.Show();

						return;
					}

					security.Id = id;
					security.ExtensionInfo = new Dictionary<object, object>();
				}

				_entityRegistry.Securities.Save(security);
			}
			else
			{
				if (priceStep == 0)
				{
					ShowError(LocalizedStrings.Str2925);
					return;
				}

				if (volumeStep == 0)
				{
					ShowError(LocalizedStrings.Str2924);
					return;
				}

				foreach (var security in Securities)
				{
					if (volumeStep != null)
						security.VolumeStep = volumeStep.Value;

					if (priceStep != null)
						security.PriceStep = priceStep.Value;

					if (decimals != null)
						security.Decimals = decimals.Value;

					if (type != null)
						security.Type = type.Value;

					_entityRegistry.Securities.Save(security);
				}
			}

			DialogResult = true;
		}
	}
}