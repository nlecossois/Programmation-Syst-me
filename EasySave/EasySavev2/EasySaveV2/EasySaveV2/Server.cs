using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EasySaveV2
{
    public class Server
    {

        private bool run = true;
        private bool connected = false;
        private List<string> messageToSend = new List<string>();

        //Method to stop the server
        public void Stop()
        {
            Trace.WriteLine("Server closed!");
            connected = false;
            run = false;
        }

        //Method to send a message to the customer
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
            Socket listener = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            while (run)
            {
                //We try to connect and send the message
                try
                {
                    //Binds the socket to the local endpoint
                    listener.Bind(localEndPoint);
                    //Starts listening on the socket with a limit of 10 pending connections
                    listener.Listen(10);
                    Socket clientSocket = listener.Accept();
                    connected = true;

                    //We are waiting for messages to be sent
                    while (connected)
                    {
                        if(messageToSend.Count() > 0)
                        {
                            string toSend = messageToSend[0];
                            messageToSend.Remove(toSend);
                            try
                            {
                                byte[] command = Encoding.ASCII.GetBytes(toSend + ";");
                                //Send message to customer
                                clientSocket.Send(command);
                            } catch (Exception ex)
                            {
                                //Closes the connection with the client
                                clientSocket.Shutdown(SocketShutdown.Both);
                                clientSocket.Close();
                                connected = false;
                            }
                            
                        } else
                        {
                            //We try to contact the client to check if the connection is ok or ko
                            try
                            {
                                byte[] command = Encoding.ASCII.GetBytes("request;");
                                //Send message to customer
                                clientSocket.Send(command);

                                //The connection is still ok, we are waiting for the message to be received
                                byte[] messageReceived = new byte[1024];
                                int byteRecv = clientSocket.Receive(messageReceived);
                                string receiveMessage = Encoding.ASCII.GetString(messageReceived, 0, byteRecv);
                                //We break down the message received
                                List<string> elements = new List<string>(receiveMessage.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
                                //We process the list of orders received
                                foreach (string element in elements)
                                {
                                    if (element != "request")
                                    {
                                        Trace.WriteLine(element);

                                        //Here we manage orders received from the customer
                                        if (element.Contains("/Break"))
                                        {
                                            string els = element.Replace("/Break ", "");
                                            Model.actionOnSave(int.Parse(els), "break");
                                            int val = GlobalVariables.vm.getProgressBarValue("Save " + els);
                                            GlobalVariables.vm.EditMessageOnProgressBar("Save " + els, val + "% - {{ thread.paused }}");
                                            GlobalVariables.vm.EditProgressBarState("Save " + els, true);
                                        } else if (element.Contains("/Unbreak"))
                                        {
                                            string els = element.Replace("/Unbreak ", "");
                                            Model.actionOnSave(int.Parse(els), "unbreak");
                                            int val = GlobalVariables.vm.getProgressBarValue("Save " + els);
                                            GlobalVariables.vm.EditMessageOnProgressBar("Save " + els, val + "%");
                                            GlobalVariables.vm.EditProgressBarState("Save " + els, false);
                                        } else if (element.Contains("/Kill"))
                                        {
                                            string els = element.Replace("/Kill ", "");
                                            Model.actionOnSave(int.Parse(els), "kill");
                                            GlobalVariables.vm.EditMessageOnProgressBar("Save " + els, "{{ thread.killed }}");
                                        }
                                    }
                                }


                            }
                            catch (Exception ex)
                            {
                                //Closes the connection with the server because we couldn't reach it
                                clientSocket.Shutdown(SocketShutdown.Both);
                                clientSocket.Close();
                                connected = false;
                            }

                            Thread.Sleep(5);
                        }
                    }
                    
                }
                catch (Exception ex)
                {
                    //If we don't succeed, we try again
                    connected = false;
                    Thread.Sleep(100);
                    Start();
                }
            }
        }



    }
}
