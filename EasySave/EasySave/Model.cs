using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySave
{
    class Model
    {
        //Method that transforms the user's request from a character string to an array.
        public Array formatUserPrompt(string userPrompt)
        {
            //On commence par déclarer notre tableau de retour
            int[] formatedData = null;


            if (userPrompt == "*")
            {
                //Cas statique où l'on a toutes les sauvegardes. (*)
                formatedData = new int[] { 1, 2, 3, 4, 5 };

            }
            else
            {
                if(userPrompt.Length == 1)
                {
                    //Cas où l'on a une sauvegarde précise.
                    //Exception de sauevgarde précise

                }
                else
                {

                }
            }







            //Cas où l'on a un intervalle de sauvegarde. (-)

            //Cas où l'on a plusieurs sauvegardes spécifiques. (;)

            //Exception


            //On contrôle si notre tableau existe et on le renvoie.
            //Sinon on renvoie un tableau vide.

            return formatedData;


        }

    }
}
