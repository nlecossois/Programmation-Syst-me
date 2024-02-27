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

namespace EasySaveV2
{
    //Global variables used
    public static class GlobalVariables
    {
        public static List<string> dataTransfert = new List<string>();
        public static int saveThreadProcess = 0;
        public static Dictionary<int, Save> currentSaveProcess = new Dictionary<int, Save>();
        public static double currentTransfertSize = 0;
    }

    public class Save
    {
        private static Mutex mutexLog;
        private int saveIndex;
        private bool isBreak;
        private bool run;
        private int left;
        private int copied;
        private int total;
        private int sizeLeft;
        private int totalSize;

        //Constructor that initializes the index and calls backup start
        public Save(int index, string jobApp, string logFormat, bool differential, List<string> selectedCryptFileType, List<string> selectedPriorityFileType, double maxSameTimeSize, Barrier barrierPrioritaryFiles)
        {
            this.saveIndex = index;
            this.isBreak = false;
            this.run = true;
            this.left = 0;
            this.total = 0;
            this.copied = 0;
            //Adding the object to the list of current backups
            GlobalVariables.currentSaveProcess.Add(saveIndex, this);
            this.StartSave(jobApp, logFormat, differential, selectedCryptFileType, selectedPriorityFileType, maxSameTimeSize, barrierPrioritaryFiles);
        }

        //Carrying out actions on backups
        public static void SaveInteract(int index, string action)
        {
            //The method will only perform an action if the backup is in progress
            if (GlobalVariables.currentSaveProcess.TryGetValue(index, out Save currentSave))
            {
                if(action == "break")
                {
                    currentSave.isBreak = true;
                } else if (action == "unbreak")
                {
                    currentSave.isBreak = false;
                } else if (action == "kill")
                {
                    currentSave.run = false;
                }
            }
        }

        //Method that sends data to the model
        private void DiffuseData(string data)
        {
            GlobalVariables.dataTransfert.Add(data);
        }

        //Method that returns the list of files in a source folder
        public List<string> GetAllFilesInFolder(string path)
        {
            List<string> inFolderFiles = new List<string>();
            foreach(string file in Directory.GetFiles(path))
            {
                inFolderFiles.Add(file);
            }
            //We recursively add the files to the subfolders
            foreach (string subfolder in Directory.GetDirectories(path))
            {
                List<string> getted = GetAllFilesInFolder(subfolder);
                foreach(string get in getted)
                {
                    inFolderFiles.Add(get);
                }
            }
            return inFolderFiles;
        }

        //Method that compares two tables and checks if there are matches
        public List<string> CompareFiles(List<string> sourceFiles, List<string> destinationFiles, int saveIndex)
        {
            List<string> copyFiles = new List<string>();
            foreach (string el in sourceFiles)
            {
                string sourceName = "Source" + saveIndex;
                int indexSource = el.IndexOf(sourceName);
                string compareEl = el.Substring(indexSource + sourceName.Length);
                if (!destinationFiles.Any(s => s.IndexOf(compareEl, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    copyFiles.Add(el);
                }
            }
            return copyFiles;
        }

        //Method that returns the list of priority/non-priority files based on parameters
        static public List<string> sortByPriority(List<string> toCopyFiles, int type, List<string> selectedPriorityFileType)
        {
            List<string> resultList = new List<string>();

            foreach (string el in toCopyFiles)
            {
                //We recover the extension
                string fileExtension = Path.GetExtension(el);
                //Remove the "."
                fileExtension = fileExtension.Substring(1);
                //we check if the extension is in the list
                if (type == 1)
                {
                    if (selectedPriorityFileType.Contains(fileExtension))
                    {
                        resultList.Add(el);
                    }
                }
                else
                {
                    if (!selectedPriorityFileType.Contains(fileExtension))
                    {
                        resultList.Add(el);
                    }
                }
            }
            return resultList;
        }

        //Method that returns subfolder paths based on the destination file path
        private List<string> GetSubString(string destPath, string filePath)
        {
            destPath = destPath + "\\";
            string tempPath = filePath.Replace("Destination" + this.saveIndex, "Destination");
            //Split the string into pieces separated by "/"
            string[] parts = tempPath.Split('\\');
            //Process that only retrieves the part of the link that interests us
            bool destPass = false;
            List<string> listSubString = new List<string>();
            foreach (string el in parts)
            {
                if (destPass == true)
                {
                    listSubString.Add(el);
                }
                else
                {
                    if (el == "Destination")
                    {
                        destPass = true;
                    }
                }

            }
            List<string> finalReturn = new List<string>();
            if (listSubString.Count() > 0)
            {
                //We remove the last element (which is our file)
                listSubString.RemoveAt(listSubString.Count - 1);

                int i = 1;

                //We will format the return values
                foreach (string el in listSubString)
                {
                    if (i == 1)
                    {
                        finalReturn.Add(destPath + el);
                    }
                    else if (i == 2)
                    {
                        finalReturn.Add(destPath + listSubString[0] + "\\" + el);
                    }
                    else if (i == 3)
                    {
                        finalReturn.Add(destPath + listSubString[0] + "\\" + listSubString[1] + "\\" + el);
                    }
                    else if (i == 4)
                    {
                        finalReturn.Add(destPath + listSubString[0] + "\\" + listSubString[1] + "\\" + listSubString[2] + "\\" + el);
                    }
                    else if (i == 5)
                    {
                        finalReturn.Add(destPath + listSubString[0] + "\\" + listSubString[1] + "\\" + listSubString[2] + "\\" + listSubString[3] + "\\" + el);
                    }

                    i++;
                }
            }
            return finalReturn;

        }

        //Method that manages the backup of a source folder
        private void StartSave(string jobApp, string logFormat, bool differential, List<string> selectedCryptFileType, List<string> selectedPriorityFileType, double maxSameTimeSize, Barrier barrierPrioritaryFiles)
        {
            //We initialize the mutex used to access our log and status files
            mutexLog = new System.Threading.Mutex(false);
            //We start by defining the total number of files to save (We also remove unnecessary files to copy in sequential mode)
            //We retrieve the path to the backup source
            string sourcePath = Model.GetParentDirectory(Model.GetFolderPath("Source" + saveIndex), "Source" + saveIndex);
            //We retrieve the path of the backup destination
            string destinationPath = Model.GetParentDirectory(Model.GetFolderPath("Destination" + saveIndex), "Destination" + saveIndex);
            //We initialize the table which will contain all the files which must be copied
            List<string> toCopyFiles = new List<string>();
            //We retrieve the list of files in the source
            List<string> sourceFiles = GetAllFilesInFolder(sourcePath);
            //If the mode is differential, then we compare the two lists and we remove the elements that we do not need to copy
            if (differential == true)
            {
                //We retrieve the list of files in the destination
                List<string> destinationFiles = GetAllFilesInFolder(destinationPath);
                //We make the comparison between the two tables
                toCopyFiles = CompareFiles(sourceFiles, destinationFiles, saveIndex);
            }
            else
            {
                toCopyFiles = sourceFiles;
            }
            //Now that we have the list of files to copy, we will divide it into two lists, the priority files and the other files
            List<string> PrioritaryToCopyFiles = sortByPriority(toCopyFiles, 1, selectedPriorityFileType);
            List<string> NormalToCopyFiles = sortByPriority(toCopyFiles, 0, selectedPriorityFileType);

            //We then build a new list with the priority files first and the normal files afterwards.
            List<string> filesToCopy = new List<string>();
            foreach (string el in PrioritaryToCopyFiles)
            {
                filesToCopy.Add(el);
            }
            foreach (string el in NormalToCopyFiles)
            {
                filesToCopy.Add(el);
            }

            //We record the total number of files to encrypt
            this.total = filesToCopy.Count();
            this.left = this.total;

            //We record the total size of the files to copy
            foreach(string path in filesToCopy)
            {
                string fileName = Path.GetFileName(path);
                this.totalSize += fileName.Length;
            }

            this.sizeLeft = this.totalSize;

            //For each file in our list
            foreach (string currentFile in filesToCopy)
            {
                //We check if the backup is still active
                if (this.run == true)
                {
                    //We check if the backup is paused: wait 1 second then try again
                    while (this.isBreak == true)
                    {
                        Thread.Sleep(1000);
                    }
                    //We check whether the business application is open or not, if so we wait a second and a half before trying again
                    while (Model.IsProcessOpen(jobApp))
                    {
                        Thread.Sleep(1500);
                    }
                    //We check if the file we are about to copy has priority
                    if (!PrioritaryToCopyFiles.Contains(currentFile))
                    {
                        //We wait for the other threads
                        barrierPrioritaryFiles.Dispose();
                    }
                    //We check if the maximum total size is not exceeded before starting the copy
                    //We start by getting the size of the file
                    FileInfo currentFileInfo = new FileInfo(currentFile);
                    long fileSizeInOctets = currentFileInfo.Length;
                    //We check that the maximum authorized size is not exceeded, otherwise we wait 1 second with a new try
                    while (((fileSizeInOctets + GlobalVariables.currentTransfertSize) > maxSameTimeSize) && (GlobalVariables.currentTransfertSize != 0))
                    {
                        Thread.Sleep(1000);
                    }
                    //If the simultaneous size allows it or is zero, we add the size of the current file to our total transfer size
                    GlobalVariables.currentTransfertSize += fileSizeInOctets;
                    //We retrieve the destination address relative to the source address
                    string destPath = sourcePath.Replace("Source", "Destination");
                    //We create the destination folder if it does not already exist
                    if (!Directory.Exists(destPath))
                    {
                        Directory.CreateDirectory(destPath);
                    }

                    //We retrieve the list of subfolders
                    string destFile = currentFile.Replace("Source", "Destination");
                    List<string> subFoldersPath = GetSubString(destPath, destFile);

                    //We will, if necessary, create all the subfolders
                    foreach (string path in subFoldersPath)
                    {
                        if (!Directory.Exists(path))
                        {
                            Directory.CreateDirectory(path);
                        }
                    }

                    //We now start the copy timer
                    Stopwatch copyTime = new Stopwatch();
                    Stopwatch cryptTime = new Stopwatch();
                    copyTime.Start();
                    //We actually copy the file
                    File.Copy(currentFile, destFile, true);
                    //We stop the clock
                    copyTime.Stop();
                    //We get the size of the file
                    string fileName = Path.GetFileName(currentFile);
                    int fileSize = fileName.Length;
                    this.left--;
                    //We manage file encryption if necessary
                    string fileExtension = Path.GetExtension(fileName);
                    fileExtension = fileExtension.TrimStart('.');

                    this.sizeLeft -= fileSize;

                    //We initialize the variable which contains the file encryption information
                    string encryptTime;

                    if (selectedCryptFileType.Contains(fileExtension))
                    {
                        //If the file extension matches and it must be encrypted:
                        //Recovering the parent folder
                        string parentDirectory = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
                        //Recovery of the CryptoSoft executable
                        string cryptoSoftPath = Path.Combine(parentDirectory, $"CryptoSoft/CryptoSoft.exe");
                        if (!File.Exists(cryptoSoftPath))
                        {
                            //If the CryptoSoft executable is not found.
                            encryptTime = "-1 : CryptoSoft.exe not found!";
                        }
                        else
                        {
                            //File encryption
                            cryptTime.Start();
                            ProcessStartInfo startInfo = new ProcessStartInfo();
                            startInfo.FileName = cryptoSoftPath;
                            startInfo.Arguments = $"\"{destFile}\"";
                            Process.Start(startInfo);
                            cryptTime.Stop();
                            encryptTime = cryptTime.ElapsedMilliseconds + " ms";
                        }
                    } else
                    {
                        encryptTime = "0";
                    }
                    this.copied++;
                    //We broadcast to the ViewModel and the Model that the state of a backup has evolved
                    //We calculate the percentage of progress of the backup
                    float fileCopied = this.copied;
                    float fileTotal = this.total;
                    float pct = ((fileCopied / fileTotal) * 100);
                    int finalPct = (int)Math.Ceiling(pct);
                    //We pass it on to the other classes
                    DiffuseData("Save" + saveIndex + " : " + finalPct);
                    //here, we place a mutex so that only one thread at a time can access the log file
                    mutexLog.WaitOne();
                    //If log format is json:
                    if (logFormat == "JSON")
                    {
                        //We format the content of the log
                        JsonElement logContent = Model.SerializeContent("Save" + saveIndex, fileName, sourcePath, destPath, fileSize, copyTime.ElapsedMilliseconds, encryptTime);
                        //We enter it in the daily log file
                        Model.WriteContentLog(logContent);
                    }
                    else if (logFormat == "XML")
                    {
                        //In the case of the log format is xml
                        Model.WriteXmlLog("Save" + saveIndex, fileName, sourcePath, destPath, fileSize, copyTime.ElapsedMilliseconds, encryptTime);
                    }
                    mutexLog.ReleaseMutex();

                    //End of copying the file, we remove the size of the current file from our total transfer size
                    GlobalVariables.currentTransfertSize -= fileSizeInOctets;
                } else
                {
                    //If this is no longer the case, get out of the loop
                    break;
                }
            }
        }
    }

    class Model
    {
        private string currentLanguage = "EN";
        private long totalSize = 0;
        private int totalFile = 0;
        private long sizeLeft = 0;
        private int fileLeft = 0;
        private int loopStar = 150;
        private string textOutput = "";
        private List<string> allSaveFileNames = new List<string>();
        private List<string> sourceFolderList = new List<string>();
        private string jobApp = "";
        private string logFormat = "JSON";
        private bool differential = false;
        private List<string> selectedCryptFileType = new List<string>();
        private int maxSameTimeSaves = 64;
        private int maxSameTimeSize = 50000;
        private delegate void DELG(object state);
        private static System.Threading.Semaphore semaphore;

        //Method which allows you to manage a waiting list between all the backups which must be created.
        public void SemaphoreWaitList(List<int> listOfSaves)
        {
            Barrier barrierPrioritaryFiles = new Barrier(participantCount: listOfSaves.Count());
            //Resetting the current transfer size (in case it has not reset itself to 0)
            GlobalVariables.currentTransfertSize = 0;
            //Semaphore declaration
            semaphore = new System.Threading.Semaphore(maxSameTimeSaves, maxSameTimeSaves);
            DELG waitList = (state) =>
            {
                GlobalVariables.saveThreadProcess++;
                semaphore.WaitOne();
                int saveIndex = (int)state;
                //Starting backup
                Save save = new Save(saveIndex, jobApp, logFormat, differential, selectedCryptFileType, selectedCryptFileType, maxSameTimeSize, barrierPrioritaryFiles);
                //We remove the object from our running object list
                GlobalVariables.currentSaveProcess.Remove(saveIndex);
                //Freeing up a place in the waiting list
                GlobalVariables.saveThreadProcess--;
                semaphore.Release();
                
            };
            //For each backup : declaration
            foreach (int index in listOfSaves)
            {
                //Declaring a new thread
                System.Threading.Thread t = new System.Threading.Thread(waitList.Invoke);
                //Starting the thread
                t.Start(((object)(index)));

            }
        }

        //Method for performing an action on a backup
        public void actionOnSave(int index, string action)
        {
            Save.SaveInteract(index, action);
        }

        //Method used to define file types that will be encrypted
        public void setEncryptFileType(List<string> extensionsList)
        {
            selectedCryptFileType = extensionsList;
        }

        //Method for defining whether the backup type is differential or full
        public void setCopyMethod(string method)
        {
            if (method.Contains("Compl"))
            {
                differential = false;
            } else
            {
                differential = true;
            }
            
        }

        //Method that transforms the user's request from a character string to an array.
        public string formatUserPrompt(string userPrompt)
        {
            //We start by declaring our return table
            string formatedData = "{{ error.noSave }}";
            if (userPrompt == "*")
            {
                //Case where we have all the backups. (*)

                List<int> indexList = new List<int>();
                //We search for all the backups from 1 to loopStar value.
                for (int i = 1; i < loopStar; i++)
                {
                    if (Directory.Exists(GetParentDirectory(GetFolderPath("Source" + i), "Source" + i)))
                    {
                        indexList.Add(i);
                    }
                }
                if (indexList.Count == 0)
                {
                    formatedData = "{{ error.saver.noFolder }}";
                }
                else
                {
                    //We concatenate the list to return it to the next method
                    formatedData = string.Join(", ", indexList);
                }
            }
            else
            {
                if (userPrompt.Length == 1)
                {
                    if (int.TryParse(userPrompt, out int res))
                    {
                        //Case where we have a precise backup.
                        formatedData = userPrompt;
                    }
                    else
                    {
                        //Error
                        formatedData = "{{ error.notFound }}";
                    }
                }
                else
                {
                    //We check that the input is not empty.
                    if (string.IsNullOrEmpty(userPrompt))
                    {
                        formatedData = "{{ error.noSave }}";
                    }
                    else
                    {
                        //We divide the string into an array separated by ';'
                        string[] parts = userPrompt.Split(';', StringSplitOptions.RemoveEmptyEntries);
                        var result = parts.SelectMany(part =>
                        {
                            //If the evaluated string contains '-'
                            if (part.Contains('-'))
                            {
                                //We check that the format is correct
                                string[] range = part.Split('-');
                                if (range.Length != 2 || !int.TryParse(range[0], out int start) || !int.TryParse(range[1], out int end) || start >= end || start < 1)
                                    //Si erreur : on renvoie l'erreur
                                    return new string[] { "{{ error.fillSyntax }}" };
                                return Enumerable.Range(start, end - start + 1).Select(i => i.ToString());
                            }

                            //Otherwise we try to convert it to an integer

                            else if (int.TryParse(part, out int number) && number >= 1)
                            {
                                //We return the corresponding character
                                return new string[] { number.ToString() };
                            }
                            else
                            {
                                //If this does not work, we return an error.
                                return new string[] { "{{ error.separatorSyntax }}" };
                            }
                        });
                        //We format the return value in a single string
                        formatedData = string.Join(", ", result);
                    }
                }
            }
            //We check that the final result does not contain any errors
            string[] control = formatedData.Split(", ");
            foreach (string currentControl in control)
            {
                if (currentControl.Contains("error"))
                {
                    formatedData = currentControl;
                    break;
                }
            }
            //We return the final result (error or accepted result)
            return formatedData;
        }

        //Method used to control the list of backups to run and the number of lines to display.
        public string GetSaveData(string formatUserPrompt)
        {
            //We transform into a list
            List<int> promptList = StringToList(formatUserPrompt);
            //We check that there has been no duplication of values ​​in the table
            HashSet<int> unitPromptList = new HashSet<int>(promptList);
            promptList = new List<int>(unitPromptList);
            //We sort everything in ascending order
            promptList.Sort();
            //We define the result list
            List<int> finalPromptList = new List<int>();
            //We check if the file actually exists
            foreach (int i in promptList)
            {
                //We retrieve the folder path
                string sourcePath = GetParentDirectory(GetFolderPath("Source" + i), "Source" + i);
                //We retrieve the total number of files in the source
                int totalFiles = getAllFilesInSave(sourcePath);
                //We check if it exists and that the folder contains files
                if (sourcePath != null && totalFiles != 0)
                {
                    //If it is zero, we remove this element from the list
                    finalPromptList.Add(i);
                }
                //We check if the parent folder contains files
            }
            //Count the number of lines to display
            int lines = finalPromptList.Count();
            if(lines > 0) {
                //We format the final result
                string allSaves = string.Join(",", finalPromptList);
                formatUserPrompt = allSaves + "-" + lines;
                
            } else
            {
                //If there are no files: error.
                formatUserPrompt = "{{ error.noSave }}";
            }
            return formatUserPrompt;
        }

        //Method used to retrieve the number of files in a backup folder.
        private int getAllFilesInSave(string sourcePath)
        {
            try
            {
                string[] fileInFolder = Directory.GetFiles(sourcePath);
                totalFile = fileInFolder.Length;
                foreach (string file in fileInFolder)
                {
                    FileInfo fileSize = new FileInfo(file);
                    totalSize = fileSize.Length;
                }

                // Recursively count files in subfolders
                string[] subfolders = Directory.GetDirectories(sourcePath);
                foreach (string subfolder in subfolders)
                {
                    getAllFileInfo(subfolder);
                }
            }
            catch (Exception ex)
            {
                totalFile = 0;
            }
            return totalFile;
        }

        //Method that returns the list of backups to run
        public List<int> extractUserPrompt(string data)
        {
            string[] parts = data.Split('-');
            string[] values = parts[0].Split(",");
            List<int> result = new List<int>();
            //We add the elements to the list
            foreach (string val in values)
            {
                result.Add(int.Parse(val));
            }
            return result; 
        }

        //Overloading the above method which returns the number of rows to display
        public int extractUserPrompt(string data, int dataType)
        {
            string[] parts = data.Split('-');
            return int.Parse(parts[1]);
        }



        //Method which transforms the received character string into an array.
        //This function does not present any error cases because here we are sure that the string only contains integers.
        public List<int> StringToList(string input)
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


        //Method for recovering the current language
        public string getLang()
        {
            return currentLanguage;
        }

        //Method for setting the current language
        public void setLang(string lang)
        {
            currentLanguage = lang;
        }

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

        //Method to recover all files in a source folder
        public int getAllFileInfo(string path)
        {
            string[] fileInFolder = Directory.GetFiles(path);
            totalFile += fileInFolder.Length;
            foreach (string file in fileInFolder)
            {
                FileInfo fileSize = new FileInfo(file);
                totalSize += fileSize.Length;
            }

            // Recursively count files in subfolders
            string[] subfolders = Directory.GetDirectories(path);
            foreach (string subfolder in subfolders)
            {
                getAllFileInfo(subfolder);
            }
            return totalFile;
        }

        //Method that formats log content
        public static JsonElement SerializeContent(string SaveName, string FileName, string SourcePath, string DestinationPath, long FileSize, long TransferTime, string CryptTime, bool etat = false, int fileLeft = 0, int totalFile = 0, int sizeLeft = 0, int totalSize = 0)
        {
            using (var stream = new System.IO.MemoryStream())
            {
                using (var jsonWriter = new Utf8JsonWriter(stream))
                {
                    //Start of JSON object
                    jsonWriter.WriteStartObject();

                    //Add properties to JSON
                    jsonWriter.WriteString("Time", DateTime.Now.ToString());
                    jsonWriter.WriteString("Save", SaveName);
                    jsonWriter.WriteString("Source Path", SourcePath);
                    jsonWriter.WriteString("Destination Path", DestinationPath);
                    if (!etat)
                    {
                        jsonWriter.WriteString("File", FileName);
                        jsonWriter.WriteNumber("File Size", FileSize);
                        jsonWriter.WriteNumber("Transfer Time (ms)", TransferTime);
                        jsonWriter.WriteString("Encryption", CryptTime);
                    }
                    else
                    {

                        jsonWriter.WriteNumber("File To Copy", totalFile);
                        jsonWriter.WriteNumber("File Left", fileLeft);
                        jsonWriter.WriteNumber("File Size Left To Copy", sizeLeft);
                        jsonWriter.WriteNumber("File Size To Copy", totalSize);
                        if (fileLeft == 0)
                        {
                            jsonWriter.WriteString("Transfert State", "Off");
                        }
                        else
                        {
                            jsonWriter.WriteString("Transfert State", "On");
                        }

                    }
                    //End of JSON object
                    jsonWriter.WriteEndObject();
                }

                //Return to start of stream
                stream.Seek(0, System.IO.SeekOrigin.Begin);

                //Parse JSON content into a JsonDocument object
                using (var logInfo = JsonDocument.Parse(stream))
                {
                    //Get root element of JSON document
                    var root = logInfo.RootElement;

                    //Create a copy of the root element to return it
                    return root.Clone();
                }
            }
        }

        //Method that writes to a log file
        public static void WriteContentLog(JsonElement log)
        {
            //We retrieve the address of the log file
            string parentDirectory = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
            string logFileName = Path.Combine(parentDirectory, $"log/logFile_{DateTime.Now:yyyy_MM_dd}.json");

            //If the day's log file does not exist: we create it
            if (!File.Exists(logFileName))
            {
                using (StreamWriter sw = new StreamWriter(logFileName))
                {
                    sw.Close();
                }
            }

            //We format the content of the log to register
            string logContent = JsonSerializer.Serialize(log, new JsonSerializerOptions { WriteIndented = true });
            //We write the log in the log file
            File.AppendAllText(logFileName, logContent + "," + Environment.NewLine);
        }


        //Method the writes to an xml log file
        public static void WriteXmlLog(string SaveName, string FileName, string SourcePath, string DestinationPath, long FileSize, long TransferTime, string CryptTime)
        {
            //We get the address of the file
            string parentDirectory = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
            string filePath = Path.Combine(parentDirectory, $"log/logFile_{DateTime.Now:yyyy_MM_dd}.xml");

            //We define the identifier of our log as the date and time plus a random number to guarantee its uniqueness
            Random random = new Random();
            int logId = random.Next(10000);
            string LogTimeValue = DateTime.Now.ToString() + " #" + logId;

            //We define the content of our xml tag
            string LogContent = Environment.NewLine + "     Save: " + SaveName + Environment.NewLine +
                         "      Source Path: " + SourcePath + Environment.NewLine +
                         "      Destination Path: " + DestinationPath + Environment.NewLine +
                         "      File: " + FileName + Environment.NewLine +
                         "      File Size: " + FileSize + "o" + Environment.NewLine +
                         "      Transfer Time: " + TransferTime + "ms" + Environment.NewLine +
                         "      Encryption: " + CryptTime + Environment.NewLine;


            //We check if the XML file does not exist to be able to initialize it
            if (!File.Exists(filePath))
            {
                //Create a new XML file
                XmlWriterSettings settings = new XmlWriterSettings();
                //We set the file indentation to "true" and the document encoding to UTF-8.
                settings.Indent = true;
                settings.Encoding = System.Text.Encoding.UTF8;



                //Document creation
                using (XmlWriter writer = XmlWriter.Create(filePath, settings))
                {
                    //Writing data into this new document
                    writer.WriteStartDocument();
                    writer.WriteStartElement("Root");
                    writer.WriteStartElement("Element");

                    writer.WriteAttributeString("LogTime", LogTimeValue);
                    writer.WriteString(LogContent);

                    writer.WriteEndElement();
                    writer.WriteEndElement();
                    writer.WriteEndDocument();

                }
            }
            else
            {
                //If the document already exists, we write the new logs afterwards
                //We load the existing content of the XML file into an XmlDocument
                XmlDocument doc = new XmlDocument();
                doc.Load(filePath);

                //We select the root element
                XmlNode root = doc.SelectSingleNode("Root");

                //We create a new element with its data
                XmlElement newElement = doc.CreateElement("Element");

                newElement.SetAttribute("LogTime", LogTimeValue);
                newElement.InnerText = LogContent;

                //We add the new element to the root element
                root.AppendChild(newElement);

                //We save the document.
                doc.Save(filePath);

            }


        }

        //Method that determines whether the business process is active or not
        public static bool IsProcessOpen(string jobApp)
        {
            Process[] currentProcess = Process.GetProcesses();
            if (currentProcess.Any(p => p.ProcessName == jobApp))
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        //Method for recovering the business application
        public string getCurrentJobApp()
        {
            return jobApp;
        }

        //Method for defining the business application
        public void setCurrentJobApp(string currentJobApp)
        {
            jobApp = currentJobApp;
        }


        //setter for the format of the log file
        public void setLogFormat(string format)
        {
            logFormat = format.ToUpper();
        }

        //Method to return the path to the source file
        public static string GetFolderPath(string folderName)
        {
            string path = null;
            //Get path user desktop folders
            string userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Desktop";
            //Get all folders in the desktop folder
            string[] allFolders = Directory.GetDirectories(userFolder, "*", SearchOption.AllDirectories);
            //Search for the folder with the specified name
            foreach (string i in allFolders)
            {
                if (i.Contains(folderName))
                {
                    path = i;
                }
            }
            return path;
        }

        //Method returning parent folder of source folder
        public static string GetParentDirectory(string path, string name)
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

        //Method for finding the location of source folders regardless of the computer
        public static string SearchFolderInDrive(string folderName, DirectoryInfo currentDirectory)
        {
            try
            {
                //Search in current directory
                string[] matchingDirectories = Directory.GetDirectories(currentDirectory.FullName, folderName, SearchOption.TopDirectoryOnly);

                if (matchingDirectories.Length > 0)
                {
                    return matchingDirectories[0]; //Return the first folder found
                }

                //Recursive search in subdirectories
                DirectoryInfo[] subDirectories = currentDirectory.GetDirectories();
                foreach (DirectoryInfo subDirectory in subDirectories)
                {
                    string foundPath = SearchFolderInDrive(folderName, subDirectory);

                    if (!string.IsNullOrEmpty(foundPath))
                    {
                        return foundPath; //Stop search if folder found
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
            }
            return string.Empty; //No folder found
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
