#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: CurrencyTypes.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;
	using System.Xml.Serialization;

	//[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.4927")]
	/// <summary>
	/// Currency type.
	/// </summary>
	/// <remarks>
	/// The codes are set in accordance with the ISO 4217 Currency Codes.
	/// </remarks>
	[Serializable]
	[XmlType(Namespace = "http://www.webserviceX.NET/")]
	[DataContract]
	public enum CurrencyTypes
	{
// ReSharper disable InconsistentNaming
		/// <summary>
		/// Afghanistan, Afghanis.
		/// </summary>
		[EnumMember]
		AFA,

		/// <summary>
		/// Turkmenistan, Turkmenistani manat.
		/// </summary>
		[EnumMember]
		TMT,

		/// <summary>
		/// Uzbekistan, Uzbekistan som.
		/// </summary>
		[EnumMember]
		UZS,

		/// <summary>
		/// Tajikistan, Somoni.
		/// </summary>
		[EnumMember]
		TJS,

		/// <summary>
		/// Armenia, Armenian dram.
		/// </summary>
		[EnumMember]
		AMD,

		/// <summary>
		/// International Monetary Fund, Special Drawing Rights.
		/// </summary>
		[EnumMember]
		XDR,

		/// <summary>
		/// Azerbaijan, Azerbaijani manat.
		/// </summary>
		[EnumMember]
		AZN,

		/// <summary>
		/// Belarus, Belarusian ruble.
		/// </summary>
		[EnumMember]
		BYR,

		/// <summary>
		/// Belarus, Belarusian ruble.
		/// </summary>
		[EnumMember]
		BYN,

		/// <summary>
		/// Romania, Romanian new leu.
		/// </summary>
		[EnumMember]
		RON,

		/// <summary>
		/// Bulgaria, Bulgarian lev.
		/// </summary>
		[EnumMember]
		BGN,

		/// <summary>
		/// Kyrgyzstan, Kyrgyzstani som.
		/// </summary>
		[EnumMember]
		KGS,

		/// <summary>
		/// Albania, Leke.
		/// </summary>
		[EnumMember]
		ALL,

		/// <summary>
		/// Algeria, Algeria Dinars.
		/// </summary>
		[EnumMember]
		DZD,

		/// <summary>
		/// Argentina, Pesos.
		/// </summary>
		[EnumMember]
		ARS,

		/// <summary>
		/// Aruba, Guilders (also called Florins).
		/// </summary>
		[EnumMember]
		AWG,

		/// <summary>
		/// Australia, Dollars.
		/// </summary>
		[EnumMember]
		AUD,

		/// <summary>
		/// Bahamas, Dollars.
		/// </summary>
		[EnumMember]
		BSD,

		/// <summary>
		/// Bahrain, Dinars.
		/// </summary>
		[EnumMember]
		BHD,

		/// <summary>
		/// Bangladesh, Taka.
		/// </summary>
		[EnumMember]
		BDT,

		/// <summary>
		/// Barbados, Dollars.
		/// </summary>
		[EnumMember]
		BBD,

		/// <summary>
		/// Belize, Dollars.
		/// </summary>
		[EnumMember]
		BZD,

		/// <summary>
		/// Bermuda, Dollars.
		/// </summary>
		[EnumMember]
		BMD,

		/// <summary>
		/// Bhutan, Ngultrum.
		/// </summary>
		[EnumMember]
		BTN,

		/// <summary>
		/// Bolivia, Bolivianos.
		/// </summary>
		[EnumMember]
		BOB,

		/// <summary>
		/// Botswana, Pulas.
		/// </summary>
		[EnumMember]
		BWP,

		/// <summary>
		/// Brazil, Brazil Real.
		/// </summary>
		[EnumMember]
		BRL,

		/// <summary>
		/// United Kingdom, Pounds sterling.
		/// </summary>
		[EnumMember]
		GBP,

		/// <summary>
		/// Brunei Darussalam, Dollars.
		/// </summary>
		[EnumMember]
		BND,

		/// <summary>
		/// Burundi, Francs.
		/// </summary>
		[EnumMember]
		BIF,

		/// <summary>
		/// Communaute Financiere Africaine BCEAO, Francs.
		/// </summary>
		[EnumMember]
		XOF,

		/// <summary>
		/// Communaute Financiere Africaine BEAC, Francs.
		/// </summary>
		[EnumMember]
		XAF,

		/// <summary>
		/// Cambodia, Riels.
		/// </summary>
		[EnumMember]
		KHR,

		/// <summary>
		/// Canada, Dollars.
		/// </summary>
		[EnumMember]
		CAD,

		/// <summary>
		/// Cape Verde, Escudos.
		/// </summary>
		[EnumMember]
		CVE,

		/// <summary>
		/// Cayman Islands, Dollars.
		/// </summary>
		[EnumMember]
		KYD,

		/// <summary>
		/// Chile, Pesos.
		/// </summary>
		[EnumMember]
		CLP,

		/// <summary>
		/// China, Yuan Renminbi.
		/// </summary>
		[EnumMember]
		CNY,

		/// <summary>
		/// China, offshore RMB.
		/// </summary>
		[EnumMember]
		CNH,

		/// <summary>
		/// Colombia, Pesos.
		/// </summary>
		[EnumMember]
		COP,

		/// <summary>
		/// Comoros, Francs.
		/// </summary>
		[EnumMember]
		KMF,

		/// <summary>
		/// Costa Rica, Colones.
		/// </summary>
		[EnumMember]
		CRC,

		/// <summary>
		/// Croatia, Kuna.
		/// </summary>
		[EnumMember]
		HRK,

		/// <summary>
		/// Cuba, Pesos.
		/// </summary>
		[EnumMember]
		CUP,

		/// <summary>
		/// Cyprus, Pounds.
		/// </summary>
		[EnumMember]
		CYP,

		/// <summary>
		/// Czech Republic, Koruny.
		/// </summary>
		[EnumMember]
		CZK,

		/// <summary>
		/// Denmark, Kroner.
		/// </summary>
		[EnumMember]
		DKK,

		/// <summary>
		/// Djibouti, Francs.
		/// </summary>
		[EnumMember]
		DJF,

		/// <summary>
		/// Dominican Republic, Pesos.
		/// </summary>
		[EnumMember]
		DOP,

		/// <summary>
		/// East Caribbean Dollars.
		/// </summary>
		[EnumMember]
		XCD,

		/// <summary>
		/// Egypt, Pounds.
		/// </summary>
		[EnumMember]
		EGP,

		/// <summary>
		/// El Salvador, Colones.
		/// </summary>
		[EnumMember]
		SVC,

		/// <summary>
		/// Estonia, Krooni.
		/// </summary>
		[EnumMember]
		EEK,

		/// <summary>
		/// Ethiopia, Birr.
		/// </summary>
		[EnumMember]
		ETB,

		/// <summary>
		/// Euro Member Countries, Euro.
		/// </summary>
		[EnumMember]
		EUR,

		/// <summary>
		/// Falkland Islands (Malvinas), Pounds.
		/// </summary>
		[EnumMember]
		FKP,

		/// <summary>
		/// Gambia, Dalasi.
		/// </summary>
		[EnumMember]
		GMD,

		/// <summary>
		/// Ghana, Cedis.
		/// </summary>
		[EnumMember]
		GHC,

		/// <summary>
		/// Gibraltar, Pounds.
		/// </summary>
		[EnumMember]
		GIP,

		/// <summary>
		/// Gold, Ounces.
		/// </summary>
		[EnumMember]
		XAU,

		/// <summary>
		/// Guatemala, Quetzales.
		/// </summary>
		[EnumMember]
		GTQ,

		/// <summary>
		/// Guinea, Francs.
		/// </summary>
		[EnumMember]
		GNF,

		/// <summary>
		/// Guyana, Dollars.
		/// </summary>
		[EnumMember]
		GYD,

		/// <summary>
		/// Haiti, Gourdes.
		/// </summary>
		[EnumMember]
		HTG,

		/// <summary>
		/// Honduras, Lempiras.
		/// </summary>
		[EnumMember]
		HNL,

		/// <summary>
		/// Hong Kong, Dollars.
		/// </summary>
		[EnumMember]
		HKD,

		/// <summary>
		/// Hungary, Forint.
		/// </summary>
		[EnumMember]
		HUF,

		/// <summary>
		/// Iceland, Kronur.
		/// </summary>
		[EnumMember]
		ISK,

		/// <summary>
		/// India, Rupees.
		/// </summary>
		[EnumMember]
		INR,

		/// <summary>
		/// Indonesia, Rupiahs.
		/// </summary>
		[EnumMember]
		IDR,

		/// <summary>
		/// Iraq, Dinars.
		/// </summary>
		[EnumMember]
		IQD,

		/// <summary>
		/// Israel, New Shekels.
		/// </summary>
		[EnumMember]
		ILS,

		/// <summary>
		/// Jamaica, Dollars.
		/// </summary>
		[EnumMember]
		JMD,

		/// <summary>
		/// Japan, Yen.
		/// </summary>
		[EnumMember]
		JPY,

		/// <summary>
		/// Jordan, Dinars.
		/// </summary>
		[EnumMember]
		JOD,

		/// <summary>
		/// Kazakstan, Tenge.
		/// </summary>
		[EnumMember]
		KZT,

		/// <summary>
		/// Kenya, Shillings.
		/// </summary>
		[EnumMember]
		KES,

		/// <summary>
		/// Korea (South), Won.
		/// </summary>
		[EnumMember]
		KRW,

		/// <summary>
		/// Kuwait, Dinars.
		/// </summary>
		[EnumMember]
		KWD,

		/// <summary>
		/// Laos, Kips.
		/// </summary>
		[EnumMember]
		LAK,

		/// <summary>
		/// Latvia, Lati.
		/// </summary>
		[EnumMember]
		LVL,

		/// <summary>
		/// Lebanon, Pounds.
		/// </summary>
		[EnumMember]
		LBP,

		/// <summary>
		/// Lesotho, Maloti.
		/// </summary>
		[EnumMember]
		LSL,

		/// <summary>
		/// Liberia, Dollars.
		/// </summary>
		[EnumMember]
		LRD,

		/// <summary>
		/// Libya, Dinars.
		/// </summary>
		[EnumMember]
		LYD,

		/// <summary>
		/// Lithuania, Litai.
		/// </summary>
		[EnumMember]
		LTL,

		/// <summary>
		/// Macau, Patacas.
		/// </summary>
		[EnumMember]
		MOP,

		/// <summary>
		/// Macedonia, Denars.
		/// </summary>
		[EnumMember]
		MKD,

		/// <summary>
		/// Malagasy, Franc.
		/// </summary>
		[EnumMember]
		MGF,

		/// <summary>
		/// Malawi, Kwachas.
		/// </summary>
		[EnumMember]
		MWK,

		/// <summary>
		/// Malaysia, Ringgits.
		/// </summary>
		[EnumMember]
		MYR,

		/// <summary>
		/// Maldives (Maldive Islands), Rufiyaa.
		/// </summary>
		[EnumMember]
		MVR,

		/// <summary>
		/// Malta, Liri.
		/// </summary>
		[EnumMember]
		MTL,

		/// <summary>
		/// Mauritania, Ouguiyas.
		/// </summary>
		[EnumMember]
		MRO,

		/// <summary>
		/// Mauritius, Rupees.
		/// </summary>
		[EnumMember]
		MUR,

		/// <summary>
		/// Mexico, Pesos.
		/// </summary>
		[EnumMember]
		MXN,

		/// <summary>
		/// Moldova, Lei.
		/// </summary>
		[EnumMember]
		MDL,

		/// <summary>
		/// Mongolia, Tugriks.
		/// </summary>
		[EnumMember]
		MNT,

		/// <summary>
		/// Morocco, Dirhams.
		/// </summary>
		[EnumMember]
		MAD,

		/// <summary>
		/// Mozambique, Meticais.
		/// </summary>
		[EnumMember]
		MZM,

		/// <summary>
		/// Myanmar (Burma), Kyats.
		/// </summary>
		[EnumMember]
		MMK,

		/// <summary>
		/// Namibia, Dollars.
		/// </summary>
		[EnumMember]
		NAD,

		/// <summary>
		/// Nepal, Nepal Rupees.
		/// </summary>
		[EnumMember]
		NPR,

		/// <summary>
		/// Netherlands Antilles, Guilders (also called Florins).
		/// </summary>
		[EnumMember]
		ANG,

		/// <summary>
		/// New Zealand, Dollars.
		/// </summary>
		[EnumMember]
		NZD,

		/// <summary>
		/// Nicaragua, Gold Cordobas.
		/// </summary>
		[EnumMember]
		NIO,

		/// <summary>
		/// Nigeria, Nairas.
		/// </summary>
		[EnumMember]
		NGN,

		/// <summary>
		/// Korea (North), Won.
		/// </summary>
		[EnumMember]
		KPW,

		/// <summary>
		/// Norway, Krone.
		/// </summary>
		[EnumMember]
		NOK,

		/// <summary>
		/// Oman, Rials.
		/// </summary>
		[EnumMember]
		OMR,

		/// <summary>
		/// Comptoirs Francais du Pacifique Francs.
		/// </summary>
		[EnumMember]
		XPF,

		/// <summary>
		/// Pakistan, Rupees.
		/// </summary>
		[EnumMember]
		PKR,

		/// <summary>
		/// Palladium Ounces.
		/// </summary>
		[EnumMember]
		XPD,

		/// <summary>
		/// Panama, Balboa.
		/// </summary>
		[EnumMember]
		PAB,

		/// <summary>
		/// Papua New Guinea, Kina.
		/// </summary>
		[EnumMember]
		PGK,

		/// <summary>
		/// Paraguay, Guarani.
		/// </summary>
		[EnumMember]
		PYG,

		/// <summary>
		/// Peru, Nuevos Soles.
		/// </summary>
		[EnumMember]
		PEN,

		/// <summary>
		/// Philippines, Pesos.
		/// </summary>
		[EnumMember]
		PHP,

		/// <summary>
		/// Platinum, Ounces.
		/// </summary>
		[EnumMember]
		XPT,

		/// <summary>
		/// Poland, Zlotych.
		/// </summary>
		[EnumMember]
		PLN,

		/// <summary>
		/// Qatar, Rials.
		/// </summary>
		[EnumMember]
		QAR,

		/// <summary>
		/// Russia, Abkhazia, South Ossetia, Russian rouble.
		/// </summary>
		[EnumMember]
		RUB,

		/// <summary>
		/// Samoa, Tala.
		/// </summary>
		[EnumMember]
		WST,

		/// <summary>
		/// Sao Tome and Principe, Dobras.
		/// </summary>
		[EnumMember]
		STD,

		/// <summary>
		/// Saudi Arabia, Riyals.
		/// </summary>
		[EnumMember]
		SAR,

		/// <summary>
		/// Seychelles, Rupees.
		/// </summary>
		[EnumMember]
		SCR,

		/// <summary>
		/// Sierra Leone, Leones.
		/// </summary>
		[EnumMember]
		SLL,

		/// <summary>
		/// Silver, Ounces.
		/// </summary>
		[EnumMember]
		XAG,

		/// <summary>
		/// Singapore, Dollars.
		/// </summary>
		[EnumMember]
		SGD,

		/// <summary>
		/// Slovakia, Koruny.
		/// </summary>
		[EnumMember]
		SKK,

		/// <summary>
		/// Slovenia, Tolars.
		/// </summary>
		[EnumMember]
		SIT,

		/// <summary>
		/// Solomon Islands, Dollars.
		/// </summary>
		[EnumMember]
		SBD,

		/// <summary>
		/// Somalia, Shillings.
		/// </summary>
		[EnumMember]
		SOS,

		/// <summary>
		/// South Africa, Rand.
		/// </summary>
		[EnumMember]
		ZAR,

		/// <summary>
		/// Sri Lanka, Rupees.
		/// </summary>
		[EnumMember]
		LKR,

		/// <summary>
		/// Saint Helena, Pounds.
		/// </summary>
		[EnumMember]
		SHP,

		/// <summary>
		/// Sudan, Dinars.
		/// </summary>
		[EnumMember]
		SDD,

		/// <summary>
		/// Surinamese dollar.
		/// </summary>
		[EnumMember]
		SRD,

		/// <summary>
		/// Swaziland, Emalangeni.
		/// </summary>
		[EnumMember]
		SZL,

		/// <summary>
		/// Sweden, Kronor.
		/// </summary>
		[EnumMember]
		SEK,

		/// <summary>
		/// Switzerland, Francs.
		/// </summary>
		[EnumMember]
		CHF,

		/// <summary>
		/// Syria, Pounds.
		/// </summary>
		[EnumMember]
		SYP,

		/// <summary>
		/// Taiwan, New Dollars.
		/// </summary>
		[EnumMember]
		TWD,

		/// <summary>
		/// Tanzania, Shillings.
		/// </summary>
		[EnumMember]
		TZS,

		/// <summary>
		/// Thailand, Baht.
		/// </summary>
		[EnumMember]
		THB,

		/// <summary>
		/// Tonga, Pa'anga.
		/// </summary>
		[EnumMember]
		TOP,

		/// <summary>
		/// Trinidad and Tobago, Dollars.
		/// </summary>
		[EnumMember]
		TTD,

		/// <summary>
		/// Tunisia, Dinars.
		/// </summary>
		[EnumMember]
		TND,

		/// <summary>
		/// Turkey, Liras.
		/// </summary>
		[EnumMember]
		TRL,

		/// <summary>
		/// United States of America, Dollars.
		/// </summary>
		[EnumMember]
		USD,

		/// <summary>
		/// United Arab Emirates, Dirhams.
		/// </summary>
		[EnumMember]
		AED,

		/// <summary>
		/// Uganda, Shillings.
		/// </summary>
		[EnumMember]
		UGX,

		/// <summary>
		/// Ukraine, Hryvnia.
		/// </summary>
		[EnumMember]
		UAH,

		/// <summary>
		/// Uruguay, Pesos.
		/// </summary>
		[EnumMember]
		UYU,

		/// <summary>
		/// Vanuatu, Vatu.
		/// </summary>
		[EnumMember]
		VUV,

		/// <summary>
		/// Venezuela, Bolivares.
		/// </summary>
		[EnumMember]
		VEB,

		/// <summary>
		/// Viet Nam, Dong.
		/// </summary>
		[EnumMember]
		VND,

		/// <summary>
		/// Yemen, Rials.
		/// </summary>
		[EnumMember]
		YER,

		/// <summary>
		/// Serbian dinar.
		/// </summary>
		[EnumMember]
		CSD,

		/// <summary>
		/// Zambia, Kwacha.
		/// </summary>
		[EnumMember]
		ZMK,

		/// <summary>
		/// Zimbabwe, Zimbabwe Dollars.
		/// </summary>
		[EnumMember]
		ZWD,

		/// <summary>
		/// Turkey, Northern Cyprus, Turkish lira.
		/// </summary>
		[EnumMember]
		TRY,

		/// <summary>
		/// Ven.
		/// </summary>
		[EnumMember]
		XVN,

		/// <summary>
		/// Bitcoin.
		/// </summary>
		[EnumMember]
		BTC,

		/// <summary>
		/// United Kingdom, Pence sterling.
		/// </summary>
		[EnumMember]
		GBX,

		/// <summary>
		/// Ghana, Ghanaian Cedi.
		/// </summary>
		[EnumMember]
		GHS,

		/// <summary>
		/// China, offshore RMB.
		/// </summary>
		[EnumMember]
		CNT,

		/// <summary>
		/// Ethereum.
		/// </summary>
		[EnumMember]
		ETH,

		/// <summary>
		/// Litecoin.
		/// </summary>
		[EnumMember]
		LTC,
		
		/// <summary>
		/// Ethereum Classic.
		/// </summary>
		[EnumMember]
		ETC,
		
		/// <summary>
		/// Tether USD.
		/// </summary>
		[EnumMember]
		USDT,
		
		/// <summary>
		/// Zcash.
		/// </summary>
		[EnumMember]
		ZEC,
		
		/// <summary>
		/// Monero.
		/// </summary>
		[EnumMember]
		XMR,

		/// <summary>
		/// Cardano.
		/// </summary>
		[EnumMember]
		ADA,
		
		/// <summary>
		/// IOTA.
		/// </summary>
		[EnumMember]
		MIOTA,
		
		/// <summary>
		/// Ripple.
		/// </summary>
		[EnumMember]
		XRP,
		
		/// <summary>
		/// Dash.
		/// </summary>
		[EnumMember]
		DASH,
		
		/// <summary>
		/// EOS.
		/// </summary>
		[EnumMember]
		EOS,
		
		/// <summary>
		/// Santiment.
		/// </summary>
		[EnumMember]
		SAN,
		
		/// <summary>
		/// Omisego.
		/// </summary>
		[EnumMember]
		OMG,
		
		/// <summary>
		/// Bitcoin Cash.
		/// </summary>
		[EnumMember]
		BCH,
		
		/// <summary>
		/// Neo.
		/// </summary>
		[EnumMember]
		NEO,
		
		/// <summary>
		/// Metaverse.
		/// </summary>
		[EnumMember]
		ETP,
		
		/// <summary>
		/// Qtum.
		/// </summary>
		[EnumMember]
		QTUM,
		
		/// <summary>
		/// Aventus.
		/// </summary>
		[EnumMember]
		AVT,
		
		/// <summary>
		/// Eidoo.
		/// </summary>
		[EnumMember]
		EDO,
		
		/// <summary>
		/// Datacoin.
		/// </summary>
		[EnumMember]
		DTC,
		
		/// <summary>
		/// Bitcoin Gold.
		/// </summary>
		[EnumMember]
		BTG,
		
		/// <summary>
		/// QASH.
		/// </summary>
		[EnumMember]
		QASH,
		
		/// <summary>
		/// Yoyow.
		/// </summary>
		[EnumMember]
		YOYOW,
		
		/// <summary>
		/// Golem.
		/// </summary>
		[EnumMember]
		GNT,
		
		/// <summary>
		/// Status.
		/// </summary>
		[EnumMember]
		SNT,
		
		/// <summary>
		/// Tether EUR.
		/// </summary>
		[EnumMember]
		EURT,
		
		/// <summary>
		/// Basic Attention Token.
		/// </summary>
		[EnumMember]
		BAT,
		
		/// <summary>
		/// MNA.
		/// </summary>
		[EnumMember]
		MNA,
		
		/// <summary>
		/// FunFair.
		/// </summary>
		[EnumMember]
		FUN,
		
		/// <summary>
		/// ZRX.
		/// </summary>
		[EnumMember]
		ZRX,
		
		/// <summary>
		/// Time New Bank.
		/// </summary>
		[EnumMember]
		TNB,
		
		/// <summary>
		/// Sparks.
		/// </summary>
		[EnumMember]
		SPK,
		
		/// <summary>
		/// TRON.
		/// </summary>
		[EnumMember]
		TRX,
		
		/// <summary>
		/// Ripio Credit Network.
		/// </summary>
		[EnumMember]
		RCN,
		
		/// <summary>
		/// iExec.
		/// </summary>
		[EnumMember]
		RLC,
		
		/// <summary>
		/// AidCoin.
		/// </summary>
		[EnumMember]
		AID,
		
		/// <summary>
		/// SnowGem.
		/// </summary>
		[EnumMember]
		SNG,

		/// <summary>
		/// Augur.
		/// </summary>
		[EnumMember]
		REP,

		/// <summary>
		/// Aelf.
		/// </summary>
		[EnumMember]
		ELF,

		/// <summary>
		/// South Africa, Rand. The Rand is subdivided into 100 cents.
		/// </summary>
		[EnumMember]
		ZAC,

		/// <summary>
		/// Deutsche Mark.
		/// </summary>
		[EnumMember]
		DEM,

		/// <summary>
		/// Luxembourgish franc.
		/// </summary>
		[EnumMember]
		LUF,
// ReSharper restore InconsistentNaming
	}
}