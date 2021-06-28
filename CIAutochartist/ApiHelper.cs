#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion
using System.IO;
using System.Net.Http;

namespace NinjaTrader.Custom.Indicators.CIAutochartist
{
    public static class ApiHelper
    {
		public static string getURL(string username, string password, string request, string symbol, int utcOffset, string format, bool hidePassword)
		{
			//string url = "https://component.autochartist.com/va/resources/results/trumpet_results?broker_id=691&";
			string url = "https://chartviper.autochartist.com/aclite/CHARTVIPERXMLAPI?";
			url += "username=" + username + "&";
			if (hidePassword)
			{
				url += "password=*****&";
			}
			else
			{
				url += "password=" + password + "&";
			}
			url += "request=" + request + "&";
			url += "timezoneoffset=" + utcOffset + "&";
			url += "format=" + format + "&";
			//Work with respective symbols from Autochartist
			switch (symbol)
            {
				case "ES":
					url += "symbol=ES_cme&";
					url += "exchange=CME";
					break;
				case "GE": case "HE":
					url += "symbol=" + symbol + "&";
					url += "exchange=CME";
					break;

				case "TN": case "UB": case "ZF":
					if (symbol.Equals("ZF"))
					{
						url += "symbol=" + symbol + "%23&";
					}
					else
					{
						url += "symbol=" + symbol + "&";
					}
					url += "exchange=CBOT-GBX";
					break;

				case "ZC":
					url += "symbol=" + symbol + "%23";
					break;

				case "ZW":
					url += "symbol=W%23";
					break;

				case "BZ":
					url += "symbol=" + symbol + "&";
					url += "exchange=NYMEX-GBX";
					break;

				case "CL": case "NG":
					url += "symbol=" + symbol + "&";
					url += "exchange=NYMEX";
					break;

				case "FGBL":
					url += "symbol=BD";
					break;

				case "FGBM":
					url += "symbol=BL%23";
					break;

				case "FGBS":
					url += "symbol=EZ%23";
					break;

				default:
					url += "symbol=" + symbol;
					break;
			}

			return url;
		}

		
	}
}
