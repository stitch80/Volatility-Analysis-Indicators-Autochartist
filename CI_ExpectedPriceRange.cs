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
using NinjaTrader.Custom.Indicators.CIAutochartist;
using System.Net.Http;
using System.Net.Http.Headers;
//using TimeZoneConverter;
//using System.Globalization;
//using System.Security.Principal;
using SharpDX.Direct2D1;
using SharpDX;
using System.Timers;
//using System.Threading;

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class CI_ExpectedPriceRange : Indicator
	{
		private string version = "1.0";
		private AutochartistPowerStatsXML resultObject;
		//private HttpClient ApiClient = new HttpClient();
		private HttpClient ApiClient;
		string url;
		private Timer apiCallTimer;
		private bool isInstrumentExists;
		//private bool isInstrumentsChecked;
		private string errorMessage;
		//private CancellationTokenSource cts = new CancellationTokenSource();

		//public CI_ExpectedPriceRange()
		//{
		//	VendorLicense("CrystalIndicators", "AutoChartistVolatilityAnalysis", "www.crystalindicators.com",
		//		"info@crystalindicators.com", null);
		//}

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Price Range Forecast";
				Name										= "CI Expected Price Range";
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

				UserName = string.Empty;
				Password = string.Empty;

			}
			else if (State == State.Configure)
			{
				Print("CI Expected Price Range Indicator - version " + version);
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
			else if(State == State.Terminated)
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
			
		}

		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			base.OnRender(chartControl, chartScale);

			if (resultObject == null)
				return;

			if (isInstrumentExists)
			{
				calculateCoordinatesForRendering(chartControl, chartScale);
			}
			else
			{
				showErrorMessage(chartControl, chartScale);
				shutdown();
			}
		}

        #region Draw Lines
		private void showErrorMessage(ChartControl chartControl, ChartScale chartScale)
		{
			Draw.TextFixed(this, "ErrorMessage", errorMessage, TextPosition.Center);
		}
        private void renderLines(ChartControl chartControl, ChartScale chartScale, int timeframe, float startX, float endX)
		{
			string instrumentName = Bars.Instrument.MasterInstrument.Name;

			//double high = resultObject.GetDataItem(instrumentName).getEdgeData("high", timeframe);
			//double low = resultObject.GetDataItem(instrumentName).getEdgeData("low", timeframe);

			string tf = "min" + timeframe;
			Pricerangeforecast pricerangeforecast = resultObject.PowerstatsResults.Pricerangeforecast;
			RangeMinutes rangeMinutes = (RangeMinutes)pricerangeforecast.GetType().GetProperty(tf).GetValue(pricerangeforecast);
			double high = rangeMinutes.high;
			double low = rangeMinutes.low;

			


			//Lines
			SharpDX.Direct2D1.Brush highLineBrush = Brushes.Green.ToDxBrush(RenderTarget);
			SharpDX.Direct2D1.Brush lowLineBrush = Brushes.Red.ToDxBrush(RenderTarget);
			Vector2 highStartPoint = new Vector2(startX, chartScale.GetYByValue(high));
			Vector2 highEndPoint = new Vector2(endX, chartScale.GetYByValue(high));
			Vector2 lowStartPoint = new Vector2(startX, chartScale.GetYByValue(low));
			Vector2 lowEndPoint = new Vector2(endX, chartScale.GetYByValue(low));

			RenderTarget.DrawLine(highStartPoint, highEndPoint, highLineBrush, 3);
			RenderTarget.DrawLine(lowStartPoint, lowEndPoint, lowLineBrush, 3);

			highLineBrush.Dispose();
			lowLineBrush.Dispose();

			//Text
			SharpDX.Direct2D1.Brush textBrush = System.Windows.Media.Brushes.Orange.ToDxBrush(RenderTarget);
			SimpleFont simpleFont = chartControl.Properties.LabelFont ?? new SimpleFont();
			SharpDX.DirectWrite.TextFormat textFormat = simpleFont.ToDirectWriteTextFormat();
			Vector2 highTextPoint = new Vector2(startX, chartScale.GetYByValue(high) + 2);
			Vector2 lowTextPoint = new Vector2(startX, chartScale.GetYByValue(low) - 20);
			string highText;
			string lowText;

			if (BarsPeriod.BarsPeriodType == BarsPeriodType.Minute && timeframe < 1440)
			{
				highText = "High " + timeframe + " minutes";
				lowText = "Low " + timeframe + " minutes";
			}
			else
			{
				highText = "High 1 day";
				lowText = "Low 1 day";
			}

			SharpDX.DirectWrite.TextLayout highTextLayout = 
				new SharpDX.DirectWrite.TextLayout(NinjaTrader.Core.Globals.DirectWriteFactory, highText, textFormat,
				ChartPanel.W, textFormat.FontSize);
			SharpDX.DirectWrite.TextLayout lowTextLayout = 
				new SharpDX.DirectWrite.TextLayout(NinjaTrader.Core.Globals.DirectWriteFactory, lowText, textFormat,
				ChartPanel.W, textFormat.FontSize);
			RenderTarget.DrawTextLayout(highTextPoint, highTextLayout, textBrush);
			RenderTarget.DrawTextLayout(lowTextPoint, lowTextLayout, textBrush);

			//TextPrice
			Vector2 highTextPricePoint = new Vector2(startX, chartScale.GetYByValue(high) - 20);
			Vector2 lowTextPricePoint = new Vector2(startX, chartScale.GetYByValue(low) + 2);
			SharpDX.DirectWrite.TextLayout highTextPriceLayout =
				new SharpDX.DirectWrite.TextLayout(NinjaTrader.Core.Globals.DirectWriteFactory,
				Instrument.MasterInstrument.RoundToTickSize(high).ToString(), textFormat, ChartPanel.W, textFormat.FontSize);
			SharpDX.DirectWrite.TextLayout lowTextPriceLayout =
				new SharpDX.DirectWrite.TextLayout(NinjaTrader.Core.Globals.DirectWriteFactory,
				Instrument.MasterInstrument.RoundToTickSize(low).ToString(), textFormat, ChartPanel.W, textFormat.FontSize);
			RenderTarget.DrawTextLayout(highTextPricePoint, highTextPriceLayout, textBrush);
			RenderTarget.DrawTextLayout(lowTextPricePoint, lowTextPriceLayout, textBrush);

			textBrush.Dispose();
			textFormat.Dispose();
			highTextLayout.Dispose();
			lowTextLayout.Dispose();
			highTextPriceLayout.Dispose();
			lowTextPriceLayout.Dispose();
		}
		private void calculateCoordinatesForRendering(ChartControl chartControl, ChartScale chartScale)
		{
			float start1 = chartControl.GetXByBarIndex(ChartBars, CurrentBar) + 20;
			float start2 = start1 + ChartPanel.W / 15;
			float start3 = start2 + ChartPanel.W / 15;
			float end = start3 + ChartPanel.W / 15;

			if (BarsPeriod.BarsPeriodType == BarsPeriodType.Minute && BarsPeriod.Value <= 15)
			{
				renderLines(chartControl, chartScale, 15, start1, start2);
				renderLines(chartControl, chartScale, 30, start2, start3);
				renderLines(chartControl, chartScale, 60, start3, end);
			}
			else if (BarsPeriod.BarsPeriodType == BarsPeriodType.Minute && BarsPeriod.Value <= 30)
			{
				renderLines(chartControl, chartScale, 30, start1, start2);
				renderLines(chartControl, chartScale, 60, start2, start3);
				renderLines(chartControl, chartScale, 240, start3, end);
			}
			else if (BarsPeriod.BarsPeriodType == BarsPeriodType.Minute && BarsPeriod.Value <= 60)
			{
				renderLines(chartControl, chartScale, 60, start1, start2);
				renderLines(chartControl, chartScale, 240, start2, start3);
				renderLines(chartControl, chartScale, 1440, start3, end);
			}
			else if (BarsPeriod.BarsPeriodType == BarsPeriodType.Minute && BarsPeriod.Value <= 240)
			{
				renderLines(chartControl, chartScale, 240, start1, start2);
				renderLines(chartControl, chartScale, 1440, start2, start3);
			}
			else if (BarsPeriod.BarsPeriodType == BarsPeriodType.Minute && BarsPeriod.Value <= 1440)
			{
				renderLines(chartControl, chartScale, 1440, start1, start2);
			}
			else if (BarsPeriod.BarsPeriodType == BarsPeriodType.Day && BarsPeriod.Value == 1)
			{
				renderLines(chartControl, chartScale, 1440, start1, start2);
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
			Log("CI_ExpectedPriceRange: WebClient initialization", LogLevel.Information);
			ApiClient = new HttpClient();
			ApiClient.DefaultRequestHeaders.Accept.Clear();
			ApiClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
			string symbol = Instrument.MasterInstrument.Name;
			//int utcOffset = TimeZoneInfo.Local.BaseUtcOffset.Hours;
			//int utcOffset = TimeZoneInfo.Local.IsDaylightSavingTime(DateTime.Now) ?
			//	TimeZoneInfo.Local.BaseUtcOffset.Hours + 1 : TimeZoneInfo.Local.BaseUtcOffset.Hours;
			int utcOffset = Core.Globals.GeneralOptions.TimeZoneInfo.IsDaylightSavingTime(DateTime.Now) ?
				Core.Globals.GeneralOptions.TimeZoneInfo.BaseUtcOffset.Hours + 1
				: Core.Globals.GeneralOptions.TimeZoneInfo.BaseUtcOffset.Hours;

			//url = getURL(UserName, Password, "powerstats", symbol, utcOffset, "xml", false);
			url = ApiHelper.getURL(UserName, Password, "powerstats", symbol, utcOffset, "xml", false);

			//url = getURL("LIVE", UserName, Password);
			//Print("Url - " + url);
			//Print("Exchange - " + Instrument.Exchange);
			//Log("CI_ExpectedPriceRange: Url: " + getURL(UserName, Password, "powerstats", symbol, utcOffset, "xml", true), LogLevel.Information);
			Log("CI_ExpectedPriceRange: Url: " + ApiHelper.getURL(UserName, Password, "powerstats", symbol, utcOffset, "xml", true), LogLevel.Information);
		}

		private void shutdown()
        {
			if (apiCallTimer != null)
			{
				apiCallTimer.Enabled = false;
				apiCallTimer.Elapsed -= LoadXMLEventProcessor;
				apiCallTimer = null;
				Print("CI_ExpectedPriceRange: Timer disposed");
			}
			if (ApiClient != null)
            {
				ApiClient.CancelPendingRequests();
				ApiClient.Dispose();
				ApiClient = null;
				Print("CI_ExpectedPriceRange: HttpClient disposed");
			}
		}

		//Fill the json with Volatility Analysis Data

		public async void LoadXML(object myObject)
		{
			Print("CI_ExpectedPriceRange: LoadXML started");
			try
			{
				//cts.CancelAfter(100);
				//using (HttpResponseMessage response = await ApiClient.GetAsync(url, cts.Token))
				using (HttpResponseMessage response = await ApiClient.GetAsync(url))
				{
					if (response.IsSuccessStatusCode)
					{
						//resultJSON = await response.Content.ReadAsAsync<AutochartistPowerStatsXML>();
						//return resultJSON;
						string resultString = await response.Content.ReadAsStringAsync();

						XmlSerializer xmlSerializer =
								new XmlSerializer(typeof(AutochartistPowerStatsXML), new XmlRootAttribute("AutochartistAPI"));
						StringReader stringReader = new StringReader(resultString);
						resultObject = (AutochartistPowerStatsXML)xmlSerializer.Deserialize(stringReader);
						Print("CI_ExpectedPriceRange: ResultObject created");


						if (resultObject.Authentication.sessionid.Equals("0"))
						{
							isInstrumentExists = false;
							errorMessage = resultObject.Authentication.errormessage;
							Log("CI_ExpectedPriceRange: " + errorMessage, LogLevel.Alert);
							//MessageBox.Show(resultObject.Authentication.errormessage);
						}
						else if (resultObject.Error != null)
						{
							isInstrumentExists = false;
							errorMessage = resultObject.Error.message;
							Log("CI_ExpectedPriceRange: " + errorMessage, LogLevel.Alert);
							//MessageBox.Show(resultObject.Error.message);
						}

						else
						{
							isInstrumentExists = true;
							//Log("CI_ExpectedPriceRange: PowerStats for instrument " 
							//	+ resultObject.PowerstatsResults.Pricerangeforecast.symbol + " are loaded.", LogLevel.Information);
							//Print(resultObject.PowerstatsResults.Pricerangeforecast.symbol);
							//Print(resultObject.PowerstatsResults.Pricerangeforecast.min30.high);
							//Print(resultObject.PowerstatsResults.Pricerangeforecast.min30.low);
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
				MessageBox.Show(e.Message);
			}
			
		}

		//private async void LoadJSON(object myObject)
		//      {
		//	try
		//	{
		//		using (HttpResponseMessage response = await ApiClient.GetAsync(url))
		//		{
		//			if (response.IsSuccessStatusCode)
		//			{
		//				resultJSON = await response.Content.ReadAsAsync<VolatilityAnalisysJSON>();
		//			}
		//			else
		//			{
		//				throw new Exception(response.ReasonPhrase);
		//			}
		//		}

		//		if (!isInstrumentsChecked)
		//		{
		//			string instrumentName = Bars.Instrument.MasterInstrument.Name;
		//			if (resultJSON.isInstrumentExists(instrumentName))
		//			{
		//				isInstrumentExists = true;
		//			}
		//			else
		//			{
		//				isInstrumentExists = false;
		//				MessageBox.Show("There is no information for instrument with name \"" + instrumentName + "\"");
		//			}
		//			isInstrumentsChecked = true;
		//		}
		//		//Print(DateTime.Now);
		//	}
		//	catch (Exception e)
		//	{
		//		MessageBox.Show(e.Message);
		//	}
		//}
		#endregion

		#region Properties
		[NinjaScriptProperty]
		[Display(Name = "User Name", Order = 1, GroupName = "Parameters")]
		public string UserName
		{ get; set; }

		[NinjaScriptProperty]
		[PasswordPropertyText(true)]
		[Display(Name = "Password", Order = 2, GroupName = "Parameters")]
		public string Password
		{ get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private CI_ExpectedPriceRange[] cacheCI_ExpectedPriceRange;
		public CI_ExpectedPriceRange CI_ExpectedPriceRange(string userName, string password)
		{
			return CI_ExpectedPriceRange(Input, userName, password);
		}

		public CI_ExpectedPriceRange CI_ExpectedPriceRange(ISeries<double> input, string userName, string password)
		{
			if (cacheCI_ExpectedPriceRange != null)
				for (int idx = 0; idx < cacheCI_ExpectedPriceRange.Length; idx++)
					if (cacheCI_ExpectedPriceRange[idx] != null && cacheCI_ExpectedPriceRange[idx].UserName == userName && cacheCI_ExpectedPriceRange[idx].Password == password && cacheCI_ExpectedPriceRange[idx].EqualsInput(input))
						return cacheCI_ExpectedPriceRange[idx];
			return CacheIndicator<CI_ExpectedPriceRange>(new CI_ExpectedPriceRange(){ UserName = userName, Password = password }, input, ref cacheCI_ExpectedPriceRange);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.CI_ExpectedPriceRange CI_ExpectedPriceRange(string userName, string password)
		{
			return indicator.CI_ExpectedPriceRange(Input, userName, password);
		}

		public Indicators.CI_ExpectedPriceRange CI_ExpectedPriceRange(ISeries<double> input , string userName, string password)
		{
			return indicator.CI_ExpectedPriceRange(input, userName, password);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.CI_ExpectedPriceRange CI_ExpectedPriceRange(string userName, string password)
		{
			return indicator.CI_ExpectedPriceRange(Input, userName, password);
		}

		public Indicators.CI_ExpectedPriceRange CI_ExpectedPriceRange(ISeries<double> input , string userName, string password)
		{
			return indicator.CI_ExpectedPriceRange(input, userName, password);
		}
	}
}

#endregion
