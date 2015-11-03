using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Oldi.Net;

namespace RedirectPay
	{
	class RedirectPay
		{
		static void Main(string[] args)
			{

			Utility.Log(Config.logFile, "RedirectPya v1.0");
			Console.WriteLine("RedirectPya v1.0");

			try
				{
				Scan scan = new Scan(args.Length>0?args[0]:"");
				scan.Run();
				}
			catch(Exception ex)
				{
				Utility.Log(Config.logFile, ex.ToString());
				Console.WriteLine(ex.ToString());
				}

			}
		}
	}
