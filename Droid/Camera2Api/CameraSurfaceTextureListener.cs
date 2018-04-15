using Android.Views;
using Android.Graphics;

namespace CustomRenderer.Droid.Camera2Api
{
    public class CameraSurfaceTextureListener : Java.Lang.Object, TextureView.ISurfaceTextureListener
    {
        private readonly CameraFragment _fragment;

        public CameraSurfaceTextureListener(CameraFragment fragment)
        {
            _fragment = fragment;
        }

        public void OnSurfaceTextureAvailable(SurfaceTexture surface, int width, int height)
        {
            _fragment.OpenCamera();
        }

        public bool OnSurfaceTextureDestroyed(SurfaceTexture surface)
        {
            return true;
        }

        public void OnSurfaceTextureSizeChanged(SurfaceTexture surface, int width, int height)
        {
            _fragment.ConfigureTransform(width, height);
        }

        public void OnSurfaceTextureUpdated(SurfaceTexture surface)
        {
        }
    }
}