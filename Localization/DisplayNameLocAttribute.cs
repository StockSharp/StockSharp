namespace StockSharp.Localization
{
	using System;
	using System.ComponentModel;

	using Ecng.Common;

	/// <summary>
	/// Specifies the display name for a property, event, or public void method which takes no arguments.
	/// </summary>
	[AttributeUsageAttribute(AttributeTargets.Class|AttributeTargets.Method|AttributeTargets.Property|AttributeTargets.Event)]
	public class DisplayNameLocAttribute : DisplayNameAttribute
	{
		/// <summary>
		/// Initializes a new instance of the DisplayNameLocAttribute class using specified resource id for the display name.
		/// </summary>
		/// <param name="resourceId">String resource id.</param>
		public DisplayNameLocAttribute(string resourceId)
			: base(LocalizedStrings.GetString(resourceId))
		{
		}

		/// <summary>
		/// Initializes a new instance of the DisplayNameLocAttribute class using specified resource id for the display name.
		/// </summary>
		/// <param name="resourceId">String resource id.</param>
		/// <param name="arg">Arg for formatted string.</param>
		public DisplayNameLocAttribute(string resourceId, string arg)
			: base(LocalizedStrings.GetString(resourceId).Put(arg))
		{
		}

		/// <summary>
		/// Initializes a new instance of the DisplayNameLocAttribute class using specified resource id for the display name.
		/// </summary>
		/// <param name="resourceId">String resource id.</param>
		/// <param name="args">Args for formatted string.</param>
		[CLSCompliant(false)]
		public DisplayNameLocAttribute(string resourceId, params object[] args)
			: base(LocalizedStrings.GetString(resourceId).Put(args))
		{
		}
	}
}
