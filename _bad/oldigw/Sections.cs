using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace Oldi.Net
{
    #region Mts sections

    public sealed class MtsSection : ConfigurationSection
    {
        /// <summary>
        /// Конструктор
        /// </summary>
        MtsSection()
        {
        }

        /// <summary>
        /// Наименование провайдера
        /// </summary>
        [ConfigurationProperty("name", DefaultValue = "mts", IsRequired = true, IsKey = true)]
        public string Name
        {
            get
            {
                return (string)this["name"];
            }
            set
            {
                this["name"] = value;
            }
        }

        /// <summary>
        /// Наименование хоста подключения к ПУ
        /// </summary>
        [ConfigurationProperty("host", DefaultValue = "http://localhost/", IsRequired = true, IsKey = true)]
        [RegexStringValidator(@"\w+:\/\/[\w.]+\S*")]
        public string Host
        {
            get
            {
                return (string)this["host"];
            }
            set
            {
                this["host"] = value;
            }
        }

        /// <summary>
        /// Имя Log-файла
        /// </summary>
        [ConfigurationProperty("log", DefaultValue = "mts.log", IsRequired = true, IsKey = true)]
        [StringValidator(InvalidCharacters = " ~!@#$%^&*()[]{}/;'\"|\\", MinLength = 1, MaxLength = 60)]
        public string Log
        {
            get
            {
                return (string)this["log"];
            }
            set
            {
                this["log"] = value;
            }
        }
    }

    #endregion

    #region Cyber sections

    public sealed class CyberSection : ConfigurationSection
    {
        /// <summary>
        /// Конструктор
        /// </summary>
        CyberSection()
        {
        }

        /// <summary>
        /// Наименование провайдера
        /// </summary>
        [ConfigurationProperty("name", DefaultValue = "cyber", IsRequired = true, IsKey = true)]
        public string Name
        {
            get
            {
                return (string)this["name"];
            }
            set
            {
                this["name"] = value;
            }
        }

        /// <summary>
        /// Наименование хоста подключения к ПУ
        /// </summary>
        [ConfigurationProperty("host", DefaultValue = "http://localhost/", IsRequired = true, IsKey = true)]
        [RegexStringValidator(@"\w+:\/\/[\w.]+\S*")]
        public string Host
        {
            get
            {
                return (string)this["host"];
            }
            set
            {
                this["host"] = value;
            }
        }

        /// <summary>
        /// шаблон проверки платежа 
        /// </summary>
        [ConfigurationProperty("pay-check", DefaultValue = "{0}/{0}_pay_check.cgi", IsRequired = true, IsKey = true)]
        public string PayCheck
        {
            get
            {
                return (string)this["pay-check"];
            }
            set
            {
                this["pay-check"] = value;
            }
        }

        /// <summary>
        /// шаблон платежа 
        /// </summary>
        [ConfigurationProperty("pay", DefaultValue = "{0}/{0}_pay.cgi", IsRequired = true, IsKey = true)]
        public string Pay
        {
            get
            {
                return (string)this["pay"];
            }
            set
            {
                this["pay"] = value;
            }
        }

        /// <summary>
        /// шаблон проверки статуса
        /// </summary>
        [ConfigurationProperty("pay-status", DefaultValue = "{0}/{0}_pay_status.cgi", IsRequired = true, IsKey = true)]
        public string PayStatus
        {
            get
            {
                return (string)this["pay-status"];
            }
            set
            {
                this["pay-status"] = value;
            }
        }

        /// <summary>
        /// Имя Log-файла
        /// </summary>
        [ConfigurationProperty("log", DefaultValue = "cyber.log", IsRequired = true, IsKey = true)]
        [StringValidator(InvalidCharacters = " ~!@#$%^&*()[]{}/;'\"|\\", MinLength = 1, MaxLength = 60)]
        public string Log
        {
            get
            {
                return (string)this["log"];
            }
            set
            {
                this["log"] = value;
            }
        }
    }

    #endregion

}
