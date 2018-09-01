using GeoCoordinatePortable;
using SlalomTracker;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class SlalomTrackerForm : Form
    {
        private CoursePass _pass;
        const int EntryMargin = 50;
        private int ScaleFactor = 4;
        private int CourseLength = 259 + (55 * 2);

        public SlalomTrackerForm()
        {
            InitializeComponent();
            this.Height = (CourseLength + (EntryMargin * 2)) * ScaleFactor;
            this.Load += SlalomTrackerForm_Load;
            this.Paint += SlalomTrackerForm_Paint;
            //this._panel1.Padding = new Padding(50);
            this._panel1.ForeColor = Color.White;
            this._panel1.BackColor = Color.White;
            this._panel1.Height = this.Height;
            _btnDraw.Click += _btnDraw_Click;
        }

        private void _btnDraw_Click(object sender, EventArgs e)
        {
            Rope rope = _cmbRopeM.SelectedItem as Rope;
            if (rope == null)
                throw new Exception("Must select a rope length.");

            try
            {
                DrawCoursePass(
                    rope.LengthM,
                    double.Parse(_txtRadS.Text),
                    double.Parse(_txtBoadSpeedMps.Text));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("Invalid entry.", ex.Message);
            }
        }

        private void _panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void SlalomTrackerForm_Paint(object sender, PaintEventArgs e)
        {
            
        }

        private Point PointFromCoursePosition(CoursePosition position)
        {
            // Converts relative course position X,Y coordiantes to a point on the drawable screen.

            if (position.Y < (-1*EntryMargin*ScaleFactor))
                return Point.Empty;

            int x = (int)(position.X * ScaleFactor);
            int y = (int)(position.Y * ScaleFactor);
            Trace.WriteLine(string.Format("Screen X: {0}, Y: {1}...Handle X: {2}, Y: {3}", 
                x, y, position.X, position.Y));
            return new Point(x, y);
        }

        private void SlalomTrackerForm_Load(object sender, EventArgs e)
        {
            //_panel1.VerticalScroll.Enabled = true;
            //_panel1.VerticalScroll.Minimum = 0;
            //_panel1.VerticalScroll.Maximum = CourseLength * ScaleFactor;
            //_panel1.VerticalScroll.Visible = true;
            _txtBoadSpeedMps.Validated += _txtBoadSpeedMps_Validated;

            _cmbRopeM.Items.AddRange(Rope.GetStandardLengths().ToArray());
            _cmbRopeM.SelectedIndex = 0;
        }

        private void _txtBoadSpeedMps_Validated(object sender, EventArgs e)
        {
            
        }

        private void _txtRadS_Validated(object sender, EventArgs e)
        {
            System.Diagnostics.Trace.WriteLine(_txtRadS.Text);
        }

        private void DrawCoursePass(double ropeM, double swingSpeedRadS, double boatSpeedRadS)
        {
            _pass = CoursePassTest.TestTrack(ropeM, swingSpeedRadS, boatSpeedRadS);
        }

        private void DrawCourseBounds(CoursePass pass)
        {
            List<GeoCoordinate> list = pass.Course.GetPolygon();
            // Convert geos to relative course positions, then to absolute screen points.
            List<Point> points = new List<Point>(list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                CoursePosition position = pass.CoursePositionFromGeo(list[i]);
                
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

            Graphics g = _panel1.CreateGraphics();
            Pen pen = new Pen(Color.Green, 0.6F);
            g.DrawLine(pen, points[0], points[1]);
            g.DrawLine(pen, points[0], points[2]);
            g.DrawLine(pen, points[2], points[3]);
            g.DrawLine(pen, points[3], points[1]);
        }

        private void DrawCourseFeature(Graphics graphics, Color color, CoursePosition[] positions)
        {
            Pen pen = new Pen(color, 2);
            foreach (var position in positions)
            {
                Point point = PointFromCoursePosition(position);
                graphics.DrawEllipse(pen, point.X, point.Y, 2, 2);
            }
        }

        private void Draw()
        { 
            Graphics graphics = _panel1.CreateGraphics();
            graphics.Clear(_panel1.BackColor);

            DrawCourseBounds(_pass);

            DrawCourseFeature(graphics, Color.Green, _pass.Course.PreGates);
            DrawCourseFeature(graphics, Color.Red, _pass.Course.Gates);
            DrawCourseFeature(graphics, Color.Red, _pass.Course.Balls);
            DrawCourseFeature(graphics, Color.Yellow, _pass.Course.BoatMarkers);

            // Draw Center Line.
            graphics.DrawLine(new Pen(Color.Gray, 1), new Point((int)11.5 * ScaleFactor, 0), 
                new Point((int)11.5 * ScaleFactor, CourseLength * ScaleFactor));

            Pen inCoursePen = new Pen(Color.Green, 3);
            Pen outCoursePen = new Pen(Color.Pink, 3);

            for (int i = 0; i < _pass.Measurements.Count-2; i++)
            {
                var m = _pass.Measurements[i];
                Pen coursePen = m.InCourse ? inCoursePen : outCoursePen;
                Point start = PointFromCoursePosition(m.HandlePosition);
                Point end = PointFromCoursePosition(_pass.Measurements[i + 1].HandlePosition);
                if (start != Point.Empty && end != Point.Empty)
                    graphics.DrawLine(coursePen, start, end);
            }
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            var open = new OpenFileDialog();
            //open.DefaultExt = ".csv";
            open.Multiselect = false;
            var result = open.ShowDialog();

            if (result != DialogResult.OK)
                return;

            //const string FilePath = @"\\files.local\video\GOPRO271.csv";
            string FilePath =  open.FileName;// @"\\files.local\video\GOPR0403.csv";
            _pass = CoursePassFromFile.Load(FilePath, 
                double.Parse(this._txtHeadingOffset.Text), 
                (Rope)this._cmbRopeM.SelectedItem);
            Draw();
        }

        private void _panel1_Paint_1(object sender, PaintEventArgs e)
        {

        }
    }
}
