using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Oldi.Net;
using Oldi.Utility;
using System.IO;
using System.Xml.Linq;
using System.Data.OleDb;
using SocialExplorer.IO.FastDBF;
using System.Net;
using System.Net.Mail;

namespace Oldi.Smtp
{
	public class Smtp: GWRequest
	{
		string ToAddress = "plotnikovna@regplat.ru";
		string FromAddress = "outbox@regplat.ru";
		string Subject = "Test";
		string Body = "Hi!<br />Good bye!";

		public Smtp()
			:base()
		{
		}

		public Smtp(GWRequest req)
			: base(req)
		{
		}
		
		/// <summary>
		/// Имя файла локального лога
		/// </summary>
		/// <returns></returns>
		protected override string GetLogName()
		{
			return Settings.Smtp.LogFile;
		}
	
		/// <summary>
		/// Процессинг платежей
		/// </summary>
		/// <param name="New"></param>
		public override void Processing(bool New = true)
		{
			if (New)  // Новый платёж
			{
				if (MakePayment() == 0)
				{
					// ReportRequest("Begin");
					DoPay(0, 6);
				}
				// ReportRequest("End");
			}
			else // Redo
			{
				// ReportRequest("Redo start");
				DoPay(State, 6);
				// ReportRequest("Redo stop");
			}

		}

		/// <summary>
		/// Отправка платежа
		/// </summary>
		/// <param name="old_state">Старое состояние</param>
		/// <param name="try_state">Требуемое состояние</param>
		/// <returns></returns>
		public override int DoPay(byte old_state, byte try_state)
		{
			// Заполняем параметры отправки платежа
			
			// Создаём аттач только 1 раз
			if (old_state == 0)
				MakeRequest();
			
			// Отправим сообщение.
			state = SendEmail() == 0? (byte)6: (byte)3;

			return 0;
		}


		/// <summary>
		/// Отправляет e-mail ПУ
		/// </summary>
		/// <returns></returns>
		public int SendEmail()
		{
			try
			{
				SmtpClient smtp = new SmtpClient(Settings.Smtp.Host, int.Parse(Settings.Smtp.Port));
				smtp.Credentials = CredentialCache.DefaultNetworkCredentials;
				smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
				smtp.Send(FromAddress, ToAddress, Subject, Body);
			}
			catch (SmtpFailedRecipientsException rre)
			{
				foreach (SmtpFailedRecipientException re in rre.InnerExceptions)
					RootLog(re.Message);
				return 1;
			}
			catch (SmtpException se)
			{
				RootLog(se.Message);
				return 1;
			}
			catch (Exception ex)
			{
				RootLog("{0}\r\n{1}", ex.Message, ex.StackTrace);
				return 1;
			}

			return 0;
		}
		
		/// <summary>
		/// Создаёт аттач-файл для отправки по e-mail
		/// </summary>
		public void MakeRequest()
		{
			string template = "";
			using (StreamReader structure = new StreamReader(Settings.Templates + Service + "-ord-structure.tpl"))
			{
				template = structure.ReadToEnd();
			}
			if (string.IsNullOrEmpty(template))
				throw new ApplicationException(string.Format("Шаблон структуры таблицы {0} пуст", Settings.Templates + Service + "-ord-structure.tpl"));
	
			// Шаблон содержит структуру таблицы

			string tableName = Tid.ToString() + ".dbf";
			
			// Создадим объект Таблица
			DbfFile odbf = new DbfFile();
			// Откроем таблицу для записи в папку attachemts
			odbf.Open(Settings.Attachments + tableName, FileMode.Create);
			
			XElement root = XElement.Parse(stResponse);
			IEnumerable<XElement> fields =
				from el in root.Elements("fields")
				select el;

			string name;
			string type;
			string len;
			string p;

			foreach (XElement el in fields)
			{

				switch (el.Name.LocalName.ToString().ToLower())
				{
					case "field": // Поле
						len = "0";
						p = "0";
						name = "";
						type = "";
						foreach (XAttribute attr in el.Attributes())
						{
							if (attr.Name.LocalName.ToLower() == "name")
								name = attr.Value;
							else if (attr.Name.LocalName.ToLower() == "type")
								type = attr.Value;
							else if (attr.Name.LocalName.ToLower() == "len")
								len = attr.Value;
							else if (attr.Name.LocalName.ToLower() == "p")
								p = attr.Value;
						}
						if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(type))
							throw new ApplicationException("Пропущены обязательные значения name или type в определении столбца");

						odbf.Header.AddColumn(new DbfColumn(name, GetDbfType(type), int.Parse(len), int.Parse(p)));
	
						break;
					default:
						// Error = ErrTemplatelInvalid;
						throw new ApplicationException("Ошибка структуры таблицы");
				}
			}

			// Создаём таблицу .DBF с заданной структурой
			odbf.WriteHeader();
			odbf.Close();
			
			// Формируем запрос Insert Into () Values()

			StringBuilder sb = new StringBuilder();
			sb.AppendFormat("Insert Into {0} (", tableName);
			int n = 0;
			foreach (KeyValuePair<string, string> kvp in Attributes)
				sb.AppendFormat("{0}{1}", n++!=0?", ": "", kvp.Key);
			sb.Append(")\r\n Values( ");
			n = 0;
			foreach (KeyValuePair<string, string> kvp in Attributes)
				sb.AppendFormat("{0}{1}", n++ != 0 ? ", " : "", kvp.Value);
			sb.Append(");");
			
			Log("Выполняется запрос:\r\n{0}", sb.ToString());

			// Записываем атрибуты в файл .dbf
			using (OleDbConnection Connection =
				new OleDbConnection(string.Format("Provider=Microsoft.Jet.OLEDB.4.0;Persist Security Info=False;Extended Properties=dbase 5.0;Data Source={0};",
					Settings.Attachments)))
			{
				Connection.Open();
				using (OleDbCommand Command = new OleDbCommand(sb.ToString(), Connection))
				{
					Command.ExecuteNonQuery();
					Connection.Close();
				}
			}

			sb.Clear();
			sb = null;
		}

		DbfColumn.DbfColumnType GetDbfType(string typeName)
		{
			switch (typeName.ToLower())
			{
				case "char": return DbfColumn.DbfColumnType.Character;
				case "numeric": return DbfColumn.DbfColumnType.Number;
				case "bit": return DbfColumn.DbfColumnType.Binary;
				case "bool": return DbfColumn.DbfColumnType.Boolean;
				case "date": return DbfColumn.DbfColumnType.Date;
				case "int": return DbfColumn.DbfColumnType.Integer;
				case "memo": return DbfColumn.DbfColumnType.Memo;
			}
			
			throw new NotSupportedException(String.Format("{0} Этот тип не поддерживается.", typeName));
		}
	}
}
