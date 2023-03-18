using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using TokenWindowsService.App_Start;
using TokenWindowsService.Models;

namespace TokenWindowsService.Controller
{
    public class TokenController
    {
        string accessToken;
        private MongoDBContext dbcontext;
        private IMongoCollection<TokenModel> tokenCollection;
        EventLog eventLog = new EventLog();

        public async void InvokeMethod()
        {
            try
            {
                dbcontext = new MongoDBContext();

                Task<string> getAccessToken = GenerateAccessToken();
                accessToken = await getAccessToken;
                TokenModel token = new TokenModel();

                var obj = JObject.Parse(accessToken);

                // Log Message
                eventLog.Source = "TokenAPI";
                eventLog.WriteEntry(String.Format("Controller TokenController | Function InvokeMethod() | Variable [accessToken = {0}]", accessToken), EventLogEntryType.Information);

                if (accessToken != null)
                {
                    tokenCollection = dbcontext.database.GetCollection<TokenModel>("Token");

                    var _id = tokenCollection.AsQueryable().FirstOrDefault()._id;

                    var filter = Builders<TokenModel>.Filter.Eq(x => x._id, _id);
                    var update = Builders<TokenModel>.Update.Set(x => x.access_token, obj["access_token"].ToString())
                                                            .Set(x => x.token_type, obj["token_type"].ToString())
                                                            .Set(x => x.expires_in, Convert.ToInt32(obj["expires_in"]))
                                                            .Set(x => x.refresh_token, obj["refresh_token"].ToString())
                                                            .Set(x => x.issued, obj[".issued"].ToString())
                                                            .Set(x => x.expires, obj[".expires"].ToString());
                    var result = tokenCollection.UpdateOneAsync(filter, update).Result;
                }
            }
            catch (Exception ex)
            {
                eventLog.Source = "TokenAPI";
                eventLog.WriteEntry(String.Format("Controller TokenController | Function InvokeMethod() error | {0}", ex.Message), EventLogEntryType.Error);

                throw ex;
            }
        }

        public async Task<string> GenerateAccessToken()
        {
            try
            {
                eventLog.Source = "TokenAPI";
                eventLog.WriteEntry("Controller TokenController | Function GenerateAccessToken()", EventLogEntryType.Information);

                HttpClient client = HeadersForAccessTokenGenerate();
                string body = "grant_type=password&username=username&password=password";

                //local
                //client.BaseAddress = new Uri("http://localhost:.../");

                //remote
                client.BaseAddress = new Uri("http://.../");

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, client.BaseAddress);
                request.Content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded");

                List<KeyValuePair<string, string>> postData = new List<KeyValuePair<string, string>>();

                postData.Add(new KeyValuePair<string, string>("grant_type", "password"));
                postData.Add(new KeyValuePair<string, string>("username", "username"));
                postData.Add(new KeyValuePair<string, string>("password", "password"));

                request.Content = new FormUrlEncodedContent(postData);
                HttpResponseMessage tokenResponse = client.PostAsync("oauth/token", new FormUrlEncodedContent(postData)).Result;

                var token = tokenResponse.Content.ReadAsStringAsync().Result;

                return token != null ? token : null;
            }
            catch (HttpRequestException ex)
            {
                eventLog.Source = "TokenAPI";
                eventLog.WriteEntry("Controller TokenController | Function GenerateAccessToken() error | {0}", EventLogEntryType.Error);

                throw ex;
            }
        }

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
        public async Task<string> GenerateRefreshToken(string refresh_token)
#pragma warning restore CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
        {
            try
            {
                eventLog.Source = "TokenAPI";
                eventLog.WriteEntry(String.Format("Controller TokenController | Function GenerateRefreshToken() | [refresh_token = {0}]", refresh_token), EventLogEntryType.Information);

                HttpClient client = HeadersForAccessTokenGenerate();
                string body = "refresh_token=" + refresh_token + "&grant_type=refresh_token";

                //local
                //client.BaseAddress = new Uri("http://localhost:.../");

                //remote
                client.BaseAddress = new Uri("http://.../");

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, client.BaseAddress);
                request.Content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded");

                List<KeyValuePair<string, string>> postData = new List<KeyValuePair<string, string>>();

                postData.Add(new KeyValuePair<string, string>("refresh_token", refresh_token));
                postData.Add(new KeyValuePair<string, string>("grant_type", "refresh_token"));

                request.Content = new FormUrlEncodedContent(postData);
                HttpResponseMessage tokenResponse = client.PostAsync("oauth/token", new FormUrlEncodedContent(postData)).Result;

                var token = tokenResponse.Content.ReadAsStringAsync().Result;

                return token != null ? token : null;
            }
            catch (HttpRequestException ex)
            {
                eventLog.Source = "TokenAPI";
                eventLog.WriteEntry(String.Format("Controller TokenController | Function GenerateRefreshToken() error | {0}", ex.Message), EventLogEntryType.Error);

                throw ex;
            }
        }

        public void GetRefreshTokenCode()
        {
            try
            {
                eventLog.Source = "TokenAPI";
                eventLog.WriteEntry("Controller TokenController | Function GetRefreshTokenCode()", EventLogEntryType.Information);

                dbcontext = new MongoDBContext();
                tokenCollection = dbcontext.database.GetCollection<TokenModel>("Token");

                GenerateRefreshToken(tokenCollection.AsQueryable().FirstOrDefault().refresh_token);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private HttpClient HeadersForAccessTokenGenerate()
        {
            eventLog.Source = "TokenAPI";
            eventLog.WriteEntry("Controller TokenController | Function HeadersForAccessTokenGenerate()", EventLogEntryType.Information);

            HttpClient client;

            try
            {
                HttpClientHandler handler = new HttpClientHandler() { UseDefaultCredentials = false };
                client = new HttpClient(handler);

                //local
                //client.BaseAddress = new Uri("http://localhost:.../");

                //remote
                client.BaseAddress = new Uri("http://.../");

                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));
            }
            catch (Exception ex)
            {
                eventLog.Source = "TokenAPI";
                eventLog.WriteEntry("Controller TokenController | Function HeadersForAccessTokenGenerate() error | {0}", EventLogEntryType.Error);

                throw ex;
            }

            return client;
        }
    }
}