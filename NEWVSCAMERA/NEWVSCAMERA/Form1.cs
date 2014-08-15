using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using AForge.Video;
using AForge;
using AForge.Video.DirectShow;
using System.Drawing.Imaging;
using System.IO;
using AForge.Imaging.Filters;
using AForge.Imaging.Textures;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Forms.DataVisualization;
using NEWVSCAMERA.Properties;

namespace NEWVSCAMERA
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            //remove grid from chart
            this.chart1.ChartAreas[0].AxisX.MajorGrid.LineWidth = 0;
            this.chart1.ChartAreas[0].AxisY.MajorGrid.LineWidth = 0;
            this.chart2.ChartAreas[0].AxisX.MajorGrid.LineWidth = 0;
            this.chart2.ChartAreas[0].AxisY.MajorGrid.LineWidth = 0;
            this.chart3.ChartAreas[0].AxisX.MajorGrid.LineWidth = 0;
            this.chart3.ChartAreas[0].AxisY.MajorGrid.LineWidth = 0;
            saveToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.S;
            openToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.O;
            exitToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.E;
            this.chart1.ChartAreas[0].AxisX.Title = "X-axis"; 
            this.chart1.ChartAreas[0].AxisY.Title = "Brightness across x";
            this.chart2.ChartAreas[0].AxisX.Title = "Y-axis";
            this.chart2.ChartAreas[0].AxisY.Title = "Brightness across y";
            this.chart3.ChartAreas[0].AxisX.Title = "Z position";
            this.chart3.ChartAreas[0].AxisY.Title = "Waist Position";
        }
        #region
        private FilterInfoCollection CaptureDevice;
        private VideoCaptureDevice FinalFrame;
        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            ImagePanel.Zoom = trackBar1.Value * 0.02f;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            CaptureDevice = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            foreach (FilterInfo Device in CaptureDevice)
            {
                comboBox1.Items.Add(Device.Name);
            }
            comboBox1.SelectedIndex = 0;
            FinalFrame = new VideoCaptureDevice();

            //set window location
            if (Settings.Default.WindowLocation != null)
            {
                this.Location = Settings.Default.WindowLocation;
            }
            //set window size
            if (Settings.Default.WindowSize != null)
            {
                this.Size = Settings.Default.WindowSize;
            }
        }

        private void Start_button(object sender, EventArgs e)
        {//video start
            FinalFrame = new VideoCaptureDevice(CaptureDevice[comboBox1.SelectedIndex].MonikerString);
            FinalFrame.NewFrame += new NewFrameEventHandler(FinalFrame_NewFrame); // delegate,newframe = event
            FinalFrame.Start();
        }

        void FinalFrame_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            pictureBox1.Image = (Bitmap)eventArgs.Frame.Clone();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {//close the video , terminate frame taking.
            if (FinalFrame.IsRunning == true)
            {
                FinalFrame.Stop();
            }
            //Copy window location to app settings
            Settings.Default.WindowLocation = this.Location;
            if (this.WindowState == FormWindowState.Normal)
            {
                Settings.Default.WindowSize = this.Size;
            }
            //Copy window size to app settings
            else
            {
                Settings.Default.WindowSize = this.RestoreBounds.Size;

            }
            //save the settings
            Settings.Default.Save();
        }



        //capture button
        private void Capture_button(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null)
            { ImagePanel.Image = (Bitmap)pictureBox1.Image.Clone(); }
            else
            { MessageBox.Show("Please press start button before capturing any image", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void Saving_button(object sender, EventArgs e)
        {//saving button
            string filename = textBox1.Text;
            if (ImagePanel.Image != null)
            {
                if (filename == "")
                { MessageBox.Show("Please enter file name."); }
                else
                {
                    ImagePanel.Image.Save(@"C:\a\" + filename + ".bmp", ImageFormat.Bmp);
                    ImagePanel.Image.Dispose();
                    MessageBox.Show("File " + filename + " has been saved.");
                }
            }//catch exception
            else { MessageBox.Show("Image has not been captured"); }
        }
        //open a picture
        private void Open_Button(object sender, System.EventArgs e)
        {
            try
            {
                OpenFileDialog open = new OpenFileDialog();
                open.Filter = "Image Files(*.jpg; *.jpeg; *.gif; *.bmp;*.png)|*.jpg; *.jpeg; *.gif; *.bmp;*.png";
                if (open.ShowDialog() == DialogResult.OK)
                {
                    Bitmap oldBmp = new Bitmap(open.FileName);

                    ImagePanel.Image = oldBmp;

                }
            }
            catch (Exception)
            {
                throw new ApplicationException("Failed loading image");
            }
        }
        private void Brightest_button(object sender, EventArgs e)
        {
            // Create a Bitmap object from an image file.
            if (ImagePanel.Image != null)
            {
                Bitmap myBitmap = new Bitmap(ImagePanel.Image);

                int width; int height;
                int xstart, xend, ystart, yend;

                Change(out xstart, out ystart, out xend, out yend, out width, out height);
                int k = 0;
                double[,] brightness_array = new double[width * height, 3]; // first is number of element, second is dimension.

                for (int i = xstart; i < width + xstart; i++)
                {

                    for (int j = ystart; j < height + ystart; j++)
                    {
                        Color pixelColor = myBitmap.GetPixel(i, j);
                        //double brightness = 0.2126 * pixelColor.R + 0.7152 * pixelColor.G + 0.0722 * pixelColor.B;
                        double brightness = (0.333 * pixelColor.R + 0.333 * pixelColor.G + (1 - 0.333 * 2) * pixelColor.B) / 255;
                        // Jony: 0.33 for each colour, respond equally to all colours "Detecting Photons"
                        // This algorithm is called luma or BT-709, used to calculate brightness
                        // Human don't perceive brightness all equally, so have to adjust the coefficients.

                        brightness_array[k, 0] = i;
                        brightness_array[k, 1] = j;
                        brightness_array[k, 2] = brightness;
                        k++;
                    }
                }

                double max_brightness = 0.0;
                int positionX = 0;
                int positionY = 0;
                for (int m = 0; m < k; m++)
                {
                    if (brightness_array[m, 2] > max_brightness)
                    {
                        positionX = Convert.ToInt32(brightness_array[m, 0]);
                        positionY = Convert.ToInt32(brightness_array[m, 1]);
                        max_brightness = brightness_array[m, 2];
                    }
                }
                label8.Text = (positionX.ToString() + "," + positionY.ToString() + "  " + max_brightness.ToString());
                //PixelFormat pixelFormat = myBitmap.PixelFormat;

                HelperClass.brightest_x = positionX;
                HelperClass.brightest_y = positionY;
            }
            else
            {
                MessageBox.Show("No image was loaded.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            plotGaussianX();
            plotGaussianY();
            if (textBox6.Text != "")
            {
                try
                {
                    label13.Text = (plotGaussianX() * Double.Parse(textBox6.Text)).ToString();
                    label14.Text = (plotGaussianY() * Double.Parse(textBox6.Text)).ToString();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Please type integers only e.g. 500000");
                }
            }
            else
            {
                label13.Text = plotGaussianX().ToString("G3");
                label14.Text = plotGaussianY().ToString("G3");
            }

        }


        private void Change(out int xstart, out int ystart, out  int xend, out  int yend, out int width, out int height)
        {
            if (textBox2.Text != "" && textBox3.Text != "" && textBox4.Text != "" && textBox5.Text != "")
            {
                xstart = Int32.Parse(textBox2.Text);
                xend = Int32.Parse(textBox3.Text);
                ystart = Int32.Parse(textBox4.Text);
                yend = Int32.Parse(textBox5.Text);
                width = xend - xstart;
                height = yend - ystart;

            }

            else
            {
                xstart = xend = ystart = yend = 0;
                width = ImagePanel.Image.Width;
                height = ImagePanel.Image.Height;

            }

        }


        private void ApplyFilter(IFilter filter)
        {
            Bitmap sourceImage = (Bitmap)ImagePanel.Image;
            // apply filter
            Bitmap filteredImage = null;
            if (sourceImage != null)
            {
                filteredImage = filter.Apply(sourceImage);
                // display filtered image
            }
            else { MessageBox.Show("No image was loaded.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            ImagePanel.Image = filteredImage;
        }

        //Apply filter to the source image and show the filtered image

        //change property of the camera including exposure

        public void DisplayPropertyPage(IntPtr parentWindow)
        { }

        private void Property_button(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null) // since image = object
                FinalFrame.DisplayPropertyPage(IntPtr.Zero); //This will display a form with camera controls
            else
            {
                MessageBox.Show("Properties are not available", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); ;
            }
        }

        private double plotGaussianY()
        {
            double[] GuessParameterArray = new double[4];
            GaussianFit fit = new GaussianFit();
            int m = 0;
            int yrange;
            SetDefault(out yrange);
            double[] arrayY = new double[yrange + 1];
            double[] arrayB = new double[yrange + 1];


            double brightness = 0;
            int ystart = HelperClass.brightest_y - yrange / 2;
            int yend = HelperClass.brightest_y + yrange / 2;
            Bitmap mybitmap = new Bitmap(ImagePanel.Image);
            //Feeding in array X and array Y data points (Y: brightness)
            for (int i = ystart; i <= yend; i++)
            {
                Color pixel = mybitmap.GetPixel(HelperClass.brightest_x, i);
                brightness = (double)pixel.B * 0.33 + pixel.G * 0.33 + pixel.R * (1 - 0.33 * 2);
                arrayY[m] = i;
                arrayB[m] = brightness;
                m++;
            }

            double[] arrayparameters = new double[4];
            GuessParameterArray = fit.SuggestParameters(arrayY, arrayB);
            arrayparameters[0] = GuessParameterArray[0];
            arrayparameters[1] = GuessParameterArray[1];
            arrayparameters[2] = GuessParameterArray[2];
            arrayparameters[3] = GuessParameterArray[3];

            fit.Fit(arrayY, arrayB, arrayparameters);
            double[] fittedB = fit.FittedValues;

            for (int j = 0; j < m; j++)
            {
                PlotXYAppend(this.chart2, this.chart2.Series[0], arrayY[j], fittedB[j]);
                PlotXYAppend(this.chart2, this.chart2.Series[1], arrayY[j], arrayB[j]);
            }
            return fit.FittedParameters[3];
        }
            
        private double plotGaussianX()
        {
            double[] GuessParameterArray = new double[4];
            GaussianFit fit = new GaussianFit();
            int m = 0;
            int xrange;
            SetDefault(out xrange);
            double[] arrayX = new double[xrange + 1];
            double[] arrayB = new double[xrange + 1];


            double brightness = 0;
            int xstart = HelperClass.brightest_x - xrange / 2;
            int xend = HelperClass.brightest_x + xrange / 2;
            Bitmap mybitmap = new Bitmap(ImagePanel.Image);
            //Feeding in array X and array Y data points (Y: brightness)
            for (int i = xstart; i <= xend; i++)
            {
                Color pixel = mybitmap.GetPixel(i, HelperClass.brightest_y);
                brightness = (double)pixel.B * 0.33 + pixel.G * 0.33 + pixel.R * (1 - 0.33 * 2);
                arrayX[m] = i;
                arrayB[m] = brightness;
                m++;
            }

            double[] arrayparameters = new double[4];
            GuessParameterArray = fit.SuggestParameters(arrayX, arrayB);
            arrayparameters[0] = GuessParameterArray[0];
            arrayparameters[1] = GuessParameterArray[1];
            arrayparameters[2] = GuessParameterArray[2];
            arrayparameters[3] = GuessParameterArray[3];

            fit.Fit(arrayX, arrayB, arrayparameters);
            double[] fittedY = fit.FittedValues;
            //Y HERE REFERS TO BRIGHTNESS
            for (int j = 0; j < m; j++)
            {
                PlotXYAppend(this.chart1, this.chart1.Series[0], arrayX[j], fittedY[j]);
                PlotXYAppend(this.chart1, this.chart1.Series[1], arrayX[j], arrayB[j]);

            }
            return fit.FittedParameters[3];
        }
        private void SetDefault(out int xrange)
        {
            if (textBox9.Text != "")
            {
                xrange = Int32.Parse(textBox9.Text);
            }


            else
            {
                xrange = 30;

            }

        }
        private delegate int PlotXYDelegate(double x, double y);

        private void PlotXYAppend(Chart chart, Series dataSeries, double x, double y)
        {
            chart.Invoke(new PlotXYDelegate(dataSeries.Points.AddXY), new Object[] { x, y });
        }

        #endregion

        private void greyFilterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ApplyFilter(Grayscale.CommonAlgorithms.BT709);
            if (ImagePanel.Image != null)
            {
                greyFilterToolStripMenuItem.Checked = true;
            }
            else
            {
                greyFilterToolStripMenuItem.Checked = false;
            }

        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Images|*.png;*.bmp;*.jpg";
            ImageFormat format = ImageFormat.Png;
            if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string ext = System.IO.Path.GetExtension(sfd.FileName);
                switch (ext)
                {
                    case ".jpg":
                        format = ImageFormat.Jpeg;
                        break;
                    case ".bmp":
                        format = ImageFormat.Bmp;
                        break;

                }
                try
                { ImagePanel.Image.Save(sfd.FileName, format); }
                catch (Exception ex)
                {
                    MessageBox.Show("No image was loaded.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void button8_Click(object sender, EventArgs e)
        {

            var myListOfObjects = new List<MyClass>();

            for (var i = 0; i <= 20; i++)
            {
                myListOfObjects.Add(new MyClass(i));
                myListOfObjects[i].pic = myListOfObjects[i].OpenAbitmap();
                myListOfObjects[i].BrightestPoint();
                myListOfObjects[i].waist = myListOfObjects[i].GaussianFit();

            }
            myListOfObjects[0].Zpos= 0;
            myListOfObjects[1].Zpos= 0.635;
            myListOfObjects[2].Zpos= 1.27;
            myListOfObjects[3].Zpos= 1.905;
            myListOfObjects[4].Zpos= 2.54;
            myListOfObjects[5].Zpos= 3.175;
            myListOfObjects[6].Zpos= 3.81;
            myListOfObjects[7].Zpos= 4.445;
            myListOfObjects[8].Zpos= 5.08;
            myListOfObjects[9].Zpos= 5.715;
            myListOfObjects[10].Zpos= 6.35;
            myListOfObjects[11].Zpos= 6.985;
            myListOfObjects[12].Zpos= 7.62;
            myListOfObjects[13].Zpos= 8.255;
            myListOfObjects[14].Zpos= 8.89;
            myListOfObjects[15].Zpos= 9.525;
            myListOfObjects[16].Zpos= 10.16;
            myListOfObjects[17].Zpos= 10.795;
            myListOfObjects[18].Zpos= 11.43;
            myListOfObjects[19].Zpos= 12.065;
            myListOfObjects[20].Zpos = 12.7;

            List<double> Zposlist = new List<double>();
            for (int j = 0; j <= 20; j++)
            {
                Zposlist.Add(myListOfObjects[j].Zpos);
            }
            double[] Zarray = Zposlist.ToArray();
            List<double> waistlist = new List<double>();
            for (int k = 0; k <= 20; k++)
            {
                waistlist.Add(myListOfObjects[k].waist);
            }
            double[] waistarray = waistlist.ToArray();
            for (int m = 0; m <= 20; m++)
            {
                PlotXYAppend(this.chart3, this.chart3.Series[0], Zarray[m], waistlist[m]);
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            foreach (var series in this.chart1.Series)
            {
                series.Points.Clear();
            }
            foreach (var series in this.chart2.Series)
            {
                series.Points.Clear();
            }
        }


    }
}
public class GaussianFit
{
    public string Name;
    double[] fittedValues;
    protected double[] lastFittedParameters;
    protected alglib.ndimensional_pfunc model;

    public GaussianFit() //Constructor
    {
        Name = "Gaussian";
        model = gaussian;
    }

    // Fit the data provided. The parameters list should contain an initial
    // guess. On return it will contain the fitted parameters.
    public void Fit(double[] xdata, double[] ydata, double[] parameters)
    {
        double[,] xDataAlg = new double[xdata.Length, 1];
        for (int i = 0; i < xdata.Length; i++) xDataAlg[i, 0] = xdata[i];
        double epsf = 0;
        double epsx = 0.000000001;
        int maxits = 0;
        int info;
        alglib.lsfitstate state; 
        alglib.lsfitreport rep;
        double diffstep = 0.001;

        alglib.lsfitcreatef(xDataAlg, ydata, parameters, diffstep, out state);
        alglib.lsfitsetcond(state, epsf, epsx, maxits);
        alglib.lsfitfit(state, model, null, null);
        alglib.lsfitresults(state, out info, out parameters, out rep);

        //calculate the fitted values
        fittedValues = new double[xdata.Length];
        for (int i = 0; i < xdata.Length; i++)
        {
            double yValue = 0;
            double[] xValueArr = new double[] { xdata[i] };
            model(parameters, xValueArr, ref yValue, null);
            fittedValues[i] = yValue;
        }
        lastFittedParameters = parameters;
    }

    protected void gaussian(double[] parameters, double[] x, ref double func, object obj)
    {
        double n = parameters[0];//y-shift
        double q = parameters[1];//prefactor of the Gaussian
        double c = parameters[2];//mean
        double w = parameters[3];//waist


        if (w == 0) w = 0.001; // watch out for divide by zero
        func = n + q * Math.Exp(-(2*Math.Pow(x[0] - c, 2)) / Math.Pow(w, 2)); 
    }

    // This returns the y-values for the model at the x-data points it was evaluated at.
    public double[] FittedValues
    {
        get
        {
            return fittedValues; 
        }
    }

    public double[] FittedParameters
    {
        get
        {
            return lastFittedParameters;
        }
    }

    public double[] SuggestParameters(double[] xDat, double[] yDat)
    {
        double cGuess;
        double qGuess;
        double wGuess;
        double nGuess;
        double ySum = 0;
        double yMax = 0;
        double yMin = yDat[0];
        double xMin = xDat[0];
        double xMax = xDat[0];
        int yMaxIndex = 0;
        // calculate the maxima, the minima, and the integral
        for (int i = 0; i < yDat.Length; i++)
        {
            ySum += yDat[i];
            if (yDat[i] > yMax)
            {
                yMax = yDat[i];
                yMaxIndex = i;
            }
            if (yDat[i] < yMin) yMin = yDat[i];
            if (xDat[i] < xMin) xMin = xDat[i];
            if (xDat[i] > xMax) xMax = xDat[i];
        }

        //nGuess = yDat[0];
        nGuess = yMin;
        cGuess = xDat[yMaxIndex];
        qGuess = yMax - nGuess;
        wGuess = 0.3 * ((xMax - xMin) / xDat.Length) * (ySum - (yDat.Length * nGuess)) / qGuess; // the 0.25 is chosen fairly arbitrarily

        double[] guess = new double[] { nGuess, qGuess, cGuess, wGuess };
        return guess;
    }
}

public static class HelperClass
{
    private  static int myVar;

    public static int brightest_x
    {
        get { return myVar; }
        set { myVar = value; }
    }

    private static int myVar1;

    public static int brightest_y
    {
        get { return myVar1; }
        set { myVar1 = value; }
    }
    
}


public class MyClass
{
    public MyClass(int i)
    {
        _index = i;
        Bitmap pic = null; // must exist to be used (local variable otherwise infinite loop)
    }
    private int _index;

    public Bitmap pic { get; set; }
    public int BrightestXpos { get; set; }
    public int BrightestYpos { get; set; }
    public double waist { get; set; }
    public Bitmap OpenAbitmap()
    {
        string filepath = @"C:\Users\James\Desktop\A\" + _index.ToString() + ".bmp";
        Bitmap picture = new Bitmap(filepath);
        return picture;
    }
    public double Zpos { get; set; }
    public void BrightestPoint()
    {

        int width = pic.Width; int height = pic.Height;
        int xstart = 0, ystart = 0;
        int k = 0;
        double[,] brightness_array = new double[width * height, 3]; // first is number of element, second is dimension.

        for (int i = xstart; i < width + xstart; i++)
        {
            for (int j = ystart; j < height + ystart; j++)
            {
                Color pixelColor = pic.GetPixel(i, j);
                double brightness = (0.333 * pixelColor.R + 0.333 * pixelColor.G + (1 - 0.333 * 2) * pixelColor.B) / 255;
                brightness_array[k, 0] = i;
                brightness_array[k, 1] = j;
                brightness_array[k, 2] = brightness;
                k++;
            }
        }

        double max_brightness = 0.0;
        int positionX = 0;
        int positionY = 0;
        for (int m = 0; m < k; m++)
        {
            if (brightness_array[m, 2] > max_brightness)
            {
                positionX = Convert.ToInt32(brightness_array[m, 0]);
                positionY = Convert.ToInt32(brightness_array[m, 1]);
                max_brightness = brightness_array[m, 2];
            }
        }
        BrightestXpos = positionX;
        BrightestYpos = positionY;
    }
    public double GaussianFit()
    {

        double[] GuessParameterArray = new double[4];
        GaussianFit fit = new GaussianFit();
        int m = 0;
        int xrange = 20;
        double[] arrayX = new double[xrange + 1];
        double[] arrayB = new double[xrange + 1];

        double brightness = 0;
        int xstart = BrightestXpos - xrange / 2;
        int xend = BrightestXpos + xrange / 2;
        //Feeding in array X and array Y data points (Y: brightness)
        for (int i = xstart; i <= xend; i++)
        {
            Color pixel = pic.GetPixel(i, BrightestYpos);
            brightness = (double)pixel.B * 0.33 + pixel.G * 0.33 + pixel.R * (1 - 0.33 * 2);
            arrayX[m] = i;
            arrayB[m] = brightness;//arrayB = raw data
            m++;
        }

        double[] arrayparameters = new double[4];
        GuessParameterArray = fit.SuggestParameters(arrayX, arrayB);
        return GuessParameterArray[3] * 2;
    }
}