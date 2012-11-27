using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Data.Xml.Dom;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace Blogg
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class newPostPage : Blogg.Common.LayoutAwarePage
    {
        private addLink AddLink;
        private Popup flyout;
        private string blogID;

        public newPostPage()
        {
            this.InitializeComponent();
            AddLink = new Blogg.addLink();
            AddLink.createButton.Click += createButton_Click;
            AddLink.Loaded += AddLink_Loaded;
            AddLink.linkTextBox.KeyUp += linkTextBox_KeyUp;
            flyout = new Popup();
            PostUtility.isFinished = new isFinishedNotifier();
            DataContext = PostUtility.isFinished;
            //pubButton.DataContext = PostUtility.isFinished;
            //progressRing.DataContext = PostUtility.isFinished;
            //AddLink.Content.
            richEditBox.Document.SetText(TextSetOptions.None, "");
            //var d = new DispatcherTimer();
            //d.Start();
            //d.Interval = new TimeSpan(0, 1, 0);
            //d.Tick += (sender, o) => { richEditBox_TextChanged(); };
            
        }

        void linkTextBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter) createButton_Click(sender, e);
        }

        void AddLink_Loaded(object sender, RoutedEventArgs e)
        {
            AddLink.linkTextBox.Text = richEditBox.Document.Selection.Link.Trim();
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="navigationParameter">The parameter value passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested.
        /// </param>
        /// <param name="pageState">A dictionary of state preserved by this page during an earlier
        /// session.  This will be null the first time a page is visited.</param>
        protected override async void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            // Restore values stored in app data.
            Windows.Storage.ApplicationDataContainer localSettings =
                Windows.Storage.ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey("richEditBox"))
            {
                try
                {
                    StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                    StorageFile sampleFile = await localFolder.GetFileAsync(localSettings.Values["richEditBox"].ToString());
                    string setter = await FileIO.ReadTextAsync(sampleFile);
                    richEditBox.Document.SetText(TextSetOptions.FormatRtf, setter);
                }
                catch (Exception e)
                {
                    richEditBox.Document.SetText(TextSetOptions.FormatRtf, "");
                }
            }

            if (localSettings.Values.ContainsKey("postTitle"))
            {
                //richEditBox.Document. = (ITextDocument)localSettings.Values["richEditBox"];
                textBox.Text = localSettings.Values["postTitle"].ToString();
            }

            blogID = (string)navigationParameter;
        }
        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="pageState">An empty dictionary to be populated with serializable state.</param>
        protected override void SaveState(Dictionary<String, Object> pageState)
        {

        }

        //buttons
        #region

        private void boldClick(object sender, RoutedEventArgs e)
        {
            richEditBox.Document.Selection.CharacterFormat.Bold = Windows.UI.Text.FormatEffect.Toggle;
        }

        private void italicClick(object sender, RoutedEventArgs e)
        {
            richEditBox.Document.Selection.CharacterFormat.Italic = Windows.UI.Text.FormatEffect.Toggle;
        }

        private void underlineClick(object sender, RoutedEventArgs e)
        {
            if (richEditBox.Document.Selection.CharacterFormat.Underline == Windows.UI.Text.UnderlineType.Single)
                richEditBox.Document.Selection.CharacterFormat.Underline = Windows.UI.Text.UnderlineType.None;
            else richEditBox.Document.Selection.CharacterFormat.Underline = Windows.UI.Text.UnderlineType.Single;
            //convertToHTML();
        }

        private void strikeClick(object sender, RoutedEventArgs e)
        {
            richEditBox.Document.Selection.CharacterFormat.Strikethrough = Windows.UI.Text.FormatEffect.Toggle;
        }

        private async void listClick(object sender, RoutedEventArgs e)
        {
            var menu = new PopupMenu();
            menu.Commands.Add(new UICommand(App.loader.GetString("unordered"), (command) =>
            {
                if (richEditBox.Document.Selection.ParagraphFormat.ListType != MarkerType.None)
                    richEditBox.Document.Selection.ParagraphFormat.ListType = MarkerType.None;
                else richEditBox.Document.Selection.ParagraphFormat.ListType = MarkerType.Bullet;
            }));
            menu.Commands.Add(new UICommand(App.loader.GetString("ordered"), (command) =>
            {
                if (richEditBox.Document.Selection.ParagraphFormat.ListType != MarkerType.None)
                    richEditBox.Document.Selection.ParagraphFormat.ListType = MarkerType.None;
                else richEditBox.Document.Selection.ParagraphFormat.ListType = MarkerType.Arabic;
            }));
            var chosenCommand = await menu.ShowForSelectionAsync(GetElementRect((FrameworkElement)sender));         
        }

        private void linkClick(object sender, RoutedEventArgs e)
        {
            if (richEditBox.Document.Selection.Length == 0)
            {
                XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText01);
                XmlNodeList stringElements = toastXml.GetElementsByTagName("text");
                stringElements[0].AppendChild(toastXml.CreateTextNode(App.loader.GetString("selectSomeText")));
                ToastNotification toast = new ToastNotification(toastXml);
                ToastNotificationManager.CreateToastNotifier().Show(toast);
            }
            else
            {
                if (flyout.IsOpen == false) ShowPopup(linkButton, AddLink);
                else flyout.IsOpen = false;
            }
        }

        private async void justifyClick(object sender, RoutedEventArgs e)
        {
            // Create a menu and add commands specifying a callback delegate for each.
            // Since command delegates are unique, no need to specify command Ids.
            var menu = new PopupMenu();
            menu.Commands.Add(new UICommand(App.loader.GetString("justifyLeft"), (command) =>
            {
                richEditBox.Document.Selection.ParagraphFormat.Alignment = Windows.UI.Text.ParagraphAlignment.Left;
            }));
            menu.Commands.Add(new UICommand(App.loader.GetString("justifyCenter"), (command) =>
            {
                richEditBox.Document.Selection.ParagraphFormat.Alignment = Windows.UI.Text.ParagraphAlignment.Center;
            }));
            menu.Commands.Add(new UICommand(App.loader.GetString("justifyRight"), (command) =>
            {
                richEditBox.Document.Selection.ParagraphFormat.Alignment = Windows.UI.Text.ParagraphAlignment.Right;
            }));
            var chosenCommand = await menu.ShowForSelectionAsync(GetElementRect((FrameworkElement)sender));
            // We don't want to obscure content, so pass in a rectangle representing the sender of the context menu event.
            // We registered command callbacks; no need to handle the menu completion event
        }

        private async void imageClick(object sender, RoutedEventArgs e)
        {
            // Create a menu and add commands specifying a callback delegate for each.
            // Since command delegates are unique, no need to specify command Ids.
            var menu = new PopupMenu();
            menu.Commands.Add(new UICommand(App.loader.GetString("From Pictures"), (command) =>
            {
                PickAFileButton_Click(sender, e);
            }));
            menu.Commands.Add(new UICommand(App.loader.GetString("From Camera"), (command) =>
            {
                CapturePhoto_Click(sender, e);
            }));
            var chosenCommand = await menu.ShowForSelectionAsync(GetElementRect((FrameworkElement)sender));
            // We don't want to obscure content, so pass in a rectangle representing the sender of the context menu event.
            // We registered command callbacks; no need to handle the menu completion event
        }

        private async void CapturePhoto_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Using Windows.Media.Capture.CameraCaptureUI API to capture a photo
                CameraCaptureUI dialog = new CameraCaptureUI();
                Size aspectRatio = new Size(16, 9);
                dialog.PhotoSettings.CroppedAspectRatio = aspectRatio;

                StorageFile file = await dialog.CaptureFileAsync(CameraCaptureUIMode.Photo);
                if (file != null)
                {
                    ImageProperties props = await file.Properties.GetImagePropertiesAsync();

                    //IRandomAccessStream fileStream = await file.OpenAsync(FileAccessMode.Read);
                    //bitmapImage.SetSource(fileStream);
                    using (IRandomAccessStream fs = await file.OpenAsync(FileAccessMode.Read))
                    {
                        int startPosition = richEditBox.Document.Selection.StartPosition;
                        int endPosition = richEditBox.Document.Selection.EndPosition;
                        string blogName = (from gr in App.dataSource.blogs
                                     where gr.id == blogID
                                     select gr.name).First();
                        bottomAppBar.IsOpen = false;
                        string link = await PostUtility.uploadPhoto(blogName, file, App.dataSource.oauth2.access_token);
                        bottomAppBar.IsOpen = true;
                        //richEditBox.Document.Selection.StartPosition--;
                        //richEditBox.Document.Selection.EndPosition++;
                        richEditBox.Document.Selection.InsertImage((int)props.Width, (int)props.Height, 0, VerticalCharacterAlignment.Baseline, "IMAGE", fs);
                        richEditBox.Document.Selection.SetRange(startPosition, endPosition + 1);
                        try
                        {
                            richEditBox.Document.Selection.Link = "\"" + link + "\"";
                        }
                        catch { };

                    }
                    //CapturedPhoto.Source = bitmapImage;
                    //IRandomAccessStream fileStream = await file.OpenAsync(FileAccessMode.Read);
                    //richEditBox.Document.Selection.InsertImage(bitmapImage.PixelWidth, bitmapImage.PixelWidth, 0, VerticalCharacterAlignment.Top, "IMAGE", fileStream);
                }
                else
                {
                    
                }
            }
            catch (Exception ex)
            {
            }
        }

        private async void PickAFileButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear previous returned file name, if it exists, between iterations of this scenario
            //rootPage.ResetScenarioOutput(OutputTextBlock);

            //if (newPostPage.EnsureUnsnapped())
            //{
                FileOpenPicker openPicker = new FileOpenPicker();
                openPicker.ViewMode = PickerViewMode.Thumbnail;
                openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                openPicker.FileTypeFilter.Add(".jpg");
                openPicker.FileTypeFilter.Add(".jpeg");
                openPicker.FileTypeFilter.Add(".png");
                StorageFile file = await openPicker.PickSingleFileAsync();
                if (file != null)
                {
                    ImageProperties props = await file.Properties.GetImagePropertiesAsync();

                    //IRandomAccessStream fileStream = await file.OpenAsync(FileAccessMode.Read);
                    //bitmapImage.SetSource(fileStream);
                    using (IRandomAccessStream fs = await file.OpenAsync(FileAccessMode.Read))
                    {
                        int startPosition = richEditBox.Document.Selection.StartPosition;
                        int endPosition = richEditBox.Document.Selection.EndPosition;
                        string blogName = (from gr in App.dataSource.blogs
                                           where gr.id == blogID
                                           select gr.name).First();
                        bottomAppBar.IsOpen = false;
                        string link = await PostUtility.uploadPhoto(blogName, file, App.dataSource.oauth2.access_token);
                        bottomAppBar.IsOpen = true;
                        //richEditBox.Document.Selection.StartPosition--;
                        //richEditBox.Document.Selection.EndPosition++;
                        richEditBox.Document.Selection.InsertImage((int)props.Width, (int)props.Height, 0, VerticalCharacterAlignment.Baseline, "IMAGE", fs);
                        richEditBox.Document.Selection.SetRange(startPosition, endPosition + 1);
                        try
                        {
                            richEditBox.Document.Selection.Link = "\"" + link + "\"";
                        }
                        catch { };
                    }
                }
                else
                {
                    
                }
        }

        #endregion

        private Popup ShowPopup(FrameworkElement source, UserControl control)
        {
            
            var windowBounds = Window.Current.Bounds;
            var rootVisual = Window.Current.Content;

            GeneralTransform gt = source.TransformToVisual(rootVisual);

            var absolutePosition = gt.TransformPoint(new Point(0, 0));

            control.Measure(new Size(Double.PositiveInfinity, double.PositiveInfinity));

            flyout.VerticalOffset = absolutePosition.Y - control.Height - 10;
            flyout.HorizontalOffset = (absolutePosition.X + source.ActualWidth / 2) - control.Width / 2;
            flyout.IsLightDismissEnabled = true;

            flyout.Child = control;
            var transitions = new TransitionCollection();
            transitions.Add(new PopupThemeTransition() { FromHorizontalOffset = 0, FromVerticalOffset = 100 });
            flyout.ChildTransitions = transitions;
            flyout.IsOpen = true;

            int flyoutOffset = 0;
            Windows.UI.ViewManagement.InputPane.GetForCurrentView().Showing += (s, args) =>
            {
                flyoutOffset = (int)args.OccludedRect.Height;
                flyout.VerticalOffset -= flyoutOffset;
            };
            Windows.UI.ViewManagement.InputPane.GetForCurrentView().Hiding += (s, args) =>
            {
                flyout.VerticalOffset += flyoutOffset;
            };

            return flyout;
        }

        public static Rect GetElementRect(FrameworkElement element)
        {
            GeneralTransform buttonTransform = element.TransformToVisual(null);
            Point point = buttonTransform.TransformPoint(new Point());
            return new Rect(point, new Size(element.ActualWidth, element.ActualHeight));
        }

        private string convertToHTML()
        {
            int intCount;
            ITextRange tr = richEditBox.Document.Selection;
            tr.StartPosition = 0;
            tr.EndPosition = tr.StoryLength;

            int lngOriginalStart = tr.StartPosition;
            int lngOriginalLength = tr.EndPosition;
            tr.SetRange(tr.StartPosition, tr.EndPosition);

            string strColour = tr.CharacterFormat.ForegroundColor.ToString();
            string strFont = tr.CharacterFormat.FontStyle.ToString();
            float shtSize = tr.CharacterFormat.Size;
            string strBold = tr.CharacterFormat.Bold.ToString();
            string strItalic = tr.CharacterFormat.Italic.ToString();
            string strUnderline = tr.CharacterFormat.Underline.ToString();
            string strFntName = tr.CharacterFormat.Name;

            //string strHTML = "<span style=\"font-family:" + strFntName + "; font-size: " + shtSize + "pt; color: #" + strColour.Substring(3) + "\">";
            string strHTML = "<span />";

            bool isBold = false, isItalic = false, isUnderline = false, isStrike = false, isNormal = false, isListed = false, endLi = true;

            for (intCount = 1; intCount < lngOriginalLength; intCount++)
            {
                tr.SetRange(intCount, intCount + 1);
                //System.Diagnostics.Debug.WriteLine(tr.Character);

                if ((tr.ParagraphFormat.ListType != MarkerType.Bullet) && (tr.ParagraphFormat.ListType != MarkerType.Arabic))
                    tr.ParagraphFormat.ListType = MarkerType.None;

                //if (tr.CharacterFormat.ForegroundColor.ToString() != strColour || tr.CharacterFormat.Name != strFntName || tr.CharacterFormat.Size != shtSize)
                //{
                //    strHTML += "</span><span style=\"font-family: " + tr.CharacterFormat.Name + "; font size: " + tr.CharacterFormat.Size + "pt; color: #" + tr.CharacterFormat.ForegroundColor.ToString().Substring(3) + "\">";
                //}

                if (((tr.ParagraphFormat.ListType == MarkerType.Arabic) || (tr.ParagraphFormat.ListType == MarkerType.Bullet)) && (isListed == false))
                {
                    isListed = true;
                    endLi = true;
                    if (tr.ParagraphFormat.ListType == MarkerType.Arabic)
                        strHTML += "<ol>";
                    else if (tr.ParagraphFormat.ListType == MarkerType.Bullet)
                        strHTML += "<ul>";
                }
                //else if ((tr.ParagraphFormat.ListType == MarkerType.None) && (isListed == true))
                //{
                //    if (tr.ParagraphFormat.ListType == MarkerType.Arabic)
                //        strHTML += "</ol>";
                //    else if (tr.ParagraphFormat.ListType == MarkerType.Bullet)
                //        strHTML += "</ul>";
                //    isListed = false;
                //}

                if (((tr.ParagraphFormat.ListType == MarkerType.Arabic) || (tr.ParagraphFormat.ListType == MarkerType.Bullet)) && (endLi == true))
                {
                    strHTML += "<li>";
                    endLi = false;
                }

                if (tr.Character == Convert.ToChar(13))
                {
                    if ((tr.ParagraphFormat.ListType == MarkerType.Arabic) || (tr.ParagraphFormat.ListType == MarkerType.Bullet))
                    {
                        strHTML += "</li>";
                        endLi = true;
                    }
                    else
                    {
                        if ((isListed == true) || (endLi == true))
                        {
                            if (tr.ParagraphFormat.ListType == MarkerType.Arabic)
                                strHTML += "</ol>";
                            else if (tr.ParagraphFormat.ListType == MarkerType.Bullet)
                                strHTML += "</ul>";
                            isListed = false;
                        }
                        strHTML += "<br />";
                    }
                }


                if (tr.CharacterFormat.Bold == FormatEffect.Off)
                {
                    if (isBold == true)
                    {
                        strHTML += "</b>";
                        isBold = false;
                    }
                }
                else if (isBold == false)
                {
                    strHTML += "<b";
                    if (tr.ParagraphFormat.Alignment == ParagraphAlignment.Center)
                        strHTML += " align=\"center\">";
                    else if (tr.ParagraphFormat.Alignment == ParagraphAlignment.Right)
                        strHTML += " align=\"right\">";
                    else strHTML += ">";

                    isBold = true;
                }

                if (tr.CharacterFormat.Italic == FormatEffect.Off)
                {
                    if (isItalic == true)
                    {
                        strHTML += "</i>";
                        isItalic = false;
                    }
                }
                else if (isItalic == false)
                {
                    strHTML += "<i";
                    if (tr.ParagraphFormat.Alignment == ParagraphAlignment.Center)
                        strHTML += " align=\"center\">";
                    else if (tr.ParagraphFormat.Alignment == ParagraphAlignment.Right)
                        strHTML += " align=\"right\">";
                    else strHTML += ">";
                    isItalic = true;
                }

                if (tr.CharacterFormat.Underline == UnderlineType.None)
                {
                    if (isUnderline == true)
                    {
                        strHTML += "</u>";
                        isUnderline = false;
                    }
                }
                else if (isUnderline == false)
                {
                    strHTML += "<u";
                    if (tr.ParagraphFormat.Alignment == ParagraphAlignment.Center)
                        strHTML += " align=\"center\">";
                    else if (tr.ParagraphFormat.Alignment == ParagraphAlignment.Right)
                        strHTML += " align=\"right\">";
                    else strHTML += ">";
                    isUnderline = true;
                }

                if (tr.CharacterFormat.Strikethrough == FormatEffect.Off)
                {
                    if (isStrike == true)
                    {
                        strHTML += "</s>";
                        isStrike = false;
                    }
                }
                else if (isStrike == false)
                {
                    strHTML += "<s";
                    if (tr.ParagraphFormat.Alignment == ParagraphAlignment.Center)
                        strHTML += " align=\"center\">";
                    else if (tr.ParagraphFormat.Alignment == ParagraphAlignment.Right)
                        strHTML += " align=\"right\">";
                    else strHTML += ">";
                    isStrike = true;
                }

                //richEditBox.Document.Selection.ParagraphFormat.Alignment = Windows.UI.Text.ParagraphAlignment.Left;

                if ((isBold == false) && (isItalic == false) && (isUnderline == false) && (isStrike == false))
                {
                    if ((tr.ParagraphFormat.Alignment == ParagraphAlignment.Left) || (tr.ParagraphFormat.Alignment == ParagraphAlignment.Undefined))
                    {
                        if (isNormal == true)
                        {
                            strHTML += "</p>";
                            isNormal = false;
                        }
                    }
                    else if (isNormal == false) 
                    {
                        strHTML += "<p";
                        if (tr.ParagraphFormat.Alignment == ParagraphAlignment.Center)
                            strHTML += " align=\"center\">";
                        else if (tr.ParagraphFormat.Alignment == ParagraphAlignment.Right)
                            strHTML += " align=\"right\">";
                        else strHTML += ">";
                        isNormal = true;
                    }
                }

                if (tr.Link != "")
                {
                    strHTML += "<a";
                    if (tr.ParagraphFormat.Alignment == ParagraphAlignment.Center)
                        strHTML += " align=\"center\"";
                    else if (tr.ParagraphFormat.Alignment == ParagraphAlignment.Right)
                        strHTML += " align=\"right\"";
                    strHTML += " href=" + tr.Link + ">";
                    if (tr.Link.Contains("googleusercontent.com"))
                        strHTML += "<img src=" + tr.Link + " />";
                    else strHTML += App.extractBlogID(tr.Text, 2, '"');
                    strHTML += "</a>";
                    intCount += tr.Text.Length;
                }
                else
                    strHTML += tr.Character;
                //strColour = tr.CharacterFormat.ForegroundColor.ToString();
                //strFont = tr.CharacterFormat.FontStyle.ToString();
                //shtSize = tr.CharacterFormat.Size;
                //strFntName = tr.CharacterFormat.Name;
            }

            if (isBold == true)
                strHTML += "</b>";
            if (isItalic == true)
                strHTML += "</i>";
            if (isUnderline == true)
                strHTML += "</u>";
            if (isStrike == true)
                strHTML += "</s>";
            if (isNormal == true)
                strHTML += "</p>";
            if (endLi == false)
                strHTML += "</li>";
            if (isListed == true)
            {
                if (tr.ParagraphFormat.ListType == MarkerType.Bullet) strHTML += "</ul>";
                else if (tr.ParagraphFormat.ListType == MarkerType.Arabic) strHTML += "</ol>";
            }
            //if (strBold == "on")
            //    strHTML += "</b>";
            //if (strItalic == "on")
            //    strHTML += "</i>";
            //if (strUnderline == "on")
            //    strHTML += "</u>";
            System.Diagnostics.Debug.WriteLine(strHTML);

            strHTML += "</span>";
            return strHTML;

            //tr.SetRange(lngOriginalStart, lngOriginalLength);
            //richeditbox.Document.SetText(TextSetOptions.FormatRtf, strHTML);
        }

        private void textBox_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (textBox.Text == "title")
            {
                textBox.Text = "";
                Color myColor = Colors.Black;
                textBox.Foreground = new SolidColorBrush(myColor);
            }
        }

        private void richEditBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            char[] spaceOrEOL = { ' ', '\r' };
            //if spacebar or enter is pressed, see if they just typed an url
            if ((e.Key == Windows.System.VirtualKey.Space) || (e.Key == Windows.System.VirtualKey.Enter))
            {
                int startpos = richEditBox.Document.Selection.StartPosition;
                int endpos = richEditBox.Document.Selection.EndPosition;

                //doublecheck there's no selection
                if (endpos == startpos)
                {
                    string curText = string.Empty;
                    richEditBox.Document.GetText(TextGetOptions.None, out curText);
                    //remove final space
                    curText = curText.TrimEnd(spaceOrEOL);
                    endpos = curText.Length - 1;

                    //walk backwards until start of line or space is found
                    startpos = curText.LastIndexOfAny(spaceOrEOL);
                    startpos++;

                    string checkForUrl = curText.Substring(startpos, endpos - startpos + 1);
                    if (checkForUrl.StartsWith("http:") || checkForUrl.StartsWith("www") || checkForUrl.StartsWith("https:"))
                    {
                        if (checkForUrl.StartsWith("www")) checkForUrl = "http://" + checkForUrl;
                        //make a hyperlink
                        ITextRange linkRange = richEditBox.Document.GetRange(startpos, endpos + 1);
                        linkRange.Link = "\"" + checkForUrl + "\"";
                        //System.Diagnostics.Debug.WriteLine(linkRange.Text);
                    }
                }
            }
        }

        private void createButton_Click(object sender, RoutedEventArgs e)
        {
            richEditBox.Document.Selection.Link = "\"" + AddLink.linkTextBox.Text + "\"";
            System.Diagnostics.Debug.WriteLine(richEditBox.Document.Selection.Text);
            var transitions = new TransitionCollection();
            transitions.Add(new PopupThemeTransition() { FromHorizontalOffset = 100, FromVerticalOffset = 0 });
            flyout.ChildTransitions = transitions;
            flyout.IsOpen = false;
        }

        private async void publishButton(object sender, RoutedEventArgs e)
        {
            if (textBox.Text != "")
            {
                await PostUtility.sendPost(blogID, textBox.Text, convertToHTML(), App.dataSource.oauth2.access_token);
                App.dataSource.blogs.Clear();
                Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                localSettings.Values.Remove("postTitle");
                localSettings.Values.Remove("richEditBox");
                this.Frame.Navigate(typeof(GroupedItemsPage), null);
            }
            else
            {
                Windows.UI.Popups.MessageDialog dialog = new Windows.UI.Popups.MessageDialog(App.loader.GetString("noTitle"), App.loader.GetString("error"));
                await dialog.ShowAsync();
            }
        }

        private void textBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Windows.Storage.ApplicationDataContainer localSettings =
                Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values["postTitle"] = textBox.Text;
        }

        private async void newGoBack(object sender, RoutedEventArgs e)
        {
            string getter = "";
            richEditBox.Document.GetText(TextGetOptions.None, out getter);
            System.Diagnostics.Debug.WriteLine(getter);
            if ((getter != ""))
            {
                var messageDialog = new MessageDialog(App.loader.GetString("unsavedChanges"), App.loader.GetString("reallyWant"));

                // Add commands and set their command ids
                messageDialog.Commands.Add(new UICommand(App.loader.GetString("Yes"), null, 1));
                messageDialog.Commands.Add(new UICommand(App.loader.GetString("stayHere"), null, 2));

                // Set the command that will be invoked by default
                messageDialog.DefaultCommandIndex = 2;

                // Show the message dialog and get the event that was invoked via the async operator
                var commandChosen = await messageDialog.ShowAsync();

                if (commandChosen.Id.ToString() == "1")
                {
                    Windows.Storage.ApplicationDataContainer localSettings =
                Windows.Storage.ApplicationData.Current.LocalSettings;
                    localSettings.Values.Remove("postTitle");
                    localSettings.Values.Remove("richEditBox");
                    Frame.GoBack();
                }
                else return;
            }
            else Frame.GoBack();
        }

        private async void richEditBox_TextChanged(object sender, RoutedEventArgs e)
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;string getter;
            richEditBox.Document.GetText(TextGetOptions.FormatRtf, out getter);
            //localSettings.Values["richEditBox"] = getter;
            try
            {
                StorageFile sampleFile = await localFolder.CreateFileAsync("rich.tmp", CreationCollisionOption.GenerateUniqueName);
                await FileIO.WriteTextAsync(sampleFile, getter);
                localSettings.Values["richEditBox"] = sampleFile.Name;
            }
            catch
            {
            }
        }
    }
}
