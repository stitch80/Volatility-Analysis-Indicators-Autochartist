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
using NinjaTrader.Custom.Indicators.CIAutochartist;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Timers;
using System.IO;
using SharpDX;
using SharpDX.Direct2D1;


//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class CI_PriceMovementPerHour_PerDay : Indicator
	{
		private string version = "1.0";
		private AutochartistPowerStatsXML resultObject;
		private HttpClient ApiClient;
		string url;
		private Timer apiCallTimer;
		private bool isInstrumentExists;
		private string errorMessage;
		double hourMax = double.NegativeInfinity;
		double hourMin = double.PositiveInfinity;
		double dayMax = double.NegativeInfinity;
		double dayMin = double.PositiveInfinity;
		int startIndexHour = 0;
		int startIndexDay = 0;
		private bool isNewDayStarted = false;



		//public CI_PriceMovementPerHour_PerDay()
		//{
		//	VendorLicense("CrystalIndicators", "AutoChartistVolatilityAnalysis", "www.crystalindicators.com",
		//		"info@crystalindicators.com", null);
		//}

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Price Movement Per Hour/Per Day";
				Name										= "CI Price Movement Per Hour/Per Day";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= true;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;

				IsAutoScale									= false;


				UserName									= string.Empty;
				Password									= string.Empty;

				ShowDailyRanges								= false;

				AddPlot(new Stroke(Brushes.DarkOliveGreen, 2), PlotStyle.Line, "HourHighMax");
				AddPlot(new Stroke(Brushes.DarkGreen, 2), PlotStyle.Line, "HourHighAvg");
				AddPlot(new Stroke(Brushes.Green, DashStyleHelper.Dash, 2), PlotStyle.Line, "HourHighMin");
				AddPlot(new Stroke(Brushes.Firebrick, 2), PlotStyle.Line, "HourLowMax");
				AddPlot(new Stroke(Brushes.OrangeRed, 2), PlotStyle.Line, "HourLowAvg");
				AddPlot(new Stroke(Brushes.Red, DashStyleHelper.Dash, 2), PlotStyle.Line, "HourLowMin");

				AddPlot(new Stroke(Brushes.Magenta, 2), PlotStyle.Line, "DayHighMax");
				AddPlot(new Stroke(Brushes.DarkMagenta, 2), PlotStyle.Line, "DayHighAvg");
				AddPlot(new Stroke(Brushes.Purple, DashStyleHelper.Dash, 2), PlotStyle.Line, "DayHighMin");
				AddPlot(new Stroke(Brushes.Cyan, 2), PlotStyle.Line, "DayLowMax");
				AddPlot(new Stroke(Brushes.DarkCyan, 2), PlotStyle.Line, "DayLowAvg");
				AddPlot(new Stroke(Brushes.Blue, DashStyleHelper.Dash, 2), PlotStyle.Line, "DayLowMin");

			}
			else if (State == State.Configure)
			{
				Print("CI Price Movement Per Hour/Per Day Indicator - version " + version);
				AddDataSeries(BarsPeriodType.Minute, 1440);
				initWebClient();
				LoadXML(null);
			}
            else if (State == State.DataLoaded)
            {
				apiCallTimer = new Timer(60000);
                apiCallTimer.Elapsed += LoadXMLEventProcessor;
                apiCallTimer.AutoReset = true;
                apiCallTimer.Enabled = true;
                //Print(Instrument.MasterInstrument.Name);
                //Print(Instrument.Exchange);
            }
            else if (State == State.Terminated)
            {
				shutdown();
			}
        }

		public override string DisplayName
		{
			get
			{
				return Name + "(" + UserName + ", v. " + version + ")";
			}
		}

		protected override void OnBarUpdate()
		{
			if (resultObject == null)
            {
				System.Threading.Thread.Sleep(300);
				return;
            }

			if (CurrentBars[1] < 1) return;

            if (isInstrumentExists)
            {
                calculateHPMRs();
				if (ShowDailyRanges)
					calculateDPMRs();
            }
			else
            {
				shutdown();
			}


        }


        protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
        {
			base.OnRender(chartControl, chartScale);

			if (!isInstrumentExists)
			{
				showErrorMessage(chartControl, chartScale);
			}


		}


        #region Draw Lines
        private void showErrorMessage(ChartControl chartControl, ChartScale chartScale)
		{
			Draw.TextFixed(this, "ErrorMessage", errorMessage, TextPosition.Center);
		}

		private void calculateHPMRs()
		{
            if (BarsPeriods[0].BarsPeriodType == BarsPeriodType.Minute &&
				BarsPeriods[0].Value <= 15 &&
				(60 % BarsPeriods[0].Value == 0))
			{

				//==========================================
				TimeZoneInfo baseTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
				TimeZoneInfo curTimeZone = Core.Globals.GeneralOptions.TimeZoneInfo;
				DateTime curzoneTime = BarsArray[0].GetTime(CurrentBars[0]);
				DateTime curBaseTime = TimeZoneInfo.ConvertTime(BarsArray[0].GetTime(CurrentBars[0]),
					curTimeZone, baseTimeZone);
				DateTime prevBarBaseTime = TimeZoneInfo.ConvertTime(BarsArray[0].GetTime(CurrentBars[0] - 1),
					curTimeZone, baseTimeZone);

				int utcOffset = Core.Globals.GeneralOptions.TimeZoneInfo.IsDaylightSavingTime(BarsArray[0].GetTime(CurrentBars[0])) ?
				Core.Globals.GeneralOptions.TimeZoneInfo.BaseUtcOffset.Hours + 1
				: Core.Globals.GeneralOptions.TimeZoneInfo.BaseUtcOffset.Hours;
				int utcBaseOffset = baseTimeZone.IsDaylightSavingTime(curBaseTime) ?
					baseTimeZone.BaseUtcOffset.Hours + 1
					: baseTimeZone.BaseUtcOffset.Hours;

				int shiftNumber = utcOffset - utcBaseOffset;

                //Print("utcOffset - " + utcOffset + " PC time - " + DateTime.Now);
                //Print("utcOffset - " + utcOffset + " utcBaseOffset - " + utcBaseOffset
                //    + " shiftNumber - " + shiftNumber + " " + BarsArray[0].GetTime(CurrentBars[0])
                //    + " " + curBaseTime);

                //==========================================


                //Current hour number
                DateTime curTime = BarsArray[0].GetTime(CurrentBars[0]);
                DateTime prevBarTime = BarsArray[0].GetTime(CurrentBars[0] - 1);
				//int curHour;
				int curHour = curTime.Hour - shiftNumber >= 0 ? (curTime.Hour - shiftNumber) % 24 
					: (curTime.Hour - shiftNumber + 24) % 24;
				if (curTime.Minute == 0)
					curHour = curHour == 0 ? 23 : curHour - 1;

				

				//if (curTime.Minute > 0)
    //                //curHour = curTime.Hour;
    //                curHour = curHour;
    //            else
    //                //curHour = curTime.Hour == 0 ? 23 : curTime.Hour - 1;
    //                curHour = curHour == 0 ? 23 : curHour - 1;

                //int prevBarHour;
                int prevBarHour = prevBarTime.Hour - shiftNumber >= 0 ? (prevBarTime.Hour - shiftNumber) % 24
					: (prevBarTime.Hour - shiftNumber + 24) % 24;
				if (prevBarTime.Minute == 0)
					prevBarHour = prevBarHour == 0 ? 23 : prevBarHour - 1;

				//Print("curTime.Hour - " + curTime.Hour);
				//Print("curHour - " + curHour);
				//Print("prevBarTime.Hour - " + prevBarTime.Hour);
				//Print("prevBarHour - " + prevBarHour);


				//if (prevBarTime.Minute > 0)
				//                //prevBarHour = prevBarTime.Hour;
				//                prevBarHour = prevBarHour;
				//            else
				//                //prevBarHour = prevBarTime.Hour == 0 ? 23 : prevBarTime.Hour - 1;
				//	prevBarHour = prevBarHour == 0 ? 23 : prevBarHour - 1;

				//Current hour max and min
				if (curTime.Minute == BarsPeriods[0].Value || BarsArray[0].IsFirstBarOfSession
					|| curHour != prevBarHour)
				{
					hourMax = BarsArray[0].GetHigh(CurrentBars[0]);
					hourMin = BarsArray[0].GetLow(CurrentBars[0]);
					startIndexHour = CurrentBars[0];
				}
				else
				{
					hourMax = hourMax > BarsArray[0].GetHigh(CurrentBars[0]) ? hourMax : BarsArray[0].GetHigh(CurrentBars[0]);
					hourMin = hourMin < BarsArray[0].GetLow(CurrentBars[0]) ? hourMin : BarsArray[0].GetLow(CurrentBars[0]);
				}


				//Current hour max and min volatility numbers

				string curHourPropName = "hour" + curHour;
				Movementperhour movementperhour = resultObject.PowerstatsResults.Movementperhour;
				Hour hour = (Hour)movementperhour.GetType().GetProperty(curHourPropName).GetValue(movementperhour);
				double curHourHigh = hour.high;
				double curHourLow = hour.low;
				double curHourAvg = (curHourHigh + curHourLow) / 2;

                //Print(Time[0] + " curHourPropName: " + curHourPropName + " hourMax:" + hourMax + " hourMin:" + hourMin
                //    + " curHourHigh:" + curHourHigh + " curHourLow:" + curHourLow + " curHourAvg:" + curHourAvg);

                //Draw Hour levels lines
                drawLevels(hourMax, hourMin, curHourHigh, curHourLow, curHourAvg, true);

			}
			//Print(Time[0]);
		}

		private void calculateDPMRs()
		{
			if (BarsPeriods[0].BarsPeriodType == BarsPeriodType.Minute && BarsPeriods[0].Value <= 240)
			{
				//Current day name
				TimeZoneInfo baseTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
				TimeZoneInfo curTimeZone = Core.Globals.GeneralOptions.TimeZoneInfo;
				DateTime curBaseTime = TimeZoneInfo.ConvertTime(BarsArray[0].GetTime(CurrentBars[0]),
					curTimeZone, baseTimeZone);
				DateTime prevBarBaseTime = TimeZoneInfo.ConvertTime(BarsArray[0].GetTime(CurrentBars[0] - 1),
					curTimeZone, baseTimeZone);
				//Print("baseTimeZone - " + baseTimeZone);
				//Print("curTimeZone - " + curTimeZone);
				//Print("BarsArray[0].GetTime(CurrentBars[0]) - " + BarsArray[0].GetTime(CurrentBars[0]));
				//Print("BarsArray[0].GetTime(CurrentBars[0] - 1)" + BarsArray[0].GetTime(CurrentBars[0] - 1));
				//Print("curBaseTime - " + curBaseTime);
				//Print("prevBarBaseTime - " + prevBarBaseTime);

				string curBaseDay;
				if (curBaseTime.Minute == 0 && curBaseTime.Hour == 0)
					curBaseDay = (curBaseTime - new TimeSpan(1, 0, 0, 0)).DayOfWeek.ToString().ToLower();
				else
					curBaseDay = curBaseTime.DayOfWeek.ToString().ToLower();

				string prevBarBaseDay;
				if (prevBarBaseTime.Minute == 0 && prevBarBaseTime.Hour == 0)
					prevBarBaseDay = (prevBarBaseTime - new TimeSpan(1, 0, 0, 0)).DayOfWeek.ToString().ToLower();
				else
					prevBarBaseDay = prevBarBaseTime.DayOfWeek.ToString().ToLower();
				//Print("curBaseDay - " + curBaseDay);
				//Print("prevBarBaseDay - " + prevBarBaseDay);
				
				//Current day max and min
				if (!isNewDayStarted)
                {
					dayMax = BarsArray[0].GetHigh(CurrentBars[0]);
					dayMin = BarsArray[0].GetLow(CurrentBars[0]);
					startIndexDay = CurrentBars[0];
					isNewDayStarted = true;
				}
				else if ((curBaseTime.Minute == BarsPeriods[0].Value && curBaseTime.Hour == 0)
					|| !curBaseDay.Equals(prevBarBaseDay))
				{
					dayMax = BarsArray[0].GetHigh(CurrentBars[0]);
					dayMin = BarsArray[0].GetLow(CurrentBars[0]);
					startIndexDay = CurrentBars[0];
					isNewDayStarted = true;
                }
                else
                {
					dayMax = dayMax > BarsArray[0].GetHigh(CurrentBars[0]) ? dayMax : BarsArray[0].GetHigh(CurrentBars[0]);
					dayMin = dayMin < BarsArray[0].GetLow(CurrentBars[0]) ? dayMin : BarsArray[0].GetLow(CurrentBars[0]);

				}
				
				//Current day max and min volatility numbers
				Movementperday movementperday = resultObject.PowerstatsResults.Movementperday;
				Day day = (Day)movementperday.GetType().GetProperty(curBaseDay).GetValue(movementperday);
				double curDayHigh = day.high;
				double curDayLow = day.low;
				double curDayAvg = (curDayHigh + curDayLow) / 2;

				//Draw Day levels lines
				drawLevels(dayMax, dayMin, curDayHigh, curDayLow, curDayAvg, false);
			}
		}

		private void drawLevels(double periodMax, double periodMin,
			double curPeriodHigh, double curPeriodLow, double curPeriodAvg, bool isHour)
        {
			if (isHour)
            {
				for (int i = CurrentBars[0] - startIndexHour; i >= 0; i--)
				{
					HourHighMax[i] = periodMin + curPeriodHigh;
					HourHighAvg[i] = periodMin + curPeriodAvg;
					HourHighMin[i] = periodMin + curPeriodLow;
					HourLowMax[i] = periodMax - curPeriodHigh;
					HourLowAvg[i] = periodMax - curPeriodAvg;
					HourLowMin[i] = periodMax - curPeriodLow;
				}

				Draw.Region(this, "HourHighArea", CurrentBars[0], 0, HourHighMax, HourHighAvg, Brushes.DarkOliveGreen, Brushes.DarkOliveGreen, 50);
				Draw.Region(this, "HourLowArea", CurrentBars[0], 0, HourLowMax, HourLowAvg, Brushes.Firebrick, Brushes.Firebrick, 50);
			}
			else
            {
				for (int i = CurrentBars[0] - startIndexDay; i >= 0; i--)
				{
					DayHighMax[i] = periodMin + curPeriodHigh;
					DayHighAvg[i] = periodMin + curPeriodAvg;
					DayHighMin[i] = periodMin + curPeriodLow;
					DayLowMax[i] = periodMax - curPeriodHigh;
					DayLowAvg[i] = periodMax - curPeriodAvg;
					DayLowMin[i] = periodMax - curPeriodLow;
				}

				Draw.Region(this, "DayHighArea", CurrentBars[0], 0, DayHighMax, DayHighAvg, Brushes.Magenta, Brushes.Magenta, 20);
				Draw.Region(this, "DayLowArea", CurrentBars[0], 0, DayLowMax, DayLowAvg, Brushes.Cyan, Brushes.Cyan, 20);

			}

		}
		#endregion

		
        #region XML Processing
        private void LoadXMLEventProcessor(object myObject, EventArgs args)
		{
			if (isInstrumentExists)
			{
				TriggerCustomEvent(LoadXML, EventArgs.Empty);
			}
		}

		//Initialize http client and url
		private void initWebClient()
		{
			Log("CI_PriceMovementPerHour_PerDay: WebClient initialization", LogLevel.Information);
			ApiClient = new HttpClient();
			ApiClient.DefaultRequestHeaders.Accept.Clear();
			ApiClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
			string symbol = Instrument.MasterInstrument.Name;

			TimeZoneInfo baseTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
			TimeZoneInfo curTimeZone = Core.Globals.GeneralOptions.TimeZoneInfo;

			int utcOffset = Core.Globals.GeneralOptions.TimeZoneInfo.IsDaylightSavingTime(DateTime.Now) ?
                Core.Globals.GeneralOptions.TimeZoneInfo.BaseUtcOffset.Hours + 1
                : Core.Globals.GeneralOptions.TimeZoneInfo.BaseUtcOffset.Hours;
            int utcBaseOffset = baseTimeZone.IsDaylightSavingTime(DateTime.Now) ?
                baseTimeZone.BaseUtcOffset.Hours + 1
                : baseTimeZone.BaseUtcOffset.Hours;

            //Print("utcOffset - " + utcOffset + " PC time - " + DateTime.Now);
			//Print("utcOffset - " + utcOffset + " PC time - " + DateTime.Now + " utcBaseOffset - " + utcBaseOffset);


			//url = ApiHelper.getURL(UserName, Password, "powerstats", symbol, utcOffset, "xml", false);
			url = ApiHelper.getURL(UserName, Password, "powerstats", symbol, utcBaseOffset, "xml", false);

            //Log("CI_PriceMovementPerHour_PerDay: Url: " + ApiHelper.getURL(UserName, Password, "powerstats", symbol, utcOffset, "xml", true), LogLevel.Information);
            Log("CI_PriceMovementPerHour_PerDay: Url: " + ApiHelper.getURL(UserName, Password, "powerstats", symbol, utcBaseOffset, "xml", true), LogLevel.Information);
            //Print("CI_PriceMovementPerHour_PerDay: Url: " + ApiHelper.getURL(UserName, Password, "powerstats", symbol, utcOffset, "xml", true));
        }

		private void shutdown()
		{
			if (apiCallTimer != null)
			{
				apiCallTimer.Enabled = false;
				apiCallTimer.Elapsed -= LoadXMLEventProcessor;
				apiCallTimer = null;
				Print("CI_PriceMovementPerHour_PerDay: Timer disposed");
			}
			if (ApiClient != null)
			{
				ApiClient.CancelPendingRequests();
				ApiClient.Dispose();
				ApiClient = null;
				Print("CI_PriceMovementPerHour_PerDay: HttpClient disposed");
			}
		}

		//Fill the object with Volatility Analysis Data
		private async void LoadXML(object myObject)
		{
			Print("CI_PriceMovementPerHour_PerDay: LoadXML started");
			try
			{
				using (HttpResponseMessage response = await ApiClient.GetAsync(url))
				{
					if (response.IsSuccessStatusCode)
					{
						string resultString = await response.Content.ReadAsStringAsync();

						XmlSerializer xmlSerializer =
								new XmlSerializer(typeof(AutochartistPowerStatsXML), new XmlRootAttribute("AutochartistAPI"));
						StringReader stringReader = new StringReader(resultString);
						resultObject = (AutochartistPowerStatsXML)xmlSerializer.Deserialize(stringReader);
						Print("CI_PriceMovementPerHour_PerDay: ResultObject created");
						if (resultObject.Authentication.sessionid.Equals("0"))
						{
							isInstrumentExists = false;
							errorMessage = resultObject.Authentication.errormessage;
							Log("CI_PriceMovementPerHour_PerDay: " + errorMessage, LogLevel.Alert);
							//MessageBox.Show(resultObject.Authentication.errormessage);
						}
						else if (resultObject.Error != null)
						{
							isInstrumentExists = false;
							errorMessage = resultObject.Error.message;
							Log("CI_PriceMovementPerHour_PerDay: " + errorMessage, LogLevel.Alert);
							//MessageBox.Show(resultObject.Error.message);
						}
						else
						{
							isInstrumentExists = true;
						}
					}
					else
					{
						throw new Exception(response.ReasonPhrase);
					}
				}
			}
			catch (Exception e)
			{
				Log("CI_PriceMovementPerHour_PerDay: " + e.Message, LogLevel.Alert);
				//MessageBox.Show(e.Message);
			}
		}

		#endregion

		#region Properties
		[NinjaScriptProperty]
		[Display(Name="UserName", Order=1, GroupName="Parameters")]
		public string UserName
		{ get; set; }

		[NinjaScriptProperty]
		[PasswordPropertyText(true)]
		[Display(Name="Password", Order=2, GroupName="Parameters")]
		public string Password
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name = "ShowDailyRanges", Order = 3, GroupName = "Parameters")]
		public bool ShowDailyRanges
		{ get; set; }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> HourHighMax
		{
			get { return Values[0]; }
		}


		[Browsable(false)]
		[XmlIgnore]
		public Series<double> HourHighAvg
		{
			get { return Values[1]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> HourHighMin
		{
			get { return Values[2]; }
		}


		[Browsable(false)]
		[XmlIgnore]
		public Series<double> HourLowMax
		{
			get { return Values[3]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> HourLowAvg
		{
			get { return Values[4]; }
		}


		[Browsable(false)]
		[XmlIgnore]
		public Series<double> HourLowMin
		{
			get { return Values[5]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> DayHighMax
		{
			get { return Values[6]; }
		}


		[Browsable(false)]
		[XmlIgnore]
		public Series<double> DayHighAvg
		{
			get { return Values[7]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> DayHighMin
		{
			get { return Values[8]; }
		}


		[Browsable(false)]
		[XmlIgnore]
		public Series<double> DayLowMax
		{
			get { return Values[9]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> DayLowAvg
		{
			get { return Values[10]; }
		}


		[Browsable(false)]
		[XmlIgnore]
		public Series<double> DayLowMin
		{
			get { return Values[11]; }
		}
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private CI_PriceMovementPerHour_PerDay[] cacheCI_PriceMovementPerHour_PerDay;
		public CI_PriceMovementPerHour_PerDay CI_PriceMovementPerHour_PerDay(string userName, string password, bool showDailyRanges)
		{
			return CI_PriceMovementPerHour_PerDay(Input, userName, password, showDailyRanges);
		}

		public CI_PriceMovementPerHour_PerDay CI_PriceMovementPerHour_PerDay(ISeries<double> input, string userName, string password, bool showDailyRanges)
		{
			if (cacheCI_PriceMovementPerHour_PerDay != null)
				for (int idx = 0; idx < cacheCI_PriceMovementPerHour_PerDay.Length; idx++)
					if (cacheCI_PriceMovementPerHour_PerDay[idx] != null && cacheCI_PriceMovementPerHour_PerDay[idx].UserName == userName && cacheCI_PriceMovementPerHour_PerDay[idx].Password == password && cacheCI_PriceMovementPerHour_PerDay[idx].ShowDailyRanges == showDailyRanges && cacheCI_PriceMovementPerHour_PerDay[idx].EqualsInput(input))
						return cacheCI_PriceMovementPerHour_PerDay[idx];
			return CacheIndicator<CI_PriceMovementPerHour_PerDay>(new CI_PriceMovementPerHour_PerDay(){ UserName = userName, Password = password, ShowDailyRanges = showDailyRanges }, input, ref cacheCI_PriceMovementPerHour_PerDay);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.CI_PriceMovementPerHour_PerDay CI_PriceMovementPerHour_PerDay(string userName, string password, bool showDailyRanges)
		{
			return indicator.CI_PriceMovementPerHour_PerDay(Input, userName, password, showDailyRanges);
		}

		public Indicators.CI_PriceMovementPerHour_PerDay CI_PriceMovementPerHour_PerDay(ISeries<double> input , string userName, string password, bool showDailyRanges)
		{
			return indicator.CI_PriceMovementPerHour_PerDay(input, userName, password, showDailyRanges);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.CI_PriceMovementPerHour_PerDay CI_PriceMovementPerHour_PerDay(string userName, string password, bool showDailyRanges)
		{
			return indicator.CI_PriceMovementPerHour_PerDay(Input, userName, password, showDailyRanges);
		}

		public Indicators.CI_PriceMovementPerHour_PerDay CI_PriceMovementPerHour_PerDay(ISeries<double> input , string userName, string password, bool showDailyRanges)
		{
			return indicator.CI_PriceMovementPerHour_PerDay(input, userName, password, showDailyRanges);
		}
	}
}

#endregion
