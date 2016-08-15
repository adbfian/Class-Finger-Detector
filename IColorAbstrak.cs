using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emgu.CV.Structure;
using Emgu.CV;

namespace ClassLibrary_Detector
{
    public abstract class IColorAbstrak
    {
        public abstract Image<Gray, Byte> DetectSkin(Image<Bgr, Byte> Img, IColor min, IColor max);
    }
}
