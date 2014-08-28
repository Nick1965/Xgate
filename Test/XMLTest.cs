using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Oldi.Utility;

namespace Test
	{

	public class ProviderItem
		{
		public string Name
			{
			get;
			set;
			}
		public string Service
			{
			get;
			set;
			}
		public string Gateway
			{
			get;
			set;
			}
		public ProviderItem(string Name, string Service, string Gateway)
			{
			this.Name = Name;
			this.Service = Service;
			this.Gateway = Gateway;
			}
		public override string ToString()
			{
			return string.Format("Name = \"{0}\" Service = \"{1}\" Gateway = \"{2}\"", Name, Service, Gateway);
			}
		}
	public class XMLTest
		{
		decimal amountLimit;
		int amountDelay;
		const string xml = "..\\..\\..\\utility\\oldigw.xml";
		List<ProviderItem> Providers = new List<ProviderItem>();
		public void Test()
			{

			try
				{
				XDocument doc = XDocument.Load(xml);
				string provider = "";

				if (doc.Element("Configuration").HasElements)
					{
					foreach (XElement el in doc.Root.Elements())
						{
						string name = el.Name.LocalName;
						string value = el.Value;
						// Console.WriteLine("Section: {0}", name);
						switch (el.Name.LocalName)
							{
							case "Provider":
								NameValueCollection nav = new NameValueCollection();
								foreach (XAttribute a in el.Attributes())
									{
									// Console.WriteLine("\t{0}={1}", a.Name.LocalName, a.Value);
									if (a.Name.LocalName == "name") provider = a.Value;
									nav.Set(a.Name.LocalName, a.Value);
									}
								if (string.IsNullOrEmpty(provider))
									{
									Console.WriteLine("Пропущено имя провайдера");
									throw new ApplicationException("Пропущено имя провайдера");
									}
								//providers.Add(provider, nav);
								break;
							case "AppSettings":
								foreach (XElement add in el.Elements())
									{
									string add_name = add.Name.LocalName;
									string add_key = (string)add.Attribute("key");
									string add_value = (string)add.Attribute("value");
									// Console.WriteLine("\t{0}={1}", add_key, add_value);
									// appSettings.Set(add_key, add_value);
									}
								break;
							case "FinancialCheck":
								Console.WriteLine("Tag = \"{0}\"", el.Name.LocalName);
								foreach (XElement s in el.Elements())
									{
									switch (s.Name.LocalName)
										{
										case "AmountLimit":
											amountLimit = XConvert.ToDecimal(s.Value.ToString());
											Console.WriteLine("AmountLimit = \"{0}\"", amountLimit);
											break;
										case "AmountDelay":
											amountDelay = int.Parse(s.Value.ToString());
											Console.WriteLine("AmountDelay = \"{0}\"", amountDelay);
											break;
										case "Providers":
											IEnumerable<XElement> elements =
												from e in s.Elements("Provider")
												select e;
											/*
												foreach (XElement e in elements)
												Console.WriteLine(e.Name.LocalName);
											*/
											foreach (XElement e in elements)
												{
												// Console.WriteLine(e.Name.LocalName);
												if (e.Name.LocalName == "Provider")
													{
													string Name = "";
													string Service = "";
													string Gateway = "";
													foreach (var item in e.Attributes())
														{
														if (item.Name.LocalName == "Name")
															Name = item.Value.ToString();
														if (item.Name.LocalName == "Service")
															Service = item.Value.ToString();
														if (item.Name.LocalName == "Gateway")
															Gateway = item.Value.ToString();
														}
													Providers.Add(new ProviderItem(Name, Service, Gateway));
													}
												}
											
											break;
										}
									}
								break;
							}
						}
					}
				else
					{
					Console.WriteLine("Нет секции Configuration");
					}
				}
			catch (Exception ex)
				{
				Console.WriteLine("Config: {0}\r\n{1}", ex.Message, ex.StackTrace);
				}

			// Распечатка списка контролируемых поставщиков
			Console.WriteLine("Распечатка списка контролируемых поставщиков");
			foreach (var item in Providers)
				Console.WriteLine(item.ToString());
			
			}

		}
	}
