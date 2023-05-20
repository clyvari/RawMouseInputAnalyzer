using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RawMouseInputAnalyzer
{
    internal partial class ResultView : Form
    {
        public ResultView(List<MousePoint> points)
        {
            InitializeComponent();

            Points = points;
        }

        public List<MousePoint> Points { get; }

        private void button1_Click(object sender, EventArgs e)
        {
            //var center = (X: pictureBox1.Width / 2, Y: pictureBox1.Height / 2);
            var center = (X: 50, Y: 50);
            var g = pictureBox1.CreateGraphics();
            var max = Points.Max(x => x.Count);
            var coeff = 255.0 / max;
            foreach (MousePoint point in Points)
            {
                var color = (int)(point.Count * coeff);
                g.FillRectangle(new SolidBrush(Color.FromArgb(color, 0, 255 - color)), point.X * 2 + center.X, point.Y * 2 + center.Y, 2, 2);
            }
        }
    }
}
