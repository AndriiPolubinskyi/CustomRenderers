using System;
using System.Collections.Generic;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using Java.Lang;
using Java.Util.Concurrent;
using Android.Graphics;
using Android.Hardware.Camera2;
using Android.Util;
using Android.Media;
using Android.Hardware.Camera2.Params;
using CustomRenderer.Enums;

namespace CustomRenderer.Droid.Camera2Api
{
    public class CameraFragment : Fragment
    {
        internal const string ExtraCameraFacingDirection = "cameraFacingDirection";
        internal const string ExtraId = "id";

        private static readonly SparseIntArray Orientations = new SparseIntArray();

        // An AutoFitTextureView for camera preview
        public AutoFitTextureView AutoFitTextureView;

        // A CameraRequest.Builder for camera preview
        private CaptureRequest.Builder _previewBuilder;

        // A CameraCaptureSession for camera preview
        public CameraCaptureSession PreviewSession;

        // A reference to the opened CameraDevice
        public CameraDevice CameraDevice;

        // The size of the camera preview
        private Size _previewSize;

        // CameraDevice.StateListener is called when a CameraDevice changes its state
        private CameraStateListener _cameraStateListener;

        /**
        * A {@link Semaphore} to prevent the app from exiting before closing the camera.
        */
        public readonly Semaphore CameraOpenCloseLock = new Semaphore(1);

        private HandlerThread _backgroundThread;
        private Handler _backgroundHandler;
        private int facing = 0;

        // TextureView.ISurfaceTextureListener handles several lifecycle events on a TextureView
        private CameraSurfaceTextureListener _surfaceTextureListener;
        private int requestId;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Bundle b = (savedInstanceState ?? Activity.Intent.Extras);

            facing = b.GetInt(ExtraCameraFacingDirection);

            _cameraStateListener = new CameraStateListener { Fragment = this };
            _surfaceTextureListener = new CameraSurfaceTextureListener(this);

            Orientations.Append((int)SurfaceOrientation.Rotation0, 90);
            Orientations.Append((int)SurfaceOrientation.Rotation90, 0);
            Orientations.Append((int)SurfaceOrientation.Rotation180, 270);
            Orientations.Append((int)SurfaceOrientation.Rotation270, 180);
        }

        public override void OnSaveInstanceState(Bundle outState)
        {
            outState.PutInt(ExtraCameraFacingDirection, (int)facing);
            base.OnSaveInstanceState(outState);
        }

        public static CameraFragment NewInstance()
        {
            var fragment = new CameraFragment();
            fragment.RetainInstance = true;
            return fragment;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.fragment_camera2_basic, container, false);
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            AutoFitTextureView = (AutoFitTextureView)view.FindViewById(Resource.Id.texture);
            AutoFitTextureView.SurfaceTextureListener = _surfaceTextureListener;

            var takePictureButton = view.FindViewById(Resource.Id.takePictureButton);
            takePictureButton.Click += TakePictureButtonOnClick;

            var switchCameraButton = view.FindViewById(Resource.Id.switchCameraButton);
            switchCameraButton.Click += SwitchCameraButtonOnClick;
        }

        public override void OnResume()
        {
            base.OnResume();

            StartBackgroundThread();
            if (AutoFitTextureView.IsAvailable)
            {
                OpenCamera();
            }
            else
            {
                AutoFitTextureView.SurfaceTextureListener = _surfaceTextureListener;
            }
        }

        public override void OnPause()
        {
            CloseCamera();
            StopBackgroundThread();
            base.OnPause();
        }

        private void StartBackgroundThread()
        {
            _backgroundThread = new HandlerThread("CameraBackground");
            _backgroundThread.Start();
            _backgroundHandler = new Handler(_backgroundThread.Looper);
        }

        private void StopBackgroundThread()
        {
            _backgroundThread.QuitSafely();
            try
            {
                _backgroundThread.Join();
                _backgroundThread = null;
                _backgroundHandler = null;
            }
            catch (InterruptedException ex)
            {
                Log.WriteLine(LogPriority.Error, "Exception", ex.Message);
            }
        }

        private static CameraFacingDirection ToCameraFacingDirection(int lensFacing)
        {
            if (lensFacing == 1)
            {
                return CameraFacingDirection.Rear;
            }

            return CameraFacingDirection.Front;
        }

        // Opens a CameraDevice. The result is listened to by 'cameraStateListener'.
        public void OpenCamera()
        {
            var activity = Activity;
            if (activity == null || activity.IsFinishing)
            {
                return;
            }

            var cameraManager = (CameraManager)activity.GetSystemService(Context.CameraService);

            try
            {
                if (!CameraOpenCloseLock.TryAcquire(2500, TimeUnit.Milliseconds))
                {
                    const string ErrorMessage = "Time out waiting to lock camera opening.";
                    throw new RuntimeException(ErrorMessage);
                }

                string idForOpen = null;
                string[] camerasIds = cameraManager.GetCameraIdList();
                foreach (string id in camerasIds)
                {
                    CameraCharacteristics cameraCharacteristics = cameraManager.GetCameraCharacteristics(id);
                    var cameraLensFacing = (int)cameraCharacteristics.Get(CameraCharacteristics.LensFacing);

                    CameraFacingDirection cameraFacingDirection = ToCameraFacingDirection(cameraLensFacing);
                    CameraFacingDirection configuredFacingDirection = ToCameraFacingDirection(facing);

                    if (cameraFacingDirection == configuredFacingDirection)
                    {
                        idForOpen = id;
                        break;
                    }
                }

                var cameraId = idForOpen ?? camerasIds[0];

                // To get a list of available sizes of camera preview, we retrieve an instance of
                // StreamConfigurationMap from CameraCharacteristics
                CameraCharacteristics characteristics = cameraManager.GetCameraCharacteristics(cameraId);
                StreamConfigurationMap map = (StreamConfigurationMap)characteristics.Get(CameraCharacteristics.ScalerStreamConfigurationMap);
                _previewSize = map.GetOutputSizes(Class.FromType(typeof(SurfaceTexture)))[0]; // We assume that the top-most element is the one with the best resolution
                Android.Content.Res.Orientation orientation = Resources.Configuration.Orientation;
                if (orientation == Android.Content.Res.Orientation.Landscape)
                {
                    AutoFitTextureView.SetAspectRatio(_previewSize.Width, _previewSize.Height);
                }
                else
                {
                    AutoFitTextureView.SetAspectRatio(_previewSize.Height, _previewSize.Width);
                }

                // We are opening the camera with a listener. When it is ready, OnOpened of cameraStateListener is called.
                cameraManager.OpenCamera(cameraId, _cameraStateListener, null);
            }
            catch (CameraAccessException caex)
            {
                Toast.MakeText(activity, "Cannot access the camera.", ToastLength.Short).Show();
                Activity.Finish();
            }
            catch (NullPointerException npex)
            {
                Log.WriteLine(LogPriority.Error, "Exception", npex.Message);
            }
            catch (InterruptedException e)
            {
                const string ErrorMessage = "Interrupted while trying to lock camera opening.";
                throw new RuntimeException(ErrorMessage);
            }
            catch 
            {
                throw;
            }
        }

        private void CloseCamera()
        {
            try
            {
                CameraOpenCloseLock.Acquire();
                if (CameraDevice != null)
                {
                    CameraDevice.Close();
                    CameraDevice = null;
                }
            }
            catch (InterruptedException e)
            {
                throw new RuntimeException("Interrupted while trying to lock camera closing.");
            }
            finally
            {
                CameraOpenCloseLock.Release();
            }
        }

        public void StartPreview()
        {
            if (CameraDevice == null || !AutoFitTextureView.IsAvailable || _previewSize == null)
            {
                return;
            }

            try
            {
                SurfaceTexture texture = AutoFitTextureView.SurfaceTexture;
                // We configure the size of the default buffer to be the size of the camera preview we want
                texture.SetDefaultBufferSize(_previewSize.Width, _previewSize.Height);

                // This is the output Surface we need to start the preview
                Surface surface = new Surface(texture);

                // We set up a CaptureRequest.Builder with the output Surface
                _previewBuilder = CameraDevice.CreateCaptureRequest(CameraTemplate.Preview);
                _previewBuilder.AddTarget(surface);

                // Here, we create a CameraCaptureSession for camera preview.
                CameraDevice.CreateCaptureSession(
                    new List<Surface> { surface },
                    new CameraCaptureStateListener
                    {
                        OnConfigureFailedAction = (CameraCaptureSession session) =>
                        {
                            Activity activity = Activity;
                            if (activity != null)
                            {
                                Toast.MakeText(activity, "Camera configuration failed", ToastLength.Short).Show();
                            }
                        },
                        OnConfiguredAction = (CameraCaptureSession session) =>
                        {
                            PreviewSession = session;
                            UpdatePreview();
                        }
                    },
                    _backgroundHandler);
            }
            catch (CameraAccessException ex)
            {
                Log.WriteLine(LogPriority.Error, "Exception", ex.Message);
            }
        }

        /// <summary>
        ///     Updates the camera preview, StartPreview() needs to be called in advance
        /// </summary>
        private void UpdatePreview()
        {
            if (CameraDevice == null)
            {
                return;
            }

            try
            {
                // The camera preview can be run in a background thread. This is a Handler for the camere preview
                SetUpCaptureRequestBuilder(_previewBuilder);
                var thread = new HandlerThread("CameraPreview");
                thread.Start();

                // Finally, we start displaying the camera preview
                PreviewSession.SetRepeatingRequest(_previewBuilder.Build(), null, _backgroundHandler);
            }
            catch (CameraAccessException ex)
            {
                Log.WriteLine(LogPriority.Error, "Exception", ex.Message);
            }
        }

        /// <summary>
        ///     Sets up capture request builder.
        /// </summary>
        /// <param name="builder">Builder.</param>
        private void SetUpCaptureRequestBuilder(CaptureRequest.Builder builder)
        {
            // In this sample, we just let the camera device pick the automatic settings
            builder.Set(CaptureRequest.ControlMode, new Integer((int)ControlMode.Auto));
        }

        /// <summary>
        ///     Configures the necessary transformation to autoFitTextureView.
        ///     This method should be called after the camera preciew size is determined in openCamera, and also the size of
        ///     autoFitTextureView is fixed
        /// </summary>
        /// <param name="viewWidth">The width of autoFitTextureView</param>
        /// <param name="viewHeight">VThe height of autoFitTextureView</param>
        public void ConfigureTransform(int viewWidth, int viewHeight)
        {
            Activity activity = Activity;
            if (AutoFitTextureView == null || _previewSize == null || activity == null)
            {
                return;
            }

            SurfaceOrientation rotation = activity.WindowManager.DefaultDisplay.Rotation;
            Matrix matrix = new Matrix();
            RectF viewRect = new RectF(0, 0, viewWidth, viewHeight);
            RectF bufferRect = new RectF(0, 0, _previewSize.Width, _previewSize.Height);
            float centerX = viewRect.CenterX();
            float centerY = viewRect.CenterY();
            if (rotation == SurfaceOrientation.Rotation90 || rotation == SurfaceOrientation.Rotation270)
            {
                bufferRect.Offset(centerX - bufferRect.CenterX(), centerY - bufferRect.CenterY());
                matrix.SetRectToRect(viewRect, bufferRect, Matrix.ScaleToFit.Fill);
                float verticalScale = (float)viewHeight / _previewSize.Height;
                float horizontalScale = (float)viewWidth / _previewSize.Width;
                float scale = System.Math.Max(verticalScale, horizontalScale);
                matrix.PostScale(scale, scale, centerX, centerY);
                matrix.PostRotate(90 * ((int)rotation - 2), centerX, centerY);
            }
            AutoFitTextureView.SetTransform(matrix);
        }

        private void TakePictureButtonOnClick(object sender, EventArgs e)
        {
            TakePicture();
        }

        private void TakePicture()
        {
            try
            {
                Activity activity = Activity;
                if (activity == null || CameraDevice == null)
                {
                    return;
                }

                var cameraManager = (CameraManager)activity.GetSystemService(Context.CameraService);

                // Pick the best JPEG size that can be captures with this CameraDevice
                var characteristics = cameraManager.GetCameraCharacteristics(CameraDevice.Id);
                Size[] jpegSizes = null;
                if (characteristics != null)
                {
                    jpegSizes = ((StreamConfigurationMap)characteristics.Get(CameraCharacteristics.ScalerStreamConfigurationMap)).GetOutputSizes((int)ImageFormatType.RawSensor);
                }

                int width = 640;
                int height = 480;
                if (jpegSizes != null && jpegSizes.Length > 0)
                {
                    width = jpegSizes[0].Width;
                    height = jpegSizes[0].Height;
                }

                // We use an ImageReader to get a JPEG from CameraDevice
                // Here, we create a new ImageReader and prepare its Surface as an output from the camera
                var reader = ImageReader.NewInstance(width, height, ImageFormatType.Jpeg, 1);
                var outputSurfaces = new List<Surface>(2);
                outputSurfaces.Add(reader.Surface);
                outputSurfaces.Add(new Surface(AutoFitTextureView.SurfaceTexture));

                var captureBuilder = CameraDevice.CreateCaptureRequest(CameraTemplate.StillCapture);
                captureBuilder.AddTarget(reader.Surface);
                SetUpCaptureRequestBuilder(captureBuilder);

                // Orientation
                var rotation = activity.WindowManager.DefaultDisplay.Rotation;
                captureBuilder.Set(CaptureRequest.JpegOrientation, new Integer(Orientations.Get((int)rotation)));

                // This listener is called when an image is ready in ImageReader 
                var readerListener = new ImageAvailableListener();
                readerListener.ImageProcessingCompleted += (s, e) =>
                {
                    (activity as CameraActivity).Photo = e;
                };

                // We create a Handler since we want to handle the resulting JPEG in a background thread
                var thread = new HandlerThread("CameraPicture");
                thread.Start();
                var backgroundHandler = new Handler(thread.Looper);
                reader.SetOnImageAvailableListener(readerListener, backgroundHandler);

                CameraDevice.CreateCaptureSession(
                    outputSurfaces,
                    new CameraCaptureStateListener
                    {
                        OnConfiguredAction = (CameraCaptureSession session) =>
                        {
                            try
                            {
                                session.Capture(captureBuilder.Build(), new CameraCaptureListener(), backgroundHandler);
                            }
                            catch (CameraAccessException ex)
                            {
                                Log.WriteLine(LogPriority.Error, "Exception", ex.Message);
                            }
                        }
                    },
                    backgroundHandler);
            }
            catch (CameraAccessException ex)
            {
                Log.WriteLine(LogPriority.Error, "Exception", ex.Message);
            }
        }

        private bool CanSwitch()
        {
            var manager = (CameraManager)Activity.GetSystemService(Context.CameraService);
            try
            {
                int numberOfCameras = manager.GetCameraIdList().Length;
                return numberOfCameras > 1;
            }
            catch (CameraAccessException ex)
            {
                Log.WriteLine(LogPriority.Error, "Exception", ex.Message);
                return false;
            }
        }

        private void SwitchCameraButtonOnClick(object sender, EventArgs e)
        {
            SwitchCamera();
        }

        private void SwitchCamera()
        {
            if (!CanSwitch())
            {
                return;
            }

            if (facing == 1)
            {
                facing = 0;
            }
            else if (facing == 0)
            {
                facing = 1;
            }

            RestartCamera();
        }

        private void RestartCamera()
        {
            CloseCamera();
            OpenCamera();
        }
    }
}