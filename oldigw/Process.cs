using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;

namespace Oldi.Net
{
    public class Process
    {
        int id;
        string name;
        string logFile;
        CancellationToken cts;
        Task task;
        protected Int16 idle;

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="id">int</param>
        /// <param name="name">string</param>
        /// <param name="logPath">string</param>
        public Process(ref int id, string name, string logPath, CancellationToken cts, Int16 idle = 1)
        {
            this.name = name;
            logFile = logPath + name + ".log";
            this.id = Interlocked.Increment(ref id);
            this.cts = cts;
            this.idle = idle;
        }

        /// <summary>
        /// Имя процесса
        /// </summary>
        public string Name { get { return name; } }

        /// <summary>
        /// Идентификатор процесса
        /// </summary>
        public int Id { get { return id; } }

        /// <summary>
        /// Возвращает объект Task
        /// </summary>
        public Task Task { get { return task; } }

        /// <summary>
        /// Локальная копия лога
        /// </summary>
        /// <param name="fmt">string</param>
        /// <param name="_params">object[]</param>
        public void Log(string fmt, params object[] _params)
        {
            Utility.Log(logFile, fmt, _params);
        }

        /// <summary>
        /// Стартовый модуль процесса
        /// </summary>
        /// <param name="stateinfo"></param>
        public void Start()
        {
            Log("Процесс Id={0}, Name={1} запущен", Id, Name);
            task = new System.Threading.Tasks.Task(Run, cts);
            task.Start();
        }

        /// <summary>
        /// Получен сигал завершения процесса
        /// </summary>
        protected virtual void Cancel()
        {
            Log("Процесс {0}[{1}] завершается", Name, Id);
        }
        
        protected virtual void Run()
        {
            if (cts.IsCancellationRequested)
            {
                // Console.WriteLine("Process {0}[{1}] canceled.", Name, id);
                // cts.ThrowIfCancellationRequested();
                Cancel();
                Log("Процесс {0}[{1}] завершен", Name, Id);
                throw new OperationCanceledException(String.Format("Процесс {0}[1] отменен", Name, Id), cts);
            }
        }
    }

    public class CyberProcess : Process
    {
        public CyberProcess(ref int Id, string logPath, CancellationToken cts)
            : base(ref Id, "Cyber", logPath, cts, 5)
        {
        }

        protected override void Run()
        {
            Log("Текущие настройки {0}", Name);
            Log("Provider = {0}", ProvidersSettings.Cyber.Name);
            Log("Host = {0}", ProvidersSettings.Cyber.Host);
            Log("PayCheck = {0}", ProvidersSettings.Cyber.PayCheck);
            Log("Pay = {0}", ProvidersSettings.Cyber.Pay);
            Log("PayStatus = {0}", ProvidersSettings.Cyber.PayStatus);

            while (true)
            {
                Log("Cyber (process={0}) step...", Id);
                base.Run();
                Thread.Sleep(idle * 1000);
            }
        }

        protected override void Cancel()
        {
            base.Cancel();
            // зделать завершающие пассы и остановиться.
            Thread.Sleep(1000);
        }
    }


}
