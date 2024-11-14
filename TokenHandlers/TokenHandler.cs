using Autodesk.Authentication;
using Autodesk.Authentication.Model;
using Autodesk.Revit.UI;
using Newtonsoft.Json;
using RevitParametersAddin.Models;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;

namespace RevitParametersAddin.TokenHandlers
{
    public class Forge
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
    }

    public class ForgeConfiguration
    {
        public Forge Forge { get; set; }
    }



    public class TokenHandler
    {
        public static TokenModel Login()
        {
            try
            {
                string token = string.Empty;
                string refreshToken = string.Empty;
                var tokenModel = new TokenModel();
                ForgeConfiguration forgeConfiguration = null;

                // Retrieve the embedded resource stream
                // Note that Build Action for appsettings.json should be Embedded Resource
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("RevitParametersAddin.appsettings.json");
                if (stream == null)
                {
                    TaskDialog.Show("Login Error", "Could not find settings resource");
                    throw new Exception("Could not find settings resource");
                }
                // Read the JSON content
                using (StreamReader reader = new StreamReader(stream))
                {
                    string json = reader.ReadToEnd();

                    // Deserialize the JSON into a strongly typed object
                    JsonSerializerOptions options = new JsonSerializerOptions();
                    options.PropertyNameCaseInsensitive = true; // Allow case-insensitive property names
                    forgeConfiguration = System.Text.Json.JsonSerializer.Deserialize<ForgeConfiguration>(json, options);
                    //close StreamReader
                    reader.Close();
                }
                //Chck if the credentials were set
                if (string.IsNullOrEmpty(forgeConfiguration.Forge.ClientId) || string.IsNullOrEmpty(forgeConfiguration.Forge.ClientSecret))
                {
                    TaskDialog.Show("Login Error", "ClientId or ClientSecret is not set");
                    throw new Exception("ClientId or ClientSecret is not set");
                }
                else
                {
                    var oAuthHandler = OAuthHandler.Create(forgeConfiguration.Forge);
                    //We want to sleep the thread until we get 3L access_token.
                    //https://stackoverflow.com/questions/6306168/how-to-sleep-a-thread-until-callback-for-asynchronous-function-is-received
                    AutoResetEvent stopWaitHandle = new AutoResetEvent(false);
                    oAuthHandler.Invoke3LeggedOAuth(async (bearer) =>
                    {
                        // This is our application delegate. It is called upon success or failure
                        // after the process completed
                        if (bearer == null)
                        {
                            TaskDialog.Show("Login Response", "Sorry, Authentication failed! 3legged test");
                            return;
                        }
                        //var rep = JsonConvert.DeserializeObject<TokenModel>(await bearer.Content.ReadAsStringAsync());
                        
                        tokenModel.access_token = bearer.AccessToken;
                        tokenModel.refresh_token = bearer.RefreshToken;
                        tokenModel.token_type = bearer.TokenType;
                        

                        //token = bearer.AccessToken;
                        //refreshToken = bearer.RefreshToken;
                        //The call returned successfully and you got a valid access_token.                
                        DateTime dt = DateTime.Now;
                        dt.AddSeconds(double.Parse(bearer.ExpiresIn.ToString()));
                        tokenModel.expires_in = dt;

                        // Ensure authenticationClient is initialized
                        var authenticationClient = new AuthenticationClient();
                        UserInfo profileApi = await authenticationClient.GetUserInfoAsync(bearer.AccessToken);
                        TaskDialog.Show("Login Response", $"Hello {profileApi.Name} !!, You are Logged in!");
                        stopWaitHandle.Set();
                    });
                    stopWaitHandle.WaitOne();
                }
                return tokenModel;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.ToString());
                TaskDialog.Show("Login Error", "Access Denied");
                throw new Exception("Login Error");
            }
        }

        /// <summary>
        /// Refresh token
        /// </summary>
        /// <returns></returns>
        /// 
        public static async Task<string> Refresh3LeggedToken(String refreshToken)
        {

            // This will get the current WORKING directory (i.e. \bin\Debug)
            string currentUserDirectory = Environment.CurrentDirectory;

            var forgeConfiguration = JsonConvert.DeserializeObject<ForgeConfiguration>(
                File.ReadAllText(Path.Combine(Directory.GetParent(currentUserDirectory).Parent.FullName, @"appsettings.json")));

            // Ensure authenticationClient is initialized
            var authenticationClient = new AuthenticationClient();
            var response = await authenticationClient.RefreshTokenAsync(refreshToken, forgeConfiguration.Forge.ClientId);
            return response.AccessToken;
           
        }


        public async Task<string> Get2LeggedForgeToken()
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri("https://developer.api.autodesk.com");
            var request = new HttpRequestMessage(HttpMethod.Post, "/authentication/v2/token");

            // This will get the current WORKING directory (i.e. \bin\Debug)
            string currentUserDirectory = Environment.CurrentDirectory;

            var forgeConfiguration = JsonConvert.DeserializeObject<ForgeConfiguration>(
                File.ReadAllText(Path.Combine(Directory.GetParent(currentUserDirectory).Parent.FullName, @"appsettings.json")));

            var credentials = new Dictionary<string, string>();
            credentials.Add("client_id", forgeConfiguration.Forge.ClientId);//Your client ID from https://aps.autodesk.com/myapps/ here
            credentials.Add("client_secret", forgeConfiguration.Forge.ClientSecret);//Your client ID from https://aps.autodesk.com/myapps/ here
            credentials.Add("grant_type", "client_credentials");
            credentials.Add("scope", "code:all data:create data:write data:read bucket:create bucket:delete");

            var content = new FormUrlEncodedContent(credentials);
            request.Content = content;
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var rep = JsonConvert.DeserializeObject<TokenModel>(await response.Content.ReadAsStringAsync());
            return rep.access_token;
        }
    }
}