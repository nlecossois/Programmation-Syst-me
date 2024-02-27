using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        public ICommand TogglePlayPauseCommand => new RelayCommand(ExecuteTogglePlayPauseCommand);
        public ICommand StopCommand => new RelayCommand(ExecuteStopCommand);
        public string lang;
        public string logType;
        public string copyType;
        public List<string> selectedScriptingTypes = new List<string>();

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
                }
                
            }
        }
        private string _inputText;
        private string _resultText;

        public string AppPrinterCalc
        {
            get { return model.getMessage("{{ app.printer.calc }}"); }
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
       
        private void ExecuteTogglePlayPauseCommand(object parameter)
        {
            string name = parameter.ToString();
            string part = name.Split(' ')[1];
            if (IsPlaying)
                {
                    if (parameter != null)
                    {
                    
                    
                    model.actionOnSave(int.Parse(part), "break");

                    // Logique de pause
                    // Exemple : Pause la lecture

                }
            }
                else
                {
                    // Logique de démarrage
                    // Exemple : Démarre la lecture
                    if (parameter != null)
                    {
                    

                    model.actionOnSave(int.Parse(part), "unbreak");
                    // Logique de pause
                    // Exemple : Pause la lecture

                }
            }
            IsPlaying = !IsPlaying;
        }
        

        private void ExecuteStopCommand(object parameter)
        {
            // Logique d'arrêt (par exemple, arrêter complètement la lecture)
            // Réinitialisez également la propriété IsPlaying si nécessaire
            
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
            ProgressBarList = new ObservableCollection<ProgressBarElement> {};

            
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

                        //On vide d'abord la liste des progressbar
                        ProgressBarList.Clear();

                        //On affiche le bon nombre de barre de progression
                        foreach (int index in listOfSaves)
                        {
                            ProgressBarList.Add(
                            new ProgressBarElement { Name = "Save " + index, ProgressBarValue = 0 }
                        );}
                        //We call the model method which will take care of threading each backup and managing the waiting list
                        model.SemaphoreWaitList(listOfSaves);
                        GlobalVariables.vm = this;
                    }
                }
            }
        }

        //Méthode pour kill tous les threads en cours d'exécution
        public void killAllThreads()
        {
            foreach(int el in GlobalVariables.currentSaveProcess.Keys)
            {
                model.actionOnSave(el, "kill");
            }
        }

        //Méthode pour mettre à jour la progress bar en fonction de son nom
        public void EditProgressBarValue(string name, int newValue)
        {
            //Parcourir la collection ProgressBarList
            foreach (var element in ProgressBarList)
            {
                //Vérifier si le nom correspond
                if (element.Name == name)
                {
                    //Mettre à jour la valeur de la barre de progression
                    element.ProgressBarValue = newValue;
                    element.FormattedProgressBarValue = "" + newValue;
                    //Sortir de la boucle car nous avons trouvé l'élément
                    break;
                }
            }
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
    public class ProgressBarElement: INotifyPropertyChanged
    {
        public string Name { get; set; }

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

        private string _formattedValue;
        public string FormattedProgressBarValue
        {
            get
            {
                return $"{_formattedValue}%";
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

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }




    }
}
