using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data.Sql;
using Oldi.Utility;
using System.Data;

namespace Oldi.Net
{
    public class Database
    {

        public class Param
        {
            SqlDataReader dr = null;


            public Param(SqlDataReader dr)
            {
                this.dr = dr;
            }

            /// <summary>
            /// Чтение параметра
            /// </summary>
            /// <param name="p">Наименование параметра</param>
            /// <param name="value">Значение параметра</param>
            public void Read(string p, out long value)
            {
				// Log("Param name={0}", p);
				if (dr.IsDBNull(dr.GetOrdinal(p)))
					value = long.MinValue;
				else
					value = dr.GetInt64(dr.GetOrdinal(p));

            }
            /// <summary>
            /// Чтение параметра
            /// </summary>
            /// <param name="p">Наименование параметра</param>
            /// <param name="value">Значение параметра</param>
            public void Read(string p, out int value)
            {
				// Log("Param name={0}", p);
				if (dr.IsDBNull(dr.GetOrdinal(p)))
					value = int.MinValue;
				else
					value = dr.GetInt32(dr.GetOrdinal(p));
            }
            /// <summary>
            /// Чтение параметра
            /// </summary>
            /// <param name="p">Наименование параметра</param>
            /// <param name="value">Значение параметра</param>
            public void Read(string p, out short value)
            {
				// Log("Param name={0}", p);
				if (dr.IsDBNull(dr.GetOrdinal(p)))
					value = short.MinValue;
				else
					value = dr.GetInt16(dr.GetOrdinal(p));
			}
            /// <summary>
            /// Чтение параметра
            /// </summary>
            /// <param name="p">Наименование параметра</param>
            /// <param name="value">Значение параметра</param>
            public void Read(string p, out byte value)
            {
				// Log("Param name={0}", p);
				if (dr.IsDBNull(dr.GetOrdinal(p)))
					value = 255;
				else
					value = dr.GetByte(dr.GetOrdinal(p));
			}
            /// <summary>
            /// Чтение параметра
            /// </summary>
            /// <param name="p">Наименование параметра</param>
            /// <param name="value">Значение параметра</param>
            public void Read(string p, out decimal value)
            {
				// Log("Param name={0}", p);
				if (dr.IsDBNull(dr.GetOrdinal(p)))
					value = decimal.MinusOne;
				else
					value = dr.GetDecimal(dr.GetOrdinal(p));
			}
            /// <summary>
            /// Чтение параметра
            /// </summary>
            /// <param name="p">Наименование параметра</param>
            /// <param name="value">Значение параметра</param>
            public void Read(string p, out DateTime value)
            {
				// Log("Param name={0}", p);
				if (dr.IsDBNull(dr.GetOrdinal(p)))
					value = DateTime.MinValue;
				else
					value = dr.GetDateTime(dr.GetOrdinal(p));
			}
			/// <summary>
			/// Чтение параметра DateTimeTZ
			/// </summary>
			/// <param name="p">Наименование параметра</param>
			/// <param name="value">Значение параметра</param>
			public void Read(string p, out DateTime? value)
			{
				// Log("Param name={0}", p);
				if (!dr.IsDBNull(dr.GetOrdinal(p)))
					value = (dr.GetDateTime(dr.GetOrdinal(p)));
				else
					value = null;
			}
			/// <summary>
            /// Чтение параметра
            /// </summary>
            /// <param name="p">Наименование параметра</param>
            /// <param name="value">Значение параметра</param>
            public void Read(string p, out string value)
            {
				// Log("Param name={0}", p);
					if (dr.IsDBNull(dr.GetOrdinal(p)))
						value = null;
					else
						value = dr.GetString(dr.GetOrdinal(p));
			}

			void Log(string fmt, params object[] _params)
			{
				Utility.Log(Settings.OldiGW.LogFile, fmt, _params);
			}
		
		}
   }
	public class SqlParamCommand : IDisposable
	{
		SqlConnection cnn = null;
		SqlCommand cmd = null;
		bool disposed = false;
		int errCode;
		string errDesc;

		public int ErrCode { get { return errCode; } }
		public string ErrDesc { get { return errDesc; } }

		/// <summary>
		/// Connection
		/// </summary>
		public SqlConnection Connection 
		{ 
			get 
			{ 
				return cmd == null? null: cmd.Connection; 
			} 
			set 
			{ 
				if (cmd != null)
					cmd.Connection = value; 
			} 
		}
		
		/// <summary>
		/// Конструктор
		/// </summary>
		/// <param name="connectionString">Строка соединения с БД</param>
		/// <param name="procName">Имя зранимой процедуры</param>
		public SqlParamCommand(string connectionString, string procName)
		{
			cnn = new SqlConnection(connectionString);
			cmd = new SqlCommand(procName, cnn);
			cmd.CommandType = System.Data.CommandType.StoredProcedure;
		}

		/// <summary>
		/// Открытие соединения
		/// </summary>
		public void ConnectionOpen()
		{
			cmd.Connection.Open();
		}

		/// <summary>
		/// Закрывает соединение к БДД
		/// </summary>
		public void ConnectionClose()
		{
			if (cnn != null && cnn.State != ConnectionState.Closed)
				cnn.Close();
		}

		/// <summary>
		/// Чтение строк из БД
		/// </summary>
		/// <returns></returns>
		public SqlDataReader ExecuteReader(CommandBehavior cb = CommandBehavior.Default)
		{
			return cmd.ExecuteReader(cb);
		}

		/// <summary>
		///  Чтение из БД только кода ошибки и ее описания
		/// </summary>
		public SqlDataReader Execute(CommandBehavior cb = CommandBehavior.Default)
		{
			SqlDataReader dataReader = cmd.ExecuteReader(cb);
			if (dataReader != null && dataReader.HasRows)
				if (dataReader.Read())
				{
					errCode = dataReader.GetInt32(dataReader.GetOrdinal("ErrCode"));
					errDesc = dataReader.GetString(dataReader.GetOrdinal("ErrDesc"));
				}
			return dataReader;
		}
		
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		public virtual void Dispose(bool disposing)
		{
			if (!disposed)
			{
				disposed = true;

				// Утилизация управляемых ресурсов
				if (disposing)
				{
					if (cmd != null)
					{
						cmd.Dispose();
						cmd = null;
					}
				}

				// Утилизация неуправляемых ресурсов
				if (cnn != null)
				{
					cnn.Close();
					cnn.Dispose();
					cnn = null;
				}
			}
		}

		/// <summary>
		/// Добавление параметра в коллекцию параметров
		/// </summary>
		/// <param name="cmd">SqlCommand</param>
		/// <param name="param">Наименование параметра</param>
		/// <param name="value">Значнение параметра</param>
		public void AddParam(string param, string value)
		{
			cmd.Add(param, value);
		}

		public void AddParam(string param, decimal value)
		{
			cmd.Add(param, value);
		}

		public void AddParam(string param, DateTime value)
		{
			cmd.Add(param, value);
		}

		public void AddParam(string param, DateTime? value)
		{
			cmd.Add(param, value);
		}

		public void AddParam(string param, int value)
		{
			cmd.Add(param, value);
		}

		public void AddParam(string param, long value)
		{
			cmd.Add(param, value);
		}

		public void AddParam(string param, short value)
		{
			cmd.Add(param, value);
		}

		public void AddParam(string param, byte value)
		{
			cmd.Add(param, value);
		}

		public void AddParam(string param, bool value)
		{
			cmd.Add(param, value);
		}


	}


	/// <summary>
	/// Класс возвращающий SqlDataReader
	/// </summary>
	public class DatabaseReader : IDisposable
	{
		string spname;
		SqlConnection cnn;
		SqlCommand cmd;
		bool disposed = false;
		SqlDataReader dataReader;

		/// <summary>
		/// Параметры процедуры
		/// </summary>
		public SqlParameterCollection Parametrs { get { return cmd.Parameters; } }

		public DatabaseReader(string spname)
		{
			this.spname = spname;
			cnn = new SqlConnection(Settings.ConnectionString);
			cmd = new SqlCommand("OldiGW.Ver3_" + spname, cnn);
		}

		public SqlDataReader ExecuteReader(CommandBehavior cb = CommandBehavior.Default)
		{
			dataReader = cmd.ExecuteReader(cb);
			return dataReader;
		}

		public void Execute()
		{
			cmd.ExecuteNonQuery();
		}

		/// <summary>
		/// Закрывает Reader и делает доступными выходные параметры
		/// </summary>
		public void CloseReader()
		{
			if (dataReader != null && !dataReader.IsClosed)
				dataReader.Close();
		}

		// Извлечение параметров из SqlDataReader
		#region GetValue
		/// <summary>
		/// Извлечение строки из SqlDataReader
		/// </summary>
		/// <param name="param">Наименование параметра</param>
		/// <param name="value">Выходное значение</param>
		public void GetValue(string param, out string value)
		{
			int c;

			value = null;
			try
			{
				c = dataReader.GetOrdinal(param);
				if (!dataReader.IsDBNull(c))
					value = dataReader.GetString(c);
			}
			catch (IndexOutOfRangeException ie)
			{
				throw new ApplicationException(string.Format("Параметр {0} в процедуре {1}", param, spname, ie.Message));
			}
			catch (InvalidCastException ce)
			{
				throw new ApplicationException(string.Format("Параметр {0} в процедуре {1}", param, spname, ce.Message));
			}
		}
		/// <summary>
		/// Извлечение байта из SqlDataReader
		/// </summary>
		/// <param name="param">Наименование параметра</param>
		/// <param name="value">Выходное значение</param>
		public void GetValue(string param, out byte value)
		{
			int c;

			value = 255;
			try
			{
				c = dataReader.GetOrdinal(param);
				if (!dataReader.IsDBNull(c))
					value = dataReader.GetByte(c);
			}
			catch (IndexOutOfRangeException ie)
			{
				throw new ApplicationException(string.Format("Параметр {0} в процедуре {1}", param, spname, ie.Message));
			}
			catch (InvalidCastException ce)
			{
				throw new ApplicationException(string.Format("Параметр {0} в процедуре {1}", param, spname, ce.Message));
			}
		}
		/// <summary>
		/// Извлечение короткого целого из SqlDataReader
		/// </summary>
		/// <param name="param">Наименование параметра</param>
		/// <param name="value">Выходное значение</param>
		public void GetValue(string param, out short value)
		{
			int c;

			value = short.MinValue;
			try
			{
				c = dataReader.GetOrdinal(param);
				if (!dataReader.IsDBNull(c))
					value = dataReader.GetInt16(c);
			}
			catch (IndexOutOfRangeException ie)
			{
				throw new ApplicationException(string.Format("Параметр {0} в процедуре {1}", param, spname, ie.Message));
			}
			catch (InvalidCastException ce)
			{
				throw new ApplicationException(string.Format("Параметр {0} в процедуре {1}", param, spname, ce.Message));
			}
		}
		/// <summary>
		/// Извлечение целого из SqlDataReader
		/// </summary>
		/// <param name="param">Наименование параметра</param>
		/// <param name="value">Выходное значение</param>
		public void GetValue(string param, out int value)
		{
			int c;

			value = int.MinValue;
			try
			{
				c = dataReader.GetOrdinal(param);
				if (!dataReader.IsDBNull(c))
					value = dataReader.GetInt32(c);
			}
			catch (IndexOutOfRangeException ie)
			{
				throw new ApplicationException(string.Format("Параметр {0} в процедуре {1}", param, spname, ie.Message));
			}
			catch (InvalidCastException ce)
			{
				throw new ApplicationException(string.Format("Параметр {0} в процедуре {1}", param, spname, ce.Message));
			}
		}
		/// <summary>
		/// Извлечение длинного целого из SqlDataReader
		/// </summary>
		/// <param name="param">Наименование параметра</param>
		/// <param name="value">Выходное значение</param>
		public void GetValue(string param, out long value)
		{
			int c;

			value = long.MinValue;
			try
			{
				c = dataReader.GetOrdinal(param);
				if (!dataReader.IsDBNull(c))
					value = dataReader.GetInt64(c);
			}
			catch (IndexOutOfRangeException ie)
			{
				throw new ApplicationException(string.Format("Параметр {0} в процедуре {1}", param, spname, ie.Message));
			}
			catch (InvalidCastException ce)
			{
				throw new ApplicationException(string.Format("Параметр {0} в процедуре {1}", param, spname, ce.Message));
			}
		}
		/// <summary>
		/// Извлечение денежного значения из SqlDataReader
		/// </summary>
		/// <param name="param">Наименование параметра</param>
		/// <param name="value">Выходное значение</param>
		public void GetValue(string param, out decimal value)
		{
			int c;

			value = decimal.MinValue;
			try
			{
				c = dataReader.GetOrdinal(param);
				if (!dataReader.IsDBNull(c))
					value = dataReader.GetDecimal(c);
			}
			catch (IndexOutOfRangeException ie)
			{
				throw new ApplicationException(string.Format("Параметр {0} в процедуре {1}", param, spname, ie.Message));
			}
			catch (InvalidCastException ce)
			{
				throw new ApplicationException(string.Format("Параметр {0} в процедуре {1}", param, spname, ce.Message));
			}
		}
		/// <summary>
		/// Извлечение даты/времени из SqlDataReader
		/// </summary>
		/// <param name="param">Наименование параметра</param>
		/// <param name="value">Выходное значение</param>
		public void GetValue(string param, out DateTime value)
		{
			int c;

			value = DateTime.MinValue;
			try
			{
				c = dataReader.GetOrdinal(param);
				if (!dataReader.IsDBNull(c))
					value = dataReader.GetDateTime(c);
			}
			catch (IndexOutOfRangeException ie)
			{
				throw new ApplicationException(string.Format("Параметр {0} в процедуре {1}", param, spname, ie.Message));
			}
			catch (InvalidCastException ce)
			{
				throw new ApplicationException(string.Format("Параметр {0} в процедуре {1}", param, spname, ce.Message));
			}
		}
		/// <summary>
		/// Извлечение даты/времени из SqlDataReader
		/// </summary>
		/// <param name="param">Наименование параметра</param>
		/// <param name="value">Выходное значение</param>
		public void GetValue(string param, out DateTime? value)
		{
			int c;

			value = null;
			try
			{
				c = dataReader.GetOrdinal(param);
				if (!dataReader.IsDBNull(c))
					value = dataReader.GetDateTime(c);
			}
			catch (IndexOutOfRangeException ie)
			{
				throw new ApplicationException(string.Format("Параметр {0} в процедуре {1}", param, spname, ie.Message));
			}
			catch (InvalidCastException ce)
			{
				throw new ApplicationException(string.Format("Параметр {0} в процедуре {1}", param, spname, ce.Message));
			}
		}
		#endregion

		/// <summary>
		/// Добавление параметра в процедуру
		/// </summary>
		/// <param name="name"></param>
		/// <param name="value"></param>
		public void Add(string name, object value)
		{
			if (value == null)
				return;

			switch (value.GetType().Name)
			{
				case "String":
					if ((string)value == "")
						return;
					break;
				case "Char":
					if ((char)value == (char)0)
						return;
					break;
				case "Byte":
					if ((byte)value == 255)
						return;
					break;
				case "Int16":
					if ((short)value == short.MinValue)
						return;
					break;
				case "Int32":
					if ((int)value == int.MinValue)
						return;
					break;
				case "Int64":
					if ((long)value == long.MinValue)
						return;
					break;
				case "Decimal":
					if ((decimal)value == decimal.MinusOne)
						return;
					break;
				case "DateTime":
					if ((DateTime)value == DateTime.MinValue)
						return;
					break;
			}
			cmd.Parameters.AddWithValue(name, value);
		}

		/// <summary>
		/// Добаляет выходной параметр, коме VarChar и NVarChar
		/// </summary>
		/// <param name="name">Имя параметра</param>
		/// <param name="type">Тип параметра Type</param>
		public void AddOut(string name, Type type)
		{
			switch (type.Name)
			{
				case "Char":
					cmd.Parameters.Add(name, SqlDbType.NChar, 1).Direction = ParameterDirection.Output;
					break;
				case "Byte":
					cmd.Parameters.Add(name, SqlDbType.TinyInt).Direction = ParameterDirection.Output;
					break;
				case "Int16":
					cmd.Parameters.Add(name, SqlDbType.SmallInt).Direction = ParameterDirection.Output;
					break;
				case "Int32":
					cmd.Parameters.Add(name, SqlDbType.Int).Direction = ParameterDirection.Output;
					break;
				case "Int64":
					cmd.Parameters.Add(name, SqlDbType.BigInt).Direction = ParameterDirection.Output;
					break;
				case "Decimal":
					cmd.Parameters.Add(name, SqlDbType.Money).Direction = ParameterDirection.Output;
					break;
				case "DateTime":
					cmd.Parameters.Add(name, SqlDbType.DateTime2).Direction = ParameterDirection.Output;
					break;
			}

		}
		/// <summary>
		/// Добавляет выходной строковый параметр VarChar
		/// </summary>
		/// <param name="name">Имя параметра</param>
		/// <param name="size">Длина</param>
		public void AddOutVarchar(string name, int size)
		{
			cmd.Parameters.Add(name, SqlDbType.VarChar, size).Direction = ParameterDirection.Output;
		}
		/// <summary>
		/// Добавляет выходной строковый параметр NVarChar
		/// </summary>
		/// <param name="name">Имя параметра</param>
		/// <param name="size">Длина</param>
		public void AddOutNVarchar(string name, int size)
		{
			cmd.Parameters.Add(name, SqlDbType.NVarChar, size).Direction = ParameterDirection.Output;
		}
		
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		public virtual void Dispose(bool disposing)
		{
			if (!disposed)
			{
				disposed = true;

				// Утилизация управляемых ресурсов
				if (disposing)
				{
					CloseReader();
					
					if (cmd != null)
					{
						cmd.Dispose();
						cmd = null;
					}
				}

				// Утилизация неуправляемых ресурсов
				if (cnn != null)
				{
					cnn.Close();
					cnn.Dispose();
					cnn = null;
				}
			}
		}

	}



}
