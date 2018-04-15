using System;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Android.Content;
using CustomRenderer.Views;
using CustomRenderer.Droid.Renderers;
using CustomRenderer.Droid.Camera2Api;

[assembly: ExportRenderer(typeof(CameraPageView), typeof(CameraPageRenderer))]
namespace CustomRenderer.Droid.Renderers
{
    public class CameraPageRenderer : PageRenderer
    {
        public CameraPageRenderer(Context context) : base(context)
        {
        }

        protected override void OnElementChanged(ElementChangedEventArgs<Page> e)
        {
            base.OnElementChanged(e);

            if (e.OldElement != null || Element == null)
            {
                return;
            }

            var activity = this.Context as MainActivity;
            if (e.OldElement != null)
            {
                activity.ActivityResult -= HandleActivityResult;
            }

            if (e.NewElement != null)
            {
                activity.ActivityResult += HandleActivityResult;
            }

            ((ContentPage)Element).Appearing += CameraPageRenderer_Appearing;

        }

        private void CameraPageRenderer_Appearing(object sender, EventArgs e)
        {
            var cameraActivityIntent = CreateCameraIntent();
            (Context as MainActivity).StartActivityForResult(cameraActivityIntent, 100);
        }

        private void HandleActivityResult(object sender, ActivityResultEventArgs e)
        {
            (Element as CameraPageView).SetPhotoResult(e.Data.GetByteArrayExtra("image"));
        }

        private Intent CreateCameraIntent()
        {
            Intent pickerIntent = new Intent(Context, typeof(CameraActivity));
            pickerIntent.PutExtra(CameraFragment.ExtraCameraFacingDirection, (int)Enums.CameraFacingDirection.Rear);
            pickerIntent.SetFlags(ActivityFlags.SingleTop);
            return pickerIntent;
        }
    }
}

