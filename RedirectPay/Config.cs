using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RedirectPay
	{
	public static class Config
		{
		public static string logFile = ".\\redirect.log";
		public static string RedoLogFile = ".\\redo.log";
		public static string ConnectorLogFile = ".\\connector.log";
		public static string GorodConnectionString = "Persist Security Info=False; Server=192.168.1.5; Initial Catalog=Gorod; User ID=sa; Password=4";
		public static string SimpleHost = "http://192.168.1.1:100/server-redo.mdl/";
		public static string Endpoint = "pay.xml";
		}
	}
