using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using CustomRenderer.Models;

namespace CustomRenderer.ViewModels
{
    public class PicturePreviewPageViewModel: BaseViewModelExt
    {
        private ICommand _addImageToCollectionCommand = null, _backToCameraPageCommand = null, _goToMainPageCommand =null;
        public GalleryImage PreviewImage { get; set; }

        public PicturePreviewPageViewModel(GalleryImage image)
        {
            PreviewImage = image;
        }
        public ICommand AddImageToCollectionCommand
        {
            get { return _addImageToCollectionCommand ?? new Command(async () => await ExecuteAddImageToCollectionCommand()); }
        }

        public async Task ExecuteAddImageToCollectionCommand()
        {
            MessagingCenter.Send(PreviewImage, "AddImage");
            await Navigation.PopAsync();
        }

        public ICommand BackToCameraPageCommand
        {
            get { return _backToCameraPageCommand ?? new Command(async () => await Navigation.PopAsync()); }
        }

        public ICommand GoToMainPageCommand
        {
            get { return _goToMainPageCommand ?? new Command(async () => await Navigation.PopToRootAsync() ); }
        }

    }
}
