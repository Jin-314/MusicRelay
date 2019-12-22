using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.IO.Ports;

namespace MusicRelay
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private Button[] buttonArray;
        private Label[] labelArray;
        private List<string>ompu = new List<string>();
        private List<int[]> Data = new List<int[]>();
        private List<string> files = new List<string>();
        private List<Onkai> onkai = new List<Onkai>();
        private string tmp = "";
        private int partsNum = 0;
        private int counter = 0;
        private uint delay = 0;
        private bool serial_end = false;
        private CancellationTokenSource _s = null;

        private void start_serial(object sender, EventArgs e)
        {
            if (serialPort1.IsOpen)
            {
                SerialStop();
            }
            else
            {
                try
                {
                    SerialStart();
                    button1.Text = "disConnect";
                    label2.Text = "Connected";
                    comboBox1.Enabled = false;
                    button2.Enabled = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }                
            }
        }
        public object GetControlArrayByName(Form frm, string name)
        {
            System.Collections.ArrayList ctrs =
                new System.Collections.ArrayList();
            object obj;
            for (int i = 1;
                (obj = FindControlByFieldName(frm, name + i.ToString())) != null; i++)
                ctrs.Add(obj);
            if (ctrs.Count == 0)
                return null;
            else
                return ctrs.ToArray(ctrs[0].GetType());
        }
        public static object FindControlByFieldName(Form frm, string name)
        {
            System.Type t = frm.GetType();
            System.Reflection.FieldInfo fi = t.GetField(name, System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.DeclaredOnly);
            if (fi == null)
                return null;
            return fi.GetValue(frm);
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            this.AutoScroll = true;
            this.AutoScrollMargin = new Size(10, 10);
            this.AutoScrollMinSize = new Size(100, 100);
            this.AutoScrollPosition = new Point(-50, 50);
            buttonArray = (Button[])GetControlArrayByName(this, "button");
            labelArray = (Label[])GetControlArrayByName(this, "label");

            string[] PortList = SerialPort.GetPortNames();

            comboBox1.Items.Clear();
            foreach (string PortName in PortList)
            {
                comboBox1.Items.Add(PortName);
            }
            if (comboBox1.Items.Count > 0)
            {
                comboBox1.SelectedIndex = 0;
            }
            buttonClear();
            counter = 0;
            StartButton.Enabled = false;
            StopButton.Enabled = false;
            NextButton.Enabled = false;
            ReturnButton.Enabled = false;
        }

        private void button_Clicked(object sender, EventArgs e)
        {
            if (this.Enabled)
            {
                int index = -1;
                for(int i = 0; i < buttonArray.Length; i++)
                {
                    if (buttonArray[i].Equals(sender))
                    {
                        index = i;
                        break;
                    }
                }
                if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string filePath = openFileDialog1.FileName;
                    labelArray[index + 2].Text = Path.GetFileNameWithoutExtension(filePath);
                    if(index > -1)
                    {
                        if (files.Count > index - 1)
                        {
                            files.RemoveAt(index - 1);
                            files.Insert(index - 1, filePath);
                        }
                        else
                        {
                            files.Add(filePath);
                        }
                        if(index < 10)
                        {
                            StartButton.Enabled = true;
                            buttonArray[index + 1].Enabled = true;
                            NextButton.Enabled = true;
                            ReturnButton.Enabled = true;
                        }
                    }
                }
            }
        }
        private void StartButton_Click(object sender, EventArgs e)
        {
            if (StartButton.Enabled)
            {
                try
                {
                    StartButton.Enabled = false;
                    StopButton.Enabled = true;
                    buttonClear();
                    serial_end = false;
                    _s = new CancellationTokenSource();
                    if(counter < 0)
                    {
                        counter = 0;
                    }
                    else if(counter >= files.Count)
                    {
                        labelResetColor();
                        counter = 0;
                        ArrayClear();
                        buttonEnable();
                        StartButton.Enabled = true;
                        StopButton.Enabled = false;
                    }
                    else
                    {
                        StreamReader sr1 = new StreamReader(files[counter], Encoding.GetEncoding("SHIFT_JIS"));
                        StreamReader sr2 = new StreamReader("OnkaiData.txt", Encoding.GetEncoding("UTF-8"));
                        labelResetColor();
                        labelSetColor();
                        onkaiLoad(sr2);
                        FileEncoder(sr1);
                        SerialDate(_s.Token);
                    }
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }
        private void StopButton_Click(object sender, EventArgs e)
        {
            if (StopButton.Enabled)
            {
                try
                {
                    TaskCancel();
                    ArrayClear();
                    buttonEnable();
                    labelResetColor();
                    StopButton.Enabled = false;
                    StartButton.Enabled = true;
                    counter = 0;
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }
        private void NextButton_Click(object sender, EventArgs e)
        {
            if (NextButton.Enabled)
            {
                ArrayClear();
                TaskCancel();
                StartButton.Enabled = true;
                counter++;
                Thread.Sleep(500);
                StartButton.PerformClick();
            }
        }
        private void ReturnButton_Click(object sender, EventArgs e)
        {
            if (ReturnButton.Enabled)
            {
                TaskCancel();
                ArrayClear();
                StartButton.Enabled = true;
                counter--;
                Thread.Sleep(500);
                StartButton.PerformClick();
            }
        }
        private void SerialStart()
        {
            serialPort1.BaudRate = 115200;
            serialPort1.PortName = comboBox1.SelectedItem.ToString();
            serialPort1.Open();
        }
        private void FileEncoder(StreamReader sr)
        {
            try
            {
                string line;
                List<int> buff1 = new List<int>();
                List<string> list = new List<string>();
                List<string> parts = new List<string>();
                while ((line = sr.ReadLine()) != null)
                {
                    list.Add(line);
                }
                partsNum = list.Count();
                int i = 0;
                foreach (string row in list)
                {
                    switch (i)
                    {
                        case 0:
                            tmp = row;
                            i++;
                            continue;
                        case 1:
                            ompu.AddRange(row.Split('\t'));
                            i++;
                            continue;
                        default:
                            parts.AddRange(row.Split('\t'));
                            break;
                    }
                    foreach (string str in parts)
                    {
                        float num = 1;
                        int pos = -1;
                        string name = str;
                        while((pos = str.IndexOf('t', pos + 1)) != -1)
                        {
                            num /= 2;
                            name = str.Remove(0, pos + 1);
                        }
                        while((pos = str.IndexOf('h', pos + 1)) != -1)
                        {
                            num *= 2;
                            name = str.Remove(0, pos + 1);
                        }
                        int result = 
                            (onkai.Find(m => m.Name == name) == null) ? 0 : (int)(onkai.Find(m => m.Name == name).Value * num);
                        buff1.Add(result);
                        i++;
                    }
                    int[] buff2 = new int[buff1.Count];
                    int j = 0;
                    foreach(int buff in buff1)
                    {
                        buff2[j] = buff;
                        j++;
                    }
                    Data.Add(buff2);
                    parts.Clear();
                    buff1.Clear();
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
            finally
            {
                sr.Close();
            }
        }
        private void onkaiLoad(StreamReader sr)
        {
            string line;
            List<string> list = new List<string>();
            List<string[]> otoData = new List<string[]>();
            List<int> vals = new List<int>();
            while ((line = sr.ReadLine()) != null)
            {
                list.Add(line);
            }
            otoData.Add(list[0].Split(','));
            otoData.Add(list[1].Split(','));
            int j = 0;
            foreach(string val in otoData[1])
            {
                vals.Add(int.Parse(val));
                j++;
            }
            j = 0;
            foreach(string name in otoData[0])
            {
                Onkai oto = new Onkai();
                oto.Name = name;
                oto.Value = vals[j];
                onkai.Add(oto);
                j++;
            }
            sr.Close();
        }
        private async void SerialDate(CancellationToken token)
        {
            try
            {
                int i = 0;
                foreach (string str in ompu)
                {
                    int _ompu = int.Parse(str);
                    String text = "";
                    switch (_ompu)
                    {
                        case 32:
                            delay = 60 * 1000 / uint.Parse(tmp) / 8;
                            break;
                        case 16:
                            delay = 60 * 1000 / uint.Parse(tmp) / 4;
                            break;
                        case 12:
                            delay = 60 * 1000 / uint.Parse(tmp) / 3;
                            break;
                        case 8:
                            delay = 60 * 1000 / uint.Parse(tmp) / 2;
                            break;
                        case 4:
                            delay = 60 * 1000 / uint.Parse(tmp);
                            break;
                        case 2:
                            delay = 60 * 1000 / uint.Parse(tmp) * 2;
                            break;
                        case 1:
                            delay = 60 * 1000 / uint.Parse(tmp) * 4;
                            break;
                    }
                    text = partsNum.ToString() + "," + tmp + "," + ompu[i] + ",";
                    foreach (int[] part in Data)
                    {
                        text += part[i].ToString() + ",";
                    }
                    i++;
                    serialPort1.Write(text);
                    await Task.Delay((int)delay, token);
                }
                if(i >= ompu.Count)
                {
                    serial_end = true;
                }
                if (serial_end)
                {
                    ArrayClear();
                    counter++;
                    if(counter < 0)
                    {
                        await Task.Delay(1000, token);
                        counter = 0;
                        StartButton.Enabled = true;
                        StopButton.Enabled = false;
                        StartButton.PerformClick();
                    }
                    else if(counter < files.Count)
                    {
                        labelSetColor();
                        await Task.Delay(1000, token);
                        StartButton.Enabled = true;
                        StopButton.Enabled = false;
                        StartButton.PerformClick();
                    }
                    else
                    {
                        labelResetColor();
                        counter = 0;
                        StartButton.Enabled = true;
                        StopButton.Enabled = false;
                    }
                }
            }
            catch(Exception ex)
            {
                //例外を無視
            }
        }
        private void ArrayClear()
        {
            ompu.Clear();
            Data.Clear();
            partsNum = 0;
        }
        private void SerialStop()
        {
            serialPort1.Close();
            comboBox1.Enabled = true;
            button1.Text = "Connect";
            label2.Text = "disConnected";
            buttonClear();
            StartButton.Enabled = false;
            StopButton.Enabled = false;
            NextButton.Enabled = false;
            ReturnButton.Enabled = false;
            for(int i = 3; i < labelArray.Length; i++)
            {
                labelArray[i].Text = "FileName";
            }
        }
        private void buttonClear()
        {
            for(int i = 1; i < buttonArray.Length; i++)
            {
                buttonArray[i].Enabled = false;
            }
        }
        private void buttonEnable()
        {
            buttonClear();
            if(files.Count != 10)
            {
                for(int i = 1; i < files.Count + 2; i++)
                {
                    buttonArray[i].Enabled = true;
                }
            }
        }
        private void TaskCancel()
        {
            if(_s == null) { return; }
            _s.Cancel();
            _s.Dispose();
            _s = null;
        }
        private void labelSetColor()
        {
            if(counter != 0)
                labelArray[counter + 2].ForeColor = Color.Black;
            labelArray[counter + 3].ForeColor = Color.Red;
        }
        private void labelResetColor()
        {
            for(int i = 3; i < labelArray.Length; i++)
            {
                labelArray[i].ForeColor = Color.Black;
            }
        }
    }
    public class Onkai
    {
        public string Name { get; set; }
        public int Value { get; set; }
    }
}