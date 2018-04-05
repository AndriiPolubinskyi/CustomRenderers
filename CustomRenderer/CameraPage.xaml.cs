using CustomRenderer.Utils;
using System;
using System.IO;
using Xamarin.Forms;

namespace CustomRenderer
{
	public partial class CameraPage : ContentPage
	{
        public async void SetPhotoResult(byte[] image, int width = -1, int height = -1)
        {
            await Navigation.PushModalAsync(new PicturePreviewPage(image));
        }

        
        public CameraPage ()
		{
			// A custom renderer is used to display the camera UI
			InitializeComponent ();
            NavigationPage.SetHasNavigationBar(this, false);
		}
	}
}

