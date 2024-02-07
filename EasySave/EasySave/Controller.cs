using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySave
{
    class Controller
    {   
        public Controller()
        {
            //Creation of an object which will contain our view
            View appView = new View();

            //Creation of an object which will contain our model
            Model appModel = new Model();

            //Ask the user for the language
            string currentLang = appView.promptConsole(appModel.getMessage("message.chooseLang", appModel.getLang())).ToUpper();

            //Apply this language to the entire program
            if (currentLang == "FR" || currentLang == "EN") 
            {
                //We call the run method which launches the application
                run(appView, appModel, currentLang);

            }
            else
            {
                //If the language does not exist: error + restart the program
                appView.sendConsole(appModel.getMessage("error.chooseLang", appModel.getLang()));
                new Controller();
            }

        }

        public void run(View appView, Model appModel, string currentLang)
        {
            //We actually define the new language
            appModel.setLang(currentLang);


            //The user is asked what they want to save
            string userPrompt = appView.promptConsole(appModel.getMessage("message.promptSequence", appModel.getLang()));


            //Formatting user-sent data
            string formatPrompt = appModel.formatUserPrompt(userPrompt);

            //Error checking
            if (formatPrompt.Contains("error"))
            {
                //Send error message to console
                appView.sendConsole(appModel.getMessage(formatPrompt, appModel.getLang()));
                //We call a new run method to ask the user for a new backup sequence.
                run(appView, appModel, currentLang);
            }
            else
            {
                //Transformation of the character string into a list and save the prompted files
                string filesSaved = appModel.SaveFolder(appModel.StringToList(formatPrompt));

                //Converting feedback messages to messages from the lang file
                string[] partsOfReturn = filesSaved.Split("//");
                //For each element of the array except the last (which is just an empty line), we send the corresponding message in the correct language to the console via the controller.
                int lastIndex = partsOfReturn.Length - 1;
                for (int i = 0; i < lastIndex; i++)
                {
                    string message = partsOfReturn[i];

                    if (message.Contains("message."))
                    {
                        appView.sendConsole(appModel.getMessage(message, appModel.getLang()), true);
                    }
                    else
                    {
                        appView.sendConsole(message, true);
                    }

                    
                }
            }
        }

    }
}
