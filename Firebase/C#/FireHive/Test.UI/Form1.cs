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
        dynamic pos = new { x = 50, y = 50 };
        public Form1()
        {
            InitializeComponent();
        }
        FireHive.Hive current;
        private void Form1_Load(object sender, EventArgs e)
        {
            current = FireHive.Hive.Current;

            var t = new Rectangle(10, 10);
            dynamic m = new FireHive.Dynamic.ExpandibleObject(t);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var n = current.Get("a");
            var p = current.set("rect", new Rectangle(10, 10));

            var first = current.set("recursive", new RecursiveStructure()
            { Name = "first" });

            var second = current.set("recursive2", new RecursiveStructure()
            { Name = "second", Child = first });
            first.Child = second;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
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
        private void timer2_Tick(object sender, EventArgs e)
        {
            if (drawingStarted)
            {
                var g = panel1.CreateGraphics();
                
                g.FillRectangle(blackBrush, 0, 0, 500, 500);
                g.FillRectangle(redBrush, pos.x - 5, pos.y - 5, 10, 10); 

            }
            else
            {
                if (current.keys().Contains("SquareDemoPosition")) { pos = current.Get("SquareDemoPosition"); }
                else
                {
                    pos = current.set("SquareDemoPosition", pos);
                }
                drawingStarted = true;
                timer2.Interval = 10;
            }
        }
    }
}
