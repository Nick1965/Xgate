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
using System.Data.Linq;
using System.Collections.Concurrent;


namespace OldiGW.Redo.Net
{

	/*
	public class OldiRec
		{
		public int ErrCode;
		public string ErrDesc;
		public int LastCode;
		public string LastDesc;
		}
	class Oldigw: DataContext
		{
		public Oldigw(): base(Settings.ConnectionString)
			{
			}

		}

	 */
  
    public class Redo
    {
        string name;
        protected Int16 idle;
        // protected GWRequest gw;
		public static int? ProviderID = null;

		public static int processes = 0;
		public volatile static bool Canceling = false;

		ConcurrentDictionary<long, DateTime> RedoDict = null;

		class CheckInfo
        {
			// public ManualResetEvent CancelEvent;
			public GWRequest gw = null;
            public CheckInfo(GWRequest gw/*, ManualResetEvent CancelEvent*/)
            {
                // this.gw = new GWRequest(gw);
			this.gw = gw;
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

			RedoDict = new ConcurrentDictionary<long, DateTime>();
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
        // public GWRequest Gw { get { return gw; } }
        
        /// <summary>
        /// Локальная копия лога
        /// </summary>
        /// <param name="fmt">string</param>
        /// <param name="_params">object[]</param>
        void Log(string fmt, params object[] _params)
        {
            Utility.Log(Settings.OldiGW.LogFile, fmt, _params);
        }
		void Log(string msg)
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
				// Ожидать 60 сек.
				Thread.Sleep(60000);
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
				Thread.Sleep(1000);
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

			GWRequest req = null;

			/*
			using (Oldigw db = new Oldigw())
				{
				}
			*/

		
			// Удалим из очереди платежи старше 1 минуты
			foreach (long key in RedoDict.Keys)
				{
				DateTime d;
				RedoDict.TryGetValue(key, out d);
				if (d < DateTime.Now.AddMinutes(-1))
					{
					RedoDict.TryRemove(key, out d);
					Log("{0} [DoREDO] Элемн старше 1 минуты - удаляем из фильтра", key);
					}
				}

			using (SqlParamCommand cmd = new SqlParamCommand(Settings.ConnectionString, "OldiGW.ver3_ReadHolded"))
				{
				cmd.ConnectionOpen();
				using (SqlDataReader dr = cmd.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
					while (dr.Read())
						{
						// Прочитаем отложенный платеж
						
						req = new GWRequest();
						if (req.ReadAll(dr) == 0)
							{

							// Проверим наличие платежа в очереди
							DateTime d;
							if (RedoDict.TryGetValue(req.Tid, out d))
								Log("{0} [REDO] Найден в фильтре. Перепроведение откладывается: {1}", req.Tid, Oldi.Utility.XConvert.AsDate2(d));
							else
								{
								// Добавим платёж в фильтр
								RedoDict.TryAdd(req.Tid, DateTime.Now);

								if (req.State < 6) // Только новые платежи допроводим
									{
									req.SetLock(1); // Заблокировать ещё до постановки в очередь
									if (string.IsNullOrEmpty(req.Account) && string.IsNullOrEmpty(req.Phone) && string.IsNullOrEmpty(req.Number))
										Log("{0} [REDO] Ошибка. Не задан ни один из параметров счёта!", req.Tid);
									// Допровести платежи с нефинальными статусами
									CheckInfo checkinfo = new CheckInfo(req);
									if (!ThreadPool.QueueUserWorkItem(new WaitCallback(CheckState), checkinfo))
										{
										req.SetLock(0); // Не удалось поставить в очередь - разблокировать
										Log("Redo: Не удалось запустить процесс допроведения для tid={0}, state={1}", req.Tid, req.State);
										}
									}
								else
									Log("DoRedo: Ошибка чтения БД");
								
								}
							}
						
						}
				}

		}

		/// <summary>
        /// Процесс обработки платежа
        /// </summary>
        public void CheckState(Object stateInfo)
        {
			if (Canceling) return; // Запущен процесс остановки службы

			GWRequest gw = ((CheckInfo)stateInfo).gw;
			
			try
			{
				// Увеличим счетчик процессов
				RegisterBackgroundProcess();

				// Если отладка не производится...
				if (gw.Provider == Settings.Cyber.Name)
					gw = new GWCyberRequest(gw);
				else if (gw.Provider == Settings.Mts.Name)
					gw = new GWMtsRequest(gw);
				else if (gw.Provider == Settings.Ekt.Name)
					gw = new GWEktRequest(gw);
				else if (gw.Provider == Settings.Rtm.Name)
					gw = new RT.RTRequest(gw);

				if (gw != null)
					{
					gw.SetLock(1);
					gw.ReportRequest("REDO - strt");

					// Синхронизация с БД Город
					gw.Sync(false);

					// Выполнение допроведения
					if (gw.State < 6)
						gw.Processing(false);

					gw.ReportRequest("REDO - stop");
					}
			}
			catch (Exception ex)
				{
				Log("{0}\r\n{1}", ex.Message, ex.StackTrace);
				}
			finally
			{
				if (gw != null) 
					gw.SetLock(0);
					// Уменьшим счетчик процессов 
				UnregisterBackgroundProcess();
			}
        
        }

	}

}
