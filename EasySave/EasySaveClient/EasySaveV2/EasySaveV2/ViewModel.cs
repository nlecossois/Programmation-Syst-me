using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace EasySaveV2
{
    
    public class ViewModel : INotifyPropertyChanged
    {

        Model model = new Model();

        private ObservableCollection<ProgressBarElement> _progressBarList;
        public ObservableCollection<ProgressBarElement> ProgressBarList {
            get
            {
                return _progressBarList;
            }
            set
            {
                if(_progressBarList != value)
                {
                    _progressBarList = value;
                    OnPropertyChanged(nameof(ProgressBarList));
                    OnPropertyChanged(nameof(IsProgressBarListEmpty));
                }
                
            }
        }
        public bool IsProgressBarListEmpty => ProgressBarList == null || ProgressBarList.Count == 0;

        private string _resultText;


        public string ResultText
        {
            get { return _resultText; }
            set
            {
                if (_resultText != value)
                {
                    _resultText = value;
                    OnPropertyChanged();
                }
            }
        }
        public ViewModel()
        {
            //Initialisation 
            GlobalVariables.vm = this;
            setResultText("{{ app.printer.waitForServer }}");
            ProgressBarList = new ObservableCollection<ProgressBarElement> {};


        }

        public void setResultText(string arg)
        {
            ResultText = model.getMessage(arg);
        }


        //Methode pour renvoyer la valeur d'une progress bar
        public int getProgressBarValue(string name)
        {
            int rt = 0;

            foreach (var element in ProgressBarList)
            {
                //Check if the name matches
                if (element.Name == name)
                {
                    //Return progress bar value
                    rt =  element.ProgressBarValue;
                    
                    //Exit the loop because we found the element
                    break;
                }
            }
            return rt;
        }

        //Méthode pour mettre en play/pause une progress bar
        public void EditProgressBarState(string name, bool state)
        {
            //Browse the ProgressBarList collection
            foreach (var element in ProgressBarList)
            {
                //Check if the name matches
                if (element.Name == name)
                {
                    //Update progress bar value
                    element.IsPlaying = state;
                    //Exit the loop because we found the element
                    break;
                }
            }
        }

        //Method to update the progress bar based on its name
        public void EditProgressBarValue(string name, int newValue)
        {
            //Browse the ProgressBarList collection
            foreach (var element in ProgressBarList)
            {
                //Check if the name matches
                if (element.Name == name)
                {
                    //Update progress bar value
                    element.ProgressBarValue = newValue;
                    element.FormattedProgressBarValue = newValue + "%";
                    //Exit the loop because we found the element
                    break;
                }
            }
        }

        public void EditMessageOnProgressBar(string name, string newValue)
        {
            newValue = model.getMessage(newValue);
            foreach (var element in ProgressBarList)
            {
                if(element.Name == name)
                {
                    //Update bar text only if thread is not already killed
                    if (!element.FormattedProgressBarValue.Contains("/!\\"))
                    {
                        element.FormattedProgressBarValue = newValue;
                        break;
                    }
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
    public class ProgressBarElement: INotifyPropertyChanged
    {

        Model model = new Model();
        public string Name { get; set; }
        public ICommand TogglePlayPauseCommand => new RelayCommand(ExecuteTogglePlayPauseCommand);
        public ICommand StopCommand => new RelayCommand(ExecuteStopCommand);
        private int _progressBarValue;
        public int ProgressBarValue {
            get
            {
                return _progressBarValue;
            }
            set
            {
                if(_progressBarValue != value)
                {
                    _progressBarValue = value;
                    OnPropertyChanged(nameof(ProgressBarValue));
                }
            }
        }
        private bool _isPlaying;
        public bool IsPlaying
        {
            get { return _isPlaying; }
            set
            {
                if (_isPlaying != value)
                {
                    _isPlaying = value;
                    OnPropertyChanged(nameof(IsPlaying));
                }
            }
        }

        private string _formattedValue;
        public string FormattedProgressBarValue
        {
            get
            {
                return $"{_formattedValue}";
            }
            set
            {
                if(_formattedValue != value)
                {
                    _formattedValue = value;
                    OnPropertyChanged(nameof(FormattedProgressBarValue));
                }
            }
        }

        private void ExecuteTogglePlayPauseCommand(object parameter)
        {
            if (parameter != null)
            {
                string name = parameter.ToString();
                string part = name.Split(' ')[1];
                if (!IsPlaying)
                {
                    //Pause logic
                    GlobalVariables.clt.Send("/Break " + part);
                    GlobalVariables.vm.EditMessageOnProgressBar(name, _progressBarValue + "% - {{ thread.paused }}");
                }
                else
                {
                    //Boot logic
                    GlobalVariables.clt.Send("/Unbreak " + part);
                    GlobalVariables.vm.EditMessageOnProgressBar(name, _progressBarValue + "%");
                }
            }
            IsPlaying = !IsPlaying;
        }

        private void ExecuteStopCommand(object parameter)
        {
            //Stop logic
            string name = parameter.ToString();
            string part = name.Split(' ')[1];
            GlobalVariables.clt.Send("/Kill " + part);
            GlobalVariables.vm.EditMessageOnProgressBar(name, "{{ thread.killed }}");
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
