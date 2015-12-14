#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.StudioPublic
File: ContentToolWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio
{
	using StockSharp.Studio.Core;

	public partial class ContentToolWindow : IContentWindow
	{
		public ContentToolWindow()
		{
			InitializeComponent();
		}

	    public string Id { get; set; }

	    public IStudioControl Control
		{
			get { return (IStudioControl)DataContext; }
			set
			{
				DataContext = value;
				Content = value;
			}
		}
	}
}