using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace EasySaveV2
{
    public class Client
    {
        private bool run = true;
        private bool connected = false;
        private List<string> messageToSend = new List<string>();

        public void Stop()
        {
            run = false;
            connected = false;
        }

        public void Send(string message)
        {
            messageToSend.Add(message);
        }

        public void Start()
        {
            //Obtient les informations sur l'hôte local
            IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());
            //Sélectionne la première adresse IP de l'hôte
            IPAddress ipAddr = ipHost.AddressList[0];
            //Crée un IPEndPoint en utilisant l'adresse IP sélectionnée et un port spécifique (dans ce cas, 1200)
            IPEndPoint localEndPoint = new IPEndPoint(ipAddr, 1200);
            //Crée un socket en utilisant la famille d'adresses IP, le type de socket, et le protocole spécifiés (IPv4, Stream, TCP)
            Socket client = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            while (run)
            {
                //On tente de se connecter au serveur
                try
                {
                    //Connecte le client au serveur
                    client.Connect(localEndPoint);
                    connected = true;
                    GlobalVariables.vm.setResultText("{{ app.printer.waitForData }}");
                    while (connected)
                    {
                        
                        if (messageToSend.Count() > 0)
                        {
                            string toSend = messageToSend[0];
                            messageToSend.Remove(toSend);
                            try
                            {
                                byte[] command = Encoding.ASCII.GetBytes(toSend + ";");
                                //Envoie le message au client
                                client.Send(command);
                            }
                            catch (Exception ex)
                            {
                                //Ferme la connexion avec le serveur
                                client.Shutdown(SocketShutdown.Both);
                                client.Close();
                                connected = false;
                            }

                        }
                        else
                        {
                            //On tente de joindre le serveur pour vérifier si la connexion est ok ou ko
                            try
                            {
                                byte[] command = Encoding.ASCII.GetBytes("request");
                                //Envoie le message au client
                                client.Send(command);

                                //La connexion est toujours ok, on attend le message à recevoir
                                byte[] messageReceived = new byte[1024];
                                int byteRecv = client.Receive(messageReceived);
                                string receiveMessage = Encoding.ASCII.GetString(messageReceived, 0, byteRecv);
                                //On décompose le message reçu
                                List<string> elements = new List<string>(receiveMessage.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
                                //On traite la liste des commandes reçues
                                foreach (string element in elements)
                                {
                                    if (element != "request")
                                    {
                                        Trace.WriteLine(element);
                                        GlobalVariables.vm.setResultText(element);
                                        GlobalVariables.vm.setResultText("");
                                        //On gère ici les commandes reçues du serveur
                                        if (element.Contains("/SaveList"))
                                        {
                                            //Commande d'affichage initiale des progress bar
                                            string els = element.Replace("/SaveList ", "");
                                            //On décompose la liste des sauvegardes
                                            List<string> saves = new List<string>(els.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries));
                                            //On vide la liste des ProgressBar
                                            Application.Current.Dispatcher.Invoke(() =>
                                            {
                                                // Mettre à jour la propriété Data du ViewModel sur le thread de l'interface utilisateur
                                                GlobalVariables.vm.ProgressBarList.Clear();
                                            });

                                            //Pour chaque element de sauvegarde : afficher la progress bar
                                            foreach (string save in saves)
                                            {
                                                

                                                Application.Current.Dispatcher.Invoke(() =>
                                                {
                                                    
                                                    GlobalVariables.vm.ProgressBarList.Add(
                                                    new ProgressBarElement { Name = "Save " + save, ProgressBarValue = 0 }
                                                    );
                                                });
                                                
                                            }
                                        } else if (element.Contains("/MajSave"))
                                        {
                                            //Commande pour actualiser une progress bar
                                            string els = element.Replace("/MajSave ", "");
                                            //On sépare le nom de la save et le pourcentage reçu
                                            List<string> majSave = new List<string>(els.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries));
                                            string saveName = majSave[0];
                                            int savePct = int.Parse(majSave[1]);
                                            //On actualise la progress bar
                                            Application.Current.Dispatcher.Invoke(() =>
                                            {
                                                GlobalVariables.vm.EditProgressBarValue(saveName, savePct);
                                            });
                                        } else if (element.Contains("/SaveKilled"))
                                        {
                                            //Commande pour dire qu'une sauvegarde à été kill
                                            string els = element.Replace("/SaveKilled ", "");
                                            //On actualise le message sur la progress bar
                                            Application.Current.Dispatcher.Invoke(() =>
                                            {
                                                GlobalVariables.vm.EditMessageOnProgressBar(els, "{{ thread.killed }}");
                                            });
                                        } else if (element.Contains("/SaveWaitForJobApp"))
                                        {
                                            //Commande pour dire qu'une sauvegarde est en pause à cause du logiciel métier
                                            string els = element.Replace("/SaveWaitForJobApp ", "");
                                            //On actualise le message sur la progress bar
                                            Application.Current.Dispatcher.Invoke(() =>
                                            {
                                                GlobalVariables.vm.EditMessageOnProgressBar(els, "{{ thread.waitForJobApp }}");
                                            });
                                        } else if (element.Contains("/SaveBreak"))
                                        {
                                            //Commande pour dire qu'une sauvegarde est en pause
                                            string els = element.Replace("/SaveBreak ", "");
                                            //On récupère la valeur de la progress bar
                                            int val = GlobalVariables.vm.getProgressBarValue(els);
                                            //On actualise le message sur la progress bar
                                            Application.Current.Dispatcher.Invoke(() =>
                                            {
                                                GlobalVariables.vm.EditMessageOnProgressBar(els, val + "% - {{ thread.paused }}");
                                                GlobalVariables.vm.EditProgressBarState(els, true);
                                            });
                                        } else if (element.Contains("/SaveUnbreak"))
                                        {
                                            //Commande pour dire qu'une sauvegarde est en pause
                                            string els = element.Replace("/SaveUnbreak ", "");
                                            //On récupère la valeur de la progress bar
                                            int val = GlobalVariables.vm.getProgressBarValue(els);
                                            //On actualise le message sur la progress bar
                                            Application.Current.Dispatcher.Invoke(() =>
                                            {
                                                GlobalVariables.vm.EditMessageOnProgressBar(els, val + "%");
                                                GlobalVariables.vm.EditProgressBarState(els, false);
                                            });
                                        }
                                    }
                                }

                                
                                
                            }
                            catch (Exception ex)
                            {
                                //Ferme la connexion avec le serveur car nous n'avons pas pu le joindre
                                client.Shutdown(SocketShutdown.Both);
                                client.Close();
                                connected = false;
                            }

                            Thread.Sleep(5);
                        }
                    }
                    
                } catch (Exception ex)
                {
                    //Si on y arrive pas, on reessaie
                    //On retire les progressbar existantes
                    if (run)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            // Mettre à jour la propriété Data du ViewModel sur le thread de l'interface utilisateur
                            GlobalVariables.vm.ProgressBarList.Clear();
                        });
                    }

                    connected = false;
                    GlobalVariables.vm.setResultText("{{ app.printer.waitForServer }}");
                    Thread.Sleep(100);
                    Start();
                }
            }

        }
    }
}
