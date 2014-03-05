using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;
using System.Threading;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Net;
using Oldi.Net;
using System.Xml.Linq;

namespace Test
{
	public class BaseClass
	{
		static Object LockObj = new Object();
		string log;
		string host;
		
		protected int? errCode;
		protected string errDesc = "";
		int? tid = null;
		protected string outtid = "";
		protected string phone = "";
		protected string account = "";
		protected DateTime? acceptDate = null;
		protected string fio = "";
		protected string opname = "";
		protected string appendix = "";
		protected decimal? amount = null;
		protected decimal? debt = null;
		protected int? statusType = null;
		protected DateTime? startDate = null;
		protected DateTime? endDate = null;

		const string POST = "POST";

		protected string Cert = "";
		Encoding enc;
		string requestMethod = POST;

		protected string stRequest;
		protected string stResponse;

		/// <summary>
		/// Конструктор класса
		/// </summary>
		/// <param name="log"></param>
		public BaseClass(string host, string log, string codePage = "utf-8")
		{
			this.host = host;
			this.log = log;

			switch (codePage.ToLower())
			{
				case "1251":
				case "windows-1251":
					enc = Encoding.GetEncoding(1251);
					break;
				case "866":
				case "cp866":
					enc = Encoding.GetEncoding(866);
					break;
				case "20866":
				case "koi8r":
					enc = Encoding.GetEncoding(20866);
					break;
				case "utf-8":
				case "65001":
					enc = Encoding.UTF8;
					break;
				default:
					enc = Encoding.ASCII;
					break;
			}

		}

		/// <summary>
		/// Обработчик входного запроса
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="certificate"></param>
		/// <param name="chain"></param>
		/// <param name="policyErrors"></param>
		/// <returns></returns>
		private bool ValidateRemote(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors policyErrors)
		{
			// allow any certificate...
			// Log("[SendRequest]: Получен сертификат выданый {0}", certificate.Issuer);

			return true;
		}

		/// <summary>
		/// Добавляет произвольные заголовки в запрос
		/// </summary>
		public virtual void AddHeaders(HttpWebRequest request)
		{
			request.ContentType = "application/x-www-form-urlencoded; charset=windows-1251";
			request.Accept = "application/x-www-form-urlencoded";
			request.Headers.Add("Accept-Charset", "windows-1251");
		}

		/// <summary>
		/// Добавление клиентского сертификата
		/// </summary>
		/// <param name="request"></param>
		public virtual int AddCertificate(HttpWebRequest request)
		{
			errCode = 0;

			if (!string.IsNullOrEmpty(Cert))
			{
				// Добавление сертификата
				using (Crypto cert = new Crypto(Cert))
				{
					if (cert != null)
						if (cert.HasPrivateKey)
							request.ClientCertificates.Add(cert.Certificate);
						else
						{
							errDesc = "SendRequest: Cert has no priv key!";
							errCode = -1;
						}
					else
					{
						errDesc = string.Format("SendRequest: Cert {0} not found", Cert);
						errCode = -1;
					}
				}
			}

			return errCode.Value;

		}

		/// <summary>
		///	Отправка запроса к провайдеру 
		/// </summary>
		/// <param name="requestMethod">Метод запроса. По умолчанию POST</param>
		public int SendRequest(string requestMethod = POST)
		{
			HttpWebRequest request = null;
			HttpWebResponse response = null;
			// StreamReader reader = null;
			byte[] buf;

			if (!string.IsNullOrEmpty(requestMethod) && requestMethod.ToUpper() != POST)
				this.requestMethod = requestMethod;

			errCode = 0;
			stResponse = "";

			try
			{

				ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3;
				ServicePointManager.CheckCertificateRevocationList = true;
				ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback(ValidateRemote);
				ServicePointManager.DefaultConnectionLimit = 10;

				request = (HttpWebRequest)WebRequest.Create(host);
				request.ProtocolVersion = HttpVersion.Version11;
				request.Credentials = CredentialCache.DefaultCredentials;
				request.KeepAlive = true;
				request.Timeout = Timeout.Infinite;
				request.Method = requestMethod;
				// request.Accept = "text/html, */*";
				request.AllowAutoRedirect = false;
				request.UserAgent = "X-Test V.RT.0";
				// request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";

				// Добавление сертификата
				if (AddCertificate(request) != 0)
					return errCode.Value;

				// ServicePoint sp = request.ServicePoint;
				// Всё устанавливаем по-умолчанию
				// sp.ConnectionLeaseTimeout = 900000; //Timeout.Infinite; // 15 мин максимум держится соединение
				// sp.MaxIdleTime = Timeout.Infinite;


				// Добавим заголовки из дочерних классов
				AddHeaders(request);

				Log("Host={0}", request.Host);
				for (int i = 0; i < request.Headers.Count; ++i)
					Log("{0}={1}", request.Headers.Keys[i], request.Headers[i]);

				buf = enc.GetBytes(stRequest);
				request.ContentLength = buf.Length;
				using (Stream requestStream = request.GetRequestStream())
					requestStream.Write(buf, 0, buf.Length);

				// Получим дескриптор ответа
				using (response = (HttpWebResponse)request.GetResponse())
					if (request.HaveResponse)
					{
						// Перехватим поток от сервера
						using (Stream dataStream = response.GetResponseStream())
						using (StreamReader reader = new StreamReader(dataStream, enc))
							stResponse = reader.ReadToEnd();

						// Получен ответ от провайдера (на транспортном уровне все ОК).
						Log("------------------------------------------\r\n{0}", stResponse);
						Console.WriteLine("------------------------------------------\r\n{0}", stResponse);
					}
			}
			catch (WebException we)
			{
				errCode = (int)we.Status + 10000;
				errDesc = string.Format("SendRequest: {0} {1} ({2})", errCode, we.Message, we.Status.ToString());
				Log(errDesc);
			}
			catch (Exception ex)
			{
				errCode = -1;
				errDesc = string.Format("SendRequest: {0}", ex.Message);
				Log("Ошибка при обращении к host = {0}\r\n{1}\r\n{2}", host, ex.Message, ex.StackTrace);
			}
			finally
			{
				ServicePointManager.ServerCertificateValidationCallback -= new RemoteCertificateValidationCallback(ValidateRemote);
				if (request != null && request.ClientCertificates != null)
					request.ClientCertificates.Clear();
				if (response != null) response.Close();
			}

			return errCode.Value;

		} // SendRequest


		/// <summary>
		/// Запись строки в лог-файл
		/// </summary>
		/// <param name="text"></param>
		public void Log(string text)
		{
			if (log.Substring(0, 1) == "{")
				throw new ApplicationException("Не задан файл журнала");

			lock (LockObj)
			{
				StreamWriter sw = null;
				try
				{
					using (sw = new StreamWriter(new FileStream(log, FileMode.Append | FileMode.Create, FileAccess.Write, FileShare.Read)))
					{
						if (string.IsNullOrEmpty(text))
						{
							text = "\r\n";
							sw.WriteLine("\r\n");
						}
						else
						{
							if (text.Substring(0, 2) == "\r\n")
							{
								sw.WriteLine("\r\n");
								text = text.Remove(0, 2);
							}
							sw.WriteLine("{0} [{1:D2}] {2}", FromDate(DateTime.Now), Thread.CurrentThread.ManagedThreadId, text);
						}
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine("{2} Ошибка при попытке записи в лог. Файл: {0}, Ошибка {1}", log, ex.Message, FromDate(DateTime.Now));
					Console.WriteLine(ex.StackTrace);
				}
			}
		}

		/// <summary>
		/// Запись строки с параметрами в лог-файл.
		/// </summary>
		/// <param name="str"></param>
		public void Log(string fmt, params object[] _params)
		{
			if (log.Substring(0, 1) == "{")
				throw new ApplicationException("Не задан файл журнала");

			lock (LockObj)
			{
				string text = string.Format(fmt, _params.Length > 0 ? _params[0] : "",
					_params.Length > 1 ? _params[1] : "",
					_params.Length > 2 ? _params[2] : "",
					_params.Length > 3 ? _params[3] : "",
					_params.Length > 4 ? _params[4] : "",
					_params.Length > 5 ? _params[5] : "",
					_params.Length > 6 ? _params[6] : "",
					_params.Length > 7 ? _params[7] : "",
					_params.Length > 8 ? _params[8] : "",
					_params.Length > 9 ? _params[9] : "",
					_params.Length > 10 ? _params[10] : "",
					_params.Length > 11 ? _params[11] : "",
					_params.Length > 12 ? _params[12] : "",
					_params.Length > 13 ? _params[13] : "",
					_params.Length > 14 ? _params[14] : "",
					_params.Length > 15 ? _params[15] : "");
				StreamWriter sw = null;
				try
				{
					using (sw = new StreamWriter(new FileStream(log, FileMode.Append | FileMode.Create, FileAccess.Write, FileShare.Read)))
					{
						if (!string.IsNullOrEmpty(text))
						{
							if (text.Substring(0, 2) == "\r\n")
							{
								sw.WriteLine("\r\n");
								text = text.Remove(0, 2);
								// text = text.Substring(2, text.Length - 3);
							}
							sw.WriteLine("{0} [{1:D2}] {2}", FromDate(DateTime.Now), Thread.CurrentThread.ManagedThreadId, text);
						}
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine("{2} Ошибка при попытке записи в лог. Файл: {0}, Ошибка {1}", log, ex.Message, FromDate(DateTime.Now));
					Console.WriteLine(ex.StackTrace);
				}

			}
		}


		/// <summary>
		/// Запись строки или буфера в файл лога
		/// </summary>
		/// <param name="msg">Записываемая структура</param>
		public void Log(string log, byte[] buf)
		{

			lock (LockObj)
			{
				try
				{
					using (FileStream fs = new FileStream(log, FileMode.Append | FileMode.Create, FileAccess.Write, FileShare.Read))
						fs.Write(buf, 0, buf.Length);
				}
				catch (Exception ex)
				{
					Console.WriteLine("Ошибка при попытке записи в лог. Файл: {0}, Ошибка {1}", log, ex.Message);
					Console.WriteLine(ex.StackTrace);
				}
			}

		} //Log

		/// <summary>
		/// Дата/время yyyy-mm-ddThh:mm:ss.mmmm
		/// </summary>
		/// <param name="x">Дата/время (DateTime)</param>
		/// <returns>Строка</returns>
		public string FromDate(DateTime? x)
		{
			return x != null ? x.Value.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture) : "";
		}

		/// <summary>
		/// Преобразование из decimal?
		/// </summary>
		/// <param name="x">decimal?</param>
		/// <returns>строка</returns>
		public string FromDecimal(decimal? x)
		{
			return x != null ? x.Value.ToString("#0.00", CultureInfo.InvariantCulture) : "";
		}

		/// <summary>
		/// Преобразование в Int?
		/// </summary>
		/// <param name="value">строка</param>
		/// <param name="deafutValue">int?. Если не null, используется для подстановки при нулевых значениях</param>
		/// <returns>int?</returns>
		public int? ToInt(string value, int? defaultValue = null)
		{
			int val;
			if (int.TryParse(value, out val))
				return val;
			else
				return defaultValue == null? null: defaultValue;
		}

		/// <summary>
		/// Преобразование в decimal?
		/// </summary>
		/// <param name="value">строка</param>
		/// <param name="deafutValue">decimal?. Если не null, используется для подстановки при нулевых значениях</param>
		/// <returns>int?</returns>
		public decimal? ToDecimal(string value, decimal? defaultValue = null)
		{
			decimal val;
			if (decimal.TryParse(value, out val))
				return val;
			else
				return defaultValue == null ? null : defaultValue;
		}

		/// <summary>
		/// Преобразование а DateTime?
		/// </summary>
		/// <param name="value">Строка</param>
		/// <returns>DateTime?</returns>
		public DateTime? ToDate(string value)
		{
			DateTime val;
			if (DateTime.TryParse(value, CultureInfo.InstalledUICulture, DateTimeStyles.AllowWhiteSpaces, out val))
				return val;
			else
				return null;
		}

		/// <summary>
		/// Трассировка запроса
		/// </summary>
		protected void Trace()
		{
			StringBuilder sb = new StringBuilder();

			if (tid != null)
				sb.AppendFormat("{0}{1}={2}", sb.ToString().Length==0?"":" ", "tid", tid);
			if (!string.IsNullOrEmpty(phone))
				sb.AppendFormat("{0}{1}={2}", sb.ToString().Length == 0 ? "" : " ", "num", phone);
			if (!string.IsNullOrEmpty(account))
				sb.AppendFormat("{0}{1}={2}", sb.ToString().Length == 0 ? "" : " ", "acc", account);
			if (amount != null)
				sb.AppendFormat("{0}{1}={2}", sb.ToString().Length == 0 ? "" : " ", "amt", FromDecimal(amount));
			if (debt != null)
				sb.AppendFormat("{0}{1}={2}", sb.ToString().Length == 0 ? "" : " ", "debt", FromDecimal(debt));
			if (!string.IsNullOrEmpty(outtid))
				sb.AppendFormat("{0}{1}={2}", sb.ToString().Length == 0 ? "" : " ", "out", outtid);
			if (!string.IsNullOrEmpty(fio))
				sb.AppendFormat("{0}{1}={2}", sb.ToString().Length == 0 ? "" : " ", "fio", fio);
			if (!string.IsNullOrEmpty(fio))
				sb.AppendFormat("{0}{1}={2}", sb.ToString().Length == 0 ? "" : " ", "app", appendix);
			if (acceptDate != null)
				sb.AppendFormat("{0}{1}={2}", sb.ToString().Length == 0 ? "" : " ", "acp", FromDate(acceptDate));
			if (statusType != null)
				sb.AppendFormat("{0}{1}={2}", sb.ToString().Length == 0 ? "" : " ", "statusType", statusType.ToString());
			if (startDate != null)
				sb.AppendFormat("{0}{1}={2}", sb.ToString().Length == 0 ? "" : " ", "acp", FromDate(startDate));
			if (endDate != null)
				sb.AppendFormat("{0}{1}={2}", sb.ToString().Length == 0 ? "" : " ", "acp", FromDate(endDate));

			if (errCode != null)
				sb.AppendFormat("{0}{1}={2}", sb.ToString().Length == 0 ? "" : " ", "err", errCode);
			if (!string.IsNullOrEmpty(errDesc))
				sb.AppendFormat("{0}{1}={2}", sb.ToString().Length == 0 ? "" : " ", "dsc", errDesc);

			if (sb.ToString().Length > 0)
			{
				Log(sb.ToString());
				Console.WriteLine(sb.ToString());
			}
		}
		
		/// <summary>
		/// Разбор ответа
		/// </summary>
		/// <param name="stResponse">Ответ сервера ЕСПП</param>
		/// <returns>0 - успешно, 1 - таймаут, -1 - неудача, ошибка в errCode</returns>
		public virtual int? ParseAnswer()
		{

			XDocument doc = null;

			// Log("Получен ответ\r\n{0}", stResponse);
			// Console.WriteLine("Получен ответ\r\n{0}", stResponse);
			
			try
			{
				doc = XDocument.Parse(stResponse);
				errCode = 0;
				errDesc = "";
			}
			catch (Exception ex)
			{
				//Log("{0}\r\n{1}", ex.Message, ex.StackTrace);
				errCode = 400;
				errDesc = ex.Message;
			}

			if (errCode != 0)
				return -1;

			var body = doc.Element("response").Elements();
			foreach (XElement el in body)
			{
				switch (el.Name.LocalName.ToLower())
				{
					// <error code="int">string</error>
					case "error": 
						errDesc = el.Value;
						foreach (XAttribute attr in el.Attributes())
							if (attr.Name.LocalName.ToLower() == "code")
							{
								errCode = ToInt(attr.Value, 0);
								break;
							}
						break;
					case "transaction":
						outtid = el.Value;
						break;
					case "accept-date":
						acceptDate = ToDate(el.Value);
						break;
					case "fio":
						fio = el.Value;
						break;
					case "opname":
						opname = el.Value;
						break;
					case "account":
						account = el.Value;
						break;
					case "info":
						appendix = el.Value;
						break;
					case "debt":
						debt = ToDecimal(el.Value, 0M);
						break;
				}
			}

			return errCode;

		}

		/*
<request type="check">
	<provider name="rt" />
	<phone param="1">3822432971</phone
	<amount>100.00</amount>
	<attribute name="purpose" value="0" />
	<attribute name="comment" value="тест" />
	<pc-date>2014-01-08T15:10:00+07:00<pc-date>
</request>


<request type="check">
	<provider name="rt" principal="RT.DV.25.ACCOUNTTEST" />
	<account param="1">1688883</account>
	<amount>100.00</amount>
	<attribute name="purpose" value="0" />
	<attribute name="comment" value="тест" />
	<pc-date>2014-01-08T15:10:00+07:00<pc-date>
</request>
		 */
		/// <summary>
		/// Построение запроса 
		/// check - проверка возможности платежа, получение параметров платежа, фаза 1
		/// pay - выполнение платежа, фаза 2
		/// payment - выполнение платежа за один проход (обычно для предоплаьтных схем)
		/// </summary>
		/// <param name="RequestType"></param>
		/// <returns></returns>
		protected string MakeRequest(string RequestType = "check", 
			int? Tid = null,
			string Filial = "", 
			string Phone = "", 
			string Account = "", 
			string SubNum = "", 
			int? Status = null,
			string Number = "",
			DateTime? StartDate = null,
			DateTime? EndDate = null,
			string Comment = "", 
			Decimal? Amount = null,
			DateTime? ReqTime = null)
		{
			StringBuilder req = new StringBuilder();

			req.AppendFormat("<request type=\"{0}\">\r\n", RequestType);

			if (Tid != null)
				req.AppendFormat("\t<{0}>{1}</{0}>\r\n", "tid", Tid);

			if (Filial != "")
				req.AppendFormat("\t<provider name=\"rt\" principal=\"{0}\" />\r\n", Filial);
			else
				req.Append("\t<provider name=\"rt\" />\r\n");

			if (SubNum != "")
			{
				if (Phone != "")
					req.AppendFormat("\t<{0} param=\"{2}\">{1}</{0}>\r\n", "phone", Phone, SubNum);
				else
					req.AppendFormat("\t<{0} param=\"{2}\">{1}</{0}>\r\n", "account", Account, SubNum);
			}
			else
			{
				if (Phone != "")
					req.AppendFormat("\t<{0}>{1}</{0}>\r\n", "phone", Phone);
				else
					req.AppendFormat("\t<{0}>{1}</{0}>\r\n", "account", Account);
			}

			if (Amount != null)
				req.AppendFormat("\t<{0}>{1}</{0}>\r\n", "amount", FromDecimal(Amount));

			if (Status != null)
				req.AppendFormat("\t<{0}>{1}</{0}>\r\n", "status-type", Status.ToString());

			if (Number != "")
				req.AppendFormat("\t<{0}>{1}</{0}>\r\n", "number", Number);

			if (StartDate != null)
				req.AppendFormat("\t<{0}>{1}</{0}>\r", "start-date", FromDate(StartDate));

			if (EndDate != null)
				req.AppendFormat("\t<{0}>{1}</{0}>\r", "end-date", FromDate(EndDate));

			if (Comment != "")
				req.AppendFormat("\t<{0}>{1}</{0}>\r\n", "comment", Comment);
			
			// req.Append("<attribute name=\"purpose\" value=\"0\" />");

	
			if (ReqTime != null)
				req.AppendFormat("\t<{0}>{1}</{0}>\r", "pc-date", FromDate(ReqTime));

			req.Append("</request>\r\n");

			return req.ToString();
		}
	
	}
}
