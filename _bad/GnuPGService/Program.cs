using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Web;
using System.Text;

namespace GPG
	{
	
	class Program
		{
		static void Main(string[] args)
			{
			// WebServiceHost sh = null;
			WebServiceHost api = null;
			// ServiceHost host = null;

			try
				{
				/*
				if (sh != null)
					sh.Close();
				*/

				// HTTPS
				// sh = new WebServiceHost(typeof(XRestApiService), new Uri("https://localhost/api") );
				// sh.AddServiceEndpoint(typeof(IXRestApi), new WebHttpBinding(WebHttpSecurityMode.Transport), ""); //.Behaviors.Add(new WebHttpBehavior());

				// sh.Credentials.ServiceCertificate.SetCertificate(StoreLocation.CurrentUser, StoreName.My, X509FindType.FindByThumbprint, Config.Thumbprint);
				// sh.Credentials.ClientCertificate.Authentication.CertificateValidationMode = System.ServiceModel.Security.X509CertificateValidationMode.Custom;
				// sh.Credentials.ClientCertificate.Authentication.CustomCertificateValidator = new XRestApi.CertificateValidator("OLDI-T_CA");
				// sh.Credentials.ClientCertificate.SetCertificate(StoreLocation.CurrentUser, StoreName.TrustedPeople, X509FindType.FindByThumbprint, "3c3916f50d78b6d24f680b407d589078c0d925c9");

				// ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
				ServicePointManager.SetTcpKeepAlive(true, 600000, 60000);

				// sh.Open();
				/*
				foreach (ChannelDispatcherBase ch in sh.ChannelDispatchers)
					{
					if (ch != null && ch.Host != null)
						{
						string HostBase = ch.Host.BaseAddresses[0].AbsoluteUri;
						Console.WriteLine("Listen host: {0}", HostBase);
						}
					}
				*/
				// HTTP
				api = new WebServiceHost(typeof(GPGService), new Uri(string.Format("http://localhost:{0}/GPGService/", Oldi.Utility.Config.AppSettings["GpgPort"])));
				api.AddServiceEndpoint(typeof(IGPGService), new WebHttpBinding(), ""); //.Behaviors.Add(new WebHttpBehavior());

				api.Open();

				foreach (ChannelDispatcherBase ch in api.ChannelDispatchers)
					{
					if (ch != null && ch.Host != null)
						{
						string HostBase = ch.Host.BaseAddresses[0].AbsoluteUri;
						Console.WriteLine("Listen host: {0}", HostBase);
						}
					}

				/*
				// TCP-host
				if (host != null)
					host.Close();
				host = new ServiceHost(typeof(XRestApiService), new Uri(string.Format("http://localhost:{0}/soap-sap/", Config.SOAPPort)));
				host.AddServiceEndpoint(typeof(IXRestApi), new BasicHttpBinding(), "");

				//MEX
				// ServiceMetadataBehavior metadataBehavior = new ServiceMetadataBehavior();
				// metadataBehavior.HttpGetEnabled = true;
				// host.Description.Behaviors.Add(metadataBehavior);
				host.AddServiceEndpoint(typeof(IMetadataExchange), MetadataExchangeBindings.CreateMexHttpBinding(), "mex");

				host.Open();
				*/

				Console.WriteLine("Press [Enter] to terminate");
				Console.ReadLine();
				}
			catch (Exception ex)
				{
				Console.WriteLine(ex.ToString());
				}
			finally
				{
				/*
				if (host != null)
					host.Close();
				if (sh != null)
					sh.Close();
				*/
				if (api != null)
					api.Close();
				}

			}
		}
	}
