using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Oldi.Net;
using System.Data.SqlClient;

namespace RedirectPay
	{
	class RedirectPay
		{
		static void Main(string[] args)
			{

			Utility.Log(Properties.Settings.Default.LogFile, "RedirectPya v1.0");
			Console.WriteLine("RedirectPya v1.0");

			try
				{
				Scan scan = new Scan(args.Length>0?args[0]:"");
				scan.Run();
				}
			catch (SqlException se)
				{
				Utility.Log(Properties.Settings.Default.LogFile, "SQL: HRESULT={0} {1} Number={2}\r\n{3}", se.ErrorCode, se.Message, se.Number, se.StackTrace);
				}
			catch(Exception ex)
				{
				Utility.Log(Properties.Settings.Default.LogFile, ex.ToString());
				Console.WriteLine(ex.ToString());
				}

			}
		}
	}
