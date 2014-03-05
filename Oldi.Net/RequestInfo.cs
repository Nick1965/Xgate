using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Concurrent;

namespace Oldi.Net
{
    public class RequestInfo
    {
        private X509Certificate2 clientCertificate;
        private HttpListenerContext context;

        /// <summary>
        /// Клиентский сертификат безопасности
        /// </summary>
        public X509Certificate2 ClientCertificate { get { return clientCertificate; } }
        /// <summary>
        /// Контекст слушателя
        /// </summary>
        public HttpListenerContext Context { get { return context; } }

        /// <summary>
        /// Код возвращенный провайдером
        /// </summary>
        public int ErrCode;
        
        /// <summary>
        /// Сообщение от провайдера
        /// </summary>
        public string ErrDesc;

        /// <summary>
        /// Строка запроса
        /// </summary>
        public string stRequest { get; set; }
        /// <summary>
        /// Ответ на запрос
        /// </summary>
        public string stResponse { get; set; }

		/// <summary>
		/// Кодировка клиента
		/// </summary>
		public Encoding ClientEncoding { get; set; }


        public string LogFile;
        
        /// <summary>
        /// Конструктор класса запроса
        /// </summary>
        /// <param name="context">Контекст запроса</param>
        public RequestInfo(HttpListenerContext context)
        {
            this.context = context;
        }
        /// <summary>
        /// Запрос клиентского сертификата
        /// </summary>
        public void GetClientCertificate()
        {
            clientCertificate = context.Request.GetClientCertificate();
        }

    }
}
