using System.Reflection;
using Newtonsoft.Json;
using System;
namespace SciTrader.Model
{
    /*
    #pragma once
    #include <string>
    namespace DarkHorse {
        struct Symbol {
            /// <summary>
            /// 심볼 코드
            /// </summary>
            std::string symbol_code;
            /// <summary>
            /// 심볼 풀코드
            /// </summary>
            std::string full_code;
            /// <summary>
            /// 영문 이름
            /// </summary>
            std::string name_en;
            /// <summary>
            /// 한글 이름
            /// </summary>
            std::string name_kr;
            /// <summary>
            /// 잔존 일수
            /// </summary>
            int remain_days;
            /// <summary>
            /// 최종 거래일
            /// </summary>
            std::string last_trade_day;
            /// <summary>
            /// 최상위 1호가 
            /// </summary>
            std::string high_limit_price;
            /// <summary>
            /// 최하위 1호가
            /// </summary>
            std::string low_limit_price;
            /// <summary>
            /// 이전일 종가
            /// </summary>
            std::string preday_close;
            /// <summary>
            /// 기준가
            /// </summary>
            std::string standard_price;
            /// <summary>
            /// 행사가
            /// </summary>
            std::string strike;
            /// <summary>
            /// 0 : future, 1 : atm , 2 : itm, 3 : otm
            /// </summary>
            int atm_type;
            /// <summary>
            /// 1 : 최근원물, 선물 스프레드, 2 : 2째월물, 3등등.
            /// </summary>
            int recent_month;
            /// <summary>
            /// 만기일
            /// </summary>
            std::string expire_day;
            /// <summary>
            /// 아이디
            /// </summary>
            int id{ 0 };
            /// <summary>
            /// 승수
            /// </summary>
            int seung_su{ 250000 };
            /// <summary>
            /// 소수점 자리수
            /// </summary>
            int decimal{ 2 };
            /// <summary>
            /// 계약 크기
            /// </summary>
            double contract_size{ 0.05 };
            /// <summary>
            /// 틱가치
            /// </summary>
            double tick_value{ 12500 };
            /// <summary>
            /// 틱크기
            /// </summary>
            double tick_size{ 0.05 };
            /// <summary>
            /// 마켓 이름
            /// </summary>
            std::string market_name;
            /// <summary>
            /// 제품 코드
            /// </summary>
            std::string product_code;
            /// <summary>
            /// 일 전체 거래량 
            /// </summary>
            int total_volume{ 0 };
            /// <summary>
            /// 이전일 거래량
            /// </summary>
            int preday_volume{ 0 };
            /// <summary>
            /// 잔고
            /// </summary>
            std::string deposit;
            /// <summary>
            /// 시작 시간
            /// </summary>
            std::string start_time;
            /// <summary>
            /// 종료 시간
            /// </summary>
            std::string end_time;
            /// <summary>
            /// 상승/하락률
            /// </summary>
            std::string preday_updown_rate;
            /// <summary>
            /// 통화표시
            /// </summary>
            std::string currency;
            /// <summary>
            /// 거래소
            /// </summary>
            std::string exchange;
        };
    }
    */
    [Obfuscation(Feature = "renaming", ApplyToMembers = true)]
    internal class Symbol
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("symbol_code")]
        public string SymbolCode { get; set; }

        [JsonProperty("name_en")]
        public string Name { get; set; }

        [JsonProperty("last_trade_day")]
        public string LastTradeDay { get; set; }

        [JsonProperty("strike")]
        public string Strike { get; set; }

        [JsonProperty("expire_day")]
        public string ExpiryDay { get; set; }

        [JsonProperty("seung_su")]
        public string SeungSu { get; set; }

        [JsonProperty("decimal")]
        public string Decimal { get; set; }

        [JsonProperty("contract_size")]
        public string ContractSize { get; set; }

        [JsonProperty("tick_value")]
        public string TickValue { get; set; }

        [JsonProperty("tick_size")]
        public string tickSize { get; set; }

        //[JsonProperty("market_name")]
        //public string MarketName { get; set; }

        [JsonProperty("product_code")]
        public string ProductCode { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("exchange")]
        public string Exchange { get; set; }
    }
}