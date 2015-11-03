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

		/// <summary>
		/// Возвращает номер транзакции
		/// </summary>
		/// <returns></returns>
		public string GetGorodSub()
			{

			string GorodCommandText = "select [sub_inner_tid] from [gorod].[dbo].payment where not sub_inner_tid like 'card-%' and tid = " + Tid.ToString();
			string Sub_inner_tid = "";

			using (SqlConnection GorodConnection = new SqlConnection(Settings.GorodConnectionString))
			using (SqlCommand GorodCommand = new SqlCommand(GorodCommandText, GorodConnection))
				{
				GorodConnection.Open();
				using (SqlDataReader DataReader = GorodCommand.ExecuteReader(CommandBehavior.CloseConnection))
					{
					if (DataReader.HasRows)
						{
						if (DataReader.Read())
							{
							Sub_inner_tid = DataReader.GetString(0); // Т.к. параметр 1
							// Посчитаем еоличество -
							int Count = 0;
							int Pos = 0;
							int last = 0;
							while (Pos != -1)
								{
								if (Pos < Sub_inner_tid.Length)
									{
									Pos = Sub_inner_tid.IndexOf('-', last);
									last = Pos;
									if (Pos != -1)
										{
										last = Pos + 1;
										Count++;
										}
									}
								}
							if (Count == 3)
								{
								RootLog("{0} [GetGorodSub] Check={1}", Tid, Sub_inner_tid);
								return Sub_inner_tid;
								}
							else
								return "";
							}
						}
					}
				}

			return "";

			}

		public class Payment
			{
			public long Tid;
			public byte State;
			}

		public class Gorod: DataContext
			{
			public Table<Payment> Payments;
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
	
		}
	}