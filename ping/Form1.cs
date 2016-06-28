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

        List<string> p_tempo;

        public class punto//oggetto dei punti con le proprietà
        {
            public string p_ip { get; set; }
            public long? p_ping { get; set; }//permette valori null
        }

        List<punto> lista_punti;//lista dei punti
        Dictionary<string, System.Windows.Forms.DataVisualization.Charting.Series> serie;//dizionario delle serie

        delegate void SetTextCallback(string ip, long ping, bool ok);//funzione per aggiornare il grafico

        Point? prevPosition = null;
        ToolTip tooltip = new ToolTip();

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
                IsXValueIndexed = true,//decide se i dati devono essere allineati per asse X
                MarkerStyle = MarkerStyle.Circle,
                ChartType = SeriesChartType.Line
            };
            nuovaserie.EmptyPointStyle.BorderDashStyle = ChartDashStyle.Solid; // select the style you want
            nuovaserie.EmptyPointStyle.Color = nuovaserie.Color; // setting the color might be required
            nuovaserie["EmptyPointValue"] = "Average";

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
                Thread.Sleep(5000);//intervallo in ms
            }
        }


        private void SetText(string ip, long ping, bool ok)
        {

            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            string tempo = DateTime.Now.ToString();
            if (this.chart1.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetText);
                this.Invoke(d, new object[] { ip, ping, ok });
            }
            else
            {
                int i=0;
                if (ok) {//ping risposto ok
                    p_tempo.Add(tempo);
                    foreach (string elemento in listBox1.Items)//controlla tutti gli indirizzi sotto esame
                    {
                        if (elemento == ip)
                        {//aggiunge il valore del ping
                            lista_punti.Add(new punto { p_ip = elemento, p_ping = ping });
                        }
                        else
                        {//imposta a null i valori di ping per gli indirizzi non pingati da questo thread
                            lista_punti.Add(new punto { p_ip = elemento, p_ping = null });
                        }
                    }

                    chart1.Series.Clear();//elimina le serie dal grafico

                    foreach (string elemento in listBox1.Items)
                    {//reinserisce le serie
                        serie[elemento].Points.Clear();//svuota i punti della serie
                        ILookup < string, punto> l_ping = lista_punti.ToLookup(punto => punto.p_ip);//cerca i ping di un dato indirizzo
                        int diff = p_tempo.Count() - l_ping[elemento].Count();//confronta quanti sono rispetto ai ping totali di tutti gli indirizzi
                        
                        for (i=0;i<diff;i++)
                        {//aggiunge all'inizio dell'elenco dei ping i valori a null per allineare le nuove serie alle vecchie
                            lista_punti.Insert(0, new punto { p_ip = elemento, p_ping = null });
                        }

                        l_ping = lista_punti.ToLookup(punto => punto.p_ip);//reinizializza la serie dei ping dopo averla allineata

                        long?[] a_punto=new long?[0];
                        i = 0;

                        foreach (punto t_punto in l_ping[elemento])
                        {//converte in array la lista di ping ILookup, si potrà fare in modo più efficiente?
                            Array.Resize(ref a_punto, a_punto.Length + 1);
                            a_punto[i] = t_punto.p_ping;
                            i++;
                        }

                        this.chart1.Series.Add(serie[elemento]);//aggiunge la serie al grafico
                        this.chart1.Series[elemento].Points.DataBindXY(p_tempo, a_punto);//collega la lista dei tempi e l'array dei ping al grafico
                        serie[elemento].Sort(PointSortOrder.Ascending, "X");//ordinamento per asse X
                    }
                    chart1.Invalidate();
                }
                else
                {//timeout
                    
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            serie = new Dictionary<string, System.Windows.Forms.DataVisualization.Charting.Series>();//inizializza il dizzionario delle serie

            p_tempo = new List<string>();//inizializza le serie
            lista_punti = new List<punto>();

            serie.Clear();//lo svuota
            chart1.Series.Clear();//svuota il grafico
            chart1.ChartAreas[0].AxisX.Title = "Istante";//nome assi
            chart1.ChartAreas[0].AxisY.Title = "Ping";//nome assi
        }

        private Color GetRandomColor()//ritorna un colore random
        {
            Random random = new Random();
            return Color.FromArgb(random.Next(0, 255), random.Next(0, 255), random.Next(0, 255));
        }

        private void chart1_MouseMove(object sender, MouseEventArgs e)
        {
            var pos = e.Location;
            if (prevPosition.HasValue && pos == prevPosition.Value)
                return;
            tooltip.RemoveAll();
            prevPosition = pos;
            var results = chart1.HitTest(pos.X, pos.Y, false,
                                            ChartElementType.DataPoint);
            foreach (var result in results)
            {
                if (result.ChartElementType == ChartElementType.DataPoint)
                {
                    var prop = result.Object as DataPoint;
                    if (prop != null)
                    {
                        var pointXPixel = result.ChartArea.AxisX.ValueToPixelPosition(prop.XValue);
                        var pointYPixel = result.ChartArea.AxisY.ValueToPixelPosition(prop.YValues[0]);

                        // check if the cursor is really close to the point (5 pixels around the point)
                        if (Math.Abs(pos.X - pointXPixel) < 5 &&
                            Math.Abs(pos.Y - pointYPixel) < 5)
                        {
                            tooltip.Show("X=" + prop.XValue + ", Ping: " + prop.YValues[0], this.chart1,
                                            pos.X, pos.Y - 15);
                        }
                    }
                }
            }
        }
    }
}
