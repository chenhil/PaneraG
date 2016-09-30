using DeathByCaptcha;
using System;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Text;

namespace PaneraG
{
    public partial class Form1 : Form
    {
        private HttpHelper httpHelper = null;
        public static Form1 _Form1;
        private int ThreadValue = 1;
        private bool ContinueRunning = true;
        private long giftcardNum = 6006491610789871616;

        private delegate void RichTextBoxUpdateEventHandler(string message);
        private delegate void ListViewUpdateEventHandler(string gcNum, string balance);

        public Form1()
        {
            InitializeComponent();
            httpHelper = new HttpHelper();
            _Form1 = this;
        }

        private async void Start_Button_Click(object sender, EventArgs e)
        {
            giftcardNum = Convert.ToInt64(CardNumber_textBox.Text);

            //Could spawn alot of threads here
            Task t1 = Start_Factory("Thread 1 ", giftcardNum);
            await Task.WhenAll(t1);
        }

        private void Stop_Button_Click(object sender, EventArgs e)
        {
            ContinueRunning = false;
            UpdateRichTextBox("Waiting for Threads to finish.");
            //need to implement this
        }

        private async Task Start_Factory(string name, long gcNum)
        {
            var tuple = await httpHelper.Start_Factory(name, gcNum);
            if (!tuple.Item2.Equals("0.00") && !tuple.Item2.Equals(""))
            {
                UpdateListViewBox(tuple.Item1, tuple.Item2);
            }
        }


        public void UpdateRichTextBox(string message)
        {
            string nDateTime = DateTime.Now.ToString("hh:mm:ss tt") + " - ";
            if (richTextBox1.InvokeRequired)
            {
                // this means we’re on the wrong thread!  
                // use BeginInvoke or Invoke to call back on the 
                // correct thread.
                richTextBox1.Invoke(
                    new RichTextBoxUpdateEventHandler(UpdateRichTextBox), // the method to call back on
                    new object[] { message });                              // the list of arguments to pass
            }
            else
            {
                richTextBox1.AppendText(nDateTime + message + System.Environment.NewLine);
                richTextBox1.ScrollToCaret();
            }
        }

        private void UpdateListViewBox(string gcNum, string balance)
        {
            if(listView1.InvokeRequired)
            {
                listView1.Invoke(
                    new ListViewUpdateEventHandler(UpdateListViewBox), 
                    new object[] { gcNum, balance });
            }
            else
            {
                ListViewItem itm = new ListViewItem(gcNum);
                itm.SubItems.Add(balance);
                listView1.Items.Add(itm);
            }
        }

        private void listView1_KeyUp(object sender, KeyEventArgs e)
        {
            if (sender != listView1) return;

            if (e.Control && e.KeyCode == Keys.C)
                CopySelectedValuesToClipboard();
        }

        private void CopySelectedValuesToClipboard()
        {
            var builder = new StringBuilder();
            foreach (ListViewItem item in listView1.SelectedItems)
                builder.AppendLine(item.SubItems[0].Text);

            Clipboard.SetText(builder.ToString());
        }
    }
}
