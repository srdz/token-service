using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using TokenWindowsService.App_Start;
using TokenWindowsService.Models;

namespace TokenWindowsService
{
    public partial class Service : ServiceBase
    {
        Timer timer = new Timer();
        string accessToken;
        public MongoDBContext dbcontext = new MongoDBContext();
        public IMongoCollection<TokenModel> tokenCollection;
        public IMongoCollection<LoginOAuthModel> loginOAuthCollection;
        EventLog eventLog = new EventLog();
        bool ABORT;

        public Service()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                this.timer = new System.Timers.Timer(/*"(Take time from database or use static time)"*/);

                // Hook up the Elapsed event for the timer.
                this.timer.Elapsed += new ElapsedEventHandler(OnElapsedTime);

                this.timer.Enabled = true;

                WriteToFile("Service | OnStart() is started at " + DateTime.Now);

                // Generate Token
                InvokeMethod();

                //timer.Elapsed += new ElapsedEventHandler(OnElapsedTime);
                timer.Elapsed += new ElapsedEventHandler(OnElapsedTime);
                timer.Interval = 9000; // 9 secondes = 9000
                timer.Enabled = true;

                timer.Start();
            }
            catch (Exception ex)
            {
                WriteToFile("Service | OnStart() error: " + ex.Message);
            }
        }

        protected override void OnStop()
        {
            WriteToFile("Service | OnStop() TokenAPI is stopped at " + DateTime.Now);

            // if there was a problem, set an exit error code
            // so the service manager will restart this
            if (ABORT) Environment.Exit(1);

            timer.Stop();
        }

        private void OnElapsedTime(object source, ElapsedEventArgs e)
        {
            WriteToFile("Service | OnElapsedTime() is recall at " + DateTime.Now);

            // Generate Refresh Token
            //GenerateRefreshToken();
            RestartWindowsService("TokenAPI");
        }

        /// <summary>
        /// Verify if a service exists
        /// </summary>
        /// <param name="ServiceName">Service name</param>
        /// <returns></returns>
        public bool serviceExists(string ServiceName)
        {
            return ServiceController.GetServices().Any(serviceController => serviceController.ServiceName.Equals(ServiceName));
        }

        /// <summary>
        /// Start a service by it's name
        /// </summary>
        /// <param name="ServiceName"></param>
        public void startService(string ServiceName)
        {
            ServiceController sc = new ServiceController();
            sc.ServiceName = ServiceName;

            WriteToFile("Service | startService() | The " + ServiceName + " service status is currently set to " + sc.Status.ToString());

            if (sc.Status == ServiceControllerStatus.Stopped)
            {
                // Start the service if the current status is stopped.
                WriteToFile("Service | startService() | Starting the " + ServiceName + " service ...");

                try
                {
                    // Start the service, and wait until its status is "Running".
                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running);

                    // Display the current service status.
                    WriteToFile("Service | startService() | The " + ServiceName + " service status is now set to " + sc.Status.ToString());
                }
                catch (InvalidOperationException e)
                {
                    WriteToFile("Service | startService() | Could not start the " + ServiceName + " service.");
                    WriteToFile("Service | startService() | error " + e.Message);
                }
            }
            else
            {
                WriteToFile("Service | startService() | else " + ServiceName + " already running.");
            }
        }

        /// <summary>
        /// Stop a service that is active
        /// </summary>
        /// <param name="ServiceName"></param>
        public void stopService(string ServiceName)
        {
            ServiceController sc = new ServiceController();
            sc.ServiceName = ServiceName;

            WriteToFile("Service | stopService() | The " + ServiceName + " service status is currently set to " + sc.Status.ToString());

            if (sc.Status == ServiceControllerStatus.Running)
            {
                // Start the service if the current status is stopped.
                WriteToFile("Service | stopService() | Stopping the " + ServiceName + " service ...");

                try
                {
                    // Start the service, and wait until its status is "Running".
                    sc.Stop();
                    sc.WaitForStatus(ServiceControllerStatus.Stopped);

                    // Display the current service status.
                    WriteToFile("Service | stopService() | The " + ServiceName + " service status is now set to {1} " + sc.Status.ToString());
                }
                catch (InvalidOperationException e)
                {
                    WriteToFile("Service | stopService() | Could not stop the " + ServiceName + " service.");
                    WriteToFile("Service | stopService() | error " + e.Message);
                }
            }
            else
            {
                WriteToFile("Cannot stop service " + ServiceName + " because it's already inactive.");
            }
        }

        /// <summary>
        ///  Verify if a service is running.
        /// </summary>
        /// <param name="ServiceName"></param>
        public bool serviceIsRunning(string ServiceName)
        {
            ServiceController sc = new ServiceController();
            sc.ServiceName = ServiceName;

            if (sc.Status == ServiceControllerStatus.Running)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Reboots a service
        /// </summary>
        /// <param name="ServiceName"></param>
        public void rebootService(string ServiceName)
        {
            if (serviceExists(ServiceName))
            {
                if (serviceIsRunning(ServiceName))
                {
                    stopService(ServiceName);
                }
                else
                {
                    startService(ServiceName);
                }
            }
            else
            {
                WriteToFile("The given service " + ServiceName + " doesn't exists");
            }
        }

        /// <summary>
        /// Restart Windows Service
        /// </summary>
        public static void RestartWindowsService(string serviceName)
        {
            using (var controller = new ServiceController(serviceName))
            {
                controller.Stop();
                int counter = 0;

                while (controller.Status != ServiceControllerStatus.Stopped)
                {
                    System.Threading.Thread.Sleep(100);
                    controller.Refresh();
                    counter++;

                    if (counter > 1000)
                    {
                        //throw new System.TimeoutException("Could not stop service: {0}", serviceName);
                    }
                }

                controller.Start();
            }
        }

        public void WriteToFile(string Message)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\ServiceLog_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";

            if (!File.Exists(filepath))
            {
                // Create a file to write to.   
                using (StreamWriter sw = File.CreateText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
        }

        public async void InvokeMethod()
        {
            try
            {
                Task<string> getAccessToken = GenerateAccessToken();
                accessToken = await getAccessToken;
                TokenModel token = new TokenModel();

                var obj = JObject.Parse(accessToken);
                // Log Message
                eventLog.Source = "TokenAPI";
                eventLog.WriteEntry(String.Format("Service | InvokeMethod() | Variable [accessToken = {0}]", accessToken), EventLogEntryType.Information);

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
                eventLog.WriteEntry(String.Format("Service | InvokeMethod() error : ", ex.Message), EventLogEntryType.Information);

                WriteToFile("Service | InvokeMethod() error " + ex.Message);
            }
        }

        public async Task<string> GenerateAccessToken()
        {
            try
            {
                eventLog.Source = "TokenAPI";
                eventLog.WriteEntry("Service | GenerateAccessToken()", EventLogEntryType.Information);

                string username = GetUsername();
                string pwd = GetPassword();

                HttpClient client = HeadersForAccessTokenGenerate();
                string body = "grant_type=password&username=" + username + "&password=" + pwd;

                //local
                //client.BaseAddress = new Uri("http://localhost:.../");

                //remote
                client.BaseAddress = new Uri("http://.../");

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, client.BaseAddress);
                request.Content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded");

                List<KeyValuePair<string, string>> postData = new List<KeyValuePair<string, string>>();

                postData.Add(new KeyValuePair<string, string>("grant_type", "password"));
                postData.Add(new KeyValuePair<string, string>("username", username));
                postData.Add(new KeyValuePair<string, string>("password", pwd));

                request.Content = new FormUrlEncodedContent(postData);
                HttpResponseMessage tokenResponse = client.PostAsync("oauth/token", new FormUrlEncodedContent(postData)).Result;

                var token = tokenResponse.Content.ReadAsStringAsync().Result;

                return token != null ? token : null;
            }
            catch (HttpRequestException ex)
            {
                eventLog.Source = "TokenAPI";
                eventLog.WriteEntry("Service | GenerateAccessToken() error : " + ex.Message, EventLogEntryType.Information);

                WriteToFile("Service | GenerateAccessToken() error : " + ex.Message);

                throw ex;
            }
        }

        public void GenerateRefreshToken()
        {
            try
            {
                tokenCollection = dbcontext.database.GetCollection<TokenModel>("Token");

                string refresh_token = tokenCollection.AsQueryable().FirstOrDefault().refresh_token.ToString();

                WriteToFile("Service | GenerateRefreshToken() refresh_token: " + refresh_token);

                eventLog.Source = "TokenAPI";
                eventLog.WriteEntry(String.Format("Service | GenerateRefreshToken() | [refresh_token = {0}]", refresh_token), EventLogEntryType.Information);

                HttpClient client = HeadersForAccessTokenGenerate();
                string body = "refresh_token=" + refresh_token + "&grant_type=refresh_token";

                WriteToFile("Service | GenerateRefreshToken() body: " + body);

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

                //test
                token = "{'error':'invalid_grant'}";

                WriteToFile("Service | GenerateRefreshToken() token: " + token);

                if (token.Contains("invalid_grant"))
                {
                    WriteToFile("Service | GenerateRefreshToken() invalid: " + token);

                    // reboot service
                    rebootService("TokenAPI");
                }

                var _dictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(token);

                ArrayList ls = new ArrayList(8);

                foreach (var key in _dictionary.Keys)
                {
                    ls.Add(_dictionary[key]);
                }

                WriteToFile("Service | GenerateRefreshToken() ls: " + ls);

                if (ls.Count > 0 && ls != null)
                {
                    tokenCollection = dbcontext.database.GetCollection<TokenModel>("Token");

                    var _id = tokenCollection.AsQueryable().FirstOrDefault()._id;

                    var filter = Builders<TokenModel>.Filter.Eq(x => x._id, _id);
                    var update = Builders<TokenModel>.Update.Set(x => x.access_token, ls[0].ToString())
                                                            .Set(x => x.token_type, ls[1].ToString())
                                                            .Set(x => x.expires_in, Convert.ToInt32(ls[2]))
                                                            .Set(x => x.refresh_token, ls[3].ToString())
                                                            .Set(x => x.issued, ls[6].ToString())
                                                            .Set(x => x.expires, ls[7].ToString());
                    var result = tokenCollection.UpdateOneAsync(filter, update).Result;

                    WriteToFile("Service | GenerateRefreshToken() result: " + result);
                }
            }
            catch (Exception ex)
            {
                WriteToFile("Service | GenerateRefreshToken() error : " + ex.Message);
            }
        }

        private HttpClient HeadersForAccessTokenGenerate()
        {
            eventLog.Source = "TokenAPI";
            eventLog.WriteEntry("Service | HeadersForAccessTokenGenerate()", EventLogEntryType.Information);

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
                eventLog.WriteEntry("Service | HeadersForAccessTokenGenerate() error : " + ex.Message, EventLogEntryType.Information);

                WriteToFile("Service | HeadersForAccessTokenGenerate() error : " + ex.Message);

                throw ex;
            }

            return client;
        }

        public string GetUsername()
        {
            loginOAuthCollection = dbcontext.database.GetCollection<LoginOAuthModel>("LoginOAuth");

            return loginOAuthCollection.AsQueryable().FirstOrDefault().username;
        }

        public string GetPassword()
        {
            loginOAuthCollection = dbcontext.database.GetCollection<LoginOAuthModel>("LoginOAuth");

            return loginOAuthCollection.AsQueryable().FirstOrDefault().password;
        }
    }
}