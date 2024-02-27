using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        private string _inputText;
        private string _resultText;
        private bool _processed = false;


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
        }

        private bool CanExecute(object parameter)
        {
            //Logic to determine if the command can be executed
            if(GlobalVariables.saveThreadProcess == 0 && _processed == false)
            {
                return true;
            } else
            {
                return false;
            }
            
        }

        private void Click(object parameter)
        {
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
                        _processed = true;
                        //From here, this piece of code corresponds to the program frame
                        //We start by extracting the information into an array of integers and an integer
                        int amountOfSaves = model.extractUserPrompt(finalUserPrompt, 1);
                        List<int> listOfSaves = model.extractUserPrompt(finalUserPrompt);
                        //We perform the action to initialize and load the bars and return an empty ResultText.

                        ResultText = "";
                        //- LoadProgressBar(amountOfSaves);

                        //We call the model method which will take care of threading each backup and managing the waiting list
                        model.SemaphoreWaitList(listOfSaves);

                        //We activate the Thread which waits for commands coming from the model
                        Thread messageGetter = new Thread(new ParameterizedThreadStart(listenData));
                        messageGetter.Start("messageGetter");
                    }
                }
            }
        }

        //Method that waits to receive an instruction from the Saves
        private void listenData(Object messageGetter)
        {
    
            while (GlobalVariables.saveThreadProcess != 0)
            {
                if (GlobalVariables.dataTransfert.Count != 0)
                {
                    //Collecting the first order
                    string cmd = GlobalVariables.dataTransfert[0];
                    //Remove this command from the list
                    GlobalVariables.dataTransfert.RemoveAt(0);

                    //Action to be carried out upon receipt of the order
                    
                    
                }
                else
                {
                    Thread.Sleep(500);
                }
            }
            _processed = false;

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
                selectedScriptingTypes = settingsWindow.selectedItems;
                lang = settingsWindow.SelectedLanguage;
                logType = settingsWindow.SelectedLogType;
                copyType = settingsWindow.SelectedCopyType;
                model.setLogFormat(logType);
                model.setCurrentJobApp(settingsWindow.currentJobApp);
                model.setLang(lang);
                model.setCopyMethod(copyType);
                model.setEncryptFileType(selectedScriptingTypes);
                OnPropertyChanged("AppPrinterCalc");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
