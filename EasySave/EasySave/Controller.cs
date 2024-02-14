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
                //We call the run method which launches the application
                run(appView, appModel, currentLang);

            }
            else
            {
                //If the language does not exist: error + restart the program
                appView.sendConsole(appModel.getMessage("{{ error.chooseLang }}"));
                new Controller();
            }

        }

        public void run(View appView, Model appModel, string currentLang)
        {
            //We actually define the new language
            appModel.setLang(currentLang);


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
                run(appView, appModel, currentLang);
            }
            else
            {
                //Sends the copy result to the user
                appView.sendConsole(appModel.getMessage(appModel.SaveFolder(appModel.StringToList(formatPrompt))));
            }
        }

    }
}
