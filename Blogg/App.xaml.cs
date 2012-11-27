using Blogg.Common;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Net;
using Blogg.Data;
using Windows.UI.ApplicationSettings;
using Windows.Storage;
using Windows.ApplicationModel.Search;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Popups;
using Windows.System;

// The Grid App template is documented at http://go.microsoft.com/fwlink/?LinkId=234226

namespace Blogg
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton Application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public static BlogsDataSource dataSource;
        public static bool isEventRegistered;
        public static SplashScreen splashScreen;
        public static Windows.ApplicationModel.Resources.ResourceLoader loader;
        SearchPane searchPane;
        public static DataTransferManager dataTransferManager;

        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
            dataSource = new BlogsDataSource();
            //blogData = new BlogData();
            loader = new Windows.ApplicationModel.Resources.ResourceLoader();
        }

        public static void onCommandsRequested(SettingsPane settingsPane, SettingsPaneCommandsRequestedEventArgs eventArgs)
        {
            UICommandInvokedHandler signOutHandler = new UICommandInvokedHandler(onSettingsCommand);
            UICommandInvokedHandler bloggerHandler = new UICommandInvokedHandler(onBloggerCommand);
            UICommandInvokedHandler privacyHandler = new UICommandInvokedHandler(onPrivacyCommand);

            if ((string)ApplicationData.Current.LocalSettings.Values["refresh_token"] != null)
            {
                SettingsCommand generalCommand = new SettingsCommand("signOut", App.loader.GetString("signOut"), signOutHandler);
                eventArgs.Request.ApplicationCommands.Add(generalCommand);
            }

            SettingsCommand helpCommand = new SettingsCommand("urlPage", App.loader.GetString("urlPage"), bloggerHandler);
            eventArgs.Request.ApplicationCommands.Add(helpCommand);

            SettingsCommand privacyCommand = new SettingsCommand("privacyPolicy", App.loader.GetString("privacyPolicy"), privacyHandler);
            eventArgs.Request.ApplicationCommands.Add(privacyCommand);

            //http://www.google.com/policies/privacy/
        }

        private static void onSettingsCommand(IUICommand command)
        {
            SettingsCommand settingsCommand = (SettingsCommand)command;
            ApplicationData.Current.LocalSettings.Values["refresh_token"] = null;
            App.dataSource.oauth2.access_token = null;
            App.dataSource.blogs.Clear();
            ExtendedSplash splash = new ExtendedSplash(App.splashScreen);
            Window.Current.Content = splash;
            //rootFrame.
            //Window.Current.Content = rootFrame;
            //rootPage.NotifyUser(
            //    "You selected the " + settingsCommand.Label + " settings command",
            //    NotifyType.StatusMessage);
        }

        private static async void onBloggerCommand(IUICommand command)
        {
            await Launcher.LaunchUriAsync(new Uri("http://www.blogger.com/", UriKind.Absolute));
        }

        private static async void onPrivacyCommand(IUICommand command)
        {
            await Launcher.LaunchUriAsync(new Uri("http://alllaboutnothing.blogspot.com/p/blogg-app-privacy-policy.html", UriKind.Absolute));
        }

        public static string extractBlogID(string blogid, int index, char znak)
        {
            string[] words = blogid.Split(znak);
            return words[index];
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used when the application is launched to open a specific file, to display
        /// search results, and so forth.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            // Do not repeat app initialization when already running, just ensure that
            // the window is active
            if (args.PreviousExecutionState == ApplicationExecutionState.Running)
            {
                Window.Current.Activate();
                return;
            }

            // SearchPane
            
            this.searchPane = SearchPane.GetForCurrentView();

            // Suggestion list
            //suggestionList = DataService.GetSearchSuggestions().ToArray();

            searchPane.SuggestionsRequested +=
                new TypedEventHandler<SearchPane, SearchPaneSuggestionsRequestedEventArgs>
                (OnSearchPaneSuggestionsRequested);
            
            // Create a Frame to act as the navigation context and associate it with
            // a SuspensionManager key
            var rootFrame = new Frame();
            SuspensionManager.RegisterFrame(rootFrame, "AppFrame");

            if (args.PreviousExecutionState == ApplicationExecutionState.Terminated)
            {
                // Restore the saved session state only when appropriate
                await SuspensionManager.RestoreAsync();
            }
            rootFrame.Loaded += OnRootFrameLoaded;

            bool loadState = (args.PreviousExecutionState == ApplicationExecutionState.Terminated);
            ExtendedSplash extendedSplash = new ExtendedSplash(args.SplashScreen);

            if (rootFrame.Content == null)
            {
                splashScreen = args.SplashScreen;
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                if ((string)ApplicationData.Current.LocalSettings.Values["refresh_token"] != null)
                {
                    if (!rootFrame.Navigate(typeof(GroupedItemsPage), "blogs"))
                    {
                        throw new Exception("Failed to create initial page");
                    }
                    Window.Current.Content = rootFrame;
                }
                else
                {
                    //if (!rootFrame.Navigate(typeof(ExtendedSplash), splashScreen))
                    //{
                    //    throw new Exception("Failed to create initial page");
                    //}
                    if (args.PreviousExecutionState != ApplicationExecutionState.Running)
                    {
                        Window.Current.Content = extendedSplash;
                    }
                }
            }

            // Place the frame in the current Window and ensure that it is active
            //Window.Current.Content = rootFrame;
            Window.Current.Activate();
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            await SuspensionManager.SaveAsync();
            deferral.Complete();
        }

        /// <summary>
        /// Wywoływane, gdy aplikacja zostaje aktywowana w celu wyświetlenia wyników wyszukiwania.
        /// </summary>
        /// <param name="args">Szczegóły żądania aktywacji.</param>
        protected async override void OnSearchActivated(Windows.ApplicationModel.Activation.SearchActivatedEventArgs args)
        {
            // TODO: Zarejestruj zdarzenie Windows.ApplicationModel.Search.SearchPane.GetForCurrentView().QuerySubmitted
            // w metodzie OnWindowCreated w celu przyspieszenia wyszukiwania, gdy aplikacja będzie już uruchomiona

            // Jeśli w oknie nie została jeszcze zastosowana nawigacja w ramce, wstaw własną ramkę
            var previousContent = Window.Current.Content;
            var frame = previousContent as Frame;

            // Jeśli aplikacja nie zawiera ramki najwyższego poziomu, może to oznaczać, 
            // że jest to początkowe uruchomienie aplikacji. Zazwyczaj metoda ta oraz metoda OnLaunched 
            // w pliku App.xaml.cs mogą wywoływać wspólną metodę.
            if (frame == null)
            {
                // Utwórz ramkę, która będzie pełnić funkcję kontekstu nawigacji, i skojarz ją z kluczem 
                // SuspensionManager
                frame = new Frame();
                Blogg.Common.SuspensionManager.RegisterFrame(frame, "AppFrame");

                if (args.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    // Przywracaj zapisany stan sesji tylko wtedy, gdy jest to potrzebne
                    try
                    {
                        await Blogg.Common.SuspensionManager.RestoreAsync();
                    }
                    catch
                    {
                        //Wystąpił błąd podczas przywracania stanu.
                        //Przyjmij, że stan jest niedostępny, i kontynuuj
                    }
                }
            }
            if (App.dataSource.blogs.Count == 0) App.dataSource.getBlogsList();
            if (args.QueryText != "")
            {
                frame.Navigate(typeof(SearchResultsPage), args.QueryText);
                Window.Current.Content = frame;
                Window.Current.Activate();
            }         
        }

        private void OnSearchPaneSuggestionsRequested(SearchPane sender, SearchPaneSuggestionsRequestedEventArgs e)
        {
            var queryText = e.QueryText;
            if (!string.IsNullOrEmpty(queryText))
            {
                var request = e.Request;

                var suggestions = (from gr in App.dataSource.blogs
                                   from i in gr.Items
                                   //where i.name.Contains(queryText)
                                   select i.name);
                foreach (string suggestion in suggestions)
                {
                    if (suggestion.StartsWith(queryText, StringComparison.CurrentCultureIgnoreCase))
                    {
                        // Add suggestion to Search Pane
                        request.SearchSuggestionCollection.AppendQuerySuggestion(suggestion);

                        // Break since the Search Pane can show at most 5 suggestions
                        if (request.SearchSuggestionCollection.Size >= 5)
                        {
                            break;
                        }
                    }
                }
            }
        }

        private void OnRootFrameLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            dataTransferManager = DataTransferManager.GetForCurrentView(); 
        }
    }
}
