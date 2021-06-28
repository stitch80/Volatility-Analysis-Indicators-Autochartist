namespace NinjaTrader.Custom.Indicators.CIAutochartist
{
    public class ItemsArray
    {
        //public bool New { get; set; }
        public DataItem Data { get; set; }

        public string getInstrument()
        {
            return Data.Symbol;
        }

        public double getEdgeData(string edge, int timeframe)
        {
            string tf = edge + "_" + timeframe;
            return (double)Data.GetType().GetProperty(tf).GetValue(Data);
        }
    }
}


