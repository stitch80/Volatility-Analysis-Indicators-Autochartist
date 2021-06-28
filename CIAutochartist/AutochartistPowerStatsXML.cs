using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.Indicators.CIAutochartist
{
    public class Day
    {
        public double low { get; set; }
        public double high { get; set; }
    }
    public class Movementperday
    {
        public string symbol { get; set; }
        public string symbolcode { get; set; }
        public string exchange { get; set; }
        public Day sunday { get; set; }
        public Day monday { get; set; }
        public Day tuesday { get; set; }
        public Day wednesday { get; set; }
        public Day thursday { get; set; }
        public Day friday { get; set; }
    }

    public class Hour
    {
        public double low { get; set; }
        public double high { get; set; }
    }

    public class Movementperhour
    {
        public string symbol { get; set; }
        public string symbolcode { get; set; }
        public string exchange { get; set; }
        public Hour hour0 { get; set; }
        public Hour hour1 { get; set; }
        public Hour hour2 { get; set; }
        public Hour hour3 { get; set; }
        public Hour hour4 { get; set; }
        public Hour hour5 { get; set; }
        public Hour hour6 { get; set; }
        public Hour hour7 { get; set; }
        public Hour hour8 { get; set; }
        public Hour hour9 { get; set; }
        public Hour hour10 { get; set; }
        public Hour hour11 { get; set; }
        public Hour hour12 { get; set; }
        public Hour hour13 { get; set; }
        public Hour hour14 { get; set; }
        public Hour hour15 { get; set; }
        public Hour hour16 { get; set; }
        public Hour hour17 { get; set; }
        public Hour hour18 { get; set; }
        public Hour hour19 { get; set; }
        public Hour hour20 { get; set; }
        public Hour hour21 { get; set; }
        public Hour hour22 { get; set; }
        public Hour hour23 { get; set; }
    }

    public class RangeMinutes
    {
        public double low { get; set; }
        public double high { get; set; }
    }

    public class Pricerangeforecast
    {
        public string symbol { get; set; }
        public string symbolcode { get; set; }
        public string exchange { get; set; }
        public RangeMinutes min15 { get; set; }
        public RangeMinutes min30 { get; set; }
        public RangeMinutes min60 { get; set; }
        public RangeMinutes min240 { get; set; }
        public RangeMinutes min1440 { get; set; }

    }

    public class PowerstatsResults
    {
        public Movementperday Movementperday { get; set; }
        public Movementperhour Movementperhour { get; set; }
        public Pricerangeforecast Pricerangeforecast { get; set; }

    }
    
    public class ErrorTag
    {
        public int id { get; set; }
        public string message { get; set; }
    }

    public class Authentication
    {
        public string sessionid { get; set; }
        public string errormessage { get; set; }
    }
    public class AutochartistPowerStatsXML
    {
        public Authentication Authentication { get; set; }
        public PowerstatsResults PowerstatsResults { get; set; }
        public ErrorTag Error { get; set; }
    }
}
