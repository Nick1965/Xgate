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
			byte GorodState = 3;

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
							GorodState = (byte)DataReader.GetInt32(0); // Т.к. параметр 1
							return GorodState;
							}
						}
					}
				}

			return 12;

			}

		}
	}