using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Oldi.Utility;
using Oldi.Net;
using System.Globalization;
using System.Data.SqlClient;
using System.Data;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Reflection;

namespace RedirectPay
	{
	public partial class Scan
		{

		DateTime FromDate;
        string GorodConnectionString = "";

        public Scan()
        {
            GorodConnectionString = Config.AppSettings["GorodConnectionString"];
        }

        public Scan(string FromDateString): this()
        {

            if (!DateTime.TryParse(FromDateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out FromDate))
                FromDate = (DateTime.Now).AddDays(-1);

        }

        /// <summary>
        /// Номер чека
        /// </summary>
        string Outtid
			{
			get
				{
				ulong x = (ulong)DateTime.Now.Ticks;
				x = x >> 12;
				string t = x.ToString();
				return t.Length > 10? t.Substring(t.Length - 10): t;
				}
			set
				{
				}
			}
		
		void Log(string fmt, params object[] prms)
			{
			Utility.Log(Properties.Settings.Default.LogFile, fmt, prms);
			}

		public void Run()
			{
			int Status = 0;
			int ErrCode = 0;
			string ErrDesc = "";
			int State = 0;
			int CheckId = 0; // Tid в ГОРОДе
			string SubInnerTid = ""; // Номер транзакции (чека)
			
			Log("Производится сканирование БД с {0}", XConvert.AsDate(FromDate));
			Console.WriteLine("Производится сканирование БД с {0}", XConvert.AsDate(FromDate));

			/*
			string RequestString = 
				@"select p.tid, d.ClientAccount, p.Card_number, p.Amount, p.Summary_Amount
					FROM [Gorod].[dbo].[payment] p 
					inner join [Gorod].[dbo].[PD4] d on p.tid = d.tid 
					where p.datepay >= {0} and p.[state] = 12 and p.[result_text] = '[BLACK] Отменён вручную'
					order by p.datepay;";
			 */
			string RequestString = 
				@"select p.DatePay, p.tid, p.template_tid, p.agent_oid, p.point_oid, d.ClientAccount, p.Card_number, p.Amount, p.Summary_Amount, p.User_id
					FROM [Gorod].[dbo].[payment] p (NOLOCK)
					inner join [Gorod].[dbo].[PD4] d (NOLOCK) on p.tid = d.tid 
					where p.datepay >= {0} and p.[state] = 12 and p.[result_text] = '[BLACK] Отменён вручную' and
						not sub_inner_tid like 'card-%' and charindex('-', sub_inner_tid) > 0
					order by p.datepay;";
			string PIN = "";
			string Nick = "";
			string Password = "";

			string GetNickAndPasswordRequest = 
				"SELECT [login],[password] FROM [Gorod].[dbo].[user] where user_id = {0}";

			Log("Database: {0}", Properties.Settings.Default.GorodConnectionString);

			using (Gorod db = new Gorod(Properties.Settings.Default.GorodConnectionString))
				{

				IEnumerable<Payment> Payments = db.ExecuteQuery<Payment>(RequestString, FromDate);
			
				foreach(Payment Row in Payments)
					{
					// string PIN = GetPIN(Row);
					// Console.Write("tid={0} card={1}", Row.Tid, Row.CardNumber);
					if (Row.Card_number > 0)
						PIN = db.GetCardPIN(Row.Card_number, 1).First<PinResult>().PIN;

					// Имя пользователя и пароль
					IEnumerable<User> Users = db.ExecuteQuery<User>(GetNickAndPasswordRequest, Row.User_id);
					User item = Users.First();
					Nick = item.Login;
					Password = item.Password;

					Console.WriteLine("Tid={0} {1,12:f2} {2,12:f2} Account={3,20} AgentCard={4,7} PIN={5} Point={6,4} Agent={7,4} User={8} Nick={9} Password={10}",
						Row.Tid, Row.Summary_amount, Row.Amount, Row.ClientAccount, Row.Card_number, PIN, 
						Row.Point_oid, Row.Agent_oid, Row.User_id, Nick, Password);
					Log("Tid={0} {1,12:f2} {2,12:f2} Account={3,20} AgentCard={4,7} PIN={5} Point={6,4} Agent={7,4} User={8} Nick={9} Password={10}",
						Row.Tid, Row.Summary_amount, Row.Amount, Row.ClientAccount, Row.Card_number, PIN,
						Row.Point_oid, Row.Agent_oid, Row.User_id, Nick, Password);

 					string QueryString = string.Format("Agent_ID={0}&Point_ID={1}&Nick={2}&Password={3}&template_tid=193&ls=900080&SUMMARY_AMOUNT={4}&tid={5}",
						Row.Agent_oid, Row.Point_oid, Nick, Password, Row.Summary_amount.AsCurrency(), Outtid);

					string Response = Get(Properties.Settings.Default.SimpleHost, Properties.Settings.Default.Endpoint, QueryString);
					// Нет связи с платёжным сервисом
					if (string.IsNullOrEmpty(Response))
						break;

					// Проверка статус и errCode
					StringBuilder sb = new StringBuilder();
					Status = GetInt(Response, "/request/result/Status");
					ErrCode = GetInt(Response, "/request/result/errCode");
					sb.AppendFormat("Status={0} errCode={1}", Status, ErrCode);
					ErrDesc =  GetString(Response, "/request/result/errDesc");
					if (ErrCode != 0)
						sb.AppendFormat(" errDesc={0}", ErrDesc);
					if (Status == 0)
						{
						State = GetInt(Response, "/request/result/state");
						CheckId = GetInt(Response, "/request/result/out_tid");
						SubInnerTid =  GetString(Response, "/request/result/tid");
						sb.AppendFormat(" state={0} checkid={1} subinnertid={2}", State, CheckId, SubInnerTid);
						}
					Console.WriteLine(sb.ToString());
					Log("{0}", sb.ToString());

					// Если создан новый платёж (chrckid > 0) установить статус 10 для исходного платежа
					if (CheckId > 0)
						{
						db.SetStatePayment(Row.Tid, 10, 0, string.Format("Платёж перепроведён. Новый платёж {0}", CheckId));
						// Вписать новый result_text в новый tid
						string ResultText = string.Format("Платёж перепроведён: Tid={0} Acc={1} Pnt={2} Agn={3} SA={4} S={5} Tpl={6}",
							Row.Tid, Row.ClientAccount, Row.Point_oid, Row.Agent_oid, Row.Summary_amount, Row.Amount, Row.Template_tid);
						db.ExecuteCommand(@"update [gorod].[dbo].[payment_history] set result_text = {1} 
												where tid = {0} and old_state is null and new_state = 0 and try_state = 0", 
																CheckId, ResultText);
						Utility.Log(Properties.Settings.Default.RedoLogFile, "DATE={7} Tid={0} SUM={1,12:f2} AMN={2,12:f2} ACC={3,20} CRD={4,7} PNT={5,4} AGN={6,4}",
							Row.Tid, Row.Summary_amount, Row.Amount, Row.ClientAccount, Row.Card_number,
							Row.Point_oid, Row.Agent_oid, XConvert.AsDate(Row.DatePay).Replace('T', ' '));
						}
					
					break; // Только одна итерация!
					}

				}


			}
		

		/// <summary>
		/// Извлечение целочисленного ответа
		/// </summary>
		/// <param name="Answer"></param>
		/// <param name="Xpath"></param>
		/// <returns></returns>
		int GetInt(string Answer, string Xpath)
			{
			string x = null;
			int i = 0;

			if (string.IsNullOrEmpty(Answer))
				return -1;

			if ((x = GetValueFromAnswer(Answer, Xpath)) == null)
				return -1;
			if (x == "")
				i = 0;
			else i = int.Parse(x);
			return i;
			}

		/// <summary>
		/// Извлечение строкового ответа
		/// </summary>
		/// <param name="Answer"></param>
		/// <param name="Xpath"></param>
		/// <returns></returns>
		string GetString(string Answer, string Xpath)
			{
			string x = null;

			if (string.IsNullOrEmpty(Answer))
				return "";

			if ((x = GetValueFromAnswer(Answer, Xpath)) == null)
				return "";
			return x;
			}
		}
	}
