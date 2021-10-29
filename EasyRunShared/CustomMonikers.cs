using Microsoft.VisualStudio.Imaging.Interop;
using System;

namespace EasyRun
{
    internal static class CustomMonikers
    {
        public static readonly Guid CustomImages = new Guid("584846ed-c810-497b-9b10-06087689849e");
        public static readonly ImageMoniker Icon16x16 = new ImageMoniker { Guid = CustomImages, Id = 1 };
        public static readonly ImageMoniker Icon = new ImageMoniker { Guid = CustomImages, Id = 2 };
    }
}
