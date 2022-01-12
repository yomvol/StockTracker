using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using OxyPlot;
using OxyPlot.Axes;
using DataHarvester;
using System.Threading.Tasks;

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
        private Queue<string> FilesToBeParsed;
        private bool IsAnswered = false;
        private bool IsSuccess = false;
        private string message;
        public StockDataSet()
        {
            CurrentDate = DateTime.Today;
            EndDate = Program.FindLastTradingDay(CurrentDate); // temporary solution
            StartDate = EndDate.AddYears(-1);
            PlotMode = EndDate - StartDate;
            DataPoints = new List<DataPoint>();
            FilesToBeParsed = null;
        }

        public bool PopulateData(Stock input, ref string s)
        {
            this.Item = input;
            StartDownloading += Program.HandleStartDownloadingEvent;
            Program.Finish += HandleDataHarvesterEvent;
            StartDownloading += this.Handle_SD_Event;
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            OnStartDownloading();
            timer.Start();
            while (timer.ElapsedMilliseconds < 60000)
            {
                if (IsAnswered)
                {
                    break;
                }
                else
                    Task.WaitAll(new Task[] { Task.Delay(2000) }); // a 2 second delay to ease condition checking bouncing
            }
            timer.Stop();
            if (!IsAnswered)
            {
                // add message handling
                s = "Модуль загрузки не отвечает. Проверьте целостность файлов программы.";
                return false;
            }
            if (!IsSuccess)
            {
                // add message handling
                s = message;
                return false;
            }
            // It`s time to parse data from downloaded files
            // First In - First Out, firstly get older data
            if (FilesToBeParsed == null)
            {
                // add message handling
                s = "Модуль загрузки отчитался об успехе, но не послал пути загруженных файлов.";
                return false;
            }
            int total_number = FilesToBeParsed.Count;
            XmlTextReader reader;
            for (int i = 0; i < total_number; i++)
            {
                reader = new XmlTextReader(FilesToBeParsed.Dequeue());
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
                        int result = DateTime.ParseExact(reader.GetAttribute("TRADEDATE"), "yyyy-mm-dd", System.Globalization.CultureInfo.InvariantCulture).CompareTo(StartDate);
                        while (result < 0)
                        {
                            reader.ReadToNextSibling("row");
                            result = DateTime.ParseExact(reader.GetAttribute("TRADEDATE"), "yyyy-mm-dd", System.Globalization.CultureInfo.InvariantCulture).CompareTo(StartDate);
                        }
                    }
                    DateTime date_x;
                    double close_price_y;
                    while(reader.ReadToNextSibling("row"))
                    {
                        date_x = DateTime.ParseExact(reader.GetAttribute("TRADEDATE"), "yyyy-mm-dd", System.Globalization.CultureInfo.InvariantCulture);
                        close_price_y = Convert.ToDouble(reader.GetAttribute("CLOSE"));
                        DataPoints.Add(DateTimeAxis.CreateDataPoint(date_x, close_price_y));
                    }
                    reader.Dispose();
                    break;
                }
            }
            s = "Полный успех!";
            return true;
        }

        public event EventHandler StartDownloading;
        private void OnStartDownloading()
        {
            EventHandler RaiseStart = StartDownloading;
            if (RaiseStart != null)
            {
                StartDownloading(this, null);
            }
        }

        private void Handle_SD_Event(object sender, EventArgs e) // Testing event handling
        {
            EndDate = DateTime.Today.AddYears(-10);
        }

        private void HandleDataHarvesterEvent(object sender, OutcomeEventArgs e)
        {
            IsAnswered = true;
            if (!Item.Name.Equals(e.Name))
                message = "Ошибка: Имена загруженной и выбранной бумаги не совпадают.";
            else
            {
                if (e.Result == HarvestOutcomes.Success)
                {
                    IsSuccess = true;
                    FilesToBeParsed = e.FilePaths;
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
}
