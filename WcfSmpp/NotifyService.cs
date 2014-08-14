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
		string log = ".\\log\\oldigw.log";
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
						r = Client.SendTextMass(21, "X-Gate", Message, phones);
						}
					else
						r = Client.SendText(21, "X-Gate", List, Message);
					Utility.Log(log, "[SMSS] Уведомление {0} на {1} отправлено", Message, List);
					}
				catch (Exception ex)
					{
					Utility.Log(log, "[SMSC] Уведомление {0} на {1} не отправлено", Message, List);
					Utility.Log(log, "{0}\r\n{1}", ex.Message, ex.StackTrace);
					}
				finally
					{
					if (Client != null)
						Client.Close();
					}
			
				});
			

			}
	
		}

	/// <summary>
	/// Проверяет сертификат службы
	/// </summary>
	class PermissiveCertificatePolicy
		{
		string log = ".\\log\\oldigw.log"; string subjectName;
		static PermissiveCertificatePolicy currentPolicy;
		/// <summary>
		/// Регистрирует событие провеки сертификата службы
		/// </summary>
		/// <param name="subjectName"></param>
		PermissiveCertificatePolicy(string subjectName)
			{
			this.subjectName = subjectName;
			ServicePointManager.ServerCertificateValidationCallback +=
                new System.Net.Security.RemoteCertificateValidationCallback(RemoteCertValidate);
			}

		public static void Enact(string subjectName)
			{
			currentPolicy = new PermissiveCertificatePolicy(subjectName);
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

			Utility.Log(log, "RemoteCertValidate: CN={0} Hash={1}", cert.Subject, cert.GetCertHashString().ToLower());
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
