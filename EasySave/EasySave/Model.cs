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
        public string formatUserPrompt(string userPrompt)
        {
            //We start by declaring our return table
            string formatedData = "Erreur : Vous n'avez fourni aucune sauvegarde.";
            if (userPrompt == "*")
            {
                //Static case where we have all the backups. (*)
                formatedData = "1, 2, 3, 4, 5";
            }
            else
            {
                if(userPrompt.Length == 1)
                {
                    if(userPrompt == "1" || userPrompt == "2" || userPrompt == "3" || userPrompt == "4" || userPrompt == "5")
                    {
                        //Case where we have a precise backup.
                        formatedData = userPrompt;
                    }
                    else
                    {
                        //Error
                        formatedData = "Erreur : Ce nom de sauvegarde n'est pas reconnu.";
                    }
                }
                else
                {

                    if (string.IsNullOrEmpty(userPrompt))
                    {
                        formatedData = "Erreur : Vous n'avez fourni aucune sauvegarde.";
                    }
                    else
                    {
                        string[] parts = userPrompt.Split(';', StringSplitOptions.RemoveEmptyEntries);

                        var result = parts.SelectMany(part =>
                        {
                            if (part.Contains('-'))
                            {
                                string[] range = part.Split('-');
                                if (range.Length != 2 || !int.TryParse(range[0], out int start) || !int.TryParse(range[1], out int end) || start >= end || start < 1 || end > 5)
                                    return new string[] { "Erreur : La syntaxe pour utiliser '-' doit contenir deux nombres compris entre 1 et 5 tel que le premier est inférieur au second." };
                                return Enumerable.Range(start, end - start + 1).Select(i => i.ToString());
                            }
                            else if (int.TryParse(part, out int number))
                            {
                                return new string[] { number.ToString() };
                            }
                            else
                            {
                                return new string[] { "Erreur : La syntaxe pour utiliser ';' doit contenir des nombres compris entre 1 et 5." };
                            }
                        });

                        formatedData = string.Join(", ", result);
                    }
                }
            }
            return formatedData;
        }


        //Method which transforms the received character string into an array.
        //This function does not present any error cases because here we are sure that the string only contains integers.
        public List<int> StringToArray(string input)
        {
            // Divide the string into substrings using the comma as separator
            string[] subInput = input.Split(',');
            // Create an array to store integers
            List<int> resultArray = new List<int>(subInput.Length);
            // Convert substrings to integers and store in array
            for (int i = 0; i < subInput.Length; i++)
            {
                if (int.TryParse(subInput[i].Trim(), out int valeur))
                {
                    resultArray.Add(valeur);
                }
            }
            //We return the value
            return resultArray;
        }
    }
}
