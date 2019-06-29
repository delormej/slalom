using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using GeoCoordinatePortable;
using System.Reflection;

namespace SlalomTracker
{
    public class CoursePassImage
    {
        private CoursePass _pass;
        private Graphics _graphics;
        private Bitmap _bitmap;
        private const int CenterOffset = 23;
        //private const int LengthOffset = 10; // buffer top & bottom.
        private const float ScaleFactor = 10.0F;
        private const int TopBottomMargin = 100;

        public CoursePassImage(CoursePass pass)
        {
            _pass = pass;
            int width = (int)((Course.WidthM + CenterOffset) * ScaleFactor);
            int height = (int)(Course.LengthM * ScaleFactor) + (TopBottomMargin*2);
            _bitmap = new Bitmap(width, height);
            _graphics = Graphics.FromImage(_bitmap);
        }

        public Bitmap Draw()
        {
            DrawVersion();

            DrawCourseBounds(_pass.Course.Polygon, Color.CadetBlue);
            DrawCourseBounds(_pass.Course.EntryPolygon, Color.Green);
            DrawCourseBounds(_pass.Course.ExitPolygon, Color.Red);

            DrawCourseFeature(Color.Green, _pass.Course.PreGates);
            DrawCourseFeature(Color.Red, _pass.Course.Gates);
            DrawCourseFeature(Color.Red, _pass.Course.Balls);
            DrawCourseFeature(Color.Yellow, _pass.Course.BoatMarkers);

            DrawCenterLine();
            DrawCoursePass();

            return _bitmap;
        }

        /// Draws center line of the course, from start to end pre-gates.
        private void DrawCenterLine()
        {
            _graphics.DrawLine(new Pen(Color.Gray, 1),
                PointFromCoursePosition(new CoursePosition(0, 0)),
                PointFromCoursePosition(new CoursePosition(0, Course.LengthM)));            
        }

        private void DrawCoursePass()
        {
            Pen inCoursePen = new Pen(Color.Green, 3);
            Pen outCoursePen = new Pen(Color.Pink, 3);
            Pen boatPen = new Pen(Color.Yellow, 1);

            int i = 0; // FirstInCourse(_pass.Measurements);
            int last = _pass.Measurements.Count - 2; // LastInCourse(_pass.Measurements); 
            for (; i < last; i++)
            {
                var m = _pass.Measurements[i];
                if (m.BoatPosition == CoursePosition.Empty)
                    continue;
                
                // DrawHandle() -- TODO refactor
                Pen coursePen = m.InCourse ? inCoursePen : outCoursePen;
                PointF start = PointFromCoursePosition(m.HandlePosition);
                PointF end = PointFromCoursePosition(_pass.Measurements[i + 1].HandlePosition);
                if (start != Point.Empty && end != Point.Empty)
                    _graphics.DrawLine(coursePen, start, end);

                // DrawBoat() -- TODO refactor 
                PointF boatStart = PointFromCoursePosition(m.BoatPosition);
                PointF boatEnd = PointFromCoursePosition(_pass.Measurements[i + 1].BoatPosition);
                if (boatStart != Point.Empty && boatEnd != Point.Empty)
                    _graphics.DrawLine(boatPen, boatStart, boatEnd);

                if (m.BoatPosition.Y >= 314 && m.BoatPosition.Y < 315)
                {
                    Console.WriteLine($"[{i} of {last} @ {m.Timestamp}s] Reached end of course: {m.BoatPosition} : {m.BoatGeoCoordinate}");
                }
                else if (m.BoatPosition.Y >= 55 && m.BoatPosition.Y < 56)
                {
                    Console.WriteLine($"[{i} of {last} @ {m.Timestamp}s] Reached beginning of course: {m.BoatPosition} : {m.BoatGeoCoordinate}");
                }
            }            
        }

        private void DrawVersion()
        {
            string version = Assembly.GetEntryAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            Font font = new Font(FontFamily.GenericMonospace, 16);
            PointF point = new PointF(5,5);
            _graphics.DrawString(version, font, Brushes.OrangeRed, point);

            // purple dot... (center line of entry gate)
            var entryGateCL = Util.MoveTo(_pass.Course.Course55EntryCL, 55.0, _pass.Course.GetCourseHeadingDeg());
            CoursePosition position = _pass.CoursePositionFromGeo(entryGateCL);
            PointF entryPoint = PointFromCoursePosition(position);
            Pen pen = new Pen(Color.Purple, 0.6F);
            _graphics.DrawEllipse(pen, entryPoint.X, entryPoint.Y, 1, 1);

            // orange dot... (upper right of entry gate)
            //_pass.Course.EntryPolygon
            //var entryGateCL2 = Util.MoveTo(_pass.Course.Course55EntryCL, 54.0, _pass.Course.GetCourseHeadingDeg());
            CoursePosition position2 = _pass.CoursePositionFromGeo(_pass.Course.EntryPolygon[0]);
            PointF entryPoint2 = PointFromCoursePosition(position2);
            Pen pen2 = new Pen(Color.Orange, 3F);
            _graphics.DrawEllipse(pen2, entryPoint2.X, entryPoint2.Y, 3, 3);


            // Blue dot (bottom left of exit gate)
            CoursePosition position3 = _pass.CoursePositionFromGeo(_pass.Course.ExitPolygon[0]);
            PointF entryPoint3 = PointFromCoursePosition(position3);
            Pen pen3 = new Pen(Color.Blue, 3F);
            _graphics.DrawEllipse(pen3, entryPoint3.X, entryPoint3.Y, 3, 3);

            // Pink dot (center exit55 CL)
            CoursePosition exit55CLPosition = _pass.CoursePositionFromGeo(_pass.Course.Course55ExitCL);
            PointF exit55CLPointF = PointFromCoursePosition(exit55CLPosition);
            Pen penExit55CL = new Pen(Color.Pink, 3F);
            _graphics.DrawEllipse(penExit55CL, exit55CLPointF.X, exit55CLPointF.Y, 3, 3);

        }

        private void DrawCourseBounds(List<GeoCoordinate> list, Color color)
        {
            // Convert geos to relative course positions, then to absolute screen points.
            List<PointF> points = new List<PointF>(list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                CoursePosition position = _pass.CoursePositionFromGeo(list[i]);
                points.Add(PointFromCoursePosition(position));
            }

            Pen pen = new Pen(color, 0.6F);
            _graphics.DrawLine(pen, points[0], points[1]);
            _graphics.DrawLine(pen, points[1], points[2]);
            _graphics.DrawLine(pen, points[2], points[3]);
            _graphics.DrawLine(pen, points[3], points[0]);
        }

        private void DrawCourseFeature(Color color, CoursePosition[] positions)
        {
            Pen pen = new Pen(color, 2);
            foreach (var position in positions)
            {
                PointF point = PointFromCoursePosition(position);
                _graphics.DrawEllipse(pen, point.X-1, point.Y-1, 2, 2);
            }
        }

       private int FirstInCourse(List<Measurement> measurements)
        {
            int i = 0;
            while (!measurements[i++].InCourse && i < measurements.Count);
            return i;
        }

        private int LastInCourse(List<Measurement> measurements)
        {
            int i = measurements.Count - 1;
            while (!measurements[i--].InCourse && i > 0);
            return i;
        }

        /// <summary>
        /// Converts relative course position X,Y coordiantes to a point on the drawable screen.
        /// Screen size is always positive, for example (0,0) upper left (46,738) lower right at a ScaleFactor of 2.0.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private PointF PointFromCoursePosition(CoursePosition position)
        {
            // CenterOffset is used to create a positive X coordinate from CoursePosition which is relative to
            // pre-gate where center line is x=0,y=0.
            float x = ScaleFactor * ((float)position.X + CenterOffset);
            float y = (ScaleFactor * (float)position.Y) + TopBottomMargin;
            return new PointF(x, y);
        }
    }
}
