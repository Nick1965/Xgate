using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Oldi.Net;
using Oldi.Utility;
using System.Security.Cryptography;
using System.Data.SqlClient;
using SMPP;
using System.Threading;

namespace Autoshow
{
	public partial class Autoshow: GWRequest
	{
	
		/// <summary>
		/// Выролнить цикл проведения/допроведения платежа
		/// </summary>
		public override void Processing(bool New = true)
		{

			if (New)  // Новый платёж
			{
				if (MakePayment() == 0)
				{
					TraceRequest("NEW");

					if (DoPay(0, 6) == 0)
						TraceRequest("SENT");
					else
						TraceRequest("ERROR");
				}
			}
			else // Redo
			{
				DoPay(state, 6);
			}

		}

		/// <summary>
		/// Резервирование билета
		/// </summary>
		/// <returns></returns>
		int GetTicket()
		{
			using (SqlConnection cnn = new SqlConnection(Settings.ConnectionString))
			using (SqlCommand cmd = new SqlCommand("OLDIGW.GetTicket", cnn))
			{
				cmd.CommandType = System.Data.CommandType.StoredProcedure;
				cnn.Open();
				SqlDataReader dr = cmd.ExecuteReader();
				if (dr.Read())
				{
					account = dr["Ticket"].ToString();
					errCode = 3;
					state = 6;
					return 0;
				}
				else
				{
					errCode = 6;
					state = 12;
					errDesc = "Нет доступного билета";
					return 1;
				}
			}
		}

		/// <summary>
		/// Продажа билета
		/// </summary>
		void BuyTicket()
		{
			using (SqlConnection cnn = new SqlConnection(Settings.ConnectionString))
			using (SqlCommand cmd = new SqlCommand("OLDIGW.BuyTicket", cnn))
			{
				cmd.CommandType = System.Data.CommandType.StoredProcedure;
				cmd.Parameters.AddWithValue("Ticket", Account);
				cnn.Open();
				cmd.ExecuteNonQuery();
			}
		}

		/// <summary>
		/// Проверяет возможность платежа
		/// </summary>
		/// <param name="OpenSession">false - сессия не открывается</param>
		/// <returns>
		/// 0 - платить по указанным реквизитам можно, Price содержит сумму платежа.
		/// != 0 - платёж невозможен
		/// </returns>
		public override void DoCheck(bool OpenSession = false)
		{
			// Поиск билета
			GetTicket();
		}
		
		/// <summary>
		/// Выполнение запроса
		/// </summary>
		/// <param name="host">Хост (не используется)</param>
		/// <param name="old_state">Текущее состояние 0 - check, 1 - pay, 3 - staus</param>
		/// <param name="try_state">Состояние, присваиваемое при успешном завершении операции</param>
		/// <returns></returns>
		public override int DoPay(byte old_state, byte try_state)
		{
			// Создадим платеж с статусом=0 и locId = 1
			// state 0 - новый
			// state 1 - получено разрешение

			int retcode = 0;
			state = 3;
			errCode = 1;
			// Создание шаблона сообщения check/pay/status

			// БД уже доступна, не будем её проверять
			if ((retcode = MakeRequest(old_state)) == 0)
			{
				// retcode = 0 - OK
				// retcode = 1 - TCP-error
				// retcode = 2 - Message sends, but no answer recieved.
				// retcode < 0 - System error
				Log("\r\nПодготовлен запрос к: {0}", Host + "?" + stRequest);

				// Эмуляция положительного ответа
				/*
				RootLog("Tid={0} эмуляция. Номер билета {1}", Tid, Account);
				state = 6;
				errCode = 3;
				errDesc = string.Format("Tid={0} эмуляция. Номер билета {1}", Tid, Account);
				*/				

				 if ((retcode = SendRequest(Host + "?" + stRequest)) == 0)
				 {
					// Разберем ответ
					if (stResponse.Substring(0, 2) == "OK")
					{
						state = 6;
						errCode = 3;
						acceptdate = DateTime.Now;

						BuyTicket();
						
						SmsMessage sms = new SmsMessage(Tid, "Regplat", Phone, string.Format(Sign, Account));
						// ThreadPool.QueueUserWorkItem(new WaitCallback(sms.SendSms), sms);
						SmsPool.Add(sms);
					}
					else
					{
						state = 12;
						errCode = 6;
						errDesc = string.Format("Ошибка: {0}", stResponse);
					}
				}
				else // Ошибка TCP или таймаут
				{
					state = 3;
					errCode = 1;
				}
				
			}

			UpdateState(tid, state: state, errCode: errCode, errDesc: errDesc, acceptdate: XConvert.AsDate2(Acceptdate), locked: 0); // Разблокировать если проведён
			return retcode;

		}

		/// <summary>
		/// Сформировать запрос
		/// </summary>
		/// <param name="old_state"></param>
		/// <returns></returns>
		public new int MakeRequest(int old_state)
		{
			try
			{
				MD5 md5hash = MD5.Create();
				string sign = string.Format("{0}:{1}:{2}:shpItem={3}", XConvert.AsAmount(Amount), Account, Password, "1");
				Hash = GetMd5Hash(md5hash, sign).ToUpper();
				Log("Signature: {0}", sign);
				stRequest = string.Format("OutSum={0}&InvId={1}&phone={2}&shpItem=1&SignatureValue={3}", XConvert.AsAmount(Amount), Account, Phone, Hash);
				return 0;
			}
			catch (Exception ex)
			{
				RootLog("{0}\r\n{1}", ex.Message, ex.StackTrace);
				errDesc = ex.Message;
				errCode = -1;
				return -1;
			}
		}

		/// <summary>
		/// Преобразование массива байтов в строку HEX
		/// </summary>
		/// <param name="md5Hash"></param>
		/// <param name="input"></param>
		/// <returns></returns>
		string GetMd5Hash(MD5 md5Hash, string input)
		{

			byte[] data = md5Hash.ComputeHash(Enc.GetBytes(input));

			StringBuilder sBuilder = new StringBuilder();

			for (int i = 0; i < data.Length; i++)
			{
				sBuilder.Append(data[i].ToString("x2"));
			}

			return sBuilder.ToString();
		}
		
	
	}
}
