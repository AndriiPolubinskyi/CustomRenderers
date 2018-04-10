using CustomRenderer.Models;
using CustomRenderer.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace CustomRenderer.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PicturePreviewPageView : ContentPage
    {
        public PicturePreviewPageView(GalleryImage image)
        {
            InitializeComponent();
            BindingContext = new PicturePreviewPageViewModel(image);
            NavigationPage.SetHasNavigationBar(this, false);
        }
    }
}