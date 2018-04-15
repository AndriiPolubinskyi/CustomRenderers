
using Android.App;
using Android.Hardware.Camera2;

namespace CustomRenderer.Droid.Camera2Api
{
    public class CameraStateListener : CameraDevice.StateCallback
    {

        public CameraFragment Fragment
        {
            get;
            set;
        }

        public override void OnOpened(CameraDevice camera)
        {
            if (Fragment != null)
            {
                Fragment.CameraDevice = camera;
                Fragment.StartPreview();

                Fragment.CameraOpenCloseLock.Release();

                if (Fragment.AutoFitTextureView != null)
                {
                    Fragment.ConfigureTransform(Fragment.AutoFitTextureView.Width, Fragment.AutoFitTextureView.Height);
                }
            }
        }

        public override void OnDisconnected(CameraDevice camera)
        {
            if (Fragment != null)
            {
                Fragment.CameraOpenCloseLock.Release();
                camera.Close();
                Fragment.CameraDevice = null;
            }
        }

        public override void OnError(CameraDevice camera, CameraError error)
        {
            if (Fragment != null)
            {
                Fragment.CameraOpenCloseLock.Release();
                camera.Close();
                Fragment.CameraDevice = null;
                Activity activity = Fragment.Activity;

                if (activity != null)
                {
                    activity.Finish();
                }
            }
        }
    }
}