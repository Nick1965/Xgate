using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Threading;
using System.Web;
using System.IO;
using System.Diagnostics;
using System.Globalization;
using Oldi.Net;
using Oldi.Utility;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using OldiGW.Redo.Net;
using Oldi.Mts;
using System.IO.Compression;

namespace Oldi.Net
{
	class InternalContext: IDisposable
	{
		public HttpListenerContext context;
		public RequestInfo ri;
		public bool RunCollect = false;
		public InternalContext(HttpListenerContext context, RequestInfo ri)
		{
			this.context = context;
			this.ri = ri;
			RunCollect = false;
		}
		public InternalContext(bool RunCollect)
		{
			this.RunCollect = RunCollect;
		}

		bool disposed = false;
		public void Dispose()
			{
			Dispose(true);
			GC.SuppressFinalize(this);
			}
		public virtual void Dispose(bool disposing)
			{
			if (!disposed)
				{
				context = null;
				disposed = true;
				}
			}
	}

	public class TaskInfo
	{
		ManualResetEvent me;
		public TaskInfo(ManualResetEvent me)
		{
			this.me = me;
		}
		public void Cancel()
		{
			me.Set();
		}
	}
	
	public class GWListener
    {
        string m_LogFile;
        int m_Port;
        int m_SslPort;
        string m_HostName;
        HttpListener m_HttpListener;
        // AuthenticationSchemes m_AuthSchemes;

		/// <summary>
		/// Количество выполняющихся фоновых процессов
		/// </summary>
		public static int processes = 0;
		static int maxprocesses = 0;

		public static bool Canceling = false;

		/// <summary>
		/// Количество обработанных запросов
		/// </summary>
		// static int requests = 0;

		volatile static Object SyncLock = new Object();

		ConcurrentBag<InternalContext> Conveyor = null;

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="port"></param>
        /// <param name="sslPort"></param>
        public GWListener(string logFile, int port, int sslPort)
        {
            m_LogFile = logFile;
            m_Port = port;
            m_SslPort = sslPort;

            // Получим имя машины
            m_HostName = Dns.GetHostName();

			if (String.IsNullOrEmpty(m_HostName))
			{
				// Маловероятно, но на всякий случай
				throw new ApplicationException(Properties.Resources.machineNameResolveError);
			}

            // Если параноить, то по полной
            if (port < 0 || sslPort < 0)
            {
                throw new ApplicationException(Properties.Resources.invalidPortNumber + " " +
                    ((sslPort < 0) ? Properties.Resources.argumentSslPort : Properties.Resources.argumentPort));
            }

            // Создаем HttpListener для приема входящих запросов
            m_HttpListener = new HttpListener();

            // Log("Слушаются порты http:{0} https:{1}", port, sslPort);
            
            // Зарегистрируем префикс для порта
			string http = String.Format("http://+:{0}/{1}", port, Settings.GWHost);
			string https = String.Format("https://+:{0}/{1}", sslPort, Settings.GWHost);

			if (port != 0)
			{
				m_HttpListener.Prefixes.Add(http);
				Log("Слушаюеся хост http {0}", http);
				Console.WriteLine("Слушаюеся хост http {0}", http);
			}

			if (sslPort != 0)
			{
				m_HttpListener.Prefixes.Add(https);
				Log("Слушаюеся хост https {0}", https);
				Console.WriteLine("Слушаюеся хост https {0}", https);
			}

            // Задаем схему идентификации клиентов
            m_HttpListener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
            
        }


        /// <summary>
        /// Основной цикл ожидания
        /// </summary>
        public void Run()
        {
            // Обрабатывать Ctrl-C как обычный ввод
            Console.TreatControlCAsInput = true;

			processes = 0;
			maxprocesses = 0;
			
			Task task;
			ManualResetEvent[] me = new ManualResetEvent[Settings.ConveyorSize];
			
            // Запуск конвеера
			Conveyor = new ConcurrentBag<InternalContext>();

			// Запуск слушателя
			try
			{
				Start();
			}
			catch (HttpListenerException ex)
			{
				Log("{0} Code={1} in {2}\r\n{3}", ex.Message, ex.ErrorCode, ex.Source, ex.StackTrace);
				Console.WriteLine("{0} Code={1} in {2}\r\n{3}", ex.Message, ex.ErrorCode, ex.Source, ex.StackTrace);
				if (ex.InnerException != null)
				{
					Log("Оригинал: {0}\r\n{1}", ex.InnerException.Message, ex.InnerException.StackTrace);
					Console.WriteLine("Оригинал: {0}\r\n{1}", ex.InnerException.Message, ex.InnerException.StackTrace);
				}
				if (ex.Data != null)
				{
					Console.WriteLine("  Extra details:");
					foreach (DictionaryEntry de in ex.Data)
					Console.WriteLine("    The key is '{0}' and the value is: {1}", de.Key, de.Value);
				}
				Redo.Canceling = true;
				GWMtsRegister.Canceling = true;
				return;
			}

			for (int i = 0; i < Settings.ConveyorSize; i++)
			{
				me[i] = new ManualResetEvent(false);
				TaskInfo ti = new TaskInfo(me[i]);
				task = new Task(RequestProcessing, ti, TaskCreationOptions.LongRunning | TaskCreationOptions.AttachedToParent | TaskCreationOptions.PreferFairness);
				task.Start();
			}
			Log("Запущено {0} процессов конвейера", Settings.ConveyorSize);
				
			// Запускаем первый запрос
            m_HttpListener.BeginGetContext(new AsyncCallback(ContextReceivedCallback), null);

            Log("GwListener: Слушатель запущен.", Properties.Resources.listenerStarted);
            Console.WriteLine("Press C to terminate process...");
			Console.WriteLine("Press R to restart process...");
				
			while (true)
			{
				Thread.Sleep(1000);
				// По идее, мы сюда больше не вернемся, так что ждем...
				ConsoleKeyInfo c = Console.ReadKey(true);
				if (c.Key == ConsoleKey.C || c.Key == ConsoleKey.R)
				{
					// Если нажаты C или Ctrl-C завершим работу слушателя
					// Остановим слушатель
					Stop();
					Log("GWListener: Заврешение. {0} запросов в очереди {1} обрабатываются", Conveyor.Count, processes);
					Console.WriteLine("GWListener: Заврешение. {0} запросов в очереди {1} обрабатываются", Conveyor.Count, processes);
					// Пока выполняются фоновые процессы будем ждать завершения
					WaitHandle.WaitAll(me);
					if (c.Key == ConsoleKey.R)
						Program.Reload = true;
					else
						Program.Reload = false;
					break;
					// throw new ApplicationException(Properties.Resources.cancelledByUser);
				}
			}

			// Закроем слушатель
			Dispose();
			
        }

        /// <summary>
        /// Запуск слушателя
        /// </summary>
        void Start()
        {
            // Не будем заново запускать слушатель, если он работает
            if (m_HttpListener != null && m_HttpListener.IsListening)
                return;
            if (m_HttpListener != null)
                m_HttpListener.Start();
            else
                throw new ApplicationException(Properties.Resources.listenerCreationError);
        }

		/// <summary>
		/// Останов слушателя
		/// </summary>
		public void Stop()
		{
			Redo.Canceling = true;
			GWMtsRegister.Canceling = true;
			Canceling = true;

			// Останавливаем 
			if (m_HttpListener != null && m_HttpListener.IsListening)
			{
				m_HttpListener.Stop();
				Console.WriteLine("GWListener: Слушатель остановлен");
				Log("GWListener: Слушатель остановлен");
			}
		}

		/// <summary>
        /// Завершение работы слушателя
        /// </summary>
		public void Dispose()
        {
			// Останавливаем 
			if (m_HttpListener != null && m_HttpListener.IsListening)
			{
				m_HttpListener.Close();
				Console.WriteLine("GWListener: Слушатель закрыт");
				Log("GWListener: Слушатель закрыт");
			}
        }

        /// <summary>
        /// Обратный вызов когда получен запрос от клиента
        /// </summary>
        /// <param name="asyncResult">IAsyncResult</param>
        private void ContextReceivedCallback(IAsyncResult asyncResult)
        {
			RequestInfo dataHolder = null;

			if (!m_HttpListener.IsListening || Canceling)
				return;
			
			HttpListenerContext listenerContext = null;
			try
				{
				// Получим контекст
				if (m_HttpListener != null && asyncResult != null)
					listenerContext = m_HttpListener.EndGetContext(asyncResult);
				else
					return;


				// Пусть слушатель слушает следующий запрос. Он вернется в другом потоке
				m_HttpListener.BeginGetContext(new AsyncCallback(ContextReceivedCallback), null);

				// Подготовка объекта RequestInfo, который будет передан в обработку
				dataHolder = new RequestInfo(listenerContext);

				// Добавим контекст запроса в очередь конвейера
				Conveyor.Add(new InternalContext(listenerContext, dataHolder));

				// После 1000 запросов добавим задание сборщика мусора
				// Interlocked.Increment(ref requests);
				// if (Interlocked.CompareExchange(ref requests, 0, 1000) == 1000)
				//	Conveyor.Add(new InternalContext(true));
				}
			catch(Exception ex)
				{
				Log("{0}\r\n{1}", ex.Message, ex.StackTrace);
				}

        }

		static int cnt = 0;
		
		/// <summary>
		/// Обрабатывает в фоновом потоке входной запрос
		/// </summary>
		/// <param name="StateInfo"></param>
		void RequestProcessing(Object objectState)
		{
			TaskInfo ti = (TaskInfo)objectState;
			// Log("Процесс {0} запущен", Thread.CurrentThread.ManagedThreadId);

			InternalContext cntx;

			try
				{
				while (true)
					{

					if (Canceling)
						{
						Log("Обработчик запросов {0} остановлен", Thread.CurrentThread.ManagedThreadId);
						ti.Cancel();
						return;
						}

					if (Conveyor.TryTake(out cntx))
						{
						if (cntx == null)
							continue;

						if (cntx.RunCollect)
							{
							// Log("Запуск сборщика мусора для MaxGeneration={0}", GC.MaxGeneration);
							// GC.Collect(GC.MaxGeneration > 1 ? GC.MaxGeneration - 1 : GC.MaxGeneration, GCCollectionMode.Forced);
							// GC.Collect();
							continue;
							}

						lock (SyncLock)
							{
							processes++;
							if (processes > maxprocesses)
								maxprocesses = processes;
							if (cnt == 50)
								{
								double pag = (double)System.Diagnostics.Process.GetCurrentProcess().PagedMemorySize64 / 1048576.0;
								double vir = (double)System.Diagnostics.Process.GetCurrentProcess().VirtualMemorySize64 / 1048576.0;
								double mem = (double)System.Diagnostics.Process.GetCurrentProcess().WorkingSet64 / 1048576.0;
								Log("Memory={0}Mb Paged={1}Mb, Vitualr={2}Mb processed {3} max {4}",
									mem.ToString("#,###.##"), pag.ToString("#,###.##"), vir.ToString("#,###.##"), processes + Redo.processes, maxprocesses + Redo.maxprocesses);
								cnt = 0;
								}
							else
								cnt++;
							}

						// Загрузим сертификат клиента, если он есть.
						if (cntx.context.Request.IsSecureConnection)
							{
							cntx.ri.GetClientCertificate();
							Log("{0}", Properties.Resources.clientCertificateReceived);
							}

						// Получим кодировку клиента
						cntx.ri.ClientEncoding = cntx.context.Request.ContentEncoding ?? Encoding.GetEncoding(1251);

						// Читаем запрос
						cntx.ri.stRequest = DecompressRead(cntx);

						// Экземпляр конвейера запроса
						using (Processing processing = new Processing(cntx.ri, m_LogFile))
							// По завершении выполнения будет отправлен ответ на входной запрос
							processing.Run();

						}

					// Дадим возможность выполнятся другим процессам
					Thread.Sleep(100);

					}
				}
			catch (Exception ex)
				{
				Log("{0}\r\n{1}", ex.Message, ex.StackTrace);
				}


		}

		/// <summary>
		/// Чтение из сжатого потока
		/// </summary>
		/// <param name="cntx"></param>
		/// <returns>Распакованная строка запрпоса</returns>
		string DecompressRead(InternalContext cntx)
		{
			string request = "";
			Stream input = cntx.ri.Context.Request.InputStream;
			string method = "";

			foreach (string key in cntx.context.Request.Headers.AllKeys)
			{
				if (key.ToLower() == "accept-encoding")
				{
					string[] values = cntx.context.Request.Headers.GetValues(key);
					if (values.Length > 0)
						method = values[0].ToLower();
					// Log("Compress = {0}", compress);
					break;
				}
			}


			if (method == "deflate")
			{
				using (DeflateStream dfls = new DeflateStream(input, CompressionMode.Decompress))
				using (StreamReader reader = new StreamReader(dfls, cntx.ri.ClientEncoding))
					request = reader.ReadToEnd();
			}
			else if (method == "gzip")
			{
				using (GZipStream gzips = new GZipStream(input, CompressionMode.Decompress))
				using (StreamReader reader = new StreamReader(gzips, cntx.ri.ClientEncoding))
					request = reader.ReadToEnd();
			}
			else // Предполагаем, что поток не сжат
			{
				using (StreamReader reader = new StreamReader(input, cntx.ri.ClientEncoding))
					request = reader.ReadToEnd();
			}

			return request;
		}

		/// <summary>
        /// Локальная копия лога
        /// </summary>
        /// <param name="fmt">string</param>
        /// <param name="_params">object[]</param>
        void Log(string fmt, params object[] _params)
        {
            Utility.Log(m_LogFile, fmt, _params);
        }
		void Log(string msg)
		{
			Utility.Log(m_LogFile, msg);
		}

	}
}
