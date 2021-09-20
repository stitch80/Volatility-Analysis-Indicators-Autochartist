using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

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

        [XmlElement(ElementName = "hour0.0")]
        public Hour hour0 { get; set; }
        [XmlElement(ElementName = "hour1.0")]
        public Hour hour1 { get; set; }
        [XmlElement(ElementName = "hour2.0")]
        public Hour hour2 { get; set; }
        [XmlElement(ElementName = "hour3.0")]
        public Hour hour3 { get; set; }
        [XmlElement(ElementName = "hour4.0")]
        public Hour hour4 { get; set; }
        [XmlElement(ElementName = "hour5.0")]
        public Hour hour5 { get; set; }
        [XmlElement(ElementName = "hour6.0")]
        public Hour hour6 { get; set; }
        [XmlElement(ElementName = "hour7.0")]
        public Hour hour7 { get; set; }
        [XmlElement(ElementName = "hour8.0")]
        public Hour hour8 { get; set; }
        [XmlElement(ElementName = "hour9.0")]
        public Hour hour9 { get; set; }
        [XmlElement(ElementName = "hour10.0")]
        public Hour hour10 { get; set; }
        [XmlElement(ElementName = "hour11.0")]
        public Hour hour11 { get; set; }
        [XmlElement(ElementName = "hour12.0")]
        public Hour hour12 { get; set; }
        [XmlElement(ElementName = "hour13.0")]
        public Hour hour13 { get; set; }
        [XmlElement(ElementName = "hour14.0")]
        public Hour hour14 { get; set; }
        [XmlElement(ElementName = "hour15.0")]
        public Hour hour15 { get; set; }
        [XmlElement(ElementName = "hour16.0")]
        public Hour hour16 { get; set; }
        [XmlElement(ElementName = "hour17.0")]
        public Hour hour17 { get; set; }
        [XmlElement(ElementName = "hour18.0")]
        public Hour hour18 { get; set; }
        [XmlElement(ElementName = "hour19.0")]
        public Hour hour19 { get; set; }
        [XmlElement(ElementName = "hour20.0")]
        public Hour hour20 { get; set; }
        [XmlElement(ElementName = "hour21.0")]
        public Hour hour21 { get; set; }
        [XmlElement(ElementName = "hour22.0")]
        public Hour hour22 { get; set; }
        [XmlElement(ElementName = "hour23.0")]
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
