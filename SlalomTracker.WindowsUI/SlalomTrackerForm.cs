using SlalomTracker;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class SlalomTrackerForm : Form
    {
        private int ScaleFactor = 4;
        private int CourseLength = 259;

        public SlalomTrackerForm()
        {
            InitializeComponent();
            this.Height = CourseLength * ScaleFactor;
            this.Load += SlalomTrackerForm_Load;
            this.Paint += SlalomTrackerForm_Paint;
            _btnDraw.Click += _btnDraw_Click;
        }

        private void _btnDraw_Click(object sender, EventArgs e)
        {
            try
            {
                DrawCoursePass(
                    double.Parse(_cmbRopeM.Text),
                    double.Parse(_txtRadS.Text),
                    double.Parse(_txtBoadSpeedMps.Text));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("Invalid entry.");
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
            // Start drawing at the bottom of the screen and go up.
            int x = (int)(position.X * ScaleFactor);
            int y = (CourseLength * ScaleFactor) - (int)(position.Y * ScaleFactor);
            System.Diagnostics.Trace.WriteLine(x + ", " + y);
            return new Point(x, y);
        }

        private void SlalomTrackerForm_Load(object sender, EventArgs e)
        {
            //_panel1.VerticalScroll.Enabled = true;
            //_panel1.VerticalScroll.Minimum = 0;
            //_panel1.VerticalScroll.Maximum = CourseLength * ScaleFactor;
            //_panel1.VerticalScroll.Visible = true;
            _txtBoadSpeedMps.Validated += _txtBoadSpeedMps_Validated;
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
            CoursePassTest test = new CoursePassTest();
            CoursePass mockPass = test.TestTrack(ropeM, swingSpeedRadS, boatSpeedRadS);
            Draw(mockPass);
        }

        private void Draw(CoursePass pass)
        { 
            _panel1.Height = CourseLength * ScaleFactor;
            Graphics graphics = _panel1.CreateGraphics();
            Pen pen = new Pen(Color.Black, 3);
            Pen inCoursePen = new Pen(Color.Green, 3);
            Pen outCoursePen = new Pen(Color.Pink, 3);
            graphics.Clear(_panel1.BackColor);

            Pen ballPen = new Pen(Color.Red, 2);

            foreach (var ball in pass.Course.Gates)
            {
                graphics.DrawEllipse(ballPen, (int)(ball.X * ScaleFactor), (int)ball.Y * ScaleFactor, 2, 2);
            }

            foreach (var ball in pass.Course.Balls)
            {
                graphics.DrawEllipse(ballPen, (int)(ball.X * ScaleFactor), (int)ball.Y * ScaleFactor, 2, 2);
            }

            // Draw Center Line.
            graphics.DrawLine(new Pen(Color.Gray, 1), new Point((int)11.5 * ScaleFactor, 0), 
                new Point((int)11.5 * ScaleFactor, CourseLength * ScaleFactor));

            for (int i = 0; i < pass.Measurements.Count-2; i++)
            {
                var m = pass.Measurements[i];
                Pen coursePen = m.InCourse ? inCoursePen : outCoursePen;
                graphics.DrawLine(coursePen, PointFromCoursePosition(m.HandlePosition),
                    PointFromCoursePosition(pass.Measurements[i+1].HandlePosition));
            }
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            const string FilePath = @"\\files.local\video\GOPRO271.csv";
            CoursePass pass = CoursePassFromCSV.Load(FilePath);
            Draw(pass);
        }
    }
}
