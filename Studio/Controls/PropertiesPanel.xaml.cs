namespace StockSharp.Studio.Controls
{
	using System;
	using System.Windows;

	using Ecng.Configuration;
	using Ecng.Serialization;

	using StockSharp.Studio.Core;
	using StockSharp.Studio.Core.Commands;
	using StockSharp.Localization;

	[DisplayNameLoc(LocalizedStrings.Str1507Key)]
	[DescriptionLoc(LocalizedStrings.Str3270Key)]
	public partial class PropertiesPanel : IStudioControl
	{
		public PropertiesPanel()
		{
			InitializeComponent();

			Type prevType = null;
			var service = ConfigManager.GetService<IStudioCommandService>();
			service.Register<SelectCommand>(this, true, cmd =>
			{
				if (cmd.Instance == null)
				{
					// если NULL отправил какая-то другая часть Студии, не активная в данный момент, то нет причины сбрасывать свойства
					if (cmd.InstanceType != prevType)
						return;
				}

				prevType = cmd.InstanceType;

				var textValue = cmd.Instance as string;
				if (textValue != null)
				{
					WatermarkTextBlock.Text = textValue;
					WatermarkTextBlock.Visibility = Visibility.Visible;

					PropertyGrid.SelectedObject = null;
				}
				else
				{
					WatermarkTextBlock.Visibility = Visibility.Collapsed;

					// если выделен этот же объект, то надо сбросить выделение
					if (PropertyGrid.SelectedObject == cmd.Instance)
						PropertyGrid.SelectedObject = null;

					PropertyGrid.IsReadOnly = !cmd.CanEdit;
					PropertyGrid.SelectedObject = cmd.Instance;
				}
			});
		}

		void IPersistable.Save(SettingsStorage settings)
		{
		}

		void IPersistable.Load(SettingsStorage settings)
		{
		}

		void IDisposable.Dispose()
		{
			var service = ConfigManager.GetService<IStudioCommandService>();
			service.UnRegister<SelectCommand>(this);
		}

		string IStudioControl.Title
		{
			get { return LocalizedStrings.Str1507; }
		}

		Uri IStudioControl.Icon
		{
			get { return null; }
		}
	}
}