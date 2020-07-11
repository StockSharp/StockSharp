namespace StockSharp.Algo
{
	using System;

	partial class MarketRuleHelper
	{
		private abstract class SubscriptionRule : MarketRule<ISubscriptionProvider, Subscription>
		{
			protected SubscriptionRule(ISubscriptionProvider provider, Subscription subscription)
				: base(provider)
			{
				Provider = provider;
				Subscription = subscription ?? throw new ArgumentNullException(nameof(subscription));
			}

			protected ISubscriptionProvider Provider { get; }
			protected Subscription Subscription { get; }
		}

		private class SubscriptionStartedRule : SubscriptionRule
		{
			public SubscriptionStartedRule(ISubscriptionProvider provider, Subscription subscription)
				: base(provider, subscription)
			{
				Name = $"{subscription.TransactionId}/{subscription.DataType} started";
				Provider.SubscriptionStarted += ProviderOnSubscriptionStarted;
			}

			private void ProviderOnSubscriptionStarted(Subscription subscription)
			{
				if (Subscription == subscription)
					Activate(subscription);
			}

			protected override void DisposeManaged()
			{
				Provider.SubscriptionStarted -= ProviderOnSubscriptionStarted;
				base.DisposeManaged();
			}
		}

		/// <summary>
		/// To create a rule for the event of started subscription.
		/// </summary>
		/// <param name="provider">Subscription provider.</param>
		/// <param name="subscription">Subscription.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<ISubscriptionProvider, Subscription> WhenSubscriptionStarted(this ISubscriptionProvider provider, Subscription subscription)
		{
			return new SubscriptionStartedRule(provider, subscription);
		}

		private class SubscriptionOnlineRule : SubscriptionRule
		{
			public SubscriptionOnlineRule(ISubscriptionProvider provider, Subscription subscription)
				: base(provider, subscription)
			{
				Name = $"{subscription.TransactionId}/{subscription.DataType} online";
				Provider.SubscriptionOnline += ProviderOnSubscriptionOnline;
			}

			private void ProviderOnSubscriptionOnline(Subscription subscription)
			{
				if (Subscription == subscription)
					Activate(subscription);
			}

			protected override void DisposeManaged()
			{
				Provider.SubscriptionOnline -= ProviderOnSubscriptionOnline;
				base.DisposeManaged();
			}
		}

		/// <summary>
		/// To create a rule for the event of online subscription.
		/// </summary>
		/// <param name="provider">Subscription provider.</param>
		/// <param name="subscription">Subscription.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<ISubscriptionProvider, Subscription> WhenSubscriptionOnline(this ISubscriptionProvider provider, Subscription subscription)
		{
			return new SubscriptionOnlineRule(provider, subscription);
		}

		private class SubscriptionStoppedRule : MarketRule<ISubscriptionProvider, Tuple<Subscription, Exception>>
		{
			private readonly ISubscriptionProvider _provider;
			private readonly Subscription _subscription;

			public SubscriptionStoppedRule(ISubscriptionProvider provider, Subscription subscription)
				: base(provider)
			{
				_provider = provider ?? throw new ArgumentNullException(nameof(provider));
				_subscription = subscription ?? throw new ArgumentNullException(nameof(subscription));

				Name = $"{subscription.TransactionId}/{subscription.DataType} stopped";
				
				_provider.SubscriptionStopped += ProviderOnSubscriptionStopped;
			}

			private void ProviderOnSubscriptionStopped(Subscription subscription, Exception error)
			{
				if (_subscription == subscription)
					Activate(Tuple.Create(subscription, error));
			}

			protected override void DisposeManaged()
			{
				_provider.SubscriptionStopped -= ProviderOnSubscriptionStopped;
				base.DisposeManaged();
			}
		}

		/// <summary>
		/// To create a rule for the event of stopped subscription.
		/// </summary>
		/// <param name="provider">Subscription provider.</param>
		/// <param name="subscription">Subscription.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<ISubscriptionProvider, Tuple<Subscription, Exception>> WhenSubscriptionStopped(this ISubscriptionProvider provider, Subscription subscription)
		{
			return new SubscriptionStoppedRule(provider, subscription);
		}

		private class SubscriptionFailedRule : MarketRule<ISubscriptionProvider, Tuple<Subscription, Exception, bool>>
		{
			private readonly ISubscriptionProvider _provider;
			private readonly Subscription _subscription;

			public SubscriptionFailedRule(ISubscriptionProvider provider, Subscription subscription)
				: base(provider)
			{
				_provider = provider ?? throw new ArgumentNullException(nameof(provider));
				_subscription = subscription ?? throw new ArgumentNullException(nameof(subscription));
				
				Name = $"{subscription.TransactionId}/{subscription.DataType} failed";
				
				_provider.SubscriptionFailed += ProviderOnSubscriptionFailed;
			}

			private void ProviderOnSubscriptionFailed(Subscription subscription, Exception error, bool isSubscribe)
			{
				if (_subscription == subscription)
					Activate(Tuple.Create(subscription, error, isSubscribe));
			}

			protected override void DisposeManaged()
			{
				_provider.SubscriptionFailed -= ProviderOnSubscriptionFailed;
				base.DisposeManaged();
			}
		}

		/// <summary>
		/// To create a rule for the event of failed subscription.
		/// </summary>
		/// <param name="provider">Subscription provider.</param>
		/// <param name="subscription">Subscription.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<ISubscriptionProvider, Tuple<Subscription, Exception, bool>> WhenSubscriptionFailed(this ISubscriptionProvider provider, Subscription subscription)
		{
			return new SubscriptionFailedRule(provider, subscription);
		}
	}
}
