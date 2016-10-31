using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace INFOIBV
{
    public partial class INFOIBV : Form
    {
        private Bitmap InputImage;
        private Bitmap OutputImage;
        private Color[,] Image;
        private float[,] ImageCalc;
        private int[,] Edges;
        private int[,] Objects;
        public int objectColor;
        private Color[,] ImageOut;
        private int Height, Width;
        public INFOIBV()
        {
            InitializeComponent();
        }

        private void LoadImageButton_Click(object sender, EventArgs e)
        {
            if (openImageDialog.ShowDialog() == DialogResult.OK)             // Open File Dialog
            {
                string file = openImageDialog.FileName;                     // Get the file name
                imageFileName.Text = file;                                  // Show file name
                if (InputImage != null) InputImage.Dispose();               // Reset image
                InputImage = new Bitmap(file);                              // Create new Bitmap from file
                if (InputImage.Size.Height <= 0 || InputImage.Size.Width <= 0 ||
                    InputImage.Size.Height > 512 || InputImage.Size.Width > 512) // Dimension check
                    MessageBox.Show("Error in image dimensions (have to be > 0 and <= 512)");
                else
                {
                    pictureBox1.Image = (Image)InputImage;                 // Display input image
                    Height = InputImage.Size.Height;
                    Width = InputImage.Size.Width;
                }
            }
        }

        private void applyButton_Click(object sender, EventArgs e)
        {
            if (InputImage == null) return;                                 // Get out if no input image
            if (OutputImage != null) OutputImage.Dispose();                 // Reset output image
            OutputImage = new Bitmap(Width, Height); // Create new output image
            Image = new Color[Width, Height]; // Create array to speed-up operations (Bitmap functions are very slow)
            ImageCalc = new float[Width, Height];
            Edges = new int[Width, Height];
            Objects = new int[Width, Height];
            ImageOut = new Color[Width, Height];
            // Setup progress bar
            progressBar.Visible = true;
            progressBar.Minimum = 1;
            progressBar.Maximum = Width * Height;
            progressBar.Value = 1;
            progressBar.Step = 1;

            // Copy input Bitmap to array            
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    Image[x, y] = InputImage.GetPixel(x, y);                // Set pixel color in array at (x,y)
                }
            }

            //==========================================================================================
            // TODO: include here your own code
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    Color pixelColor = Image[x, y];                                                         // Get the pixel color at coordinate (x,y)
                    int avgColor = (int)(pixelColor.R * 0.3 + pixelColor.G * 0.59 + pixelColor.B * 0.11);
                    //if (avgColor < 32 || avgColor > 200) avgColor = 0; //(double)Threshhold -> Window slicing
                    ImageCalc[x, y] = avgColor;
                    Color updatedColor = Color.FromArgb(avgColor, avgColor, avgColor);                      //Grayscale image
                                                                                                            //Color updatedColor = Color.FromArgb(255 - pixelColor.R, 255 - pixelColor.G, 255 - pixelColor.B); // Negative image
                    Image[x, y] = updatedColor;                                                             // Set the new pixel color at coordinate (x,y)
                    progressBar.PerformStep();                                                              // Increment progress bar
                }
            }
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    int color = DetectEdges(x, y);
                    Edges[x, y] = color;
                    Objects[x, y] = 0;
                    Color updatedColor = Color.FromArgb(color, color, color); //Edged image
                    ImageOut[x, y] = updatedColor;                             // Set the new pixel color at coordinate (x,y)
                }
            }
            objectColor = 1;
            searchForObjects();
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    int color = Objects[x, y];
                    Color updatedColor = Color.FromArgb(color, color, color); //object image
                    ImageOut[x, y] = updatedColor;                             // Set the new pixel color at coordinate (x,y)
                }
            }
            //==========================================================================================

            // Copy array to output Bitmap
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    OutputImage.SetPixel(x, y, ImageOut[x, y]);               // Set the pixel color at coordinate (x,y)
                }
            }

            pictureBox2.Image = (Image)OutputImage;                         // Display output image
            progressBar.Visible = false;                                    // Hide progress bar
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            if (OutputImage == null) return;                                // Get out if no output image
            if (saveImageDialog.ShowDialog() == DialogResult.OK)
                OutputImage.Save(saveImageDialog.FileName);                 // Save the output image
        }
        public int DetectEdges(int x, int y)
        {
            float colorOut = 0;
            float colorOut1 = 0;
            float colorOut2 = 0;
            float colorOut3 = 0;

            if (x == 0 || x == Width - 1 || y == 0 || y == Height - 1)   //handle side pixels of the image, make them into an edge
                colorOut = 255;
            else
            {
                colorOut += ImageCalc[x - 1, y + -1] * -1;    //kernel
                colorOut += ImageCalc[x, y - 1] * -1;         //-1-1-1
                colorOut += ImageCalc[x + 1, y - 1] * -1;     // 0 0 0
                colorOut += ImageCalc[x - 1, y + 1] * 1;      // 1 1 1
                colorOut += ImageCalc[x, y + 1] * 1;
                colorOut += ImageCalc[x + 1, y + 1] * 1;
                if (colorOut < 0)
                    colorOut = 0;
                //colorOut /= 6;

                colorOut1 += ImageCalc[x - 1, y + -1] * 1;    //kernel
                colorOut1 += ImageCalc[x, y - 1] * 1;         // 1 1 1
                colorOut1 += ImageCalc[x + 1, y - 1] * 1;     // 0 0 0
                colorOut1 += ImageCalc[x - 1, y + 1] * -1;    //-1-1-1
                colorOut1 += ImageCalc[x, y + 1] * -1;
                colorOut1 += ImageCalc[x + 1, y + 1] * -1;
                if (colorOut1 < 0)
                    colorOut1 = 0;
                //colorOut1 /= 6;

                colorOut2 += ImageCalc[x - 1, y + -1] * 1;    //kernel
                colorOut2 += ImageCalc[x - 1, y] * 1;         // 1 0-1
                colorOut2 += ImageCalc[x - 1, y + 1] * 1;     // 1 0-1
                colorOut2 += ImageCalc[x + 1, y + -1] * -1;   // 1 0-1
                colorOut2 += ImageCalc[x + 1, y] * -1;
                colorOut2 += ImageCalc[x + 1, y + 1] * -1;
                if (colorOut2 < 0)
                    colorOut2 = 0;
                //colorOut2 /= 6;

                colorOut3 += ImageCalc[x - 1, y + -1] * -1;   //kernel
                colorOut3 += ImageCalc[x - 1, y] * -1;        //-1 0 1
                colorOut3 += ImageCalc[x - 1, y + 1] * -1;    //-1 0 1
                colorOut3 += ImageCalc[x + 1, y + -1] * 1;    //-1 0 1
                colorOut3 += ImageCalc[x + 1, y] * 1;
                colorOut3 += ImageCalc[x + 1, y + 1] * 1;
                if (colorOut3 < 0)
                    colorOut3 = 0;
                //colorOut3 /= 6;

                colorOut += colorOut3 + colorOut2 + colorOut1;
                if (colorOut / 4 < 32)
                    colorOut = 0;
                else colorOut = 255;
            }
            return (int)colorOut;
        }
        public void checkNeighbours(int x, int y)
        {
            Objects[x, y] = objectColor;
            if (x > 0)                                                          //stay within image
                if (Edges[x - 1, y] == 0 && Objects[x - 1, y] == 0)         //if the pixel on the left is black
                {
                    checkNeighbours(x - 1, y);                                  //recusrively check for connected pixels
                }
            if (x < Width -1)                                                      //stay within image
                if (Edges[x + 1, y] == 0 && Objects[x + 1, y] == 0)         //if the pixel on the right is black
                {
                    checkNeighbours(x + 1, y);                                  //recusrively check for connected pixels
                }
            if (y > 0)                                                          //stay within image
                if (Edges[x, y - 1] == 0 && Objects[x, y - 1] == 0)         //if the pixel above is black
                {
                    checkNeighbours(x, y - 1);                                  //recusrively check for connected pixels
                }
            if (y < Height -1)                                                     //stay within image
                if (Edges[x, y + 1] == 0 && Objects[x, y + 1] == 0)         //if the pixel beneath is black
                {
                    checkNeighbours(x, y + 1);                                  //recusrively check for connected pixels
                }

        }

        public void searchForObjects()
        {
            for (int x = 0; x < Width; x++)
            {                                                       //for all pixels in the image
                for (int y = 0; y < Height; y++)
                {
                    if (Edges[x, y] == 0 && Objects[x, y] == 0)    //check if they are black
                    {
                        checkNeighbours(x, y);                      //and see if they are connected to any other black pixels
                        objectColor += 20;
                    }
                    
                }
            }
        }
    }
}
