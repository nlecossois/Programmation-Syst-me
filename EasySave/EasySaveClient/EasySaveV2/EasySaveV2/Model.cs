using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.IO;
using System.Text.Json;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Threading;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;

namespace EasySaveV2
{
    //Global variables used
    public static class GlobalVariables
    {
        public static ViewModel vm;
        public static Client clt;
    }


    class Model
    {
        private string currentLanguage = "EN";


        //Method that retrieves messages from the lang file
        public string getMessage(string message)
        {
            //Recovering the parent folder
            string parentDirectory = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
            //Recovery of the lang file
            string filePath = Path.Combine(parentDirectory, $"lang/{currentLanguage}.json");
            try
            {
                //Retrieving file contents
                string jsonString = File.ReadAllText(filePath);
                //Serializing file contents
                JObject json = JObject.Parse(jsonString);
                //Definition of the search pattern
                string pattern = @"\{\{(.+?)\}\}";
                //Searching for possible patterns and adding them to a collection
                MatchCollection matches = Regex.Matches(message, pattern);
                //For each pattern
                foreach (Match match in matches)
                {
                    //Recover the key by removing spaces around it
                    string key = match.Groups[1].Value.Trim();
                    //Retrieve the corresponding value in the JSON
                    string replacement = json[key]?.ToString() ?? match.Value;
                    //Replace in input string
                    message = message.Replace(match.Value, replacement);
                }
                //We return the result


            }
            catch (Exception ex)
            {
                message = $"Error : {ex.Message}" + Environment.NewLine + "The application failed to run !";
            }
            return message;
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }
    }
}