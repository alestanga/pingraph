using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.NetworkInformation;
using System.Threading;
using System.Windows.Forms.DataVisualization.Charting;

namespace ping
{
    public partial class Form1 : Form
    {
        delegate void SetTextCallback(string ip, long ping, bool ok);
        Dictionary<string, System.Windows.Forms.DataVisualization.Charting.Series> serie;
        int i;

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Thread th_ping = new Thread (new ParameterizedThreadStart(fun_ping));


            var nuovaserie = new System.Windows.Forms.DataVisualization.Charting.Series
            {
                Name = textBox1.Text,
                Color = GetRandomColor(),
                IsVisibleInLegend = true,
                IsXValueIndexed = false,
                ChartType = SeriesChartType.FastLine
        };
            serie.Add(textBox1.Text, nuovaserie);//aggiunge la serie alla lista

            th_ping.IsBackground = true;
            th_ping.Start(textBox1.Text);
            listBox1.Items.Add(textBox1.Text);

            this.chart1.Series.Add(serie[textBox1.Text]);
        }

        private void fun_ping(object parametri)
        {
            Ping pingSender = new Ping();
            PingOptions options = new PingOptions();
            // Use the default Ttl value which is 128,
            // but change the fragmentation behavior.
            options.DontFragment = true;

            // Create a buffer of 32 bytes of data to be transmitted.
            string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            byte[] buffer = Encoding.ASCII.GetBytes(data);
            int timeout = 10000; //millisecondi
            while (true)
            {
                PingReply reply = pingSender.Send(parametri.ToString(), timeout, buffer, options);
                if (reply.Status == IPStatus.Success)//risposta ricevuta
                {
                    this.SetText(reply.Address.ToString(), reply.RoundtripTime, true);
                }
                else
                {//timeout
                    this.SetText(reply.Address.ToString(), reply.RoundtripTime, false);
                }
                Thread.Sleep(1000);//intervallo in ms
            }
        }


        private void SetText(string ip, long ping, bool ok)
        {

            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            
            if (this.listBox2.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetText);
                this.Invoke(d, new object[] { ip, ping, ok });
            }
            else
            {
                if (ok) {//ping risposto ok
                    this.listBox2.Items.Add(ip + " " + ping.ToString());
                    //serie[ip].Points.AddXY(DateTime.Now.ToString(), ping);
                    i++;
                    serie[ip].Points.AddXY(i, ping);
                    chart1.Invalidate();
                    serie[ip].Sort(PointSortOrder.Ascending, "X");
                    //chart1.AlignDataPointsByAxisLabel();
                }
                else
                {//timeout
                    
                }
                //chart1.AlignDataPointsByAxisLabel();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            serie = new Dictionary<string, System.Windows.Forms.DataVisualization.Charting.Series>();
            serie.Clear();
            chart1.Series.Clear();
            chart1.ChartAreas[0].AxisX.Title = "X";
            chart1.ChartAreas[0].AxisY.Title = "Y";
        }

        private Color GetRandomColor()//ritorna un colore random
        {
            Random random = new Random();
            return Color.FromArgb(random.Next(0, 255), random.Next(0, 255), random.Next(0, 255));
        }
    }
}
