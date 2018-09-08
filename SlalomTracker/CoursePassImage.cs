using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using GeoCoordinatePortable;

namespace SlalomTracker
{
    public class CoursePassImage
    {
        private CoursePass _pass;
        private Graphics _graphics;
        private Bitmap _bitmap;
        private const int DefaultScaleFactor = 5;
        public static readonly double CenterOffset = 23;
        public static readonly double LengthOffset = 10; // buffer top & bottom.

        public int Width { get; private set; }
        public int Height { get; private set; }
        public int ScaleFactor { get; private set; }

        public CoursePassImage(CoursePass pass)
        {
            _pass = pass;
            ScaleFactor = DefaultScaleFactor;
            double widthFactor = CenterOffset * 2;
            double heightFactor = LengthOffset * 2;
            Width = (int)(ScaleFactor * (Course.WidthM + widthFactor));
            Height = (int)(ScaleFactor * (Course.LengthM + heightFactor));
            _bitmap = new Bitmap(Width, Height);
            _graphics = Graphics.FromImage(_bitmap);
        }

        public Bitmap Draw()
        {
            DrawCourseBounds();

            DrawCourseFeature(Color.Green, _pass.Course.PreGates);
            DrawCourseFeature(Color.Red, _pass.Course.Gates);
            DrawCourseFeature(Color.Red, _pass.Course.Balls);
            DrawCourseFeature(Color.Yellow, _pass.Course.BoatMarkers);

            // Draw Center Line.
            _graphics.DrawLine(new Pen(Color.Gray, 1),
                PointFromCoursePosition(new CoursePosition(11.5, 0)),
                PointFromCoursePosition(new CoursePosition(11.5, Course.LengthM)));

            Pen inCoursePen = new Pen(Color.Green, 3);
            Pen outCoursePen = new Pen(Color.Pink, 3);

            for (int i = 0; i < _pass.Measurements.Count - 2; i++)
            {
                var m = _pass.Measurements[i];
                Pen coursePen = m.InCourse ? inCoursePen : outCoursePen;
                Point start = PointFromCoursePosition(m.HandlePosition);
                Point end = PointFromCoursePosition(_pass.Measurements[i + 1].HandlePosition);
                if (start != Point.Empty && end != Point.Empty)
                    _graphics.DrawLine(coursePen, start, end);
            }

            return _bitmap;
        }

        private void DrawCourseBounds()
        {
            List<GeoCoordinate> list = _pass.Course.GetPolygon();
            // Convert geos to relative course positions, then to absolute screen points.
            List<Point> points = new List<Point>(list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                CoursePosition position = _pass.CoursePositionFromGeo(list[i]);

                if (i == 0 || i == 2)
                {
                    position.X = 0;
                }
                else
                {
                    position.X = 23;
                }

                points.Add(PointFromCoursePosition(position));
            }

            Pen pen = new Pen(Color.Green, 0.6F);
            _graphics.DrawLine(pen, points[0], points[1]);
            _graphics.DrawLine(pen, points[0], points[2]);
            _graphics.DrawLine(pen, points[2], points[3]);
            _graphics.DrawLine(pen, points[3], points[1]);
        }

        private void DrawCourseFeature(Color color, CoursePosition[] positions)
        {
            Pen pen = new Pen(color, 2);
            foreach (var position in positions)
            {
                Point point = PointFromCoursePosition(position);
                _graphics.DrawEllipse(pen, point.X-1, point.Y-1, 2, 2);
            }
        }

        /// <summary>
        /// Converts relative course position X,Y coordiantes to a point on the drawable screen.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private Point PointFromCoursePosition(CoursePosition position)
        {
            // Image is 46m wider (23 on each side).
            return new Point((int)((position.X + CenterOffset) * ScaleFactor), 
                (int)((position.Y + LengthOffset) * ScaleFactor));
        }
    }
}
