using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Blogg.Data;
using Newtonsoft.Json.Linq;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.Networking.Connectivity;

namespace Blogg
{
    public class isFinishedNotifier : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private bool _Visible = false;
        private bool _isOpen = true;
        private Visibility _visible;

        public bool Visible
        {
            get { return this._Visible; }

            set
            {
                if (value != this._Visible)
                {
                    this._Visible = value;
                    NotifyPropertyChanged("Visible");
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

        private bool _Enabled;

        public bool Enabled
        {
            get { return this._Enabled; }

            set
            {
                if (value != this._Enabled)
                {
                    this._Enabled = value;
                    NotifyPropertyChanged("Enabled");
                }
            }
        }

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

    }

    public static class PostUtility
    {
        public static OAuth2 bloggerOAuth2;
        public static isFinishedNotifier isFinished;

        public static async Task sendPost(string blogID, string title, string content, string access_token)
        {
            isFinished.Visible = true;
            isFinished.Enabled = false;
            HttpClient client = new HttpClient();

            if (title == null)
            {
                title = "";
            }

            JObject rss = new JObject(
                new JProperty("kind", "blogger#post"),
                new JProperty("blog", new JObject(new JProperty("id", blogID))),
                new JProperty("title", title),
                new JProperty("content", content));
            //labels

            //System.Diagnostics.Debug.WriteLine(rss.ToString());

            try
            {
                client.DefaultRequestHeaders.Authorization = System.Net.Http.Headers.AuthenticationHeaderValue.Parse("Bearer " + access_token);
                HttpContent httpContent = new StringContent(rss.ToString());
                httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                HttpResponseMessage response = await client.PostAsync("https://www.googleapis.com/blogger/v3/blogs/" + blogID + "/posts/", httpContent);
                //string ziuta = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine(response.StatusCode);
                if (response.IsSuccessStatusCode == true)
                {
                    Windows.UI.Popups.MessageDialog dialog = new Windows.UI.Popups.MessageDialog(App.loader.GetString("successUpload"), App.loader.GetString("hurray"));
                    await dialog.ShowAsync();
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
                App.dataSource.logout();
                return;
            }
            isFinished.Visible = false;
            isFinished.Enabled = true;
            isFinished.visible = Visibility.Visible;

        }

        public static async Task removePost(string blogID, string postID, string access_token)
        {
            var messageDialog = new MessageDialog(App.loader.GetString("reallyWantRemove"), App.loader.GetString("Question"));

            messageDialog.Commands.Add(new UICommand(App.loader.GetString("yesDeleteIt"), null, 1));
            messageDialog.Commands.Add(new UICommand(App.loader.GetString("no"), null, 2));

            messageDialog.DefaultCommandIndex = 2;

            var commandChosen = await messageDialog.ShowAsync();

            if (commandChosen.Id.ToString() == "1")
            {
                try
                {
                    HttpClient client = new HttpClient();
                    client.DefaultRequestHeaders.Authorization = System.Net.Http.Headers.AuthenticationHeaderValue.Parse("Bearer " + access_token);
                    HttpResponseMessage response = await client.DeleteAsync("https://www.googleapis.com/blogger/v3/blogs/" + blogID + "/posts/" + postID);
                    if (response.IsSuccessStatusCode == true)
                    {
                        /*
                         * from gr in App.dataSource.blogs
                               from items in gr.Items
                               where items.id == (string)navigationParameter
                               select items).First();*/
                        for (int i = 0; i < App.dataSource.blogs.Count(); i++)
                        {
                            for (int j = 0; j < App.dataSource.blogs[i].Items.Count; j++)
                            {
                                if ((App.dataSource.blogs[i].Items[j].id == postID) &&
                                    (App.dataSource.blogs[i].Items[j].blogID == blogID))
                                    App.dataSource.blogs[i].Items.RemoveAt(j);
                            }
                        }

                        var success = new MessageDialog(App.loader.GetString("successDelete"), App.loader.GetString("hurray"));
                        success.Commands.Add(new UICommand("OK", null, 1));
                        success.DefaultCommandIndex = 1;
                        await success.ShowAsync();
                    }
                    else
                    {
                        Windows.UI.Popups.MessageDialog dialog = new Windows.UI.Popups.MessageDialog(App.loader.GetString("checkYourConnection"), App.loader.GetString("errorReturned"));
                        return;
                    }
                }
                catch
                {
                    Windows.UI.Popups.MessageDialog dialog = new Windows.UI.Popups.MessageDialog(App.loader.GetString("checkYourConnection"), App.loader.GetString("notConnected"));
                    dialog.ShowAsync();
                    App.dataSource.logout();
                    return;
                }
            }


            //DELETE https://www.googleapis.com/blogger/v3/blogs/8070105920543249955/posts/6819100329896798058
            //Authorization: /* OAuth 2.0 token here */
            
        }

        public static async Task<string> uploadPhoto(string blogName, StorageFile file, string access_token)
        {
            isFinished.Visible = true;
            isFinished.Enabled = false;
            isFinished.visible = Visibility.Collapsed;
            string blogUrl = null;
            HttpClient client = new HttpClient();
            try
            {
                client.DefaultRequestHeaders.Authorization = System.Net.Http.Headers.AuthenticationHeaderValue.Parse("Bearer " + access_token);
                client.DefaultRequestHeaders.Add("GData-Version", "2");
                HttpResponseMessage response = await client.GetAsync("https://picasaweb.google.com/data/feed/api/user/default");
                if (response.IsSuccessStatusCode == true)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    //System.Diagnostics.Debug.WriteLine(content);
                    XElement doc = XElement.Parse(content);
                    XNamespace name = "http://www.w3.org/2005/Atom";
                    foreach (XElement element in doc.Descendants(name + "entry"))
                    {
                        if (element.Element(name + "title").Value == blogName)
                        {
                            blogUrl = element.Element(name + "link").Attribute("href").Value;
                            //blogUrl = element.Element(name + "id").Value;
                            break;
                        }
                    }
                    if (blogUrl == null)
                    {
                        blogUrl = "https://picasaweb.google.com/data/feed/api/user/default/albumid/default";
                    }
                    //System.Diagnostics.Debug.WriteLine(blogUrl);
                    //StorageFile file = await StorageFile.GetFileFromPathAsync(localURL);
                    ConnectionProfile internetConnectionProfile = NetworkInformation.GetInternetConnectionProfile();
                    var dataPlanStatus = internetConnectionProfile.GetDataPlanStatus();
                    if (dataPlanStatus.MaxTransferSizeInMegabytes != null) 
                    {
                        var size = (await file.GetBasicPropertiesAsync()).Size;
                        if (size > (dataPlanStatus.MaxTransferSizeInMegabytes) * 1024 * 1024)
                        {
                            var messageDialog = new MessageDialog(App.loader.GetString("fileSize"), App.loader.GetString("warning"));

                            messageDialog.Commands.Add(new UICommand("OK", null, 1));
                            messageDialog.Commands.Add(new UICommand(App.loader.GetString("cancel"), null, 2));

                            messageDialog.DefaultCommandIndex = 1;

                            var commandChosen = await messageDialog.ShowAsync();

                            if (commandChosen.Id.ToString() == "2")
                            {
                                return null;
                            }
                        }
                    }

                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, new Uri(blogUrl));

                    StreamReader sr = new StreamReader(await file.OpenStreamForReadAsync());
                    request.Content = new StreamContent(sr.BaseStream);
                    request.Content.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                    HttpResponseMessage postResponse = await client.SendAsync(request);
                    string result = await postResponse.Content.ReadAsStringAsync();
                    //System.Diagnostics.Debug.WriteLine(result);
                    if (postResponse.IsSuccessStatusCode == true)
                    {
                        XDocument res = XDocument.Parse(result);
                        //System.Diagnostics.Debug.WriteLine(res.Element(name + "entry").Element(name + "content").Attribute("src").Value);
                        isFinished.Visible = false;
                        isFinished.Enabled = true;
                        isFinished.visible = Visibility.Visible;
                        return res.Element(name + "entry").Element(name + "content").Attribute("src").Value;
                    }
                    else
                    {
                        Windows.UI.Popups.MessageDialog dialog = new Windows.UI.Popups.MessageDialog(App.loader.GetString("checkYourConnection"), App.loader.GetString("errorReturned"));
                        await dialog.ShowAsync();
                        isFinished.visible = Visibility.Visible;
                        isFinished.Visible = false;
                        isFinished.Enabled = true;
                        return "";
                    }
                }
                else
                {
                    Windows.UI.Popups.MessageDialog dialog = new Windows.UI.Popups.MessageDialog(App.loader.GetString("checkYourConnection"), App.loader.GetString("errorReturned"));
                    await dialog.ShowAsync();
                    System.Diagnostics.Debug.WriteLine("response ERROR : " + response.StatusCode);
                    isFinished.visible = Visibility.Visible;
                    isFinished.Visible = false;
                    isFinished.Enabled = true;
                    return "";
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

        public static Task<StreamContent> ReadStorageFileAsync(StorageFile storageFile)
        {
            return Task.Run<StreamContent>(async () =>
            {
                var props = await storageFile.GetBasicPropertiesAsync();
                ulong size = props.Size;
                IInputStream inputStream = await storageFile.OpenReadAsync();
                DataReader dataReader = new DataReader(inputStream);
                await dataReader.LoadAsync((uint)size);
                byte[] buffer = new byte[(int)size];
                dataReader.ReadBytes(buffer);
                //return buffer;
                MemoryStream theMemStream = new MemoryStream();
                theMemStream.Write(buffer, 0, buffer.Length);
                StreamContent content = new StreamContent(theMemStream);
                return content;
            });
        }
                
    }
}
