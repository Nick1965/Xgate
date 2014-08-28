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
using System.Threading.Tasks;

namespace Oldi.Net
	{

	/// <summary>
	/// Проверяет сертификат службы
	/// </summary>
	class PermissiveCertificatePolicy
		{
		static PermissiveCertificatePolicy currentPolicy;
		string log = Settings.OldiGW.LogFile;
		string subjectName;

		/// <summary>
		/// Регистрирует событие провеки сертификата службы
		/// </summary>
		/// <param name="subjectName"></param>
		PermissiveCertificatePolicy(string subjectName)
			{
			this.subjectName = subjectName;
			if (ServicePointManager.ServerCertificateValidationCallback != null)
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
