using EasyRun.Tye.DTOS;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace EasyRun.Tye
{
    public static class TyeHostApi
    {
        private static Lazy<HttpClient> httpClient = new Lazy<HttpClient>(() => BuildClient());

        private static HttpClient BuildClient()
        {
            var client = new HttpClient();

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.Timeout = TimeSpan.FromSeconds(5);

            return client;
        }

        public static async Task<List<TyeServiceDTO>> GetTyeServicesAsync(int tyeHostPort)
        {
            var client = httpClient.Value;

            try
            {
                HttpResponseMessage response = await client.GetAsync($"http://localhost:{tyeHostPort}/api/v1/services");
                if (response.IsSuccessStatusCode)
                {
                    var servicesJson = await response.Content.ReadAsStringAsync();

                    if (servicesJson != null)
                    {
                        return JsonConvert.DeserializeObject<List<TyeServiceDTO>>(servicesJson);
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }

            return null;
        }

        public static async Task<bool> ShutdownTyeHostAsync(int tyeHostPort)
        {
            var client = httpClient.Value;

            try
            {
                HttpResponseMessage response = await client.DeleteAsync($"http://localhost:{tyeHostPort}/api/v1/control");
                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool IsTyeHostRunning(int tyeHostPort)
        {
            IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();

            foreach (IPEndPoint endPoint in ipProperties.GetActiveTcpListeners())
            {
                if (endPoint.Port == tyeHostPort)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
