using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Oldi.Utility;
using System.Data.SqlClient;
using System.Data;
using System.Net;
using System.IO;
using System.Threading;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Globalization;
using System.Resources;
using System.Xml.Linq;
using System.ServiceModel;
using Oldi.Net.Proxy;
using System.IO.Compression;
using System.Data.Linq;

namespace Oldi.Net
	{
	public partial class GWRequest
		{

		/// <summary>
		/// Получить состояние платежа в системе ГОРОД
		/// </summary>
		/// <returns></returns>
		public byte GetGorodState()
			{

			string GorodCommandText = "select [state] from [gorod].[dbo].payment where tid = " + Tid.ToString();
			byte GorodState = 12;

			using (SqlConnection GorodConnection = new SqlConnection(Settings.GorodConnectionString))
			using (SqlCommand GorodCommand = new SqlCommand(GorodCommandText, GorodConnection))
				{
				GorodConnection.Open();
				using (SqlDataReader DataReader = GorodCommand.ExecuteReader(CommandBehavior.CloseConnection))
					{
					if (DataReader.HasRows)
						{
						if (DataReader.Read())
							GorodState = (byte)DataReader.GetInt32(0); // Т.к. параметр 1
						}
					}
				}

			return GorodState;

			}

		public class Payment
			{
			public long Tid;
			public byte State;
			public string Sub_inner_tid;
			}

		public class Gorod: DataContext
			{
			// public Table<Payment> Payments;
			public Gorod(string cnn)
				: base(cnn)
				{
				}
			}

		/// <summary>
		/// Получает статус платежа из ГОРОДа, используя Linq
		/// </summary>
		/// <returns></returns>
		public byte GetGorodStateLinq()
			{

			byte GorodState;
			string GorodCommandText = "select [state] from [gorod].[dbo].payment where tid = {0}";

			using (Gorod db = new Gorod(Settings.GorodConnectionString))
				{
				IEnumerable<Payment> Payments = db.ExecuteQuery<Payment>(GorodCommandText, Tid);
				GorodState = Payments.First<Payment>().State;
				}
			
			return GorodState;

			}

		public string GetGorodSubLinq()
			{

			string Sub;
			string GorodCommandText = "select [sub_inner_tid] from [gorod].[dbo].payment where tid = {0}";

			using (Gorod db = new Gorod(Settings.GorodConnectionString))
				{
				IEnumerable<string> Payments = db.ExecuteQuery<string>(GorodCommandText, Tid);
				Sub = Payments.First<string>();
				}

			return Sub;

			}

		}
	}