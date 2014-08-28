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
					}
			
				});
			

			}
	
		}

	}
