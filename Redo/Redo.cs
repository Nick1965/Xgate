using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Oldi.Net;
using System.Data.SqlClient;
using Oldi.Utility;
using Oldi.Mts;
using Oldi.Ekt;
using Oldi.Net.Cyber;
using Autoshow;

namespace OldiGW.Redo.Net
{
    public class Redo
    {
        string name;
        protected Int16 idle;
        protected GWRequest gw;

		public static int processes = 0;
		public volatile static bool Canceling = false;

        class CheckInfo
        {
			// public ManualResetEvent CancelEvent;
			public GWRequest gw = null;
            public CheckInfo(GWRequest gw/*, ManualResetEvent CancelEvent*/)
            {
                this.gw = new GWRequest(gw);
				// this.CancelEvent = CancelEvent;
            }
        }

		/// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="id">int</param>
        /// <param name="name">string</param>
        /// <param name="logPath">string</param>
        public Redo(string name, Int16 idle = 1)
        {
            this.name = name;
            // this.cts = cts;
            this.idle = idle;
            processes = 0;
        }

        /// <summary>
        /// Имя процесса
        /// </summary>
        public string Name { get { return name; } }

        /// <summary>
        /// Возвращает объект Task
        /// </summary>
        // public Task Task { get { return task; } }

        /// <summary>
        /// Структура GWRequest - запись о платеже
        /// </summary>
        public GWRequest Gw { get { return gw; } }
        
        /// <summary>
        /// Локальная копия лога
        /// </summary>
        /// <param name="fmt">string</param>
        /// <param name="_params">object[]</param>
        public void Log(string fmt, params object[] _params)
        {
            Utility.Log(Settings.OldiGW.LogFile, fmt, _params);
        }
		public void Log(string msg)
		{
			Utility.Log(Settings.OldiGW.LogFile, msg);
		}

        /// <summary>
        /// Количество выполняющихся процессов
        /// </summary>
        protected int Running { get { return processes; } }

		/// <summary>
        /// Регистрация фонового процесса
        /// </summary>
        public static int maxprocesses = 0;
		protected virtual void RegisterBackgroundProcess()
        {

			Interlocked.Increment(ref processes);
			if (processes > maxprocesses)
				maxprocesses = processes;
			
        }

        /// <summary>
        /// Уменьшает счетчик зарегистрированных фоновых процессов
        /// </summary>
        protected virtual void UnregisterBackgroundProcess()
        {
			Interlocked.Decrement(ref processes);
        }

		public virtual void Run(Object state)
		{

			TaskState stateInfo = (TaskState)state;
	
			Log(Properties.Resources.MsgRedoRunning);
			Console.WriteLine(Properties.Resources.MsgRedoRunning);

			// Читать отложенные платежи

			while (!Canceling)
			{
				try
				{
					DoRedo();
				}
				catch (Exception ex)
				{
					Log("Redo: {0}\r\n{1}", ex.Message, ex.StackTrace);
				}
				// Ожидать 20 сек.
				Thread.Sleep(20 * 1000);
			}

			// Завершение дочерних процессов
			if (processes > 0)
			{
				Log("Redo: Процесс {0}[{1}] завершается. Выполняется {2} процессов допроведения", Thread.CurrentThread.ManagedThreadId, Name, processes);
				Console.WriteLine("{3} Redo: Процесс {0}[{1}] завершается. Выполняется {2} процессов допроведения",
					Thread.CurrentThread.ManagedThreadId, Name, processes, XConvert.AsDate2(DateTime.Now));
			}

			// Ожидаем допроведения всех платежей
			while (processes > 0)
			{
				Console.WriteLine("{1} Работающих процессов: {0}", processes, XConvert.AsDate2(DateTime.Now));
				// Ждем 1 секунду
				Thread.Sleep(100);
			}

			Console.WriteLine("Redo: Процесс {0} завершен", Name);
			Log("Redo: Процесс {0} завершен", Name);
			stateInfo.CancelEvent.Set();

		}

		/// <summary>
		/// Допроведение платежей
		/// </summary>
		private void DoRedo()
		{

			using (SqlParamCommand cmd = new SqlParamCommand(Settings.ConnectionString, "OldiGW.ver3_ReadHolded"))
			{
				cmd.ConnectionOpen();
				using (SqlDataReader dr = cmd.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
					while (dr.Read())
						if (cmd.ErrCode == 0)
						{
							// Прочитаем отложенный платеж
							GWRequest req = new GWRequest();
							if (req.ReadAll(dr) == 0)
							{
								// Допровести платежи с нефинальными статусами
								CheckInfo checkinfo = new CheckInfo(req);
								if (!ThreadPool.QueueUserWorkItem(new WaitCallback(CheckState), checkinfo))
									Log("Redo: Не удалось запустить процесс допроведения для tid={0}, state={1}", req.Tid, req.State);
							}
							else
								Log("Redo: Ошибка чтения БД");
					}
			}

		}

		/// <summary>
        /// Процесс обработки платежа
        /// </summary>
        public void CheckState(Object stateInfo)
        {
			if (Canceling) return; // Запущен процесс остановки службы

			GWRequest gw = null;
			
			try
			{
				// Увеличим счетчик процессов
				RegisterBackgroundProcess();

				gw = ((CheckInfo)stateInfo).gw;

				// gw.ReportRequest("Redo   ");

				// Платежи Ростелекома не допроводим
				if (gw.Provider == Settings.Rt.Name)
					return;

				gw.SetLock(1);

				// Если отладка не производится...
				if (gw.Provider == Settings.Cyber.Name)
					gw = new GWCyberRequest(gw);
				else if (gw.Provider == Settings.Mts.Name)
					gw = new GWMtsRequest(gw);
				else if (gw.Provider == Settings.Ekt.Name)
					gw = new GWEktRequest(gw);
				else if (gw.Provider == Settings.Rt.Name)
					gw = new RT.RTRequest(gw);
				else if (gw.Provider == "as")
					gw = new Autoshow.Autoshow(gw);

				gw.ReportRequest("REDO - начало");
				gw.Processing(false);
				gw.ReportRequest("REDO - конец");
			}
			catch (Exception ex)
			{
				// Log("{0} state={2} error={3}\r\n{1}", gw.Provider, gw.Service, gw.Gateway, gw.State, gw.ErrCode, ex.Message, ex.StackTrace);
				// gw.ReportRequest("REDO ");
				Log("{0}\r\n{1}", ex.Message, ex.StackTrace);
			}
			finally
			{
				if (gw != null) gw.SetLock(0);
				// Уменьшим счетчик процессов 
				UnregisterBackgroundProcess();
			}
        
        }

	}

}
