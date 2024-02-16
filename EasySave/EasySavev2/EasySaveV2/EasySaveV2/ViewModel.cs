using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
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
        public List<string> selectedScriptingTypes = new List<string>();
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

        public bool processIsLaunch(string process)
        {
            return model.processIsLaunch(process);
        }

        public ViewModel()
        {
            OpenSettingsCommand = new RelayCommand(OpenSettings);
            SaveWork = new RelayCommand(Click, CanExecute);
        }

        private bool CanExecute(object parameter)
        {
            // Logique pour déterminer si la commande peut être exécutée
            return true;
        }

        private void Click(object parameter)
        {
            if(InputText == null)
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
                    ResultText = model.getMessage(model.SaveFolder(model.StringToList(formatUserPrompt)));
                }
            }
            
        }
        private void OpenSettings(object parameter)
        {
            // Créez une instance de votre fenêtre de paramètres
            SettingsWindow settingsWindow = new SettingsWindow(this);

            // Affichez la fenêtre en mode dialogue
            bool? result = settingsWindow.ShowDialog();

            // Vous pouvez vérifier le résultat si nécessaire (par exemple, si l'utilisateur a appuyé sur OK ou Annuler)
            if (result == true)
            {
                // Logique à exécuter si l'utilisateur a appuyé sur OK
                selectedScriptingTypes = settingsWindow.selectedItems;
                lang = settingsWindow.SelectedLanguage;
                logType = settingsWindow.SelectedLogType;
                model.setLogFormat(logType);
                model.setCurrentJobApp(settingsWindow.currentJobApp);
                model.setLang(lang);
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
