using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace EasySaveV2
{
    public partial class SettingsWindow : Window
    {
        private ViewModel viewModel;
        public string SelectedLanguage { get; set; }
        public string currentJobApp;
        private List<string> _selectedScriptingTypes;
        public string SelectedLogType { get; set; }
        public string SelectedCopyType { get; set; }
        public ICommand SaveCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }

        public List<string> Languages { get; set; }
        public List<string> logType { get; set; }
        public List<string> copyType { get; set; }
        public List<string> selectedItems { get; set; }
        private ObservableCollection<string> scriptingType;
        public ObservableCollection<string> ScriptType
        {
            get { return scriptingType; }
            set
            {
                if (scriptingType != value)
                {
                    scriptingType = value;
                    OnPropertyChanged(nameof(ScriptType));
                }
            }
        }
        public string AppPrintLanguage { get; set; }
        public string AppPrintCryptType { get; set; }
        public string AppPrintLogType { get; set; }
        public string AppPrintJobApp { get; set; }
        public string AppPrintSaveParam { get; set; }
        public string AppPrintCancelParam { get; set; }

        public string AppPrintCopyType { get; set; }
        public string CurrentJobApp { get; set; }





        public SettingsWindow(ViewModel viewModel)
        {
            this.viewModel = viewModel;
            Languages = new List<string>();
            logType = new List<string>();
            copyType = new List<string>();
            selectedItems = new List<string>();
            

            InitializeComponent();
            Setup();
            
        }
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            //Code to run when checkbox is checked
            UpdateSelection();
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            //Code to run when checkbox is unchecked
            UpdateSelection();
        }

        private void UpdateSelection()
        {
            //Update the selection manually
            ScriptingType.SelectedItems.Clear();
            selectedItems.Clear();
            foreach (ScriptTypeItem item in ScriptingType.Items)
            {
                if (item.IsSelected)
                {
                    ScriptingType.SelectedItems.Add(item);
                }
            }

            //Access to selected items
            foreach (ScriptTypeItem selectedItem in ScriptingType.SelectedItems)
            {
                selectedItems.Add(selectedItem.ScriptTypeName);
            }
        }


        private void SaveSettings(object parameter)
        {
            SelectedLanguage = Language.SelectedItem as string;
            SelectedLogType = LogType.SelectedItem as string;
            SelectedCopyType = CopyType.SelectedItem as string;
            DialogResult = true;
            currentJobApp = InputTextBox.Text;
            //This will close the window with a positive result (true).
        }

        private void CancelSettings(object parameter)
        {
            //Logic to undo changes
            DialogResult = false; //This will close the window with a negative result (false).
        }
        // Setup of the parameters windows
        private void Setup()
        {
            AppPrintLanguage = viewModel.getMessageFromParameter("{{ app.printer.language }}");
            AppPrintCryptType = viewModel.getMessageFromParameter("{{ app.printer.cryptType }}");
            AppPrintLogType = viewModel.getMessageFromParameter("{{ app.printer.logType }}");
            AppPrintJobApp = viewModel.getMessageFromParameter("{{ app.printer.jobApp }}");
            AppPrintSaveParam = viewModel.getMessageFromParameter("{{ app.printer.saveParam }}");
            AppPrintCancelParam = viewModel.getMessageFromParameter("{{ app.printer.cancelParam }}");
            AppPrintCopyType = viewModel.getMessageFromParameter("{{ app.printer.copyType }}");
            logType.Add("Json");
            logType.Add("Xml");
            copyType.Add(viewModel.getMessageFromParameter("{{ printer.copyType.complete }}"));
            copyType.Add(viewModel.getMessageFromParameter("{{ printer.copyType.differential }}"));
            GetAllLanguage();
            CurrentJobApp = viewModel.getJobApp();
            Language.SelectedItem = Languages.Contains(viewModel.lang, StringComparer.OrdinalIgnoreCase) ? viewModel.lang : "EN";
            LogType.SelectedItem = logType.Contains(viewModel.logType, StringComparer.OrdinalIgnoreCase) ? viewModel.logType : "Json";
            CopyType.SelectedItem = copyType.Contains(viewModel.copyType, StringComparer.OrdinalIgnoreCase) ? viewModel.copyType : viewModel.getMessageFromParameter("{{ printer.copyType.complete }}");
            List<ScriptTypeItem> scriptTypeList = new List<ScriptTypeItem>
            {
                new ScriptTypeItem { ScriptTypeName = "apk", IsSelected = false },
                new ScriptTypeItem { ScriptTypeName = "csv", IsSelected = false },
                new ScriptTypeItem { ScriptTypeName = "css", IsSelected = false },
                new ScriptTypeItem { ScriptTypeName = "cpp", IsSelected = false },
                new ScriptTypeItem { ScriptTypeName = "cs", IsSelected = false },
                new ScriptTypeItem { ScriptTypeName = "db", IsSelected = false },
                new ScriptTypeItem { ScriptTypeName = "dll", IsSelected = false },
                new ScriptTypeItem { ScriptTypeName = "docx", IsSelected = false },
                new ScriptTypeItem { ScriptTypeName = "exe", IsSelected = false },
                new ScriptTypeItem { ScriptTypeName = "gif", IsSelected = false },
                new ScriptTypeItem { ScriptTypeName = "html", IsSelected = false },
                new ScriptTypeItem { ScriptTypeName = "ini", IsSelected = false },
                new ScriptTypeItem { ScriptTypeName = "iso", IsSelected = false },
                new ScriptTypeItem { ScriptTypeName = "jar", IsSelected = false },
                new ScriptTypeItem { ScriptTypeName = "java", IsSelected = false },
                new ScriptTypeItem { ScriptTypeName = "js", IsSelected = false },
                new ScriptTypeItem { ScriptTypeName = "json", IsSelected = false },
                new ScriptTypeItem { ScriptTypeName = "jpg", IsSelected = false },
                new ScriptTypeItem { ScriptTypeName = "jpeg", IsSelected = false },
                new ScriptTypeItem { ScriptTypeName = "lib", IsSelected = false },
                new ScriptTypeItem { ScriptTypeName = "log", IsSelected = false },
                new ScriptTypeItem { ScriptTypeName = "m4v", IsSelected = false },
                new ScriptTypeItem { ScriptTypeName = "mp3", IsSelected = false },
                new ScriptTypeItem { ScriptTypeName = "mp4", IsSelected = false },
                new ScriptTypeItem { ScriptTypeName = "pdf", IsSelected = false },
                new ScriptTypeItem { ScriptTypeName = "php", IsSelected = false },
                new ScriptTypeItem { ScriptTypeName = "png", IsSelected = false },
                new ScriptTypeItem { ScriptTypeName = "ppt", IsSelected = false },
                new ScriptTypeItem { ScriptTypeName = "pptx", IsSelected = false },
                new ScriptTypeItem { ScriptTypeName = "py", IsSelected = false },
                new ScriptTypeItem { ScriptTypeName = "rar", IsSelected = false },
                new ScriptTypeItem { ScriptTypeName = "rtf", IsSelected = false },
                new ScriptTypeItem { ScriptTypeName = "sh", IsSelected = false },
                new ScriptTypeItem { ScriptTypeName = "sql", IsSelected = false },
                new ScriptTypeItem { ScriptTypeName = "tar.gz", IsSelected = false },
                new ScriptTypeItem { ScriptTypeName = "ts", IsSelected = false },
                new ScriptTypeItem { ScriptTypeName = "txt", IsSelected = false },
                new ScriptTypeItem { ScriptTypeName = "wav", IsSelected = false },
                new ScriptTypeItem { ScriptTypeName = "webp", IsSelected = false },
                new ScriptTypeItem { ScriptTypeName = "xls", IsSelected = false },
                new ScriptTypeItem { ScriptTypeName = "xlsx", IsSelected = false },
                new ScriptTypeItem { ScriptTypeName = "xml", IsSelected = false },
                new ScriptTypeItem { ScriptTypeName = "xaml", IsSelected = false },
                new ScriptTypeItem { ScriptTypeName = "yaml", IsSelected = false },
                new ScriptTypeItem { ScriptTypeName = "zip", IsSelected = false },
            };
            foreach (ScriptTypeItem item in scriptTypeList)
            {
                //Check if the item should be selected by default
                if (viewModel.selectedScriptingTypes.Contains(item.ScriptTypeName, StringComparer.OrdinalIgnoreCase))
                {
                    item.IsSelected = true;
                }
            }
            //Initialize the list with ScriptingTypeItem objects
            ScriptingType.ItemsSource = scriptTypeList;
            SaveCommand = new RelayCommand(SaveSettings);
            CancelCommand = new RelayCommand(CancelSettings);
            DataContext = this;
        }

        private void GetAllLanguage()
        {
            string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            //Go back several levels to get the project folder
            string projectDirectory = Directory.GetParent(currentDirectory).Parent.Parent.FullName;

            //Combine the path with the "lang" folder
            string langFolderPath = Path.Combine(projectDirectory, "lang");
            foreach (string lang in Directory.GetFiles(langFolderPath))
            {
                Languages.Add(Path.GetFileNameWithoutExtension(lang));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    public class ScriptTypeItem
{
    public string ScriptTypeName { get; set; }
    public bool IsSelected { get; set; }
}
}
