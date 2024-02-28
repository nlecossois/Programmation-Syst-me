using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;

namespace EasySaveV2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class View : Window
    {
        Client client = new Client();

        public View()
        {
            
            InitializeComponent();
            DataContext = new ViewModel();
            Thread clientThread = new Thread(client.Start);
            GlobalVariables.clt = client;
            clientThread.Start();
            Closing += Window_Closing;

        }

        void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            client.Stop();
            Trace.WriteLine("Stop Client!");
        }

    }
}
