namespace StockSharp.Tests;

using StockSharp.Algo.Slippage;

[TestClass]
public class SlippageTests
{
	private static readonly SecurityId _secId = Helper.CreateSecurityId();

	[TestMethod]
	public void Level1AndOrderRegisterBuy()
	{
		var mgr = new SlippageManager();

		mgr.ProcessMessage(new Level1ChangeMessage
		{
			SecurityId = _secId
		}
		.Add(Level1Fields.BestBidPrice, 100m)
		.Add(Level1Fields.BestAskPrice, 102m));

		// имитируем регистрацию Buy-заявки
		mgr.ProcessMessage(new OrderRegisterMessage
		{
			SecurityId = _secId,
			Side = Sides.Buy,
			TransactionId = 1
		});

		// исполнили заявку с ценой хуже (больше) на 2 пункта
		var slip = mgr.ProcessMessage(new ExecutionMessage
		{
			SecurityId = _secId,
			OriginalTransactionId = 1,
			TradePrice = 104m,
			Side = Sides.Buy
		});

		slip.AssertNotNull();
		slip.Value.AssertEqual(2m); // 104 - 102 (ask при регистрации)
	}

	[TestMethod]
	public void Level1AndOrderRegisterSell()
	{
		var mgr = new SlippageManager();

		mgr.ProcessMessage(new Level1ChangeMessage
		{
			SecurityId = _secId
		}
		.Add(Level1Fields.BestBidPrice, 101m)
		.Add(Level1Fields.BestAskPrice, 103m));

		mgr.ProcessMessage(new OrderRegisterMessage
		{
			SecurityId = _secId,
			Side = Sides.Sell,
			TransactionId = 1
		});

		var slip = mgr.ProcessMessage(new ExecutionMessage
		{
			SecurityId = _secId,
			OriginalTransactionId = 1,
			TradePrice = 100m,
			Side = Sides.Sell
		});

		slip.AssertNotNull();
		slip.Value.AssertEqual(1m); // 101 - 100 (bid при регистрации)
	}

	[TestMethod]
	public void QuoteChangeRegisterAndExecution()
	{
		var mgr = new SlippageManager();

		mgr.ProcessMessage(new QuoteChangeMessage
		{
			SecurityId = _secId,
			Bids = [new(50m, 1)],
			Asks = [new(55m, 1)]
		});

		mgr.ProcessMessage(new OrderRegisterMessage
		{
			SecurityId = _secId,
			Side = Sides.Buy,
			TransactionId = 77
		});

		var slip = mgr.ProcessMessage(new ExecutionMessage
		{
			SecurityId = _secId,
			OriginalTransactionId = 77,
			TradePrice = 60m,
			Side = Sides.Buy
		});

		slip.AssertNotNull();
		slip.Value.AssertEqual(5m); // 60 - 55
	}

	[TestMethod]
	public void SlippageNegativeAndOption()
	{
		var mgr = new SlippageManager();

		mgr.ProcessMessage(new Level1ChangeMessage
		{
			SecurityId = _secId
		}
		.Add(Level1Fields.BestBidPrice, 100m)
		.Add(Level1Fields.BestAskPrice, 101m));

		mgr.ProcessMessage(new OrderRegisterMessage
		{
			SecurityId = _secId,
			Side = Sides.Buy,
			TransactionId = 123
		});

		// исполнение ЛУЧШЕ ask — отрицательное проскальзывание
		var slip = mgr.ProcessMessage(new ExecutionMessage
		{
			SecurityId = _secId,
			OriginalTransactionId = 123,
			TradePrice = 100m,
			Side = Sides.Buy
		});

		slip.AssertNotNull();
		slip.Value.AssertEqual(-1m); // 100 - 101

		// Отключить отрицательное проскальзывание
		mgr.CalculateNegative = false;

		var slip2 = mgr.ProcessMessage(new ExecutionMessage
		{
			SecurityId = _secId,
			OriginalTransactionId = 123,
			TradePrice = 100m,
			Side = Sides.Buy
		});

		slip2.AssertNotNull();
		slip2.Value.AssertEqual(0m); // не может быть <0
	}

	[TestMethod]
	public void NoBestPrices()
	{
		var mgr = new SlippageManager();

		// Без Level1/QuoteChange: OrderRegister не добавляет plannedPrice
		mgr.ProcessMessage(new OrderRegisterMessage
		{
			SecurityId = _secId,
			Side = Sides.Buy,
			TransactionId = 404
		});

		// Исполнение должно вернуть null (нет plannedPrice)
		var slip = mgr.ProcessMessage(new ExecutionMessage
		{
			SecurityId = _secId,
			OriginalTransactionId = 404,
			TradePrice = 88,
			Side = Sides.Buy
		});

		slip.AssertNull();
	}

	[TestMethod]
	public void NoPlannedPrice()
	{
		var mgr = new SlippageManager();

		mgr.ProcessMessage(new Level1ChangeMessage
		{
			SecurityId = _secId
		}
		.Add(Level1Fields.BestBidPrice, 90m)
		.Add(Level1Fields.BestAskPrice, 92m));

		// Без регистрации заявки, сразу Execution — plannedPrice не будет
		var slip = mgr.ProcessMessage(new ExecutionMessage
		{
			SecurityId = _secId,
			OriginalTransactionId = 999,
			TradePrice = 91m,
			Side = Sides.Buy
		});

		slip.AssertNull();
	}

	[TestMethod]
	public void ResetWorks()
	{
		var mgr = new SlippageManager();

		mgr.ProcessMessage(new Level1ChangeMessage
		{
			SecurityId = _secId
		}
		.Add(Level1Fields.BestBidPrice, 1m)
		.Add(Level1Fields.BestAskPrice, 2m));

		mgr.ProcessMessage(new OrderRegisterMessage
		{
			SecurityId = _secId,
			Side = Sides.Buy,
			TransactionId = 12
		});

		var slip = mgr.ProcessMessage(new ExecutionMessage
		{
			SecurityId = _secId,
			OriginalTransactionId = 12,
			TradePrice = 3m,
			Side = Sides.Buy
		});
		slip.AssertNotNull();

		// сброс
		mgr.ProcessMessage(new ResetMessage());

		// Данные должны быть очищены, следующий Execution уже не найдет plannedPrice
		var slip2 = mgr.ProcessMessage(new ExecutionMessage
		{
			SecurityId = _secId,
			OriginalTransactionId = 12,
			TradePrice = 3m,
			Side = Sides.Buy
		});

		slip2.AssertNull();
	}

	[TestMethod]
	public void SaveLoadSettings()
	{
		var mgr = new SlippageManager
		{
			CalculateNegative = false
		};

		var storage = new SettingsStorage();
		mgr.Save(storage);

		var mgr2 = new SlippageManager
		{
			CalculateNegative = true
		};

		mgr2.Load(storage);

		mgr2.CalculateNegative.AssertFalse();
	}
}
