using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EasySave
{
    class Model
    {
        public string SaveFolder(List<int> workOfSave)
        {
            string textOutput = "";
            List<string> allName = new List<string>();
            foreach (int i in workOfSave)
            {
                string folder = "Test" + i;
                string path = GetFolderPath(folder);
                if (path != null)
                {
                    string[] test = Directory.GetFiles(path);
                    foreach(string name in test)
                    {
                        allName.Add(Path.GetFileName(name));
                        textOutput = "The files :" + Environment.NewLine + string.Join(Environment.NewLine, allName) + Environment.NewLine + "have been copied.";
                    }
                }
                else
                {
                    textOutput = "No folders found";
                }
            }
            return textOutput;
        }

        static string GetFolderPath(string folderName)
        {
            string path = null;
            // Get path user desktop folders
            string userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)+"\\Desktop";

            // Get all folders in the desktop folder
            string[] allFolders = Directory.GetDirectories(userFolder, "*", SearchOption.AllDirectories);

            // Search for the folder with the specified name
            foreach (string i in allFolders)
            {
                if (i.Contains(folderName))
                {
                     path = i;
                }
            }
            return path;
        }
    }
}
