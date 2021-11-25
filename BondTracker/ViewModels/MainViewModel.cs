using System;
using System.Collections.Generic;
using System.Text;
using OxyPlot;
using OxyPlot.Series;
using BondTracker.Models;

namespace BondTracker.ViewModels
{
    class MainViewModel
    {
        public List<Stock> LoadDataOnStocksCollection()
        {
            List<Stock> stocks = new List<Stock>();
            stocks.Add(new Stock(){
                ID = 1, Name = "RUAL", Price = 72.73, Change = 1.24, IsDivident = false, NumberOfSharesInLot = 100
            });

            stocks.Add(new Stock()
            {
                ID = 2, Name = "CHMF", Price = 1654, Change = -0.07, IsDivident = true, NumberOfSharesInLot = 1, DividentsPerShare = 116.4,
                NextDividentPayment = new DateTime(2021, 12, 14)
            });

            stocks.Add(new Stock()
            {
                ID = 3, Name = "MGNT", Price = 5925, Change = 0.84, IsDivident = true, NumberOfSharesInLot = 1, DividentsPerShare = 490.62,
                NextDividentPayment = new DateTime(2021, 12, 28)
            });

            return stocks;
        }
        public PlotModel MyModel { get; private set; }
        public MainViewModel()
        {
            this.MyModel = new PlotModel{ Title = "Example1" };
            this.MyModel.Series.Add(new FunctionSeries(Math.Cos, 0, 10, 0.1, "cos(x)"));
        }
    }
}
