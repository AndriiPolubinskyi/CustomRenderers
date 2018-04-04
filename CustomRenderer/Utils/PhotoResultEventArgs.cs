using System;

namespace CustomRenderer.Utils
{
    public class PhotoResultEventArgs : EventArgs
    {

        public PhotoResultEventArgs()
        {
            Success = false;
        }

        public PhotoResultEventArgs(byte[] image, int width, int height)
        {
            Success = true;
            Image = image;
            Width = width;
            Height = height;
        }

        public byte[] Image { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public bool Success { get; private set; }
    }
}
