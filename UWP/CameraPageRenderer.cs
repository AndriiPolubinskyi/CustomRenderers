﻿using CustomRenderer.UWP;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Devices.Enumeration;
using Windows.Devices.Sensors;
using Windows.Foundation.Metadata;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Phone.UI.Input;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.System.Display;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Xamarin.Forms.Platform.UWP;
using Windows.Foundation;
using Windows.UI;
using System.Runtime.InteropServices.WindowsRuntime;
using CustomRenderer.Views;

[assembly: ExportRenderer(typeof(CameraPageView), typeof(CameraPageRenderer))]
namespace CustomRenderer.UWP
{
    public class CameraPageRenderer : PageRenderer
    {
        readonly DisplayInformation displayInformation = DisplayInformation.GetForCurrentView();
        readonly SimpleOrientationSensor orientationSensor = SimpleOrientationSensor.GetDefault();
        readonly DisplayRequest displayRequest = new DisplayRequest();
        SimpleOrientation deviceOrientation = SimpleOrientation.NotRotated;
        DisplayOrientations displayOrientation = DisplayOrientations.Portrait;

        // Rotation metadata to apply to preview stream (https://msdn.microsoft.com/en-us/library/windows/apps/xaml/hh868174.aspx)
        static readonly Guid RotationKey = new Guid("C380465D-2271-428C-9B83-ECEA3B4A85C1"); // (MF_MT_VIDEO_ROTATION)

        StorageFolder captureFolder = null;

        readonly SystemMediaTransportControls systemMediaControls = SystemMediaTransportControls.GetForCurrentView();

        MediaCapture mediaCapture;
        CaptureElement captureElement;
        bool isInitialized;
        bool isPreviewing;
        bool externalCamera;
        bool mirroringPreview;
        
        Page page; 
        Application app;


        protected override void OnElementChanged(ElementChangedEventArgs<Xamarin.Forms.Page> e)
        {
            base.OnElementChanged(e);

            if (e.OldElement != null || Element == null)
            {
                return;
            }

            try
            {
                app = Application.Current;
                app.Suspending += OnAppSuspending;
                app.Resuming += OnAppResuming;

                SetupUserInterface();
                //SetupCamera();

                this.Children.Add(page);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(@"      ERROR: ", ex.Message);
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            page.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
            return finalSize;
        }

        void SetupUserInterface()
        {

            var tagButton = new AppBarButton
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Icon = new SymbolIcon(Symbol.Tag)
            };

            var approvePhotoButton = new AppBarButton
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Icon = new SymbolIcon(Symbol.Accept)
            };

            var cancelPhotoButton = new AppBarButton
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Icon = new SymbolIcon(Symbol.Cancel)
            };

            var commandBar = new CommandBar();
          
            commandBar.PrimaryCommands.Add(approvePhotoButton);
            commandBar.PrimaryCommands.Add(tagButton);
            commandBar.PrimaryCommands.Add(cancelPhotoButton);

            captureElement = new CaptureElement();
            
            captureElement.Stretch = Stretch.UniformToFill;


            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(140) });
            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

            Grid.SetColumnSpan(captureElement, 2);
            Grid.SetRowSpan(captureElement, 2);
            Grid.SetColumn(captureElement, 0);
            Grid.SetRow(captureElement, 0);
            grid.Children.Add(captureElement);


            var button = new Button
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                Content = "Photo",
                Margin = new Thickness(12),
                Background = new SolidColorBrush(Colors.Blue)
            };

            button.Click += Button_Click;
            Grid.SetColumnSpan(button, 2);
            Grid.SetRowSpan(button, 2);
            Grid.SetColumn(button, 0);
            Grid.SetRow(button, 0);
            grid.Children.Add(button);


            page = new Page();
            page.BottomAppBar = commandBar;
            page.Content = grid;
            page.Loaded += Page_Loaded;
            page.Unloaded += OnPageUnloaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
           
            SetupCamera();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await TakePhotoAsync();

        }

        async void SetupCamera()
        {
            await SetupUIAsync();
            await InitializeCameraAsync();
        }

        #region Event Handlers

        async void OnSystemMediaControlsPropertyChanged(SystemMediaTransportControls sender, SystemMediaTransportControlsPropertyChangedEventArgs args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                if (page.Frame != null)
                {
                    // Only handle event if the page is being displayed
                    if (args.Property == SystemMediaTransportControlsProperty.SoundLevel && page.Frame.CurrentSourcePageType == typeof(MainPage))
                    {
                        // Check if the app is being muted. If so, it's being minimized
                        // Otherwise if it is not initialized, it's being brought into focus
                        if (sender.SoundLevel == SoundLevel.Muted)
                        {
                            await CleanupCameraAsync();
                        }
                        else if (!isInitialized)
                        {
                            await InitializeCameraAsync();
                        }
                    }
                }
            });
        }

        void OnOrientationSensorOrientationChanged(SimpleOrientationSensor sender, SimpleOrientationSensorOrientationChangedEventArgs args)
        {
            // Only update orientatino if the device is not parallel to the ground
            if (args.Orientation != SimpleOrientation.Faceup && args.Orientation != SimpleOrientation.Facedown)
            {
                deviceOrientation = args.Orientation;
            }
        }

        async void OnDisplayInformationOrientationChanged(DisplayInformation sender, object args)
        {
            displayOrientation = sender.CurrentOrientation;

            if (isPreviewing)
            {
                await SetPreviewRotationAsync();
            }
        }

        async void OnTakePhotoButtonClicked(object sender, RoutedEventArgs e)
        {
            await TakePhotoAsync();
        }

        async void OnHardwareCameraButtonPressed(object sender, CameraEventArgs e)
        {
            await TakePhotoAsync();
        }

        #endregion

        #region Media Capture

        async Task InitializeCameraAsync()
        {
            if (mediaCapture == null)
            {
                var devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
                var cameraDevice = devices.FirstOrDefault(c => c.EnclosureLocation != null && c.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Back);
                // Get any camera if there isn't one on the back panel
                cameraDevice = cameraDevice ?? devices.FirstOrDefault();

                if (cameraDevice == null)
                {
                    Debug.WriteLine("No camera found");
                    return;
                }

                mediaCapture = new MediaCapture();
              

                var mediaCaptureInitSettings = this.CreateInitializationSettings(cameraDevice.Id);
                await mediaCapture.InitializeAsync(mediaCaptureInitSettings);

                try
                {

                    // Prevent the device from sleeping while the preview is running
                    displayRequest.RequestActive();

                    // Setup preview source in UI and mirror if required
                    captureElement.Source = mediaCapture;
                    captureElement.FlowDirection = mirroringPreview ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

                    // Start preview
                    await mediaCapture.StartPreviewAsync();

                    isInitialized = true;
                }

                catch (UnauthorizedAccessException)
                {
                    Debug.WriteLine("Camera access denied");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Exception initializing MediaCapture - {0}: {1}", cameraDevice.Id, ex.ToString());
                }

                if (isInitialized)
                {
                    if (cameraDevice.EnclosureLocation == null || cameraDevice.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Unknown)
                    {
                        externalCamera = true;
                    }
                    else
                    {
                        // Camera is on device
                        externalCamera = false;

                        // Mirror preview if camera is on front panel
                        mirroringPreview = (cameraDevice.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Front);
                    }
                    await StartPreviewAsync();
                }
            }
        }

        private async Task<MediaCapture> CreateCameraAsync(string cameraId)
        {
            var mediaCapture = new MediaCapture();
           // mediaCapture.Failed += this.OnCaptureFailed;

            var mediaCaptureInitSettings = this.CreateInitializationSettings(cameraId);
            await mediaCapture.InitializeAsync(mediaCaptureInitSettings);

            var focusControl = mediaCapture.VideoDeviceController.FocusControl;

            if (focusControl.Supported)
            {
                await focusControl.SetPresetAsync(Windows.Media.Devices.FocusPreset.Manual);
            }

            var flashControl = mediaCapture.VideoDeviceController.FlashControl;
            if (flashControl.Supported)
            {
                flashControl.Auto = true;
                flashControl.Enabled = true;
            }

            // Setup default capture options
            var _captureRotation = VideoRotation.None;
            mediaCapture.SetPreviewRotation(_captureRotation);

            // Use maximum video preview resolution
            var maximumVideoPreviewResolution = this.GetMaximumResolution(mediaCapture, MediaStreamType.VideoPreview);
            await mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoPreview, maximumVideoPreviewResolution);

            // Use maximum resolution for capturing
            var maximumCaptureResolution = this.GetMaximumResolution(mediaCapture, MediaStreamType.Photo);
            await mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.Photo, maximumCaptureResolution);

            return mediaCapture;
        }

        private IMediaEncodingProperties GetMaximumResolution(MediaCapture mediaCapture, MediaStreamType streamType)
        {
            VideoEncodingProperties maxResolution = null;

            var resolutions = mediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(streamType);

            foreach (var resolution in resolutions)
            {
                if (resolution is VideoEncodingProperties)
                {
                    var imageProperty = (VideoEncodingProperties)resolution;

                    var aspectRatio = Convert.ToDouble(imageProperty.Width) / Convert.ToDouble(imageProperty.Height);
                        if (maxResolution == null || imageProperty.Width > maxResolution.Width)
                        {
                            maxResolution = imageProperty;
                        }
                    }
            }


            return maxResolution;
        }
        private MediaCaptureInitializationSettings CreateInitializationSettings(string cameraId)
        {
            return new MediaCaptureInitializationSettings
            {
                VideoDeviceId = cameraId
            };
        }


        async Task StartPreviewAsync()
        {
            isPreviewing = true;

            if (isPreviewing)
            {
                await SetPreviewRotationAsync();
            }
        }

        async Task StopPreviewAsync()
        {
            isPreviewing = false;
            await mediaCapture.StopPreviewAsync();

            // Use dispatcher because sometimes this method is called from non-UI threads
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                // UI cleanup
                captureElement.Source = null;

                // Allow device screen to sleep now preview is stopped
                displayRequest.RequestRelease();
            });
        }

        async Task SetPreviewRotationAsync()
        {
            // Only update the orientation if the camera is mounted on the device
            if (externalCamera)
            {
                return;
            }

            // Derive the preview rotation
            int rotation = ConvertDisplayOrientationToDegrees(displayOrientation);

            // Invert if mirroring
            if (mirroringPreview)
            {
                rotation = (360 - rotation) % 360;
            }

            // Add rotation metadata to preview stream
            var props = mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview);
            props.Properties.Add(RotationKey, rotation);
            await mediaCapture.SetEncodingPropertiesAsync(MediaStreamType.VideoPreview, props, null);
        }

        async Task TakePhotoAsync()
        {
            
            var stream = new InMemoryRandomAccessStream();

            await mediaCapture.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateJpeg(), stream);
            await StopPreviewAsync();

            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
            SoftwareBitmap softwareBitmap = await decoder.GetSoftwareBitmapAsync();
            

            var data = await EncodedBytes(softwareBitmap, BitmapEncoder.JpegEncoderId);

            (Element as CameraPageView).SetPhotoResult(data.ToArray(), (int)decoder.PixelWidth, (int)decoder.PixelHeight);

        }

        private async Task<byte[]> EncodedBytes(SoftwareBitmap soft, Guid encoderId)
        {
            byte[] array = null;

            // First: Use an encoder to copy from SoftwareBitmap to an in-mem stream (FlushAsync)
            // Next:  Use ReadAsync on the in-mem stream to get byte[] array

            using (var ms = new InMemoryRandomAccessStream())
            {
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(encoderId, ms);
                encoder.SetSoftwareBitmap(soft);

                try
                {
                    await encoder.FlushAsync();
                }
                catch (Exception ex) { return new byte[0]; }

                array = new byte[ms.Size];
                await ms.ReadAsync(array.AsBuffer(), (uint)ms.Size, InputStreamOptions.None);
            }
            return array;
        }

        private async Task<IRandomAccessStream> TransformImageAsync(BitmapDecoder decoder, VideoRotation captureRotation)
        {
            var inputImageBytes = (await decoder.GetPixelDataAsync()).DetachPixelData();

            var destinationMemoryStream = new InMemoryRandomAccessStream();

            // set image scaling transformation
            
            var bitmapTransform = new BitmapTransform()
            {
                ScaledWidth = (uint)decoder.PixelWidth,
                ScaledHeight = (uint)decoder.PixelHeight,
                InterpolationMode = BitmapInterpolationMode.Cubic
            };

            // get scaled image data
            var pixels = (await decoder.GetPixelDataAsync(
                                            decoder.BitmapPixelFormat,
                                            decoder.BitmapAlphaMode,
                                            bitmapTransform,
                                            ExifOrientationMode.RespectExifOrientation,
                                            ColorManagementMode.DoNotColorManage)).DetachPixelData();

            // set image quality
            var imagePropertySet = new BitmapPropertySet();
            var imageQualityProperty = new BitmapTypedValue(1000, PropertyType.Single);
            imagePropertySet.Add("ImageQuality", imageQualityProperty);

            var encoder = await BitmapEncoder.CreateAsync(
                                                BitmapEncoder.JpegEncoderId,
                                                destinationMemoryStream,
                                                imagePropertySet);

            // set image rotation transformation
            encoder.BitmapTransform.Rotation = BitmapRotation.None;

            // encode image data with the specified scaling, rotation and quality
            encoder.SetPixelData(
                        decoder.BitmapPixelFormat,
                        decoder.BitmapAlphaMode,
                        bitmapTransform.ScaledWidth,
                        bitmapTransform.ScaledHeight,
                        decoder.DpiX,
                        decoder.DpiY,
                        pixels);

            await encoder.FlushAsync();

            return destinationMemoryStream;
        }

        async Task CleanupCameraAsync()
        {
            if (isInitialized)
            {
                if (isPreviewing)
                {
                    await StopPreviewAsync();
                }
                isInitialized = false;
            }
            if (mediaCapture != null)
            {
                mediaCapture.Dispose();
                mediaCapture = null;
            }
        }

        #endregion

        #region Helpers

        async Task SetupUIAsync()
        {
            // Lock page to landscape to prevent the capture element from rotating
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;

            // Hide status bar
            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
            {
                await Windows.UI.ViewManagement.StatusBar.GetForCurrentView().HideAsync();
            }

            displayOrientation = displayInformation.CurrentOrientation;
            if (orientationSensor != null)
            {
                deviceOrientation = orientationSensor.GetCurrentOrientation();
            }

            RegisterEventHandlers();

          
        }

        async Task CleanupUIAsync()
        {
            UnregisterEventHandlers();

            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
            {
                await Windows.UI.ViewManagement.StatusBar.GetForCurrentView().ShowAsync();
            }

            // Revert orientation preferences
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.None;
        }

        void RegisterEventHandlers()
        {
            if (ApiInformation.IsTypePresent("Windows.Phone.UI.Input.HardwareButtons"))
            {
                HardwareButtons.CameraPressed += OnHardwareCameraButtonPressed;
            }

            if (orientationSensor != null)
            {
                orientationSensor.OrientationChanged += OnOrientationSensorOrientationChanged;
            }

            displayInformation.OrientationChanged += OnDisplayInformationOrientationChanged;
            systemMediaControls.PropertyChanged += OnSystemMediaControlsPropertyChanged;
            //takePhotoButton.Click += OnTakePhotoButtonClicked;
        }
        
        void UnregisterEventHandlers()
        {
            if (ApiInformation.IsTypePresent("Windows.Phone.UI.Input.HardwareButtons"))
            {
                HardwareButtons.CameraPressed -= OnHardwareCameraButtonPressed;
            }

            if (orientationSensor != null)
            {
                orientationSensor.OrientationChanged -= OnOrientationSensorOrientationChanged;
            }

            displayInformation.OrientationChanged -= OnDisplayInformationOrientationChanged;
            systemMediaControls.PropertyChanged -= OnSystemMediaControlsPropertyChanged;
           // takePhotoButton.Click -= OnTakePhotoButtonClicked;
        }

        static async Task ReencodeAndSavePhotoAsync(IRandomAccessStream stream, StorageFile file, PhotoOrientation orientation)
        {
            using (var inputStream = stream)
            {
                var decoder = await BitmapDecoder.CreateAsync(inputStream);

                using (var outputStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var encoder = await BitmapEncoder.CreateForTranscodingAsync(outputStream, decoder);
                    var properties = new BitmapPropertySet
                    {
                        {
                            "System.Photo.Orientation", new BitmapTypedValue(orientation, Windows.Foundation.PropertyType.UInt16)
                        }
                    };

                    await encoder.BitmapProperties.SetPropertiesAsync(properties);
                    await encoder.FlushAsync();
                }
            }
        }

        #endregion

        #region Rotation

        SimpleOrientation GetCameraOrientation()
        {
            if (externalCamera)
            {
                // Cameras that aren't attached to the device do not rotate along with it
                return SimpleOrientation.NotRotated;
            }

            var result = deviceOrientation;

            // On portrait-first devices, the camera sensor is mounted at a 90 degree offset to the native orientation
            if (displayInformation.NativeOrientation == DisplayOrientations.Portrait)
            {
                switch (result)
                {
                    case SimpleOrientation.Rotated90DegreesCounterclockwise:
                        result = SimpleOrientation.NotRotated;
                        break;
                    case SimpleOrientation.Rotated180DegreesCounterclockwise:
                        result = SimpleOrientation.Rotated90DegreesCounterclockwise;
                        break;
                    case SimpleOrientation.Rotated270DegreesCounterclockwise:
                        result = SimpleOrientation.Rotated180DegreesCounterclockwise;
                        break;
                    case SimpleOrientation.NotRotated:
                        result = SimpleOrientation.Rotated270DegreesCounterclockwise;
                        break;
                }
            }

            // If the preview is mirrored for a front-facing camera, invert the rotation
            if (mirroringPreview)
            {
                // Rotating 0 and 180 ddegrees is the same clockwise and anti-clockwise
                switch (result)
                {
                    case SimpleOrientation.Rotated90DegreesCounterclockwise:
                        return SimpleOrientation.Rotated270DegreesCounterclockwise;
                    case SimpleOrientation.Rotated270DegreesCounterclockwise:
                        return SimpleOrientation.Rotated90DegreesCounterclockwise;
                }
            }

            return result;
        }

        static int ConvertDeviceOrientationToDegrees(SimpleOrientation orientation)
        {
            switch (orientation)
            {
                case SimpleOrientation.Rotated90DegreesCounterclockwise:
                    return 90;
                case SimpleOrientation.Rotated180DegreesCounterclockwise:
                    return 180;
                case SimpleOrientation.Rotated270DegreesCounterclockwise:
                    return 270;
                case SimpleOrientation.NotRotated:
                default:
                    return 0;
            }
        }

        static int ConvertDisplayOrientationToDegrees(DisplayOrientations orientation)
        {
            switch (orientation)
            {
                case DisplayOrientations.Portrait:
                    return 90;
                case DisplayOrientations.LandscapeFlipped:
                    return 180;
                case DisplayOrientations.PortraitFlipped:
                    return 270;
                case DisplayOrientations.Landscape:
                default:
                    return 0;
            }
        }

        static PhotoOrientation ConvertOrientationToPhotoOrientation(SimpleOrientation orientation)
        {
            switch (orientation)
            {
                case SimpleOrientation.Rotated90DegreesCounterclockwise:
                    return PhotoOrientation.Rotate90;
                case SimpleOrientation.Rotated180DegreesCounterclockwise:
                    return PhotoOrientation.Rotate180;
                case SimpleOrientation.Rotated270DegreesCounterclockwise:
                    return PhotoOrientation.Rotate270;
                case SimpleOrientation.NotRotated:
                default:
                    return PhotoOrientation.Normal;
            }
        }
        
        #endregion

        #region Lifecycle

        async void OnAppSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            await CleanupCameraAsync();
            await CleanupUIAsync();
            deferral.Complete();
        }

        async void OnAppResuming(object sender, object o)
        {
            await SetupUIAsync();
            await InitializeCameraAsync();
        }

        async void OnPageUnloaded(object sender, RoutedEventArgs e)
        {
            await CleanupCameraAsync();
            await CleanupUIAsync();
        }

        #endregion
    }
}
