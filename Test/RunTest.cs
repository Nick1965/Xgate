using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Oldi.Net.Cyber;
using Oldi.Utility;
using System.Collections.Specialized;
using Oldi.Ekt;

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

            Console.WriteLine("Begin");
            Settings.ReadConfig();

            EktTest ekt = new EktTest();

            Console.WriteLine("ekt.Run()");
            ekt.Run();
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
            gateway = "458";
            service = "W1";
            amount = 100m;
            amountAll = 200m;
            tid = (int)(DateTime.Now.Ticks & 0xFFFF);
        }

        /// <summary>
        /// Test new MakeRequest
        /// </summary>
        public void Run()
        {
            state = 0;
            phone = "27026503011810";
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
