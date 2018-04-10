using MvvmHelpers;
using System;
using Xamarin.Forms;

namespace CustomRenderer.Models
{
    public class GalleryImage : ObservableObject
    {
        public GalleryImage()
        {
            ImageId = Guid.NewGuid();
        }

        public Guid ImageId
        {
            get;
            set;
        }

        public ImageSource Source
        {
            get;
            set;
        }

        public byte[] OriginalImage
        {
            get;
            set;
        }
    }
}
