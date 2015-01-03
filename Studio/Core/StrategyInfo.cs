namespace StockSharp.Studio.Core
{
	using System;
	using System.ComponentModel;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Serialization;

	using StockSharp.Algo;
	using StockSharp.Algo.Strategies;
	using StockSharp.Localization;

	public enum StrategyInfoStates
	{
		[EnumDisplayNameLoc(LocalizedStrings.Str3178Key)]
		Stopped,

		[EnumDisplayNameLoc(LocalizedStrings.Str3179Key)]
		Runned,
	}

	public enum StrategyInfoTypes
	{
		[EnumDisplayNameLoc(LocalizedStrings.Str3180Key)]
		SourceCode,

		[EnumDisplayNameLoc(LocalizedStrings.Str3181Key)]
		Diagram,

		[EnumDisplayNameLoc(LocalizedStrings.Str3182Key)]
		Assembly,

		[EnumDisplayNameLoc(LocalizedStrings.Str1221Key)]
		Analytics,

		[EnumDisplayNameLoc(LocalizedStrings.Str3183Key)]
		Terminal,
	}

	[DisplayNameLoc(LocalizedStrings.Str1320Key)]
	[DescriptionLoc(LocalizedStrings.Str3184Key)]
	public class StrategyInfo : NotifiableObject
	{
		public StrategyInfo()
		{
            _strategies = new SynchronizedList<StrategyContainer>();
			_strategies.Added += s => s.ProcessStateChanged += StrategyProcessStateChanged;
			_strategies.Removed += s => s.ProcessStateChanged -= StrategyProcessStateChanged;
		}

		[Identity]
		[Field("Id", ReadOnly = true)]
		[Browsable(false)]
		public long Id { get; set; }

		private string _name;

		[DisplayNameLoc(LocalizedStrings.NameKey)]
		[DescriptionLoc(LocalizedStrings.Str1359Key)]
		[CategoryLoc(LocalizedStrings.Str1559Key)]
		public string Name
		{
			get { return _name; }
			set
			{
				_name = value;
				NotifyChanged("Name");
			}
		}

		private string _description;

		[DisplayNameLoc(LocalizedStrings.Str268Key)]
		[DescriptionLoc(LocalizedStrings.Str3185Key)]
		[CategoryLoc(LocalizedStrings.Str1559Key)]
		public string Description
		{
			get { return _description; }
			set
			{
				_description = value;
				NotifyChanged("Description");
			}
		}

		[DisplayNameLoc(LocalizedStrings.StateKey)]
		[DescriptionLoc(LocalizedStrings.Str3186Key)]
		[CategoryLoc(LocalizedStrings.Str1559Key)]
		public StrategyInfoStates State
		{
			get
			{
				return Strategies.Any(s => s.ProcessState != ProcessStates.Stopped)
					? StrategyInfoStates.Runned
					: StrategyInfoStates.Stopped;
			}
		}

		[DisplayNameLoc(LocalizedStrings.Str359Key)]
		[DescriptionLoc(LocalizedStrings.Str3187Key)]
		[CategoryLoc(LocalizedStrings.Str1559Key)]
		[ReadOnly(true)]
		public StrategyInfoTypes Type { get; set; }

		private string _body;

		[Browsable(false)]
	    public string Body
	    {
	        get { return _body; }
			set
			{
				_body = value;
				NotifyChanged("Body");
			}
	    }

		private Type _strategyType;

		[Browsable(false)]
		[Ignore]
		public Type StrategyType
		{
			get { return _strategyType; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				if (_strategyType == value)
					return;

				_strategyType = value;
				_strategyTypeName = value.GetTypeName(false);

				NotifyChanged("StrategyType");
				NotifyChanged("StrategyTypeName");
			}
		}

		private string _strategyTypeName;

		[DisplayNameLoc(LocalizedStrings.Str3188Key)]
		[DescriptionLoc(LocalizedStrings.Str3189Key)]
		[CategoryLoc(LocalizedStrings.Str1559Key)]
		[ReadOnly(true)]
		public string StrategyTypeName
		{
			get { return _strategyTypeName; }
			set
			{
				if (StrategyType != null)
					throw new InvalidOperationException(LocalizedStrings.Str3190Params.Put(StrategyType.Name));

				_strategyTypeName = value;
				NotifyChanged("StrategyTypeName");
			}
		}

        private readonly SynchronizedList<StrategyContainer> _strategies;
		
		[Browsable(false)]
		[Ignore]
		public SynchronizedList<StrategyContainer> Strategies
		{
			get { return _strategies; }
		}

		[DisplayNameLoc(LocalizedStrings.Str2804Key)]
		[DescriptionLoc(LocalizedStrings.Str3191Key)]
		[CategoryLoc(LocalizedStrings.Str1559Key)]
		[ReadOnly(true)]
		public string Path { get; set; }

		[Browsable(false)]
		public byte[] Assembly { get; set; }

		[RelationSingle]
		[Browsable(false)]
		public Session Session { get; set; }

		public StrategyInfo Clone()
		{
			return new StrategyInfo
			{
				Id = Id,
				Name = Name,
				Description = Description,
				Type = Type,
				Body = Body,
				//CompiledType = CompiledType,
				StrategyType = StrategyType,
				Assembly = Assembly,
				Path = Path,
			};
		}

		private void StrategyProcessStateChanged(Strategy s)
		{
			NotifyChanged("State");
		}
	}
}