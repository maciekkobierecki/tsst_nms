using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NMS
{
    public partial class Form1 : Form
    {
        private Socket output_socket = null;
        private Socket inputSocket = null;
        public Command inCommand = new Command();
        public Command outCommand = new Command();
        private int outputPort;

        public Form1()
        {
            
            InitializeComponent();
            inputSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress ipAdd = IPAddress.Parse("127.0.0.1");
            IPEndPoint remoteEP = new IPEndPoint(ipAdd, 7386);
            inputSocket.Bind(remoteEP);
            if (!backgroundWorker1.IsBusy)
            {
                backgroundWorker1.RunWorkerAsync();
            }
        }

        private void update_b_Click(object sender, EventArgs e)
        {
            outCommand.agentId = "Add";
            outCommand.agentPort = (int)(comboBox1.SelectedItem);
            outCommand.inPort = Int32.Parse(textBox_ip.Text);
            outCommand.inLabel = Int32.Parse(textBox_il.Text);
            outCommand.outPort = Int32.Parse(textBox_op.Text);
            outCommand.outLabel = Int32.Parse(textBox_ol.Text);
            outputPort= (int)(comboBox1.SelectedItem);
            SendSingleCommand(outCommand);
        }
        private void delete_b_Click(object sender, EventArgs e)
        {
            outCommand.agentId = "Delete";
            outCommand.agentPort = (int)(comboBox1.SelectedItem);
            outCommand.inPort = Int32.Parse(textBox_ip.Text);
            outCommand.inLabel = Int32.Parse(textBox_il.Text);
            outCommand.outPort = Int32.Parse(textBox_op.Text);
            outCommand.outLabel = Int32.Parse(textBox_ol.Text);
            outputPort = (int)(comboBox1.SelectedItem);
            SendSingleCommand(outCommand);
        }

        /// <summary>
        /// //////////////////////////////////////////////////
        /// </summary>
        public void Listen()
        {
            
            inputSocket.Listen(0);
            Socket foreignSocket = null;
            while (true)
            {
                foreignSocket = inputSocket.Accept();
                Thread thread = new Thread(() => process(foreignSocket));
                thread.Start();
            }

        }

        private void process(Socket foreignSocket)
        {
            try
            {
                while (true)
                {
                    int readByte;
                    byte[] bytes = new byte[foreignSocket.SendBufferSize];
                    readByte = foreignSocket.Receive(bytes);
                    inCommand = GetDeserializedCommand(bytes);

                    if (inCommand.agentId != "KeepAlive")
                    {
                        outputPort = inCommand.agentPort;
                        comboBox1.Invoke(new Action(delegate ()
                        {
                            comboBox1.Items.Add(inCommand.agentPort);
                        }));

                        listBox2.Invoke(new Action(delegate ()
                        {
                            listBox2.Items.Add(inCommand.agentId + " " + inCommand.agentPort);
                            listBox2.SelectedIndex = listBox2.Items.Count - 1;
                        }));

                        if (inCommand.agentId != null)
                        {
                            ParseConfig();
                        }
                    }
                    else
                    {

                        listBox2.Invoke(new Action(delegate ()
                        {
                            listBox2.Items.Add("Keep alive");
                            listBox2.SelectedIndex = listBox2.Items.Count - 1;
                        }));
                    }
                }
            }catch(Exception e)
            {
                Listen();
            }
            

        }

        private void SendSingleCommand(Command cm)
        {
            Connect();
            output_socket.Send(GetSerializedCommand(cm));
            listBox1.Invoke(new Action(delegate ()
            {
                listBox1.Items.Add(cm.inPort + " " + cm.inLabel + " " + cm.outPort + " " + cm.outLabel + " " + outCommand.newLabel + " " + outCommand.removeLabel + " " + outCommand.ipAdress);
                listBox1.SelectedIndex = listBox1.Items.Count - 1;
            }));
        }

        private void Connect()
        {
            output_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress ipAdd = IPAddress.Parse("127.0.0.1");
            IPEndPoint remoteEP = new IPEndPoint(ipAdd, outputPort); //Int32.Parse(ConfigurationManager.AppSettings["output_port"]));
            output_socket.Connect(remoteEP);
        }


        private void ParseConfig()
        {
            string line;
            string[] integerStrings;

            StreamReader file = new StreamReader("C:\\Users\\Maciek\\Desktop\\tsst projekt1\\TSST\\tsst_nms\\NMS\\NMS\\bin\\Debug\\" + inCommand.agentId + ".txt");
            while ((line = file.ReadLine()) != null)
            {
                integerStrings = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                outCommand.agentId = "Add";
                outCommand.inPort = Int32.Parse(integerStrings[0]);
                outCommand.inLabel = Int32.Parse(integerStrings[1]);
                outCommand.outPort = Int32.Parse(integerStrings[2]);
                outCommand.outLabel = Int32.Parse(integerStrings[3]);
                outCommand.newLabel = Int32.Parse(integerStrings[4]);
                outCommand.removeLabel = Int32.Parse(integerStrings[5]);
                outCommand.ipAdress = integerStrings[6];
                SendSingleCommand(outCommand);
            }

        }


        private Command GetDeserializedCommand(byte[] b)
        {
            Command c = new Command();
            MemoryStream memStream = new MemoryStream();
            BinaryFormatter binForm = new BinaryFormatter();
            memStream.Write(b, 0, b.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            c = (Command)binForm.Deserialize(memStream);
            return c;
        }

        private byte[] GetSerializedCommand(Command com)    //Serializacja bajtowa
        {
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, com);
            return ms.ToArray();
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                Listen();
            }
            catch (IOException)
            {
                listBox1.Invoke(new Action(delegate ()
                {
                    listBox1.Items.Add("Problem with communication");
                }));
            }
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                MessageBox.Show("Processing cancelled", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message, "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(e.Result.ToString(), "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }















        private int GetPort()
        {
            int p = 0;
            for (int i = 1; i <= Config.getIntegerProperty("NbOfRouters"); i++)
            {
                if (inCommand.agentId == Config.getProperty("Agent" + i.ToString()))
                {
                    p = inCommand.agentPort;
                    outCommand.inPort = Config.getIntegerProperty("InPort" + i.ToString());
                    outCommand.inLabel = Config.getIntegerProperty("InLabel" + i.ToString());
                    outCommand.outPort = Config.getIntegerProperty("OutPort" + i.ToString());
                    outCommand.outLabel = Config.getIntegerProperty("OutLabel" + i.ToString());
                }
            }
            return p;
        }

    }
}
