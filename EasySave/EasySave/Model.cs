using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EasySave
{
    class Model
    {
        private string textOutput = "";
        private List<string> allSaveFileNames = new List<string>();

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
                            else if (int.TryParse(part, out int number) && number <= 5 && number >= 1)
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
      public string SaveFolder(List<int> workOfSave)
        { 
            foreach (int index in workOfSave)
            {
                allSaveFileNames.Clear();
                string sourcePath = GetParentDirectory(GetFolderPath("Source" + index), "Source" + index);
                createFolder(sourcePath, index);
                string destinationPath = GetParentDirectory(GetFolderPath("Destination" + index), "Destination" + index);
                if (sourcePath != null)
                {
                    addFiles(sourcePath, destinationPath, index);
                    addFolders(sourcePath, destinationPath, index);
                    if (allSaveFileNames.Count() > 0)
                    {
                        textOutput += "The files of " + "Source" + index + ":" + Environment.NewLine + string.Join(Environment.NewLine, allSaveFileNames) + " have been copied." + Environment.NewLine;
                    }
                }
                else
                {
                    textOutput = "Folder Source" + index + " not found check if your folders named 'Source+number'(ex: Source1)";
                }
            }
            return textOutput;
        }

        public void addFiles(string sourcePath, string destinationPath, int index)
        {
            string[] allFiles = Directory.GetFiles(sourcePath);
            foreach (string filePath in allFiles)
            {
                addFile(destinationPath, filePath);
            }
        }

        public void addFile(string destinationPath, string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            string fileDestinationPath = Path.Combine(destinationPath, fileName);
            try
            {
                File.Copy(filePath, fileDestinationPath, true);
                allSaveFileNames.Add(fileName);
            }
            catch (Exception err)
            {
                textOutput += err.Message + Environment.NewLine;
            }
        }

        public void addFolders(string sourcePath, string destinationPath, int index)
        {
            string[] subDirectories = GetSubDirectories(sourcePath);
            foreach (string folder in subDirectories)
            {
                string newPath = addFolder(destinationPath, folder);
                addFiles(folder, newPath, index);
                addFolders(folder, newPath, index);
            }
        }

        public string addFolder(string destinationPath, string folderPath)
        {
            string newPath = Path.Combine(destinationPath, Path.GetFileName(folderPath));
            if (!File.Exists(newPath))
            {
                Directory.CreateDirectory(newPath);
            }           
        return newPath;
        }

        static string GetFolderPath(string folderName)
        {
            string path = null;
            // Get path user desktop folders
            string userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Desktop";
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

        static string GetParentDirectory(string path, string name)
        {
            string parentDir = path;
            if (path != null)
            {
                if (Path.GetFileName(path) != name)
                {
                   parentDir = GetParentDirectory(Path.GetDirectoryName(path), name);
                }
            }
           
            return parentDir;
        }

        static string[] GetSubDirectories(string parentFolderPath)
        {
            // Use Directory.GetDirectories to get the list of folders
            string[] subDirectories = Directory.GetDirectories(parentFolderPath);

            return subDirectories;
        }
      
        static string SearchFolderInDrives(string folderName)
        {
            DriveInfo[] drives = DriveInfo.GetDrives();

            foreach (DriveInfo drive in drives)
            {
                if (drive.DriveType == DriveType.Fixed || drive.DriveType == DriveType.Removable || drive.DriveType == DriveType.Network)
                {
                    string foundPath = SearchFolderInDrive(folderName, drive.RootDirectory);
                    if (!string.IsNullOrEmpty(foundPath))
                    {
                        return foundPath; //  Stop search if folder found
                    }
                }
            }
            return string.Empty; // No folder found
        }

        static string SearchFolderInDrive(string folderName, DirectoryInfo currentDirectory)
        {
            try
            {
                // Search in current directory
                string[] matchingDirectories = Directory.GetDirectories(currentDirectory.FullName, folderName, SearchOption.TopDirectoryOnly);

                if (matchingDirectories.Length > 0)
                {
                    return matchingDirectories[0]; // Return the first folder found
                }

                // Recursive search in subdirectories
                DirectoryInfo[] subDirectories = currentDirectory.GetDirectories();
                foreach (DirectoryInfo subDirectory in subDirectories)
                {
                    string foundPath = SearchFolderInDrive(folderName, subDirectory);

                    if (!string.IsNullOrEmpty(foundPath))
                    {
                        return foundPath; // Stop search if folder found
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
            }
            return string.Empty; // No folder found
        }

        public void createFolder(string path, int index)
        {
            Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(path), "Destination" + index));
        }
    }
}
