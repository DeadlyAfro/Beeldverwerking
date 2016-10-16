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
        private Color[,] ImageOut;
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
                    pictureBox1.Image = (Image) InputImage;                 // Display input image
            }
        }

        private void applyButton_Click(object sender, EventArgs e)
        {
            if (InputImage == null) return;                                 // Get out if no input image
            if (OutputImage != null) OutputImage.Dispose();                 // Reset output image
            OutputImage = new Bitmap(InputImage.Size.Width, InputImage.Size.Height); // Create new output image
            Image = new Color[InputImage.Size.Width, InputImage.Size.Height]; // Create array to speed-up operations (Bitmap functions are very slow)
            ImageOut = new Color[InputImage.Size.Width, InputImage.Size.Height];
            // Setup progress bar
            progressBar.Visible = true;
            progressBar.Minimum = 1;
            progressBar.Maximum = InputImage.Size.Width * InputImage.Size.Height;
            progressBar.Value = 1;
            progressBar.Step = 1;

            // Copy input Bitmap to array            
            for (int x = 0; x < InputImage.Size.Width; x++)
            {
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    Image[x, y] = InputImage.GetPixel(x, y);                // Set pixel color in array at (x,y)
                }
            }

            //==========================================================================================
            // TODO: include here your own code
            for (int x = 0; x < InputImage.Size.Width; x++)
            {
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    Color pixelColor = Image[x, y];                         // Get the pixel color at coordinate (x,y)
                    int avgColor = (int)(pixelColor.R *0.3 + pixelColor.G*0.59 + pixelColor.B * 0.11);
                    //if (avgColor < 128 || avgColor > 200) avgColor = 0; //(double)Threshhold -> Window slicing
                    Color updatedColor = Color.FromArgb(avgColor, avgColor, avgColor); //Grayscale image
                    //Color updatedColor = Color.FromArgb(255 - pixelColor.R, 255 - pixelColor.G, 255 - pixelColor.B); // Negative image
                    Image[x, y] = updatedColor;                             // Set the new pixel color at coordinate (x,y)
                    progressBar.PerformStep();                              // Increment progress bar
                }
            }
            for (int x = 1; x < InputImage.Size.Width - 1; x++)
            {
                for (int y = 1; y < InputImage.Size.Height - 1; y++)
                {
                    Color pixelColor = Image[x, y];                         // Get the pixel color at coordinate (x,y)
                    int color = DetectEdges(x, y);
                    Color updatedColor = Color.FromArgb(color, color, color); //Edged image
                    ImageOut[x, y] = updatedColor;                             // Set the new pixel color at coordinate (x,y)
                }
            }
            //==========================================================================================

            // Copy array to output Bitmap
            for (int x = 0; x < InputImage.Size.Width; x++)
            {
                for (int y = 0; y < InputImage.Size.Height; y++)
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
            int colorOut = 0;
            int colorOut1 = 0;
            int colorOut2 = 0;
            int colorOut3 = 0;

            colorOut += Image[x - 1, y + -1].R * -1;    //kernel
            colorOut += Image[x, y - 1].R * -1;         //-1-1-1
            colorOut += Image[x + 1, y - 1].R * -1;     // 0 0 0
            colorOut += Image[x - 1, y + 1].R * 1;      // 1 1 1
            colorOut += Image[x, y + 1].R * 1;
            colorOut += Image[x + 1, y + 1].R * 1;
            if (colorOut < 0)
                colorOut = 0;
            colorOut /= 6;

            colorOut1 += Image[x - 1, y + -1].R * 1;    //kernel
            colorOut1 += Image[x, y - 1].R * 1;         // 1 1 1
            colorOut1 += Image[x + 1, y - 1].R * 1;     // 0 0 0
            colorOut1 += Image[x - 1, y + 1].R * -1;    //-1-1-1
            colorOut1 += Image[x, y + 1].R * -1;
            colorOut1 += Image[x + 1, y + 1].R * -1;
            if (colorOut1 < 0)
                colorOut1 = 0;
            colorOut1 /= 6;

            colorOut2 += Image[x - 1, y + -1].R * 1;    //kernel
            colorOut2 += Image[x - 1, y].R * 1;         // 1 0-1
            colorOut2 += Image[x - 1, y + 1].R * 1;     // 1 0-1
            colorOut2 += Image[x + 1, y + -1].R * -1;   // 1 0-1
            colorOut2 += Image[x + 1, y].R * -1;
            colorOut2 += Image[x + 1, y + 1].R * -1;
            if (colorOut2 < 0)
                colorOut2 = 0;
            colorOut2 /= 6;

            colorOut3 += Image[x - 1, y + -1].R * -1;   //kernel
            colorOut3 += Image[x - 1, y].R * -1;        //-1 0 1
            colorOut3 += Image[x - 1, y + 1].R * -1;    //-1 0 1
            colorOut3 += Image[x + 1, y + -1].R * 1;    //-1 0 1
            colorOut3 += Image[x + 1, y].R * 1;
            colorOut3 += Image[x + 1, y + 1].R * 1;
            if (colorOut3 < 0)
                colorOut3 = 0;
            colorOut3 /= 6;

            colorOut += colorOut3 + colorOut2 + colorOut1;
            return colorOut;
        }
    }
}
