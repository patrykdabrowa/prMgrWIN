using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.IO;

namespace PracaMagisterska
{

    public partial class Form1 : Form
    {
        Graphics drawArea1;
        Graphics drawArea2;

        //zmienne do rysowania autka i pomieszczenia
        int rozpoczecieSkanowania = 0;
        int moveXtab = 0;
        int moveYtab = 0;
        int[] moveX = new int[40];
        int[] moveY = new int[40];
        int przebytaOdlegloscN;
        int przebytaOdlegloscW;
        int przebytaOdlegloscS;
        int przebytaOdlegloscE;
        int ustawienieRobota = 0;

        int licznikKropek = 0;
        int[] kropkaX = new int[300];
        int[] kropkaY = new int[300];
        int[] kropkaXW = new int[300];
        int[] kropkaYW = new int[300];
        int[] kropkaXE = new int[300];
        int[] kropkaYE = new int[300];

        float pomiarCzujnikaN = 0;
        float pomiarCzujnikaW = 0;
        float pomiarCzujnikaE = 0;
        float buforN;
        float buforW;
        float buforE;
        int[] pomieszczenieX = new int[40];
        int[] pomieszczenieY = new int[40];

        string dataOutN = "oox";
        string dataOutW = "ofd";
        string dataOutE = "fod";
        string dataOutStop = "ffx";
        string dataReset = "rrr";

        //string dataIn;
        private string ramka = "";
        private string bufor = "";

        int licznikProstych = 1;
        float[] xProstej = new float[20];
        float[] yProstej = new float[20];


        //AUTOMATYCZNE SKANOWANIE
        int rozpoczecieAutomatycznegoSkanowania = 0;
        int wybranyAlgorytm = 0;

        int wspolrzednaRobotaX;
        int wspolrzednaRobotaY;

        int obrotRobota = 0; //zmienna do obrotu
        int odczytZyroskopu = 0; //zmienna do obrotu o 90 stopni ze skokiem

        int odlegloscOdPrzeszkod = 20;
        int wykonaneObroty = 0;


        public Form1()
        {
            InitializeComponent();
            drawArea1 = drawingArea1.CreateGraphics();
            drawArea2 = drawingArea2.CreateGraphics();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            label5.Text = cBoxPortCom.Text;
            label5.ForeColor = Color.Green;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            cBoxBaudRate.Text = "9600";
            cBoxDataBits.Text = "8";
            cBoxStopBits.Text = "1";
            cBoxParityBits.Text = "None";

            groupBox3.Enabled = false;
            groupBox4.Enabled = false;
            groupBox5.Enabled = false;

            string[] ports = SerialPort.GetPortNames();
            cBoxPortCom.Items.AddRange(ports);

        }

        private void buttonOpen_Click_1(object sender, EventArgs e)
        {
            try
            {
                serialPort1.PortName = cBoxPortCom.Text;
                serialPort1.BaudRate = Convert.ToInt32(cBoxBaudRate.Text);
                serialPort1.DataBits = Convert.ToInt32(cBoxDataBits.Text);
                serialPort1.StopBits = (StopBits)Enum.Parse(typeof(StopBits), cBoxStopBits.Text);
                serialPort1.Parity = (Parity)Enum.Parse(typeof(Parity), cBoxParityBits.Text);

                serialPort1.Open();


                const string czyaktywne = "aktywne";
                label7.Text = czyaktywne;
                label7.ForeColor = Color.Green;
                groupBox3.Enabled = true;
                groupBox4.Enabled = true;
                groupBox5.Enabled = true;
            }

            catch (Exception err)
            {
                MessageBox.Show(err.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void buttonClose_Click_1(object sender, EventArgs e)
        {
            if (serialPort1.IsOpen)
            {
                serialPort1.Close();
                const string czyaktywne = "nieaktywne";
                label7.Text = czyaktywne;
                label7.ForeColor = Color.Red;
                groupBox3.Enabled = false;
                groupBox4.Enabled = false;
                groupBox5.Enabled = false;
            }
        }

        private void buttonSendN_Click(object sender, EventArgs e)
        {
            if (serialPort1.IsOpen)
            {
                serialPort1.WriteLine(dataOutN);
            }
        }

        private void buttonSendW_Click(object sender, EventArgs e)
        {
            if (serialPort1.IsOpen)
            {
                wykonaneObroty++;
                serialPort1.WriteLine(dataOutW);
                if (ustawienieRobota == 3) ustawienieRobota = 0;
                else ustawienieRobota++;
                licznikProstych++;
                obrotRobota = 1;

            }
        }

        private void buttonSendE_Click(object sender, EventArgs e)
        {
            if (serialPort1.IsOpen)
            {
                wykonaneObroty--;
                serialPort1.WriteLine(dataOutE);
                if (ustawienieRobota == 0) ustawienieRobota = 3;
                else ustawienieRobota--;
                licznikProstych++;
            }
        }

        private void buttonSendStop_Click(object sender, EventArgs e)
        {
            if (serialPort1.IsOpen)
            {
                serialPort1.WriteLine(dataOutStop);
            }
        }

        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            //"N%03d W%03d E%03d EL%03d EP%03d K%04d\n\r" struktura - ramka transmisyjna

            string dataIn = serialPort1.ReadExisting();
            bufor += dataIn;
            int i = bufor.IndexOf("\r");
            if (i > 0)
            {
                ramka = bufor.Substring(0, i);
                bufor = bufor.Remove(0, i + 1);

                try
                {
                    this.Invoke(new EventHandler(ShowData));
                }
                catch (System.IndexOutOfRangeException error)
                {
                    //System.Console.WriteLine(error.Message);
                    // Set IndexOutOfRangeException to the new exception's InnerException.
                    //throw new System.ArgumentOutOfRangeException("index parameter is out of range.", error);
                }
                
                
                if (rozpoczecieAutomatycznegoSkanowania == 1) this.Invoke(new EventHandler(AutomatyczneSkanowanie));
            }
        }

        private void ShowData(object sender, EventArgs e)
        {
            buforN = pomiarCzujnikaN;
            buforE = pomiarCzujnikaE;
            buforW = pomiarCzujnikaW;


            //zapisywanie z ramki odl z sensora N
            string sensorN;
            string sensorLiczbaP = Convert.ToString(ramka[1]);
            string sensorLiczbaD = Convert.ToString(ramka[2]);
            string sensorLiczbaT = Convert.ToString(ramka[3]);
            string sensorLiczbaPP = Convert.ToString(ramka[5]);
            string sensorLiczbaPD = Convert.ToString(ramka[6]);

            if (sensorLiczbaP != "0")
            {
                sensorN = sensorLiczbaP + sensorLiczbaD + sensorLiczbaT + "," + sensorLiczbaPP + sensorLiczbaPD;
                textBoxSensorN.Text = sensorN;
            }

            if ((sensorLiczbaP == "0") && (sensorLiczbaD != "0"))
            {
                sensorN = sensorLiczbaD + sensorLiczbaT + "," + sensorLiczbaPP + sensorLiczbaPD;
                textBoxSensorN.Text = sensorN;
            }
            if ((sensorLiczbaP == "0") && (sensorLiczbaD == "0"))
            {
                sensorN = sensorLiczbaT + "," + sensorLiczbaPP + sensorLiczbaPD;
                textBoxSensorN.Text = sensorN;
            }
            if ((sensorLiczbaP == "0") && (sensorLiczbaD == "0") && (sensorLiczbaT == "0"))
            {
                textBoxSensorN.Text = "0";
            }


            //zapisywanie z ramki odl z sensora W
            string sensorW;
            sensorLiczbaP = Convert.ToString(ramka[9]);
            sensorLiczbaD = Convert.ToString(ramka[10]);
            sensorLiczbaT = Convert.ToString(ramka[11]);
            sensorLiczbaPP = Convert.ToString(ramka[13]);
            sensorLiczbaPD = Convert.ToString(ramka[14]);

            if (sensorLiczbaP != "0")
            {
                sensorW = sensorLiczbaP + sensorLiczbaD + sensorLiczbaT + "," + sensorLiczbaPP + sensorLiczbaPD;
                textBoxSensorW.Text = sensorW;
            }

            if ((sensorLiczbaP == "0") && (sensorLiczbaD != "0"))
            {
                sensorW = sensorLiczbaD + sensorLiczbaT + "," + sensorLiczbaPP + sensorLiczbaPD;
                textBoxSensorW.Text = sensorW;
            }
            if ((sensorLiczbaP == "0") && (sensorLiczbaD == "0"))
            {
                sensorW = sensorLiczbaT + "," + sensorLiczbaPP + sensorLiczbaPD;
                textBoxSensorW.Text = sensorW;
            }
            if ((sensorLiczbaP == "0") && (sensorLiczbaD == "0") && (sensorLiczbaT == "0"))
            {
                textBoxSensorW.Text = "0";
            }

            //zapisywanie z ramki odl z sensora E
            string sensorE;
            sensorLiczbaP = Convert.ToString(ramka[17]);
            sensorLiczbaD = Convert.ToString(ramka[18]);
            sensorLiczbaT = Convert.ToString(ramka[19]);
            sensorLiczbaPP = Convert.ToString(ramka[21]);
            sensorLiczbaPD = Convert.ToString(ramka[22]);

            if (sensorLiczbaP != "0")
            {
                sensorE = sensorLiczbaP + sensorLiczbaD + sensorLiczbaT + "," + sensorLiczbaPP + sensorLiczbaPD;
                textBoxSensorE.Text = sensorE;
            }

            if ((sensorLiczbaP == "0") && (sensorLiczbaD != "0"))
            {
                sensorE = sensorLiczbaD + sensorLiczbaT + "," + sensorLiczbaPP + sensorLiczbaPD;
                textBoxSensorE.Text = sensorE;
            }
            if ((sensorLiczbaP == "0") && (sensorLiczbaD == "0"))
            {
                sensorE = sensorLiczbaT + "," + sensorLiczbaPP + sensorLiczbaPD;
                textBoxSensorE.Text = sensorE;
            }
            if ((sensorLiczbaP == "0") && (sensorLiczbaD == "0") && (sensorLiczbaT == "0"))
            {
                textBoxSensorE.Text = "0";
            }

            //zapisywanie z ramki przebytego dystansu
            string distance;
            sensorLiczbaP = Convert.ToString(ramka[25]);
            sensorLiczbaD = Convert.ToString(ramka[26]);
            sensorLiczbaT = Convert.ToString(ramka[27]);
            string sensorLiczbaC = Convert.ToString(ramka[28]);
            string sensorLiczbaPi = Convert.ToString(ramka[29]);


            if (sensorLiczbaP != "0")
            {
                distance = sensorLiczbaP + sensorLiczbaD + sensorLiczbaT + sensorLiczbaC + sensorLiczbaPi;
                textBoxEnkoderL.Text = distance;
            }

            if ((sensorLiczbaP == "0") && (sensorLiczbaD != "0"))
            {
                distance = sensorLiczbaD + sensorLiczbaT + sensorLiczbaC + sensorLiczbaPi;
                textBoxEnkoderL.Text = distance;
            }
            if ((sensorLiczbaP == "0") && (sensorLiczbaD == "0"))
            {
                distance = sensorLiczbaT + sensorLiczbaC + sensorLiczbaPi;
                textBoxEnkoderL.Text = distance;
            }
            if ((sensorLiczbaP == "0") && (sensorLiczbaD == "0") && (sensorLiczbaT == "0"))
            {
                distance = sensorLiczbaC + sensorLiczbaPi;
                textBoxEnkoderL.Text = distance;
            }
            if ((sensorLiczbaP == "0") && (sensorLiczbaD == "0") && (sensorLiczbaT == "0") && (sensorLiczbaC == "0"))
            {
                distance = sensorLiczbaPi;
                textBoxEnkoderL.Text = distance;
            }
            if ((sensorLiczbaP == "0") && (sensorLiczbaD == "0") && (sensorLiczbaT == "0") && (sensorLiczbaC == "0") && (sensorLiczbaPi == "0"))
            {
                textBoxEnkoderL.Text = "0";
            }

            //zapisywanie z ramki enkodera P
            string enkoderP;
            sensorLiczbaP = Convert.ToString(ramka[33]);
            sensorLiczbaD = Convert.ToString(ramka[34]);
            sensorLiczbaT = Convert.ToString(ramka[35]);
            sensorLiczbaC = Convert.ToString(ramka[36]);
            sensorLiczbaPi = Convert.ToString(ramka[37]);

            if (sensorLiczbaP != "0")
            {
                enkoderP = sensorLiczbaP + sensorLiczbaD + sensorLiczbaT + sensorLiczbaC + sensorLiczbaPi;
                textBoxEnkoderP.Text = enkoderP;
            }

            if ((sensorLiczbaP == "0") && (sensorLiczbaD != "0"))
            {
                enkoderP = sensorLiczbaD + sensorLiczbaT + sensorLiczbaC + sensorLiczbaPi;
                textBoxEnkoderP.Text = enkoderP;
            }
            if ((sensorLiczbaP == "0") && (sensorLiczbaD == "0"))
            {
                enkoderP = sensorLiczbaT + sensorLiczbaC + sensorLiczbaPi;
                textBoxEnkoderP.Text = enkoderP;
            }
            if ((sensorLiczbaP == "0") && (sensorLiczbaD == "0") && (sensorLiczbaT == "0"))
            {
                enkoderP = sensorLiczbaC + sensorLiczbaPi;
                textBoxEnkoderP.Text = enkoderP;
            }
            if ((sensorLiczbaP == "0") && (sensorLiczbaD == "0") && (sensorLiczbaT == "0") && (sensorLiczbaC == "0"))
            {
                enkoderP = sensorLiczbaPi;
                textBoxEnkoderP.Text = enkoderP;
            }
            if ((sensorLiczbaP == "0") && (sensorLiczbaD == "0") && (sensorLiczbaT == "0") && (sensorLiczbaC == "0") && (sensorLiczbaPi == "0"))
            {
                textBoxEnkoderP.Text = "0";
            }

            //zapisywanie z ramki zyroskopu
            string zyro;
            string sensorLiczbaZ = Convert.ToString(ramka[40]);
            sensorLiczbaP = Convert.ToString(ramka[41]);
            sensorLiczbaD = Convert.ToString(ramka[42]);
            sensorLiczbaT = Convert.ToString(ramka[43]);

            if (sensorLiczbaZ != "0")
            {
                if (sensorLiczbaP != "0")
                {
                    zyro = "-" + sensorLiczbaP + sensorLiczbaD + sensorLiczbaT;
                    textBoxZyroskop.Text = zyro;
                    textBox_katObrotu.Text = zyro;
                    odczytZyroskopu = Convert.ToInt32(zyro);
                }

                if ((sensorLiczbaP == "0") && (sensorLiczbaD != "0"))
                {
                    zyro = "-" + sensorLiczbaD + sensorLiczbaT;
                    textBoxZyroskop.Text = zyro;
                    textBox_katObrotu.Text = zyro;
                    odczytZyroskopu = Convert.ToInt32(zyro);
                }
                if ((sensorLiczbaP == "0") && (sensorLiczbaD == "0"))
                {
                    zyro = "-" + sensorLiczbaT;
                    textBoxZyroskop.Text = zyro;
                    textBox_katObrotu.Text = zyro;
                    odczytZyroskopu = Convert.ToInt32(zyro);
                }

            }
            else
            {
                if (sensorLiczbaP != "0")
                {
                    zyro = sensorLiczbaP + sensorLiczbaD + sensorLiczbaT;
                    textBoxZyroskop.Text = zyro;
                    textBox_katObrotu.Text = zyro;
                    odczytZyroskopu = Convert.ToInt32(zyro);
                }

                if ((sensorLiczbaP == "0") && (sensorLiczbaD != "0"))
                {
                    zyro = sensorLiczbaD + sensorLiczbaT;
                    textBoxZyroskop.Text = zyro;
                    textBox_katObrotu.Text = zyro;
                    odczytZyroskopu = Convert.ToInt32(zyro);
                }
                if ((sensorLiczbaP == "0") && (sensorLiczbaD == "0"))
                {
                    zyro = sensorLiczbaT;
                    textBoxZyroskop.Text = zyro;
                    textBox_katObrotu.Text = zyro;
                    odczytZyroskopu = Convert.ToInt32(zyro);
                }
                if ((sensorLiczbaP == "0") && (sensorLiczbaD == "0") && (sensorLiczbaT == "0"))
                {
                    textBoxZyroskop.Text = "0";
                    textBox_katObrotu.Text = "0";
                }
            }

            textBox7.Text = ramka;

            int dataLength = textBox7.TextLength;
            //textBox6.Text = string.Format("{0:00}", dataLength);

            if (rozpoczecieSkanowania == 1)
            {
                //dodanie odleglosci do moveX i moveY
                switch (ustawienieRobota)
                {
                    case 0:
                        przebytaOdlegloscN = int.Parse(textBoxEnkoderL.Text);
                        przebytaOdlegloscN = przebytaOdlegloscN - przebytaOdlegloscE - przebytaOdlegloscS - przebytaOdlegloscW;
                        break;
                    case 1:
                        przebytaOdlegloscW = int.Parse(textBoxEnkoderL.Text);
                        przebytaOdlegloscW = przebytaOdlegloscW - przebytaOdlegloscE - przebytaOdlegloscS - przebytaOdlegloscN;
                        break;
                    case 2:
                        przebytaOdlegloscS = int.Parse(textBoxEnkoderL.Text);
                        przebytaOdlegloscS = przebytaOdlegloscS - przebytaOdlegloscE - przebytaOdlegloscN - przebytaOdlegloscW;
                        break;
                    case 3:
                        przebytaOdlegloscE = int.Parse(textBoxEnkoderL.Text);
                        przebytaOdlegloscE = przebytaOdlegloscE - przebytaOdlegloscN - przebytaOdlegloscS - przebytaOdlegloscW;
                        break;
                }


                switch (ustawienieRobota)
                {
                    case 0:
                        moveYtab = przebytaOdlegloscN + (przebytaOdlegloscS * (-1));
                        moveY[licznikProstych] = przebytaOdlegloscN + (przebytaOdlegloscS * (-1));
                        break;
                    case 1:
                        moveXtab = (przebytaOdlegloscW * (-1));
                        moveX[licznikProstych] = (przebytaOdlegloscW * (-1));
                        break;
                    case 2:
                        moveYtab = (przebytaOdlegloscS * (-1)) + przebytaOdlegloscN;
                        moveY[licznikProstych] = (przebytaOdlegloscS * (-1)) + przebytaOdlegloscN;
                        break;
                    case 3:
                        moveXtab = (przebytaOdlegloscE) + (przebytaOdlegloscW * (-1));
                        moveX[licznikProstych] = (przebytaOdlegloscE) + (przebytaOdlegloscW * (-1));
                        break;
                }
                //moveX = przebytaOdleglosc;

                rysowanieRamki();
                rysowanieDrogi();

                pomiarCzujnikaN = float.Parse(textBoxSensorN.Text);
                pomiarCzujnikaW = float.Parse(textBoxSensorW.Text);
                pomiarCzujnikaE = float.Parse(textBoxSensorE.Text);

                rysowaniePomieszczenia();

                switch (ustawienieRobota)
                {
                    case 0:
                        rysowanieRobotaDoGory();
                        break;
                    case 1:
                        rysowanieRobotaWPrawo();
                        break;
                    case 2:
                        rysowanieRobotaDoDolu();
                        break;
                    case 3:
                        rysowanieRobotaWLewo();
                        break;
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            textBox7.Text = "";
        }

        private void buttonUstawDomyslne_Click(object sender, EventArgs e)
        {
            cBoxPredkoscRobota.Text = "20";
            comboBoxCzestSkanowania.Text = "5";
        }

        private void buttonWyslijUstawienia_Click(object sender, EventArgs e)
        {
            string predkoscRobota = cBoxPredkoscRobota.Text;
            if (predkoscRobota == "10") serialPort1.WriteLine("axx");
            if (predkoscRobota == "20") serialPort1.WriteLine("bxx");
            if (predkoscRobota == "50") serialPort1.WriteLine("cxx");
        }

        private void startScan_Click(object sender, EventArgs e)
        {
            rozpoczecieSkanowania = 1;
        }

        private void rysowanieRamki()
        {
            //NARYSOWANE STAKI
            //OBRAMOWANIE RAMKI I WYPEŁNIENIE
            SolidBrush whiteBrush = new SolidBrush(Color.White);
            drawArea1.FillRectangle(whiteBrush, 0, 0, 670, 630);
            drawArea2.FillRectangle(whiteBrush, 0, 0, 670, 630);
            //linie pionowe
            Pen lightGrayPen = new Pen(Color.LightGray);
            for (int i = 1; i < 67; i++)
            {
                drawArea1.DrawLine(lightGrayPen, i * 10, 0, i * 10, 670);
                drawArea2.DrawLine(lightGrayPen, i * 10, 0, i * 10, 670);
            }
            for (int i = 1; i < 63; i++)
            {
                drawArea1.DrawLine(lightGrayPen, 0, i * 10, 670, i * 10);
                drawArea2.DrawLine(lightGrayPen, 0, i * 10, 670, i * 10);
            }
            //obramowanie
            Pen blackPen = new Pen(Color.Black);
            drawArea1.DrawLine(blackPen, 0, 0, 0, 630);
            drawArea1.DrawLine(blackPen, 0, 0, 670, 0);
            drawArea1.DrawLine(blackPen, 670, 0, 670, 630);
            drawArea1.DrawLine(blackPen, 0, 630, 670, 630);
           
            drawArea2.DrawLine(blackPen, 0, 0, 0, 630);
            drawArea2.DrawLine(blackPen, 0, 0, 670, 0);
            drawArea2.DrawLine(blackPen, 670, 0, 670, 630);
            drawArea2.DrawLine(blackPen, 0, 630, 670, 630);

            Pen greenPen = new Pen(Color.Green);
            drawArea1.DrawLine(greenPen, 600, 610, 650, 610);
            drawArea1.DrawLine(greenPen, 600, 605, 600, 615);
            drawArea1.DrawLine(greenPen, 650, 605, 650, 615);

            drawArea2.DrawLine(greenPen, 600, 610, 650, 610);
            drawArea2.DrawLine(greenPen, 600, 605, 600, 615);
            drawArea2.DrawLine(greenPen, 650, 605, 650, 615);

            var font = new Font("TimesNewRoman", 15, FontStyle.Regular, GraphicsUnit.Pixel);
            drawArea1.DrawString("50cm", font, Brushes.Green, new Point(605, 590));
            drawArea2.DrawString("50cm", font, Brushes.Green, new Point(605, 590));

        }

        private void rysowanieDrogi()
        {
            //rysowanie przebytej drogi
            SolidBrush blueBrush = new SolidBrush(Color.Blue);
            Pen bluePen = new Pen(Color.Blue);
            drawArea1.FillEllipse(blueBrush, ((drawingArea1.Width / 2) - 5), ((drawingArea1.Height / 2) - 5), 10, 10); //kolko startu robota
            drawArea2.FillEllipse(blueBrush, ((drawingArea1.Width / 2) - 5), ((drawingArea1.Height / 2) - 5), 10, 10); //kolko startu robota

            xProstej[0] = (drawingArea1.Width / 2);
            yProstej[0] = (drawingArea1.Height / 2);

            switch (licznikProstych)
            {
                case 1:
                    xProstej[1] = xProstej[0];
                    yProstej[1] = (drawingArea1.Height / 2) - moveYtab;
                    break;
                case 2:
                    xProstej[2] = (drawingArea1.Width / 2) - moveXtab;
                    yProstej[2] = yProstej[1];
                    break;
                case 3:
                    xProstej[3] = xProstej[2];
                    yProstej[3] = (drawingArea1.Height / 2) - moveYtab;
                    break;
                case 4:
                    xProstej[4] = (drawingArea1.Width / 2) - moveXtab;
                    yProstej[4] = yProstej[3];
                    break;
                case 5:
                    xProstej[5] = xProstej[4];
                    yProstej[5] = (drawingArea1.Height / 2) - moveYtab;
                    break;
                case 6:
                    xProstej[6] = (drawingArea1.Width / 2) - moveXtab;
                    yProstej[6] = yProstej[5];
                    break;
                case 7:
                    xProstej[7] = xProstej[6];
                    yProstej[7] = (drawingArea1.Height / 2) - moveYtab;
                    break;
                case 8:
                    xProstej[8] = (drawingArea1.Width / 2) - moveXtab;
                    yProstej[8] = yProstej[7];
                    break;
                case 9:
                    xProstej[9] = xProstej[8];
                    yProstej[9] = (drawingArea1.Height / 2) - moveYtab;
                    break;
                case 10:
                    xProstej[10] = (drawingArea1.Width / 2) - moveXtab;
                    yProstej[10] = yProstej[9];
                    break;
                case 11:
                    xProstej[11] = xProstej[10];
                    yProstej[11] = (drawingArea1.Height / 2) - moveYtab;
                    break;
                case 12:
                    xProstej[12] = xProstej[11];
                    yProstej[12] = (drawingArea1.Height / 2) - moveYtab;
                    break;
                case 13:
                    xProstej[13] = xProstej[12];
                    yProstej[13] = (drawingArea1.Height / 2) - moveYtab;
                    break;
                case 14:
                    xProstej[14] = xProstej[13];
                    yProstej[14] = (drawingArea1.Height / 2) - moveYtab;
                    break;
                case 15:
                    xProstej[15] = xProstej[14];
                    yProstej[15] = (drawingArea1.Height / 2) - moveYtab;
                    break;
                case 16:
                    xProstej[16] = xProstej[15];
                    yProstej[16] = (drawingArea1.Height / 2) - moveYtab;
                    break;
                case 17:
                    xProstej[17] = xProstej[16];
                    yProstej[17] = (drawingArea1.Height / 2) - moveYtab;
                    break;
                case 18:
                    xProstej[18] = xProstej[17];
                    yProstej[18] = (drawingArea1.Height / 2) - moveYtab;
                    break;
                case 19:
                    xProstej[19] = xProstej[18];
                    yProstej[19] = (drawingArea1.Height / 2) - moveYtab;
                    break;
                case 20:
                    xProstej[20] = xProstej[19];
                    yProstej[20] = (drawingArea1.Height / 2) - moveYtab;
                    break;

            }

            for (int i = 1; i <= licznikProstych; i++)
            {
                rysowanieProstej(xProstej[i - 1], yProstej[i - 1], xProstej[i], yProstej[i]);

                wspolrzednaRobotaX = Convert.ToInt32(xProstej[i]);
                wspolrzednaRobotaY = Convert.ToInt32(yProstej[i]);

                textBox_wspolrzednaX.Text = Convert.ToString(wspolrzednaRobotaX-335);
                textBox_wspolrzednaY.Text = Convert.ToString((wspolrzednaRobotaY-315)*(-1));
            }
        }

        private void rysowanieProstej(float startx, float koniecx, float starty, float koniecy)
        {
            Pen bluePen = new Pen(Color.Blue);
            PointF x = new PointF(startx, koniecx);
            PointF y = new PointF(starty, koniecy);
            drawArea1.DrawLine(bluePen, x, y);
            drawArea2.DrawLine(bluePen, x, y);
        }

        private void rysowanieRobotaDoGory()
        {
            SolidBrush yellowBrush = new SolidBrush(Color.Yellow);
            SolidBrush blackBrush = new SolidBrush(Color.Black);
            SolidBrush blueBrush = new SolidBrush(Color.Blue);
            drawArea1.FillRectangle(blackBrush, (((drawingArea1.Width / 2) + 10) - moveXtab), (((drawingArea1.Height / 2) - 6) - moveYtab), 5, 13); //prawe kolo
            drawArea1.FillRectangle(blackBrush, (((drawingArea1.Width / 2) - 14) - moveXtab), (((drawingArea1.Height / 2) - 6) - moveYtab), 5, 13); //lewe kolo
            drawArea1.FillEllipse(blueBrush, ((drawingArea1.Width / 2) - 10) - moveXtab, ((drawingArea1.Height / 2) - 15) - moveYtab, 20, 30); //cialo robota
            drawArea1.FillRectangle(yellowBrush, (((drawingArea1.Width / 2) - 5) - moveXtab), (((drawingArea1.Height / 2) - 10) - moveYtab), 5, 5); //lewe oko
            drawArea1.FillRectangle(yellowBrush, (((drawingArea1.Width / 2) + 2) - moveXtab), (((drawingArea1.Height / 2) - 10) - moveYtab), 5, 5); //prawe oko

            drawArea2.FillRectangle(blackBrush, (((drawingArea1.Width / 2) + 10) - moveXtab), (((drawingArea1.Height / 2) - 6) - moveYtab), 5, 13); //prawe kolo
            drawArea2.FillRectangle(blackBrush, (((drawingArea1.Width / 2) - 14) - moveXtab), (((drawingArea1.Height / 2) - 6) - moveYtab), 5, 13); //lewe kolo
            drawArea2.FillEllipse(blueBrush, ((drawingArea1.Width / 2) - 10) - moveXtab, ((drawingArea1.Height / 2) - 15) - moveYtab, 20, 30); //cialo robota
            drawArea2.FillRectangle(yellowBrush, (((drawingArea1.Width / 2) - 5) - moveXtab), (((drawingArea1.Height / 2) - 10) - moveYtab), 5, 5); //lewe oko
            drawArea2.FillRectangle(yellowBrush, (((drawingArea1.Width / 2) + 2) - moveXtab), (((drawingArea1.Height / 2) - 10) - moveYtab), 5, 5); //prawe oko
        }

        private void rysowanieRobotaDoDolu()
        {
            SolidBrush yellowBrush = new SolidBrush(Color.Yellow);
            SolidBrush blackBrush = new SolidBrush(Color.Black);
            SolidBrush blueBrush = new SolidBrush(Color.Blue);
            drawArea1.FillRectangle(blackBrush, (((drawingArea1.Width / 2) + 10) - moveXtab), (((drawingArea1.Height / 2) - 6) - moveYtab), 5, 13); //prawe kolo
            drawArea1.FillRectangle(blackBrush, (((drawingArea1.Width / 2) - 14) - moveXtab), (((drawingArea1.Height / 2) - 6) - moveYtab), 5, 13); //lewe kolo
            drawArea1.FillEllipse(blueBrush, ((drawingArea1.Width / 2) - 10) - moveXtab, ((drawingArea1.Height / 2) - 15) - moveYtab, 20, 30); //cialo robota
            drawArea1.FillRectangle(yellowBrush, (((drawingArea1.Width / 2) - 5) - moveXtab), (((drawingArea1.Height / 2) + 5) - moveYtab), 5, 5); //lewe oko
            drawArea1.FillRectangle(yellowBrush, (((drawingArea1.Width / 2) + 2) - moveXtab), (((drawingArea1.Height / 2) + 5) - moveYtab), 5, 5); //prawe oko

            drawArea2.FillRectangle(blackBrush, (((drawingArea1.Width / 2) + 10) - moveXtab), (((drawingArea1.Height / 2) - 6) - moveYtab), 5, 13); //prawe kolo
            drawArea2.FillRectangle(blackBrush, (((drawingArea1.Width / 2) - 14) - moveXtab), (((drawingArea1.Height / 2) - 6) - moveYtab), 5, 13); //lewe kolo
            drawArea2.FillEllipse(blueBrush, ((drawingArea1.Width / 2) - 10) - moveXtab, ((drawingArea1.Height / 2) - 15) - moveYtab, 20, 30); //cialo robota
            drawArea2.FillRectangle(yellowBrush, (((drawingArea1.Width / 2) - 5) - moveXtab), (((drawingArea1.Height / 2) + 5) - moveYtab), 5, 5); //lewe oko
            drawArea2.FillRectangle(yellowBrush, (((drawingArea1.Width / 2) + 2) - moveXtab), (((drawingArea1.Height / 2) + 5) - moveYtab), 5, 5); //prawe oko
        }

        private void rysowanieRobotaWPrawo()
        {
            SolidBrush yellowBrush = new SolidBrush(Color.Yellow);
            SolidBrush blackBrush = new SolidBrush(Color.Black);
            SolidBrush blueBrush = new SolidBrush(Color.Blue);
            drawArea1.FillRectangle(blackBrush, (((drawingArea1.Width / 2) - 6) - moveXtab), (((drawingArea1.Height / 2) - 15) - moveYtab), 13, 5); //prawe kolo
            drawArea1.FillRectangle(blackBrush, (((drawingArea1.Width / 2) - 6) - moveXtab), (((drawingArea1.Height / 2) + 9) - moveYtab), 13, 5); //lewe kolo
            drawArea1.FillEllipse(blueBrush, ((drawingArea1.Width / 2) - 15) - moveXtab, ((drawingArea1.Height / 2) - 11) - moveYtab, 30, 20); //cialo robota
            drawArea1.FillRectangle(yellowBrush, (((drawingArea1.Width / 2) + 7) - moveXtab), (((drawingArea1.Height / 2) - 7) - moveYtab), 5, 5); //lewe oko
            drawArea1.FillRectangle(yellowBrush, (((drawingArea1.Width / 2) + 7) - moveXtab), (((drawingArea1.Height / 2)) - moveYtab), 5, 5); //prawe oko

            drawArea2.FillRectangle(blackBrush, (((drawingArea1.Width / 2) - 6) - moveXtab), (((drawingArea1.Height / 2) - 15) - moveYtab), 13, 5); //prawe kolo
            drawArea2.FillRectangle(blackBrush, (((drawingArea1.Width / 2) - 6) - moveXtab), (((drawingArea1.Height / 2) + 9) - moveYtab), 13, 5); //lewe kolo
            drawArea2.FillEllipse(blueBrush, ((drawingArea1.Width / 2) - 15) - moveXtab, ((drawingArea1.Height / 2) - 11) - moveYtab, 30, 20); //cialo robota
            drawArea2.FillRectangle(yellowBrush, (((drawingArea1.Width / 2) + 7) - moveXtab), (((drawingArea1.Height / 2) - 7) - moveYtab), 5, 5); //lewe oko
            drawArea2.FillRectangle(yellowBrush, (((drawingArea1.Width / 2) + 7) - moveXtab), (((drawingArea1.Height / 2)) - moveYtab), 5, 5); //prawe oko
        }

        private void rysowanieRobotaWLewo()
        {
            SolidBrush yellowBrush = new SolidBrush(Color.Yellow);
            SolidBrush blackBrush = new SolidBrush(Color.Black);
            SolidBrush blueBrush = new SolidBrush(Color.Blue);
            drawArea1.FillRectangle(blackBrush, (((drawingArea1.Width / 2) - 6) - moveXtab), (((drawingArea1.Height / 2) - 15) - moveYtab), 13, 5); //prawe kolo
            drawArea1.FillRectangle(blackBrush, (((drawingArea1.Width / 2) - 6) - moveXtab), (((drawingArea1.Height / 2) + 9) - moveYtab), 13, 5); //lewe kolo
            drawArea1.FillEllipse(blueBrush, ((drawingArea1.Width / 2) - 15) - moveXtab, ((drawingArea1.Height / 2) - 11) - moveYtab, 30, 20); //cialo robota
            drawArea1.FillRectangle(yellowBrush, (((drawingArea1.Width / 2) - 10) - moveXtab), (((drawingArea1.Height / 2) - 7) - moveYtab), 5, 5); //lewe oko
            drawArea1.FillRectangle(yellowBrush, (((drawingArea1.Width / 2) - 10) - moveXtab), (((drawingArea1.Height / 2)) - moveYtab), 5, 5); //prawe oko

            drawArea2.FillRectangle(blackBrush, (((drawingArea1.Width / 2) - 6) - moveXtab), (((drawingArea1.Height / 2) - 15) - moveYtab), 13, 5); //prawe kolo
            drawArea2.FillRectangle(blackBrush, (((drawingArea1.Width / 2) - 6) - moveXtab), (((drawingArea1.Height / 2) + 9) - moveYtab), 13, 5); //lewe kolo
            drawArea2.FillEllipse(blueBrush, ((drawingArea1.Width / 2) - 15) - moveXtab, ((drawingArea1.Height / 2) - 11) - moveYtab, 30, 20); //cialo robota
            drawArea2.FillRectangle(yellowBrush, (((drawingArea1.Width / 2) - 10) - moveXtab), (((drawingArea1.Height / 2) - 7) - moveYtab), 5, 5); //lewe oko
            drawArea2.FillRectangle(yellowBrush, (((drawingArea1.Width / 2) - 10) - moveXtab), (((drawingArea1.Height / 2)) - moveYtab), 5, 5); //prawe oko
        }

        private void buttonResetValue_Click(object sender, EventArgs e)
        {
            serialPort1.WriteLine(dataReset);
            drawArea1.Clear(Color.White);
            drawArea2.Clear(Color.White);
            ustawienieRobota = 0;
        }

        private void buttonKalibracjaZyroskopu_Click(object sender, EventArgs e)
        {
            serialPort1.WriteLine(dataReset);
            drawArea1.Clear(Color.White);
            drawArea2.Clear(Color.White);
            ustawienieRobota = 0;
        }

        private void rysowaniePomieszczenia()
        {
            SolidBrush blackBrush = new SolidBrush(Color.Black);
            SolidBrush greenBrush = new SolidBrush(Color.DarkGreen);

            if (odczytZyroskopu > (wykonaneObroty * 80)) obrotRobota = 0;

            licznikKropek++;
            if (obrotRobota == 1)
            {
                kropkaX[licznikKropek] = 0;
                kropkaY[licznikKropek] = 0;

                kropkaXW[licznikKropek] = 0;
                kropkaYW[licznikKropek] = 0;

                switch (ustawienieRobota)
                {
                    case 0:

                        break;

                    case 1:
                        
                        if (odczytZyroskopu <= (wykonaneObroty * 45))
                        {
                            int x, y;
                            x = Convert.ToInt32(pomiarCzujnikaE);
                            y = Convert.ToInt32(Math.Sqrt(Math.Abs((x * x) - (odlegloscOdPrzeszkod * odlegloscOdPrzeszkod))));

                            kropkaXE[licznikKropek] = ((drawingArea1.Width / 2 - 4) - moveXtab - odlegloscOdPrzeszkod); //Convert.ToInt32(pomiarCzujnikaE))
                            kropkaYE[licznikKropek] = ((drawingArea1.Height / 2 - 4) - moveYtab - y);
                        }
                        else if (odczytZyroskopu >= (wykonaneObroty * 45))
                        {
                            int x, y;
                            x = Convert.ToInt32(pomiarCzujnikaE);
                            y = Convert.ToInt32(Math.Sqrt(Math.Abs((x * x) - (odlegloscOdPrzeszkod * odlegloscOdPrzeszkod))));

                            kropkaXE[licznikKropek] = ((drawingArea1.Width / 2 - 4) - moveXtab - y); //Convert.ToInt32(pomiarCzujnikaE))
                            kropkaYE[licznikKropek] = ((drawingArea1.Height / 2 - 4) - moveYtab - odlegloscOdPrzeszkod);
                        }
                        break;

                    case 2:
                        
                        if (odczytZyroskopu <= (wykonaneObroty * 45))
                        {
                            int x, y;
                            x = Convert.ToInt32(pomiarCzujnikaE);
                            y = Convert.ToInt32(Math.Sqrt(Math.Abs((x * x) - (odlegloscOdPrzeszkod * odlegloscOdPrzeszkod))));

                            kropkaXE[licznikKropek] = ((drawingArea1.Width / 2 - 4) - moveXtab + odlegloscOdPrzeszkod); //Convert.ToInt32(pomiarCzujnikaE))
                            kropkaYE[licznikKropek] = ((drawingArea1.Height / 2 - 4) - moveYtab - y);
                        }
                        else if (odczytZyroskopu >= (wykonaneObroty * 45))
                        {
                            int x, y;
                            x = Convert.ToInt32(pomiarCzujnikaE);
                            y = Convert.ToInt32(Math.Sqrt(Math.Abs((x * x) - (odlegloscOdPrzeszkod * odlegloscOdPrzeszkod))));

                            kropkaXE[licznikKropek] = ((drawingArea1.Width / 2 - 4) - moveXtab + y); //Convert.ToInt32(pomiarCzujnikaE))
                            kropkaYE[licznikKropek] = ((drawingArea1.Height / 2 - 4) - moveYtab - odlegloscOdPrzeszkod);
                        }
                        break;

                    case 3:

                        if (odczytZyroskopu <= (wykonaneObroty * 45))
                        {
                            int x, y;
                            x = Convert.ToInt32(pomiarCzujnikaE);
                            y = Convert.ToInt32(Math.Sqrt(Math.Abs((x * x) - (odlegloscOdPrzeszkod * odlegloscOdPrzeszkod))));

                            kropkaXE[licznikKropek] = ((drawingArea1.Width / 2 - 4) - moveXtab + odlegloscOdPrzeszkod); //Convert.ToInt32(pomiarCzujnikaE))
                            kropkaYE[licznikKropek] = ((drawingArea1.Height / 2 - 4) - moveYtab + y);
                        }
                        else if (odczytZyroskopu >= (wykonaneObroty * 45))
                        {
                            int x, y;
                            x = Convert.ToInt32(pomiarCzujnikaE);
                            y = Convert.ToInt32(Math.Sqrt(Math.Abs((x * x) - (odlegloscOdPrzeszkod * odlegloscOdPrzeszkod))));

                            kropkaXE[licznikKropek] = ((drawingArea1.Width / 2 - 4) - moveXtab + y); //Convert.ToInt32(pomiarCzujnikaE))
                            kropkaYE[licznikKropek] = ((drawingArea1.Height / 2 - 4) - moveYtab + odlegloscOdPrzeszkod);
                        }
                        break;

                }


            }
            else
            {
            switch (ustawienieRobota)
                {

                    case 0:
                        if (obrotRobota == 0)
                        {
                            if ((pomiarCzujnikaN > (1.1 * buforN)) && (pomiarCzujnikaN > (0.9 * buforN)))
                            {
                                kropkaX[licznikKropek] = (drawingArea1.Width / 2 - 4) - moveXtab;
                                kropkaY[licznikKropek] = ((drawingArea1.Height / 2) - 4) - moveYtab - Convert.ToInt32(pomiarCzujnikaN);
                            }

                            if ((pomiarCzujnikaE > (1.1 * buforW)) && (pomiarCzujnikaE > (0.9 * buforW)))
                            {
                                kropkaXW[licznikKropek] = ((drawingArea1.Width / 2 - 4) + Convert.ToInt32(pomiarCzujnikaW)) - moveXtab;
                                kropkaYW[licznikKropek] = ((drawingArea1.Height / 2 - 4) - moveYtab);
                            }

                            //if ((pomiarCzujnikaE > (1.1 * buforE)) && (pomiarCzujnikaE > (0.9 * buforE)))
                            //{
                                kropkaXE[licznikKropek] = ((drawingArea1.Width / 2 - 4) - Convert.ToInt32(pomiarCzujnikaE)) - moveXtab;
                                kropkaYE[licznikKropek] = ((drawingArea1.Height / 2 - 4) - moveYtab);
                            //}
                        }
                       
                        break;
                    case 1:
                        if (obrotRobota == 0)
                        {
                            kropkaX[licznikKropek] = ((drawingArea1.Width / 2 - 3) - moveXtab) + Convert.ToInt32(pomiarCzujnikaN);
                            kropkaY[licznikKropek] = ((drawingArea1.Height / 2 - 13) - moveYtab);

                            kropkaXW[licznikKropek] = ((drawingArea1.Width / 2 - 4)) - moveXtab;
                            kropkaYW[licznikKropek] = ((drawingArea1.Height / 2 - 4) - moveYtab) + Convert.ToInt32(pomiarCzujnikaW);

                            kropkaXE[licznikKropek] = ((drawingArea1.Width / 2 - 4)) - moveXtab;
                            kropkaYE[licznikKropek] = ((drawingArea1.Height / 2 - 4) - moveYtab) - Convert.ToInt32(pomiarCzujnikaE);
                        }
                            
                        break;
                    case 2:
                        if (obrotRobota == 0)
                        {
                            kropkaX[licznikKropek] = (drawingArea1.Width / 2 - 13) - moveXtab;
                            kropkaY[licznikKropek] = ((drawingArea1.Height / 2 - 3) - moveYtab) + Convert.ToInt32(pomiarCzujnikaN);

                            kropkaXW[licznikKropek] = ((drawingArea1.Width / 2 - 4) - Convert.ToInt32(pomiarCzujnikaW)) - moveXtab;
                            kropkaYW[licznikKropek] = ((drawingArea1.Height / 2 - 4) - moveYtab);

                            kropkaXE[licznikKropek] = ((drawingArea1.Width / 2 - 4) + Convert.ToInt32(pomiarCzujnikaE)) - moveXtab;
                            kropkaYE[licznikKropek] = ((drawingArea1.Height / 2 - 4) - moveYtab);
                        }
                            
                        break;
                    case 3:
                        if (obrotRobota == 0)
                        {
                            kropkaX[licznikKropek] = ((drawingArea1.Width / 2 - 3) - moveXtab) - Convert.ToInt32(pomiarCzujnikaN);
                            kropkaY[licznikKropek] = ((drawingArea1.Height / 2 - 13) - moveYtab);

                            kropkaXW[licznikKropek] = ((drawingArea1.Width / 2 - 4)) - moveXtab;
                            kropkaYW[licznikKropek] = ((drawingArea1.Height / 2 - 4) - moveYtab) - Convert.ToInt32(pomiarCzujnikaW);

                            kropkaXE[licznikKropek] = ((drawingArea1.Width / 2 - 4)) - moveXtab;
                            kropkaYE[licznikKropek] = ((drawingArea1.Height / 2 - 4) - moveYtab) + Convert.ToInt32(pomiarCzujnikaE);
                        }
                            
                        break;
                }
            }

            for (int i = 1; i <= licznikKropek; i++)
            {
                //drawArea1.FillEllipse(blackBrush, kropkaX[i], kropkaY[i], 8, 8);
                drawArea1.FillEllipse(blackBrush, kropkaXW[i], kropkaYW[i], 8, 8);
                drawArea1.FillEllipse(blackBrush, kropkaXE[i], kropkaYE[i], 8, 8);

                drawArea2.FillEllipse(blackBrush, kropkaXW[i], kropkaYW[i], 8, 8);
                drawArea2.FillEllipse(blackBrush, kropkaXE[i], kropkaYE[i], 8, 8);
            }

            switch (ustawienieRobota)
            {
                case 0:
                    //czunik N
                    drawArea1.FillEllipse(greenBrush, (drawingArea1.Width / 2 - 4) - moveXtab, ((drawingArea1.Height / 2 - 4) - moveYtab) - pomiarCzujnikaN, 8, 8);
                    drawArea1.FillEllipse(greenBrush, (drawingArea1.Width / 2 - 13) - moveXtab, ((drawingArea1.Height / 2 - 3) - moveYtab) - pomiarCzujnikaN, 6, 6);
                    drawArea1.FillEllipse(greenBrush, (drawingArea1.Width / 2 + 7) - moveXtab, ((drawingArea1.Height / 2 - 3) - moveYtab) - pomiarCzujnikaN, 6, 6);
                    //czujnik W
                    drawArea1.FillEllipse(greenBrush, ((drawingArea1.Width / 2 - 4) + pomiarCzujnikaW) - moveXtab, ((drawingArea1.Height / 2 - 4) - moveYtab), 8, 8);
                    drawArea1.FillEllipse(greenBrush, ((drawingArea1.Width / 2 - 3) + pomiarCzujnikaW) - moveXtab, ((drawingArea1.Height / 2 - 13) - moveYtab), 6, 6);
                    drawArea1.FillEllipse(greenBrush, ((drawingArea1.Width / 2 - 3) + pomiarCzujnikaW) - moveXtab, ((drawingArea1.Height / 2 + 7) - moveYtab), 6, 6);
                    //czujnik E
                    drawArea1.FillEllipse(greenBrush, ((drawingArea1.Width / 2 - 4) - pomiarCzujnikaE) - moveXtab, ((drawingArea1.Height / 2 - 4) - moveYtab), 8, 8);
                    drawArea1.FillEllipse(greenBrush, ((drawingArea1.Width / 2 - 3) - pomiarCzujnikaE) - moveXtab, ((drawingArea1.Height / 2 - 13) - moveYtab), 6, 6);
                    drawArea1.FillEllipse(greenBrush, ((drawingArea1.Width / 2 - 3) - pomiarCzujnikaE) - moveXtab, ((drawingArea1.Height / 2 + 7) - moveYtab), 6, 6);

                    //czunik N
                    drawArea2.FillEllipse(greenBrush, (drawingArea1.Width / 2 - 4) - moveXtab, ((drawingArea1.Height / 2 - 4) - moveYtab) - pomiarCzujnikaN, 8, 8);
                    drawArea2.FillEllipse(greenBrush, (drawingArea1.Width / 2 - 13) - moveXtab, ((drawingArea1.Height / 2 - 3) - moveYtab) - pomiarCzujnikaN, 6, 6);
                    drawArea2.FillEllipse(greenBrush, (drawingArea1.Width / 2 + 7) - moveXtab, ((drawingArea1.Height / 2 - 3) - moveYtab) - pomiarCzujnikaN, 6, 6);
                    //czujnik W
                    drawArea2.FillEllipse(greenBrush, ((drawingArea1.Width / 2 - 4) + pomiarCzujnikaW) - moveXtab, ((drawingArea1.Height / 2 - 4) - moveYtab), 8, 8);
                    drawArea2.FillEllipse(greenBrush, ((drawingArea1.Width / 2 - 3) + pomiarCzujnikaW) - moveXtab, ((drawingArea1.Height / 2 - 13) - moveYtab), 6, 6);
                    drawArea2.FillEllipse(greenBrush, ((drawingArea1.Width / 2 - 3) + pomiarCzujnikaW) - moveXtab, ((drawingArea1.Height / 2 + 7) - moveYtab), 6, 6);
                    //czujnik E
                    drawArea2.FillEllipse(greenBrush, ((drawingArea1.Width / 2 - 4) - pomiarCzujnikaE) - moveXtab, ((drawingArea1.Height / 2 - 4) - moveYtab), 8, 8);
                    drawArea2.FillEllipse(greenBrush, ((drawingArea1.Width / 2 - 3) - pomiarCzujnikaE) - moveXtab, ((drawingArea1.Height / 2 - 13) - moveYtab), 6, 6);
                    drawArea2.FillEllipse(greenBrush, ((drawingArea1.Width / 2 - 3) - pomiarCzujnikaE) - moveXtab, ((drawingArea1.Height / 2 + 7) - moveYtab), 6, 6);

                    break;

                case 1:
                    //czujnik N
                    drawArea1.FillEllipse(greenBrush, ((drawingArea1.Width / 2 - 4) - moveXtab) + pomiarCzujnikaN, ((drawingArea1.Height / 2 - 4) - moveYtab), 8, 8);
                    drawArea1.FillEllipse(greenBrush, ((drawingArea1.Width / 2 - 3) - moveXtab) + pomiarCzujnikaN, ((drawingArea1.Height / 2 - 13) - moveYtab), 6, 6);
                    drawArea1.FillEllipse(greenBrush, ((drawingArea1.Width / 2 - 3) - moveXtab) + pomiarCzujnikaN, ((drawingArea1.Height / 2 + 7) - moveYtab), 6, 6);
                    //czujnik W
                    drawArea1.FillEllipse(greenBrush, ((drawingArea1.Width / 2 - 4)) - moveXtab, ((drawingArea1.Height / 2 - 4) - moveYtab) + pomiarCzujnikaW, 8, 8);
                    drawArea1.FillEllipse(greenBrush, (drawingArea1.Width / 2 - 13) - moveXtab, ((drawingArea1.Height / 2 - 3) - moveYtab) + pomiarCzujnikaW, 6, 6);
                    drawArea1.FillEllipse(greenBrush, (drawingArea1.Width / 2 + 7) - moveXtab, ((drawingArea1.Height / 2 - 3) - moveYtab) + pomiarCzujnikaW, 6, 6);
                    //czujnik E
                    drawArea1.FillEllipse(greenBrush, ((drawingArea1.Width / 2 - 4)) - moveXtab, ((drawingArea1.Height / 2 - 4) - moveYtab) - pomiarCzujnikaE, 8, 8);
                    drawArea1.FillEllipse(greenBrush, (drawingArea1.Width / 2 - 13) - moveXtab, ((drawingArea1.Height / 2 - 3) - moveYtab) - pomiarCzujnikaE, 6, 6);
                    drawArea1.FillEllipse(greenBrush, (drawingArea1.Width / 2 + 7) - moveXtab, ((drawingArea1.Height / 2 - 3) - moveYtab) - pomiarCzujnikaE, 6, 6);

                    //czujnik N
                    drawArea2.FillEllipse(greenBrush, ((drawingArea1.Width / 2 - 4) - moveXtab) + pomiarCzujnikaN, ((drawingArea1.Height / 2 - 4) - moveYtab), 8, 8);
                    drawArea2.FillEllipse(greenBrush, ((drawingArea1.Width / 2 - 3) - moveXtab) + pomiarCzujnikaN, ((drawingArea1.Height / 2 - 13) - moveYtab), 6, 6);
                    drawArea2.FillEllipse(greenBrush, ((drawingArea1.Width / 2 - 3) - moveXtab) + pomiarCzujnikaN, ((drawingArea1.Height / 2 + 7) - moveYtab), 6, 6);
                    //czujni2 W
                    drawArea2.FillEllipse(greenBrush, ((drawingArea1.Width / 2 - 4)) - moveXtab, ((drawingArea1.Height / 2 - 4) - moveYtab) + pomiarCzujnikaW, 8, 8);
                    drawArea2.FillEllipse(greenBrush, (drawingArea1.Width / 2 - 13) - moveXtab, ((drawingArea1.Height / 2 - 3) - moveYtab) + pomiarCzujnikaW, 6, 6);
                    drawArea2.FillEllipse(greenBrush, (drawingArea1.Width / 2 + 7) - moveXtab, ((drawingArea1.Height / 2 - 3) - moveYtab) + pomiarCzujnikaW, 6, 6);
                    //czujnik E
                    drawArea2.FillEllipse(greenBrush, ((drawingArea1.Width / 2 - 4)) - moveXtab, ((drawingArea1.Height / 2 - 4) - moveYtab) - pomiarCzujnikaE, 8, 8);
                    drawArea2.FillEllipse(greenBrush, (drawingArea1.Width / 2 - 13) - moveXtab, ((drawingArea1.Height / 2 - 3) - moveYtab) - pomiarCzujnikaE, 6, 6);
                    drawArea2.FillEllipse(greenBrush, (drawingArea1.Width / 2 + 7) - moveXtab, ((drawingArea1.Height / 2 - 3) - moveYtab) - pomiarCzujnikaE, 6, 6);

                    break;

                case 2:
                    //czunik N
                    drawArea1.FillEllipse(greenBrush, (drawingArea1.Width / 2 - 4) - moveXtab, ((drawingArea1.Height / 2 - 4) - moveYtab) + pomiarCzujnikaN, 8, 8);
                    drawArea1.FillEllipse(greenBrush, (drawingArea1.Width / 2 - 13) - moveXtab, ((drawingArea1.Height / 2 - 3) - moveYtab) + pomiarCzujnikaN, 6, 6);
                    drawArea1.FillEllipse(greenBrush, (drawingArea1.Width / 2 + 7) - moveXtab, ((drawingArea1.Height / 2 - 3) - moveYtab) + pomiarCzujnikaN, 6, 6);
                    //czujnik W
                    drawArea1.FillEllipse(greenBrush, ((drawingArea1.Width / 2 - 4) - pomiarCzujnikaW) - moveXtab, ((drawingArea1.Height / 2 - 4) - moveYtab), 8, 8);
                    drawArea1.FillEllipse(greenBrush, ((drawingArea1.Width / 2 - 3) - pomiarCzujnikaW) - moveXtab, ((drawingArea1.Height / 2 - 13) - moveYtab), 6, 6);
                    drawArea1.FillEllipse(greenBrush, ((drawingArea1.Width / 2 - 3) - pomiarCzujnikaW) - moveXtab, ((drawingArea1.Height / 2 + 7) - moveYtab), 6, 6);
                    //czujnik E
                    drawArea1.FillEllipse(greenBrush, ((drawingArea1.Width / 2 - 4) + pomiarCzujnikaE) - moveXtab, ((drawingArea1.Height / 2 - 4) - moveYtab), 8, 8);
                    drawArea1.FillEllipse(greenBrush, ((drawingArea1.Width / 2 - 3) + pomiarCzujnikaE) - moveXtab, ((drawingArea1.Height / 2 - 13) - moveYtab), 6, 6);
                    drawArea1.FillEllipse(greenBrush, ((drawingArea1.Width / 2 - 3) + pomiarCzujnikaE) - moveXtab, ((drawingArea1.Height / 2 + 7) - moveYtab), 6, 6);

                    //czunik N
                    drawArea2.FillEllipse(greenBrush, (drawingArea1.Width / 2 - 4) - moveXtab, ((drawingArea1.Height / 2 - 4) - moveYtab) + pomiarCzujnikaN, 8, 8);
                    drawArea2.FillEllipse(greenBrush, (drawingArea1.Width / 2 - 13) - moveXtab, ((drawingArea1.Height / 2 - 3) - moveYtab) + pomiarCzujnikaN, 6, 6);
                    drawArea2.FillEllipse(greenBrush, (drawingArea1.Width / 2 + 7) - moveXtab, ((drawingArea1.Height / 2 - 3) - moveYtab) + pomiarCzujnikaN, 6, 6);
                    //czujnik W
                    drawArea2.FillEllipse(greenBrush, ((drawingArea1.Width / 2 - 4) - pomiarCzujnikaW) - moveXtab, ((drawingArea1.Height / 2 - 4) - moveYtab), 8, 8);
                    drawArea2.FillEllipse(greenBrush, ((drawingArea1.Width / 2 - 3) - pomiarCzujnikaW) - moveXtab, ((drawingArea1.Height / 2 - 13) - moveYtab), 6, 6);
                    drawArea2.FillEllipse(greenBrush, ((drawingArea1.Width / 2 - 3) - pomiarCzujnikaW) - moveXtab, ((drawingArea1.Height / 2 + 7) - moveYtab), 6, 6);
                    //czujnik E
                    drawArea2.FillEllipse(greenBrush, ((drawingArea1.Width / 2 - 4) + pomiarCzujnikaE) - moveXtab, ((drawingArea1.Height / 2 - 4) - moveYtab), 8, 8);
                    drawArea2.FillEllipse(greenBrush, ((drawingArea1.Width / 2 - 3) + pomiarCzujnikaE) - moveXtab, ((drawingArea1.Height / 2 - 13) - moveYtab), 6, 6);
                    drawArea2.FillEllipse(greenBrush, ((drawingArea1.Width / 2 - 3) + pomiarCzujnikaE) - moveXtab, ((drawingArea1.Height / 2 + 7) - moveYtab), 6, 6);

                    break;

                case 3:
                    //czujnik N
                    drawArea1.FillEllipse(greenBrush, ((drawingArea1.Width / 2 - 4) - moveXtab) - pomiarCzujnikaN, ((drawingArea1.Height / 2 - 4) - moveYtab), 8, 8);
                    drawArea1.FillEllipse(greenBrush, ((drawingArea1.Width / 2 - 3) - moveXtab) - pomiarCzujnikaN, ((drawingArea1.Height / 2 - 13) - moveYtab), 6, 6);
                    drawArea1.FillEllipse(greenBrush, ((drawingArea1.Width / 2 - 3) - moveXtab) - pomiarCzujnikaN, ((drawingArea1.Height / 2 + 7) - moveYtab), 6, 6);
                    //czujnik W
                    drawArea1.FillEllipse(greenBrush, ((drawingArea1.Width / 2 - 4)) - moveXtab, ((drawingArea1.Height / 2 - 4) - moveYtab) - pomiarCzujnikaW, 8, 8);
                    drawArea1.FillEllipse(greenBrush, (drawingArea1.Width / 2 - 13) - moveXtab, ((drawingArea1.Height / 2 - 3) - moveYtab) - pomiarCzujnikaW, 6, 6);
                    drawArea1.FillEllipse(greenBrush, (drawingArea1.Width / 2 + 7) - moveXtab, ((drawingArea1.Height / 2 - 3) - moveYtab) - pomiarCzujnikaW, 6, 6);
                    //czujnik E
                    drawArea1.FillEllipse(greenBrush, ((drawingArea1.Width / 2 - 4)) - moveXtab, ((drawingArea1.Height / 2 - 4) - moveYtab) + pomiarCzujnikaE, 8, 8);
                    drawArea1.FillEllipse(greenBrush, (drawingArea1.Width / 2 - 13) - moveXtab, ((drawingArea1.Height / 2 - 3) - moveYtab) + pomiarCzujnikaE, 6, 6);
                    drawArea1.FillEllipse(greenBrush, (drawingArea1.Width / 2 + 7) - moveXtab, ((drawingArea1.Height / 2 - 3) - moveYtab) + pomiarCzujnikaE, 6, 6);

                    drawArea2.FillEllipse(greenBrush, ((drawingArea1.Width / 2 - 4) - moveXtab) - pomiarCzujnikaN, ((drawingArea1.Height / 2 - 4) - moveYtab), 8, 8);
                    drawArea2.FillEllipse(greenBrush, ((drawingArea1.Width / 2 - 3) - moveXtab) - pomiarCzujnikaN, ((drawingArea1.Height / 2 - 13) - moveYtab), 6, 6);
                    drawArea2.FillEllipse(greenBrush, ((drawingArea1.Width / 2 - 3) - moveXtab) - pomiarCzujnikaN, ((drawingArea1.Height / 2 + 7) - moveYtab), 6, 6);
                    //czujnik W
                    drawArea2.FillEllipse(greenBrush, ((drawingArea1.Width / 2 - 4)) - moveXtab, ((drawingArea1.Height / 2 - 4) - moveYtab) - pomiarCzujnikaW, 8, 8);
                    drawArea2.FillEllipse(greenBrush, (drawingArea1.Width / 2 - 13) - moveXtab, ((drawingArea1.Height / 2 - 3) - moveYtab) - pomiarCzujnikaW, 6, 6);
                    drawArea2.FillEllipse(greenBrush, (drawingArea1.Width / 2 + 7) - moveXtab, ((drawingArea1.Height / 2 - 3) - moveYtab) - pomiarCzujnikaW, 6, 6);
                    //czujnik E
                    drawArea2.FillEllipse(greenBrush, ((drawingArea1.Width / 2 - 4)) - moveXtab, ((drawingArea1.Height / 2 - 4) - moveYtab) + pomiarCzujnikaE, 8, 8);
                    drawArea2.FillEllipse(greenBrush, (drawingArea1.Width / 2 - 13) - moveXtab, ((drawingArea1.Height / 2 - 3) - moveYtab) + pomiarCzujnikaE, 6, 6);
                    drawArea2.FillEllipse(greenBrush, (drawingArea1.Width / 2 + 7) - moveXtab, ((drawingArea1.Height / 2 - 3) - moveYtab) + pomiarCzujnikaE, 6, 6);
                    break;
            }
        }

        //AUTOMATYCZNE SKANOWANIE

        private void button_algorytmPierwszy_Click(object sender, EventArgs e)
        {
            textBox_wybranyAlgorytm.Text = "Pierwszy";
            odlegloscOdPrzeszkod = 20;
            wybranyAlgorytm = 1;
        }

        private void button_algorytmDrugi_Click(object sender, EventArgs e)
        {
            textBox_wybranyAlgorytm.Text = "Drugi";
            odlegloscOdPrzeszkod = 30;
            wybranyAlgorytm = 2;
        }

        private void rysowanieRamkiAutomatyczneSkanowanie()
        {
            //NARYSOWANE STAKI
            //OBRAMOWANIE RAMKI I WYPEŁNIENIE
            SolidBrush whiteBrush = new SolidBrush(Color.White);
            drawArea2.FillRectangle(whiteBrush, 0, 0, 670, 630);
            //linie pionowe
            Pen lightGrayPen = new Pen(Color.LightGray);
            for (int i = 1; i < 67; i++)
            {
                drawArea2.DrawLine(lightGrayPen, i * 10, 0, i * 10, 670);
            }
            for (int i = 1; i < 63; i++)
            {
                drawArea2.DrawLine(lightGrayPen, 0, i * 10, 670, i * 10);
            }
            //obramowanie
            Pen blackPen = new Pen(Color.Black);
            drawArea2.DrawLine(blackPen, 0, 0, 0, 630);
            drawArea2.DrawLine(blackPen, 0, 0, 670, 0);
            drawArea2.DrawLine(blackPen, 670, 0, 670, 630);
            drawArea2.DrawLine(blackPen, 0, 630, 670, 630);

            Pen greenPen = new Pen(Color.Green);
            drawArea2.DrawLine(greenPen, 600, 610, 650, 610);
            drawArea2.DrawLine(greenPen, 600, 605, 600, 615);
            drawArea2.DrawLine(greenPen, 650, 605, 650, 615);

            var font = new Font("TimesNewRoman", 15, FontStyle.Regular, GraphicsUnit.Pixel);
            drawArea2.DrawString("50cm", font, Brushes.Green, new Point(605, 590));

        }

        private void button_rozpocznijSkanowanie_Click(object sender, EventArgs e)
        {
            rozpoczecieAutomatycznegoSkanowania = 1;
        }

        private void AutomatyczneSkanowanie(object sender, EventArgs e)
        {
            //wysylanie komend do robota do sterowania
            rysowanieRamkiAutomatyczneSkanowanie();

            if (wybranyAlgorytm == 1)
            {

            }

            if (wybranyAlgorytm == 2)
            {

            }
        }

        private void button_automatyczneSkanowanieReset_Click(object sender, EventArgs e)
        {
            rozpoczecieAutomatycznegoSkanowania = 0;
            drawArea2.Clear(Color.White);
        }

        private void button_automatyczneSkanowanieStop_Click(object sender, EventArgs e)
        {
            rozpoczecieAutomatycznegoSkanowania = 0;
        }

        private void button_zapisDanych_Click(object sender, EventArgs e)
        {
            string x, y, daneDoZapisu;
            string sciezkaPliku = textBoxSciezkaPliku.Text;
            string path = sciezkaPliku; //sciezka zapisu pliku
            StreamWriter sw;

            if (!File.Exists(path))
            {
                sw = File.CreateText(path);
            }
            else
            {
                sw = new StreamWriter(path, true);
            }
            //ZAPIS DANYCH DOT. DROGI PRZEBYTEJ PRZEZ ROBOTA
            sw.WriteLine("DROGA PRZEBYTA PRZEZ ROBOTA");
            sw.WriteLine("Wspolrzedne x:    Wspolrzedne Y:");
            sw.WriteLine("0                 0");
            for (int i = 0; i < licznikProstych; i++)
            {
                x = Convert.ToString(xProstej[i] - 335);
                y = Convert.ToString(yProstej[i] - 315);
                daneDoZapisu = x + "               " + y;
                sw.WriteLine(daneDoZapisu);
            }

            sw.WriteLine("WYKRYTE PRZESZKODY:");
            sw.WriteLine("Wspolrzedne x:    Wspolrzedne Y:");
            for (int i = 0; i < licznikKropek; i++)
            {
                if ((kropkaX[i] != 0) && (kropkaY[i] != 0))
                {
                    x = Convert.ToString(kropkaX[i] - 335);
                    y = Convert.ToString(kropkaY[i] - 315);
                    daneDoZapisu = x + "               " + y;
                    sw.WriteLine(daneDoZapisu);
                }

                if ((kropkaXW[i] != 0) && (kropkaYW[i] != 0))
                {
                    x = Convert.ToString(kropkaXW[i] - 335);
                    y = Convert.ToString(kropkaYW[i] - 315);
                    daneDoZapisu = x + "               " + y;
                    sw.WriteLine(daneDoZapisu);
                }

                if ((kropkaXE[i] != 0) && (kropkaYE[i] != 0))
                {
                    x = Convert.ToString(kropkaXE[i] - 335);
                    y = Convert.ToString(kropkaYE[i] - 315);
                    daneDoZapisu = x + "               " + y;
                    sw.WriteLine(daneDoZapisu);
                }
            }

            sw.Close();
        }

    }
}





    /*
     * czyszczenie:
     * drawArea1.Clear(Color.white)
     */
