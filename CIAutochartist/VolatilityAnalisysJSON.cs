using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using NinjaTrader.CQG.ProtoBuf;

namespace NinjaTrader.Custom.Indicators.CIAutochartist
{
    public class VolatilityAnalisysJSON
    {
        public DateTimeOffset Date { get; set; }
        public ItemsArray[] Items { get; set; }
        public string[] ReturnSymbols { get; set; }

        public ItemsArray GetDataItem(string instrument)
        {
            foreach (var ia in Items)
            {
                if (ia.getInstrument().Equals(instrument))
                    return ia;
            }
            return null;
        }

        public bool isInstrumentExists(string instrument)
        {
            foreach (var symbol in ReturnSymbols)
            {
                if (symbol.Equals(instrument))
                {
                    return true;
                }
            }
            return false;
        }
    }
}


