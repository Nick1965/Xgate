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

namespace WcfSmpp
	{
	public class NotifyService
		{
		string log = Settings.OldiGW.LogFile;
		string From = "X-Gate";
		public void Notify(string List, string Message)
			{

			XWcfApiServiceClient Client = null;
			TaskFactory factory = new TaskFactory(TaskCreationOptions.LongRunning, TaskContinuationOptions.None);

			PermissiveCertificatePolicy.Enact("CN=apitest.regplat.ru");

			factory.StartNew(() =>
				{
				try
					{
					Result r = null;
					string[] phones;
					Client = new XWcfApiServiceClient();
					if (Message.IndexOf(',') != -1 || Message.IndexOf(';') != -1 || Message.IndexOf('|') != -1 || Message.IndexOf(' ') != -1)
						{
						phones = List.Split(new Char[] { ' ', ',', ';', '|' });
						r = Client.SendTextMass(21, From, Message, phones);
						}
					else
						r = Client.SendText(21, From, List, Message);
					Utility.Log(log, "[SMSS] Уведомление {0} на {1} отправлено", Message, List);
					}
				catch (Exception ex)
					{
					Utility.Log(log, "[SMSS] Уведомление {0} на {1} не отправлено", Message, List);
					Utility.Log(log, "{0}\r\n{1}", ex.Message, ex.StackTrace);
					}
				finally
					{
					if (Client != null)
						Client.Close();
					PermissiveCertificatePolicy.Deact();
					}
			
				});
			

			}
	
		}

	/// <summary>
	/// Проверяет сертификат службы
	/// </summary>
	class PermissiveCertificatePolicy
		{
		static PermissiveCertificatePolicy currentPolicy;
		string log = Settings.OldiGW.LogFile; 
		string subjectName;
		static object TheLock = new object();
		static int Processes = 0;

		/// <summary>
		/// Регистрирует событие провеки сертификата службы
		/// </summary>
		/// <param name="subjectName"></param>
		PermissiveCertificatePolicy(string subjectName)
			{
			this.subjectName = subjectName;
			lock (TheLock)
				{
				if (ServicePointManager.ServerCertificateValidationCallback != null)
					ServicePointManager.ServerCertificateValidationCallback +=
						new System.Net.Security.RemoteCertificateValidationCallback(RemoteCertValidate);
				Processes++;
				}
			}

		public static void Enact(string subjectName)
			{
			currentPolicy = new PermissiveCertificatePolicy(subjectName);
			}

		public static void Deact()
			{
			lock (TheLock)
				{
				--Processes;
				if (Processes == 0)
					ServicePointManager.ServerCertificateValidationCallback = null;
				}
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

			Utility.Log(log, "[RCTV] Issuer={0}", cert.Issuer);
			Utility.Log(log, "[RCTV] Hash={0}", cert.GetCertHashString().ToLower());
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
