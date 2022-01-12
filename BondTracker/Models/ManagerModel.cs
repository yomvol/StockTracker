using System;
using System.Collections.Generic;
using System.Text;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace BondTracker.Models
{
    class ManagerModel
    {
        public PlotModel StockChart { get; private set; }
        public string Message = "";
        public ManagerModel()
        {
            this.StockChart = new PlotModel { Title = "YNDX" };
            DateTime end = DateTime.Today.AddDays(-1.0); // I haven`t come up with an idea how to fetch info on today price yet
            DateTime start = end.AddYears(-1);
            double MinValue = DateTimeAxis.ToDouble(start);
            double MaxValue = DateTimeAxis.ToDouble(end);
            this.StockChart.Axes.Add(new DateTimeAxis { Position = AxisPosition.Bottom, Minimum = MinValue, Maximum = MaxValue, StringFormat = "MMM" });
            this.StockChart.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0});
            LineSeries chart = new LineSeries();
            StockDataSet DataSet = new StockDataSet();
            
            Stock YNDX_Stock = new Stock { Name = "YNDX", ID = 1 };
            if (!DataSet.PopulateData(YNDX_Stock, ref Message)) // As a temporary solution. There`s no actual stock argument to give yet
            {
                chart.Background = OxyColor.FromRgb(255, 0, 50); // Doing something, because all went wrong
            }
            chart.CanTrackerInterpolatePoints = false;
            chart.ItemsSource = DataSet.DataPoints;
            this.StockChart.Series.Add(chart);
        }
    }
}
