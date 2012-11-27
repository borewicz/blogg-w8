/* 
        Client ID: 	
        470788067421.apps.googleusercontent.com
        Client secret: 	
        zHB7W8MJlp-VDeQ96TdR8sKM
        Redirect URIs: 	urn:ietf:wg:oauth:2.0:oob http://localhost
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Security.Authentication.Web;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Blogg
{
    public class OAuth2
    {
        //https://www.googleapis.com/auth/blogger
        public const string HOST = "https://accounts.google.com/o/oauth2/auth";
        public const string TOKEN_HOST = "https://accounts.google.com/o/oauth2/token";
        public const string REDIRECT_URI = "urn:ietf:wg:oauth:2.0:oob";

        public string consumerKey = "470788067421.apps.googleusercontent.com";
        public string consumerSecret = "zHB7W8MJlp-VDeQ96TdR8sKM";
        //public string token { get; set; }
        //public string refresh_token = "1/y3tYGtnT2vX0Xye3KfL3RRa7tVqqqsM9gipiyCbfN2o";
        public string refresh_token { get; set; }
        public string access_token { get; set; }
        public string scope = "https://www.googleapis.com/auth/blogger+https://picasaweb.google.com/data/";
        //https://accounts.google.com/o/oauth2/auth?redirect_uri=https://developers.google.com/oauthplayground&response_type=code&client_id=407408718192.apps.googleusercontent.com&approval_prompt=force&scope=&access_type=offline
        //4/nqffjgT8Cln0xxQOIg7-AAAoks1J.otqfE69jo30QOl05ti8ZT3bLlLsVcQI

        public string GetAuthenticationLink()
        {
            string getData = HOST;
            getData += "?response_type=code";
            getData += "&client_id=" + this.consumerKey;
            getData += "&redirect_uri=" + REDIRECT_URI;
            getData += "&scope=" + scope;
            //byte[] byteArray = Encoding.UTF8.GetBytes(postData);
            //return string.Format("response_type=code&client_id={0}&redirect_uri={1}", this.consumerKey, REDIRECT_URI);
            return getData;
        }

        public async Task<string> ConnectToGoogle()
        {   
            System.Uri StartUri = new Uri(GetAuthenticationLink());
            System.Uri EndUri = new Uri("https://accounts.google.com/o/oauth2/approval?");

            WebAuthenticationResult WebAuthenticationResult = await WebAuthenticationBroker.AuthenticateAsync(
                                                    WebAuthenticationOptions.UseTitle,
                                                    StartUri,
                                                    EndUri);

            //System.Diagnostics.Debug.WriteLine(App.extractBlogID(WebAuthenticationResult.ResponseData.ToString(), 1, '='));
            HttpClient client = new HttpClient();

            string postData = "code=" + App.extractBlogID(WebAuthenticationResult.ResponseData.ToString(), 1, '=');
            postData += "&client_id=" + consumerKey;
            postData += "&client_secret=" + consumerSecret;
            postData += "&redirect_uri=" + REDIRECT_URI;
            postData += "&grant_type=authorization_code";

            HttpContent content = new StringContent(postData);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
            HttpResponseMessage response = await client.PostAsync("https://accounts.google.com/o/oauth2/token", content);
            return await response.Content.ReadAsStringAsync();
	    }

        public async Task<string> RefreshToken()
        {
            HttpClient client = new HttpClient();

            string postData = "&client_id=" + consumerKey;
            postData += "&client_secret=" + consumerSecret;
            postData += "&refresh_token=" + refresh_token;
            postData += "&grant_type=refresh_token";

            try
            {
                HttpContent content = new StringContent(postData);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
                HttpResponseMessage response = await client.PostAsync("https://accounts.google.com/o/oauth2/token", content);
                if (response.IsSuccessStatusCode == true)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    Windows.UI.Popups.MessageDialog dialog = new Windows.UI.Popups.MessageDialog(App.loader.GetString("checkYourConnection"), App.loader.GetString("errorReturned"));
                    await dialog.ShowAsync();
                    return null;
                }
            }
            catch
            {
                Windows.UI.Popups.MessageDialog dialog = new Windows.UI.Popups.MessageDialog(App.loader.GetString("checkYourConnection"), App.loader.GetString("notConnected"));
                dialog.ShowAsync();
                App.dataSource.logout();
                return null;
            }
        }
    }
}
