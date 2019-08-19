using Quartz;
using System;
using System.Threading.Tasks;

namespace CSharpQuartzScheduler
{
    public class Job : IJob
    {
        Task IJob.Execute(IJobExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            Task taskA = new Task(() => new Something());
            taskA.Start();
            return taskA;
        }
    }
}
