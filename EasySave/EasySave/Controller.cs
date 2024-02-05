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

            //The user is asked what they want to save
            string userPrompt = appView.promptConsole("Séquence de sauvegarde ?");

            //Construction of a data table in the model.
            //This table contains all the files to save.
            Array formatedPrompt = appModel.formatUserPrompt(userPrompt);

            foreach (int element in formatedPrompt)
            {
                appView.sendConsole(element.ToString());
            }

            



        }
    }
}
