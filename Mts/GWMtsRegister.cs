using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.IO;
using Oldi.Utility;
using System.Data.SqlClient;
using System.IO.Compression;
using System.Threading;
using System.Xml.Schema;
using System.Xml;

using Settings = Oldi.Utility.Settings;
using Oldi.Net;

namespace Oldi.Mts
{
	class RegisterRecord
	{
		// public string AsVps;		// Код ПЦ представителя
		public string Outtid;		// Транзакция ЕСПП
		public DateTime Pcdate;	// Дата операции
		public long Tid;			// Номер платежа в ПЦ
		public int Tppid;		// Номер терминала
		public string Phone;		// Телефон
		public string Opcode;		// Код домашнего оператора
		public string Account;		// Номер л/с
		public decimal Amount;		// Сумма
		public string Currency;	// Валюта
		public string PIC;			// Тип платежного инструмента
		public decimal Debt;		// Задолженность перед оператором
	}

	public class GWMtsRegister: GWMtsRequest
	{
		/// <summary>
		/// Флаг завершения процесса
		/// </summary>
		public static volatile bool Canceling = false;

		/// <summary>
		/// Дата начала периода
		/// </summary>
		public DateTime? Datefrom { get { return datefrom; } }
		protected DateTime? datefrom = null;

		/// <summary>
		/// Дата окончания периода
		/// </summary>
		public DateTime? Dateto { get { return dateto; } }
		protected DateTime? dateto = null;

		/// <summary>
		/// Путь к папке с реестрами
		/// </summary>
		string regpath = Oldi.Utility.Settings.Registers;

		/// <summary>
		/// Шаблон запроса ComparePacket
		/// </summary>
		string template;
		/// <summary>
		/// Тело реестра
		/// </summary>
		string stBody;

		/// <summary>
		/// Количество платежей
		/// </summary>
		int totalPayments = 0;
		/// <summary>
		/// Сумма платежей по реестру
		/// </summary>
		decimal totalSum = 0.00m;
		/// <summary>
		/// Задолженность перед оператором по реестру
		/// </summary>
		decimal totalDebt = 0.00m;

		Queue<RegisterRecord> body;

		string f_01, f_02, f_03;

		/// <summary>
		/// Идентификатор реестра
		/// </summary>
		long rid;

		/// <summary>
		/// Инициализирует класс реестра
		/// </summary>
		/// <param name="datefrom">DateTime. Начало периода</param>
		/// <param name="dateto">DateTime. Конец периода</param>
		public GWMtsRegister(DateTime? datefrom = null, DateTime? dateto = null)
			: base()
		{
			DateTime now = DateTime.Now;
			if (datefrom == null)
			{
				this.datefrom = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0).AddDays(-1);
				this.dateto = new DateTime(now.Year, now.Month, now.Day, 23, 59, 59).AddDays(-1);
			}
			else if (dateto == null)
			{
				this.datefrom = datefrom;
				this.dateto = new DateTime(Datefrom.Value.Year, Datefrom.Value.Month, Datefrom.Value.Day, 23, 59, 59);
			}
			else
			{
				this.datefrom = datefrom;
				this.dateto = dateto;
			}

			body = new Queue<RegisterRecord>();
			regpath = Oldi.Utility.Settings.Registers;
			provider = "mts";

		}

		/// <summary>
		/// Инициализирует класс реестра
		/// </summary>
		/// <param name="datefrom">String. Начало периода</param>
		/// <param name="dateto">String. Конец периода</param>
		public GWMtsRegister(string datefrom = "", string dateto = "")
			: this(string.IsNullOrEmpty(datefrom) ? null : (DateTime?)DateTime.Parse(datefrom.Replace('T', ' ')), 
			string.IsNullOrEmpty(dateto) ? null : (DateTime?)DateTime.Parse(dateto.Replace('T', ' ')))
		{
		}

		public GWMtsRegister()
			: this("", "")
		{
			rid = 0;
		}

		/// <summary>
		/// Инициализация базовых переменных класса
		/// </summary>
		public override void InitializeComponents()
		{
			base.InitializeComponents();

			try
			{
				/*
				Schemas.Add(string.Format("http://schema.mts.ru/ESPP/AgentPayments/Registries/Reconciliation/v{0}", ProvidersSettings.Mts.Xsd),
					string.Format("{0}\\ESPP_AgentPayments_Registries_Reconciliation_v{1}.xsd", ProvidersSettings.Mts.Schemas, ProvidersSettings.Mts.Xsd));
				 */
			// Schemas.Add(string.Format("http://schema.mts.ru/ESPP/AgentPayments/Registries/Reconciliation/v5_00", ProvidersSettings.Mts.Xsd),
			//	string.Format("{0}\\ESPP_AgentPayments_Registries_Reconciliation_v5_02.xsd", ProvidersSettings.Mts.Schemas, ProvidersSettings.Mts.Xsd));

			}
			catch (Exception ex)
			{
				errCode = 400;
				state = 12;
				errDesc = ex.Message;
				Log("{0}\r\n{1}", errDesc, ex.StackTrace);
			}

		}

		protected override string GetLogName()
			{
			return ".\\log\\registers.log";
			}

		/// <summary>
		/// Имя файла реестра
		/// </summary>
		/// <returns></returns>
		string Makername(string ext)
		{
			return string.Format("{0}\\{1}\\{2}-{3}-{4:D6}.{5}", Settings.Root, regpath,
				Datefrom.Value.ToString("yyMMddHHmmss", CultureInfo.InvariantCulture),
				Dateto.Value.ToString("yyMMddHHmmss", CultureInfo.InvariantCulture),
				rid, ext);
		}

		/// <summary>
		/// Создание файла реестра, запись информации о реестре в таблицу RQueue
		/// </summary>
		/// <returns>0 - ОК</returns>
		public int MakeRegister()
		{

			FileStream fs = null;
			bool compressed = false;
			string stBase64;

			Log("Создаётся реестр платежей с {0} по {1}", XConvert.AsDate(Datefrom), XConvert.AsDate(Dateto));
			Console.WriteLine("Создаётся реестр платежей с {0} по {1}", XConvert.AsDate(Datefrom), XConvert.AsDate(Dateto));
	
			// Загрузим записи
			LoadRecors();
			// Сделаем реестр
			MakeTemplate();

			// Проверим шаблон
			// CheckXML(template);
			if (ErrCode == 400)
			{
				Log("Реестр: err={0} {1}", ErrCode, ErrDesc);
				Console.WriteLine("Реестр: err={0} {1}", ErrCode, ErrDesc);
				return 1;
			}
			
			byte[] buf = new UTF8Encoding(true).GetBytes(template);
			int len = buf.Length;

			// Запишем реестр в файл
			try
			{
				Console.WriteLine("Создаётся файл {0}", Makername("asc"));
				using (fs = new FileStream(Makername("asc"), FileMode.Create, FileAccess.Write, FileShare.None))
				{
					
					fs.Write(buf, 0, len);
					// Если реестр блольше 1 Мб сожмем его
					if (len >= 1024 * 1024)
					{
						compressed = true;
						using (FileStream dflfs = new FileStream(Makername("pak"), FileMode.Create, FileAccess.ReadWrite, FileShare.None))
						{
							using (DeflateStream dfl = new DeflateStream(dflfs, CompressionMode.Compress, true))
							{
								dfl.Write(buf, 0, len);
								dfl.Flush();
								dfl.Close();
							}
							dflfs.Flush();
							dflfs.Seek(0L, SeekOrigin.Begin);
							len = dflfs.Read(buf, 0, buf.Length);
						}
					}
				}
				
				Console.WriteLine("Создаётся base64 файл {0}", Makername("b64"));
				stBase64 = Convert.ToBase64String(buf, 0, len);
				// buf = new UTF8Encoding(true).GetBytes(stBase64);
				buf = new ASCIIEncoding().GetBytes(stBase64);
				len = buf.Length;
				using (fs = new FileStream(Makername("b64"), FileMode.Create, FileAccess.Write, FileShare.None))
				{
					// Записываетя Base64
					fs.Write(buf, 0, len);
				}

				// Готов для обработки - Nexttime установлен в текущую дату
				if (compressed)
					UpdateRState(rid, Compress: compressed, Nexttime: DateTime.Now);
				else
					UpdateRState(rid, Nexttime: DateTime.Now);
			}
			catch (Exception ex)
			{
				Log("{0}\r\n{1}", ex.Message, ex.StackTrace);
				Console.WriteLine("{0}\r\n{1}", ex.Message, ex.StackTrace);
			}

			
			return 0;
		}

		/// <summary>
		/// Загрузка записей реестра из БД
		/// </summary>
		void LoadRecors()
		{
			RegisterRecord r;// = new RegisterRecord();
			int cnt = 0;

			using (SqlConnection cnn = new SqlConnection(Settings.ConnectionString))
			using (SqlCommand cmd = new SqlCommand("OLDIGW.VER3_MakeRegister", cnn))
			{
				cmd.CommandType = System.Data.CommandType.StoredProcedure;
				cmd.Parameters.AddWithValue("DateFrom", Datefrom);
				cmd.Parameters.AddWithValue("DateTo", Dateto);
				cmd.Parameters.Add("Rid", System.Data.SqlDbType.BigInt).Direction = System.Data.ParameterDirection.Output;
				cmd.Parameters.Add("TotalPayments", System.Data.SqlDbType.Int).Direction = System.Data.ParameterDirection.Output;
				cmd.Parameters.Add("TotalSum", System.Data.SqlDbType.Decimal).Direction = System.Data.ParameterDirection.Output;
				cmd.Parameters.Add("TotalDebt", System.Data.SqlDbType.Decimal).Direction = System.Data.ParameterDirection.Output;
				cmd.Parameters["TotalSum"].Scale = 2;
				cmd.Parameters["TotalSum"].Precision = 16;
				cmd.Parameters["TotalDebt"].Scale = 2;
				cmd.Parameters["TotalDebt"].Precision = 16;
				cmd.Connection.Open();
				using (SqlDataReader dr = cmd.ExecuteReader())
				{
					while (dr.Read())
					{
						r = new RegisterRecord();
						dr.GetValue("Outtid", out r.Outtid);
						dr.GetValue("Pcdate", out r.Pcdate);
						dr.GetValue("Tid", out r.Tid);
						dr.GetValue("TppId", out r.Tppid);
						dr.GetValue("Phone", out r.Phone);
						dr.GetValue("Opcode", out r.Opcode);
						dr.GetValue("Account", out r.Account);
						dr.GetValue("CUR", out r.Currency);
						dr.GetValue("Amount", out r.Amount);
						dr.GetValue("PIC", out r.PIC);
						dr.GetValue("Debt", out r.Debt);
						body.Enqueue(r);
						cnt++;
					}
				}
				rid = Convert.ToInt64(cmd.Parameters["Rid"].Value);
				totalPayments = Convert.ToInt32(cmd.Parameters["TotalPayments"].Value);
				totalSum = Convert.ToDecimal(cmd.Parameters["TotalSum"].Value, CultureInfo.InvariantCulture);
				totalDebt = Convert.ToDecimal(cmd.Parameters["TotalDebt"].Value, CultureInfo.InvariantCulture);
				
				Log("Rid={0} count={1}({4}) sum={2} debt={3}", rid, totalPayments, XConvert.AsAmount(totalSum), XConvert.AsAmount(totalDebt), cnt);
				Console.WriteLine("Rid={0} count={1}({4}) sum={2} debt={3}", rid, totalPayments, XConvert.AsAmount(totalSum), XConvert.AsAmount(totalDebt), cnt);
			}
			// r = null;
		}
		
		/// <summary>
		/// Создание шаблона comparePacket
		/// </summary>
		void MakeTemplate()
		{
			string format;
			
			// Прочитаем шаблон
			using (StreamReader stream = new StreamReader(Settings.Templates + "comparepacket-" + ProvidersSettings.Mts.Ott + "-" + ProvidersSettings.Mts.Xsd + ".tpl"))
			{
				format = stream.ReadToEnd();
			}
			
			template = string.Format(format, ProvidersSettings.Mts.Timeout, ProvidersSettings.Mts.Contract, 
				XConvert.AsDate(Datefrom), XConvert.AsDate(Dateto), 
				totalPayments, Cur, XConvert.AsAmount(totalSum), XConvert.AsAmount(totalDebt),
				MakeBody());
		}

		/// <summary>
		/// Зполнение тела реестра
		/// </summary>
		/// <returns></returns>
		string MakeBody()
		{
			StringBuilder sb = new StringBuilder(200000);
			Queue<RegisterRecord> copy = new Queue<RegisterRecord>(body.ToArray());

			int i = 0;
			foreach (RegisterRecord r in copy)
			{
				// r = body.Peek();
				i++;
				sb.AppendFormat("<p id=\"{0}\">{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11};{12}</p>\r\n",
					i.ToString(),
					Asvps,
					r.Outtid,
					r.Pcdate.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture) + "000",
					r.Tid,
					ProvidersSettings.Mts.TerminalPrefix + "." + r.Tppid.ToString(),
					r.Phone,
					r.Opcode,
					r.Account,
					XConvert.AsAmount(r.Amount),
					r.Currency,
					r.PIC,
					XConvert.AsAmount(r.Debt));
			}
	
			copy.Clear();
			return sb.ToString();
		}

		/// <summary>
		/// Обновление состояния очереди реестров
		/// </summary>
		/// <param name="Rid">long ID реестра</param>
		/// <param name="Compress">bool флаг сжатия</param>
		/// <param name="Nexttime">DateTime Время следующего запуска</param>
		/// <param name="State">int Состояние реестра</param>
		/// <param name="Times">int Количество повторов</param>
		void UpdateRState(long Rid, bool? Compress = null, DateTime? Nexttime = null, byte? State = null, int? Times = null)
		{
			using (SqlConnection cnn = new SqlConnection(Settings.ConnectionString))
			using (SqlCommand cmd = new SqlCommand("OLDIGW.VER3_UpdateQState", cnn))
			{
				cmd.CommandType = System.Data.CommandType.StoredProcedure;
				cmd.Parameters.AddWithValue("Rid", Rid);
				
				if (Compress != null)
					cmd.Parameters.AddWithValue("Compress", Compress);
				if (Nexttime != null)
					cmd.Parameters.AddWithValue("Nexttime", Nexttime);
				if (State != null)
					cmd.Parameters.AddWithValue("State", State);
				if (Times != null)
					cmd.Parameters.AddWithValue("Times", Times);
				cmd.Connection.Open();
				cmd.ExecuteNonQuery();
			}
		}

		/// <summary>
		/// Отправка реестров
		/// </summary>
		public void ProcessingRegisters(Object State)
		{
			SqlDataReader dr = null;

			Log(Properties.Resources.MsgRedoRunning);
			Console.WriteLine(Properties.Resources.MsgRedoRunning);

			TaskState stateInfo = (TaskState)State;
	
			f_01 = "5004";

			int min = 0;

			while (!Canceling)
			{

				try
				{
					// RootLog("Проверка очереди реестров");

					if (++min == 60)
					{
						min = 0;
						using (SqlConnection cnn = new SqlConnection(Settings.ConnectionString))
						using (SqlCommand cmd = new SqlCommand("OLDIGW.Ver3_ReadHoldedRegisters", cnn))
						{
							cmd.CommandType = System.Data.CommandType.StoredProcedure;
							cmd.Connection.Open();
							using (dr = cmd.ExecuteReader())
							{
								while (dr.Read())
								{
									rid = dr.GetInt64(dr.GetOrdinal("Rid"));
									f_03 = rid.ToString();
									f_02 = "0";
									if (!dr.IsDBNull(dr.GetOrdinal("Compress")))
										if (dr.GetBoolean(dr.GetOrdinal("Compress")))
											f_02 = "2";
									state = dr.GetByte(dr.GetOrdinal("State"));
									datefrom = dr.GetDateTime(dr.GetOrdinal("Datefrom"));
									dateto = dr.GetDateTime(dr.GetOrdinal("Dateto"));

									Log("Реестр: state={0} rid={1}", state, rid);

									if (state == 0)
										SendRegister();
									else if (state == 3)
										CheckRegister();
								}
								dr.Close();
							}
							cnn.Close();
						}

						Thread.Sleep(1000);
					}
				}
				catch (Exception ex)
				{
					Log("{0}\r\n{1}", ex.Message, ex.StackTrace);
				}

				Thread.Sleep(100);
			
			}

			Log("Процесс обработки реестров завершён");
			Console.WriteLine("Процесс обработки реестров завершён");

			stateInfo.CancelEvent.Set();

		}
		
		/// <summary>
		/// Чтение реестра из 
		/// </summary>
		void SendRegister()
		{
	
			// Прочтём файл в формате Base64 в буфер
			byte[] buf;
			int len;
			
			Log("Отправляется реестр Rid={0} f_01={1} f_02={2} f_03={3}", rid, f_01, f_02, f_03);
			string fname = Makername("b64");

			using (FileStream fs = new FileStream(fname, FileMode.Open, FileAccess.Read))
			{
				len = (int)fs.Length;
				buf = new byte[len];
				len = fs.Read(buf, 0, len);
				fs.Close();
			}

			// Преобразуем byte[] в String
			stBody = Encoding.ASCII.GetString(buf);

			// Построим шаблон
			using (StreamReader st = new StreamReader(Settings.Templates + "0104050-" + ProvidersSettings.Mts.Ott + "-" + ProvidersSettings.Mts.Xsd + ".tpl"))
			{
				template = st.ReadToEnd();
			}
			stRequest = string.Format(template, stBody);

			Log("log\\registers.log", "Host={0} content-type={1}", Settings.Mts.Host, ContentType);
			Log("log\\registers.log", "Подготовлен запрос к серверу, f_01={0} f_02={1} f_03={2}:\r\n{3}", f_01, f_02, f_03, stRequest);
			// CheckXML(stRequest);
			int ret = 0;

			if (errCode != 400)
				ret = SendRequest(Settings.Mts.Host, f_01, f_02, f_03);

			if (ret == 502)
				state = 0;
			else if (ret != 0)
				state = 12;
			else if (ret == 0)
			{
				ret = ParseAnswer(stResponse);
				if (ret == 0)
					state = 3;
				else
					state = 12;
			}

			Log("Реестр: state={0} err={1} {2}", State, errCode, errDesc);
			
			// Обновим состояние реестра
			if (state == 0)
				UpdateRState(rid, Nexttime: (DateTime?)DateTime.Now.AddSeconds(errCode > 10000? 300: 60), State: state);
			else if (state == 3)
				UpdateRState(rid, Nexttime: (DateTime?)DateTime.Now.AddMinutes(15), State: state);
			else
				UpdateRState(rid, State: state);
		}

		/// <summary>
		/// Проверка состояния реестра
		/// </summary>
		void CheckRegister()
		{

			// Построим шаблон
			using (StreamReader st = new StreamReader(Settings.Templates + "0104051-" + ProvidersSettings.Mts.Ott + "-" + ProvidersSettings.Mts.Xsd + ".tpl"))
			{
				template = st.ReadToEnd();
			}
			stRequest = string.Format(template, f_01, f_02, f_03, Contract);

			Log("Подготовлен запрос к серверу:\r\n{0}", stRequest);

			int ret = 0;
			ret = SendRequest(Settings.Mts.Host, f_01, f_02, f_03);

			double to = 15;

			if (ret == 502)
			{
				state = 3;
				to = 1;
			}
			else if (ret != 0)
				state = 12;
			else if (ret == 0)
			{
				ret = ParseAnswer(stResponse);
				if (ret == 0)
				{
					state = 6;
					errCode = 3;
					errDesc = string.Format("Реестр {0} расхождений нет", f_03);
				}
				else if (ret > 1 && ret <= 5)
				{
					state = 12;
					if (!string.IsNullOrEmpty(XmlFile))
						SaveRegister();
				}
				else if (ret == 502 || ret == 805)
					state = 3;
			}

			Log("Реестр: state={0} err={1} {2}", State, errCode, errDesc);

			// Обновим состояние реестра
			if (state == 3)
				UpdateRState(rid, Nexttime: (DateTime?)DateTime.Now.AddMinutes(to), State: state);
			else
				UpdateRState(rid, State: state);
		}

		/// <summary>
		/// Запись реестра расхождений
		/// </summary>
		void SaveRegister()
		{

			byte[] buf = Convert.FromBase64String(xmlFile);
			int len = buf.Length;
			FileStream fs;

			// Запишем реестр в файл
			try
			{
				Console.WriteLine("Создаётся файл {0}", Makername("dif"));
				using (fs = new FileStream(Makername("dif"), FileMode.Create, FileAccess.Write, FileShare.None))
				{
					fs.Write(buf, 0, len);
				}
			}
			catch (Exception ex)
			{
				Log("{0}\r\n{1}", ex.Message, ex.StackTrace);
			}

		}
	
	}
}
