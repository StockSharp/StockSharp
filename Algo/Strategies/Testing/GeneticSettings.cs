namespace StockSharp.Algo.Strategies.Testing;

using System;

using Ecng.ComponentModel;
using Ecng.Serialization;

using GeneticSharp;

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
	public int PopulationSize { get; set; } = 8;

	/// <summary>
	/// The maximum population.
	/// </summary>
	public int PopulationSizeMaximum { get; set; } = 16;

	/// <summary>
	/// <see cref="FitnessStagnationTermination"/>
	/// </summary>
	public int StagnationGenerations { get; set; } = 10;

	/// <summary>
	/// <see cref="GeneticAlgorithm.MutationProbability"/>
	/// </summary>
	public float MutationProbability { get; set; } = GeneticAlgorithm.DefaultMutationProbability;

	/// <summary>
	/// <see cref="GeneticAlgorithm.CrossoverProbability"/>
	/// </summary>
	public float CrossoverProbability { get; set; } = GeneticAlgorithm.DefaultCrossoverProbability;

	private Type _reinsertion = typeof(ElitistReinsertion);

	/// <summary>
	/// <see cref="IReinsertion"/>
	/// </summary>
	[ItemsSource(typeof(ReinsertionItemsSource))]
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
	public Type Selection
	{
		get => _selection;
		set => _selection = value ?? throw new ArgumentNullException(nameof(value));
	}

	void IPersistable.Load(SettingsStorage storage)
	{
		PopulationSize = storage.GetValue(nameof(PopulationSize), PopulationSize);
		PopulationSizeMaximum = storage.GetValue(nameof(PopulationSizeMaximum), PopulationSizeMaximum);
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
			.Set(nameof(PopulationSize), PopulationSize)
			.Set(nameof(PopulationSizeMaximum), PopulationSizeMaximum)
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
