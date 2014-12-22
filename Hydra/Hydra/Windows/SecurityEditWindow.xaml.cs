namespace StockSharp.Hydra.Windows
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
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

			if (DesignerProperties.GetIsInDesignMode(this))
				return;

			_entityRegistry = ConfigManager.GetService<IEntityRegistry>();
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
				VolumeStep.Text = value.VolumeStep.To<string>();
				PriceStep.Text = value.PriceStep.To<string>();
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

				VolumeStep.Text = value.All(s => s.VolumeStep == firstSecurity.VolumeStep) ? firstSecurity.VolumeStep.To<string>() : string.Empty;
				PriceStep.Text = value.All(s => s.PriceStep == firstSecurity.PriceStep) ? firstSecurity.PriceStep.To<string>() : string.Empty;
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
			var volumeStep = VolumeStep.Text.To<decimal?>();
			var priceStep = PriceStep.Text.To<decimal?>();
			var type = TypeCtrl.SelectedType;

			if (Securities == null)
			{
				if (Code.Text.IsEmpty())
				{
					ShowError(LocalizedStrings.Str2923);
					return;
				}

				//if (SecName.Text.IsEmpty())
				//{
				//	ShowError("Не заполнено название инструмента.");
				//	return;
				//}

				if (volumeStep == null)
				{
					ShowError(LocalizedStrings.Str2924);
					return;
				}

				if (priceStep == null)
				{
					ShowError(LocalizedStrings.Str2925);
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

				security.VolumeStep = volumeStep.Value;
				security.PriceStep = priceStep.Value;
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
				foreach (var security in Securities)
				{
					if (volumeStep != null)
						security.VolumeStep = volumeStep.Value;

					if (priceStep != null)
						security.PriceStep = priceStep.Value;

					if (type != null)
						security.Type = type.Value;

					_entityRegistry.Securities.Save(security);
				}
			}

			DialogResult = true;
		}
	}
}