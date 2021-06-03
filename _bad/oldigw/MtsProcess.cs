using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;

namespace Oldi.Net
{
    public class MtsProcess : Process
    {
        public MtsProcess(ref int Id, string logPath, CancellationToken cts)
            : base(ref Id, "Mts", logPath, cts, 10)
        {
        }

        protected override void Run()
        {
            Log("Текущие настройки {0}", Name);
            Log("Provider = {0}", ProvidersSettings.Mts.Name);
            Log("Host = {0}", ProvidersSettings.Mts.Host);

            while (true)
            {
                Log("Mts (process={0}) step...", Id);
                base.Run();
                Thread.Sleep(idle * 1000);
            }
        }

        /// <summary>
        /// Обработка сигнала отмены задачи
        /// </summary>
        protected override void Cancel()
        {
            base.Cancel();
            // зделать завершающие пассы и остановиться.
            Thread.Sleep(5000);
        }
    }

}
