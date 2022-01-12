using System;
using System.Xml;
using System.Xml.Linq;
using System.Net;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataHarvester
{
    public class Program
    {
        private static bool IsStart = false;
        static void Main(string[] args)
        {
            string ItemName = "YNDX"; // In future we`re going to receive an item to find from the GUI app
            DateTime Today = DateTime.Today;
            DateTime LastTradingDay = FindLastTradingDay(Today);
            DateTime StartDate = LastTradingDay.AddYears(-1);
            TimeSpan PlotMode = LastTradingDay - StartDate; // One year for the most basic example

            while (IsStart == false)
            {
                Task.WaitAll(new Task[] { Task.Delay(3000) }); // a 3 second delay to ease condition checking bouncing
            }

            string url = "https://iss.moex.com/iss/engines/stock/markets/shares/boards/TQBR/securities/" + ItemName + ".xml";
            WebClient client = new WebClient();
            client.Proxy = null;
            string xml;
            try
            {
                xml = client.DownloadString(url);
            }
            catch (Exception)
            {
                client.Dispose();
                // Web client failed to download general security description
                OnFinish(new OutcomeEventArgs(ItemName, HarvestOutcomes.Error_Web_General_Info));
                throw;
            }
            
            XDocument xdoc = XDocument.Parse(xml);
            xdoc.Save("..\\..\\..\\ListingDownloads\\Stocks\\" + ItemName + "_stc.xml");

            url = "https://iss.moex.com/iss/history/engines/stock/markets/shares/sessions/total/securities/" + ItemName + ".xml";
            try
            {
                xml = client.DownloadString(url);
            }
            catch (Exception)
            {
                client.Dispose();
                // Web client failed to download the oldest historical data of this security. Very bad.
                OnFinish(new OutcomeEventArgs(ItemName, HarvestOutcomes.Error_Web_History_Zero_Index));
                throw;
            }
            xdoc = XDocument.Parse(xml);
            xdoc.Save("..\\..\\..\\TradeHistoryDownloads\\" + ItemName + "_0.xml");
            XmlTextReader reader = new XmlTextReader("..\\..\\..\\TradeHistoryDownloads\\" + ItemName + "_0.xml");
            bool IsCursorFound = false;
            uint CursorPos = 0, Total = 0, PageSize = 0;

            while (reader.ReadToFollowing("data"))
            {
                if (reader.GetAttribute("id") != "history.cursor")
                {
                    reader.Skip();
                    continue;
                }
                else
                {
                    IsCursorFound = true;
                    reader.ReadToDescendant("rows");
                    reader.ReadStartElement("rows");
                    reader.Read();
                    
                    CursorPos = System.Convert.ToUInt32(reader.GetAttribute("INDEX"));
                    Total = System.Convert.ToUInt32(reader.GetAttribute("TOTAL"));
                    PageSize = System.Convert.ToUInt32(reader.GetAttribute("PAGESIZE"));
                    //Console.WriteLine("Name: " + reader.Name);
                    //Console.WriteLine("Attribute Count: " + reader.AttributeCount.ToString());
                    //Console.WriteLine("Value: " + reader.Value.ToString());
                    //Console.WriteLine("Cursor position: " + CursorPos.ToString());
                    //Console.WriteLine("Total size: " + Total.ToString());
                    //Console.WriteLine("Page size: " + PageSize.ToString());
                }
            }

            if(IsCursorFound == false)
            {
                Console.WriteLine("Error: Cursor is not found.");
                // Cursor error
                OnFinish(new OutcomeEventArgs(ItemName, HarvestOutcomes.Error_Cursor_Is_Not_Found));
                reader.Dispose();
                client.Dispose();
                return;
            }

            uint LastPageIndex = Total - PageSize;
            if (LastPageIndex == 0)
            {
                Console.WriteLine("Error: Invalid recent data index");
                // Failed at calculating index of the page with the most recent data
                OnFinish(new OutcomeEventArgs(ItemName, HarvestOutcomes.Error_Calc_Last_Page_Index));
                reader.Dispose();
                client.Dispose();
                return;
            }
            url = String.Format("https://iss.moex.com/iss/history/engines/stock/markets/shares/sessions/total/securities/{0}.xml?start={1}", ItemName, LastPageIndex );
            try
            {
                xml = client.DownloadString(url);
            }
            catch (Exception)
            {
                reader.Dispose();
                client.Dispose();
                // Failed to download the page with the most recent data
                OnFinish(new OutcomeEventArgs(ItemName, HarvestOutcomes.Error_Web_History_Last_Page));
                throw;
            }
            xdoc = XDocument.Parse(xml);
            xdoc.Save("..\\..\\..\\TradeHistoryDownloads\\" + ItemName + "_" + LastPageIndex.ToString() + ".xml");

            // Okay, now it`s time to download as few files as possible to sufficiently cover Plotting mode time span with data
            // The question is: How to find out where is this cursor spot one year before the last trading date
            // One row corresponds to one trading day, five rows to one trading week, one normal year is 365 days, ~52 weeks
            // Number of trading days without holidays is 365 - 52 * 2 =~ 261
            // Hence one year before spot is approximately 261 rows up

            const uint NumberOfRowsInOneYear = 262;
            CursorPos = Total - NumberOfRowsInOneYear;
            Queue<string> FilesToBeSent = new Queue<string>(); // Actual files to parse data from

            for (; CursorPos <= Total;) 
            {
                url = String.Format("https://iss.moex.com/iss/history/engines/stock/markets/shares/sessions/total/securities/{0}.xml?start={1}", ItemName, CursorPos);
                xml = client.DownloadString(url);
                xdoc = XDocument.Parse(xml);
                xdoc.Save("..\\..\\..\\TradeHistoryDownloads\\" + ItemName + "_" + CursorPos.ToString() + ".xml");
                FilesToBeSent.Enqueue("..\\..\\..\\..\\DataHarvester\\TradeHistoryDownloads\\" + ItemName + "_" + CursorPos.ToString() + ".xml");
                if (CursorPos + 100 <= Total)
                    CursorPos += 100;
                else
                    break;
            }

            reader.Dispose();
            client.Dispose();
            OnFinish(new OutcomeEventArgs(ItemName, HarvestOutcomes.Success, FilesToBeSent));
            Console.WriteLine("\nFiles have been sucessfully downloaded.");
        }

        public static void HandleStartDownloadingEvent(object sender, EventArgs e)
        {
            IsStart = true;
        }

        public static DateTime FindLastTradingDay(DateTime today) // Method doesn`t account holiday Moscow exchange schedule
        {
            today.AddDays(-1.0); // There`s no way to get today info through historical data tab, hence yesterday it is
            if (today.DayOfWeek == DayOfWeek.Sunday)
            {
                today.AddDays(-1.0); // the day before yesterday
                return today;
            }
            if (today.DayOfWeek == DayOfWeek.Monday)
            {
                today.AddDays(-2.0); // Even it`s Monday today, we`re stil going to fetch data on Friday
                return today;
            }
            else
                return today; // if today is Saturday, then yesterday is still last day
        }
        private static XmlDocument ToXmlDocument(XDocument xDocument)
        {
            var xmlDocument = new XmlDocument();
            using (var reader = xDocument.CreateReader())
            {
                xmlDocument.Load(reader);
            }

            var xDeclaration = xDocument.Declaration;
            if (xDeclaration != null)
            {
                var xmlDeclaration = xmlDocument.CreateXmlDeclaration(
                    xDeclaration.Version,
                    xDeclaration.Encoding,
                    xDeclaration.Standalone);

                xmlDocument.InsertBefore(xmlDeclaration, xmlDocument.FirstChild);
            }
            return xmlDocument;
        }
 
        public static event EventHandler<OutcomeEventArgs> Finish;// Code for my event to fire
        private static void OnFinish(OutcomeEventArgs e) // a static event to fire in Main. I`ll come up with something, if I need info on sender.
        {
            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            EventHandler<OutcomeEventArgs> raise_finish = Finish;
            if (raise_finish != null)
            {
                raise_finish(null, e);
            }
        }
    }

    public enum HarvestOutcomes
    {
        Success = 0,
        Error_Web_General_Info,
        Error_Web_History_Zero_Index,
        Error_Web_History_Last_Page,
        Error_Cursor_Is_Not_Found,
        Error_Calc_Last_Page_Index
    }

    public class OutcomeEventArgs : EventArgs
    {
        public string Name { get; private set; } // Item name
        public HarvestOutcomes Result { get; private set; }
        public Queue<string> FilePaths { get; private set; }
        public OutcomeEventArgs(string name, HarvestOutcomes val, Queue<string> q = null)
        {
            Name = name;
            Result = val;
            FilePaths = q;
        }
    }
}
