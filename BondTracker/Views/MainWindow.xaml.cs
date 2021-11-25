using System;
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
        MainViewModel _main = new MainViewModel();
        public MainWindow()
        {
            InitializeComponent();
            DataContext = _main;
            InfoPanel.ItemsSource = _main.LoadDataOnStocksCollection();
        }
    }
}
