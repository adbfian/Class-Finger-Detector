using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emgu.CV.Structure;
using Emgu.CV;

namespace ClassLibrary_Detector
{
    public class HSVDetector:IColorAbstrak
    {
        //Class untuk eksekusi Hue dan Saturation pada gambar
        public override Image<Gray, byte> DetectSkin(Image<Bgr, byte> Img, IColor min, IColor max)
        {
            Image<Hsv, Byte> currentHsvFrame = Img.Convert<Hsv, Byte>();
            Image<Gray, byte> skin = new Image<Gray, byte>(Img.Width, Img.Height);
            skin = currentHsvFrame.InRange((Hsv)min, (Hsv)max);
            return skin;
        }
    }
}
