// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// Copyright (c) Microsoft Corporation. All rights reserved

using Blogg;

using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Navigation;
using Windows.System;
using System.Collections.Generic;
using Blogg.Common;
using Windows.UI.ApplicationSettings;

namespace Blogg
{
    partial class ExtendedSplash
    {
        internal Rect splashImageRect; // Rect to store splash screen image coordinates.
        internal bool dismissed = false; // Variable to track splash screen dismissal status.
        internal Frame rootFrame;


        private SplashScreen splash; // Variable to hold the splash screen object.

        public ExtendedSplash(SplashScreen splashscreen)
        {
            InitializeComponent();

            if (!App.isEventRegistered)
            {
                SettingsPane.GetForCurrentView().CommandsRequested += App.onCommandsRequested;
                App.isEventRegistered = true;
            }

            // Listen for window resize events to reposition the extended splash screen image accordingly.
            // This is important to ensure that the extended splash screen is formatted properly in response to snapping, unsnapping, rotation, etc...
            Window.Current.SizeChanged += new WindowSizeChangedEventHandler(ExtendedSplash_OnResize);

            splash = splashscreen;

            if (splash != null)
            {
                // Register an event handler to be executed when the splash screen has been dismissed.
                splash.Dismissed += new TypedEventHandler<SplashScreen, Object>(DismissedEventHandler);

                // Retrieve the window coordinates of the splash screen image.
                splashImageRect = splash.ImageLocation;
                PositionImage();
            }

            // Create a Frame to act as the navigation context 
            rootFrame = new Frame();

            // Restore the saved session state if necessary
            //RestoreStateAsync(loadState);

        }

        async void RestoreStateAsync(bool loadState)
        {
            if (loadState)
                await SuspensionManager.RestoreAsync();

            // Normally you should start the time consuming task asynchronously here and 
            // dismiss the extended splash screen in the completed handler of that task
            // This sample dismisses extended splash screen  in the handler for "Learn More" button for demonstration
        }

        // Position the extended splash screen image in the same location as the system splash screen image.
        void PositionImage()
        {
            extendedSplashImage.SetValue(Canvas.LeftProperty, splashImageRect.X);
            extendedSplashImage.SetValue(Canvas.TopProperty, splashImageRect.Y);
            extendedSplashImage.Height = splashImageRect.Height;
            extendedSplashImage.Width = splashImageRect.Width;
        }

        void ExtendedSplash_OnResize(Object sender, WindowSizeChangedEventArgs e)
        {
            // Safely update the extended splash screen image coordinates. This function will be fired in response to snapping, unsnapping, rotation, etc...
            if (splash != null)
            {
                // Update the coordinates of the splash screen image.
                splashImageRect = splash.ImageLocation;
                PositionImage();
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Windows.UI.Xaml.Media.Animation.Storyboard sb =
            this.FindName("PopInStoryboard") as Windows.UI.Xaml.Media.Animation.Storyboard;
            if (sb != null) sb.Begin();
            // Retrieve splash screen object
            splash = (SplashScreen)(e.Parameter);
            if (splash != null)
            {
                // Register an event handler to be executed when the splash screen has been dismissed.
                splash.Dismissed += new TypedEventHandler<SplashScreen, object>(DismissedEventHandler);

                // Retrieve the window coordinates of the splash screen image.
                splashImageRect = splash.ImageLocation;
                PositionImage();
            }
        }

        async void registerClick(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("http://www.blogger.com/", UriKind.Absolute));
        }

        void signinClick(object sender, RoutedEventArgs e)
        {
            // Navigate away from the app's extended splash screen.
            rootFrame.Navigate(typeof(GroupedItemsPage), this);
            Window.Current.Content = rootFrame;
        }

        // Include code to be executed when the system has transitioned from the splash screen to the extended splash screen (application's first view).
        void DismissedEventHandler(SplashScreen sender, object e)
        {
            dismissed = true;

            // Navigate away from the app's extended splash screen after completing setup operations here...
            // This sample navigates away from the extended splash screen when the "Learn More" button is clicked.
        }
    }
}
