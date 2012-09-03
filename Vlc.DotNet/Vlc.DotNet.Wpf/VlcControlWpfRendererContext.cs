using System;
using System.Runtime.InteropServices;
using System.Windows.Media;

namespace Vlc.DotNet.Wpf
{
    internal class VlcControlWpfRendererContext
    {
        public IntPtr Data { get; private set; }
        public int Size { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Stride { get; private set; }
        public PixelFormat PixelFormat { get; private set; }

        public VlcControlWpfRendererContext(uint width, uint height, PixelFormat format)
            : this((int)width, (int)height, format)
        {
            
        }
        public VlcControlWpfRendererContext(double width, double height, PixelFormat format)
            : this((int)width, (int)height, format)
        {
        }
        public VlcControlWpfRendererContext(int width, int height, PixelFormat format)
        {
            Size = width * height * format.BitsPerPixel / 8;
            Data = Marshal.AllocHGlobal(Size);
            Width = width;
            Height = height;
            PixelFormat = format;
            Stride = width * format.BitsPerPixel / 8;
        }
        ~VlcControlWpfRendererContext()
        {
            Marshal.FreeHGlobal(Data);
        }
    }
}