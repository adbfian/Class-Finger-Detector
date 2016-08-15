using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;
using ClassLibrary_Detector;

namespace Prototype_5
{
    public partial class Form1 : Form
    {
        IColorAbstrak skinDetector;
        Image<Bgr, Byte> targetFrame;
        Image<Bgr, Byte> targetFrameCopy;
       
        Capture grabber;
        AdaptiveSkinDetector detector;

        int framePanjang;
        int frameLebar;

        Hsv hsv_min;
        Hsv hsv_max;
        Ycc ycrcb_min;
        Ycc ycrcb_max;

        Seq<Point> hull;
        Seq<Point> filteredHull;
        Seq<MCvConvexityDefect> defects;
        MCvConvexityDefect[] defectArray;
        MCvBox2D box;
        Ellipse ellip;

        public Form1()
        {
            InitializeComponent();
            grabber = new Emgu.CV.Capture();
            grabber.QueryFrame();
            frameLebar = grabber.Width;
            framePanjang = grabber.Height;
            detector = new AdaptiveSkinDetector(1, AdaptiveSkinDetector.MorphingMethod.NONE);
            hsv_min = new Hsv(0, 45, 0);
            hsv_max = new Hsv(20, 255, 255);
            ycrcb_min = new Ycc(0, 131, 80);
            ycrcb_max = new Ycc(255, 185, 135);
            box = new MCvBox2D();
            ellip = new Ellipse();
            Application.Idle += new EventHandler(jepret);
        }

        void jepret(object sender, EventArgs e)
        {
            targetFrame = grabber.QueryFrame();
            if (targetFrame != null)
            {
                targetFrameCopy = targetFrame.Copy();
                skinDetector = new YCRCBDetector();
                Image<Gray, Byte> skin = skinDetector.DetectSkin(targetFrameCopy, ycrcb_min, ycrcb_max);
                ExtractContourAndHull(skin);
                DrawAndComputeFingersNum();
                imageBox1.Image = targetFrame;
            }
        }
        private void ExtractContourAndHull(Image<Gray, byte> skin)
        {
            using (MemStorage storage = new MemStorage())
            {

                Contour<Point> contours = skin.FindContours(Emgu.CV.CvEnum.CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, Emgu.CV.CvEnum.RETR_TYPE.CV_RETR_LIST, storage);
                Contour<Point> biggestContour = null;

                Double Result1 = 0;
                Double Result2 = 0;
                while (contours != null)
                {
                    Result1 = contours.Area;
                    if (Result1 > Result2)
                    {
                        Result2 = Result1;
                        biggestContour = contours;
                    }
                    contours = contours.HNext;
                }

                if (biggestContour != null)
                {
                    Contour<Point> currentContour = biggestContour.ApproxPoly(biggestContour.Perimeter * 0.0025, storage);
                    targetFrame.Draw(currentContour, new Bgr(Color.LimeGreen), 2);
                    biggestContour = currentContour;


                    hull = biggestContour.GetConvexHull(Emgu.CV.CvEnum.ORIENTATION.CV_CLOCKWISE);
                    box = biggestContour.GetMinAreaRect();
                    PointF[] points = box.GetVertices();

                    Point[] ps = new Point[points.Length];
                    for (int i = 0; i < points.Length; i++)
                        ps[i] = new Point((int)points[i].X, (int)points[i].Y);

                    targetFrame.DrawPolyline(hull.ToArray(), true, new Bgr(200, 125, 75), 2);
                    targetFrame.Draw(new CircleF(new PointF(box.center.X, box.center.Y), 3), new Bgr(200, 125, 75), 2);

                    PointF center;
                    float radius;

                    filteredHull = new Seq<Point>(storage);
                    for (int i = 0; i < hull.Total; i++)
                    {
                        if (Math.Sqrt(Math.Pow(hull[i].X - hull[i + 1].X, 2) + Math.Pow(hull[i].Y - hull[i + 1].Y, 2)) > box.size.Width / 10)
                        {
                            filteredHull.Push(hull[i]);
                        }
                    }

                    defects = biggestContour.GetConvexityDefacts(storage, Emgu.CV.CvEnum.ORIENTATION.CV_CLOCKWISE);

                    defectArray = defects.ToArray();
                }
            }
        }

        private void DrawAndComputeFingersNum()
        {
            int fingerNum = 0;

            #region hull drawing
            //  for (int i = 0; i < filteredHull.Total; i++)
            //  {
            //      PointF hullPoint = new PointF((float)filteredHull[i].X,
            //                                    (float)filteredHull[i].Y);
            //      CircleF hullCircle = new CircleF(hullPoint, 4);
            //      targetFrame.Draw(hullCircle, new Bgr(Color.Aquamarine), 2);
            //  }
            #endregion

            #region defects drawing
            for (int i = 0; i < defects.Total; i++)
            {
                PointF startPoint = new PointF((float)defectArray[i].StartPoint.X,
                                                (float)defectArray[i].StartPoint.Y);

                PointF depthPoint = new PointF((float)defectArray[i].DepthPoint.X,
                                                (float)defectArray[i].DepthPoint.Y);

                PointF endPoint = new PointF((float)defectArray[i].EndPoint.X,
                                                (float)defectArray[i].EndPoint.Y);

                LineSegment2D startDepthLine = new LineSegment2D(defectArray[i].StartPoint, defectArray[i].DepthPoint);

                LineSegment2D depthEndLine = new LineSegment2D(defectArray[i].DepthPoint, defectArray[i].EndPoint);

                CircleF startCircle = new CircleF(startPoint, 5f);

                CircleF depthCircle = new CircleF(depthPoint, 5f);

                CircleF endCircle = new CircleF(endPoint, 5f);

                //Custom heuristic based on some experiment, double check it before use
                if ((startCircle.Center.Y < box.center.Y || depthCircle.Center.Y < box.center.Y) && (startCircle.Center.Y < depthCircle.Center.Y) && (Math.Sqrt(Math.Pow(startCircle.Center.X - depthCircle.Center.X, 2) + Math.Pow(startCircle.Center.Y - depthCircle.Center.Y, 2)) > box.size.Height / 6.5))
                {
                    fingerNum++;
                    targetFrame.Draw(startDepthLine, new Bgr(Color.White), 2);
                    //currentFrame.Draw(depthEndLine, new Bgr(Color.Magenta), 2);
                }


                targetFrame.Draw(startCircle, new Bgr(Color.GreenYellow), 2);
                targetFrame.Draw(depthCircle, new Bgr(Color.Gold), 2);
                //currentFrame.Draw(endCircle, new Bgr(Color.DarkBlue), 4);
            }
            #endregion

      
            MCvFont font = new MCvFont(Emgu.CV.CvEnum.FONT.CV_FONT_HERSHEY_SIMPLEX, 5d, 5d);
            targetFrame.Draw(fingerNum.ToString(), ref font, new Point(50, 150), new Bgr(Color.Green));
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
