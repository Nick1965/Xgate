using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Globalization;
using Oldi.Utility;

namespace Oldi.Net
{
	public partial class GWRequest
	{

		/// <summary>
		/// Разбор входного XML-запроса
		/// </summary>
		/// <param name="stResponse"></param>
		/// <returns></returns>
		public int Parse(string stSource)
        {
			DateTime dateValue;
			
			if (Settings.LogLevel.IndexOf("OEREQ") != -1)
				RootLog("\r\nПолучен запро:с\r\n{0}\r\n", stSource);

			state = 0;

            try
            {
                XDocument doc = XDocument.Parse(stSource);

                if (doc.Element("request").HasAttributes)
                {
                    requestType = (string)doc.Element("request").Attribute("type");
                }
                else
                {
                    ErrCode = Properties.Settings.Default.CodeLongWait;
                    ErrDesc = string.Format(Properties.Resources.MsgURT, "payment/status/undo/redo");
					Log("{0}\r\n", ErrDesc, stSource);
                    return ErrCode;
                }

                #region Parse
                var body = doc.Element("request")
                              .Elements();

				errCode = 0; // Если где-то будет ошибка разбора установится код, отличный от 0

				foreach (XElement el in body)
                {
		
					if (Settings.LogLevel.IndexOf("PARS") != -1)
						if (el.Name.LocalName.ToLower() != "provider")
							Log("Parse: {0} = {1}", el.Name.LocalName.ToLower(), el.Value);

					switch (el.Name.LocalName.ToLower())
                    {
                        case "provider":
                            provider = ((string)el.Attribute("name")).ToLower();
							foreach (XAttribute attr in el.Attributes())
							{
								if (attr.Name.LocalName.ToLower() == "gateway"
									|| attr.Name.LocalName.ToLower() == "principal"
									|| attr.Name.LocalName.ToLower() == "gw")
									gateway = (string)attr.Value;
								else if (attr.Name.LocalName.ToLower() == "service")
									service = (string)attr.Value;
							}
							if (Settings.LogLevel.IndexOf("PARS") != -1)
								Log("Parse: provider  = {0}, service = {1} gw = {2}", provider, service, gateway);
                            break;
                        case "tid":
                            if (!Int64.TryParse((string)el.Value, out tid))
								SetParseError(6, "Tid");
                            break;
                        case "phone":
                            phone = (string)el.Value;
							foreach (XAttribute attr in el.Attributes())
							{
								if (attr.Name.LocalName.ToLower() == "param")
									phoneParam = (string)attr.Value;
							}
                            break;
                        case "account":
                            account = (string)el.Value;
							bool zerronull = false;
							string split = "||";
							string[] keys = new string[20];
							int cnt = 0;
							foreach (XAttribute attr in el.Attributes())
							{
								if (attr.Name.LocalName.ToLower() == "param")
									accountParam = (string)attr.Value;
								else if (attr.Name.LocalName.ToLower() == "ifzerro")
									zerronull = (string)attr.Value == "null"? true: false;
								else if (attr.Name.LocalName.ToLower() == "split")
									split = (string)attr.Value;
								else if (attr.Name.LocalName.ToLower().Substring(0, 3) == "key" && cnt < keys.Length)
									keys[cnt++] = (string)attr.Value;
							}
							if (cnt > 0) // т.е. есть ключи к параметрам
							{
								for (int i = 0; i < cnt; i++)
									account += split + ValueOrNull(keys[i], zerronull);
							}
                            break;
						case "status-type":
							int val;
							if (int.TryParse(el.Value, out val))
								statusType = val;
							else
								statusType = null;
							break;
						case "start-date":
							if (DateTime.TryParse(el.Value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out dateValue))
								startDate = dateValue;
							else
								startDate = null;
							break;
						case "end-date":
							if (DateTime.TryParse(el.Value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out dateValue))
								endDate = dateValue;
							else
								endDate = null;
							break;
						case "attribute":
							// Коллекция дополнительных атрибутов:
							//		<attribute name="e-mail" value="info@mail.ru" />
							string n = null;
							string v = null;
							StringBuilder sb = new StringBuilder();
							n = "";
							v = "";
							foreach (XAttribute attr in el.Attributes())
								{
								if (attr.Name.LocalName.ToLower() == "name")
									n = (string)attr.Value;
								else if (attr.Name.LocalName.ToLower() == "value")
									v = (string)attr.Value;
								}
							if (!string.IsNullOrEmpty(n))
								{
								sb.AppendFormat("{0}={1};", n, v);
								Attributes.Add(n, v);
								}
							// if (Settings.LogLevel.IndexOf("PARS") != -1)
								Log("Parse: atrs={0}", sb.ToString());
							break;
						case "filial":
							// if (!byte.TryParse(el.Value, out filial))
							filial = el.Value;
							/*
								foreach (XAttribute attr in el.Attributes())
								{
									if (attr.Name.LocalName.ToLower() == "param")
										filialParam = (string)attr.Value;
								}
							*/
							break;
						case "card":
							card = (string)el.Value;
							break;
						case "amount":
							amount = decimal.Parse(el.Value.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture);
							// Console.WriteLine("{0} - {1}", el.Value, XConvert.AsAmount(amount));
                            break;
                        case "amount-all":
							amountAll = decimal.Parse(el.Value.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture);
                            break;
                        case "oid":	// Номер платежного инструмента
							oid = (string)el.Value;
							break;
						case "service":
                            service = (string)el.Value;
                            break;
                        case "terminal-id":
							if (!int.TryParse(el.Value, out terminal))
								terminal = Settings.FakeTppId;
                            break;
						case "agent-id":
							if (!int.TryParse(el.Value, out agentId))
								agentId = 0;
							break;
						case "transaction":
                            transaction = (string)el.Value;
                            break;
                        case "pc-date":
							if (!DateTime.TryParse(el.Value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out pcdate))
						// pcdate = DateTime.Parse(el.Value);
								pcdate = DateTime.Now; // Установим PCDate с этого момента
                            break;
						case "terminal-date":
							terminalDate = DateTime.Parse(el.Value);
							break;
						// Cyber
                        case "comment":
                            comment = (string)el.Value;
                            break;
                        case "orgname":
                            orgname = (string)el.Value;
                            break;
                        case "number":
                            number = (string)el.Value;
                            break;
                        case "fio":
                            fio = (string)el.Value;
							break;
                        case "docnum":
                            docnum = (string)el.Value;
                            break;
                        case "docdate":
                            docdate = (string)el.Value;
                            break;
                        case "purpose":
                            purpose = (string)el.Value;
                            break;
                        case "address":
                            address = (string)el.Value;
                            break;
                        case "agree":
							agree = byte.Parse(el.Value);
                            break;
                        case "inn":
                            inn = (string)el.Value;
                            break;
						case "kpp":
							kpp = (string)el.Value;
							break;
						case "payer-inn":
							payerInn = (string)el.Value;
							break;
						case "contact":
                            contact = (string)el.Value;
                            break;
                        // Дополнительные параметры:
						case "ben":
							ben = (string)el.Value;
							break;
						case "pay-type":
							payType = Convert.ToInt32(el.Value);
							break;
						case "reason":
							reason = (string)el.Value;
							break;
						case "tax":
							tax = -1;
							tax = Convert.ToInt32(el.Value);
							if (tax != 0 && tax != 10 && tax != 18)
								SetParseError(6, string.Format("НДС должен быть 0, 10 или 18! Получено значение {0}", tax));
							break;
						case "bik":
							bik = (string)el.Value;
							break;
						case "kbk":
							kbk = (string)el.Value;
							break;
						case "okato":
							okato = (string)el.Value;
							break;
						default:
                            if (requestType != "status")
								SetParseError(6, "", string.Format("Неверный тип запроса [{0}]=<{1}>", el.Name.LocalName, el.Value));
                            else
                            {
                                errCode = Properties.Settings.Default.CodeSUCS;
                                errDesc = Properties.Resources.MsgSUCS;
                            }
                            break;
                    }
                }

                
				if (ErrCode == 0)
                {
					// Запрос успешно разобран
					// Log("Запрос разобран:\r\n{0}", PrintParams());
                    // ErrDesc = Properties.Resources.MsgSUCS;
					// errDesc = string.Format("[{0}] Запрос разобран", requestType);
					errDesc = "";
                    return 0;
                }
                else
                    return errCode;
                #endregion Parse
            }
            catch (Exception ex)
            {
				ErrCode = 400; // Properties.Settings.Default.CodeLongWait;
				state = 12;
                ErrDesc = "Parse: " + ex.Message;
                Log("Parse: {0}\r\n{1}", ex.Message, ex.StackTrace);
            }

            return ErrCode;

        }


		string ValueOrNull(string value, bool zerronull)
		{
			string newvalue;

			if (zerronull)
			{
				newvalue = value.Replace('0', ' ').Trim();
				if (string.IsNullOrEmpty(newvalue))
					return "";
			}
			return value;
		}
		
		/// <summary>
		/// Установка ошибки разбора Parse
		/// </summary>
		/// <param name="err">Код ошибки</param>
		/// <param name="field">Имя поля</param>
		/// <param name="desc">Текст ошибки</param>
		protected void SetParseError(int err, string field, string desc = "")
		{
			errCode = err;
			if (desc == "")
				errDesc = string.Format("Неверный формат поля \"{0}\"", field);
			else
				errDesc = desc;
		}

	}
}
