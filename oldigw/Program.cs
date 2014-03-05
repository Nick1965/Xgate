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
        delegate void CallBackResut(string response);
        // public static ConcurrentBag<Redo> tasks = new ConcurrentBag<Redo>();
        public static int NumberOfProcess = 0;
        static string logFile;
		static bool stop = false;
		static bool noredo = false;

		static void Main(string[] args)
		{
			try
			{

				// Config.Load();

				Settings.ReadConfig();
				logFile = Settings.OldiGW.LogFile;

				// Если задание уже выполняется, завершить выполнение
				// if (args.Length == 0 && Jobrunning()) return;

				// Проверка доступности БД
				if (!CheckDbConnection()) return;

				/*
				
				if (args.Length != 0)
				{
					if (args[0] == "--noredo")
						noredo = true;
					else
					{
						if (args[0] == "--pays")
						{
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
								Usage();
								return;
							}

							reg.MakeRegister();
							return;
						}
						else
						{
							Usage();
							return;
						}
					}
				}
				
				*/
				noredo = true;
				// Если произойдёт сбой, процесс перезапустится
				while (!stop)
				{
					try
					{
						Run(noredo);
					}
					catch (Exception ex)
					{
						Log("{0}\r\n{1}", ex.Message, ex.StackTrace);
					}
				}
			}
			catch (Exception se)
			{
				Console.WriteLine("Main: {0}\r\n{1}", se.Message, se.StackTrace);
				// Log("Stack: {0}\r\n{1}", se.Message, se.StackTrace);
			}

			// Console.WriteLine("Press any key to exit...");
			// Console.ReadKey();
		}

		static void Usage()
		{
			Console.WriteLine("Usage oldigw {command} [{key1} [{key2}]], где\r\n\r\n" +
				"\tcommand:\r\n" +
				"\t\t --noredo - запуск без допроведения (отладка)\r\n\r\n" +
				// "\t\t --tpp - передать реестр регистрации ТПП\r\n" +
				"\t\t --pays - передать реестров платежей\r\n\r\n" +
				// "\t\t --undo - выполнить отмену платежей\r\n\r\n" +
				"\tКлючи:\r\n" +
				"\t\t --from dd.mm.yyyy - начальная дата реестра платежей\r\n" +
				"\t\t --to dd.mm.yyyy - конечная дата реестра платежей\r\n" +
				"\t\t --noredo - запуск шлюза без допроведения");
		}

		static void Run(bool noredo)
        {
            // CancellationTokenSource cts = new CancellationTokenSource();

			// ThreadPool.SetMinThreads(3, 3);

			// Максимальное количество соединений с конечной точкой
			ServicePointManager.DefaultConnectionLimit = 4;

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
				task = new Task(GWRequest.SendSmsProcess, TaskCreationOptions.LongRunning | TaskCreationOptions.AttachedToParent);
				task.Start();
				Oldi.Net.Utility.Log(Settings.OldiGW.LogFile, "Запущен процесс отправки СМС");
			}

			// Запуск слушателя
			GWListener listener = new GWListener(Settings.OldiGW.LogFile, Settings.Port, Settings.SslPort);
            listener.Run();
			stop = true;
			// Остановить слушатель
			listener.Dispose();

			Console.WriteLine("{0} останавливается...", Settings.Title);
			Log("{0} останавливается...", Settings.Title);

			if (CancelTaskEvents.Length > 0)
				CancelTaskEvents[0].WaitOne();
			Console.WriteLine("Процес допроведения остановлен");
			if (CancelTaskEvents.Length > 1)
				CancelTaskEvents[1].WaitOne();
			Console.WriteLine("Процесс обработки реестров остановлен");
			// if (!noredo)
			//	WaitHandle.WaitAll(CancelTaskEvents);
            
			// Остановка службы
			// Console.WriteLine("Служба {0} остановлена", Settings.Title);
			Log("Служба {0} остановлена", Settings.Title);


		}

		/// <summary>
        /// Проверка доступности базы данных в течение DbCheckTimeout минут
        /// </summary>
        static bool CheckDbConnection()
        {
            string stRequest = "Select Top 1 tid From OldiGW.Queue";

            // Log("Проверка соединения с БД...");

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
                    {
                        SqlCommand cmd = new SqlCommand(stRequest, cnn);
                        cnn.Open();
                        SqlDataReader dr = cmd.ExecuteReader();
                        if (dr.HasRows)
                        {
                            if (dr.Read())
                            {
                                long tid = Convert.ToInt64(dr["Tid"]);
                            }
                        }
                    }
                    // Log("Соединение с БД проверено.");
                    return true;
                }
                catch (SqlException ex)
                {
                    Log("Нет соединения с БД: {0}", ex.Message);
                }

                if (++count > attempts) break;
                Thread.Sleep(idle * 1000);
            }

            return false;
        
        }


        private static bool Jobrunning()
        {
			Console.WriteLine("\r\nПроверка работоспособности процесса {0}", Settings.Jobname);
			Log("\r\nПроверка работоспособности процесса {0}", Settings.Jobname);

			Process[] Jobs = Process.GetProcessesByName(Settings.Jobname);

			if (Jobs.Length > 1)
            {
				Console.WriteLine("{0}: {1}", Settings.Jobname, Properties.Resources.MsgJobAlreadyRunning);
				Log("{0}: {1}", Settings.Jobname, Properties.Resources.MsgJobAlreadyRunning);
                return true;
            }

			Console.WriteLine(Settings.Title);
			Utility.Log(Settings.OldiGW.LogFile, "\r\n{0}\r\n", Settings.Title);

            return false;
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
        static void Result(string response)
        {
            Console.WriteLine("Ответ сервера: {0}", response);
        }
        
    }
}
