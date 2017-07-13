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
        Brush blackBrush = Brushes.Black;
        ColorConverter converter = new ColorConverter();
        dynamic pos = new object();//new { x = 50, y = 50, color = "" };
        private Random rnd = new Random();
        public Form1()
        {
            InitializeComponent();
        }
        FireHive.Hive current;
        private void Form1_Load(object sender, EventArgs e)
        {
            current = FireHive.Hive.Current;
            current.Initialized += Current_Initialized;
        }

        private void Current_Initialized(object sender, EventArgs e)
        {
            this.Invoke(new MethodInvoker(() =>
            {
                if (current.keys().Contains("SquareDemoPosition") && current.Get("SquareDemoPosition") != null)
                {
                    rectangles = current.Get("SquareDemoPosition");
                    pos = current.set("temporal", pos);
                    pos.color = getRandomColor();
                    pos.x = 50;
                    pos.y = 50;
                    rectangles.Add(pos);
                    timer2.Interval = 10;
                    panel1.MouseMove += Panel1_MouseMove;
                    timer2.Enabled = true;
                    timer1.Enabled = true;
                }
            }));


        }

        private void Panel1_MouseMove(object sender, MouseEventArgs e)
        {
            pos.x = e.X;
            pos.y = e.Y;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            pos = current.Get("temporal");
            pos.x = 50;
            pos.y = 50;

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            label4.Text = pos.x.ToString() + ":" + pos.y.ToString();
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

        private string getRandomColor()
        {
            Color randomColor = Color.FromArgb(rnd.Next(256), rnd.Next(256), rnd.Next(256));
            return HexConverter(randomColor);
        }

        private void timer2_Tick(object sender, EventArgs e)
        {

            var g = panel1.CreateGraphics();
            g.FillRectangle(blackBrush, 0, 0, 500, 500);
            foreach (dynamic item in rectangles)
            {
                if (item.x != null && item.y != null)
                    g.FillRectangle(new SolidBrush((Color)converter.ConvertFromString(item.color)), item.x - 5, item.y - 5, 10, 10);
            }

        }
        private String HexConverter(System.Drawing.Color c)
        {
            return "#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
        }


    }
}
