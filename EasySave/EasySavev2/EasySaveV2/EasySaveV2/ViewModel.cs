﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
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
        public ICommand SaveWork { get; private set; }
        public ICommand OpenSettingsCommand { get; private set; }
        public string lang;
        public string logType;
        public string copyType;
        public List<string> selectedScriptingTypes = new List<string>();
        public List<string> selectedPriorityType = new List<string>();

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
        private string _inputText;

        private string _resultText;

        public string AppPrinterCalc
        {
            get { return model.getMessage("{{ app.printer.calc }}"); }
        }

        public string getMessageFromParameter(string param)
        {
            return model.getMessage(param);
        }

        public string getJobApp()
        {
            return model.getCurrentJobApp();
        }

        public string InputText
        {
            get { return _inputText; }
            set
            {
                if (_inputText != value)
                {
                    _inputText = value;
                    OnPropertyChanged();
                }
            }
        }

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
            OpenSettingsCommand = new RelayCommand(OpenSettings);
            SaveWork = new RelayCommand(Click, CanExecute);
            ProgressBarList = new ObservableCollection<ProgressBarElement> { };
            _resultText = model.getMessage("{{ app.printer.noData }}");
        }



        private bool CanExecute(object parameter)
        {
            //Logic to determine if the command can be executed
            if(GlobalVariables.saveThreadProcess == 0)
            {
                return true;
            } else
            {
                return false;
            }
            
        }

        private void Click(object parameter)
        {
            ProgressBarList.Clear();
            if (InputText == null)
            {
                ResultText = model.getMessage("{{ error.noSave }}");
            } else
            {
                string formatUserPrompt = model.getMessage(model.formatUserPrompt(InputText));
                if (model.formatUserPrompt(InputText).Contains(" error."))
                {
                    ResultText = formatUserPrompt;
                }
                else
                {
                    //Retrieving the backups to run and the total number of backups
                    string finalUserPrompt = model.GetSaveData(formatUserPrompt);
                    if (finalUserPrompt.Contains("error"))
                    {
                        ResultText = model.getMessage(finalUserPrompt);
                    } else
                    {
                        GlobalVariables.saveThreadProcess = 0;
                        //From here, this piece of code corresponds to the program frame
                        //We start by extracting the information into an array of integers and an integer
                        int amountOfSaves = model.extractUserPrompt(finalUserPrompt, 1);
                        List<int> listOfSaves = model.extractUserPrompt(finalUserPrompt);
                        //We perform the action to initialize and load the bars and return an empty ResultText.
                        ResultText = "";

                        GlobalVariables.prioritaryEnded = 0;
                        GlobalVariables.totalSaves = amountOfSaves;

                        //We first empty the list of progressbars
                        ProgressBarList.Clear();

                        //We display the correct number of progress bars
                        foreach (int index in listOfSaves)
                        {
                            ProgressBarList.Add(
                            new ProgressBarElement { Name = "Save " + index, ProgressBarValue = 0 }
                        );}
                        //We send data to app client
                        GlobalVariables.srv.Send("/SaveList " + string.Join("-", listOfSaves));

                        //We call the model method which will take care of threading each backup and managing the waiting list
                        model.SemaphoreWaitList(listOfSaves);
                        GlobalVariables.vm = this;
                    }
                }
            }
        }

        //Method to play/pause a progress bar
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

        //Method to return the value of a progress bar
        public int getProgressBarValue(string name)
        {
            int rt = 0;

            foreach (var element in ProgressBarList)
            {
                //Check if the name matches
                if (element.Name == name)
                {
                    //Return progress bar value
                    rt = element.ProgressBarValue;

                    //Exit the loop because we found the element
                    break;
                }
            }
            return rt;
        }

        //Method used to get max transfert size
        public string getMaxTransfert()
        {
            return model.getMaxSizeTransfert();
        }

        private void OpenSettings(object parameter)
        {
            //Create an instance of your settings window
            SettingsWindow settingsWindow = new SettingsWindow(this);

            //Display the window in dialog mode
            bool? result = settingsWindow.ShowDialog();

            //You can check the result if necessary (for example, if the user pressed Save or Cancel)
            if (result == true)
            {
                //Logic to execute if user pressed save
                selectedScriptingTypes = settingsWindow.selectedScripting;
                selectedPriorityType = settingsWindow.selectedPriority;
                lang = settingsWindow.SelectedLanguage;
                logType = settingsWindow.SelectedLogType;
                copyType = settingsWindow.SelectedCopyType;
                model.setLogFormat(logType);
                model.setCurrentJobApp(settingsWindow.currentJobApp);
                model.setLang(lang);
                model.setCopyMethod(copyType);
                model.setEncryptFileType(selectedScriptingTypes);
                model.setPriorityType(selectedPriorityType);
                model.setMaxSizeTransfert(settingsWindow.currentMaxTransfert);
                //Kill all thread and remove the lines
                foreach (int el in GlobalVariables.currentSaveProcess.Keys)
                {
                    Model.actionOnSave(el, "kill");
                    
                }
                Trace.WriteLine("Kill all threads!");
                ProgressBarList.Clear();
                ResultText = model.getMessage("{{ app.printer.noData }}");
                OnPropertyChanged("AppPrinterCalc");
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
                    Model.actionOnSave(int.Parse(part), "break");
                    GlobalVariables.vm.EditMessageOnProgressBar(name, _progressBarValue + "% - {{ thread.paused }}");
                    GlobalVariables.srv.Send("/SaveBreak Save " + part);

                }
                else
                {
                    //Boot logic
                    Model.actionOnSave(int.Parse(part), "unbreak");
                    GlobalVariables.vm.EditMessageOnProgressBar(name, _progressBarValue + "%");
                    GlobalVariables.srv.Send("/SaveUnbreak Save " + part);
                }
            }
            IsPlaying = !IsPlaying;
        }

        private void ExecuteStopCommand(object parameter)
        {
            //Stop logic
            string name = parameter.ToString();
            string part = name.Split(' ')[1];
            Model.actionOnSave(int.Parse(part), "kill");
            GlobalVariables.vm.EditMessageOnProgressBar(name, "{{ thread.killed }}");
            GlobalVariables.srv.Send("/SaveKilled Save " + part);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
