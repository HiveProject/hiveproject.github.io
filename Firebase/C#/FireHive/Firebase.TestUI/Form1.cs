using Firebase.Data.Changeset;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Firebase.TestUI
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        Firebase.FirebaseClient client;
        private void Form1_Load(object sender, EventArgs e)
        {
            client = new Firebase.FirebaseClient("https://hive-1336.firebaseio.com/");
            client.On("objects", Firebase.SubscribeOperations.Added, dataAdded);
            client.On("objects", Firebase.SubscribeOperations.Changed, dataChanged);
            client.On("objects", Firebase.SubscribeOperations.Removed, dataRemoved);
        }

        private void dataAdded(string key, ChangeSet data)
        { }
        private void dataChanged(string key, ChangeSet data)
        { }
        private void dataRemoved(string key, ChangeSet data)
        { }
         
    }
}
