using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SQLite;
using System.Threading;

namespace Who_sDeffingMyFarms
{
    public partial class Form1 : Form
    {

        public string sql;
        public SQLiteConnection con;
        public SQLiteCommand command;

        Boolean webchecked = false;
        Boolean firstCheck = true;

        public static List<Deffers> DefList = new List<Deffers>();
        public static List<Deffers> DefList2 = new List<Deffers>();
        public static List<DeffDiff> DiffList = new List<DeffDiff>();
        public int PageNum=0;
        public string serverlink = "tx3.travian.com.hr";
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            serverlink = textBox1.Text;
            webBrowser1.Navigate(textBox1.Text+ "/statistiken.php?id=0&idSub=2&page=1");
        }

        private void button2_Click(object sender, EventArgs e) //CHECK NUM
        {
            checking();
            PageNum = Convert.ToInt16(numericUpDown1.Value);
            Thread chck = new Thread(new ThreadStart(Check));
            chck.Start();
        }

        public void GoTo(int num) {
            webBrowser1.Navigate(serverlink + "/statistiken.php?id=0&idSub=2&page="+num);
        }

        public void checking() {
            //DefList.Clear();
            button2.Enabled = false;
            button4.Enabled = false;
        }

        private void button4_Click(object sender, EventArgs e) //CHECK ALL
        {
            checking();
            HtmlElementCollection b = webBrowser1.Document.GetElementsByTagName("a");
            int last = 0;
            bool locked = false;
            for (int i = 0; i < b.Count; i++)
            {
                if (locked)
                {
                    last = Convert.ToInt16(b[i].InnerHtml);
                    break;
                }
                try
                {
                    if (Convert.ToInt16(b[i].InnerHtml) == 2)
                    {
                        locked = true;
                    }
                }
                catch { }

            }
            PageNum = last;
            Thread chck = new Thread(new ThreadStart(Check));
            chck.Start();
        }

        private void DoInvoke(MethodInvoker del)
        {
            if (InvokeRequired) { Invoke(del); }
            else { del(); }
        }

        public void Check() {
            DoInvoke(delegate { CheckPage(); });
            for (int i = 2; i <= PageNum; i++)
            {
                Console.WriteLine("GoTo page " + i);
                webchecked = false;
                DoInvoke(delegate { GoTo(i); });
                Random rnd = new Random();
                while (!webchecked) {
                    Thread.Sleep(rnd.Next(30, 100));
                }
                Thread.Sleep(rnd.Next(10, 30));
                DoInvoke(delegate { CheckPage(); });
                webchecked = false;
                Thread.Sleep(rnd.Next(10, 30));
            }
            try
            {
                DoInvoke(delegate { Done(); });
                database();
            }
            catch { Console.WriteLine("error"); }            
        }

        public void Done() {
            if (!firstCheck)
            {
                try {
                    foreach (Deffers acc in DefList)
                {
                    Deffers deffer = DefList2.First(o => o.name.Equals(acc.name));
                    if (deffer != null) {
                        int deffpoints2 = deffer.deffPoints;
                        if (!(deffpoints2 == acc.deffPoints))
                        {
                            DiffList.Add(new DeffDiff
                            {
                                name = acc.name,
                                Diff = deffpoints2 - acc.deffPoints
                            });
                        }
                    }
                }
                UpdateDiffTable();
                } catch {
                }
                
            }
            else {
                label2.Text = "Finished";
            }
            UpdateBox();
            button5.Enabled = true;
            label2.Text = "This is 2.check";
            firstCheck = false;
            GoTo(1);
        }

        public void CheckPage() {
            HtmlElementCollection b = webBrowser1.Document.GetElementsByTagName("tr");

            for (int i = 1; i < b.Count-1; i++)
            {
                HtmlElementCollection TD = b[i].GetElementsByTagName("td");
                if (!String.IsNullOrEmpty(TD[1].InnerText))
                {
                    if (firstCheck)
                    {
                        DefList.Add(new Deffers
                        {
                            rang = Convert.ToInt16(TD[0].InnerText.Trim('.')),
                            //name = TD[1].InnerText.Trim(' ').Replace(' ', '_').Replace('.', '_').Replace('-', '_'),
                            name = TD[1].InnerText,
                            pop = Convert.ToInt32(TD[2].InnerText),
                            deffPoints = Convert.ToInt32(TD[4].InnerText)
                        });
                        Console.WriteLine("deffer name: "+TD[1].InnerText);
                    }
                    else
                    {
                        DefList2.Add(new Deffers
                        {
                            rang = Convert.ToInt16(TD[0].InnerText.Trim('.')),
                            //name = TD[1].InnerText.Trim(' ').Replace(' ', '_').Replace('.', '_').Replace('-', '_'),
                            name = TD[1].InnerText,
                            pop = Convert.ToInt32(TD[2].InnerText),
                            deffPoints = Convert.ToInt32(TD[4].InnerText)
                        });
                    }
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            UpdateBox();
        }
        public void UpdateBox() {
            richTextBox1.Text = "";
            foreach (Deffers a in DefList)
            {
                richTextBox1.AppendText(a.rang + ". " + a.name + " - " + a.deffPoints + "\n");
            }
        }
        public void database() {
            con = new SQLiteConnection("Data Source = DB.sqlite; Version = 3");
            con.Open();
            foreach (Deffers acc in DefList) {
                sql = "CREATE TABLE IF NOT EXISTS "+acc.name.Trim(' ').Replace(' ', '_').Replace('.', '_').Replace('-', '_')+" (timestamp DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP, points NUMERIC, pop NUMERIC, rang NUMERIC)";
                command = new SQLiteCommand(sql, con);
                command.ExecuteNonQuery();
                sql = "insert into "+acc.name+"(points, pop, rang) values('" + acc.deffPoints + "','" + acc.pop + "','" + acc.rang + "')";
                command = new SQLiteCommand(sql, con);
                command.ExecuteNonQuery();
            }
            con.Close();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Thread chck = new Thread(new ThreadStart(Check));
            chck.Start();
        }

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            webchecked = true;
        }

        //----------------------CLASSES-----------------
        public class Deffers
        {
            public string name { get; set; }
            public int deffPoints { get; set; }
            public int pop { get; set; }
            public int rang { get; set; }
        }

        public class DeffDiff
        {
            public string name { get; set; }
            public int Diff { get; set; }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button6_Click(object sender, EventArgs e)
        {
            UpdateDiffTable();
        }

        private void UpdateDiffTable() {
            richTextBox2.Text = "";
            List<DeffDiff> SortedDif = DiffList.OrderBy(o => o.Diff).ToList();
            foreach (DeffDiff o in SortedDif)
            {
                richTextBox2.AppendText(o.name + ": " + o.Diff + "\n");
            }
        }
    }

}
