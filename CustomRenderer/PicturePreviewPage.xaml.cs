using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace CustomRenderer
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PicturePreviewPage : ContentPage
    {
        private ImageSource Image
        {
            get; set;
        }
        public PicturePreviewPage(byte[] img)
        {
            InitializeComponent();
            Image = ImageSource.FromStream(() => new MemoryStream(img));

            imgView.Source = Image;
        }

        private async void ButtonNo_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }
    }
}