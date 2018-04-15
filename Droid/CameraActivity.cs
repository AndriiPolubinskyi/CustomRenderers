using System;

using Android.App;
using Android.OS;
using Android.Views;
using Android.Content;
using CustomRenderer.Droid.Camera2Api;

namespace CustomRenderer.Droid
{
    [Activity]
    public class CameraActivity : Activity
    {
        public event EventHandler<KeyEvent> VolumeUpButtonPressed;

        private byte[] _photo;
        public byte[] Photo
        {
            get
            {
                return _photo;
            }
            set
            {
                _photo = value;
                if (value != null)
                    CloseActivity();
            }
        }

        private void CloseActivity()
        {
            Intent intent = new Intent();
            intent.PutExtra("image", _photo);
            SetResult(Result.Ok, intent);
            Finish();
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            Window.AddFlags(WindowManagerFlags.Fullscreen);
            Window.AddFlags(WindowManagerFlags.TranslucentNavigation);

            ActionBar.Hide();

            this.SetContentView(Resource.Layout.camera_layout);

            if (bundle == null)
            {
                FragmentManager.BeginTransaction().Replace(Resource.Id.container, CameraFragment.NewInstance()).Commit();
            }
        }

        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            if (keyCode == Keycode.VolumeUp)
            {
                VolumeUpButtonPressed?.Invoke(this, e);
                return true;
            }
            return base.OnKeyDown(keyCode, e);
        }
    }
}