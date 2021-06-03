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
using WcfSmpp.Proxy;

namespace WcfSmpp
	{
	public class NotifyService
		{
		string log = Settings.OldiGW.LogFile;
		public void Notify(string List, string Message)
			{

			string From = "RegPlat";
			string[] phones = null;
			ChannelFactory<IXSMPP> myChannelFactory = null;

			try
				{
				// Открыть TCP-канал
				NetTcpBinding binding = new NetTcpBinding(SecurityMode.None);
				EndpointAddress endPointAddress = new EndpointAddress("net.tcp://odbs1.regplat.ru:1101/sms");
				myChannelFactory = new ChannelFactory<IXSMPP>(binding, endPointAddress);
				IXSMPP Client = myChannelFactory.CreateChannel();
				Response r = null;

				if (List.IndexOf(',') != -1 || List.IndexOf(';') != -1 || List.IndexOf('|') != -1 || List.IndexOf(' ') != -1)
					{
					phones = List.Split(new Char[] { ' ', ',', ';', '|' });
					StringBuilder sb = new StringBuilder();
					foreach (string p in phones)
						{
						r = Client.Send(From, p, Message);
						if (r.errCode == 0)
							Utility.Log(log, "[SMSS] Уведомление {0} на {1} отправлено", Message, p);
						else
							Utility.Log(log, "[SMSS] Уведомление {0} на {1} не отправлено", Message, p);
						}
					}
				else
					{
					r = Client.Send(From, List, Message);
					if (r.errCode == 0)
						Utility.Log(log, "[SMSS] Уведомление {0} на {1} отправлено", Message, List);
					else
						Utility.Log(log, "[SMSS] Уведомление {0} на {1} не отправлено", Message, List);
					}
				}
			catch (Exception ex)
				{
				Utility.Log(log, ex.ToString());
				}
			finally
				{
				if (myChannelFactory != null)
					myChannelFactory.Close();
				}
			}

		}
	}
