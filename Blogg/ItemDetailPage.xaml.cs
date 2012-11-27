using Blogg.Data;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Documents;
using HtmlAgilityPack;
using System.Net.Http;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;
using Windows.System;
using Windows.UI.ViewManagement;
using Windows.ApplicationModel.DataTransfer;


// The Item Detail Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234232

namespace Blogg
{
    /// <summary>
    /// A page that displays details for a single item within a group while allowing gestures to
    /// flip through other items belonging to the same group.
    /// </summary>
    public sealed partial class ItemDetailPage : Blogg.Common.LayoutAwarePage
    {
        public ItemDetailPage()
        {
            this.InitializeComponent();
            App.dataTransferManager.DataRequested += ShareTextHandler;
            //this.DataContext = App.dataSource;
        }

        private string uri, name, blog;
        private PostItem item;
        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="navigationParameter">The parameter value passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested.
        /// </param>
        /// <param name="pageState">A dictionary of state preserved by this page during an earlier
        /// session.  This will be null the first time a page is visited.</param>
        /// 

        private void ShareTextHandler(DataTransferManager sender, DataRequestedEventArgs e)
        {
            DataRequest request = e.Request;
            request.Data.Properties.Title = "Blogg";
            request.Data.Properties.Description = App.loader.GetString("sharePost");
            request.Data.SetText(name + " | " + blog + " " + uri);

        }

        protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            // Run the PopInThemeAnimation 
            Windows.UI.Xaml.Media.Animation.Storyboard sb =
                this.FindName("PopInStoryboard") as Windows.UI.Xaml.Media.Animation.Storyboard;
            if (sb != null) sb.Begin();

            // Allow saved page state to override the initial item to display
            if (pageState != null && pageState.ContainsKey("SelectedItem"))
            {
                navigationParameter = pageState["SelectedItem"];
            }

            item = (from gr in App.dataSource.blogs
                           from items in gr.Items
                           where items.id == (string)navigationParameter
                           select items).First();
            var group = (from gr in App.dataSource.blogs
                         from items in gr.Items
                         where items.blogID == item.blogID
                         select gr).First();

            //var item = BlogsDataSource.GetItem((string)navigationParameter);
            this.DefaultViewModel["Group"] = group;
            this.DefaultViewModel["Items"] = group.Items;
            this.DefaultViewModel["Item"] = item;
            uri = item.url;
            name = item.name;
            blog = group.name;
           // convertToXAML(item.content);
            webView.Navigate(new Uri(item.url));
            //?m=1
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="pageState">An empty dictionary to be populated with serializable state.</param>
        protected override void SaveState(Dictionary<String, Object> pageState)
        {
            //var selectedItem = (PostItem)this.flipView.SelectedItem;
            //pageState["SelectedItem"] = selectedItem.id;
        }

        private async void webBrowser_Click_1(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri(uri, UriKind.Absolute));
        }

        private async void deleteClick(object sender, RoutedEventArgs e)
        {
            await PostUtility.removePost(item.blogID, item.id, App.dataSource.oauth2.access_token);
            this.Frame.GoBack();
        }

        /*
        public async void convertToXAML(string html)
        {
            //string html = "<div class=\"separator\" style=\"clear: both; text-align: center;\">" +
            //    "<a href=\"http://pages.cs.wisc.edu/~roy/Crepes/pictures-medium/39-BreakEgg.jpg\" imageanchor=\"1\" style=\"margin-left: 1em; margin-right: 1em;\"><img border=\"0\" height=\"240\" src=\"http://pages.cs.wisc.edu/~roy/Crepes/pictures-medium/39-BreakEgg.jpg\" width=\"320\" /></a></div>" +
            //    "<br /> I probably confirm something what you already noticed xD<br /><br />" +
            //    "I break from blogging, at least the end of May - simply because my current job prevents me from writing a valuable posts (on some level).<br /><br />However, this doesn't mean that I don't come back. Not only that, expect a small revolution ;)";
            //RichTextBlock block = new RichTextBlock();
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(html);

            richTextBlock.VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Top;
            foreach (HtmlNode node in document.DocumentNode.Descendants())
            {
                if (node.Name == "a")
                {
                    //var inline = new InlineUIContainer();
                    //var link = new HyperlinkButton();
                    //link.NavigateUri = new Uri(node.Attributes["href"].Value);
                    //inline.Child = link;
                    //paragraph.Inlines.Add(inline);
                }
                //else if (node.Name == "br")
                //{
                //    paragraph.Inlines.Add(new LineBreak());
                //}
                //else if (node.Name == "p")
                //{
                //    Run run = new Run();
                //    run.Text = node.InnerText.Trim();
                //    paragraph.Inlines.Add(run);
                //}
                else if (node.Name == "img")
                {
                    Paragraph paragraph = new Paragraph();
                    //========================================================================
                    HttpClient client = new HttpClient();
                    HttpResponseMessage message = await client.GetAsync(node.Attributes["src"].Value);
                    byte[] bytes = await message.Content.ReadAsByteArrayAsync();
                    InMemoryRandomAccessStream randomAccessStream = new InMemoryRandomAccessStream();
                    DataWriter writer = new DataWriter(randomAccessStream.GetOutputStreamAt(0));
                    writer.WriteBytes(bytes);
                    await writer.StoreAsync();
                    BitmapImage image = new BitmapImage();
                    image.SetSource(randomAccessStream);
                    InlineUIContainer container = new InlineUIContainer();
                    Image ima = new Image();
                    ima.Source = image;
                    ima.Stretch = Stretch.Uniform;
                    //ima.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Center;
                    ima.MaxHeight = 400;
                    container.Child = ima;
                    paragraph.Inlines.Add(container);
                    paragraph.TextAlignment = TextAlignment.Center;
                    richTextBlock.Blocks.Add(paragraph);

                }
                else if ((node.Name == "#text") || (node.InnerText.Trim() != ""))
                {
                    Paragraph paragraph = new Paragraph();
                    Run run = new Run();
                    run.Text = node.InnerText.Trim();
                paragraph.Inlines.Add(run);
                    paragraph.TextAlignment = TextAlignment.Justify;
                    richTextBlock.Blocks.Add(paragraph);
                }
                System.Diagnostics.Debug.WriteLine(node.Name);
            }
         */
        //}

        private void pageRoot_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ApplicationView.Value == ApplicationViewState.Snapped)
            {
                webView.Navigate(new Uri(item.url + "?m=1"));
            }
            else
            {
                webView.Navigate(new Uri(item.url));
            }
        }

        private void pageRoot_Unloaded(object sender, RoutedEventArgs e)
        {
            App.dataTransferManager.DataRequested -= ShareTextHandler;
        }

        private void AppBar_Opened(object sender, object e)
        {
            WebViewBrush wvb = new WebViewBrush();
            wvb.SourceName = "webView";
            wvb.Redraw();
            contentViewRect.Fill = wvb;
            webView.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        private void AppBar_Closed(object sender, object e)
        {
            webView.Visibility = Windows.UI.Xaml.Visibility.Visible;
            contentViewRect.Fill = new SolidColorBrush(Windows.UI.Colors.Transparent);
        }
    }
}
