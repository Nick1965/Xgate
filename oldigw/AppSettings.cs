using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace OldiGW
{
    public static class Settings
    {
        static string title;
        static string version;
        static string root;
        static string logPath;
        static string templates;
        static string cyber;
        static string port;
        static string sslPort;
        static string connectionString;
        static string dbCheckTimeout;

        public static string Title { get { return title; } }
        public static string Version { get { return version; } }
        
        public static string Root { get { return root; } }
        public static string LogPath { get { return logPath; } }
        public static string Templates { get { return templates; } }

        public static int Port { get { return Convert.ToInt32(port); } }
        public static int SslPort { get { return Convert.ToInt32(sslPort); } }

        /// <summary>
        /// Строка подключения к БД
        /// </summary>
        public static string ConnectionString { get { return connectionString; } }
        
        /// <summary>
        /// Время попытки проверки БД при старте шлюза
        /// </summary>
        public static int DbCheckTimeout { get { return Convert.ToInt32(dbCheckTimeout); } }
        
        // Общая секция для шлюза
        public static class OldiGW
        {
            public static string LogFile
            {
                get { return LogPath + "OldiGW.log"; }
            }
        }

        // Секции провайдеров
        // MTS
        public static class Mts
        {
            public static string LogFile {
                get { return LogPath + ProvidersSettings.Mts.Log; }
            }
        }

        // CyberPlat
        public static class Cyber
        {
            public static string LogFile
            {
                get { return LogPath + ProvidersSettings.Cyber.Log; }
            }
        }
        

        /// <summary>
        /// Путь к папке КиберПлат
        /// </summary>
        public static string CyberPath { get { return cyber; } }

        /// <summary>
        /// Чтение файла *.exe.config
        /// </summary>
        public static void ReadExeConfig()
        {

            AppSettingsReader ar = new AppSettingsReader();

            title = (string)ar.GetValue("Title", typeof(string));
            version = (string)ar.GetValue("Version", typeof(string));

            root = (string)ar.GetValue("Root", typeof(string));
            if (root.Substring(root.Length - 1, 1) != "\\") root += "\\";
            logPath = root + (string)ar.GetValue("LogPath", typeof(string));
            templates = root + (string)ar.GetValue("Templates", typeof(string));

            cyber = root + (string)ar.GetValue("Cyber", typeof(string));

            port = (string)ar.GetValue("Port", typeof(string));
            sslPort =(string)ar.GetValue("SslPort", typeof(string));

            connectionString = (string)ar.GetValue("ConnectionString", typeof(string));
            dbCheckTimeout = (string)ar.GetValue("DbCheckTimeout", typeof(string));
        }
    }
}
