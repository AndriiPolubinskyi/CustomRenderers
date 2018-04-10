using CustomRenderer.Models;
using CustomRenderer.ViewModels;
using System.IO;
using Xamarin.Forms;

namespace CustomRenderer.Views
{
    public partial class CameraPageView : ContentPage
	{
        private CameraPageViewModel _viewModel;
        public void SetPhotoResult(byte[] image, int width = -1, int height = -1)
        {
            _viewModel.Image = new GalleryImage();
            _viewModel.Image.OriginalImage = image;
            _viewModel.Image.Source = ImageSource.FromStream(() => new MemoryStream(image));
            _viewModel.PreviewPhotoCommand.Execute(null);
        }


        public CameraPageView ()
		{
			// A custom renderer is used to display the camera UI
			InitializeComponent ();
            _viewModel = new CameraPageViewModel();
            BindingContext = _viewModel;
            NavigationPage.SetHasNavigationBar(this, false);
		}
	}
}

