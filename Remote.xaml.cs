using Microsoft.UI.Xaml;
using System;
using Windows.Media.Capture;
using System.Diagnostics;
using Microsoft.UI.Windowing;
using System.Linq;
using Microsoft.UI.Dispatching;
using Microsoft.UI;

namespace Control3
{
    public sealed partial class Remote : Window
    {
        private MediaCapture mediaCapture;
        public bool IsClosed { get; private set; }
        public Remote()
        {
            this.AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(280, 40, 1280, 774));
            this.AppWindow.SetIcon(@"Assets\control.ico");

            this.InitializeComponent();
            SetFullScreen(App.Flag.isFullScreen);
            this.IsClosed = false;

            this.Closed += OnWindowClosed;
            this.Activated += OnWindowActivated;

            StartCaptureElement();

            MainWindow.mouse.GetCurrentState();
        }
        async private void StartCaptureElement()
        {
            mediaCapture = new MediaCapture();
            var settings = new MediaCaptureInitializationSettings { VideoDeviceId = App.Flag.selectedSource };

            await mediaCapture.InitializeAsync(settings);

            // var vidStream = mediaCapture.FrameSources.FirstOrDefault().Value; 
            // Update: select Video Stream to prevent accidental usage of the audio stream
            var vidStream = mediaCapture.FrameSources
                                        .FirstOrDefault(source => source.Value.Info.MediaStreamType == MediaStreamType.VideoPreview ||
                                                                  source.Value.Info.MediaStreamType == MediaStreamType.VideoRecord).Value;
            
            captureElement.Source = Windows.Media.Core.MediaSource.CreateFromMediaFrameSource(vidStream);

            var desiredFormat = vidStream.SupportedFormats.FirstOrDefault(format => format.Subtype == "MJPG" && format.VideoFormat.Width == 1920 && format.FrameRate.Numerator == 30);
            if (desiredFormat != null)
            {
                await vidStream.SetFormatAsync(desiredFormat);
                Debug.WriteLine($"Format set: {desiredFormat.Subtype} {desiredFormat.FrameRate.Numerator} {desiredFormat.VideoFormat.Width}");
            }
        }
        public void OnWindowClosed(object sender, WindowEventArgs e)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (mediaCapture != null)
                {
                    mediaCapture.Dispose();
                    mediaCapture = null;
                }
                App.Flag.isRemote = false;
                this.IsClosed = true;
                MainWindow.SetMessage("", Colors.Blue);
            });
        }
        private void captureClicked(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (e.GetCurrentPoint(sender as UIElement).Properties.PointerUpdateKind == Microsoft.UI.Input.PointerUpdateKind.LeftButtonPressed)
            {
                MainWindow.mouse.GetCurrentState();
                App.Flag.Decoration = 0;
                App.Flag.isRemote = true;
                MainWindow.SetMessage("Remote Session Active", Colors.Blue);
            }
        }
        private void OnWindowActivated(object sender, Microsoft.UI.Xaml.WindowActivatedEventArgs e)
        {
            if (e.WindowActivationState == WindowActivationState.CodeActivated)
            {
                MainWindow.mouse.GetCurrentState();
                App.Flag.Decoration = 0;
                App.Flag.isRemote = true;
                MainWindow.SetMessage("Remote Session Active", Colors.Blue);
            }
        }
        public void SetFullScreen(bool fullScreen)
        {
            if (!fullScreen)
            {
                this.AppWindow.SetPresenter(AppWindowPresenterKind.Default);
                ImageBorder.BorderThickness = new Thickness(5);
                App.Flag.isFullScreen = false;
            }
            else
            {
                this.AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
                ImageBorder.BorderThickness = new Thickness(0);
                App.Flag.isFullScreen = true;
            }
        }
    }
}
