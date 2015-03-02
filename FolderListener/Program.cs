using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FolderListener
{
    class Program
    {
        static void Main(string[] args)
        {

            // If a directory is not specified, exit program. 
            if (args.Length != 1)
            {
                // Display the proper way to call the program.
                Console.WriteLine("Usage: Watcher.exe (directory)");
                Console.ReadLine();
                return;
            }

            
            Watcher w = new Watcher();
            w.Run(args);
        }
    }
}
