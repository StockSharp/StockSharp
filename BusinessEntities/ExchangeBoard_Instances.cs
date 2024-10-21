namespace StockSharp.BusinessEntities;

partial class ExchangeBoard
{
	private static readonly DateTime[] _russianSpecialWorkingDays =
	[
		// http://www.rts.ru/a742
		new(2001, 3, 11),
		new(2001, 4, 28),
		new(2001, 6, 9),
		new(2001, 12, 29),

		// http://www.rts.ru/a3414
		new(2002, 4, 27),
		new(2002, 5, 18),
		new(2002, 11, 10),

		// http://www.rts.ru/a5194
		new(2003, 1, 4),
		new(2003, 1, 5),
		new(2003, 6, 21),

		// http://www.rts.ru/a6598
		// дат нет

		// http://www.rts.ru/a7751
		new(2005, 3, 5),
		new(2005, 5, 14),

		// http://www.rts.ru/a743
		new(2006, 2, 26),
		new(2006, 5, 6),

		// http://www.rts.ru/a13059
		new(2007, 4, 28),
		new(2007, 6, 9),

		// http://www.rts.ru/a15065
		new(2008, 5, 4),
		new(2008, 6, 7),
		new(2008, 11, 1),

		// http://www.rts.ru/a17902
		new(2009, 1, 11),

		// http://www.rts.ru/a19524
		new(2010, 2, 27),
		new(2010, 11, 13),

		// http://www.rts.ru/s355
		new(2011, 3, 5),

		// http://moex.com/a254
		new(2012, 3, 11),
		new(2012, 4, 28),
		new(2012, 5, 5),
		new(2012, 5, 12),
		new(2012, 6, 9),
		new(2012, 12, 29),

		// http://moex.com/a3367
		new(2016, 02, 20)
	];
	private static readonly DateTime[] _russianSpecialHolidays =
	[
		// http://www.rts.ru/a742
		new(2001, 1, 1),
		new(2001, 1, 2),
		new(2001, 1, 8),
		new(2001, 3, 8),
		new(2001, 3, 9),
		new(2001, 4, 30),
		new(2001, 5, 1),
		new(2001, 5, 2),
		new(2001, 5, 9),
		new(2001, 6, 11),
		new(2001, 6, 12),
		new(2001, 11, 7),
		new(2001, 12, 12),
		new(2001, 12, 31),

		// http://www.rts.ru/a3414
		new(2002, 1, 1),
		new(2002, 1, 2),
		new(2002, 1, 7),
		new(2002, 2, 25),
		new(2002, 3, 8),
		new(2002, 3, 9),
		new(2002, 5, 1),
		new(2002, 5, 2),
		new(2002, 5, 3),
		new(2002, 5, 9),
		new(2002, 5, 10),
		new(2002, 6, 12),
		new(2002, 11, 7),
		new(2002, 11, 8),
		new(2002, 12, 12),
		new(2002, 12, 13),

		// http://www.rts.ru/a5194
		new(2003, 1, 1),
		new(2003, 1, 2),
		new(2003, 1, 3),
		new(2003, 1, 6),
		new(2003, 1, 7),
		new(2003, 2, 24),
		new(2003, 3, 10),
		new(2003, 5, 1),
		new(2003, 5, 2),
		new(2003, 5, 9),
		new(2003, 6, 12),
		new(2003, 6, 13),
		new(2003, 11, 7),
		new(2003, 12, 12),

		// http://www.rts.ru/a6598
		new(2004, 1, 1),
		new(2004, 1, 2),
		new(2004, 1, 7),
		new(2004, 2, 23),
		new(2004, 3, 8),
		new(2004, 5, 3),
		new(2004, 5, 4),
		new(2004, 5, 10),
		new(2004, 6, 14),
		new(2004, 11, 8),
		new(2004, 12, 13),

		// http://www.rts.ru/a7751
		new(2005, 1, 3),
		new(2005, 1, 4),
		new(2005, 1, 5),
		new(2005, 1, 6),
		new(2005, 1, 7),
		new(2005, 1, 10),
		new(2005, 2, 23),
		new(2005, 3, 7),
		new(2005, 3, 8),
		new(2005, 5, 2),
		new(2005, 5, 9),
		new(2005, 5, 10),
		new(2005, 6, 13),
		new(2005, 11, 4),

		// http://www.rts.ru/a743
		new(2006, 1, 2),
		new(2006, 1, 3),
		new(2006, 1, 4),
		new(2006, 1, 5),
		new(2006, 1, 6),
		new(2006, 1, 9),
		new(2006, 2, 23),
		new(2006, 2, 24),
		new(2006, 3, 8),
		new(2006, 5, 1),
		new(2006, 5, 8),
		new(2006, 5, 9),
		new(2006, 6, 12),
		new(2006, 11, 6),

		// http://www.rts.ru/a13059
		new(2007, 1, 1),
		new(2007, 1, 2),
		new(2007, 1, 3),
		new(2007, 1, 4),
		new(2007, 1, 5),
		new(2007, 1, 8),
		new(2007, 2, 23),
		new(2007, 3, 8),
		new(2007, 4, 30),
		new(2007, 5, 1),
		new(2007, 5, 9),
		new(2007, 6, 11),
		new(2007, 6, 12),
		new(2007, 11, 5),
		new(2007, 12, 31),

		// http://www.rts.ru/a15065
		new(2008, 1, 1),
		new(2008, 1, 2),
		new(2008, 1, 3),
		new(2008, 1, 4),
		new(2008, 1, 7),
		new(2008, 1, 8),
		new(2008, 2, 25),
		new(2008, 3, 10),
		new(2008, 5, 1),
		new(2008, 5, 2),
		new(2008, 6, 12),
		new(2008, 6, 13),
		new(2008, 11, 3),
		new(2008, 11, 4),

		// http://www.rts.ru/a17902
		new(2009, 1, 1),
		new(2009, 1, 2),
		new(2009, 1, 5),
		new(2009, 1, 6),
		new(2009, 1, 7),
		new(2009, 1, 8),
		new(2009, 1, 9),
		new(2009, 2, 23),
		new(2009, 3, 9),
		new(2009, 5, 1),
		new(2009, 5, 11),
		new(2009, 6, 12),
		new(2009, 11, 4),

		// http://www.rts.ru/a19524
		new(2010, 1, 1),
		new(2010, 1, 4),
		new(2010, 1, 5),
		new(2010, 1, 6),
		new(2010, 1, 7),
		new(2010, 1, 8),
		new(2010, 2, 22),
		new(2010, 2, 23),
		new(2010, 3, 8),
		new(2010, 5, 3),
		new(2010, 5, 10),
		new(2010, 6, 14),
		new(2010, 11, 4),
		new(2010, 11, 5),

		// http://www.rts.ru/s355
		new(2011, 1, 3),
		new(2011, 1, 4),
		new(2011, 1, 5),
		new(2011, 1, 6),
		new(2011, 1, 7),
		new(2011, 1, 10),
		new(2011, 2, 23),
		new(2011, 3, 7),
		new(2011, 3, 8),
		new(2011, 5, 2),
		new(2011, 5, 9),
		new(2011, 6, 13),
		new(2011, 11, 4),

		// http://moex.com/a254
		new(2012, 1, 2),
		new(2012, 2, 23),
		new(2012, 3, 8),
		new(2012, 3, 9),
		new(2012, 4, 30),
		new(2012, 5, 1),
		new(2012, 5, 9),
		new(2012, 6, 11),
		new(2012, 6, 12),
		new(2012, 11, 5),
		new(2012, 12, 31),

		// http://moex.com/a1343
		new(2013, 1, 1),
		new(2013, 1, 2),
		new(2013, 1, 3),
		new(2013, 1, 4),
		new(2013, 1, 7),
		new(2013, 3, 8),
		new(2013, 5, 1),
		new(2013, 5, 9),
		new(2013, 6, 12),
		new(2013, 11, 4),
		new(2013, 12, 31),

		// http://moex.com/a2973
		new(2014, 1, 1),
		new(2014, 1, 2),
		new(2014, 1, 3),
		new(2014, 1, 7),
		new(2014, 3, 10),
		new(2014, 5, 1),
		new(2014, 5, 9),
		new(2014, 6, 12),
		new(2014, 11, 4),
		new(2014, 12, 31),

		// http://moex.com/a2793
		new(2015, 1, 1),
		new(2015, 1, 2),
		new(2015, 1, 7),
		new(2015, 2, 23),
		new(2015, 3, 9),
		new(2015, 5, 1),
		new(2015, 5, 4),
		new(2015, 5, 11),
		new(2015, 6, 12),
		new(2015, 11, 4),
		new(2015, 12, 31),

		// http://moex.com/a3367
		new(2016, 1, 1),
		new(2016, 1, 7),
		new(2016, 1, 8),
		new(2016, 2, 23),
		new(2016, 3, 8),
		new(2016, 5, 2),
		new(2016, 5, 3),
		new(2016, 5, 9),
		new(2016, 6, 13),
		new(2016, 11, 4),
	];

	private static readonly WorkingTime _micexWorkingTime = new()
	{
		IsEnabled = true,
		Periods =
		[
			new()
			{
				Till = DateTime.MaxValue,
				Times =
				[
					new("10:00:00".To<TimeSpan>(), "18:45:00".To<TimeSpan>())
				],
			}
		],
		SpecialWorkingDays = _russianSpecialWorkingDays,
		SpecialHolidays = _russianSpecialHolidays,
	};

	private static ExchangeBoard CreateMoex(string code) => new()
	{
		Code = code,
		WorkingTime = _micexWorkingTime.Clone(),
		Exchange = Exchange.Moex,
		TimeZone = TimeHelper.Moscow,
	};

	private static readonly TimeZoneInfo _singaporeTime = "Singapore Standard Time".To<TimeZoneInfo>();
	private static readonly TimeZoneInfo _bangkokTime = "SE Asia Standard Time".To<TimeZoneInfo>();
	private static readonly TimeZoneInfo _indiaTime = "India Standard Time".To<TimeZoneInfo>();

	private static TimeZoneInfo GetTimeZone(string id, TimeSpan offset)
	{
		try
		{
			return id.To<TimeZoneInfo>();
		}
		catch (TimeZoneNotFoundException)
		{
			return TimeZoneInfo.GetSystemTimeZones().First(z => z.BaseUtcOffset == offset);
		}
	}

	/// <summary>
	/// Information about board <see cref="Associated"/>.
	/// </summary>
	public static ExchangeBoard Associated { get; } = new()
	{
		Code = SecurityId.AssociatedBoardCode,
		Exchange = Exchange.Test,
	};

	/// <summary>
	/// Information about board <see cref="Test"/>.
	/// </summary>
	public static ExchangeBoard Test { get; } = new()
	{
		Code = BoardCodes.Test,
		Exchange = Exchange.Test,
	};

	/// <summary>
	/// Information about board <see cref="Forts"/>.
	/// </summary>
	public static ExchangeBoard Forts { get; } = new()
	{
		Code = BoardCodes.Forts,
		WorkingTime = new()
		{
			IsEnabled = true,
			Periods =
			[
				new()
				{
					Till = DateTime.MaxValue,
					Times =
					[
						new("10:00:00".To<TimeSpan>(), "14:00:00".To<TimeSpan>()),
						new("14:03:00".To<TimeSpan>(), "18:45:00".To<TimeSpan>()),
						new("19:00:00".To<TimeSpan>(), "23:50:00".To<TimeSpan>())
					],
				}
			],
			SpecialWorkingDays = _russianSpecialWorkingDays,
			SpecialHolidays = _russianSpecialHolidays,
		},
		ExpiryTime = new TimeSpan(18, 45, 00),
		//IsSupportAtomicReRegister = true,
		Exchange = Exchange.Moex,
		TimeZone = TimeHelper.Moscow,
	};

	/// <summary>
	/// Information about board <see cref="Micex"/>.
	/// </summary>
	public static ExchangeBoard Micex { get; } = CreateMoex(BoardCodes.Micex);

	/// <summary>
	/// Information about board <see cref="MicexAuct"/>.
	/// </summary>
	public static ExchangeBoard MicexAuct { get; } = CreateMoex(BoardCodes.MicexAuct);

	/// <summary>
	/// Information about board <see cref="MicexAubb"/>.
	/// </summary>
	public static ExchangeBoard MicexAubb { get; } = CreateMoex(BoardCodes.MicexAubb);

	/// <summary>
	/// Information about board <see cref="MicexCasf"/>.
	/// </summary>
	public static ExchangeBoard MicexCasf { get; } = CreateMoex(BoardCodes.MicexCasf);

	/// <summary>
	/// Information about board <see cref="MicexEqbr"/>.
	/// </summary>
	public static ExchangeBoard MicexEqbr { get; } = CreateMoex(BoardCodes.MicexEqbr);

	/// <summary>
	/// Information about board <see cref="MicexEqbs"/>.
	/// </summary>
	public static ExchangeBoard MicexEqbs { get; } = CreateMoex(BoardCodes.MicexEqbs);

	/// <summary>
	/// Information about board <see cref="MicexEqdp"/>.
	/// </summary>
	public static ExchangeBoard MicexEqdp { get; } = CreateMoex(BoardCodes.MicexEqdp);

	/// <summary>
	/// Information about board <see cref="MicexEqeu"/>.
	/// </summary>
	public static ExchangeBoard MicexEqeu { get; } = CreateMoex(BoardCodes.MicexEqeu);

	/// <summary>
	/// Information about board <see cref="MicexEqus"/>.
	/// </summary>
	public static ExchangeBoard MicexEqus { get; } = CreateMoex(BoardCodes.MicexEqus);

	/// <summary>
	/// Information about board <see cref="MicexEqnb"/>.
	/// </summary>
	public static ExchangeBoard MicexEqnb { get; } = CreateMoex(BoardCodes.MicexEqnb);

	/// <summary>
	/// Information about board <see cref="MicexEqne"/>.
	/// </summary>
	public static ExchangeBoard MicexEqne { get; } = CreateMoex(BoardCodes.MicexEqne);

	/// <summary>
	/// Information about board <see cref="MicexEqnl"/>.
	/// </summary>
	public static ExchangeBoard MicexEqnl { get; } = CreateMoex(BoardCodes.MicexEqnl);

	/// <summary>
	/// Information about board <see cref="MicexEqno"/>.
	/// </summary>
	public static ExchangeBoard MicexEqno { get; } = CreateMoex(BoardCodes.MicexEqno);

	/// <summary>
	/// Information about board <see cref="MicexEqob"/>.
	/// </summary>
	public static ExchangeBoard MicexEqob { get; } = CreateMoex(BoardCodes.MicexEqob);

	/// <summary>
	/// Information about board <see cref="MicexEqos"/>.
	/// </summary>
	public static ExchangeBoard MicexEqos { get; } = CreateMoex(BoardCodes.MicexEqos);

	/// <summary>
	/// Information about board <see cref="MicexEqov"/>.
	/// </summary>
	public static ExchangeBoard MicexEqov { get; } = CreateMoex(BoardCodes.MicexEqov);

	/// <summary>
	/// Information about board <see cref="MicexEqlv"/>.
	/// </summary>
	public static ExchangeBoard MicexEqlv { get; } = CreateMoex(BoardCodes.MicexEqlv);

	/// <summary>
	/// Information about board <see cref="MicexEqdb"/>.
	/// </summary>
	public static ExchangeBoard MicexEqdb { get; } = CreateMoex(BoardCodes.MicexEqdb);

	/// <summary>
	/// Information about board <see cref="MicexEqde"/>.
	/// </summary>
	public static ExchangeBoard MicexEqde { get; } = CreateMoex(BoardCodes.MicexEqde);

	/// <summary>
	/// Information about board <see cref="MicexEqli"/>.
	/// </summary>
	public static ExchangeBoard MicexEqli { get; } = CreateMoex(BoardCodes.MicexEqli);

	/// <summary>
	/// Information about board <see cref="MicexEqqi"/>.
	/// </summary>
	public static ExchangeBoard MicexEqqi { get; } = CreateMoex(BoardCodes.MicexEqqi);

	/// <summary>
	/// Information about board <see cref="MicexSmal"/>.
	/// </summary>
	public static ExchangeBoard MicexSmal { get; } = CreateMoex(BoardCodes.MicexSmal);

	/// <summary>
	/// Information about board <see cref="MicexSpob"/>.
	/// </summary>
	public static ExchangeBoard MicexSpob { get; } = CreateMoex(BoardCodes.MicexSpob);

	/// <summary>
	/// Information about board <see cref="MicexTqbr"/>.
	/// </summary>
	public static ExchangeBoard MicexTqbr { get; } = CreateMoex(BoardCodes.MicexTqbr);

	/// <summary>
	/// Information about board <see cref="MicexTqde"/>.
	/// </summary>
	public static ExchangeBoard MicexTqde { get; } = CreateMoex(BoardCodes.MicexTqde);

	/// <summary>
	/// Information about board <see cref="MicexTqbs"/>.
	/// </summary>
	public static ExchangeBoard MicexTqbs { get; } = CreateMoex(BoardCodes.MicexTqbs);

	/// <summary>
	/// Information about board <see cref="MicexTqeu"/>.
	/// </summary>
	public static ExchangeBoard MicexTqeu { get; } = CreateMoex(BoardCodes.MicexTqeu);

	/// <summary>
	/// Information about board <see cref="MicexTqus"/>.
	/// </summary>
	public static ExchangeBoard MicexTqus { get; } = CreateMoex(BoardCodes.MicexTqus);

	/// <summary>
	/// Information about board <see cref="MicexTqnb"/>.
	/// </summary>
	public static ExchangeBoard MicexTqnb { get; } = CreateMoex(BoardCodes.MicexTqnb);

	/// <summary>
	/// Information about board <see cref="MicexTqne"/>.
	/// </summary>
	public static ExchangeBoard MicexTqne { get; } = CreateMoex(BoardCodes.MicexTqne);

	/// <summary>
	/// Information about board <see cref="MicexTqnl"/>.
	/// </summary>
	public static ExchangeBoard MicexTqnl { get; } = CreateMoex(BoardCodes.MicexTqnl);

	/// <summary>
	/// Information about board <see cref="MicexTqno"/>.
	/// </summary>
	public static ExchangeBoard MicexTqno { get; } = CreateMoex(BoardCodes.MicexTqno);

	/// <summary>
	/// Information about board <see cref="MicexTqob"/>.
	/// </summary>
	public static ExchangeBoard MicexTqob { get; } = CreateMoex(BoardCodes.MicexTqob);

	/// <summary>
	/// Information about board <see cref="MicexTqos"/>.
	/// </summary>
	public static ExchangeBoard MicexTqos { get; } = CreateMoex(BoardCodes.MicexTqos);

	/// <summary>
	/// Information about board <see cref="MicexTqov"/>.
	/// </summary>
	public static ExchangeBoard MicexTqov { get; } = CreateMoex(BoardCodes.MicexTqov);

	/// <summary>
	/// Information about board <see cref="MicexTqlv"/>.
	/// </summary>
	public static ExchangeBoard MicexTqlv { get; } = CreateMoex(BoardCodes.MicexTqlv);

	/// <summary>
	/// Information about board <see cref="MicexTqli"/>.
	/// </summary>
	public static ExchangeBoard MicexTqli { get; } = CreateMoex(BoardCodes.MicexTqli);

	/// <summary>
	/// Information about board <see cref="MicexTqqi"/>.
	/// </summary>
	public static ExchangeBoard MicexTqqi { get; } = CreateMoex(BoardCodes.MicexTqqi);

	/// <summary>
	/// Information about board <see cref="MicexEqrp"/>.
	/// </summary>
	public static ExchangeBoard MicexEqrp { get; } = CreateMoex(BoardCodes.MicexEqrp);

	/// <summary>
	/// Information about board <see cref="MicexPsrp"/>.
	/// </summary>
	public static ExchangeBoard MicexPsrp { get; } = CreateMoex(BoardCodes.MicexPsrp);

	/// <summary>
	/// Information about board <see cref="MicexRfnd"/>.
	/// </summary>
	public static ExchangeBoard MicexRfnd { get; } = CreateMoex(BoardCodes.MicexRfnd);

	/// <summary>
	/// Information about board <see cref="MicexTadm"/>.
	/// </summary>
	public static ExchangeBoard MicexTadm { get; } = CreateMoex(BoardCodes.MicexTadm);

	/// <summary>
	/// Information about board <see cref="MicexNadm"/>.
	/// </summary>
	public static ExchangeBoard MicexNadm { get; } = CreateMoex(BoardCodes.MicexNadm);

	/// <summary>
	/// Information about board <see cref="MicexPsau"/>.
	/// </summary>
	public static ExchangeBoard MicexPsau { get; } = CreateMoex(BoardCodes.MicexPsau);

	/// <summary>
	/// Information about board <see cref="MicexPaus"/>.
	/// </summary>
	public static ExchangeBoard MicexPaus { get; } = CreateMoex(BoardCodes.MicexPaus);

	/// <summary>
	/// Information about board <see cref="MicexPsbb"/>.
	/// </summary>
	public static ExchangeBoard MicexPsbb { get; } = CreateMoex(BoardCodes.MicexPsbb);

	/// <summary>
	/// Information about board <see cref="MicexPseq"/>.
	/// </summary>
	public static ExchangeBoard MicexPseq { get; } = CreateMoex(BoardCodes.MicexPseq);

	/// <summary>
	/// Information about board <see cref="MicexPses"/>.
	/// </summary>
	public static ExchangeBoard MicexPses { get; } = CreateMoex(BoardCodes.MicexPses);

	/// <summary>
	/// Information about board <see cref="MicexPseu"/>.
	/// </summary>
	public static ExchangeBoard MicexPseu { get; } = CreateMoex(BoardCodes.MicexPseu);

	/// <summary>
	/// Information about board <see cref="MicexPsdb"/>.
	/// </summary>
	public static ExchangeBoard MicexPsdb { get; } = CreateMoex(BoardCodes.MicexPsdb);

	/// <summary>
	/// Information about board <see cref="MicexPsde"/>.
	/// </summary>
	public static ExchangeBoard MicexPsde { get; } = CreateMoex(BoardCodes.MicexPsde);

	/// <summary>
	/// Information about board <see cref="MicexPsus"/>.
	/// </summary>
	public static ExchangeBoard MicexPsus { get; } = CreateMoex(BoardCodes.MicexPsus);

	/// <summary>
	/// Information about board <see cref="MicexPsnb"/>.
	/// </summary>
	public static ExchangeBoard MicexPsnb { get; } = CreateMoex(BoardCodes.MicexPsnb);

	/// <summary>
	/// Information about board <see cref="MicexPsne"/>.
	/// </summary>
	public static ExchangeBoard MicexPsne { get; } = CreateMoex(BoardCodes.MicexPsne);

	/// <summary>
	/// Information about board <see cref="MicexPsnl"/>.
	/// </summary>
	public static ExchangeBoard MicexPsnl { get; } = CreateMoex(BoardCodes.MicexPsnl);

	/// <summary>
	/// Information about board <see cref="MicexPsno"/>.
	/// </summary>
	public static ExchangeBoard MicexPsno { get; } = CreateMoex(BoardCodes.MicexPsno);

	/// <summary>
	/// Information about board <see cref="MicexPsob"/>.
	/// </summary>
	public static ExchangeBoard MicexPsob { get; } = CreateMoex(BoardCodes.MicexPsob);

	/// <summary>
	/// Information about board <see cref="MicexPsos"/>.
	/// </summary>
	public static ExchangeBoard MicexPsos { get; } = CreateMoex(BoardCodes.MicexPsos);

	/// <summary>
	/// Information about board <see cref="MicexPsov"/>.
	/// </summary>
	public static ExchangeBoard MicexPsov { get; } = CreateMoex(BoardCodes.MicexPsov);

	/// <summary>
	/// Information about board <see cref="MicexPslv"/>.
	/// </summary>
	public static ExchangeBoard MicexPslv { get; } = CreateMoex(BoardCodes.MicexPslv);

	/// <summary>
	/// Information about board <see cref="MicexPsli"/>.
	/// </summary>
	public static ExchangeBoard MicexPsli { get; } = CreateMoex(BoardCodes.MicexPsli);

	/// <summary>
	/// Information about board <see cref="MicexPsqi"/>.
	/// </summary>
	public static ExchangeBoard MicexPsqi { get; } = CreateMoex(BoardCodes.MicexPsqi);

	/// <summary>
	/// Information about board <see cref="MicexRpeu"/>.
	/// </summary>
	public static ExchangeBoard MicexRpeu { get; } = CreateMoex(BoardCodes.MicexRpeu);

	/// <summary>
	/// Information about board <see cref="MicexRpma"/>.
	/// </summary>
	public static ExchangeBoard MicexRpma { get; } = CreateMoex(BoardCodes.MicexRpma);

	/// <summary>
	/// Information about board <see cref="MicexRpmo"/>.
	/// </summary>
	public static ExchangeBoard MicexRpmo { get; } = CreateMoex(BoardCodes.MicexRpmo);

	/// <summary>
	/// Information about board <see cref="MicexRpua"/>.
	/// </summary>
	public static ExchangeBoard MicexRpua { get; } = CreateMoex(BoardCodes.MicexRpua);

	/// <summary>
	/// Information about board <see cref="MicexRpuo"/>.
	/// </summary>
	public static ExchangeBoard MicexRpuo { get; } = CreateMoex(BoardCodes.MicexRpuo);

	/// <summary>
	/// Information about board <see cref="MicexRpuq"/>.
	/// </summary>
	public static ExchangeBoard MicexRpuq { get; } = CreateMoex(BoardCodes.MicexRpuq);

	/// <summary>
	/// Information about board <see cref="MicexFbcb"/>.
	/// </summary>
	public static ExchangeBoard MicexFbcb { get; } = CreateMoex(BoardCodes.MicexFbcb);

	/// <summary>
	/// Information about board <see cref="MicexFbfx"/>.
	/// </summary>
	public static ExchangeBoard MicexFbfx { get; } = CreateMoex(BoardCodes.MicexFbfx);

	/// <summary>
	/// Information about board <see cref="MicexIrk2"/>.
	/// </summary>
	public static ExchangeBoard MicexIrk2 { get; } = CreateMoex(BoardCodes.MicexIrk2);

	/// <summary>
	/// Information about board <see cref="MicexRpqi"/>.
	/// </summary>
	public static ExchangeBoard MicexRpqi { get; } = CreateMoex(BoardCodes.MicexRpqi);

	/// <summary>
	/// Information about board <see cref="MicexPteq"/>.
	/// </summary>
	public static ExchangeBoard MicexPteq { get; } = CreateMoex(BoardCodes.MicexPteq);

	/// <summary>
	/// Information about board <see cref="MicexPtes"/>.
	/// </summary>
	public static ExchangeBoard MicexPtes { get; } = CreateMoex(BoardCodes.MicexPtes);

	/// <summary>
	/// Information about board <see cref="MicexPteu"/>.
	/// </summary>
	public static ExchangeBoard MicexPteu { get; } = CreateMoex(BoardCodes.MicexPteu);

	/// <summary>
	/// Information about board <see cref="MicexPtus"/>.
	/// </summary>
	public static ExchangeBoard MicexPtus { get; } = CreateMoex(BoardCodes.MicexPtus);

	/// <summary>
	/// Information about board <see cref="MicexPtnb"/>.
	/// </summary>
	public static ExchangeBoard MicexPtnb { get; } = CreateMoex(BoardCodes.MicexPtnb);

	/// <summary>
	/// Information about board <see cref="MicexPtne"/>.
	/// </summary>
	public static ExchangeBoard MicexPtne { get; } = CreateMoex(BoardCodes.MicexPtne);

	/// <summary>
	/// Information about board <see cref="MicexPtnl"/>.
	/// </summary>
	public static ExchangeBoard MicexPtnl { get; } = CreateMoex(BoardCodes.MicexPtnl);

	/// <summary>
	/// Information about board <see cref="MicexPtno"/>.
	/// </summary>
	public static ExchangeBoard MicexPtno { get; } = CreateMoex(BoardCodes.MicexPtno);

	/// <summary>
	/// Information about board <see cref="MicexPtob"/>.
	/// </summary>
	public static ExchangeBoard MicexPtob { get; } = CreateMoex(BoardCodes.MicexPtob);

	/// <summary>
	/// Information about board <see cref="MicexPtos"/>.
	/// </summary>
	public static ExchangeBoard MicexPtos { get; } = CreateMoex(BoardCodes.MicexPtos);

	/// <summary>
	/// Information about board <see cref="MicexPtov"/>.
	/// </summary>
	public static ExchangeBoard MicexPtov { get; } = CreateMoex(BoardCodes.MicexPtov);

	/// <summary>
	/// Information about board <see cref="MicexPtlv"/>.
	/// </summary>
	public static ExchangeBoard MicexPtlv { get; } = CreateMoex(BoardCodes.MicexPtlv);

	/// <summary>
	/// Information about board <see cref="MicexPtli"/>.
	/// </summary>
	public static ExchangeBoard MicexPtli { get; } = CreateMoex(BoardCodes.MicexPtli);

	/// <summary>
	/// Information about board <see cref="MicexPtqi"/>.
	/// </summary>
	public static ExchangeBoard MicexPtqi { get; } = CreateMoex(BoardCodes.MicexPtqi);

	/// <summary>
	/// Information about board <see cref="MicexScvc"/>.
	/// </summary>
	public static ExchangeBoard MicexScvc { get; } = CreateMoex(BoardCodes.MicexScvc);

	/// <summary>
	/// Information about board <see cref="MicexRpng"/>.
	/// </summary>
	public static ExchangeBoard MicexRpng { get; } = CreateMoex(BoardCodes.MicexRpng);

	/// <summary>
	/// Information about board <see cref="MicexRpfg"/>.
	/// </summary>
	public static ExchangeBoard MicexRpfg { get; } = CreateMoex(BoardCodes.MicexRpfg);

	/// <summary>
	/// Information about board <see cref="MicexCbcr"/>.
	/// </summary>
	public static ExchangeBoard MicexCbcr { get; } = CreateMoex(BoardCodes.MicexCbcr);

	/// <summary>
	/// Information about board <see cref="MicexCred"/>.
	/// </summary>
	public static ExchangeBoard MicexCred { get; } = CreateMoex(BoardCodes.MicexCred);

	/// <summary>
	/// Information about board <see cref="MicexDepz"/>.
	/// </summary>
	public static ExchangeBoard MicexDepz { get; } = CreateMoex(BoardCodes.MicexDepz);

	/// <summary>
	/// Information about board <see cref="MicexDpvb"/>.
	/// </summary>
	public static ExchangeBoard MicexDpvb { get; } = CreateMoex(BoardCodes.MicexDpvb);

	/// <summary>
	/// Information about board <see cref="MicexDpfk"/>.
	/// </summary>
	public static ExchangeBoard MicexDpfk { get; } = CreateMoex(BoardCodes.MicexDpfk);

	/// <summary>
	/// Information about board <see cref="MicexDpfo"/>.
	/// </summary>
	public static ExchangeBoard MicexDpfo { get; } = CreateMoex(BoardCodes.MicexDpfo);

	/// <summary>
	/// Information about board <see cref="MicexDppf"/>.
	/// </summary>
	public static ExchangeBoard MicexDppf { get; } = CreateMoex(BoardCodes.MicexDppf);

	/// <summary>
	/// Information about board <see cref="MicexCets"/>.
	/// </summary>
	public static ExchangeBoard MicexCets { get; } = CreateMoex(BoardCodes.MicexCets);

	/// <summary>
	/// Information about board <see cref="MicexAets"/>.
	/// </summary>
	public static ExchangeBoard MicexAets { get; } = CreateMoex(BoardCodes.MicexAets);

	/// <summary>
	/// Information about board <see cref="MicexCngd"/>.
	/// </summary>
	public static ExchangeBoard MicexCngd { get; } = CreateMoex(BoardCodes.MicexCngd);

	/// <summary>
	/// Information about board <see cref="MicexTran"/>.
	/// </summary>
	public static ExchangeBoard MicexTran { get; } = CreateMoex(BoardCodes.MicexTran);

	/// <summary>
	/// Information about board <see cref="MicexJunior"/>.
	/// </summary>
	public static ExchangeBoard MicexJunior { get; } = CreateMoex(BoardCodes.MicexJunior);

	/// <summary>
	/// Information about board <see cref="Spb"/>.
	/// </summary>
	public static ExchangeBoard Spb { get; } = new()
	{
		Code = BoardCodes.Spb,
		Exchange = Exchange.Spb,
		TimeZone = TimeHelper.Moscow,
	};

	/// <summary>
	/// Information about board <see cref="Ux"/>.
	/// </summary>
	public static ExchangeBoard Ux { get; } = new()
	{
		Code = BoardCodes.Ux,
		WorkingTime = new()
		{
			IsEnabled = true,
			Periods =
			[
				new()
				{
					Till = DateTime.MaxValue,
					Times =
					[
						new("10:30:00".To<TimeSpan>(), "13:00:00".To<TimeSpan>()),
						new("13:03:00".To<TimeSpan>(), "17:30:00".To<TimeSpan>())
					],
				}
			],
		},
		ExpiryTime = new TimeSpan(18, 45, 00),
		//IsSupportAtomicReRegister = true,
		Exchange = Exchange.Ux,
		TimeZone = TimeHelper.Fle,
	};

	/// <summary>
	/// Information about board <see cref="UxStock"/>.
	/// </summary>
	public static ExchangeBoard UxStock { get; } = new()
	{
		Code = BoardCodes.UxStock,
		WorkingTime = new()
		{
			IsEnabled = true,
			Periods =
			[
				new()
				{
					Till = DateTime.MaxValue,
					Times =
					[
						new("10:30:00".To<TimeSpan>(), "17:30:00".To<TimeSpan>())
					],
				}
			],
		},
		Exchange = Exchange.Ux,
		TimeZone = TimeHelper.Fle,
	};

	/// <summary>
	/// Information about board <see cref="Cme"/>.
	/// </summary>
	public static ExchangeBoard Cme { get; } = new()
	{
		Code = BoardCodes.Cme,
		TimeZone = TimeHelper.Cst,
		Exchange = Exchange.Cme,
	};

	/// <summary>
	/// Information about board <see cref="Cme"/>.
	/// </summary>
	public static ExchangeBoard CmeMini { get; } = new()
	{
		Code = BoardCodes.CmeMini,
		TimeZone = TimeHelper.Cst,
		Exchange = Exchange.Cme,
	};

	/// <summary>
	/// Information about board <see cref="Cce"/>.
	/// </summary>
	public static ExchangeBoard Cce { get; } = new()
	{
		Code = BoardCodes.Cce,
		TimeZone = TimeHelper.Cst,
		Exchange = Exchange.Cce,
	};

	/// <summary>
	/// Information about board <see cref="Cbot"/>.
	/// </summary>
	public static ExchangeBoard Cbot { get; } = new()
	{
		Code = BoardCodes.Cbot,
		TimeZone = TimeHelper.Cst,
		Exchange = Exchange.Cbot,
	};

	/// <summary>
	/// Information about board <see cref="Nymex"/>.
	/// </summary>
	public static ExchangeBoard Nymex { get; } = new()
	{
		Code = BoardCodes.Nymex,
		TimeZone = TimeHelper.Est,
		Exchange = Exchange.Nymex,
	};

	/// <summary>
	/// Information about board <see cref="Amex"/>.
	/// </summary>
	public static ExchangeBoard Amex { get; } = new()
	{
		Code = BoardCodes.Amex,
		WorkingTime = new()
		{
			IsEnabled = true,
			Periods =
			[
				new()
				{
					Till = DateTime.MaxValue,
					Times =
					[
						new("09:30:00".To<TimeSpan>(), "16:00:00".To<TimeSpan>())
					],
				}
			],
		},
		//IsSupportMarketOrders = true,
		TimeZone = TimeHelper.Est,
		Exchange = Exchange.Amex
	};

	/// <summary>
	/// Information about board <see cref="Nyse"/>.
	/// </summary>
	public static ExchangeBoard Nyse { get; } = new()
	{
		Code = BoardCodes.Nyse,
		WorkingTime = new()
		{
			IsEnabled = true,
			Periods =
			[
				new()
				{
					Till = DateTime.MaxValue,
					Times =
					[
						new("09:30:00".To<TimeSpan>(), "16:00:00".To<TimeSpan>())
					],
				}
			],
		},
		//IsSupportMarketOrders = true,
		TimeZone = TimeHelper.Est,
		Exchange = Exchange.Nyse
	};

	/// <summary>
	/// Information about board <see cref="Nasdaq"/>.
	/// </summary>
	public static ExchangeBoard Nasdaq { get; } = new()
	{
		Code = BoardCodes.Nasdaq,
		WorkingTime = new()
		{
			IsEnabled = true,
			Periods =
			[
				new()
				{
					Till = DateTime.MaxValue,
					Times =
					[
						new("09:30:00".To<TimeSpan>(), "16:00:00".To<TimeSpan>())
					],
				}
			],
		},
		//IsSupportMarketOrders = true,
		Exchange = Exchange.Nasdaq,
		TimeZone = TimeHelper.Est,
	};

	/// <summary>
	/// Information about board <see cref="Nqlx"/>.
	/// </summary>
	public static ExchangeBoard Nqlx { get; } = new()
	{
		Code = BoardCodes.Nqlx,
		Exchange = Exchange.Nqlx,
		TimeZone = TimeHelper.Est,
	};

	/// <summary>
	/// Information about board <see cref="Lse"/>.
	/// </summary>
	public static ExchangeBoard Lse { get; } = new()
	{
		Code = BoardCodes.Lse,
		WorkingTime = new()
		{
			IsEnabled = true,
			Periods =
			[
				new()
				{
					Till = DateTime.MaxValue,
					Times =
					[
						new("08:00:00".To<TimeSpan>(), "16:30:00".To<TimeSpan>())
					],
				}
			],
		},
		Exchange = Exchange.Lse,
		TimeZone = TimeHelper.Gmt,
	};

	/// <summary>
	/// Information about board <see cref="Lme"/>.
	/// </summary>
	public static ExchangeBoard Lme { get; } = new()
	{
		Code = BoardCodes.Lme,
		WorkingTime = new()
		{
			IsEnabled = true,
			Periods =
			[
				new()
				{
					Till = DateTime.MaxValue,
					Times =
					[
						new("09:00:00".To<TimeSpan>(), "18:00:00".To<TimeSpan>())
					],
				}
			],
		},
		Exchange = Exchange.Lme,
		TimeZone = TimeHelper.Gmt,
	};

	/// <summary>
	/// Information about board <see cref="Tse"/>.
	/// </summary>
	public static ExchangeBoard Tse { get; } = new()
	{
		Code = BoardCodes.Tse,
		WorkingTime = new()
		{
			IsEnabled = true,
			Periods =
			[
				new()
				{
					Till = DateTime.MaxValue,
					Times =
					[
						new("09:00:00".To<TimeSpan>(), "11:30:00".To<TimeSpan>()),
						new("12:30:00".To<TimeSpan>(), "15:00:00".To<TimeSpan>())
					],
				}
			],
		},
		Exchange = Exchange.Tse,
		TimeZone = TimeHelper.Tokyo,
	};

	/// <summary>
	/// Information about board <see cref="Hkex"/>.
	/// </summary>
	public static ExchangeBoard Hkex { get; } = new()
	{
		Code = BoardCodes.Hkex,
		WorkingTime = new()
		{
			IsEnabled = true,
			Periods =
			[
				new()
				{
					Till = DateTime.MaxValue,
					Times =
					[
						new("09:20:00".To<TimeSpan>(), "12:00:00".To<TimeSpan>()),
						new("13:00:00".To<TimeSpan>(), "16:00:00".To<TimeSpan>())
					],
				}
			],
		},
		Exchange = Exchange.Hkex,
		TimeZone = TimeHelper.China,
	};

	/// <summary>
	/// Information about board <see cref="Hkfe"/>.
	/// </summary>
	public static ExchangeBoard Hkfe { get; } = new()
	{
		Code = BoardCodes.Hkfe,
		WorkingTime = new()
		{
			IsEnabled = true,
			Periods =
			[
				new()
				{
					Till = DateTime.MaxValue,
					Times =
					[
						new("09:15:00".To<TimeSpan>(), "12:00:00".To<TimeSpan>()),
						new("13:00:00".To<TimeSpan>(), "16:00:00".To<TimeSpan>())
					],
				}
			],
		},
		Exchange = Exchange.Hkfe,
		TimeZone = TimeHelper.China,
	};

	/// <summary>
	/// Information about board <see cref="Sse"/>.
	/// </summary>
	public static ExchangeBoard Sse { get; } = new()
	{
		Code = BoardCodes.Sse,
		WorkingTime = new()
		{
			IsEnabled = true,
			Periods =
			[
				new()
				{
					Till = DateTime.MaxValue,
					Times =
					[
						new("09:30:00".To<TimeSpan>(), "11:30:00".To<TimeSpan>()),
						new("13:00:00".To<TimeSpan>(), "15:00:00".To<TimeSpan>())
					],
				}
			],
		},
		Exchange = Exchange.Sse,
		TimeZone = TimeHelper.China,
	};

	/// <summary>
	/// Information about board <see cref="Szse"/>.
	/// </summary>
	public static ExchangeBoard Szse { get; } = new()
	{
		Code = BoardCodes.Szse,
		WorkingTime = new()
		{
			IsEnabled = true,
			Periods =
			[
				new()
				{
					Till = DateTime.MaxValue,
					Times =
					[
						new("09:30:00".To<TimeSpan>(), "11:30:00".To<TimeSpan>()),
						new("13:00:00".To<TimeSpan>(), "15:00:00".To<TimeSpan>())
					],
				}
			],
		},
		Exchange = Exchange.Szse,
		TimeZone = TimeHelper.China,
	};

	/// <summary>
	/// Information about board <see cref="Tsx"/>.
	/// </summary>
	public static ExchangeBoard Tsx { get; } = new()
	{
		Code = BoardCodes.Tsx,
		WorkingTime = new()
		{
			IsEnabled = true,
			Periods =
			[
				new()
				{
					Till = DateTime.MaxValue,
					Times =
					[
						new("09:30:00".To<TimeSpan>(), "16:00:00".To<TimeSpan>())
					],
				}
			],
		},
		Exchange = Exchange.Tsx,
		TimeZone = TimeHelper.Est,
	};

	/// <summary>
	/// Information about board <see cref="Fwb"/>.
	/// </summary>
	public static ExchangeBoard Fwb { get; } = new()
	{
		Code = BoardCodes.Fwb,
		WorkingTime = new()
		{
			IsEnabled = true,
			Periods =
			[
				new()
				{
					Till = DateTime.MaxValue,
					Times =
					[
						new("08:00:00".To<TimeSpan>(), "22:00:00".To<TimeSpan>())
					],
				}
			],
		},
		Exchange = Exchange.Fwb,
		TimeZone = "W. Europe Standard Time".To<TimeZoneInfo>(),
	};

	/// <summary>
	/// Information about board <see cref="Asx"/>.
	/// </summary>
	public static ExchangeBoard Asx { get; } = new()
	{
		Code = BoardCodes.Asx,
		WorkingTime = new()
		{
			IsEnabled = true,
			Periods =
			[
				new()
				{
					Till = DateTime.MaxValue,
					Times =
					[
						new("09:50:00".To<TimeSpan>(), "16:12:00".To<TimeSpan>())
					],
				}
			],
		},
		Exchange = Exchange.Asx,
		TimeZone = "AUS Eastern Standard Time".To<TimeZoneInfo>(),
	};

	/// <summary>
	/// Information about board <see cref="Nzx"/>.
	/// </summary>
	public static ExchangeBoard Nzx { get; } = new()
	{
		Code = BoardCodes.Nzx,
		WorkingTime = new()
		{
			IsEnabled = true,
			Periods =
			[
				new()
				{
					Till = DateTime.MaxValue,
					Times =
					[
						new("10:00:00".To<TimeSpan>(), "17:00:00".To<TimeSpan>())
					],
				}
			],
		},
		Exchange = Exchange.Nzx,
		TimeZone = "New Zealand Standard Time".To<TimeZoneInfo>(),
	};

	/// <summary>
	/// Information about board <see cref="Bse"/>.
	/// </summary>
	public static ExchangeBoard Bse { get; } = new()
	{
		Code = BoardCodes.Bse,
		WorkingTime = new()
		{
			IsEnabled = true,
			Periods =
			[
				new()
				{
					Till = DateTime.MaxValue,
					Times =
					[
						new("09:15:00".To<TimeSpan>(), "15:30:00".To<TimeSpan>())
					],
				}
			],
		},
		Exchange = Exchange.Bse,
		TimeZone = _indiaTime,
	};

	/// <summary>
	/// Information about board <see cref="Nse"/>.
	/// </summary>
	public static ExchangeBoard Nse { get; } = new()
	{
		Code = BoardCodes.Nse,
		WorkingTime = new()
		{
			IsEnabled = true,
			Periods =
			[
				new()
				{
					Till = DateTime.MaxValue,
					Times =
					[
						new("09:15:00".To<TimeSpan>(), "15:30:00".To<TimeSpan>())
					],
				}
			],
		},
		Exchange = Exchange.Nse,
		TimeZone = _indiaTime,
	};

	/// <summary>
	/// Information about board <see cref="Swx"/>.
	/// </summary>
	public static ExchangeBoard Swx { get; } = new()
	{
		Code = BoardCodes.Swx,
		WorkingTime = new()
		{
			IsEnabled = true,
			Periods =
			[
				new()
				{
					Till = DateTime.MaxValue,
					Times =
					[
						new("9:00:00".To<TimeSpan>(), "17:30:00".To<TimeSpan>())
					],
				}
			],
		},
		Exchange = Exchange.Swx,
		TimeZone = GetTimeZone("Central European Standard Time", TimeSpan.FromHours(1)),
	};

	/// <summary>
	/// Information about board <see cref="Krx"/>.
	/// </summary>
	public static ExchangeBoard Krx { get; } = new()
	{
		Code = BoardCodes.Krx,
		WorkingTime = new()
		{
			IsEnabled = true,
			Periods =
			[
				new()
				{
					Till = DateTime.MaxValue,
					Times =
					[
						new("09:00:00".To<TimeSpan>(), "15:00:00".To<TimeSpan>())
					],
				}
			],
		},
		Exchange = Exchange.Krx,
		TimeZone = TimeHelper.Korea,
	};

	/// <summary>
	/// Information about board <see cref="Mse"/>.
	/// </summary>
	public static ExchangeBoard Mse { get; } = new()
	{
		Code = BoardCodes.Mse,
		WorkingTime = new()
		{
			IsEnabled = true,
			Periods =
			[
				new()
				{
					Till = DateTime.MaxValue,
					Times =
					[
						new("9:00:00".To<TimeSpan>(), "17:30:00".To<TimeSpan>())
					],
				}
			],
		},
		Exchange = Exchange.Mse,
		TimeZone = "Romance Standard Time".To<TimeZoneInfo>(),
	};

	/// <summary>
	/// Information about board <see cref="Jse"/>.
	/// </summary>
	public static ExchangeBoard Jse { get; } = new()
	{
		Code = BoardCodes.Jse,
		WorkingTime = new()
		{
			IsEnabled = true,
			Periods =
			[
				new()
				{
					Till = DateTime.MaxValue,
					Times =
					[
						new("9:00:00".To<TimeSpan>(), "17:00:00".To<TimeSpan>())
					],
				}
			],
		},
		Exchange = Exchange.Jse,
		TimeZone = GetTimeZone("South Africa Standard Time", TimeSpan.FromHours(2)),
	};

	/// <summary>
	/// Information about board <see cref="Sgx"/>.
	/// </summary>
	public static ExchangeBoard Sgx { get; } = new()
	{
		Code = BoardCodes.Sgx,
		WorkingTime = new()
		{
			IsEnabled = true,
			Periods =
			[
				new()
				{
					Till = DateTime.MaxValue,
					Times =
					[
						new("09:00:00".To<TimeSpan>(), "17:00:00".To<TimeSpan>())
					],
				}
			],
		},
		Exchange = Exchange.Sgx,
		TimeZone = _singaporeTime,
	};

	/// <summary>
	/// Information about board <see cref="Tsec"/>.
	/// </summary>
	public static ExchangeBoard Tsec { get; } = new()
	{
		Code = BoardCodes.Tsec,
		WorkingTime = new()
		{
			IsEnabled = true,
			Periods =
			[
				new()
				{
					Till = DateTime.MaxValue,
					Times =
					[
						new("09:00:00".To<TimeSpan>(), "13:30:00".To<TimeSpan>())
					],
				}
			],
		},
		Exchange = Exchange.Tsec,
		TimeZone = TimeHelper.China,
	};

	/// <summary>
	/// Information about board <see cref="Pse"/>.
	/// </summary>
	public static ExchangeBoard Pse { get; } = new()
	{
		Code = BoardCodes.Pse,
		WorkingTime = new()
		{
			IsEnabled = true,
			Periods =
			[
				new()
				{
					Till = DateTime.MaxValue,
					Times =
					[
						new("09:30:00".To<TimeSpan>(), "12:00:00".To<TimeSpan>()),
						new("13:30:00".To<TimeSpan>(), "15:30:00".To<TimeSpan>())
					],
				}
			],
		},
		Exchange = Exchange.Pse,
		TimeZone = _singaporeTime,
	};

	/// <summary>
	/// Information about board <see cref="Klse"/>.
	/// </summary>
	public static ExchangeBoard Klse { get; } = new()
	{
		Code = BoardCodes.Klse,
		WorkingTime = new()
		{
			IsEnabled = true,
			Periods =
			[
				new()
				{
					Till = DateTime.MaxValue,
					Times =
					[
						new("09:00:00".To<TimeSpan>(), "12:30:00".To<TimeSpan>()),
						new("14:00:00".To<TimeSpan>(), "17:00:00".To<TimeSpan>())
					],
				}
			],
		},
		Exchange = Exchange.Klse,
		TimeZone = _singaporeTime,
	};

	/// <summary>
	/// Information about board <see cref="Idx"/>.
	/// </summary>
	public static ExchangeBoard Idx { get; } = new()
	{
		Code = BoardCodes.Idx,
		WorkingTime = new()
		{
			IsEnabled = true,
			Periods =
			[
				new()
				{
					Till = DateTime.MaxValue,
					Times =
					[
						new("09:30:00".To<TimeSpan>(), "16:00:00".To<TimeSpan>())
					],
				}
			],
		},
		Exchange = Exchange.Idx,
		TimeZone = _bangkokTime,
	};

	/// <summary>
	/// Information about board <see cref="Set"/>.
	/// </summary>
	public static ExchangeBoard Set { get; } = new()
	{
		Code = BoardCodes.Set,
		WorkingTime = new()
		{
			IsEnabled = true,
			Periods =
			[
				new()
				{
					Till = DateTime.MaxValue,
					Times =
					[
						new("10:00:00".To<TimeSpan>(), "12:30:00".To<TimeSpan>()),
						new("14:30:00".To<TimeSpan>(), "16:30:00".To<TimeSpan>())
					],
				}
			],
		},
		Exchange = Exchange.Set,
		TimeZone = _bangkokTime,
	};

	/// <summary>
	/// Information about board <see cref="Cse"/>.
	/// </summary>
	public static ExchangeBoard Cse { get; } = new()
	{
		Code = BoardCodes.Cse,
		WorkingTime = new()
		{
			IsEnabled = true,
			Periods =
			[
				new()
				{
					Till = DateTime.MaxValue,
					Times =
					[
						new("09:30:00".To<TimeSpan>(), "14:30:00".To<TimeSpan>())
					],
				}
			],
		},
		Exchange = Exchange.Cse,
		TimeZone = "Sri Lanka Standard Time".To<TimeZoneInfo>(),
	};

	/// <summary>
	/// Information about board <see cref="Tase"/>.
	/// </summary>
	public static ExchangeBoard Tase { get; } = new()
	{
		Code = BoardCodes.Tase,
		WorkingTime = new()
		{
			IsEnabled = true,
			Periods =
			[
				new()
				{
					Till = DateTime.MaxValue,
					Times =
					[
						new("09:00:00".To<TimeSpan>(), "16:25:00".To<TimeSpan>())
					],
				}
			],
		},
		Exchange = Exchange.Tase,
		TimeZone = "Israel Standard Time".To<TimeZoneInfo>(),
	};

	/// <summary>
	/// Information about board <see cref="Lmax"/>.
	/// </summary>
	public static ExchangeBoard Lmax { get; } = new()
	{
		Code = BoardCodes.Lmax,
		WorkingTime = new()
		{
			IsEnabled = true,
			Periods =
			[
				new()
				{
					Till = DateTime.MaxValue,
					Times =
					[
						new("9:00:00".To<TimeSpan>(), "17:00:00".To<TimeSpan>())
					],
				}
			],
		},
		Exchange = Exchange.Lmax,
	};

	/// <summary>
	/// Information about board <see cref="DukasCopy"/>.
	/// </summary>
	public static ExchangeBoard DukasCopy { get; } = new()
	{
		Code = BoardCodes.DukasCopy,
		Exchange = Exchange.DukasCopy,
	};

	/// <summary>
	/// Information about board <see cref="GainCapital"/>.
	/// </summary>
	public static ExchangeBoard GainCapital { get; } = new()
	{
		Code = BoardCodes.GainCapital,
		Exchange = Exchange.GainCapital,
	};

	/// <summary>
	/// Information about board <see cref="MBTrading"/>.
	/// </summary>
	public static ExchangeBoard MBTrading { get; } = new()
	{
		Code = BoardCodes.MBTrading,
		Exchange = Exchange.MBTrading,
	};

	/// <summary>
	/// Information about board <see cref="TrueFX"/>.
	/// </summary>
	public static ExchangeBoard TrueFX { get; } = new()
	{
		Code = BoardCodes.TrueFX,
		Exchange = Exchange.TrueFX,
	};

	/// <summary>
	/// Information about board <see cref="Integral"/>.
	/// </summary>
	public static ExchangeBoard Integral { get; } = new()
	{
		Code = BoardCodes.Integral,
		Exchange = Exchange.Integral,
	};

	/// <summary>
	/// Information about board <see cref="Cfh"/>.
	/// </summary>
	public static ExchangeBoard Cfh { get; } = new()
	{
		Code = BoardCodes.Cfh,
		Exchange = Exchange.Cfh,
	};

	/// <summary>
	/// Information about board <see cref="Ond"/>.
	/// </summary>
	public static ExchangeBoard Ond { get; } = new()
	{
		Code = BoardCodes.Ond,
		Exchange = Exchange.Ond,
	};

	/// <summary>
	/// Information about board <see cref="Nasdaq"/>.
	/// </summary>
	public static ExchangeBoard Smart { get; } = new()
	{
		Code = BoardCodes.Smart,
		Exchange = Exchange.Nasdaq,
	};

	/// <summary>
	/// Information about board <see cref="Btce"/>.
	/// </summary>
	public static ExchangeBoard Btce { get; } = new()
	{
		Code = Exchange.Btce.Name,
		Exchange = Exchange.Btce,
	};

	/// <summary>
	/// Information about board <see cref="BitStamp"/>.
	/// </summary>
	public static ExchangeBoard BitStamp { get; } = new()
	{
		Code = Exchange.BitStamp.Name,
		Exchange = Exchange.BitStamp,
	};

	/// <summary>
	/// Information about board <see cref="BtcChina"/>.
	/// </summary>
	public static ExchangeBoard BtcChina { get; } = new()
	{
		Code = Exchange.BtcChina.Name,
		Exchange = Exchange.BtcChina,
	};

	/// <summary>
	/// Information about board <see cref="Icbit"/>.
	/// </summary>
	public static ExchangeBoard Icbit { get; } = new()
	{
		Code = Exchange.Icbit.Name,
		Exchange = Exchange.Icbit,
	};

	/// <summary>
	/// Information about board <see cref="Finam"/>.
	/// </summary>
	public static ExchangeBoard Finam { get; } = new()
	{
		Code = BoardCodes.Finam,
		Exchange = Exchange.Test,
	};

	/// <summary>
	/// Information about board <see cref="Mfd"/>.
	/// </summary>
	public static ExchangeBoard Mfd { get; } = new()
	{
		Code = BoardCodes.Mfd,
		Exchange = Exchange.Test,
	};

	/// <summary>
	/// Information about board <see cref="Arca"/>.
	/// </summary>
	public static ExchangeBoard Arca { get; } = new()
	{
		Code = BoardCodes.Arca,
		Exchange = Exchange.Nyse,
	};

	/// <summary>
	/// Information about board <see cref="Bats"/>.
	/// </summary>
	public static ExchangeBoard Bats { get; } = new()
	{
		Code = BoardCodes.Bats,
		Exchange = Exchange.Cbot,
	};

	/// <summary>
	/// Information about board <see cref="Currenex"/>.
	/// </summary>
	public static ExchangeBoard Currenex { get; } = new()
	{
		Code = Exchange.Currenex.Name,
		Exchange = Exchange.Currenex,
	};

	/// <summary>
	/// Information about board <see cref="Fxcm"/>.
	/// </summary>
	public static ExchangeBoard Fxcm { get; } = new()
	{
		Code = Exchange.Fxcm.Name,
		Exchange = Exchange.Fxcm,
	};

	/// <summary>
	/// Information about board <see cref="Poloniex"/>.
	/// </summary>
	public static ExchangeBoard Poloniex { get; } = new()
	{
		Code = Exchange.Poloniex.Name,
		Exchange = Exchange.Poloniex,
	};

	/// <summary>
	/// Information about board <see cref="Kraken"/>.
	/// </summary>
	public static ExchangeBoard Kraken { get; } = new()
	{
		Code = Exchange.Kraken.Name,
		Exchange = Exchange.Kraken,
	};

	/// <summary>
	/// Information about board <see cref="Bittrex"/>.
	/// </summary>
	public static ExchangeBoard Bittrex { get; } = new()
	{
		Code = Exchange.Bittrex.Name,
		Exchange = Exchange.Bittrex,
	};

	/// <summary>
	/// Information about board <see cref="Bitfinex"/>.
	/// </summary>
	public static ExchangeBoard Bitfinex { get; } = new()
	{
		Code = Exchange.Bitfinex.Name,
		Exchange = Exchange.Bitfinex,
	};

	/// <summary>
	/// Information about board <see cref="Coinbase"/>.
	/// </summary>
	public static ExchangeBoard Coinbase { get; } = new()
	{
		Code = Exchange.Coinbase.Name,
		Exchange = Exchange.Coinbase,
	};

	/// <summary>
	/// Information about board <see cref="Gdax"/>.
	/// </summary>
	public static ExchangeBoard Gdax { get; } = new()
	{
		Code = Exchange.Gdax.Name,
		Exchange = Exchange.Gdax,
	};

	/// <summary>
	/// Information about board <see cref="Bithumb"/>.
	/// </summary>
	public static ExchangeBoard Bithumb { get; } = new()
	{
		Code = Exchange.Bithumb.Name,
		Exchange = Exchange.Bithumb,
	};

	/// <summary>
	/// Information about board <see cref="HitBtc"/>.
	/// </summary>
	public static ExchangeBoard HitBtc { get; } = new()
	{
		Code = Exchange.HitBtc.Name,
		Exchange = Exchange.HitBtc,
	};

	/// <summary>
	/// Information about board <see cref="OkCoin"/>.
	/// </summary>
	public static ExchangeBoard OkCoin { get; } = new()
	{
		Code = Exchange.OkCoin.Name,
		Exchange = Exchange.OkCoin,
	};

	/// <summary>
	/// Information about board <see cref="Coincheck"/>.
	/// </summary>
	public static ExchangeBoard Coincheck { get; } = new()
	{
		Code = Exchange.Coincheck.Name,
		Exchange = Exchange.Coincheck,
	};

	/// <summary>
	/// Information about board <see cref="Binance"/>.
	/// </summary>
	public static ExchangeBoard Binance { get; } = new()
	{
		Code = Exchange.Binance.Name,
		Exchange = Exchange.Binance,
	};

	/// <summary>
	/// Information about board <see cref="BinanceCoin"/>.
	/// </summary>
	public static ExchangeBoard BinanceCoin { get; } = new()
	{
		Code = BoardCodes.BinanceCoin,
		Exchange = Exchange.Binance,
	};

	/// <summary>
	/// Information about board <see cref="Bitexbook"/>.
	/// </summary>
	public static ExchangeBoard Bitexbook { get; } = new()
	{
		Code = Exchange.Bitexbook.Name,
		Exchange = Exchange.Bitexbook,
	};

	/// <summary>
	/// Information about board <see cref="Bitmex"/>.
	/// </summary>
	public static ExchangeBoard Bitmex { get; } = new()
	{
		Code = Exchange.Bitmex.Name,
		Exchange = Exchange.Bitmex,
	};

	/// <summary>
	/// Information about board <see cref="Cex"/>.
	/// </summary>
	public static ExchangeBoard Cex { get; } = new()
	{
		Code = Exchange.Cex.Name,
		Exchange = Exchange.Cex,
	};

	/// <summary>
	/// Information about board <see cref="Cryptopia"/>.
	/// </summary>
	public static ExchangeBoard Cryptopia { get; } = new()
	{
		Code = Exchange.Cryptopia.Name,
		Exchange = Exchange.Cryptopia,
	};

	/// <summary>
	/// Information about board <see cref="Okex"/>.
	/// </summary>
	public static ExchangeBoard Okex { get; } = new()
	{
		Code = Exchange.Okex.Name,
		Exchange = Exchange.Okex,
	};

	/// <summary>
	/// Information about board <see cref="Bitmart"/>.
	/// </summary>
	public static ExchangeBoard Bitmart { get; } = new()
	{
		Code = Exchange.Bitmart.Name,
		Exchange = Exchange.Bitmart,
	};

	/// <summary>
	/// Information about board <see cref="Yobit"/>.
	/// </summary>
	public static ExchangeBoard Yobit { get; } = new()
	{
		Code = Exchange.Yobit.Name,
		Exchange = Exchange.Yobit,
	};

	/// <summary>
	/// Information about board <see cref="CoinExchange"/>.
	/// </summary>
	public static ExchangeBoard CoinExchange { get; } = new()
	{
		Code = Exchange.CoinExchange.Name,
		Exchange = Exchange.CoinExchange,
	};

	/// <summary>
	/// Information about board <see cref="LiveCoin"/>.
	/// </summary>
	public static ExchangeBoard LiveCoin { get; } = new()
	{
		Code = Exchange.LiveCoin.Name,
		Exchange = Exchange.LiveCoin,
	};

	/// <summary>
	/// Information about board <see cref="Exmo"/>.
	/// </summary>
	public static ExchangeBoard Exmo { get; } = new()
	{
		Code = Exchange.Exmo.Name,
		Exchange = Exchange.Exmo,
	};

	/// <summary>
	/// Information about board <see cref="Deribit"/>.
	/// </summary>
	public static ExchangeBoard Deribit { get; } = new()
	{
		Code = Exchange.Deribit.Name,
		Exchange = Exchange.Deribit,
	};

	/// <summary>
	/// Information about board <see cref="Kucoin"/>.
	/// </summary>
	public static ExchangeBoard Kucoin { get; } = new()
	{
		Code = Exchange.Kucoin.Name,
		Exchange = Exchange.Kucoin,
	};

	/// <summary>
	/// Information about board <see cref="Liqui"/>.
	/// </summary>
	public static ExchangeBoard Liqui { get; } = new()
	{
		Code = Exchange.Liqui.Name,
		Exchange = Exchange.Liqui,
	};

	/// <summary>
	/// Information about board <see cref="Huobi"/>.
	/// </summary>
	public static ExchangeBoard Huobi { get; } = new()
	{
		Code = Exchange.Huobi.Name,
		Exchange = Exchange.Huobi,
	};

	/// <summary>
	/// Information about board <see cref="Globex"/>.
	/// </summary>
	public static ExchangeBoard Globex { get; } = new()
	{
		Code = BoardCodes.Globex,
		Exchange = Exchange.Cme,
	};

	/// <summary>
	/// Information about board <see cref="IEX"/>.
	/// </summary>
	public static ExchangeBoard IEX { get; } = new()
	{
		Code = Exchange.IEX.Name,
		Exchange = Exchange.IEX,
	};

	/// <summary>
	/// Information about board <see cref="AlphaVantage"/>.
	/// </summary>
	public static ExchangeBoard AlphaVantage { get; } = new()
	{
		Code = Exchange.AlphaVantage.Name,
		Exchange = Exchange.AlphaVantage,
	};

	/// <summary>
	/// Information about board <see cref="Bitbank"/>.
	/// </summary>
	public static ExchangeBoard Bitbank { get; } = new()
	{
		Code = Exchange.Bitbank.Name,
		Exchange = Exchange.Bitbank,
	};

	/// <summary>
	/// Information about board <see cref="Zaif"/>.
	/// </summary>
	public static ExchangeBoard Zaif { get; } = new()
	{
		Code = Exchange.Zaif.Name,
		Exchange = Exchange.Zaif,
	};

	/// <summary>
	/// Information about board <see cref="Quoinex"/>.
	/// </summary>
	public static ExchangeBoard Quoinex { get; } = new()
	{
		Code = Exchange.Quoinex.Name,
		Exchange = Exchange.Quoinex,
	};

	/// <summary>
	/// Information about board <see cref="Wiki"/>.
	/// </summary>
	public static ExchangeBoard Wiki { get; } = new()
	{
		Code = Exchange.Wiki.Name,
		Exchange = Exchange.Wiki,
	};

	/// <summary>
	/// Information about board <see cref="Idax"/>.
	/// </summary>
	public static ExchangeBoard Idax { get; } = new()
	{
		Code = Exchange.Idax.Name,
		Exchange = Exchange.Idax,
	};

	/// <summary>
	/// Information about board <see cref="Digifinex"/>.
	/// </summary>
	public static ExchangeBoard Digifinex { get; } = new()
	{
		Code = Exchange.Digifinex.Name,
		Exchange = Exchange.Digifinex,
	};

	/// <summary>
	/// Information about board <see cref="TradeOgre"/>.
	/// </summary>
	public static ExchangeBoard TradeOgre { get; } = new()
	{
		Code = Exchange.TradeOgre.Name,
		Exchange = Exchange.TradeOgre,
	};

	/// <summary>
	/// Information about board <see cref="CoinCap"/>.
	/// </summary>
	public static ExchangeBoard CoinCap { get; } = new()
	{
		Code = Exchange.CoinCap.Name,
		Exchange = Exchange.CoinCap,
	};

	/// <summary>
	/// Information about board <see cref="Coinigy"/>.
	/// </summary>
	public static ExchangeBoard Coinigy { get; } = new()
	{
		Code = Exchange.Coinigy.Name,
		Exchange = Exchange.Coinigy,
	};

	/// <summary>
	/// Information about board <see cref="LBank"/>.
	/// </summary>
	public static ExchangeBoard LBank { get; } = new()
	{
		Code = Exchange.LBank.Name,
		Exchange = Exchange.LBank,
	};

	/// <summary>
	/// Information about board <see cref="BitMax"/>.
	/// </summary>
	public static ExchangeBoard BitMax { get; } = new()
	{
		Code = Exchange.BitMax.Name,
		Exchange = Exchange.BitMax,
	};

	/// <summary>
	/// Information about board <see cref="BW"/>.
	/// </summary>
	public static ExchangeBoard BW { get; } = new()
	{
		Code = Exchange.BW.Name,
		Exchange = Exchange.BW,
	};

	/// <summary>
	/// Information about board <see cref="Bibox"/>.
	/// </summary>
	public static ExchangeBoard Bibox { get; } = new()
	{
		Code = Exchange.Bibox.Name,
		Exchange = Exchange.Bibox,
	};

	/// <summary>
	/// Information about board <see cref="CoinBene"/>.
	/// </summary>
	public static ExchangeBoard CoinBene { get; } = new()
	{
		Code = Exchange.CoinBene.Name,
		Exchange = Exchange.CoinBene,
	};

	/// <summary>
	/// Information about board <see cref="BitZ"/>.
	/// </summary>
	public static ExchangeBoard BitZ { get; } = new()
	{
		Code = Exchange.BitZ.Name,
		Exchange = Exchange.BitZ,
	};

	/// <summary>
	/// Information about board <see cref="ZB"/>.
	/// </summary>
	public static ExchangeBoard ZB { get; } = new()
	{
		Code = Exchange.ZB.Name,
		Exchange = Exchange.ZB,
	};

	/// <summary>
	/// Information about board <see cref="Tradier"/>.
	/// </summary>
	public static ExchangeBoard Tradier { get; } = new()
	{
		Code = Exchange.Tradier.Name,
		Exchange = Exchange.Tradier,
	};

	/// <summary>
	/// Information about board <see cref="SwSq"/>.
	/// </summary>
	public static ExchangeBoard SwSq { get; } = new()
	{
		Code = Exchange.SwSq.Name,
		Exchange = Exchange.SwSq,
	};

	/// <summary>
	/// Information about board <see cref="StockSharp"/>.
	/// </summary>
	public static ExchangeBoard StockSharp { get; } = new()
	{
		Code = Exchange.StockSharp.Name,
		Exchange = Exchange.StockSharp,
	};

	/// <summary>
	/// Information about board <see cref="Upbit"/>.
	/// </summary>
	public static ExchangeBoard Upbit { get; } = new()
	{
		Code = Exchange.Upbit.Name,
		Exchange = Exchange.Upbit,
	};

	/// <summary>
	/// Information about board <see cref="CoinEx"/>.
	/// </summary>
	public static ExchangeBoard CoinEx { get; } = new()
	{
		Code = Exchange.CoinEx.Name,
		Exchange = Exchange.CoinEx,
	};

	/// <summary>
	/// Information about board <see cref="FatBtc"/>.
	/// </summary>
	public static ExchangeBoard FatBtc { get; } = new()
	{
		Code = Exchange.FatBtc.Name,
		Exchange = Exchange.FatBtc,
	};

	/// <summary>
	/// Information about board <see cref="Latoken"/>.
	/// </summary>
	public static ExchangeBoard Latoken { get; } = new()
	{
		Code = Exchange.Latoken.Name,
		Exchange = Exchange.Latoken,
	};

	/// <summary>
	/// Information about board <see cref="Gopax"/>.
	/// </summary>
	public static ExchangeBoard Gopax { get; } = new()
	{
		Code = Exchange.Gopax.Name,
		Exchange = Exchange.Gopax,
	};

	/// <summary>
	/// Information about board <see cref="CoinHub"/>.
	/// </summary>
	public static ExchangeBoard CoinHub { get; } = new()
	{
		Code = Exchange.CoinHub.Name,
		Exchange = Exchange.CoinHub,
	};

	/// <summary>
	/// Information about board <see cref="Hotbit"/>.
	/// </summary>
	public static ExchangeBoard Hotbit { get; } = new()
	{
		Code = Exchange.Hotbit.Name,
		Exchange = Exchange.Hotbit,
	};

	/// <summary>
	/// Information about board <see cref="Bitalong"/>.
	/// </summary>
	public static ExchangeBoard Bitalong { get; } = new()
	{
		Code = Exchange.Bitalong.Name,
		Exchange = Exchange.Bitalong,
	};

	/// <summary>
	/// Information about board <see cref="PrizmBit"/>.
	/// </summary>
	public static ExchangeBoard PrizmBit { get; } = new()
	{
		Code = Exchange.PrizmBit.Name,
		Exchange = Exchange.PrizmBit,
	};

	/// <summary>
	/// Information about board <see cref="DigitexFutures"/>.
	/// </summary>
	public static ExchangeBoard DigitexFutures { get; } = new()
	{
		Code = Exchange.DigitexFutures.Name,
		Exchange = Exchange.DigitexFutures,
	};

	/// <summary>
	/// Information about board <see cref="Bovespa"/>.
	/// </summary>
	public static ExchangeBoard Bovespa { get; } = new()
	{
		Code = Exchange.Bovespa.Name,
		Exchange = Exchange.Bovespa,
	};

	/// <summary>
	/// Information about board <see cref="Bvmt"/>.
	/// </summary>
	public static ExchangeBoard Bvmt { get; } = new()
	{
		Code = Exchange.Bvmt.Name,
		Exchange = Exchange.Bvmt,
		TimeZone = TimeHelper.Tunisia,
		WorkingTime = new()
		{
			IsEnabled = true,
			Periods =
			[
				new()
				{
					Till = DateTime.MaxValue,
					Times =
					[
						new("09:00:00".To<TimeSpan>(), "14:00:00".To<TimeSpan>())
					],
				}
			],
		},
	};

	/// <summary>
	/// Information about board <see cref="IQFeed"/>.
	/// </summary>
	public static ExchangeBoard IQFeed { get; } = new()
	{
		Code = Exchange.IQFeed.Name,
		Exchange = Exchange.IQFeed,
	};

	/// <summary>
	/// Information about board <see cref="IBKR"/>.
	/// </summary>
	public static ExchangeBoard IBKR { get; } = new()
	{
		Code = Exchange.IBKR.Name,
		Exchange = Exchange.IBKR,
	};

	/// <summary>
	/// Information about board <see cref="STRLG"/>.
	/// </summary>
	public static ExchangeBoard STRLG { get; } = new()
	{
		Code = Exchange.STRLG.Name,
		Exchange = Exchange.STRLG,
	};

	/// <summary>
	/// Information about board <see cref="QNDL"/>.
	/// </summary>
	public static ExchangeBoard QNDL { get; } = new()
	{
		Code = Exchange.QNDL.Name,
		Exchange = Exchange.QNDL,
	};

	/// <summary>
	/// Information about board <see cref="FTX"/>.
	/// </summary>
	public static ExchangeBoard FTX { get; } = new()
	{
		Code = Exchange.FTX.Name,
		Exchange = Exchange.FTX,
	};

	/// <summary>
	/// Information about board <see cref="YHF"/>.
	/// </summary>
	public static ExchangeBoard YHF { get; } = new()
	{
		Code = Exchange.YHF.Name,
		Exchange = Exchange.YHF,
	};

	/// <summary>
	/// Information about board <see cref="EUREX"/>.
	/// </summary>
	public static ExchangeBoard EUREX { get; } = new()
	{
		Code = Exchange.EUREX.Name,
		Exchange = Exchange.EUREX,
	};
}
