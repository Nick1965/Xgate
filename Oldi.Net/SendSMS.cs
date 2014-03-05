using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using SMPP;
using Oldi.Utility;
using System.Threading;
using System.Collections.Concurrent;
using System.Net;

namespace Oldi.Net
{

	/// <summary>
	/// Сообщение
	/// </summary>
	public class SmsMessage
	{
		public int delivery;
		public long tid;
		public string sign;
		public string phone;
		public string text;
		public DateTime? start;
		public DateTime? stop;


		/// <summary>
		/// Конструктор класса
		/// </summary>
		/// <param name="tid">int tid</param>
		/// <param name="sign">string подпись</param>
		/// <param name="phone">string номер телефона</param>
		/// <param name="text">string сообщение</param>
		/// <param name="delivery">int номер рассылки</param>
		public SmsMessage(long tid, string sign, string phone, string text, int delivery = 0, DateTime? start = null, DateTime? stop = null)
		{
			this.tid = tid;
			this.sign = sign;
			this.text = text;
			this.phone = phone;
			this.delivery = delivery;
			this.start = start ?? null;
			this.stop = stop ?? null;
		}
	}

	public partial class GWRequest
	{
		public static ConcurrentBag<SmsMessage> SmsPool = new ConcurrentBag<SmsMessage>();

		/// <summary>
		/// Отправка СМС-сообщения
		/// </summary>
		void SendSMS()
		{

			string _phone = "";
			string sign = "";
			string text = "";
			DateTime? start = null;
			DateTime? stop = null;

			// 0 - рассылок нет
			if (Settings.Delivery == 0)
				return;

			if (!string.IsNullOrEmpty(phone) && phone.Substring(0, 1) == "9" && phone.Length == 10)
				_phone = phone;
			else if (!string.IsNullOrEmpty(number) && number.Substring(0, 1) == "9" && number.Length == 10)
				_phone = number;
			else if (!string.IsNullOrEmpty(contact) && number.Substring(0, 1) == "9" && contact.Length == 10)
				_phone = contact;
			else
				return;

			// if (_phone != "9138207050" && _phone != "9039531420" && _phone != "9095380201")
			//	return;

			int count = 0;
			string message = "";
			// string subProvider = "";

			try
			{
				using (SqlConnection cnn = new SqlConnection(Settings.ConnectionString))
				using (SqlCommand cmd = new SqlCommand("OLDIGW.GetDelivery", cnn))
				{
					cmd.CommandType = System.Data.CommandType.StoredProcedure;
					cmd.Parameters.AddWithValue("Delivery", Settings.Delivery);
					cnn.Open();
					using (SqlDataReader dr = cmd.ExecuteReader())
						if (dr.HasRows)
						{
							dr.Read();
							count = int.Parse(dr["count"].ToString());
							sign = dr["signed"].ToString();
							text = dr["message"].ToString();
							if (dr["start"] != DBNull.Value)
								start = dr.GetDateTime(dr.GetOrdinal("start"));
							if (dr["stop"] != DBNull.Value)
								stop = dr.GetDateTime(dr.GetOrdinal("stop"));
							string promo = "";
							if (dr["promo"] != DBNull.Value)
								promo = dr["promo"].ToString();
							message = string.Format("{0} {1}", text, promo); 
						}
				}

				if (!string.IsNullOrEmpty(message)/* && count < 10000*/)
				{
					// ThreadPool.QueueUserWorkItem(new WaitCallback(sms.SendSms), sms);
					// Добавим сообщение в пул
					SmsPool.Add(new SmsMessage(Tid, sign, _phone, message, Settings.Delivery, start , stop));
					Oldi.Net.Utility.Log(Settings.LogPath + "delivery.log",
						"{0} {1} Рассылка #{2} {4} {3} байт",
						tid, _phone, Settings.Delivery, Encoding.UTF8.GetByteCount(message), message);
				}
			}
			catch (Exception ex)
			{
				RootLog(ex.Message);
				RootLog(ex.StackTrace);
			}

		}

		/// <summary>
		/// Процесс асинхронной отправки смс
		/// </summary>
		public static void SendSmsProcess()
		{
			SmsMessage sms;

			while (true)
			{
				try
				{
					if (SmsPool.TryTake(out sms))
					{
						if (sms == null)
							continue;
						Do(sms);
					}
				}
				catch (Exception ex)
				{
					Oldi.Net.Utility.Log(Settings.OldiGW.LogFile, "{0}\r\n{1}", ex.Message, ex.StackTrace);
				}
				Thread.Sleep(100);
			}

		}
	
		/// <summary>
		/// Отправка одиночного сообщения
		/// </summary>
		/// <param name="sms"></param>
		static void Do(SmsMessage sms)
		{

			bool sent = false;
			string log = "delivery.log";
			DateTime str, stp;

			// Oldi.Net.Utility.Log("Отправляется в {0} диапазон {1} - {2}", XConvert.AsDate(DateTime.Now), XConvert.AsDate(sms.start), XConvert.AsDate(sms.stop));
			// Если задан диапазон рассылки - проверим вхождение
			if (sms.start != null && sms.stop != null)
			{
				str = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, sms.start.Value.Hour, sms.start.Value.Minute, sms.start.Value.Second);
				stp = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, sms.stop.Value.Hour, sms.stop.Value.Minute, sms.stop.Value.Second);

				// Oldi.Net.Utility.Log("Отправляется в {0} диапазон {1} - {2}", XConvert.AsDate(DateTime.Now), XConvert.AsDate(str), XConvert.AsDate(stp));

				if (DateTime.Now < str || DateTime.Now > stp)
				{
					// Oldi.Net.Utility.Log(Settings.LogPath + log, 
					//	"Время {0} вне допустимого диапазона {1} - {2}", DateTime.Now.ToShortTimeString(), str.ToShortTimeString(), stp.ToShortTimeString());
					return;
				}
				
			}

			try
			{
				// RootLog("{0} Рассылка #{1} {2} отправляется СМС", tid, Settings.Delivery, phone);
				using (SqlConnection cnn = new SqlConnection(Settings.ConnectionString))
				{
					cnn.Open();
					using (SqlCommand cmd = new SqlCommand("OLDIGW.SmsWrite", cnn))
					{
						cmd.CommandType = System.Data.CommandType.StoredProcedure;
						cmd.Parameters.AddWithValue("DeliveryId", sms.delivery);
						cmd.Parameters.AddWithValue("Tid", sms.tid);
						cmd.Parameters.AddWithValue("From", sms.sign);
						cmd.Parameters.AddWithValue("To", sms.phone);
						cmd.Parameters.AddWithValue("Text", sms.text);
						cmd.ExecuteNonQuery();
					}

					IPHostEntry hostinfo = Dns.GetHostEntry("smpp4.integrationapi.net");
					IPAddress[] ips = hostinfo.AddressList;

					using (SmsClient client = new SmsClient("OLDI-T LLC", ips[0].ToString(), 2775, "regplat", "yd2qox45", "run", (int)sms.tid))
					// using (SmsClient client = new SmsClient())
					{
						client.Connect();

						// throw new ApplicationException (string.Format("Отправляется {0} в {1} подписано {2}", account, phone, sign));

						if (!client.SendSms(sms.sign, "+7" + sms.phone, sms.text))
							Oldi.Net.Utility.Log(Settings.LogPath + log, "{0} {1} ошибка отправки SMS", sms.tid, sms.phone);
						else
						{
							sent = true;
							Oldi.Net.Utility.Log(Settings.LogPath + log, "{0} {1} отправлен", sms.tid, sms.phone);
						}
						
						client.Disconnect();
					}

					if (sent)
						using (SqlCommand cmd = new SqlCommand("OLDIGW.SmsSent", cnn))
						{
							cmd.CommandType = System.Data.CommandType.StoredProcedure;
							cmd.Parameters.AddWithValue("Tid", sms.tid);
							cmd.ExecuteNonQuery();
						}

				}
			}
			catch (Exception ex)
			{
				Oldi.Net.Utility.Log(Settings.OldiGW.LogFile, "{0}\r\n{1}", ex.Message, ex.StackTrace);
			}
		}

	}


}
