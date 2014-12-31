using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.Text;
using System.Threading;
using Oldi.Net;
using Oldi.Utility;
using XWcfApiService;
using System.Threading.Tasks;
using System.Net.Security;

namespace WcfSmpp
	{
	public class NotifyService
		{
		string log = Settings.OldiGW.LogFile;
		string From = "3822497049";
		public void Notify(string List, string Message)
			{

			TaskFactory factory = new TaskFactory(TaskCreationOptions.LongRunning, TaskContinuationOptions.None);

			factory.StartNew(() =>
				{
					XWcfApiServiceClient Client = null;
					// RemoteCertificateValidationCallback validator = new RemoteCertificateValidationCallback(RemoteCertValidate);
					// ServicePointManager.ServerCertificateValidationCallback += validator;
					try
					{
					Result r = null;
					string[] phones;

					// Указываем явно файл конфигурации
					Client = new XWcfApiServiceClient(".\app.config");
					
					Client.ClientCredentials.ClientCertificate.SetCertificate(StoreLocation.CurrentUser, StoreName.My, X509FindType.FindBySubjectName, "nick-pc@regplat.ru");

					if (List.IndexOf(',') != -1 || List.IndexOf(';') != -1 || List.IndexOf('|') != -1 || List.IndexOf(' ') != -1)
						{
						phones = List.Split(new Char[] { ' ', ',', ';', '|' });
						StringBuilder sb = new StringBuilder();
						foreach (string p in phones)
							sb.AppendFormat("{0}||", p);
						r = Client.SendTextMass(21, From, Message, phones, "Tomsk");
						Utility.Log(log, "[SMSS] Уведомление {0} на {1} отправлено", Message, sb.ToString());
						}
					else
						{
						r = Client.SendText(21, From, List, Message);
						Utility.Log(log, "[SMSS] Уведомление {0} на {1} отправлено", Message, List);
						}
					}
				catch (Exception ex)
					{
					if (List.IndexOf(',') != -1 || List.IndexOf(';') != -1 || List.IndexOf('|') != -1 || List.IndexOf(' ') != -1)
						{
						string[] phones = List.Split(new Char[] { ' ', ',', ';', '|' });
						StringBuilder sb = new StringBuilder();
						foreach (string p in phones)
							sb.AppendFormat("{0}||", p);
						Utility.Log(log, "[SMSS] Уведомление {0} на {1} не отправлено", Message, sb.ToString());
						}
					else
						{
						Utility.Log(log, "[SMSS] Уведомление {0} на {1} не отправлено", Message, List);
						}
					Utility.Log(log, "{0}\r\n{1}", ex.Message, ex.StackTrace);
					}
				finally
					{
					if (Client != null)
						Client.Close();
					// ServicePointManager.ServerCertificateValidationCallback -= validator;
					}
			
				});
			

			}

		/// <summary>
		/// Проверяет полученный сертификат
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="cert"></param>
		/// <param name="chain"></param>
		/// <param name="error"></param>
		/// <returns></returns>
		bool RemoteCertValidate(object sender, X509Certificate cert, X509Chain chain, System.Net.Security.SslPolicyErrors error)
			{

			string ErrDesc = ErrDesc = "Ошибок нет";
			switch(error)
				{
				case SslPolicyErrors.RemoteCertificateChainErrors:
					ErrDesc = "Chain возвратил непустой массив";
					break;
				case SslPolicyErrors.RemoteCertificateNameMismatch:
					ErrDesc = "Несовпадение имён сертификатов";
					break;
				case SslPolicyErrors.RemoteCertificateNotAvailable:
					ErrDesc = "Сертификат недоступен";
					break;
				}
			Utility.Log(log, "[RCTV] Issuer={0}", cert.Issuer);
			Utility.Log(log, "[RCTV] Hash={0}", cert.GetCertHashString().ToLower());
			Utility.Log(log, "[RCTV] {0}", ErrDesc);
			// Console.WriteLine("{0} [{1:d2}] RemoteCertValidate: Hash: {2}",
			//	DateTime.Now.ToLongTimeString(), Thread.CurrentThread.ManagedThreadId, cert.GetCertHashString().ToLower());

			return true;

			/*
			if (cert.Subject == subjectName)
			{
				return true;
			}

			return false;
			 */
			}

		}

	}
