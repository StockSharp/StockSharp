namespace StockSharp.Algo.Strategies;

using System.Drawing;

using StockSharp.Algo.Indicators;
using StockSharp.Charting;

/// <summary>
/// Subscription handler.
/// </summary>
public interface ISubscriptionHandler : IDisposable
{
	/// <summary>
	/// <see cref="Subscription"/>.
	/// </summary>
	Subscription Subscription { get; }
}

/// <summary>
/// Subscription handler.
/// </summary>
/// <typeparam name="T">Market-data type.</typeparam>
public interface ISubscriptionHandler<T> : ISubscriptionHandler
{
	/// <summary>
	/// Start subscription.
	/// </summary>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	ISubscriptionHandler<T> Start();

	/// <summary>
	/// Start subscription.
	/// </summary>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	ISubscriptionHandler<T> Stop();

	/// <summary>
	/// Bind the subscription.
	/// </summary>
	/// <param name="callback">Callback.</param>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	ISubscriptionHandler<T> Bind(Action<T> callback);

	/// <summary>
	/// Bind indicator to the subscription with possible empty <see cref="IIndicatorValue.IsEmpty"/> values.
	/// </summary>
	/// <param name="indicator">Indicator.</param>
	/// <param name="callback">Callback.</param>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	ISubscriptionHandler<T> BindWithEmpty(IIndicator indicator, Action<T, decimal?> callback);

	/// <summary>
	/// Bind indicator to the subscription.
	/// </summary>
	/// <param name="indicator">Indicator.</param>
	/// <param name="callback">Callback.</param>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	ISubscriptionHandler<T> Bind(IIndicator indicator, Action<T, decimal> callback);

	/// <summary>
	/// Bind indicator to the subscription.
	/// </summary>
	/// <param name="indicator">Indicator.</param>
	/// <param name="callback">Callback.</param>
	/// <param name="allowEmpty">If <see langword="true"/>, then the callback will be called even if the indicator returns empty value.</param>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	ISubscriptionHandler<T> BindEx(IIndicator indicator, Action<T, IIndicatorValue> callback, bool allowEmpty = false);

	/// <summary>
	/// Bind indicators to the subscription with possible empty <see cref="IIndicatorValue.IsEmpty"/> values.
	/// </summary>
	/// <param name="indicator1">Indicator.</param>
	/// <param name="indicator2">Indicator.</param>
	/// <param name="callback">Callback.</param>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	ISubscriptionHandler<T> BindWithEmpty(IIndicator indicator1, IIndicator indicator2, Action<T, decimal?, decimal?> callback);

	/// <summary>
	/// Bind indicators to the subscription.
	/// </summary>
	/// <param name="indicator1">Indicator.</param>
	/// <param name="indicator2">Indicator.</param>
	/// <param name="callback">Callback.</param>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	ISubscriptionHandler<T> Bind(IIndicator indicator1, IIndicator indicator2, Action<T, decimal, decimal> callback);

	/// <summary>
	/// Bind indicators to the subscription.
	/// </summary>
	/// <param name="indicator1">Indicator.</param>
	/// <param name="indicator2">Indicator.</param>
	/// <param name="callback">Callback.</param>
	/// <param name="allowEmpty">If <see langword="true"/>, then the callback will be called even if one of the indicators returns empty value.</param>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	ISubscriptionHandler<T> BindEx(IIndicator indicator1, IIndicator indicator2, Action<T, IIndicatorValue, IIndicatorValue> callback, bool allowEmpty = false);

	/// <summary>
	/// Bind indicators to the subscription with possible empty <see cref="IIndicatorValue.IsEmpty"/> values.
	/// </summary>
	/// <param name="indicator1">Indicator.</param>
	/// <param name="indicator2">Indicator.</param>
	/// <param name="indicator3">Indicator.</param>
	/// <param name="callback">Callback.</param>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	ISubscriptionHandler<T> BindWithEmpty(IIndicator indicator1, IIndicator indicator2, IIndicator indicator3, Action<T, decimal?, decimal?, decimal?> callback);

	/// <summary>
	/// Bind indicators to the subscription.
	/// </summary>
	/// <param name="indicator1">Indicator.</param>
	/// <param name="indicator2">Indicator.</param>
	/// <param name="indicator3">Indicator.</param>
	/// <param name="callback">Callback.</param>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	ISubscriptionHandler<T> Bind(IIndicator indicator1, IIndicator indicator2, IIndicator indicator3, Action<T, decimal, decimal, decimal> callback);

	/// <summary>
	/// Bind indicators to the subscription.
	/// </summary>
	/// <param name="indicator1">Indicator.</param>
	/// <param name="indicator2">Indicator.</param>
	/// <param name="indicator3">Indicator.</param>
	/// <param name="callback">Callback.</param>
	/// <param name="allowEmpty">If <see langword="true"/>, then the callback will be called even if one of the indicators returns empty value.</param>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	ISubscriptionHandler<T> BindEx(IIndicator indicator1, IIndicator indicator2, IIndicator indicator3, Action<T, IIndicatorValue, IIndicatorValue, IIndicatorValue> callback, bool allowEmpty = false);

	/// <summary>
	/// Bind indicators to the subscription with possible empty <see cref="IIndicatorValue.IsEmpty"/> values.
	/// </summary>
	/// <param name="indicator1">Indicator.</param>
	/// <param name="indicator2">Indicator.</param>
	/// <param name="indicator3">Indicator.</param>
	/// <param name="indicator4">Indicator.</param>
	/// <param name="callback">Callback.</param>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	ISubscriptionHandler<T> BindWithEmpty(IIndicator indicator1, IIndicator indicator2, IIndicator indicator3, IIndicator indicator4, Action<T, decimal?, decimal?, decimal?, decimal?> callback);

	/// <summary>
	/// Bind indicators to the subscription.
	/// </summary>
	/// <param name="indicator1">Indicator.</param>
	/// <param name="indicator2">Indicator.</param>
	/// <param name="indicator3">Indicator.</param>
	/// <param name="indicator4">Indicator.</param>
	/// <param name="callback">Callback.</param>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	ISubscriptionHandler<T> Bind(IIndicator indicator1, IIndicator indicator2, IIndicator indicator3, IIndicator indicator4, Action<T, decimal, decimal, decimal, decimal> callback);

	/// <summary>
	/// Bind indicators to the subscription.
	/// </summary>
	/// <param name="indicator1">Indicator.</param>
	/// <param name="indicator2">Indicator.</param>
	/// <param name="indicator3">Indicator.</param>
	/// <param name="indicator4">Indicator.</param>
	/// <param name="callback">Callback.</param>
	/// <param name="allowEmpty">If <see langword="true"/>, then the callback will be called even if one of the indicators returns empty value.</param>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	ISubscriptionHandler<T> BindEx(IIndicator indicator1, IIndicator indicator2, IIndicator indicator3, IIndicator indicator4, Action<T, IIndicatorValue, IIndicatorValue, IIndicatorValue, IIndicatorValue> callback, bool allowEmpty = false);

	/// <summary>
	/// Bind indicators to the subscription with possible empty <see cref="IIndicatorValue.IsEmpty"/> values.
	/// </summary>
	/// <param name="indicator1">Indicator.</param>
	/// <param name="indicator2">Indicator.</param>
	/// <param name="indicator3">Indicator.</param>
	/// <param name="indicator4">Indicator.</param>
	/// <param name="indicator5">Indicator.</param>
	/// <param name="callback">Callback.</param>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	ISubscriptionHandler<T> BindWithEmpty(IIndicator indicator1, IIndicator indicator2, IIndicator indicator3, IIndicator indicator4, IIndicator indicator5, Action<T, decimal?, decimal?, decimal?, decimal?, decimal?> callback);

	/// <summary>
	/// Bind indicators to the subscription.
	/// </summary>
	/// <param name="indicator1">Indicator.</param>
	/// <param name="indicator2">Indicator.</param>
	/// <param name="indicator3">Indicator.</param>
	/// <param name="indicator4">Indicator.</param>
	/// <param name="indicator5">Indicator.</param>
	/// <param name="callback">Callback.</param>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	ISubscriptionHandler<T> Bind(IIndicator indicator1, IIndicator indicator2, IIndicator indicator3, IIndicator indicator4, IIndicator indicator5, Action<T, decimal, decimal, decimal, decimal, decimal> callback);

	/// <summary>
	/// Bind indicators to the subscription.
	/// </summary>
	/// <param name="indicator1">Indicator.</param>
	/// <param name="indicator2">Indicator.</param>
	/// <param name="indicator3">Indicator.</param>
	/// <param name="indicator4">Indicator.</param>
	/// <param name="indicator5">Indicator.</param>
	/// <param name="callback">Callback.</param>
	/// <param name="allowEmpty">If <see langword="true"/>, then the callback will be called even if one of the indicators returns empty value.</param>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	ISubscriptionHandler<T> BindEx(IIndicator indicator1, IIndicator indicator2, IIndicator indicator3, IIndicator indicator4, IIndicator indicator5, Action<T, IIndicatorValue, IIndicatorValue, IIndicatorValue, IIndicatorValue, IIndicatorValue> callback, bool allowEmpty = false);

	/// <summary>
	/// Bind indicators to the subscription with possible empty <see cref="IIndicatorValue.IsEmpty"/> values.
	/// </summary>
	/// <param name="indicator1">Indicator.</param>
	/// <param name="indicator2">Indicator.</param>
	/// <param name="indicator3">Indicator.</param>
	/// <param name="indicator4">Indicator.</param>
	/// <param name="indicator5">Indicator.</param>
	/// <param name="indicator6">Indicator.</param>
	/// <param name="callback">Callback.</param>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	ISubscriptionHandler<T> BindWithEmpty(IIndicator indicator1, IIndicator indicator2, IIndicator indicator3, IIndicator indicator4, IIndicator indicator5, IIndicator indicator6, Action<T, decimal?, decimal?, decimal?, decimal?, decimal?, decimal?> callback);

	/// <summary>
	/// Bind indicators to the subscription.
	/// </summary>
	/// <param name="indicator1">Indicator.</param>
	/// <param name="indicator2">Indicator.</param>
	/// <param name="indicator3">Indicator.</param>
	/// <param name="indicator4">Indicator.</param>
	/// <param name="indicator5">Indicator.</param>
	/// <param name="indicator6">Indicator.</param>
	/// <param name="callback">Callback.</param>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	ISubscriptionHandler<T> Bind(IIndicator indicator1, IIndicator indicator2, IIndicator indicator3, IIndicator indicator4, IIndicator indicator5, IIndicator indicator6, Action<T, decimal, decimal, decimal, decimal, decimal, decimal> callback);

	/// <summary>
	/// Bind indicators to the subscription.
	/// </summary>
	/// <param name="indicator1">Indicator.</param>
	/// <param name="indicator2">Indicator.</param>
	/// <param name="indicator3">Indicator.</param>
	/// <param name="indicator4">Indicator.</param>
	/// <param name="indicator5">Indicator.</param>
	/// <param name="indicator6">Indicator.</param>
	/// <param name="callback">Callback.</param>
	/// <param name="allowEmpty">If <see langword="true"/>, then the callback will be called even if one of the indicators returns empty value.</param>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	ISubscriptionHandler<T> BindEx(IIndicator indicator1, IIndicator indicator2, IIndicator indicator3, IIndicator indicator4, IIndicator indicator5, IIndicator indicator6, Action<T, IIndicatorValue, IIndicatorValue, IIndicatorValue, IIndicatorValue, IIndicatorValue, IIndicatorValue> callback, bool allowEmpty = false);

	/// <summary>
	/// Bind indicators to the subscription with possible empty <see cref="IIndicatorValue.IsEmpty"/> values.
	/// </summary>
	/// <param name="indicator1">Indicator.</param>
	/// <param name="indicator2">Indicator.</param>
	/// <param name="indicator3">Indicator.</param>
	/// <param name="indicator4">Indicator.</param>
	/// <param name="indicator5">Indicator.</param>
	/// <param name="indicator6">Indicator.</param>
	/// <param name="indicator7">Indicator.</param>
	/// <param name="callback">Callback.</param>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	ISubscriptionHandler<T> BindWithEmpty(IIndicator indicator1, IIndicator indicator2, IIndicator indicator3, IIndicator indicator4, IIndicator indicator5, IIndicator indicator6, IIndicator indicator7, Action<T, decimal?, decimal?, decimal?, decimal?, decimal?, decimal?, decimal?> callback);

	/// <summary>
	/// Bind indicators to the subscription.
	/// </summary>
	/// <param name="indicator1">Indicator.</param>
	/// <param name="indicator2">Indicator.</param>
	/// <param name="indicator3">Indicator.</param>
	/// <param name="indicator4">Indicator.</param>
	/// <param name="indicator5">Indicator.</param>
	/// <param name="indicator6">Indicator.</param>
	/// <param name="indicator7">Indicator.</param>
	/// <param name="callback">Callback.</param>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	ISubscriptionHandler<T> Bind(IIndicator indicator1, IIndicator indicator2, IIndicator indicator3, IIndicator indicator4, IIndicator indicator5, IIndicator indicator6, IIndicator indicator7, Action<T, decimal, decimal, decimal, decimal, decimal, decimal, decimal> callback);

	/// <summary>
	/// Bind indicators to the subscription.
	/// </summary>
	/// <param name="indicator1">Indicator.</param>
	/// <param name="indicator2">Indicator.</param>
	/// <param name="indicator3">Indicator.</param>
	/// <param name="indicator4">Indicator.</param>
	/// <param name="indicator5">Indicator.</param>
	/// <param name="indicator6">Indicator.</param>
	/// <param name="indicator7">Indicator.</param>
	/// <param name="callback">Callback.</param>
	/// <param name="allowEmpty">If <see langword="true"/>, then the callback will be called even if one of the indicators returns empty value.</param>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	ISubscriptionHandler<T> BindEx(IIndicator indicator1, IIndicator indicator2, IIndicator indicator3, IIndicator indicator4, IIndicator indicator5, IIndicator indicator6, IIndicator indicator7, Action<T, IIndicatorValue, IIndicatorValue, IIndicatorValue, IIndicatorValue, IIndicatorValue, IIndicatorValue, IIndicatorValue> callback, bool allowEmpty = false);

	/// <summary>
	/// Bind indicators to the subscription with possible empty <see cref="IIndicatorValue.IsEmpty"/> values.
	/// </summary>
	/// <param name="indicator1">Indicator.</param>
	/// <param name="indicator2">Indicator.</param>
	/// <param name="indicator3">Indicator.</param>
	/// <param name="indicator4">Indicator.</param>
	/// <param name="indicator5">Indicator.</param>
	/// <param name="indicator6">Indicator.</param>
	/// <param name="indicator7">Indicator.</param>
	/// <param name="indicator8">Indicator.</param>
	/// <param name="callback">Callback.</param>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	ISubscriptionHandler<T> BindWithEmpty(IIndicator indicator1, IIndicator indicator2, IIndicator indicator3, IIndicator indicator4, IIndicator indicator5, IIndicator indicator6, IIndicator indicator7, IIndicator indicator8, Action<T, decimal?, decimal?, decimal?, decimal?, decimal?, decimal?, decimal?, decimal?> callback);

	/// <summary>
	/// Bind indicators to the subscription.
	/// </summary>
	/// <param name="indicator1">Indicator.</param>
	/// <param name="indicator2">Indicator.</param>
	/// <param name="indicator3">Indicator.</param>
	/// <param name="indicator4">Indicator.</param>
	/// <param name="indicator5">Indicator.</param>
	/// <param name="indicator6">Indicator.</param>
	/// <param name="indicator7">Indicator.</param>
	/// <param name="indicator8">Indicator.</param>
	/// <param name="callback">Callback.</param>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	ISubscriptionHandler<T> Bind(IIndicator indicator1, IIndicator indicator2, IIndicator indicator3, IIndicator indicator4, IIndicator indicator5, IIndicator indicator6, IIndicator indicator7, IIndicator indicator8, Action<T, decimal, decimal, decimal, decimal, decimal, decimal, decimal, decimal> callback);

	/// <summary>
	/// Bind indicators to the subscription.
	/// </summary>
	/// <param name="indicator1">Indicator.</param>
	/// <param name="indicator2">Indicator.</param>
	/// <param name="indicator3">Indicator.</param>
	/// <param name="indicator4">Indicator.</param>
	/// <param name="indicator5">Indicator.</param>
	/// <param name="indicator6">Indicator.</param>
	/// <param name="indicator7">Indicator.</param>
	/// <param name="indicator8">Indicator.</param>
	/// <param name="callback">Callback.</param>
	/// <param name="allowEmpty">If <see langword="true"/>, then the callback will be called even if one of the indicators returns empty value.</param>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	ISubscriptionHandler<T> BindEx(IIndicator indicator1, IIndicator indicator2, IIndicator indicator3, IIndicator indicator4, IIndicator indicator5, IIndicator indicator6, IIndicator indicator7, IIndicator indicator8, Action<T, IIndicatorValue, IIndicatorValue, IIndicatorValue, IIndicatorValue, IIndicatorValue, IIndicatorValue, IIndicatorValue, IIndicatorValue> callback, bool allowEmpty = false);

	/// <summary>
	/// Bind indicator to the subscription.
	/// </summary>
	/// <param name="indicators">Indicators.</param>
	/// <param name="callback">Callback.</param>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	ISubscriptionHandler<T> Bind(IIndicator[] indicators, Action<T, decimal[]> callback);

	/// <summary>
	/// Bind indicators to the subscription with possible empty <see cref="IIndicatorValue.IsEmpty"/> values.
	/// </summary>
	/// <param name="indicators">Indicators.</param>
	/// <param name="callback">Callback.</param>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	ISubscriptionHandler<T> BindWithEmpty(IIndicator[] indicators, Action<T, decimal?[]> callback);

	/// <summary>
	/// Bind indicators to the subscription.
	/// </summary>
	/// <param name="indicators">Indicators.</param>
	/// <param name="callback">Callback.</param>
	/// <param name="allowEmpty">If <see langword="true"/>, then the callback will be called even if one of the indicators returns empty value.</param>
	/// <returns><see cref="ISubscriptionHandler{T}"/></returns>
	ISubscriptionHandler<T> BindEx(IIndicator[] indicators, Action<T, IIndicatorValue[]> callback, bool allowEmpty = false);
}
