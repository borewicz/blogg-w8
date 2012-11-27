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
using Windows.UI.ApplicationSettings;
using Windows.UI.Popups;
using Windows.Storage;
using Windows.System;
using Windows.ApplicationModel.Activation;

// The Grouped Items Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234231

namespace Blogg
{
    /// <summary>
    /// A page that displays a grouped collection of items.
    /// </summary>
    public sealed partial class GroupedItemsPage : Blogg.Common.LayoutAwarePage
    {
        internal Frame rootFrame;
        public GroupedItemsPage()
        {
            rootFrame = new Frame();
            this.InitializeComponent();
            this.DataContext = App.dataSource;
            if (!App.isEventRegistered)
            {
                SettingsPane.GetForCurrentView().CommandsRequested += App.onCommandsRequested;
                App.isEventRegistered = true;
            }
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
        protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            if (App.dataSource.blogs.Count == 0) App.dataSource.getBlogsList();
            //var sampleDataGroups = SampleDataSource.GetGroups((String)navigationParameter);
            this.DefaultViewModel["Groups"] = App.dataSource.blogs;
            (this.semanticZoom.ZoomedOutView as GridView).ItemsSource = App.dataSource.blogs;
        }

        /// <summary>
        /// Invoked when a group header is clicked.
        /// </summary>
        /// <param name="sender">The Button used as a group header for the selected group.</param>
        /// <param name="e">Event data that describes how the click was initiated.</param>
        void Header_Click(object sender, RoutedEventArgs e)
        {
            // Determine what group the Button instance represents
            var group = (sender as FrameworkElement).DataContext;

            // Navigate to the appropriate destination page, configuring the new page
            // by passing required information as a navigation parameter
            this.Frame.Navigate(typeof(GroupDetailPage), ((BlogItem)group).id);
        }

        public static Rect GetElementRect(FrameworkElement element)
        {
            GeneralTransform buttonTransform = element.TransformToVisual(null);
            Point point = buttonTransform.TransformPoint(new Point());
            return new Rect(point, new Size(element.ActualWidth, element.ActualHeight));
        }

        async void newPost_click(object sender, RoutedEventArgs e)
        {
            var menu = new PopupMenu();
            if (App.dataSource.blogs.Count != 0)
            {           
                if (App.dataSource.blogs.Count == 1)
                {
                    this.Frame.Navigate(typeof(newPostPage), App.dataSource.blogs[0].id);
                }
                else
                {
                    foreach (var blog in App.dataSource.blogs)
                    {
                        menu.Commands.Add(new UICommand(blog.name, (command) =>
                        {
                            this.Frame.Navigate(typeof(newPostPage), blog.id);
                        }));
                    }
                    var chosenCommand = await menu.ShowForSelectionAsync(GetElementRect((FrameworkElement)sender)); 
                }
            }
        }

        /// <summary>
        /// Invoked when an item within a group is clicked.
        /// </summary>
        /// <param name="sender">The GridView (or ListView when the application is snapped)
        /// displaying the item clicked.</param>
        /// <param name="e">Event data that describes the item clicked.</param>
        void ItemView_ItemClick(object sender, ItemClickEventArgs e)
        {
            // Navigate to the appropriate destination page, configuring the new page
            // by passing required information as a navigation parameter
            var itemId = ((PostItem)e.ClickedItem).id;
            this.Frame.Navigate(typeof(ItemDetailPage), itemId);
        }

        void refreshClick(object sender, RoutedEventArgs e)
        {
            App.dataSource.blogs.Clear();
            App.dataSource.getBlogsList();
        }
    }
}
