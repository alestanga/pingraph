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


            var nuovaserie = new System.Windows.Forms.DataVisualization.Charting.Series//configura il prototipo di serie
            {
                Name = textBox1.Text,//nome della serie
                Color = GetRandomColor(),//colore random
                IsVisibleInLegend = true,//lo mette in legenda
                IsXValueIndexed = false,//decide se i dati devono essere allineati per asse X
                ChartType = SeriesChartType.FastLine
        };
            serie.Add(textBox1.Text, nuovaserie);//aggiunge la serie al dizionario
            this.chart1.Series.Add(serie[textBox1.Text]);//aggiunge la serie al grafico

            th_ping.IsBackground = true;//inizializza e avvia il thread del ping
            th_ping.Start(textBox1.Text);
            listBox1.Items.Add(textBox1.Text);//aggiunge l'indirizzo alla lista
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
            while (true)//ciclo infinito di ping
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
            
            if (this.chart1.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetText);
                this.Invoke(d, new object[] { ip, ping, ok });
            }
            else
            {
                if (ok) {//ping risposto ok
                    //serie[ip].Points.AddXY(DateTime.Now.ToString(), ping);
                    i++;
                    serie[ip].Points.AddXY(i, ping);//inserisce il punto sul grafico
                    chart1.Invalidate();
                    serie[ip].Sort(PointSortOrder.Ascending, "X");//ordinamento per asse X
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
            serie = new Dictionary<string, System.Windows.Forms.DataVisualization.Charting.Series>();//inizializza il dizzionario delle serie
            serie.Clear();//lo svuota
            chart1.Series.Clear();//svuota il grafico
            chart1.ChartAreas[0].AxisX.Title = "X";//nome assi
            chart1.ChartAreas[0].AxisY.Title = "Y";//nome assi
        }

        private Color GetRandomColor()//ritorna un colore random
        {
            Random random = new Random();
            return Color.FromArgb(random.Next(0, 255), random.Next(0, 255), random.Next(0, 255));
        }
    }
}
