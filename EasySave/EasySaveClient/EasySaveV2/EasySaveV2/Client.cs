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
            //Gets local host information
            IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());
            //Selects the first IP address of the host
            IPAddress ipAddr = ipHost.AddressList[0];
            //Creates an IPEndPoint using the selected IP address and a specific port (in this case, 1200)
            IPEndPoint localEndPoint = new IPEndPoint(ipAddr, 1200);
            //Creates a socket using the specified IP address family, socket type, and protocol (IPv4, Stream, TCP)
            Socket client = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            while (run)
            {
                //We are trying to connect to the server
                try
                {
                    //Connects the client to the server
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
                                //Send message to customer
                                client.Send(command);
                            }
                            catch (Exception ex)
                            {
                                //Close the connection with the server
                                client.Shutdown(SocketShutdown.Both);
                                client.Close();
                                connected = false;
                            }

                        }
                        else
                        {
                            //We try to contact the server to check if the connection is ok or ko
                            try
                            {
                                byte[] command = Encoding.ASCII.GetBytes("request");
                                //Envoie le message au client
                                client.Send(command);

                                //The connection is still ok, we are waiting for the message to be received
                                byte[] messageReceived = new byte[1024];
                                int byteRecv = client.Receive(messageReceived);
                                string receiveMessage = Encoding.ASCII.GetString(messageReceived, 0, byteRecv);
                                //We break down the message received
                                List<string> elements = new List<string>(receiveMessage.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
                                //We process the list of orders received
                                foreach (string element in elements)
                                {
                                    if (element != "request")
                                    {
                                        Trace.WriteLine(element);
                                        GlobalVariables.vm.setResultText(element);
                                        GlobalVariables.vm.setResultText("");
                                        //Here we manage the commands received from the server
                                        if (element.Contains("/SaveList"))
                                        {
                                            //Initial progress bar display command
                                            string els = element.Replace("/SaveList ", "");
                                            //We break down the list of backups
                                            List<string> saves = new List<string>(els.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries));
                                            //We empty the list of ProgressBar
                                            Application.Current.Dispatcher.Invoke(() =>
                                            {
                                                //Update the Data property of the ViewModel on the UI thread
                                                GlobalVariables.vm.ProgressBarList.Clear();
                                            });

                                            //For each backup item: display the progress bar
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
                                            //Command to refresh a progress bar
                                            string els = element.Replace("/MajSave ", "");
                                            //We separate the name of the save and the percentage received
                                            List<string> majSave = new List<string>(els.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries));
                                            string saveName = majSave[0];
                                            int savePct = int.Parse(majSave[1]);
                                            //We update the progress bar
                                            Application.Current.Dispatcher.Invoke(() =>
                                            {
                                                GlobalVariables.vm.EditProgressBarValue(saveName, savePct);
                                            });
                                        } else if (element.Contains("/SaveKilled"))
                                        {
                                            //Command to say that a backup has been killed
                                            string els = element.Replace("/SaveKilled ", "");
                                            //We update the message on the progress bar
                                            Application.Current.Dispatcher.Invoke(() =>
                                            {
                                                GlobalVariables.vm.EditMessageOnProgressBar(els, "{{ thread.killed }}");
                                            });
                                        } else if (element.Contains("/SaveWaitForJobApp"))
                                        {
                                            //Command to say that a backup is paused because of business software
                                            string els = element.Replace("/SaveWaitForJobApp ", "");
                                            //We update the message on the progress bar
                                            Application.Current.Dispatcher.Invoke(() =>
                                            {
                                                GlobalVariables.vm.EditMessageOnProgressBar(els, "{{ thread.waitForJobApp }}");
                                            });
                                        } else if (element.Contains("/SaveBreak"))
                                        {
                                            //Command to say a backup is paused
                                            string els = element.Replace("/SaveBreak ", "");
                                            //We recover the value of the progress bar
                                            int val = GlobalVariables.vm.getProgressBarValue(els);
                                            //We update the message on the progress bar
                                            Application.Current.Dispatcher.Invoke(() =>
                                            {
                                                GlobalVariables.vm.EditMessageOnProgressBar(els, val + "% - {{ thread.paused }}");
                                                GlobalVariables.vm.EditProgressBarState(els, true);
                                            });
                                        } else if (element.Contains("/SaveUnbreak"))
                                        {
                                            //Command to say a backup is paused
                                            string els = element.Replace("/SaveUnbreak ", "");
                                            //We recover the value of the progress bar
                                            int val = GlobalVariables.vm.getProgressBarValue(els);
                                            //We update the message on the progress bar
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
                                //Closes the connection with the server because we couldn't reach it
                                client.Shutdown(SocketShutdown.Both);
                                client.Close();
                                connected = false;
                            }

                            Thread.Sleep(5);
                        }
                    }
                    
                } catch (Exception ex)
                {
                    //If we don't succeed, we try again
                    //We remove the existing progressbars
                    if (run)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            //Update the Data property of the ViewModel on the UI thread
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
