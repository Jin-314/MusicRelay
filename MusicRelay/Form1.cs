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

        private string tmp = "";
        private Button[] buttonArray;
        private Label[] labelArray;
        private List<string>parts = new List<string>();
        private List<string>ompu = new List<string>();
        private List<int> part1 = new List<int>();
        private List<int> part2 = new List<int>();
        private List<int> part3 = new List<int>();
        private List<int> part4 = new List<int>();
        private List<int> part5 = new List<int>();
        private List<int> part6 = new List<int>();
        private List<int> part7 = new List<int>();
        private List<int> part8 = new List<int>();
        private List<int> part9 = new List<int>();
        private List<int> part10 = new List<int>();
        private int partsNum = 0;
        private int counter = 0;
        private int counter2 = 0;
        private uint delay = 0;
        private List<string> files = new List<string>();
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
            counter2 = 0;
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
                    if(counter2 < 0)
                    {
                        counter2 = 0;
                    }else if(counter2 >= files.Count)
                    {
                        labelResetColor();
                        counter2 = 0;
                        ArrayClear();
                        buttonEnable();
                        StartButton.Enabled = true;
                        StopButton.Enabled = false;
                    }
                    StreamReader sr = new StreamReader(files[counter2], Encoding.GetEncoding("SHIFT_JIS"));
                    counter = 0;
                    labelResetColor();
                    labelSetColor();
                    FileEncoder(sr);
                    SerialDate(_s.Token);
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
                    counter2 = 11;
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
                counter2++;
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
                counter2--;
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
                string line = "";
                List<string> list = new List<string>();
                while ((line = sr.ReadLine()) != null)
                {
                    list.Add(line);
                }
                tmp = list[0];
                ompu.AddRange(list[1].Split('\t'));
                partsNum = list.Count();
                for (int i = 2; i < partsNum; i++)
                {
                    parts.AddRange(list[i].Split('\t'));
                    for (int j = 0; j < parts.Count; j++)
                    {
                        int onkai = 0;
                        switch (parts[j])
                        {
                            case "0":
                                onkai = 0;
                                break;
                            case "hhhfSI":
                                onkai = 17168;
                                break;
                            case "hhhSI":
                                onkai = 16192;
                                break;
                            case "hhDO":
                                onkai = 15268;
                                break;
                            case "hhfMI":
                                onkai = 12780;
                                break;
                            case "hhMI":
                                onkai = 12120;
                                break;
                            case "hhFA":
                                onkai = 11460;
                                break;
                            case "hhSO":
                                onkai = 10204;
                                break;
                            case "hhfRA":
                                onkai = 9648;
                                break;
                            case "hhRA":
                                onkai = 9092;
                                break;
                            case "hhfSI":
                                onkai = 8584;
                                break;
                            case "hhSI":
                                onkai = 8096;
                                break;
                            case "hDO":
                                onkai = 7634;
                                break;
                            case "hsDO":
                                onkai = 7215;
                                break;
                            case "hRE":
                                onkai = 6802;
                                break;
                            case "hfMI":
                                onkai = 6390;
                                break;
                            case "hMI":
                                onkai = 6060;
                                break;
                            case "hFA":
                                onkai = 5730;
                                break;
                            case "hfSO":
                                onkai = 5416;
                                break;
                            case "hSO":
                                onkai = 5102;
                                break;
                            case "hfRA":
                                onkai = 4824;
                                break;
                            case "hRA":
                                onkai = 4546;
                                break;
                            case "hfSI":
                                onkai = 4296;
                                break;
                            case "hSI":
                                onkai = 4048;
                                break;
                            case "DO":
                                onkai = 3817;
                                break;
                            case "RE":
                                onkai = 3401;
                                break;
                            case "fMI":
                                onkai = 3195;
                                break;
                            case "MI":
                                onkai = 3030;
                                break;
                            case "fFA":
                                onkai = 2925;
                                break;
                            case "FA":
                                onkai = 2865;
                                break;
                            case "fSO":
                                onkai = 2708;
                                break;
                            case "SO":
                                onkai = 2551;
                                break;
                            case "fRA":
                                onkai = 2412;
                                break;
                            case "RA":
                                onkai = 2273;
                                break;
                            case "fSI":
                                onkai = 2146;
                                break;
                            case "SI":
                                onkai = 2024;
                                break;
                            case "tDO":
                                onkai = 3817 / 2;
                                break;
                            case "tRE":
                                onkai = 3401 / 2;
                                break;
                            case "tMI":
                                onkai = 3030 / 2;
                                break;
                            case "tfMI":
                                onkai = 3195 / 2;
                                break;
                            case "tFA":
                                onkai = 2865 / 2;
                                break;
                            case "tSO":
                                onkai = 2551 / 2;
                                break;
                            case "tfRA":
                                onkai = 2412 / 2;
                                break;
                            case "tRA":
                                onkai = 2273 / 2;
                                break;
                            case "tfSI":
                                onkai = 2146 / 2;
                                break;
                            case "tSI":
                                onkai = 2024 / 2;
                                break;
                            case "ttDO":
                                onkai = 3817 / 4;
                                break;
                            case "ttRE":
                                onkai = 3401 / 4;
                                break;
                            case "ttMI":
                                onkai = 3030 / 4;
                                break;
                            case "ttfMI":
                                onkai = 3195 / 4;
                                break;
                            case "ttFA":
                                onkai = 2865 / 4;
                                break;
                            case "ttfSO":
                                onkai = 2708 / 4;
                                break;
                            case "ttSO":
                                onkai = 2551 / 4;
                                break;
                            case "ttRA":
                                onkai = 2273 / 4;
                                break;
                            default:
                                //MessageBox.Show("音符データが不正です。");
                                break;
                        }
                        switch (counter)
                        {
                            case 0:
                                part1.Add(onkai);
                                break;
                            case 1:
                                part2.Add(onkai);
                                break;
                            case 2:
                                part3.Add(onkai);
                                break;
                            case 3:
                                part4.Add(onkai);
                                break;
                            case 4:
                                part5.Add(onkai);
                                break;
                            case 5:
                                part6.Add(onkai);
                                break;
                            case 6:
                                part7.Add(onkai);
                                break;
                            case 7:
                                part8.Add(onkai);
                                break;
                            case 8:
                                part9.Add(onkai);
                                break;
                            case 9:
                                part10.Add(onkai);
                                break;
                        }
                    }
                    counter++;
                    parts.Clear();
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
        private async void SerialDate(CancellationToken token)
        {
            try
            {
                int i = 0;
                for (i = 0; i < ompu.Count; i++)
                {
                    int _ompu = int.Parse(ompu[i]);
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
                        default:
                            //MessageBox.Show("音符データが不正です。");
                            break;
                    }
                    for (int k = 3; k <= partsNum; k++){
                        switch (k)
                        {
                            case 3:
                                text = partsNum.ToString() + "," + tmp + "," + ompu[i] + "," + part1[i].ToString();
                                break;
                            case 4:
                                text += "," + part2[i].ToString();
                                break;
                            case 5:
                                text += "," + part3[i].ToString();
                                break;
                            case 6:
                                text += "," + part4[i].ToString();
                                break;
                            case 7:
                                text += "," + part5[i].ToString();
                                break;
                            case 8:
                                text += "," + part6[i].ToString();
                                break;
                            case 9:
                                text += "," + part7[i].ToString();
                                break;
                            case 10:
                                text += "," + part8[i].ToString();
                                break;
                            case 11:
                                text += "," + part9[i].ToString();
                                break;
                            case 12:
                                text += "," + part10[i].ToString();
                                break;
                        }
                    }
                    text += ',';
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
                    counter2++;
                    if(counter2 < 0)
                    {
                        await Task.Delay(1000, token);
                        counter2 = 0;
                        StartButton.Enabled = true;
                        StopButton.Enabled = false;
                        StartButton.PerformClick();
                    }
                    else if(counter2 < files.Count)
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
                        counter2 = 0;
                        StartButton.Enabled = true;
                        StopButton.Enabled = false;
                    }
                }
            }
            catch(Exception ex)
            {
                //MessageBox.Show(ex.Message);
            }
        }
        private void ArrayClear()
        {
            ompu.Clear();
            parts.Clear();
            part1.Clear();
            part2.Clear();
            part3.Clear();
            part4.Clear();
            part5.Clear();
            part6.Clear();
            part7.Clear();
            part8.Clear();
            part9.Clear();
            part10.Clear();
            partsNum = 0;
        }
        private void SerialStop()
        {
            serialPort1.Close();
            comboBox1.Enabled = true;
            button1.Text = "Connect";
            label2.Text = "disConnected";
            button2.Enabled = false;
            button3.Enabled = false;
            button4.Enabled = false;
            button5.Enabled = false;
            button6.Enabled = false;
            button7.Enabled = false;
            button8.Enabled = false;
            button9.Enabled = false;
            button10.Enabled = false;
            button11.Enabled = false;
            StartButton.Enabled = false;
            StopButton.Enabled = false;
            NextButton.Enabled = false;
            ReturnButton.Enabled = false;
            label4.Text = "FileName";
            label5.Text = "FileName";
            label6.Text = "FileName";
            label8.Text = "FileName";
            label7.Text = "FileName";
            label9.Text = "FileName";
            label10.Text = "FileName";
            label11.Text = "FileName";
            label12.Text = "FileName";
            label13.Text = "FileName";
        }
        private void buttonClear()
        {
            button2.Enabled = false;
            button3.Enabled = false;
            button4.Enabled = false;
            button5.Enabled = false;
            button6.Enabled = false;
            button7.Enabled = false;
            button8.Enabled = false;
            button9.Enabled = false;
            button10.Enabled = false;
            button11.Enabled = false;
        }
        private void buttonEnable()
        {
            switch (files.Count)
            {
                case 1:
                    button2.Enabled = true;
                    button3.Enabled = false;
                    button4.Enabled = false;
                    button5.Enabled = false;
                    button6.Enabled = false;
                    button7.Enabled = false;
                    button8.Enabled = false;
                    button9.Enabled = false;
                    button10.Enabled = false;
                    button11.Enabled = false;
                    break;
                case 2:
                    button2.Enabled = true;
                    button3.Enabled = true;
                    button4.Enabled = false;
                    button5.Enabled = false;
                    button6.Enabled = false;
                    button7.Enabled = false;
                    button8.Enabled = false;
                    button9.Enabled = false;
                    button10.Enabled = false;
                    button11.Enabled = false;
                    break;
                case 3:
                    button2.Enabled = true;
                    button3.Enabled = true;
                    button4.Enabled = true;
                    button5.Enabled = false;
                    button6.Enabled = false;
                    button7.Enabled = false;
                    button8.Enabled = false;
                    button9.Enabled = false;
                    button10.Enabled = false;
                    button11.Enabled = false;
                    break;
                case 4:
                    button2.Enabled = true;
                    button3.Enabled = true;
                    button4.Enabled = true;
                    button5.Enabled = true;
                    button6.Enabled = false;
                    button7.Enabled = false;
                    button8.Enabled = false;
                    button9.Enabled = false;
                    button10.Enabled = false;
                    button11.Enabled = false;
                    break;
                case 5:
                    button2.Enabled = true;
                    button3.Enabled = true;
                    button4.Enabled = true;
                    button5.Enabled = true;
                    button6.Enabled = true;
                    button7.Enabled = false;
                    button8.Enabled = false;
                    button9.Enabled = false;
                    button10.Enabled = false;
                    button11.Enabled = false;
                    break;
                case 6:
                    button2.Enabled = true;
                    button3.Enabled = true;
                    button4.Enabled = true;
                    button5.Enabled = true;
                    button6.Enabled = true;
                    button7.Enabled = true;
                    button8.Enabled = false;
                    button9.Enabled = false;
                    button10.Enabled = false;
                    button11.Enabled = false;
                    break;
                case 7:
                    button2.Enabled = true;
                    button3.Enabled = true;
                    button4.Enabled = true;
                    button5.Enabled = true;
                    button6.Enabled = true;
                    button7.Enabled = true;
                    button8.Enabled = true;
                    button9.Enabled = false;
                    button10.Enabled = false;
                    button11.Enabled = false;
                    break;
                case 8:
                    button2.Enabled = true;
                    button3.Enabled = true;
                    button4.Enabled = true;
                    button5.Enabled = true;
                    button6.Enabled = true;
                    button7.Enabled = true;
                    button8.Enabled = true;
                    button9.Enabled = true;
                    button10.Enabled = false;
                    button11.Enabled = false;
                    break;
                case 9:
                    button2.Enabled = true;
                    button3.Enabled = true;
                    button4.Enabled = true;
                    button5.Enabled = true;
                    button6.Enabled = true;
                    button7.Enabled = true;
                    button8.Enabled = true;
                    button9.Enabled = true;
                    button10.Enabled = true;
                    button11.Enabled = false;
                    break;
                case 10:
                    button2.Enabled = true;
                    button3.Enabled = true;
                    button4.Enabled = true;
                    button5.Enabled = true;
                    button6.Enabled = true;
                    button7.Enabled = true;
                    button8.Enabled = true;
                    button9.Enabled = true;
                    button10.Enabled = true;
                    button11.Enabled = true;
                    break;
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
            switch (counter2)
            {
                case 0:
                    label4.ForeColor = Color.Red;
                    break;
                case 1:
                    label4.ForeColor = Color.Black;
                    label5.ForeColor = Color.Red;
                    break;
                case 2:
                    label5.ForeColor = Color.Black;
                    label6.ForeColor = Color.Red;
                    break;
                case 3:
                    label6.ForeColor = Color.Black;
                    label7.ForeColor = Color.Red;
                    break;
                case 4:
                    label7.ForeColor = Color.Black;
                    label8.ForeColor = Color.Red;
                    break;
                case 5:
                    label8.ForeColor = Color.Black;
                    label9.ForeColor = Color.Red;
                    break;
                case 6:
                    label9.ForeColor = Color.Black;
                    label10.ForeColor = Color.Red;
                    break;
                case 7:
                    label10.ForeColor = Color.Black;
                    label11.ForeColor = Color.Red;
                    break;
                case 8:
                    label11.ForeColor = Color.Black;
                    label12.ForeColor = Color.Red;
                    break;
                case 9:
                    label12.ForeColor = Color.Black;
                    label13.ForeColor = Color.Red;
                    break;
            }
        }
        private void labelResetColor()
        {
            label4.ForeColor = Color.Black;
            label5.ForeColor = Color.Black;
            label6.ForeColor = Color.Black;
            label7.ForeColor = Color.Black;
            label8.ForeColor = Color.Black;
            label9.ForeColor = Color.Black;
            label10.ForeColor = Color.Black;
            label11.ForeColor = Color.Black;
            label12.ForeColor = Color.Black;
            label13.ForeColor = Color.Black;
        }
    }
}
