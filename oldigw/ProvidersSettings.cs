using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Configuration;
using System.Collections.Specialized;

namespace Oldi.Net
{

    public static class ProvidersSettings
    {
        static MtsSection mtsSection;
        static CyberSection cyberSection;
        static PccSection pccSection;

        public static class Pcc
        {
            public static string Name { get { return pccSection.Name; } }
            public static string Host { get { return pccSection.Host; } }
            public static string Log { get { return pccSection.Log; } }
        }


        public static void GetPccSection()
        {

            try
            {
                // Get the current configuration file.
                System.Configuration.Configuration config =
                        ConfigurationManager.OpenExeConfiguration(
                        ConfigurationUserLevel.None) as Configuration;

                pccSection = config.GetSection("pccSection") as PccSection;

                Settings.Pcc.Host = Pcc.Host;

            }
            catch (ConfigurationErrorsException err)
            {
                Console.WriteLine("Using GetSection(string): {0}", err.ToString());
            }
        }

        public static class Mts
        {
            public static string Name { get { return mtsSection.Name; } }
            public static string Host { get { return mtsSection.Host; } }
            public static string Log { get { return mtsSection.Log; } }
        }

        public static void GetMtsSection()
        {

            try
            {
                // Get the current configuration file.
                System.Configuration.Configuration config =
                        ConfigurationManager.OpenExeConfiguration(
                        ConfigurationUserLevel.None) as Configuration;

                mtsSection = config.GetSection("mtsSection") as MtsSection;

                Settings.Mts.Host = Mts.Host;

            }
            catch (ConfigurationErrorsException err)
            {
                Console.WriteLine("Using GetSection(string): {0}", err.ToString());
            }
        }

        public static class Cyber
        {
            public static string Name { get { return cyberSection.Name; } }
            public static string Host { get { return cyberSection.Host; } }
            public static string PayCheck { get { return cyberSection.PayCheck; } }
            public static string Pay { get { return cyberSection.Pay; } }
            public static string PayStatus { get { return cyberSection.PayStatus; } }
            public static string Log { get { return cyberSection.Log; } }

            public static string SD { get { return cyberSection.SD; } }
            public static string AP { get { return cyberSection.AP; } }
            public static string OP { get { return cyberSection.OP; } }
            public static string SecretKey { get { return cyberSection.SecretKey; } }
            public static string PublicKeys { get { return cyberSection.PublicKeys; } }
            public static string Passwd { get { return cyberSection.Passwd; } }
            public static string BankKeySerial { get { return cyberSection.BankKeySerial; } }
        }


        public static void GetCyberSection()
        {

            try
            {
                // Get the current configuration file.
                System.Configuration.Configuration config =
                        ConfigurationManager.OpenExeConfiguration(
                        ConfigurationUserLevel.None) as Configuration;

                cyberSection = config.GetSection("cyberSection") as CyberSection;

            }
            catch (ConfigurationErrorsException err)
            {
                Console.WriteLine("Using GetSection(string): {0}", err.ToString());
            }
        }
    }
}
