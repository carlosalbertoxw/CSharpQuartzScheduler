using System;

namespace CSharpQuartzScheduler
{
    public class Something
    {
        public Something()
        {
            Console.WriteLine("Hello from task at {0}", DateTime.Now.ToString());
        }
    }
}
