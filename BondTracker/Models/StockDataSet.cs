using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using OxyPlot;
using OxyPlot.Axes;
using DataHarvester;
using System.Threading;
using System.Threading.Tasks;
using System.IO.MemoryMappedFiles;

namespace BondTracker.Models
{
    class StockDataSet
    {
        private Stock Item;
        public List<DataPoint> DataPoints;
        private DateTime CurrentDate;
        private DateTime StartDate;
        private DateTime EndDate;
        private TimeSpan PlotMode;
        
        public StockDataSet()
        {
            CurrentDate = DateTime.Today;
            EndDate = Program.FindLastTradingDay(CurrentDate); // temporary solution
            StartDate = EndDate.AddYears(-1);
            PlotMode = EndDate - StartDate;
            DataPoints = new List<DataPoint>();
        }

        public bool PopulateData(Stock input, ref string s)
        {
            this.Item = input;
            Task.WaitAll(new Task[] { Task.Delay(2000) });
            EventWaitHandle DownloadSyncEvent;
            try
            {
                DownloadSyncEvent = EventWaitHandle.OpenExisting(@"Local\MyDownloadSyncEvent");
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                throw;
            }
            DownloadSyncEvent.Set();
            DownloadSyncEvent.Reset();
            DownloadSyncEvent.WaitOne(100000);

            Program.HarvestOutput outcome;
            using (MemoryMappedFile mmf = MemoryMappedFile.OpenExisting("my_map"))
            {
                Mutex mutex = Mutex.OpenExisting("forkInDorm");
                mutex.WaitOne();
                using (MemoryMappedViewStream view_stream = mmf.CreateViewStream())
                {
                    var serializer = new XmlSerializer(typeof(Program.HarvestOutput));
                    StreamReader S_reader = new StreamReader(view_stream);
                    outcome = (Program.HarvestOutput)serializer.Deserialize(S_reader);
                    
                }
            }
            if (!outcome.IsAnswered)
            {
                // add message handling
                s = "Модуль загрузки не отвечает. Проверьте целостность файлов программы.";
                return false;
            }
            if (!outcome.IsSuccess)
            {
                // add message handling
                s = outcome.message;
                return false;
            }
            // It`s time to parse data from downloaded files
            // First In - First Out, firstly get older data
            if (outcome.FilesToBeParsed == null)
            {
                // add message handling
                s = "Модуль загрузки отчитался об успехе, но не послал пути загруженных файлов.";
                return false;
            }
            int total_number = outcome.FilesToBeParsed.Count;
            XmlTextReader reader;
            bool IsStartFound = false;
            for (int i = 0; i < total_number; i++)
            {
                if (IsStartFound == false)
                    i = 0;
                if (outcome.FilesToBeParsed.Count == 0)
                    break;
                reader = new XmlTextReader(outcome.FilesToBeParsed.Dequeue());
                while (reader.ReadToFollowing("data"))
                {
                    if (reader.GetAttribute("id") != "history")
                    {
                        reader.Skip();
                        continue;
                    }
                    reader.ReadToDescendant("rows");
                    reader.ReadStartElement("rows");
                    reader.Read(); // Reader position is on the first row
                    if (i == 0)
                    {
                        // Data earlier than starting date must be skipped
                        int result = DateTime.ParseExact(reader.GetAttribute("TRADEDATE"), "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture).CompareTo(StartDate);
                        while (result < 0)
                        {
                            if (reader.ReadToNextSibling("row"))
                            {
                                DateTime dt = DateTime.ParseExact(reader.GetAttribute("TRADEDATE"), "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                                result = dt.CompareTo(StartDate);
                            }
                            else
                                break;
                        }
                        if (result >= 0)
                            IsStartFound = true;
                        else
                            continue;
                    }
                    DateTime date_x;
                    double close_price_y;
                    while(reader.ReadToNextSibling("row"))
                    {
                        date_x = DateTime.ParseExact(reader.GetAttribute("TRADEDATE"), "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                        string str = reader.GetAttribute("CLOSE");
                        if (str != string.Empty)
                            close_price_y = Double.Parse(str, System.Globalization.CultureInfo.InvariantCulture);
                        else
                            continue;
                        DataPoints.Add(DateTimeAxis.CreateDataPoint(date_x, close_price_y));
                    }
                    reader.Dispose();
                    break;
                }
            }
            s = "Полный успех!";
            return true;
        }
    }
}
