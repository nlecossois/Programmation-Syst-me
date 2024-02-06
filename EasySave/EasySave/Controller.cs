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

            
            //Formatage des données envoyées par l'utilisateur
            string formatPrompt = appModel.formatUserPrompt(userPrompt);

            //Contrôle d'erreur
            if(formatPrompt.Contains(" : "))
            {
                //Envoie du message d'erreur dans la console
                appView.sendConsole(formatPrompt);
            }
            else
            {
                //Transformation de la chaine de caractère en tableau
                List<int> promptFinal = appModel.StringToArray(formatPrompt);

                foreach(int element in promptFinal)
                {
                    appView.sendConsole(element.ToString());
                }
            }
        }
    }
}
