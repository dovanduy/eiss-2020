﻿// Copyright (C) Information Services. All Rights Reserved.
// Licensed under the Apache License, Version 2.0

using IOWebApplication.Infrastructure.Utils;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace IOWebApplication.Infrastructure.Http
{
    public class HttpRequester : IHttpRequester
    {
        public string ApiKey { get; set; }
        public string CertificatePath { get; set; }
        public string CertificatePassword { get; set; }
        public bool ValidateServerCertificate { get; set; }
        public HttpRequester()
        {
            ValidateServerCertificate = true;
        }




        public async Task<HttpResponseMessage> DeleteAsync(string url)
        {
            return await Request(url, HttpMethod.Delete);
        }

        public async Task<T> GetAsync<T>(string url)
        {
            var response = await Request(url, HttpMethod.Get);
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<T>(content);
            };

            throw new InvalidOperationException("Unexpected error!",
                new ApplicationException(response.ReasonPhrase));
        }

        public async Task<T> GetXmlAsync<T>(string url) where T : class
        {
            var response = await Request(url, HttpMethod.Get);
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return XmlUtils.DeserializeXml<T>(content);
            };

            throw new InvalidOperationException("Unexpected error!",
                new ApplicationException(response.ReasonPhrase));
        }

        public async Task<HttpResponseMessage> GetAsync(string url)
        {
            return await Request(url, HttpMethod.Get);
        }



        public async Task<HttpResponseMessage> PostAsync(string url, object data)
        {
            return await Request(url, HttpMethod.Post, data);
        }

        public async Task<HttpResponseMessage> PutAsync(string url, object data = null)
        {
            return await Request(url, HttpMethod.Put, data);
        }

        private async Task<HttpResponseMessage> Request(string url, HttpMethod method, object data = null)
        {
            var request = new HttpRequestMessage(method, url);


            HttpClientHandler ch = new HttpClientHandler();
            
            if (ValidateServerCertificate == false)
            {
                ch.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; };
            }


            if (!string.IsNullOrEmpty(this.CertificatePath))
            {
                var _cert = new X509Certificate2(this.CertificatePath, this.CertificatePassword);
                ch.ClientCertificates.Add(_cert);
            }

            using (var client = new HttpClient(ch, true))
            {
                if (data != null)
                {
                    var jsonData = JsonConvert.SerializeObject(data);
                    request.Content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                }

                if (this.ApiKey != null)
                {
                    client.DefaultRequestHeaders.Add("X-apiKey", this.ApiKey);
                }

                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                return await client.SendAsync(request);
            }
        }
    }
}
