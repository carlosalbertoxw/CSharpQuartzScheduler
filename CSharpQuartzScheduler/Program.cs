using System;

namespace CSharpQuartzScheduler
{
    public class Program
    {
        static void Main(string[] args)
        {
            JobScheduler jobScheduler = new JobScheduler();
            jobScheduler.Start();
            Console.ReadLine();
        }
    }
}
