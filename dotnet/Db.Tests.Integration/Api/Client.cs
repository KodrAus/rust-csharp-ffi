using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Db.Tests.Integration.Api
{
    class Client : IDisposable
    {
        private readonly Uri _baseUri;
        private readonly HttpClient _client;

        public Client(string baseUri)
        {
            _client = new HttpClient(new HttpClientHandler());
            _baseUri = new Uri(baseUri);
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        public async Task<Data[]> GetAll()
        {
            var response = await _client.GetAsync(new Uri(_baseUri, "api/data"));
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<Data[]>(content);
        }

        public async Task Set(Data data)
        {
            var content = new ByteArrayContent(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data.Value)));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await _client.PostAsync(new Uri(_baseUri, $"api/data/{data.Key}"), content);
            response.EnsureSuccessStatusCode();
        }

        public async Task Remove(string key)
        {
            var response = await _client.DeleteAsync(new Uri(_baseUri, $"api/data/{key}"));
            response.EnsureSuccessStatusCode();
        }
    }
}