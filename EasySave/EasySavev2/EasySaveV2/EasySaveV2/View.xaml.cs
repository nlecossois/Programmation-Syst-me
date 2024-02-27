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

namespace EasySaveV2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
public partial class View : Window
    {
        public View()
        {
            //Here we carry out the control to make the application single-instance using a mutex
            Mutex mutex = new Mutex(true, "{F48SDQF6f-sd8g-54fs-48p2-JH2IKK6A8}");

            //If the mutex is already taken this means that another instance of the application is running on this device
            if (!mutex.WaitOne(TimeSpan.Zero, true))
            {
                MessageBoxResult result = MessageBox.Show("Unable to start application : The application is already open on this computer.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                //Closing the application
                if (result == MessageBoxResult.OK)
                {
                    Application.Current.Shutdown();
                }
                return;

            } else
            {
                InitializeComponent();
                DataContext = new ViewModel();
            }


            void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
            {
                mutex.ReleaseMutex();
            }

            
        }

    }
}
