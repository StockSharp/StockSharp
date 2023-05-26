namespace StockSharp.Algo.Strategies.Optimization;

using System;
using System.ComponentModel.DataAnnotations;

using Ecng.ComponentModel;
using Ecng.Serialization;

using GeneticSharp;

using StockSharp.Localization;

/// <summary>
/// Genetic settings.
/// </summary>
public class GeneticSettings : IPersistable
{
	private class ReinsertionItemsSource : ItemsSourceBase<Type>
	{
        public ReinsertionItemsSource()
			: base(ReinsertionService.GetReinsertionTypes())
        {
        }
    }

	private class MutationItemsSource : ItemsSourceBase<Type>
	{
		public MutationItemsSource()
			: base(MutationService.GetMutationTypes())
		{
		}
	}

	private class CrossoverItemsSource : ItemsSourceBase<Type>
	{
		public CrossoverItemsSource()
			: base(CrossoverService.GetCrossoverTypes())
		{
		}
	}

	private class SelectionItemsSource : ItemsSourceBase<Type>
	{
		public SelectionItemsSource()
			: base(SelectionService.GetSelectionTypes())
		{
		}
	}

	/// <summary>
	/// The initial size of population.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PopulationKey,
		Description = LocalizedStrings.PopulationDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 0)]
	public int Population { get; set; } = 8;

	/// <summary>
	/// The maximum population.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PopulationMaxKey,
		Description = LocalizedStrings.PopulationMaxDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 1)]
	public int PopulationMax { get; set; } = 16;

	/// <summary>
	/// The genetic algorithm will be terminate when the best chromosome's fitness has no change in the last generations specified.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.StagnationKey,
		Description = LocalizedStrings.StagnationDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 2)]
	public int StagnationGenerations { get; set; } = 10;

	/// <summary>
	/// <see cref="GeneticAlgorithm.MutationProbability"/>
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.MutationProbabilityKey,
		Description = LocalizedStrings.MutationProbabilityDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 3)]
	public decimal MutationProbability { get; set; } = (decimal)GeneticAlgorithm.DefaultMutationProbability;

	/// <summary>
	/// <see cref="GeneticAlgorithm.CrossoverProbability"/>
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.CrossoverProbabilityKey,
		Description = LocalizedStrings.CrossoverProbabilityDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 4)]
	public decimal CrossoverProbability { get; set; } = (decimal)GeneticAlgorithm.DefaultCrossoverProbability;

	private Type _reinsertion = typeof(ElitistReinsertion);

	/// <summary>
	/// <see cref="IReinsertion"/>
	/// </summary>
	[ItemsSource(typeof(ReinsertionItemsSource))]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ReinsertionKey,
		Description = LocalizedStrings.ReinsertionDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 5)]
	public Type Reinsertion
	{
		get => _reinsertion;
		set => _reinsertion = value ?? throw new ArgumentNullException(nameof(value));
	}

	private Type _mutation = typeof(UniformMutation);

	/// <summary>
	/// <see cref="IMutation"/>
	/// </summary>
	[ItemsSource(typeof(MutationItemsSource))]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.MutationKey,
		Description = LocalizedStrings.MutationDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 6)]
	public Type Mutation
	{
		get => _mutation;
		set => _mutation = value ?? throw new ArgumentNullException(nameof(value));
	}

	private Type _crossover = typeof(OnePointCrossover);

	/// <summary>
	/// <see cref="ICrossover"/>
	/// </summary>
	[ItemsSource(typeof(CrossoverItemsSource))]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.CrossoverKey,
		Description = LocalizedStrings.CrossoverDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 7)]
	public Type Crossover
	{
		get => _crossover;
		set => _crossover = value ?? throw new ArgumentNullException(nameof(value));
	}

	private Type _selection = typeof(TournamentSelection);

	/// <summary>
	/// <see cref="ISelection"/>
	/// </summary>
	[ItemsSource(typeof(SelectionItemsSource))]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SelectionKey,
		Description = LocalizedStrings.SelectionDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 8)]
	public Type Selection
	{
		get => _selection;
		set => _selection = value ?? throw new ArgumentNullException(nameof(value));
	}

	void IPersistable.Load(SettingsStorage storage)
	{
		Population = storage.GetValue(nameof(Population), Population);
		PopulationMax = storage.GetValue(nameof(PopulationMax), PopulationMax);
		StagnationGenerations = storage.GetValue(nameof(StagnationGenerations), StagnationGenerations);
		MutationProbability = storage.GetValue(nameof(MutationProbability), MutationProbability);
		CrossoverProbability = storage.GetValue(nameof(CrossoverProbability), CrossoverProbability);
		Reinsertion = storage.GetValue(nameof(Reinsertion), Reinsertion);
		Mutation = storage.GetValue(nameof(Mutation), Mutation);
		Crossover = storage.GetValue(nameof(Crossover), Crossover);
		Selection = storage.GetValue(nameof(Selection), Selection);
	}

	void IPersistable.Save(SettingsStorage storage)
	{
		storage
			.Set(nameof(Population), Population)
			.Set(nameof(PopulationMax), PopulationMax)
			.Set(nameof(StagnationGenerations), StagnationGenerations)
			.Set(nameof(MutationProbability), MutationProbability)
			.Set(nameof(CrossoverProbability), CrossoverProbability)
			.Set(nameof(Reinsertion), Reinsertion)
			.Set(nameof(Mutation), Mutation)
			.Set(nameof(Crossover), Crossover)
			.Set(nameof(Selection), Selection)
		;
	}
}
