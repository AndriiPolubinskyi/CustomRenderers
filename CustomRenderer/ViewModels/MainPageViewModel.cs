using CustomRenderer.Models;
using CustomRenderer.Views;
using MvvmHelpers;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace CustomRenderer.ViewModels
{
    public class MainPageViewModel: BaseViewModelExt
    {
        ICommand _cameraCommand, _previewImageCommand = null;
        ObservableCollection<GalleryImage> _images = new ObservableCollection<GalleryImage>();
        ImageSource _previewImage = null;

        public MainPageViewModel()
        {
            MessagingCenter.Subscribe<GalleryImage>(this, "AddImage", (image) =>
            {
                _images.Add(image);
            });
        }
        public ObservableCollection<GalleryImage> Images
        {
            get { return _images; }
        }

        public ImageSource PreviewImage
        {
            get { return _previewImage; }
            set
            {
                SetProperty(ref _previewImage, value);
            }
        }

        public ICommand CameraCommand
        {
            get { return _cameraCommand ?? new Command(async () => await ExecuteCameraCommand()); }
        }


        public async Task ExecuteCameraCommand()
        {
            await Navigation.PushAsync(new CameraPageView());
        }

        public ICommand PreviewImageCommand
        {
            get
            {
                return _previewImageCommand ?? new Command<Guid>((img) => {

                    var image = _images.Single(x => x.ImageId == img).OriginalImage;

                    PreviewImage = ImageSource.FromStream(() => new MemoryStream(image));

                });
            }
        }
    }
}

