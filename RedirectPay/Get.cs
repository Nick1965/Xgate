using Oldi.Net;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.XPath;

namespace RedirectPay
	{
	public partial class Scan
		{

		/// <summary>
		/// Выполнение запроса GET
		/// </summary>
		/// <param name="Host"></param>
		/// <param name="Entry"></param>
		/// <param name="x"></param>
		/// <returns></returns>
		string Get(string Host, string Endpoint, string QueryString)
			{
			string Url = Host + Endpoint + "?" + QueryString;

			// Console.WriteLine("Url = {0}", QueryString);
			// Log("Url = {0}", QueryString);

			System.Net.HttpWebRequest request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(new Uri(Url));
			request.Method = "GET";
			request.Accept = "text/xml */*";
			request.UserAgent = "XNET-test";
			request.ContentType = "application/x-www-form-urlencoded; charset=utf-8";
			// request.ContentType = "application/json; charset=utf-8";
			// request.ContentType = "application/x-www-form-urlencoded; charset=utf-8";
			// request.Headers.Add("Accept-Encoding", "identity");
			// request.Headers.Add("Accept-Encoding", "deflate, gzip, identity");
			// request.Headers.Add("Accept-Encoding", "identity");
			Guid session = Guid.NewGuid();
			request.Headers.Add("IDHTTPSESSIONID", session.ToString("D", CultureInfo.InvariantCulture));

			// Set some reasonable limits on resources used by this request
			request.MaximumAutomaticRedirections = 4;
			request.MaximumResponseHeadersLength = 4;
			// Set credentials to use for this request.
			// CredentialCache myCache = new CredentialCache();
			// myCache.Add(new Uri(TargetUri), "Basic", new NetworkCredential(UserName, Password));
			// request.Credentials = myCache;

			Log("GET: {0}", Url);
			// Console.WriteLine("GET: {0}", Url);
			foreach (string key in request.Headers.AllKeys)
				{
				Log("{0} = {1}", key, request.Headers[key]);
				// Console.WriteLine("{0} = {1}", key, request.Headers[key]);
				}
				

			System.Net.HttpWebResponse response = null;
			try
				{
				// Добавление сертификата
				// if (AddCertificate(request) != 0)
				//	return null;

				response = (System.Net.HttpWebResponse)request.GetResponse();
				}
			catch (WebException we)
				{
				Log("GET: Status = {0} {1}", we.Status, we.ToString());
				// Console.WriteLine("GET: Status = {0} {1}", we.Status, we.ToString());
				if (we.Response != null)
					{
					Stream r = we.Response.GetResponseStream();
					using (StreamReader reader = new StreamReader(r, Encoding.GetEncoding(1251)))
						return reader.ReadToEnd();
					}
				return null;
				}
			catch (Exception ex)
				{
				Log("GET: " + ex.ToString());
				// Console.WriteLine("GET: " + ex.ToString());
				return null;
				}

			// Console.WriteLine("Content length is {0}", response.ContentLength);
			// Console.WriteLine("Content type is {0}", response.ContentType);
			// Console.WriteLine("The encoding method used is: " + response.ContentEncoding);
			// Console.WriteLine("The character set used is :" + response.CharacterSet);
			// Get the stream associated with the response.
			Stream receiveStream = response.GetResponseStream();

			Log("\r\nПолучен ответ:");
			// Console.WriteLine("\r\nПолучен ответ:");
			foreach (string key in response.Headers.AllKeys)
				{
				Log("{0} = {1}", key, response.Headers[key]);
				// Console.WriteLine("{0} = {1}", key, response.Headers[key]);
				}
			CookieCollection cookies = response.Cookies;
			foreach (Cookie cookie in cookies)
				{
				// Console.WriteLine("Cookie={0}={1}", cookie.Name, cookie.Value);
				Log("Cookie={0}={1}", cookie.Name, cookie.Value);
				}

			// Pipes the stream to a higher level stream reader with the required encoding format. 
			Encoding enc;

			enc = Encoding.GetEncoding(1251);
			string buf = null;

			string[] ce = response.ContentEncoding.Split(new char[] { ',' });
			foreach (string c in ce)
				{
				if (c.ToLower() == "deflate")
					{
					using (DeflateStream dfls = new DeflateStream(receiveStream, CompressionMode.Decompress))
					using (StreamReader reader = new StreamReader(dfls, enc))
						buf = reader.ReadToEnd();
					Log("Read Deflate. Charset=\"{0}\", Content-Length={1}", enc.WebName, buf.Length);
					break;
					}
				else if (c.ToLower() == "gzip")
					{
					using (GZipStream gzips = new GZipStream(receiveStream, CompressionMode.Decompress))
					using (StreamReader reader = new StreamReader(gzips, enc))
						buf = reader.ReadToEnd();
					Log("Read GZip/{1}. Charset=\"{0}\"", enc.WebName, buf.Length);
					break;
					}
				else
					{
					using (StreamReader reader = new StreamReader(receiveStream, enc))
						buf = reader.ReadToEnd();
					Log("Read uncompress. Charset=\"{0}\"", enc.WebName);
					break;
					}
				}

			receiveStream.Close();

			// Console.WriteLine(buf);
			Log("--------------------------\r\n{0}", buf);

			return buf;
			}

		/// <summary>
		/// Извлекает параметр из ответа в виде XPath запроса
		/// </summary>
		/// <param name="answer"></param>
		/// <param name="expr"></param>
		/// <returns></returns>
		String GetValueFromAnswer(string answer, string expr)
			{

			if (string.IsNullOrEmpty(answer))
				return null;

			try
				{

				XPathDocument doc = new XPathDocument(new StringReader(answer.ToLower()));
				XPathNavigator nav = doc.CreateNavigator();
				// XPathNodeIterator items = nav.Select(expr);
				XPathNavigator node = nav.SelectSingleNode(expr.ToLower());

				// Log("****************************************************");
				// Log("XPath: {0} = {1}", expr, node.Select(expr).Current.Value);
				// Log("****************************************************");

				return node.Select(expr.ToLower()).Current.Value;

				}
			catch (Exception ex)
				{
				Log("Value: {0}", ex.Message);
				// Console.WriteLine("Value: {0}", ex.Message);
				}

			return null;
			}

		}
	}
