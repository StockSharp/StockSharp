#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.Core.CorePublic
File: TemplateTxtRegistry.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.Core
{
	using Ecng.Serialization;

	using StockSharp.Localization;
	using StockSharp.Messages;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	/// <summary>
	/// Реестр txt шаблонов.
	/// </summary>
	public class TemplateTxtRegistry : IPersistable
	{
		/// <summary>
		/// Создать <see cref="TemplateTxtRegistry"/>.
		/// </summary>
		public TemplateTxtRegistry()
		{
			TemplateTxtCandle = typeof(TimeFrameCandleMessage).GetTxtTemplate();
			TemplateTxtDepth = typeof(QuoteChangeMessage).GetTxtTemplate();
			TemplateTxtLevel1 = typeof(Level1ChangeMessage).GetTxtTemplate();
			TemplateTxtOrderLog = typeof(ExecutionMessage).GetTxtTemplate(ExecutionTypes.OrderLog);
			TemplateTxtSecurity = typeof(SecurityMessage).GetTxtTemplate();
			TemplateTxtTick = typeof(ExecutionMessage).GetTxtTemplate(ExecutionTypes.Tick);
			TemplateTxtTransaction = typeof(ExecutionMessage).GetTxtTemplate(ExecutionTypes.Transaction);
			TemplateTxtNews = typeof(NewsMessage).GetTxtTemplate();
		}

		/// <summary>
		/// Шаблон экспорта в txt для стаканов.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.TemplateDepthKey)]
		[DescriptionLoc(LocalizedStrings.TemplateTxtDepthKey)]
		[PropertyOrder(0)]
		public string TemplateTxtDepth { get; set; }

		/// <summary>
		/// Шаблон экспорта в txt для тиков.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.TemplateTickKey)]
		[DescriptionLoc(LocalizedStrings.TemplateTxtTickKey)]
		[PropertyOrder(1)]
		public string TemplateTxtTick { get; set; }

		/// <summary>
		/// Шаблон экспорта в txt для свечей.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.TemplateCandleKey)]
		[DescriptionLoc(LocalizedStrings.TemplateTxtCandleKey)]
		[PropertyOrder(2)]
		public string TemplateTxtCandle { get; set; }

		/// <summary>
		/// Шаблон экспорта в txt для level1.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.TemplateLevel1Key)]
		[DescriptionLoc(LocalizedStrings.TemplateTxtLevel1Key)]
		[PropertyOrder(3)]
		public string TemplateTxtLevel1 { get; set; }

		/// <summary>
		/// Шаблон экспорта в txt для лога заявок.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.TemplateOrderLogKey)]
		[DescriptionLoc(LocalizedStrings.TemplateTxtOrderLogKey)]
		[PropertyOrder(4)]
		public string TemplateTxtOrderLog { get; set; }

		/// <summary>
		/// Шаблон экспорта в txt для транзакций.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.TemplateTransactionKey)]
		[DescriptionLoc(LocalizedStrings.TemplateTxtTransactionKey)]
		[PropertyOrder(5)]
		public string TemplateTxtTransaction { get; set; }

		/// <summary>
		/// Шаблон экспорта в txt для инструментов.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.TemplateSecurityKey)]
		[DescriptionLoc(LocalizedStrings.TemplateTxtSecurityKey)]
		[PropertyOrder(6)]
		public string TemplateTxtSecurity { get; set; }

		/// <summary>
		/// Шаблон экспорта в txt для новостей.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.TemplateNewsKey)]
		[DescriptionLoc(LocalizedStrings.TemplateTxtNewsKey)]
		[PropertyOrder(7)]
		public string TemplateTxtNews { get; set; }

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public void Load(SettingsStorage storage)
		{
			TemplateTxtDepth = storage.GetValue(nameof(TemplateTxtDepth), TemplateTxtDepth);
			TemplateTxtTick = storage.GetValue(nameof(TemplateTxtTick), TemplateTxtTick);
			TemplateTxtCandle = storage.GetValue(nameof(TemplateTxtCandle), TemplateTxtCandle);
			TemplateTxtLevel1 = storage.GetValue(nameof(TemplateTxtLevel1), TemplateTxtLevel1);
			TemplateTxtOrderLog = storage.GetValue(nameof(TemplateTxtOrderLog), TemplateTxtOrderLog);
			TemplateTxtTransaction = storage.GetValue(nameof(TemplateTxtTransaction), TemplateTxtTransaction);
			TemplateTxtSecurity = storage.GetValue(nameof(TemplateTxtSecurity), TemplateTxtSecurity);
			TemplateTxtNews = storage.GetValue(nameof(TemplateTxtNews), TemplateTxtNews);
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(TemplateTxtDepth), TemplateTxtDepth);
			storage.SetValue(nameof(TemplateTxtTick), TemplateTxtTick);
			storage.SetValue(nameof(TemplateTxtCandle), TemplateTxtCandle);
			storage.SetValue(nameof(TemplateTxtLevel1), TemplateTxtLevel1);
			storage.SetValue(nameof(TemplateTxtOrderLog), TemplateTxtOrderLog);
			storage.SetValue(nameof(TemplateTxtTransaction), TemplateTxtTransaction);
			storage.SetValue(nameof(TemplateTxtSecurity), TemplateTxtSecurity);
			storage.SetValue(nameof(TemplateTxtNews), TemplateTxtNews);
		}
	}
}