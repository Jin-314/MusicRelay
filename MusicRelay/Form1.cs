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
        //ノートの種類
        private enum NoteType
        {
            On,
            Off,
        }
        //ノートの情報を格納する構造体
        private struct NoteData
        {
            public int eventTime;
            public int laneIndex;
            public NoteType type;
        }
        //テンポを格納する構造体
        private struct TempoData
        {
            public int eventTime;
            public float bpm;
            public float tick;
        }
        //ヘッダーチャンク解析用
        private struct HeaderChunkData
        {
            public byte[] cunkID;
            public int dataLength;
            public short format;
            public short tracks;
            public short division;
        };
        //トラックチャンク解析用
        private struct TrackChunkData
        {
            public byte[] chunkID;
            public int dataLength;
            public byte[] data;
        };
        //フォームコントロールの配列
        private Button[] buttonArray;
        private Label[] labelArray;
        private List<string> files = new List<string>();
        private List<NoteData> noteList = new List<NoteData>();
        private List<TempoData> tempoList = new List<TempoData>();
        private int counter = 0;
        private HeaderChunkData headerChunk = new HeaderChunkData();
        private CancellationTokenSource _s = null;

        //Connect Buttonのクリックイベント
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
        //フォームコントロールの名前からコントロールを検索し、配列に格納
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
        //form1の初期処理
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
        //各種ボタンのクリックイベント
        private void button_Clicked(object sender, EventArgs e)
        {
            if (this.Enabled)
            {
                int index = -1;
                //押したボタンについてボタン配列のインデックスを取得
                for(int i = 0; i < buttonArray.Length; i++)
                {
                    if (buttonArray[i].Equals(sender))
                    {
                        index = i;
                        break;
                    }
                }
                //ダイアログを表示して選択したファイルのパスを取得し、ファイルパスの配列に追加
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
                    _s = new CancellationTokenSource();
                    if(counter < 0)
                    {
                        counter = 0;
                    }
                    else if(counter >= files.Count)
                    {
                        labelResetColor();
                        counter = 0;
                        buttonEnable();
                        StartButton.Enabled = true;
                        StopButton.Enabled = false;
                    }
                    else
                    {
                        labelResetColor();
                        labelSetColor();
                        FileEncoder();
                        //解読が終了したら、データをシリアル通信でarduinoに送信
                        SendSerial(_s.Token);
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
        //midi解析＆配列格納用メソッド
        private void FileEncoder()
        {
            noteList.Clear();
            tempoList.Clear();
            //現在の再生位置のファイルパスからファイルをバイナリで開く
            using (FileStream stream = new FileStream(files[counter], FileMode.Open, FileAccess.Read))
            using (BinaryReader reader = new BinaryReader(stream))
            {

                headerChunk.cunkID = reader.ReadBytes(4);

                //ヘッダチャンクの解析
                if (BitConverter.IsLittleEndian)
                {
                    //リトルエディアンならビットを反転させる

                    byte[] byteArray = reader.ReadBytes(4);
                    Array.Reverse(byteArray);
                    headerChunk.dataLength = BitConverter.ToInt32(byteArray, 0);

                    byteArray = reader.ReadBytes(2);
                    Array.Reverse(byteArray);
                    headerChunk.format = BitConverter.ToInt16(byteArray, 0);

                    byteArray = reader.ReadBytes(2);
                    Array.Reverse(byteArray);
                    headerChunk.tracks = BitConverter.ToInt16(byteArray, 0);

                    byteArray = reader.ReadBytes(2);
                    Array.Reverse(byteArray);
                    headerChunk.division = BitConverter.ToInt16(byteArray, 0);
                }
                else
                {
                    headerChunk.dataLength = BitConverter.ToInt32(reader.ReadBytes(4), 0);
                    headerChunk.format = BitConverter.ToInt16(reader.ReadBytes(2), 0);
                    headerChunk.tracks = BitConverter.ToInt16(reader.ReadBytes(2), 0);
                    headerChunk.division = BitConverter.ToInt16(reader.ReadBytes(2), 0);
                }

                TrackChunkData[] trackChunks = new TrackChunkData[headerChunk.tracks];
                //トラックチャンクの解析
                for (int i = 0; i < headerChunk.tracks; i++)
                {
                    trackChunks[i].chunkID = reader.ReadBytes(4);
                    if (BitConverter.IsLittleEndian)
                    {
                        byte[] byteArray = reader.ReadBytes(4);
                        Array.Reverse(byteArray);
                        trackChunks[i].dataLength = BitConverter.ToInt32(byteArray, 0);
                    }
                    else
                    {
                        trackChunks[i].dataLength = BitConverter.ToInt32(reader.ReadBytes(4), 0);
                    }
                    trackChunks[i].data = reader.ReadBytes(trackChunks[i].dataLength);
                    //各トラックデータについてイベントとデルタタイムの抽出
                    TrackDataAnalaysis(trackChunks[i].data);
                }
            }
        }
        //midi / メタ イベント抽出用メソッド
        //主にデルタタイム、ノート番号、テンポの変化を見て配列に格納する
        private void TrackDataAnalaysis(byte[] data)
        {
            uint currentTime = 0;
            byte statusByte = 0;
            bool[] longFlags = new bool[128];

            for (int i = 0; i < data.Length;)
            {
                uint deltaTime = 0;
                while (true)
                {
                    //デルタタイムの抽出
                    byte tmp = data[i++];
                    deltaTime |= tmp & (uint)0x7f;
                    if ((tmp & 0x80) == 0) break;
                    deltaTime = deltaTime << 7;
                }
                currentTime = deltaTime;
                if (data[i] < 0x80)
                {
                    //ランニングステータス
                }
                else
                {
                    statusByte = data[i++];
                }

                byte dataByte0, dataByte1;

                if (statusByte >= 0x80 && statusByte <= 0xef)
                {
                    switch (statusByte & 0xf0)
                    {
                        //ノートオフ
                        case 0x80:
                            dataByte0 = data[i++];
                            dataByte1 = data[i++];
                            if (longFlags[dataByte0])
                            {
                                NoteData note = new NoteData();
                                note.eventTime = (int)currentTime;
                                note.laneIndex = (int)dataByte0;
                                note.type = NoteType.Off;

                                noteList.Add(note);
                                longFlags[note.laneIndex] = false;
                            }
                            break;
                        //ノートオン
                        case 0x90:
                            dataByte0 = data[i++];
                            dataByte1 = data[i++];
                            {
                                NoteData note = new NoteData();
                                note.eventTime = (int)currentTime;
                                note.laneIndex = (int)dataByte0;
                                note.type = NoteType.On;
                                longFlags[note.laneIndex] = true;
                                if (dataByte1 == 0)
                                {
                                    if (longFlags[note.laneIndex])
                                    {
                                        note.type = NoteType.Off;
                                        longFlags[note.laneIndex] = false;
                                    }
                                }
                                noteList.Add(note);
                            }
                            break;
                        //これ以降はインクリメント用
                        case 0xa0:
                            i += 2;
                            break;
                        case 0xb0:
                            dataByte0 = data[i++];
                            dataByte1 = data[i++];
                            if (dataByte0 < 0x78)
                            {

                            }
                            else
                            {
                                switch (dataByte0)
                                {
                                    case 0x78:
                                    case 0x7a:
                                    case 0x7b:
                                    case 0x7c:
                                    case 0x7d:
                                    case 0x7e:
                                    case 0x7f:
                                        break;
                                }
                            }
                            break;
                        case 0xc0:
                        case 0xd0:
                            i += 1;
                            break;
                        case 0xe0:
                            i += 2;
                            break;
                    }
                }
                //SysExイベント用、インクリメントオンリー
                else if (statusByte == 0x70 || statusByte == 0x7f)
                {
                    byte dataLength = data[i++];
                    i += dataLength;
                }
                //メタイベント用
                else if (statusByte == 0xff)
                {
                    byte metaEventID = data[i++];
                    byte dataLength = data[i++];
                    switch (metaEventID)
                    {
                        case 0x00:
                        case 0x01:
                        case 0x02:
                        case 0x03:
                        case 0x04:
                        case 0x05:
                        case 0x06:
                        case 0x07:
                        case 0x20:
                        case 0x21:
                        case 0x2f:
                        case 0x54:
                        case 0x58:
                        case 0x59:
                        case 0x7f:
                            i += dataLength;
                            break;
                        //テンポ情報を格納
                        case 0x51:
                            {
                                TempoData tempoData = new TempoData();
                                tempoData.eventTime = (int)currentTime;
                                uint tempo = 0;
                                tempo |= data[i++];
                                tempo <<= 8;
                                tempo |= data[i++];
                                tempo <<= 8;
                                tempo |= data[i++];
                                tempoData.bpm = 60000000 / (float)tempo;
                                tempoData.bpm = (float)(Math.Floor(tempoData.bpm * 10) / 10);
                                tempoList.Add(tempoData);
                            }
                            break;
                    }
                }
            }
        }
        //シリアル送信用メソッド
        private async void SendSerial(CancellationToken token)
        {
            try
            {
                int i = 0, j = 1;
                int delay = 0;
                int[] parts = new int[128];
                string text = "";

                foreach (NoteData data in noteList)
                {
                    int tempo = (int)tempoList[0].bpm;
                    //インデックスから周期を計算
                    double freq = 440.0 * (Math.Pow(2.0, ((data.laneIndex - 69) / 12.0)));
                    //マイコンが制御しやすいようにマイクロ秒単位周期に変換
                    int period = (int)(1000000 / freq);

                    //鳴らすパートを指定し、マイコンに送信
                    //こうすることでマイコン側が制御しやすくなる
                    if (parts[data.laneIndex] == 0)
                    {
                        parts[data.laneIndex] = j;
                    }
                    if (data.type == NoteType.On)
                    {
                        text += parts[data.laneIndex].ToString() + ",On," + period.ToString() + ",";
                    }
                    else if(data.type == NoteType.Off){
                        text += parts[data.laneIndex].ToString() + ",Off,";
                        parts[data.laneIndex] = 0;
                    }
                    if(data.eventTime != 0)
                    {
                        delay = (int)((60000 / tempo) * (double)((double)data.eventTime / (double)headerChunk.division));
                    }
                    if (i < noteList.Count - 1)
                    {
                        NoteData nextData = noteList[i + 1];
                        if (nextData.eventTime == 0)
                        {
                            if(data.type == nextData.type)
                            {
                                j += 1;
                            }
                            else
                            {
                                j = 1;
                            }
                        }
                        else
                        {
                            j = 1;
                            await Task.Delay(delay, token);
                            serialPort1.Write(text);
                            text = "";
                        }
                    }
                    else
                    {
                        await Task.Delay(delay, token);
                        serialPort1.Write(text);
                    }
                    i += 1;
                }
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
                    buttonEnable();
                }
            }
            catch(Exception ex)
            {
                //例外を無視
            }
        }
        //以降コントロール制御用メソッド
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
}