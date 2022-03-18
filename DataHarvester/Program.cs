using System;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Threading;
using System.Security.AccessControl;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace DataHarvester
{
    public class Program
    {
        public static EventWaitHandle DownloadSyncEvent;
        static void Main(string[] args)
        {
            // create a rule that allows anybody in the "Users" group to synchronise with us
            var users = new System.Security.Principal.SecurityIdentifier(System.Security.Principal.WellKnownSidType.BuiltinUsersSid, null);
            var rule = new EventWaitHandleAccessRule(users, EventWaitHandleRights.Synchronize | EventWaitHandleRights.Modify, AccessControlType.Allow);
            var security = new EventWaitHandleSecurity();
            security.AddAccessRule(rule);
            bool created = false;
            DownloadSyncEvent = new EventWaitHandle(false, EventResetMode.ManualReset, "MyDownloadSyncEvent", out created);
            if(created == false)
            {
                Console.WriteLine("Error: Failed to setup the synchronizing mechanism.");
                return;
            }
            DownloadSyncEvent.SetAccessControl(security);

            string ItemName = "YNDX"; // In future we`re going to receive an item to find from the GUI app
            DateTime Today = DateTime.Today;
            DateTime LastTradingDay = FindLastTradingDay(Today);
            DateTime StartDate = LastTradingDay.AddYears(-1);
            TimeSpan PlotMode = LastTradingDay - StartDate; // One year for the most basic example
            
            DownloadSyncEvent.WaitOne();

            HarvestOutput outcome = new HarvestOutput();
            Finish += outcome.HandleHarvesterOutcomeEvent;

            // Let`s assume that MOEX API is no more available... Hence, all code related to accessing MOEX Web Service must be commented out.
            // Actually, the entire purpose of the harvesting module is compromised. Luckily, there are some files with data on Yandex stock left.

            //string url = "https://iss.moex.com/iss/engines/stock/markets/shares/boards/TQBR/securities/" + ItemName + ".xml";
            //WebClient client = new WebClient();
            //client.Proxy = null;
            //string xml;
            //try
            //{
            //    xml = client.DownloadString(url);
            //}
            //catch (Exception)
            //{
            //    client.Dispose();
            //    // Web client failed to download general security description
            //    OnFinish(new OutcomeEventArgs(HarvestOutcomes.Error_Web_General_Info));
            //    DownloadSyncEvent.Set();
            //    Thread.Sleep(2000);
            //    throw;
            //}
            
            //XDocument xdoc = XDocument.Parse(xml);
            //xdoc.Save("..\\..\\..\\ListingDownloads\\Stocks\\" + ItemName + "_stc.xml");

            //url = "https://iss.moex.com/iss/history/engines/stock/markets/shares/sessions/total/securities/" + ItemName + ".xml";
            //try
            //{
            //    xml = client.DownloadString(url);
            //}
            //catch (Exception)
            //{
            //    client.Dispose();
            //    // Web client failed to download the oldest historical data of this security. Very bad.
            //    OnFinish(new OutcomeEventArgs(HarvestOutcomes.Error_Web_History_Zero_Index));
            //    DownloadSyncEvent.Set();
            //    Thread.Sleep(2000);
            //    throw;
            //}
            //xdoc = XDocument.Parse(xml);
            //xdoc.Save("..\\..\\..\\TradeHistoryDownloads\\" + ItemName + "_0.xml");
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
                OnFinish(new OutcomeEventArgs(HarvestOutcomes.Error_Cursor_Is_Not_Found), ref outcome);
                reader.Dispose();
                //client.Dispose();
                DownloadSyncEvent.Set();
                Thread.Sleep(2000);
                return;
            }

            uint LastPageIndex = Total - PageSize;
            if (LastPageIndex == 0)
            {
                Console.WriteLine("Error: Invalid recent data index");
                // Failed at calculating index of the page with the most recent data
                OnFinish(new OutcomeEventArgs(HarvestOutcomes.Error_Calc_Last_Page_Index), ref outcome);
                reader.Dispose();
                //client.Dispose();
                DownloadSyncEvent.Set();
                Thread.Sleep(2000);
                return;
            }
            //url = String.Format("https://iss.moex.com/iss/history/engines/stock/markets/shares/sessions/total/securities/{0}.xml?start={1}", ItemName, LastPageIndex );
            //try
            //{
            //    xml = client.DownloadString(url);
            //}
            //catch (Exception)
            //{
            //    reader.Dispose();
            //    client.Dispose();
            //    // Failed to download the page with the most recent data
            //    OnFinish(new OutcomeEventArgs(HarvestOutcomes.Error_Web_History_Last_Page));
            //    DownloadSyncEvent.Set();
            //    Thread.Sleep(2000);
            //    throw;
            //}
            //xdoc = XDocument.Parse(xml);
            //xdoc.Save("..\\..\\..\\TradeHistoryDownloads\\" + ItemName + "_" + LastPageIndex.ToString() + ".xml");

            // Okay, now it`s time to download as few files as possible to sufficiently cover Plotting mode time span with data
            // The question is: How to find out where is this cursor spot one year before the last trading date
            // One row corresponds to one trading day, five rows to one trading week, one normal year is 365 days, ~52 weeks
            // Number of trading days without holidays is 365 - 52 * 2 =~ 261
            // Hence one year before spot is approximately 261 rows up

            const uint NumberOfRowsInOneYear = 262;
            CursorPos = Total - NumberOfRowsInOneYear;
            Queue<string> FilesToBeSent = new Queue<string>(); // Actual files to parse data from

            //for (; CursorPos <= Total;) 
            //{
            //    url = String.Format("https://iss.moex.com/iss/history/engines/stock/markets/shares/sessions/total/securities/{0}.xml?start={1}", ItemName, CursorPos);
            //    xml = client.DownloadString(url);
            //    xdoc = XDocument.Parse(xml);
            //    xdoc.Save("..\\..\\..\\TradeHistoryDownloads\\" + ItemName + "_" + CursorPos.ToString() + ".xml");
            //    FilesToBeSent.Enqueue("..\\..\\..\\..\\DataHarvester\\TradeHistoryDownloads\\" + ItemName + "_" + CursorPos.ToString() + ".xml");
            //    if (CursorPos + 100 <= Total)
            //        CursorPos += 100;
            //    else
            //        break;
            //}

            // Enqueue old files
            FilesToBeSent.Enqueue("..\\..\\..\\..\\DataHarvester\\TradeHistoryDownloads\\YNDX_0.xml");
            FilesToBeSent.Enqueue("..\\..\\..\\..\\DataHarvester\\TradeHistoryDownloads\\YNDX_2213.xml");
            FilesToBeSent.Enqueue("..\\..\\..\\..\\DataHarvester\\TradeHistoryDownloads\\YNDX_2313.xml");
            FilesToBeSent.Enqueue("..\\..\\..\\..\\DataHarvester\\TradeHistoryDownloads\\YNDX_2375.xml");
            FilesToBeSent.Enqueue("..\\..\\..\\..\\DataHarvester\\TradeHistoryDownloads\\YNDX_2413.xml");

            reader.Dispose();
            //client.Dispose();
            OnFinish(new OutcomeEventArgs(HarvestOutcomes.Success, FilesToBeSent), ref outcome);
            DownloadSyncEvent.Set();
            Console.WriteLine("\nFiles have been sucessfully downloaded.");
            Thread.Sleep(10000);
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
        private static void OnFinish(OutcomeEventArgs e, ref HarvestOutput reference) // a static event to fire in Main.
        {
            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            EventHandler<OutcomeEventArgs> raise_finish = Finish;
            if (raise_finish != null)
            {
                raise_finish(null, e);
            }

            MemoryMappedFile mmf = MemoryMappedFile.CreateNew("my_map", 1000000);
            {
                Mutex mutex = new Mutex(true, "forkInDorm");
                MemoryMappedViewStream view_stream = mmf.CreateViewStream();
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(HarvestOutput));
                    StreamWriter writer = new StreamWriter(view_stream);
                    serializer.Serialize(writer, reference);
                    writer.Close();
                }
                mutex.ReleaseMutex();
            }
        }

        [Serializable]
        public class SerializableQueue : Queue<string>
        {
            public string this[int index]
            {
                get
                {
                    int count = 0;
                    foreach (string o in this)
                    {
                        if (count == index)
                            return o;
                        count++;
                    }
                    return null;
                }
            }

            public void Add(string r)
            {
                Enqueue(r);
            }
        }

        [Serializable]
        public class HarvestOutput
        {
            public bool IsAnswered; // for the serialization every field must be public, though private set is logically preferable
            public bool IsSuccess;
            public string message;
            public SerializableQueue FilesToBeParsed;

            public HarvestOutput()
            {
                IsAnswered = false;
                IsSuccess = false;
                FilesToBeParsed = new SerializableQueue();
            }
            internal void HandleHarvesterOutcomeEvent(object sender, OutcomeEventArgs e)
            {
                IsAnswered = true;
                if (e.Result == HarvestOutcomes.Success)
                {
                    IsSuccess = true;
                    foreach (string str in e.FilePaths)
                    {
                        FilesToBeParsed.Add(str);
                    }
                    message = "Данные успешно загружены.";
                }
                else if (e.Result == HarvestOutcomes.Error_Web_General_Info)
                    message = "Ошибка: Не удалось загрузить общую информацию об этой бумаге. Проверьте, есть ли она в листинге биржи. Проверьте ваше подключение к сети.";
                else if (e.Result == HarvestOutcomes.Error_Web_History_Zero_Index)
                    message = "Ошибка: Не удалось загрузить историческую информацию об этой бумаге.";
                else if (e.Result == HarvestOutcomes.Error_Web_History_Last_Page)
                    message = "Ошибка: Не удалось загрузить последнюю историческую информацию об этой бумаге.";
                else if (e.Result == HarvestOutcomes.Error_Cursor_Is_Not_Found)
                    message = "Ошибка: Ошибка чтения файла с исторической информацией. Проверьте структуру загруженного файла.";
                else if (e.Result == HarvestOutcomes.Error_Calc_Last_Page_Index)
                    message = "Ошибка: Историческую информацию об этой бумаге удалось загрузить, но фвйл имеет нетипичный механизм связи с родстсвенными файлами. Проверьте структуру загруженного файла.";
                else
                    message = "Неизвестная ошибка.";
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
        public HarvestOutcomes Result { get; private set; }
        public Queue<string> FilePaths { get; private set; }
        public OutcomeEventArgs( HarvestOutcomes val, Queue<string> q = null)
        {
            Result = val;
            FilePaths = q;
        }
    }
}
