using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Oldi.Utility;
using System.IO;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Collections.Specialized;

namespace Oldi.Ekt
{
    public class TppRecord
    {
        public string Id;
        public string Kladr;
        public string City;
        public string Address;
        public TppRecord(string Id, string Kladr, string City, string Address)
        {
            this.Id = Id;
            this.Kladr = Kladr;
            this.City = City;
            this.Address = Address;
        }
    }

    public static class Mts
    {

        static string termFolder = "";
        static string logFile = Config.AppSettings["Root"] + Config.AppSettings["Root"] + "regtpp.log";
        static string sqlConnection = Config.AppSettings["CoinnectionString"];
        static List<TppRecord> terminals = new List<TppRecord>();

        static string host = Settings.Ekt.Host;
		static string CodePage = Settings.Ekt.Codepage;
		static string ContentType = Settings.Ekt.ContentType;
		static string commonName = Settings.Ekt.Certname;

        static string pointid = Settings.Ekt.Pointid;

        /// <summary>
        /// Регистрация терминалов МТС
        /// </summary>
        public static void RegTerminals()
        {
            termFolder = Config.AppSettings["Root"] + "tpp\\";
            ReadTppList();
        }

        #region ReadTppList

        /// <summary>
        /// Чтение списка терминалов
        /// </summary>
        static void ReadTppList()
        {
            try
            {
                Console.WriteLine($"Загрузка списка из {termFolder}");
                OnLog($"Загрузка списка из {termFolder}");

                DirectoryInfo dir = new DirectoryInfo(termFolder);
                foreach (FileInfo f in dir.GetFiles("*.xls"))
                    if (f.Extension.ToLower() == ".xls")
                    {
                        string excelConnectionString = string.Format("Provider=Microsoft.Jet.OLEDB.4.0;Data Source={0};Extended Properties=Excel 8.0", f.FullName);

                        Console.WriteLine($"Найдена таблица: {f.Name}");
                        OnLog($"Найдена таблица: {f.Name}");

                        using (OleDbConnection cnn = new OleDbConnection(excelConnectionString))
                        {

                            string queryString = "SELECT * FROM [ЛИСТ1$]";

                            using (OleDbCommand cmd = new OleDbCommand(queryString, cnn))
                            {
                                cmd.CommandType = System.Data.CommandType.TableDirect;
                                cnn.Open();
                                OleDbDataReader dr = cmd.ExecuteReader(/*System.Data.CommandBehavior.Default*/);
                                // Пропустить первые 3 строки
                                // int line = 0;

                                string[] prms = new string[25];
                                while (dr.Read())
                                {
                                    // if (++line < 5) continue;
                                    OnLog($"id={prms[1]} kladr={prms[11]} city={prms[16]} address={prms[17]}, {prms[18]}");
                                    // WriteToServer(prms);
                                    terminals.Add(new Ekt.TppRecord(prms[1], prms[11], prms[17], prms[18]));
                                }
                            }
                        }
                    }
            }
            catch (Exception ex)
            {
                OnLog(ex.ToString());
            }
        }

        #endregion ReadTppList


        /// <summary>
        /// Отправка пакета для регистрации терминалов
        /// </summary>
        static void SendTppList()
        {

            OnLog($"Подготовлен пакет:\r\n{MakeTppPacket()}");

        }

        /// <summary>
        /// Подготовка пакета регистрации терминалов
        /// </summary>
        /// <returns></returns>
        static string MakeTppPacket()
        {
            string packet = $"<request point=\"{pointid}\"\r\n";

            foreach (TppRecord tpp in terminals)
            {
                packet += $"\t<point id=\"{tpp.Id}\" kladr=\"{tpp.Kladr}\" city=\"{tpp.City}\" address=\"{tpp.Address}\" />\r\n";
            }

            return packet + $"<status id></request>";
        }

        static void OnLog(string text)
        {
            Oldi.Net.Utility.Log(logFile, text);
        }
    }
}
