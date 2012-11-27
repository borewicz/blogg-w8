using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using HtmlAgilityPack;
using Windows.UI.ApplicationSettings;
using Windows.ApplicationModel.Resources;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.Storage;
using System.ComponentModel;
using Windows.UI.Xaml.Documents;
using Windows.System;

namespace Blogg.Data
{
    [Windows.Foundation.Metadata.WebHostHidden]

    public class BlogCommon : Blogg.Common.BindableBase
    {
        public string id { get; set; }
        public string name { get; set; }
        public string url { get; set; }
    }

    public class BlogItem : BlogCommon
    {
        public string summary { get; set; }
        public string posts { get; set; }
        public DateTime published { get; set; }
        public DateTime updated { get; set; }

        private List<PostItem> _Items = new List<PostItem>();
        public List<PostItem> Items
        {
            get
            {
                return this._Items;
            }
        }

        public IEnumerable<PostItem> TopItems
        {
            get { return this._Items.Take(8); }
        }
    }

    public class PostItem : BlogCommon
    {
        public string blogID { get; set; }
        public DateTime time { get; set; }
        public string author { get; set; }
        //public string content { get; set; }
          public BitmapImage authorAvatar { get; set; }

        private BlogItem _group;
        public BlogItem Group
        {
            get { return this._group; }
            set { this.SetProperty(ref this._group, value); }
        }
    }

    public sealed class BlogsDataSource : INotifyPropertyChanged
    {
        private static BlogsDataSource _blogsDataSource = new BlogsDataSource();
        public event PropertyChangedEventHandler PropertyChanged;
        private ObservableCollection<BlogItem> _Blogs = new ObservableCollection<BlogItem>();
        public ObservableCollection<BlogItem> blogs { get { return this._Blogs; } }
        private bool _isFinished, _isOpen;
        private Visibility _visible;

        public string authenticationToken { get; set; } //ClientLogin
        public OAuth2 oauth2;


        public BlogsDataSource()
        {
            oauth2 = new OAuth2();
            isFinished = false;
            //getBlogsList();
        }
            
        public static IEnumerable<BlogItem> GetGroups(string id)
        {
            if (!id.Equals("blogs")) throw new ArgumentException("Only 'blogs' is supported as a collection of groups");
            
            return _blogsDataSource.blogs;
        }

        public static BlogItem GetGroup(string id)
        {
            // Simple linear search is acceptable for small data sets
            var matches = _blogsDataSource.blogs.Where((group) => group.id.Equals(id));
            if (matches.Count() == 1) return matches.First();
            else
            {
                System.Diagnostics.Debug.WriteLine("No matches");
                return null;
            }
        }

        public bool isFinished
        {
            get { return this._isFinished; }

            set
            {
                if (value != this._isFinished)
                {
                    this._isFinished = value;
                    NotifyPropertyChanged("isFinished");
                }
            }
        }

        public bool isOpen
        {
            get { return this._isOpen; }

            set
            {
                if (value != this._isOpen)
                {
                    this._isOpen = value;
                    NotifyPropertyChanged("isOpen");
                }
            }
        }

        public Visibility visible
        {
            get { return this._visible; }

            set
            {
                if (this._visible != Visibility.Collapsed)
                {
                    this._visible = Visibility.Collapsed;
                    NotifyPropertyChanged("visible");
                }
                else
                {
                    this._visible = Visibility.Visible;
                    NotifyPropertyChanged("visible");
                }
            }
        }

        public void logout()
        {
            ApplicationData.Current.LocalSettings.Values["refresh_token"] = null;
            App.dataSource.oauth2.access_token = null;
            App.dataSource.blogs.Clear();
            ExtendedSplash splash = new ExtendedSplash(App.splashScreen);
            Window.Current.Content = splash;
        }

        public async void getBlogsList()
        {
            isFinished = true;
            isOpen = false;
            if (visible != Visibility.Collapsed) visible = Visibility.Collapsed;
            oauth2.refresh_token = (string)ApplicationData.Current.LocalSettings.Values["refresh_token"];
            //this.blogs.Clear();
            if (oauth2.access_token == null)
            {
                if (oauth2.refresh_token == null)
                {
                    string tokenResponse = await oauth2.ConnectToGoogle();
                    //System.Diagnostics.Debug.WriteLine(tokenResponse);
                    //while (tokenResponse == null)
                    //{
                    //    MessageDialog md = new MessageDialog(App.loader.GetString("cancelEnter"), App.loader.GetString("erroe"));
                    //    md.Commands.Add(
                    //        new UICommand("Try again", null, 0));
                    //    await md.ShowAsync();
                    //    tokenResponse = await oauth2.ConnectToGoogle();
                    //}
                    if (tokenResponse == null)
                    {
                        ExtendedSplash splash = new ExtendedSplash(App.splashScreen);
                        Window.Current.Content = splash;
                        return;
                    }
                    JObject o = JObject.Parse(tokenResponse);
                    if (o["error"] == null)
                    {
                        oauth2.access_token = o["access_token"].ToString();
                        oauth2.refresh_token = o["refresh_token"].ToString();
                        ApplicationData.Current.LocalSettings.Values["refresh_token"] = oauth2.refresh_token;
                        //System.Diagnostics.Debug.WriteLine(access_token + ", " + refresh_token);
                        //System.Diagnostics.Debug.WriteLine(o["access_token"]);
                        //System.Diagnostics.Debug.WriteLine(o["refresh_token"]);
                    }
                    else System.Diagnostics.Debug.WriteLine("Error : " + o["error"]);
                }
                else
                {
                    string tokenResponse = await oauth2.RefreshToken();
                    if (tokenResponse == null) return;
                    JObject o = JObject.Parse(tokenResponse);
                    if (o["error"] == null)
                    {
                        oauth2.access_token = o["access_token"].ToString();
                    }
                    else System.Diagnostics.Debug.WriteLine("Error : " + o["error"]);
                }
            }
            try
            {
                HttpClient client = new HttpClient();
                //System.Diagnostics.Debug.WriteLine("Bearer " + oauth2.access_token);
                client.DefaultRequestHeaders.Authorization = System.Net.Http.Headers.AuthenticationHeaderValue.Parse("Bearer " + oauth2.access_token);
                HttpResponseMessage response = await client.GetAsync("https://www.googleapis.com/blogger/v3/users/self/blogs");
                if (response.IsSuccessStatusCode == true)
                {
                    //System.Diagnostics.Debug.WriteLine(content);
                    string content = await response.Content.ReadAsStringAsync();

                    JObject root = JObject.Parse(content);
                    Dictionary<string, BitmapImage> dict = new Dictionary<string,BitmapImage>();

                    if (root["items"] != null)
                    {
                        JArray items = (JArray)root["items"];

                        //JObject item;
                        //JToken jtoken;
                        foreach (JObject i in items)
                        {
                            BlogItem blogItem = new BlogItem();

                            //System.Diagnostics.Debug.WriteLine(i["id"]);
                            //System.Diagnostics.Debug.WriteLine(i["name"]);
                            //System.Diagnostics.Debug.WriteLine(i["description"]);

                            blogItem.id = i["id"].ToString();
                            blogItem.name = i["name"].ToString();
                            blogItem.summary = i["description"].ToString();
                            blogItem.url = i["url"].ToString();
                            blogItem.posts = i["posts"]["totalItems"].ToString();
                            blogItem.published = Convert.ToDateTime(i["published"].ToString());     //i["published"].ToString();
                            blogItem.updated = Convert.ToDateTime(i["updated"].ToString());


                            //System.Diagnostics.Debug.WriteLine("https://www.googleapis.com/blogger/v3/blogs/" + blogItem.id + "/posts");
                            HttpResponseMessage postResponse = await client.GetAsync("https://www.googleapis.com/blogger/v3/blogs/" + blogItem.id + "/posts?fetchBodies=false&maxResults=20");
                            if (response.IsSuccessStatusCode == true)
                            {    
                                string postContent = await postResponse.Content.ReadAsStringAsync();
                                //System.Diagnostics.Debug.WriteLine(postContent);
                                JObject postRoot = JObject.Parse(postContent);
                                if (postRoot["items"] != null)
                                {
                                    JArray posts = (JArray)postRoot["items"];

                                    foreach (JObject p in posts)
                                    {
                                        PostItem postItem = new PostItem();
                                        postItem.blogID = blogItem.id;
                                        postItem.id = p["id"].ToString();
                                        postItem.name = p["title"].ToString();
                                        postItem.time = Convert.ToDateTime(p["published"].ToString());
                                        postItem.author = p["author"]["displayName"].ToString();
                                        postItem.url = p["url"].ToString();
                                        postItem.authorAvatar = new BitmapImage();

                                        string profile = p["author"]["url"].ToString();
                                        if (dict.ContainsKey(profile))
                                            postItem.authorAvatar = dict[profile];
                                        else
                                        {
                                            BitmapImage bitmap = new BitmapImage();
                                            //System.Diagnostics.Debug.WriteLine(profile);
                                            HttpResponseMessage profileResponse = await client.GetAsync(profile);
                                            if (profileResponse.IsSuccessStatusCode == true)
                                            {
                                                string profileHTML = await profileResponse.Content.ReadAsStringAsync();
                                                HtmlDocument doc = new HtmlDocument();
                                                doc.LoadHtml(profileHTML);

                                                string ziuta = (from node in doc.DocumentNode.Descendants()
                                                                where node.Name == "img"
                                                                where node.Attributes[0].Value == "photo"
                                                                select node.Attributes[1].Value).FirstOrDefault();
                                                if (ziuta != null)
                                                {
                                                    HttpResponseMessage message = await client.GetAsync(ziuta);
                                                    byte[] bytes = await message.Content.ReadAsByteArrayAsync();
                                                    InMemoryRandomAccessStream randomAccessStream = new InMemoryRandomAccessStream();
                                                    DataWriter writer = new DataWriter(randomAccessStream.GetOutputStreamAt(0));
                                                    writer.WriteBytes(bytes);
                                                    await writer.StoreAsync();
                                                    bitmap.SetSource(randomAccessStream);
                                                }
                                                else
                                                    bitmap.UriSource = new Uri("ms-appx:/Assets/blogger.png", UriKind.Absolute);
                                            }
                                            else bitmap.UriSource = new Uri("ms-appx:/Assets/blogger.png", UriKind.Absolute);
                                            dict.Add(profile, bitmap);
                                            postItem.authorAvatar = dict[profile];
                                        }

                                        blogItem.Items.Add(postItem);
                                    }
                                    isFinished = false;
                                    isOpen = true;
                                }
                                this.blogs.Add(blogItem);
                            }
                            else
                            {
                                Windows.UI.Popups.MessageDialog dialog = new Windows.UI.Popups.MessageDialog(App.loader.GetString("checkYourConnection"), App.loader.GetString("errorReturned"));
                                await dialog.ShowAsync();
                                return;
                            }
                        }                
                    }
                    else
                    {
                        isFinished = false;
                        isOpen = true;
                        if (visible != Visibility.Visible) visible = Visibility.Visible;
                    }
                }
                else
                {
                    Windows.UI.Popups.MessageDialog dialog = new Windows.UI.Popups.MessageDialog(App.loader.GetString("checkYourConnection"), App.loader.GetString("errorReturned"));
                    await dialog.ShowAsync();
                }
            }
            catch
            {
                Windows.UI.Popups.MessageDialog dialog = new Windows.UI.Popups.MessageDialog(App.loader.GetString("checkYourConnection"), App.loader.GetString("notConnected"));
                dialog.ShowAsync();
                logout();
                return;
            }
        }

        public async void doLogin(string email, string password)
        {
            HttpClient client = new HttpClient();
            //client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("));

            string postData = "accountType=HOSTED_OR_GOOGLE";
            postData += "&Email=" + email;
            postData += "&Passwd=" + password;
            postData += "&service=blogger";
            postData += "&source=test-test-.01";
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);

            System.Diagnostics.Debug.WriteLine(postData);

            HttpContent content = new ByteArrayContent(byteArray);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
            HttpResponseMessage response = await client.PostAsync("https://www.google.com/accounts/ClientLogin", content);
            string responseFromServer = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine(responseFromServer);
            this.authenticationToken = responseFromServer.Substring(responseFromServer.IndexOf("Auth=") + 5);
        }

        private async Task<string> WelcomeDialog()
        {
            isFinished = true;

            var messageDialog = new MessageDialog("To use this program, you need to sign in. ", "Welcome to Blogg!");

            // Add commands and set their command ids
            messageDialog.Commands.Add(new UICommand("Sign in...", null, 1));
            messageDialog.Commands.Add(new UICommand("or create new account.", null, 2));

            // Set the command that will be invoked by default
            messageDialog.DefaultCommandIndex = 1;

            // Show the message dialog and get the event that was invoked via the async operator
            var commandChosen = await messageDialog.ShowAsync();

            if (commandChosen.Id.ToString() == "2")
            {
                await Launcher.LaunchUriAsync(new Uri("http://www.blogger.com/", UriKind.Absolute));
                isFinished = false;
                await WelcomeDialog();
            }
            else return await oauth2.ConnectToGoogle();
            // Display message showing the label and id of the command that was invoked
            // rootPage.NotifyUser("The '" + commandChosen.Label + "' (" + commandChosen.Id + ") command has been selected.", NotifyType.StatusMessage);

            return null;
        }

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
    }
}
