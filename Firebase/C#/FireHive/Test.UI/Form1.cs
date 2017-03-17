using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Test.UI
{
    public partial class Form1 : Form
    {
        dynamic rectangles = new List<dynamic>();
        public Form1()
        {
            InitializeComponent();
        }
        FireHive.Hive current;
        private void Form1_Load(object sender, EventArgs e)
        {
            current = FireHive.Hive.Current;
            panel1.MouseMove += panel1_MouseMove;
        }

        private void button1_Click(object sender, EventArgs e)
        {

            pos = current.Get("temporal");
            // pos.color = getRandomColor();
            pos.x = 50;
            // pos.y = 50;
            drawingStarted = true;
            //var n = current.Get("a");
            //var p = current.set("rect", new Rectangle(10, 10));
            //var currentArea = p.Area();
            //p.depth = 200;

            //var arr = current.Get("arr");
            //var first = current.set("recursive", new RecursiveStructure()
            //{ Name = "first" });

            //var second = current.set("recursive2", new RecursiveStructure()
            //{ Name = "second", Child = first });
            //first.Child = second;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (drawingStarted)
            { label4.Text = pos.x.ToString() + ":" + pos.y.ToString(); }
            var value = current.Get("a");
            var count = current.keys().Count();
            label3.Text = count.ToString();
            if (value == null)
            {
                label1.Text = "null";
            }
            else
            {

                label1.Text = value.ToString();
            }

        }
        bool drawingStarted = false;
        Brush blackBrush = Brushes.Black;
        Brush redBrush = Brushes.Red;
        ColorConverter converter = new ColorConverter();
        dynamic pos = new object();//new { x = 50, y = 50, color = "" };
        private Random rnd = new Random();
        private string getRandomColor()
        {
            Color randomColor = Color.FromArgb(rnd.Next(256), rnd.Next(256), rnd.Next(256));
            return HexConverter(randomColor);
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            if (drawingStarted)
            {
                var g = panel1.CreateGraphics();
                g.FillRectangle(blackBrush, 0, 0, 500, 500);
                foreach (dynamic item in rectangles)
                {
                    if (item.x != null && item.y != null)
                        g.FillRectangle(new SolidBrush((Color)converter.ConvertFromString(item.color)), item.x - 5, item.y - 5, 10, 10);
                }
            }
            else
            {
                //pos.color = getRandomColor();
                if (current.keys().Contains("SquareDemoPosition")) { rectangles = current.Get("SquareDemoPosition"); }
                else
                {
                    rectangles = current.set("SquareDemoPosition", rectangles);
                }
                pos = current.set("temporal", pos);
                pos.color = getRandomColor();
                pos.x = 50;
                pos.y = 50;
                rectangles.Add(pos);
                drawingStarted = true;
                timer2.Interval = 10;
            }
        }
        private String HexConverter(System.Drawing.Color c)
        {
            return "#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
        }

        private void panel1_MouseMove(object sender, MouseEventArgs e)
        {
            if (drawingStarted)
            {
                pos.x = e.X;
                pos.y = e.Y;
            }
        }
    }
}
