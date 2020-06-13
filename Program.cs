using System;

namespace PortForward
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                if (args[0] == "pf")
                {
                    new PortForward().Start(args);
                }
                else if ((args[0] == "s") || (args[0] == "c"))
                {
                    new ClientServer().Start(args);
                }
            }
            else
            {
                Console.WriteLine("PortForward.dll <mode:[pf|s|c]> ...");
            }
        }
    }
}
