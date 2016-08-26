﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Net.Rest;
using System.Threading;
using System.IO;

namespace Discord.Tests.Framework
{
    class TestRestClient : IRestClient
    {
        public static Dictionary<string, string> Headers = new Dictionary<string, string>();

        public TestRestClient(string baseUrl)
        {
        }

        Task<Stream> IRestClient.SendAsync(string method, string endpoint, bool headerOnly = false)
        {
            if (headerOnly) return null;
            return Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(EndpointHandler.Instance.HandleMessage(method, endpoint, ""))));
        }

        Task<Stream> IRestClient.SendAsync(string method, string endpoint, IReadOnlyDictionary<string, object> multipartParams, bool headerOnly = false)
        {
            if (headerOnly) return null;
            throw new NotImplementedException("method only used for SendFile, not concerned with that yet.");
        }

        Task<Stream> IRestClient.SendAsync(string method, string endpoint, string json, bool headerOnly = false)
        {
            if (headerOnly) return null;
            return Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(EndpointHandler.Instance.HandleMessage(method, endpoint, json))));
        }

        void IRestClient.SetCancelToken(CancellationToken cancelToken)
        {
        }

        void IRestClient.SetHeader(string key, string value)
        {
            if (Headers.ContainsKey(key))
            {
                Headers.Remove(key);
            }
            Headers.Add(key, value);
            Console.WriteLine($"[Header Set]: {key} -> {value}");
        }
    }
}
