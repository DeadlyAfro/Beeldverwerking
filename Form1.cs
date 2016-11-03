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

			Color[,] grayscaleImage = ConvertToGrayscale(Image);

			float[,] floatImage = ConvertToFloat(grayscaleImage);

			float[,] edgeImage = DetectEdges(floatImage);

			float[,] thresholdImage = ApplyThreshold(edgeImage, 5);


			// TODO: Extract objects using floodfill (requires new class to store objects? Could be used to split work and keep additional information, such as original location etc.)

			// TODO: Find center of each object

			// TODO: Scan object for shape

			// TODO: Normalize shape curve

			// TODO: Compare curve with reference (which needs to be constructed)

			// TODO: Show detections on original image

			float[,] normalizedImage = NormalizeFloats(thresholdImage);
			ImageOut = ConvertToImage(normalizedImage);

			//objectColor = 1;
			//searchForObjects();
			//for (int x = 0; x < Width; x++)
			//{
			//	for (int y = 0; y < Height; y++)
			//	{
			//		int color = Objects[x, y];
			//		Color updatedColor = Color.FromArgb(color, color, color); //object image
			//		ImageOut[x, y] = updatedColor;                             // Set the new pixel color at coordinate (x,y)
			//	}
			//}

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

		private float[,] NormalizeFloats(float[,] input)
		{
			float[,] output = new float[Width, Height];
			progressBar.Value = progressBar.Minimum;

			float MIN = float.MaxValue, MAX = float.MinValue;

			for (int x = 0; x < Width; x++)
			{
				for (int y = 0; y < Height; y++)
				{
					float value = input[x, y];

					if (value < MIN)
						MIN = value;
					if (value > MAX)
						MAX = value;

					progressBar.PerformStep(); // Increment progress bar
				}
			}

			progressBar.Value = progressBar.Minimum;

			MAX += MIN; // Offset MAX to allow for easy division to normalize value

			for (int x = 0; x < Width; x++)
			{
				for (int y = 0; y < Height; y++)
				{
					float value = input[x, y];

					value += MIN;
					value /= MAX;

					output[x, y] = value;

					progressBar.PerformStep(); // Increment progress bar
				}
			}

			progressBar.Value = progressBar.Minimum;
			return output;
		}

		private Color[,] ConvertToImage(float[,] input)
		{
			Color[,] output = new Color[Width, Height];
			progressBar.Value = progressBar.Minimum;

			for (int x = 0; x < Width; x++)
			{
				for (int y = 0; y < Height; y++)
				{
					float value = input[x, y];
					int grayValue = (int)Math.Round(value * 255);

					output[x, y] = Color.FromArgb(grayValue, grayValue, grayValue);

					progressBar.PerformStep(); // Increment progress bar
				}
			}

			progressBar.Value = progressBar.Minimum;
			return output;
		}

		private Color[,] ConvertToGrayscale(Color[,] input)
		{
			Color[,] output = new Color[Width, Height];
			progressBar.Value = progressBar.Minimum;

			for (int x = 0; x < Width; x++)
			{
				for (int y = 0; y < Height; y++)
				{
					Color inputColor = Image[x, y];
					int grayscale = (int)(inputColor.R * 0.3 + inputColor.G * 0.59 + inputColor.B * 0.11); // Calculate grayscale value
					Color outputColor = Color.FromArgb(grayscale, grayscale, grayscale);
					output[x, y] = outputColor;

					progressBar.PerformStep();
				}
			}

			progressBar.Value = progressBar.Minimum;
			return output;
		}

		private float[,] ConvertToFloat(Color[,] input)
		{
			float[,] output = new float[Width, Height];
			progressBar.Value = progressBar.Minimum;

			for (int x = 0; x < Width; x++)
			{
				for (int y = 0; y < Height; y++)
				{
					float value = input[x, y].R; // Calculate grayscale value
					output[x, y] = value; // Save to output

					progressBar.PerformStep(); // Increment progress bar
				}
			}

			progressBar.Value = progressBar.Minimum;
			return output;
		}

		public float[,] DetectEdges(float[,] input)
		{
			float[,] output = new float[Width, Height];
			progressBar.Value = progressBar.Minimum;

			for (int x = 0; x < Width; x++)
			{
				for (int y = 0; y < Height; y++)
				{
					float value = 0;

					if (0 < x && x < Width - 1)
						value += (-input[x - 1, y] + input[x + 1, y]) / 3f; // Horizontal (-1, 0, 1) kernel
					if (0 < y && y < Height - 1)
						value += (-input[x, y - 1] + input[x, y + 1]) / 3f; // Vertical (-1, 0, 1) kernel

					output[x, y] = value;

					progressBar.PerformStep(); // Increment progress bar
				}
			}

			progressBar.Value = progressBar.Minimum;
			return output;
		}

		private float[,] ApplyThreshold(float[,] input, float threshold)
		{
			float[,] output = new float[Width, Height];
			progressBar.Value = progressBar.Minimum;

			for (int x = 0; x < Width; x++)
			{
				for (int y = 0; y < Height; y++)
				{
					if (input[x, y] > threshold)
						output[x, y] = 1;
					else
						output[x, y] = 0;

					progressBar.PerformStep(); // Increment progress bar
				}
			}

			progressBar.Value = progressBar.Minimum;
			return output;
		}

		private void saveButton_Click(object sender, EventArgs e)
		{
			if (OutputImage == null) return;                                // Get out if no output image
			if (saveImageDialog.ShowDialog() == DialogResult.OK)
				OutputImage.Save(saveImageDialog.FileName);                 // Save the output image
		}

		public void checkNeighbours(int x, int y)
		{
			Objects[x, y] = objectColor;
			if (x > 0)                                                          //stay within image
				if (Edges[x - 1, y] == 0 && Objects[x - 1, y] == 0)         //if the pixel on the left is black
				{
					checkNeighbours(x - 1, y);                                  //recusrively check for connected pixels
				}
			if (x < Width - 1)                                                      //stay within image
				if (Edges[x + 1, y] == 0 && Objects[x + 1, y] == 0)         //if the pixel on the right is black
				{
					checkNeighbours(x + 1, y);                                  //recusrively check for connected pixels
				}
			if (y > 0)                                                          //stay within image
				if (Edges[x, y - 1] == 0 && Objects[x, y - 1] == 0)         //if the pixel above is black
				{
					checkNeighbours(x, y - 1);                                  //recusrively check for connected pixels
				}
			if (y < Height - 1)                                                     //stay within image
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
