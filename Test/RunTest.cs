using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Oldi.Net.Cyber;
using Oldi.Utility;
using System.Collections.Specialized;
using Oldi.Ekt;
using Oldi.Net;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace Test
{
	class RunTest
	{
        static void Main(string[] args)
		{

            // XMLTest test = new XMLTest();
            // test.Test();

            // RTTest test = new RTTest();
            // test.Run();

            /*
			try
			{
				CyberTest cyber = new CyberTest();
				Console.WriteLine("Secret: {0} Public: {1}", cyber.SecretKey, cyber.PublicKyes);
				cyber.MakeCheckRequest();
				Console.WriteLine("Запрос к серверу:\r\n{0}", cyber.TestString1);
			}
			catch (IPrivException ie)
			{
				Console.WriteLine("IPrivException {0} {1}\r\n{2}", ie.code, ie.ToString(), ie.StackTrace);
			}
			*/

            // Settings.ReadConfig();

            // EktTest ekt = new EktTest();



            // Console.WriteLine("ekt.Run()");
            // ekt.Run();

            // int Account = 744;

            // Console.WriteLine($"Account={Account} DayLimit={DayLimit.AsCF()} OnePayment={OnePayment.AsCF()}");

            // RedefineForAccount(Account);

            // Console.WriteLine($"Account={Account} DayLimit={DayLimit.AsCF()} OnePayment={OnePayment.AsCF()}");

            // Console.WriteLine("Test end");

            CyberAnswerTest();

        }


        static void CyberAnswerTest()
        {
            string stResponse = @"0000066701SM000004060000040600000125
                                0J0005              00904291
                                                    00000000
                                BEGIN
                                DATE=11.03.2016 16:32:35
                                SESSION=OLDIGW10193746
                                ERROR=0
                                RESULT=0
                                TRANSID=1006254576199
                                ADDINFO=ШТРАФ ПО АДМИНИСТРАТИВНОМУ ПРАВОНАРУШЕНИЮ ПОСТАНОВЛЕНИЕ №18810070150000234892; КБК: 18811630020016000140; Получатель: Отдел ГИБДД МО МВД России ""Стрежевской""; КПП: 701701001; ИНН: 7018016237; Р/СЧ: 40101810900000010007; Банк: ГРКЦ ГУ Банка России по Томской области г.Томск; БИК: 046902001
                                PRICE = 1000.00

                                END
                                BEGIN SIGNATURE
                                iQBRAwkBAA3MY1biyPMBAc4 + Af9Pxrayd6hEwTI1YjAT2qb5YQD5 / lTvJmvRKNsE
                                am8SiMJskCscXqvbn1qv77utU4YUvyX + uBSZFNY5nkr1aRC6sAHH
                                = +d62
                                END SIGNATURE
                               ";

            Console.WriteLine($"Target:\r\n{stResponse}\r\n");

            Console.WriteLine("Result:");
            // Получим блок текста между begin ... end
            string pattern = @"BEGIN(.*)END(.*)begin";

            Match m = Regex.Match(stResponse.Replace("\r", "").Replace("\n", ""), pattern, RegexOptions.IgnoreCase);
            while (m.Success)
            {
                Console.WriteLine($"'{m.Groups[1].Value}'");
                Console.WriteLine("-------------------------");
                Console.WriteLine($"'{m.Groups[2].Value}'");
                Console.WriteLine("=========================");
                m = m.NextMatch();
            }

        }

        static string Site = @"
            <Site>
	            <!-- DayLimit:		дневной лимит
	                 OnePayment:	максимальный единовременный платёж
	            В корне задаётся значение по умолчанию.
	            В каждом элементе устанавливается значение для текущего счёта (account)
	            -->
	            <Blocks DayLimit=""10000.00"" OnePayment=""5000.00"">
		            <!-- Модэл -->
		            <Block Account = ""941171"" DayLimit=""15000.00"" />
		            <Block Account = ""938744"" DayLimit=""50000.00"" />  <!-- Аскольдович -->
		            <!-- Островское, ТСЖ -->
		            <Block Account = ""942175"" DayLimit=""15000.00"" />
	            </Blocks>
            </Site>        
            ";

        static decimal DayLimit = 0M;
        static decimal OnePayment = 0M;
        
        /// <summary>
        /// Чтение переопределений для Account
        /// </summary>
        /// <returns></returns>
        static void RedefineForAccount(int account)
        {

            // XElement root = XElement.Load(Settings.Root + @"Lists\Site.xml");
            XElement root = XElement.Parse(Site);
            IEnumerable<XAttribute> RootAttributes = root.Elements().Attributes();

            // Установка общих значений
            foreach (XAttribute at in RootAttributes)
                switch (at.Name.LocalName)
                {
                    case "DayLimit":
                        DayLimit = at.Value.ToDecimal();
                        break;
                    case "OnePayment":
                        OnePayment = at.Value.ToDecimal();
                        break;
                }

            // Чтение индивидуальных значений для Account
            IEnumerable<XElement> Blocks =
                from el in root.Elements("Blocks").Elements("Block")
                // where el.Attributes().ToString() == account.ToString()
                select el;

            decimal _dayLimit = 0M;
            decimal _onePayment = 0M;

            foreach (XElement el in Blocks)
            {
                string acnt = "";
                // Переопределение общих значений
                foreach (XAttribute at in el.Attributes())
                    switch (at.Name.LocalName)
                    {
                        case "DayLimit":
                            _dayLimit = at.Value.ToDecimal();
                            break;
                        case "OnePayment":
                            _onePayment = at.Value.ToDecimal();
                            break;
                        case "Account":
                            acnt = at.Value;
                            break;
                    }

                if (acnt == account.ToString())
                {
                    if (_dayLimit > 0M)
                        DayLimit = _dayLimit;
                    if (_onePayment > 0M)
                        OnePayment = _onePayment;
                    Console.WriteLine($"\t\tName={el.Name.LocalName} account={acnt}");
                }
            }


        }

    }



    public class EktTest : GWEktRequest
    {

        // string pointid = "";
        // new Result result = null;

        public EktTest()
        {
            InitializeComponents();
            // terminal = 1224;
            tid = (int)(DateTime.Now.Ticks & 0xFFFF);
        }

        /// <summary>
        /// Test new MakeRequest
        /// </summary>
        public void Run()
        {
            state = 0;
            phone = "27026503011810";
            account = "938744";
            attributes = new AttributesCollection();
            attributes.Add("id2", "9521822210");
            int r = MakeRequest(0);
            if (r == 0)
                Console.WriteLine(stRequest);
            else
                Console.WriteLine($"Err = {r}");
        }

    }

    class CyberTest : GWCyberRequest
	{
		public CyberTest()
			: base()
		{
		}

		public override void InitializeComponents()
		{
			SD = "1839361";
			AP = "1354719";
			OP = "1354720";
			number = "701713072502959";
			fio = "ЛАРИОНОВ В.Н.";
			contact = "9039554393";
			amount = 400M;
			amountAll = 450M;

			// Пути к файлам ключей
			secret_key_path = "c:\\oldigw\\cyber\\secret.key";
			passwd = "ЕщрщкщфяР1!";
			public_key_path = "c:\\oldigw\\cyber\\pubkeys.key";
			serial = "904291";

			Settings.logLevel = "INFO";

		}

		public string Request { get { return stRequest; } }
	}

}
