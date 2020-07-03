using System;
using System.Collections.Generic;
using System.Drawing;
using GeoCoordinatePortable;
using System.Reflection;
using System.Linq;
using Logger = jasondel.Tools.Logger;

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
            DrawCourseSpeed();
            DrawRopeLength();
            DrawCenterLineOffset();

            //DrawCourseBounds(_pass.Course.Polygon, Color.CadetBlue);
            DrawCourseBounds(_pass.Course.Entry55Polygon, Color.Green);
            DrawCourseBounds(_pass.Course.Exit55Polygon, Color.Red);

            DrawCourseFeature(Color.Green, Course.PreGates);
            DrawCourseFeature(Color.Red, Course.Gates);
            DrawCourseFeature(Color.Red, Course.Balls);
            DrawCourseFeature(Color.Yellow, Course.BoatMarkers);

            DrawCenterLine();
            DrawCoursePass();

            DrawPullOutAngle();

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
            int i = 0; // FirstInCourse(_pass.Measurements);
            int last = _pass.Measurements.Count - 2; // LastInCourse(_pass.Measurements); 
            HandleMeasurements handleMeasurements = new HandleMeasurements(this);
            for (; i < last; i++)
            {
                var m = _pass.Measurements[i];
                if (m.BoatPosition == CoursePosition.Empty)
                {
                    Logger.Log($"Out of course {i}");
                    continue;
                }
                
                DrawBoat(m, i);
                DrawHandle(m, i);
                handleMeasurements.Draw(m, i);
            }            
        }

        private void DrawHandle(Measurement m, int i) 
        {
            Pen coursePen = new Pen(m.InCourse ? Color.Green : Color.Pink, 3);
            PointF start = PointFromCoursePosition(m.HandlePosition);
            PointF end = PointFromCoursePosition(_pass.Measurements[i + 1].HandlePosition);
            if (start != Point.Empty && end != Point.Empty)
                _graphics.DrawLine(coursePen, start, end);            
        }

        private void DrawBoat(Measurement m, int i)
        {
            Pen boatPen = new Pen(Color.Yellow, 1);
            PointF boatStart = PointFromCoursePosition(m.BoatPosition);
            PointF boatEnd = PointFromCoursePosition(_pass.Measurements[i + 1].BoatPosition);
            if (boatStart != Point.Empty && boatEnd != Point.Empty)
                _graphics.DrawLine(boatPen, boatStart, boatEnd);
        }

        private void DrawVersion()
        {
            string version = Assembly.GetEntryAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            Font font = new Font(FontFamily.GenericMonospace, 16);
            PointF point = new PointF(5,5);
            _graphics.DrawString(version, font, Brushes.OrangeRed, point);
        }

        private void DrawCourseSpeed()
        {
            string speed = $"Course speed: {_pass.AverageBoatSpeed}mph";
            Font font = new Font(FontFamily.GenericMonospace, 12);

            PointF point = new PointF(5,30);
            _graphics.DrawString(speed, font, Brushes.Yellow, point);            
        }

        private void DrawRopeLength()
        {
            string speed = $"Rope length: {Math.Round(_pass.Rope.FtOff,0)}' off";
            Font font = new Font(FontFamily.GenericMonospace, 12);

            PointF point = new PointF(5,51);
            _graphics.DrawString(speed, font, Brushes.Green, point);
        }

        private void DrawCenterLineOffset()
        {
            string clOffset = $"CL Offset: {Math.Round(_pass.CenterLineDegreeOffset,2)}";
            Font font = new Font(FontFamily.GenericMonospace, 12);

            PointF point = new PointF(5,72);
            _graphics.DrawString(clOffset, font, Brushes.Green, point);
        }

        private void DrawCourseBounds(List<GeoCoordinate> list, Color color)
        {
            // Convert geos to relative course positions, then to absolute screen points.
            List<PointF> points = new List<PointF>(list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                CoursePosition position = _pass.Course.CoursePositionFromGeo(list[i]);
                points.Add(PointFromCoursePosition(position));
            }

            Pen pen = new Pen(color, 0.6F);
            pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
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

        private void DrawPullOutAngle()
        {
            var range = _pass.Measurements.Where(
                /* In between 55s and Gates */
                m => m.HandlePosition.Y > Course.PreGates[0].Y &&
                m.HandlePosition.Y < Course.Gates[0].Y);

            if (range?.Count() == 0)
            {
                Logger.Log("Cannot draw pullout angle, no measurments between 55s and gates.");
                return;
            }

            double maxRopeAngle = range.Min(m => m.RopeAngleDegrees); // always pulls to the left which is a negative #.
            Measurement maxPullout = range.Where(m => m.RopeAngleDegrees == maxRopeAngle).Last();

            if (maxPullout != null)            
                DrawAngleAtBall(maxPullout);
        }

        private void DrawAngleAtBall(Measurement m)
        {
            string text = Math.Round(Math.Abs(m.RopeAngleDegrees), 1) + "°";
            DrawTextNearMeasurement(m, text);
        }            

        private void DrawTextNearMeasurement(Measurement m, string text)
        {
            const float textMargin = 15.0F;
            Font font = new Font(FontFamily.GenericMonospace, 12);

            PointF point = PointFromCoursePosition(m.HandlePosition);
            point.X += textMargin;
            _graphics.DrawString(text, font, Brushes.LightSeaGreen, point);            
        }

        internal class HandleMeasurements 
        {
            double maxHandleSpeed;
            int lastBall;
            CoursePassImage parent;

            internal HandleMeasurements(CoursePassImage image)
            {
                this.parent = image;
                //maxHandleSpeed = this.parent._pass.Measurements.Max(v => v.HandleSpeedMps);
                maxHandleSpeed = 1.0d;
                lastBall = 0;
            }

            internal void Draw(Measurement m, int i)
            {
                DrawHandleSpeed(m, i);
                if (m.InCourse)
                {
                    if (lastBall < Course.Balls.Length &&
                        m.HandlePosition.Y >= Course.Balls[lastBall].Y)
                    {
                        parent.DrawAngleAtBall(m);
                        lastBall++;
                    }        
                }            
            }

            void DrawHandleSpeed(Measurement m, int measurementIndex)
            {
                const double SpeedGraphHeight = 10.0d;
                double speedHeight = (Math.Abs(m.RopeSwingSpeedRadS) / maxHandleSpeed) * SpeedGraphHeight;
                bool accelerating = false;

                // if (speedHeight == SpeedGraphHeight)
                // {
                //     string maxSpeed = Math.Round(m.HandleSpeedMps * 2.23694, 1) + "mph";
                //     parent.DrawTextNearMeasurement(m, maxSpeed);
                // }

                if (measurementIndex > 0)
                {
                    double prevSpeed = parent._pass.Measurements[measurementIndex - 1].RopeSwingSpeedRadS;
                    accelerating = Math.Abs(m.RopeSwingSpeedRadS) >= Math.Abs(prevSpeed);
                }

                // Course.WidthM
                CoursePosition startPosition = new CoursePosition(Course.WidthM, m.HandlePosition.Y);
                CoursePosition endPosition = new CoursePosition(Course.WidthM - speedHeight, m.HandlePosition.Y);

                PointF startPoint = parent.PointFromCoursePosition(startPosition);
                PointF endPoint = parent.PointFromCoursePosition(endPosition);

                Color color = GetAcceleratingColor(accelerating);
                Pen pen = new Pen(color, 5.0f);

                parent._graphics.DrawLine(pen, startPoint, endPoint);

                // // Get the root mean square of the last 10 measurements.
                // double averageSpeed = parent._pass.Measurements.GetRange(measurementIndex-10, 10)
                //     .Select(s => s.HandleSpeedMps)
                //     .RootMeanSquare() * 2.23694;

                // string speed = Math.Round(averageSpeed, 1) + "mph";
                // parent.DrawTextNearMeasurement(m, speed);
            }

            private Color GetAcceleratingColor(bool accelerating)
            {
                // Make color slightly transparent.
                int alpha = 100;
                Color color = accelerating ? Color.Green : Color.Red;
                return Color.FromArgb(alpha, color);
            }
        }
    }
}
