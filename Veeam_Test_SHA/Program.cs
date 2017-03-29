using System;
using Veeam_Test_SHA.Extensions;

namespace Veeam_Test_SHA
{
    class Program
    {
        private static Options options;
        
        static void Main(string[] args)
        {
            try
            {
                options = new Options(args);
            }
            catch(ArgumentException ex)
            {
                ex.Print();
            }

            using(var converter = new SHAConverter(options))
            {
                try
                {
                    converter.StartConverting();
                }
                catch(ArgumentException ex)
                {
                    ex.Print();
                }

                converter.WaitResult();
            }

            Console.WriteLine("Complete!");
            Console.ReadKey();
        }
    }
}
