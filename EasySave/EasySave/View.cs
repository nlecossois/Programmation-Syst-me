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

        //Overloading the method to define that we do not want a new line.
        public void sendConsole(string input, bool newLine)
        {
            Console.Write(input);
        }

        //Method that prompts the user to enter a message in the console
        public string promptConsole(string prompt)
        {
            Console.WriteLine(">> " + prompt);
            return Console.ReadLine();
        }

    }
}
