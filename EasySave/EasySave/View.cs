using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySave
{
    class View
    {

        //Method that sends a message to the console
        public void sendConsole(string input)
        {
            Console.WriteLine(">> " + input);
        }

        //Method that prompts the user to enter a message in the console
        public string promptConsole(string prompt)
        {
            Console.WriteLine(">> " + prompt);
            return Console.ReadLine();
        }

    }
}
