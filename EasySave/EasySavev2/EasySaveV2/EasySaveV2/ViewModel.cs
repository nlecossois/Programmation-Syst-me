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
            return true;
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
                    //Récupération des sauvegardes à exécuter et du nombre total de sauvegarde
                    string finalUserPrompt = model.GetSaveData(formatUserPrompt);
                    if (finalUserPrompt.Contains("error"))
                    {
                        ResultText = model.getMessage(finalUserPrompt);
                    } else
                    {
                        //A partir d'ici, ce morceau de code correspond à la trame du programme
                        //On commence par extraire les informations en un tableau d'entier et un entier
                        int amountOfSaves = model.extractUserPrompt(finalUserPrompt, 1);
                        List<int> listOfSaves = model.extractUserPrompt(finalUserPrompt);
                        //On réalise l'action pour initialiser et charger les barres et renvoyer un ResultText vide.

                        ResultText = "";
                        //- LoadProgressBar(amountOfSaves);

                        //On appel la méthode du model qui va se charger de threader chaque sauvegarde et de gérer la liste d'attente
                        model.SemaphoreWaitList(listOfSaves);

                        //On active le Thread qui attend les commandes en provenances du model
                        Thread messageGetter = new Thread(new ParameterizedThreadStart(listenData));
                        messageGetter.Start("messageGetter");

                        
                    }
                }
            }
        }

        //Méthode qui attend de recevoir une instruction puis qui l'affiche sous forme de messageBox
        private void listenData(Object messageGetter)
        {
    
            while (GlobalVariables.saveThreadProcess != 0)
            {
                if (GlobalVariables.dataTransfert.Count != 0)
                {
                    //Récupération de la première commande
                    string cmd = GlobalVariables.dataTransfert[0];
                    //On retire cette commande de la liste
                    GlobalVariables.dataTransfert.RemoveAt(0);

                    //Action à réaliser en récéption de la commande
                    MessageBoxResult displayer = MessageBox.Show(cmd, "Return from Model", MessageBoxButton.OK, MessageBoxImage.Information);

                    //C'est ici que l'on appelera la mise a jour de la barre, des boutons etc en fonction de la commande exécutée
                    
                }
                else
                {
                    Thread.Sleep(500);
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
}
