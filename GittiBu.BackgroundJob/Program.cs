using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceProcess;

namespace GittiBu.BackgroundJob
{
    public class Program
    {
        static void Main(string[] args)
        {
            var serviceProvider = new ServiceCollection().AddLogging(cfg => cfg.AddEventSourceLogger()).BuildServiceProvider();
            var logger = serviceProvider.GetService<ILogger<Program>>();
            using (var service = new GittibuService(logger))
            {
                ServiceBase.Run(service);
            }
        }
    }

    public class GittibuService : ServiceBase
    {
        private readonly ILogger _logger;
        protected readonly string path = AppDomain.CurrentDomain.BaseDirectory + "\\JobLog\\";
        public GittibuService(ILogger logger)
        {
            _logger = logger;
            ServiceName = "GittibuJobs";
        }

        protected override void OnStart(string[] args)
        {
            WriteToFile("Servis başlatıldı. Başlama zamanı : " + DateTime.Now);
            _logger.LogWarning("Test");
            InitializeJobs();
            base.OnStart(args);
        }

        protected override void OnStop()
        {
            WriteToFile("Servis durduruldu. Durdurulma zamanı : " + DateTime.Now);
        }

        private async void InitializeJobs()
        {
            var scheduler = await new StdSchedulerFactory().GetScheduler();
            await scheduler.Start();

            var autoTransferJob = JobBuilder.Create<AutoTransferJob>()
                .WithIdentity("AutoTransferJob")
                .Build();

            var autoTransferTrigger = TriggerBuilder.Create()
                .WithIdentity("AutoTransferJobCron")
                .StartNow()
                .WithSimpleSchedule(x => x.WithIntervalInHours(4).RepeatForever())
                .Build();

            var payoutTransferJob = JobBuilder.Create<PayoutTransactionJob>()
                .WithIdentity("PayoutTransactionJob")
                .Build();

            var payoutTransferTrigger = TriggerBuilder.Create()
                .WithIdentity("payoutTransferJobCron")
                .StartNow()
                .WithCronSchedule("0 0 10,12,14,16,19 ? * * *") //Her gün her ay her yıl saat 10,12,14,16,19 da calistir
                .Build();
            //her saat: 0 0 0/1 1/1 * ? *

            //PZT-CUM saat 15:00 (1 kere) -  0 0 15 ? * MON,TUE,WED,THU,FRI * 



            var dictionary = new Dictionary<IJobDetail, IReadOnlyCollection<ITrigger>>
            {
                {
                    autoTransferJob,
                    new List<ITrigger>()
                          {
                              autoTransferTrigger
                          }
                },
                {
                    payoutTransferJob,
                    new List<ITrigger>()
                          {
                              payoutTransferTrigger
                          }
                }
            };
            await scheduler.ScheduleJobs(dictionary, true);
        }

        #region Methods
        public void WriteToFile(string Message)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\ServiceLog_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
            if (!File.Exists(filepath))
            {
                using (StreamWriter sw = File.CreateText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
        }
        #endregion

    }
}
