namespace StockSharp.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.ComponentModel;
	using Ecng.Xaml;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The table showing portfolios and positions.
	/// </summary>
	public partial class PortfolioGrid
	{
		private sealed class PositionItem : NotifiableObject
		{
			public PositionItem(BasePosition position)
			{
				if (position == null)
					throw new ArgumentNullException(nameof(position));

				Position = position;
				Portfolio = position as Portfolio;

				if (Portfolio == null)
				{
					var pos = (Position)position;

					PortfolioName = pos.Portfolio.Name;
					Name = pos.Security.Id;
					DepoName = pos.DepoName;
					LimitType = pos.LimitType;
					Portfolio = pos.Portfolio;
				}
				else
				{
					PortfolioName = Portfolio.Name;
					Name = LocalizedStrings.Str1543;
				}
			}

			public string PortfolioName { get; private set; }

			public string Name { get; private set; }

			public BasePosition Position { get; }

			public Portfolio Portfolio { get; }

			public string DepoName { get; private set; }

			public TPlusLimits? LimitType { get; private set; }
		}

		private readonly ConvertibleObservableCollection<BasePosition, PositionItem> _positions;

		/// <summary>
		/// Initializes a new instance of the <see cref="PortfolioGrid"/>.
		/// </summary>
		public PortfolioGrid()
		{
			InitializeComponent();

			var itemsSource = new ObservableCollectionEx<PositionItem>();
			ItemsSource = itemsSource;

			_positions = new ConvertibleObservableCollection<BasePosition, PositionItem>(new ThreadSafeObservableCollection<PositionItem>(itemsSource), p => new PositionItem(p));

			GroupingColumns.Add(Columns[0]);
		}

		/// <summary>
		/// The list of portfolios added to the table.
		/// </summary>
		public IListEx<BasePosition> Portfolios => _positions;

		/// <summary>
		/// The list of positions added to the table.
		/// </summary>
		public IListEx<BasePosition> Positions => _positions;

		/// <summary>
		/// The selected position.
		/// </summary>
		public BasePosition SelectedPosition => SelectedPositions.FirstOrDefault();

		/// <summary>
		/// Selected trades.
		/// </summary>
		public IEnumerable<BasePosition> SelectedPositions
		{
			get { return SelectedItems.Cast<PositionItem>().Select(p => p.Position); }
		}
	}
}