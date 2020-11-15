namespace Odyssey.API.Tasks
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// API Client
    /// </summary>
    internal class DataWalletClient
    {        
        public string Token { get; private set; }

        private static HttpClient Client = new HttpClient();

        public string pub_key { get; set; }
        public string private_key { get; set; }
        public string receiverPublicDefaultKey { get; set; }
        public string receiverdefaultFolderId { get; set; }

        public string jsonlogger_url { get; set; } = "https://dev-api-dw.datachain.cloud/apps/json-logger/send";
        public string login_url { get; set; } = "https://dev-api-dw.datachain.cloud/auth/login";
        public string logout_url { get; set; } = "https://dev-api-dw.datachain.cloud/auth/logout";

        /// <summary>
        /// POST https://dev-api-dw.datachain.cloud/auth/login
        /// Get authorization token
        /// Warning remember to renew it.
        /// </summary>
        /// <returns>true if token has been retrieved, false otherwise</returns>
        public async Task<bool> AutenticateAsync()
        {
            Client.DefaultRequestHeaders.Clear();
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));

            var body = new Dictionary<string, string>();
            body.Add("userPublicKey", pub_key);
            body.Add("userPrivateKey", private_key);
            var req = new HttpRequestMessage(HttpMethod.Post, $"{login_url}")
            {
                Content = new FormUrlEncodedContent(body)
            };

            try
            {
                var res = await Client.SendAsync(req);
                Console.WriteLine($"#### {res.StatusCode} - POST - {login_url}", ConsoleColor.Red);
                var response = await res.Content.ReadAsStringAsync();
                dynamic occ = JsonConvert.DeserializeObject(response);
                Token = occ.payload.token;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message, ConsoleColor.Red);
                return false;
            }

            return true;
        }

        internal async Task<bool> AddDocument(dynamic jsonObject)
        {
            Client.DefaultRequestHeaders.Clear();
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", Token);
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            try
            {
                string o = JsonConvert.SerializeObject(jsonObject);
                var content = new StringContent(o.Replace("[[","[").Replace("]]","]"), Encoding.UTF8, "application/json");
                var res = await Client.PostAsync($"{jsonlogger_url}", content);
                Console.WriteLine($"#### {res.StatusCode} - POST - {jsonlogger_url}", ConsoleColor.Red);
                var response = await res.Content.ReadAsStringAsync();
                dynamic occ = JsonConvert.DeserializeObject(response);
                return occ.success;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }      
    }
}
