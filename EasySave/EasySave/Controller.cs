using System;


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
            string currentLang = appView.promptConsole(appModel.getMessage("{{ message.chooseLang }}")).ToUpper();

            //Apply this language to the entire program
            if (currentLang == "FR" || currentLang == "EN") 
            {

                //We actually define the new language
                appModel.setLang(currentLang);

                //We call the run method which prompt user to choose log format
                logChoice(appView, appModel);

            }
            else
            {
                //If the language does not exist: error + restart the program
                appView.sendConsole(appModel.getMessage("{{ error.chooseLang }}"));
                new Controller();
            }

        }

        public void logChoice(View appView, Model appModel)
        {

            string logFormat = appView.promptConsole(appModel.getMessage("{{ message.logFormat }}")).ToUpper();
            if (logFormat == "JSON" || logFormat == "XML")
            {
                //We save the choice of the user
                appModel.setLogFormat(logFormat);

                //We call the run method which launches the application
                methodChoice(appView, appModel);
            } else
            {
                appView.sendConsole(appModel.getMessage("{{ error.invalidLogFormat }}"));
                logChoice(appView, appModel);
            }
            
        }


        public void methodChoice(View appView, Model appModel)
        {

            //Ask the user for the backup type
            string backupType = appView.promptConsole(appModel.getMessage("{{ message.backupType }}"));
            //We check if the type is compliant and we define it in the model
            if (backupType == "0" || backupType == "1")
            {
                if (backupType == "0")
                {
                    appModel.setCopyMethod(false);
                }
                else
                {
                    appModel.setCopyMethod(true);
                }

                run(appView, appModel);
            }
            else
            {
                appView.sendConsole(appModel.getMessage("{{ error.backupType }}"));
                methodChoice(appView, appModel);
            }

        }



        public void run(View appView, Model appModel)
        {
            //The user is asked what they want to save
            string userPrompt = appView.promptConsole(appModel.getMessage("{{ message.promptSequence }}"));

            //Formatting user-sent data
            string formatPrompt = appModel.formatUserPrompt(userPrompt);

            //Error checking
            if (formatPrompt.Contains("error"))
            {
                //Send error message to console
                appView.sendConsole(appModel.getMessage(formatPrompt));
                //We call a new run method to ask the user for a new backup sequence.
                run(appView, appModel);
            }
            else
            {
                //Sends the copy result to the user
                appView.sendConsole(appModel.getMessage(appModel.SaveFolder(appModel.StringToList(formatPrompt))));
            }
        }

    }
}
