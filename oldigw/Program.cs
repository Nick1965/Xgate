using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Configuration;
using System.IO;
using System.Reflection;
using Oldi.Net;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;
using System.Data.SqlClient;
using OldiGW.Redo.Net;
using System.Diagnostics;
using Oldi.Utility;
using Oldi.Mts;
using System.Net;

namespace Oldi.Net
{
    
    class Program
    {
        // static dynamic MtsGW;
        // static dynamic CyberPlatGW;
        // delegate void CallBackResut(string response);
        // public static ConcurrentBag<Redo> tasks = new ConcurrentBag<Redo>();
        public static int NumberOfProcess = 0;
        static string logFile;
		static bool stop = false;
		static bool noredo = false;
		public static bool Reload = false;
		static string SubjectName = "CN=apitest.regplat.ru";

		static void Main(string[] args)
			{
			try
				{
				Start(args);
				}
			catch(Exception ex)
				{
				logFile = Settings.OldiGW.LogFile;
				Log(ex.ToString());
				Console.WriteLine(ex.ToString());
				}
			finally
				{
				Log("Приложение остановлено");
				Console.WriteLine("Приложение остановлено");
				}
			}
		static void Start(string[] args)
		{
			try
			{

				// Config.Load();

				// Заставляем не печатать лог, если отправляется реестр
				if (args.Length != 0 && args[0] == "--tpp")
					Settings.logLevel = "";

				Settings.ReadConfig();
				logFile = Settings.OldiGW.LogFile;
				Log($"\r\n{Settings.Title}");

				// Больше не будем проверять работоспособность так.
				// В следующей версии процесс будем пинговать.
				// Если задание уже выполняется, завершить выполнение
				// if (args.Length == 0 && Jobrunning()) return;

				// Проверка доступности БД
				if (!CheckDbConnection())
				{
					Log("Соединение с БД не установлено после {0} попыток", Settings.DbCheckTimeout * 60 / 20);
					return;
				}

				if (args.Length != 0)
				{
					switch (args[0])
                    {
						case "--provider":
						Redo.ProviderID = int.Parse(args[1]);
							break;
                        case "--noredo":
                            noredo = true;
                            break;
                        /*
                        case "--pays":
							GWMtsRegister reg = null;

							if (args.Length == 1)
								reg = new GWMtsRegister("");
							else if (args.Length == 3 && args[1] == "--from" && !String.IsNullOrWhiteSpace(args[2]))
								reg = new GWMtsRegister(args[2]);
							else if (args.Length == 5 && args[1] == "--from" && !String.IsNullOrWhiteSpace(args[2]) &&
														 args[3] == "--to" && !String.IsNullOrWhiteSpace(args[4]))
								reg = new GWMtsRegister(args[2], args[4]);
							else
							{
                                Console.WriteLine(Properties.Resources.MsgUsage);
								return;
							}

							reg.MakeRegister();
							return;
                        */
                        case "--tpp":
                            Oldi.Ekt.Mts.RegTerminals();
                            return;
                        default:
							Console.WriteLine(Properties.Resources.MsgUsage);
                            return;
                    }
				}


				// Если произойдёт сбой, процесс перезапустится
				while (!stop)
					{
					try
						{
						Run(noredo);
						}
					catch (Exception ex)
						{
						Log($"Необработанное исключение:\r\n{ex.ToString()}");
						}
					}

				Console.WriteLine("Служба остановлена оператором");
				Log("Служба остановлена оператором");

			}
			catch (Exception se)
			{
				Console.WriteLine($"Main: {se.ToString()}");
                Log($"Main: {se.ToString()}");
            }

            // Console.WriteLine("Press any key to exit...");
			// Console.ReadKey();
		}

		static void Run(bool noredo)
        {
            // CancellationTokenSource cts = new CancellationTokenSource();

			// ThreadPool.SetMinThreads(3, 3);

			// Максимальное количество соединений с конечной точкой
            ServicePointManager.DefaultConnectionLimit = Settings.ConnectionLimit;

			ManualResetEvent[] CancelTaskEvents = new ManualResetEvent[2];

			Task task;
            Redo redo = new Redo("Redo");
			GWMtsRegister regs = new GWMtsRegister();
			TaskState stateInfo;
			if (!noredo)
			{
				// Запустить процесс допроведения платежей
				CancelTaskEvents[0] = new ManualResetEvent(false);
				stateInfo = new TaskState(CancelTaskEvents[0]);
				task = new System.Threading.Tasks.Task(redo.Run, stateInfo, TaskCreationOptions.LongRunning);
				task.Start();
				Oldi.Net.Utility.Log(Settings.OldiGW.LogFile, "Запущен процесс допроведения платежей");

				CancelTaskEvents[1] = new ManualResetEvent(false);
				stateInfo = new TaskState(CancelTaskEvents[1]);
				task = new System.Threading.Tasks.Task(regs.ProcessingRegisters, stateInfo, TaskCreationOptions.LongRunning);
				task.Start();
				Oldi.Net.Utility.Log(Settings.OldiGW.LogFile, "Запущен процесс отправки реестров МТС");

				// Запуск процесса отправки СМС
				// task = new Task(GWRequest.SendSmsProcess, TaskCreationOptions.LongRunning | TaskCreationOptions.AttachedToParent);
				// task.Start();
				// Oldi.Net.Utility.Log(Settings.OldiGW.LogFile, "Запущен процесс отправки СМС");
			}

			// Запуск слушателя
			try
				{
				Log($"Регистрация сертифика службы SMPP: {SubjectName}");

				GWListener listener = new GWListener(Settings.OldiGW.LogFile, Settings.Port, Settings.SslPort);
				listener.Run();
				stop = true;
				// Остановить слушатель
				listener.Dispose();
				}
			catch (Exception ex)
				{
				Log(ex.ToString());
				}

			Console.WriteLine("{0} останавливается...", Settings.Title);
			Log("{0} останавливается...", Settings.Title);

            // Остановка процессов допроведения
            if (!noredo)
				WaitHandle.WaitAll(CancelTaskEvents);
            
			// Остановка службы
			// Console.WriteLine("Служба {0} остановлена", Settings.Title);
			Log($"Служба {Settings.Title} остановлена");

			// Перезагрузка службы
			if (Reload)
				{
				Log("Перезагрузка....");
				stop = false;
				Reload = false;
				Redo.Canceling = false;
				GWListener.Canceling = false;
				}

		}

		/// <summary>
        /// Проверка доступности базы данных в течение DbCheckTimeout минут
        /// </summary>
        static bool CheckDbConnection()
        {
            Log("Проверка соединения с БД...");

            // Время задержки меду тестами
            int idle = 20;
            // количество попыток
            int attempts = Settings.DbCheckTimeout * 60 / idle;
            int count = 0;

            while (true)
            {
                try
                {
                    using (SqlConnection cnn = new SqlConnection(Settings.ConnectionString))
                    using (SqlCommand cmd = new SqlCommand("Select Top 1 tid From OldiGW.Queue", cnn))
                    {
                        cnn.Open();
                        using (SqlDataReader dr = cmd.ExecuteReader())
                            if (dr.HasRows)
                                dr.Read();
                    }
                    break;
                }
                catch (SqlException)
                {
                    Log("CheckDbConnection: Нет соединения с БД");
                }

                if (++count > attempts)
                    return false;

                Thread.Sleep(idle * 1000);
            }

            return true;
        
        }

        
        /// <summary>
        /// Локальная копия лога
        /// </summary>
        /// <param name="fmt">string</param>
        /// <param name="_params">object[]</param>
        static void Log(string fmt, params object[] _params)
        {
            Utility.Log(logFile, fmt, _params);
        }
		static void Log(string msg)
		{
			Utility.Log(logFile, msg);
		}

        /// <summary>
        /// Результат вызова метода Gateway
        /// </summary>
        /// <param name="response">ответ сервера ПУ</param>
		/*
		static void Result(string response)
        {
            Console.WriteLine("Ответ сервера: {0}", response);
        }
		*/
    }
}
