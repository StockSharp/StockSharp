#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Localization.Localization
File: DisplayNameLocAttribute.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Localization
{
	using System;
	using System.ComponentModel;

	using Ecng.Common;

	/// <summary>
	/// Specifies the display name for a property, event, or public void method which takes no arguments.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Event)]
	public class DisplayNameLocAttribute : DisplayNameAttribute
	{
		/// <summary>
		/// String resource id.
		/// </summary>
		public string ResourceId { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="DisplayNameLocAttribute"/> class using specified resource id for the display name.
		/// </summary>
		/// <param name="resourceId">String resource id.</param>
		public DisplayNameLocAttribute(string resourceId)
			: base(LocalizedStrings.GetString(resourceId))
		{
			ResourceId = resourceId;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DisplayNameLocAttribute"/> class using specified resource id for the display name.
		/// </summary>
		/// <param name="resourceId">String resource id.</param>
		/// <param name="arg">Arg for formatted string.</param>
		public DisplayNameLocAttribute(string resourceId, string arg)
			: base(LocalizedStrings.GetString(resourceId).Put(LocalizedStrings.GetString(arg)))
		{
			ResourceId = resourceId;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DisplayNameLocAttribute"/> class using specified resource id for the display name.
		/// </summary>
		/// <param name="resourceId">String resource id.</param>
		/// <param name="args">Args for formatted string.</param>
		[CLSCompliant(false)]
		public DisplayNameLocAttribute(string resourceId, params object[] args)
			: base(LocalizedStrings.GetString(resourceId).Put(args))
		{
			ResourceId = resourceId;
		}
	}
}