using System;
using Quartz;
using Quartz.Impl;

namespace CSharpQuartzScheduler
{
    public class JobScheduler
    {
        public async void Start()
        {
            ISchedulerFactory schedulerFactory = new StdSchedulerFactory();
            IScheduler scheduler = await schedulerFactory.GetScheduler();
            await scheduler.Start();

            IJobDetail job = JobBuilder.Create<Job>().Build();

            ITrigger trigger = TriggerBuilder.Create()

                .WithIdentity("HelloJob ", "GreetingGroup")

                .WithCronSchedule("0 0/1 * 1/1 * ? *")

                .StartAt(DateTime.UtcNow)

                .WithPriority(1)

                .Build();

            await scheduler.ScheduleJob(job, trigger);

        }

    }
}
