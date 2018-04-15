using CustomRenderer.Models;
using CustomRenderer.Views;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace CustomRenderer.ViewModels
{
    public class CameraPageViewModel: BaseViewModelExt
    {
        private ICommand _previewPhotoCommand;

        public GalleryImage Image { get; set; }

        public ICommand PreviewPhotoCommand
        {
            get { return _previewPhotoCommand ?? new Command(async () => await ExecutePreviewPhotoCommand()); }
        }

        public async Task ExecutePreviewPhotoCommand()
        {
            await Navigation.PushAsync(new PicturePreviewPageView(Image), false);
        }

       
    }
}
