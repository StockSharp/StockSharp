namespace StockSharp.Tests;

using StockSharp.Algo.Strategies;

[TestClass]
public class StrategyNameGeneratorTests : BaseTestClass
{
	private class MyTestStrategy : Strategy { }

	private class SmaStrategy : Strategy { }

	private class BollingerBandsMeanReversionStrategy : Strategy { }

	[TestMethod]
	public void DefaultPattern_ShortName()
	{
		var s = new MyTestStrategy();
		// ShortName = upper-case letters of "MyTestStrategy" => "MTS"
		s.NameGenerator.ShortName.AssertEqual("MTS");
		s.Name.AssertEqual("MTS");
	}

	[TestMethod]
	public void DefaultPattern_SmaStrategy()
	{
		var s = new SmaStrategy();
		s.NameGenerator.ShortName.AssertEqual("SS");
		s.Name.AssertEqual("SS");
	}

	[TestMethod]
	public void DefaultPattern_LongName()
	{
		var s = new BollingerBandsMeanReversionStrategy();
		s.NameGenerator.ShortName.AssertEqual("BBMRS");
		s.Name.AssertEqual("BBMRS");
	}

	[TestMethod]
	public void FullNamePattern()
	{
		var s = new MyTestStrategy();
		s.NameGenerator.Pattern = "{FullName}";
		s.Name.AssertEqual("MyTestStrategy");
	}

	[TestMethod]
	public void ShortNamePattern()
	{
		var s = new MyTestStrategy();
		s.NameGenerator.Pattern = "{ShortName}";
		s.Name.AssertEqual("MTS");
	}

	[TestMethod]
	public void CustomPattern_WithSecurity()
	{
		var s = new MyTestStrategy();

		var security = Helper.CreateStorageSecurity();
		s.Security = security;

		s.NameGenerator.Pattern = "{ShortName}{Security:_{0.Security}|}";

		s.Name.Contains("MTS").AssertTrue();
		s.Name.Contains(security.Id).AssertTrue();
	}

	[TestMethod]
	public void CustomPattern_WithPortfolio()
	{
		var s = new MyTestStrategy();

		s.Portfolio = new Portfolio { Name = "TestPortfolio" };

		s.NameGenerator.Pattern = "{ShortName}{Portfolio:_{0.Portfolio}|}";

		s.Name.AssertEqual("MTS_TestPortfolio");
	}

	[TestMethod]
	public void CustomPattern_WithSecurityAndPortfolio()
	{
		var s = new MyTestStrategy();

		var security = Helper.CreateStorageSecurity();
		s.Security = security;
		s.Portfolio = new Portfolio { Name = "MyAccount" };

		// default pattern
		s.NameGenerator.Pattern = "{ShortName}{Security:_{0.Security}|}{Portfolio:_{0.Portfolio}|}";

		s.Name.Contains("MTS").AssertTrue();
		s.Name.Contains(security.Id).AssertTrue();
		s.Name.Contains("MyAccount").AssertTrue();
	}

	[TestMethod]
	public void ManualName_DisablesAutoGeneration()
	{
		var s = new MyTestStrategy();
		s.Name.AssertEqual("MTS");

		s.Name = "CustomName";
		s.NameGenerator.AutoGenerateStrategyName.AssertFalse();
		s.Name.AssertEqual("CustomName");
	}

	[TestMethod]
	public void ManualName_IgnoresPatternChange()
	{
		var s = new MyTestStrategy();
		s.Name = "CustomName";

		// changing pattern should NOT regenerate since auto is off
		s.NameGenerator.Pattern = "{FullName}";
		s.Name.AssertEqual("CustomName");
	}

	[TestMethod]
	public void AutoGeneration_ReenableAfterManual()
	{
		var s = new MyTestStrategy();
		s.Name = "CustomName";

		s.NameGenerator.AutoGenerateStrategyName = true;
		s.NameGenerator.Pattern = "{FullName}";

		s.Name.AssertEqual("MyTestStrategy");
	}

	[TestMethod]
	public void PatternChange_RegeneratesName()
	{
		var s = new MyTestStrategy();
		s.Name.AssertEqual("MTS");

		s.NameGenerator.Pattern = "{FullName}";
		s.Name.AssertEqual("MyTestStrategy");

		s.NameGenerator.Pattern = "{ShortName}";
		s.Name.AssertEqual("MTS");
	}

	[TestMethod]
	public void ChangedEvent_Fires()
	{
		var s = new MyTestStrategy();
		var names = new List<string>();
		s.NameGenerator.Changed += n => names.Add(n);

		s.NameGenerator.Pattern = "{FullName}";

		names.Count.AssertEqual(1);
		names[0].AssertEqual("MyTestStrategy");
	}

	[TestMethod]
	public void Dispose_UnsubscribesFromPropertyChanged()
	{
		var s = new MyTestStrategy();
		var names = new List<string>();
		s.NameGenerator.Changed += n => names.Add(n);

		s.NameGenerator.Dispose();

		// after dispose, changing security should not trigger name change
		var countBefore = names.Count;
		s.Security = Helper.CreateStorageSecurity();

		names.Count.AssertEqual(countBefore);
	}

	[TestMethod]
	public void UnderscoreInPattern()
	{
		var s = new MyTestStrategy();
		s.NameGenerator.Pattern = "{ShortName}_v2";
		s.Name.AssertEqual("MTS_v2");
	}

	[TestMethod]
	public void StaticTextPattern()
	{
		var s = new MyTestStrategy();
		s.NameGenerator.Pattern = "Hello World";
		s.Name.AssertEqual("Hello World");
	}

	[TestMethod]
	public void EmptySecurity_NoSuffix()
	{
		var s = new MyTestStrategy();
		// default pattern includes conditional Security part
		s.NameGenerator.Pattern = "{ShortName}{Security:_{0.Security}|}";

		// no security set — conditional block should produce empty
		s.Name.AssertEqual("MTS");
	}

	[TestMethod]
	public void SecurityChange_UpdatesName()
	{
		var s = new MyTestStrategy();
		s.NameGenerator.Pattern = "{ShortName}{Security:_{0.Security}|}";

		s.Name.AssertEqual("MTS");

		var security = Helper.CreateStorageSecurity();
		s.Security = security;

		s.Name.Contains(security.Id).AssertTrue();
	}
}
