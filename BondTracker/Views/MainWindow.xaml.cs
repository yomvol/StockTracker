﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BondTracker.ViewModels;

namespace BondTracker.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MainViewModel main_view_model;
        public MainWindow()
        {
            InitializeComponent();
            main_view_model = new MainViewModel();
            DataContext = main_view_model.manager;
            //InfoPanel.ItemsSource = _main.LoadDataOnStocksCollection();
        }
    }
}
