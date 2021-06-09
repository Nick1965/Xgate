using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Security;
using System.Threading;
using System.IO;
using Oldi.Utility;
using System.Security.Cryptography.X509Certificates;
using System.Web;

namespace Oldi.Net
{
	public partial class GWRequest: IDisposable
	{
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
		}

		/// <summary>
		/// Добавление клиентского сертификата
		/// </summary>
		/// <param name="request"></param>
		public virtual int AddCertificate(HttpWebRequest request)
		{
			errCode = 0;
			
			// Добавление сертификата
			if (!string.IsNullOrEmpty(commonName))
			{
				using (Crypto cert = new Crypto(CommonName))
				{
					if (cert != null)
						if (cert.HasPrivateKey)
							request.ClientCertificates.Add(cert.Certificate);
						else
						{
							errDesc = Properties.Resources.MsgCHNPK;
							errCode = -1;
						}
					else
					{
						errDesc = $"{Properties.Resources.MsgCNF} {CommonName}";
						errCode = -1;
					}
				}
			}

			return errCode;

		}
		
		/// <summary>
		///	Отправка запроса к провайдеру 
		/// </summary>
		/// <param name="Host">Адрес хоста</param>
		/// <param name="f01">Поле f01</param>
		/// <param name="f02">Поле f02</param>
		/// <param name="f03">Поле f03</param>
		/// <returns>Ответ провайдера</returns>
		public int SendRequest(string Host, string f01 = null, string f02 = null, string f03 = null)
		{
			HttpWebRequest request = null;
			HttpWebResponse response = null;
			// StreamReader reader = null;
			byte[] buf;
			Encoding enc;

			switch (CodePage.ToLower())
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

            Log("Codepage: {0}", enc.WindowsCodePage.ToString());

            /*
            if (Provider != "as")
				if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(stRequest))
				{
					state = 11;
					errCode = 2;
					errDesc = string.Format("{0} is null", string.IsNullOrEmpty(Host) ? "HOST" : "REQUEST");
					RootLog(ErrDesc);
					return 1;
				}
                */
            try
            {

                // Использем только протоколы семейства TLS
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
				ServicePointManager.CheckCertificateRevocationList = true;
				ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback(ValidateRemote);
				ServicePointManager.DefaultConnectionLimit = 10;

				request = (HttpWebRequest)WebRequest.Create(Host);
				request.ProtocolVersion = HttpVersion.Version11;
				request.Credentials = CredentialCache.DefaultCredentials;
				request.KeepAlive = true;
				request.Timeout = TimeOut() * 1000;
				request.Method = "POST";
				request.Accept = "text/html, */*";
                request.ContentType = "application/x-www-form-urlencoded";
                request.Headers.Set("Accept-Encoding", "identity");
                request.AllowAutoRedirect = false;
				request.UserAgent = Settings.Title;
				request.ContentType = ContentType;

				// Добавление сертификата
				if (AddCertificate(request) != 0)
					return errCode;
	
				// ServicePoint sp = request.ServicePoint;
				// Всё устанавливаем по-умолчанию
				// sp.ConnectionLeaseTimeout = 900000; //Timeout.Infinite; // 15 мин максимум держится соединение
				// sp.MaxIdleTime = Timeout.Infinite;


				// Добавим заголовки в коллекцию, если они есть
				if (!string.IsNullOrEmpty(f01)) request.Headers.Add("f_01", f01);
				if (!string.IsNullOrEmpty(f02)) request.Headers.Add("f_02", f02);
				if (!string.IsNullOrEmpty(f03)) request.Headers.Add("f_03", f03);
				
				// Добавим заголовки из дочерних классов
				AddHeaders(request);
				request.UserAgent = Settings.ClientName;

                Log("------------------------------------------------------");
                Log("Method={0}", request.Method);
                Log("Host={0}", Host);
                // Log("Body={0}", stRequest);

                /*
				if (Settings.LogLevel.IndexOf("HDR") != -1)
				{
					for (int i = 0; i < request.Headers.Count; ++i)
						Log("{0}={1}", request.Headers.Keys[i], request.Headers[i]);
				}
				*/

                LogRequest(request.Headers, stRequest);
				Log("------------------------------------------------------");

				if (request.Method == WebRequestMethods.Http.Post)
				{
					// buf = enc.GetBytes(stRequest.Replace("\r\n", ""));
					buf = enc.GetBytes(stRequest);
					request.ContentLength = buf.Length;
                    Log("ContentLength={0}", request.ContentLength);
                    using (Stream requestStream = request.GetRequestStream())
						requestStream.Write(buf, 0, buf.Length);
				}

				// Получим дескриптор ответа
				using (response = (HttpWebResponse)request.GetResponse())
					if (request.HaveResponse)
					{
						// Перехватим поток от сервера
						using (Stream dataStream = response.GetResponseStream())
						using (StreamReader reader = new StreamReader(dataStream, enc))
							stResponse = reader.ReadToEnd();

						// Если пустой ответ: 1
						if (string.IsNullOrEmpty(stResponse))
						{
							errCode = 1;
							if (state == 1)
								state = 0;
							errDesc = "Не получен ответ от провайдера";
						}
						else // Получен ответ от провайдера
						{
							errCode = 0;
							// Получен ответ от провайдера (на транспортном уровне все ОК.
							LogRequest(response.Headers, stResponse);
						}
					}
				else
				{
					errCode = 1;
					if (state == 1)
						state = 0;
					errDesc = "Не получен ответ от провайдера";
					Log(ErrDesc);
					RootLog(ErrDesc);
				}
			}
			catch (WebException we)
			{
				errDesc = string.Format("{0} {1} ({2})", errCode, we.Message, we.Status.ToString());
				if (state == 1)
					state = 0;
				errCode = 502;
				Log(we.ToString());
				RootLog("[{0}] {1} {2}", Tid, Host, errDesc);
                Log(we.ToString());

                // Если это ошибка установки SSL-соединения, надо повторить.
                if (we.Status == WebExceptionStatus.SecureChannelFailure)
                    return (int)WebExceptionStatus.SecureChannelFailure;
			}
			catch (Exception ex)
			{
				state = 0;
				errCode = 11;
				ErrDesc = string.Format("Ошибка шлюза {0}", ex.Message);
				state = 1;
				RootLog("{0}\r\n{1}", ex.Message, ex.StackTrace);
				Log("{0}\r\n{1}", ex.Message, ex.StackTrace);
			}
			finally
			{
				ServicePointManager.ServerCertificateValidationCallback -= new RemoteCertificateValidationCallback(ValidateRemote);
				if (request != null && request.ClientCertificates != null)
					request.ClientCertificates.Clear();
				if (response != null) response.Close();
			}

			Console.WriteLine($"ErrCode={ErrCode} ErrDesc={ErrDesc}");
			return errCode;

		} // SendRequest


		/// <summary>
		/// Вывести трассировку запрос/ответ
		/// </summary>
		/// <param name="headers"></param>
		/// <param name="text"></param>
		void LogRequest(WebHeaderCollection headers, string text)
		{
            StringBuilder s = new StringBuilder();
			s.AppendFormat("Host: {0}\r\n", Host);
			if (Settings.LogLevel.IndexOf("HDR") != -1)
			{
				for (int i = 0; i < headers.Count; ++i)
					s.AppendFormat("{0}={1}\r\n", headers.Keys[i], headers[i]);
            }
			if (Provider != "cyber")
				{
				s.Append(text);
				Log(HttpUtility.UrlDecode(s.ToString().Replace("\r\n", "\r\n\t\t\t")));
				}
		}
	
	}
}
