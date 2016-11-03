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

		private int Width, Height;

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
					pictureBox1.Image = (Image)InputImage;                 // Display input image
			}
		}

		private void applyButton_Click(object sender, EventArgs e)
		{
			if (InputImage == null) return;                                 // Get out if no input image
			if (OutputImage != null) OutputImage.Dispose();                 // Reset output image
			OutputImage = new Bitmap(InputImage.Size.Width, InputImage.Size.Height); // Create new output image
			Color[,] Image = new Color[InputImage.Size.Width, InputImage.Size.Height]; // Create array to speed-up operations (Bitmap functions are very slow)

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

			Color[,] grayscaleImage = ConvertToGrayscale(Image);

			float[,] floatImage = ConvertToFloat(grayscaleImage);

			float[,] edgeImage = DetectEdges(floatImage);

			float[,] thresholdImage = ApplyThreshold(edgeImage, 5);

			Detection[] detectedObjects = FloodFillExtraction(thresholdImage);

			// TODO: Extract objects using floodfill (requires new class to store objects? Could be used to split work and keep additional information, such as original location etc.)

			// TODO: Find center of each object

			// TODO: Scan object for shape

			// TODO: Normalize shape curve

			// TODO: Compare curve with reference (which needs to be constructed)

			// TODO: Show detections on original image

			float[,] normalizedImage = NormalizeFloats(thresholdImage);
			Image = ConvertToImage(normalizedImage);

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
					OutputImage.SetPixel(x, y, Image[x, y]);               // Set the pixel color at coordinate (x,y)
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

		// ===== PROCESSING FUNCTIONS =====

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

			for (int x = 0; x < Width; x++)
			{
				for (int y = 0; y < Height; y++)
				{
					float value = input[x, y];

					if (MAX - MIN == 0) // Catch devide-by-zero
						throw new Exception("SHIT");

					value = (value - MIN) / (MAX - MIN);

					if (value < 0 || value > 1 || float.IsNaN(value)) // Still something went wrong
						throw new Exception("ALSO SHIT");

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
					{
						value += Math.Abs((-input[x - 1, y] + input[x + 1, y]) / 3f); // Horizontal (-1, 0, 1) kernel
						value += Math.Abs((input[x - 1, y] - input[x + 1, y]) / 3f); // Horizontal (1, 0, -1) kernel
					}
					if (0 < y && y < Height - 1)
					{
						value += Math.Abs((-input[x, y - 1] + input[x, y + 1]) / 3f); // Vertical (-1, 0, 1) kernel
						value += Math.Abs((input[x, y - 1] - input[x, y + 1]) / 3f); // Vertical (1, 0, -1) kernel
					}

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

		private Detection[] FloodFillExtraction(float[,] input)
		{
			// STAGE 0: Copy the input to an array that can be manipulated
			int[,] flood = new int[Width, Height];
			for (int x = 0; x < Width; x++)
				for (int y = 0; y < Height; y++)
					flood[x, y] = (int)input[x, y]; // Copy the input to an array we can process

			progressBar.Value = progressBar.Minimum;

			// STAGE 1: Use flood fill to find all objects and assign an identifier to all their pixels
			int ObjectIdentifier = 2; // At this stage, 0 should represent an object and 1 should represent an edge.
			for (int x = 0; x < Width; x++)
			{
				for (int y = 0; y < Height; y++)
				{
					if (input[x, y] == 0) // Discovered new object
					{
						Queue<Point> work = new Queue<Point>(); // Keep track of BFS frontier
						work.Enqueue(new Point(x, y)); // Start with the point we just found

						while (work.Count > 0) // Continue until every pixel of the object has been processed
						{
							Point p = work.Dequeue();

							flood[p.X, p.Y] = ObjectIdentifier;

							if (flood[p.X - 1, p.Y] == 0)
								work.Enqueue(new Point(p.X - 1, p.Y));
							if (flood[p.X + 1, p.Y] == 0)
								work.Enqueue(new Point(p.X - 1, p.Y));
							if (flood[p.X, p.Y - 1] == 0)
								work.Enqueue(new Point(p.X - 1, p.Y));
							if (flood[p.X, p.Y + 1] == 0)
								work.Enqueue(new Point(p.X - 1, p.Y));
						}

						ObjectIdentifier++; // Increase the counter for the next detection
					}

					progressBar.PerformStep(); // Increment progress bar
				}
			}

			progressBar.Value = progressBar.Minimum;

			// STAGE 2: Group all pixels of each object
			List<List<Point>> extract = new List<List<Point>>();
			for (int x = 0; x < Width; x++)
			{
				for (int y = 0; y < Height; y++)
				{
					if (flood[x, y] > 1) // Part of object found
					{
						int Identifier = flood[x, y] - 2;

						if (extract.Count < Identifier) // Should never happen
							throw new Exception("SHIT");

						if (extract.Count == Identifier) // Object not yet stored
							extract.Add(new List<Point>());

						extract[Identifier].Add(new Point(x, y)); // Add point to the correct object
					}

					progressBar.PerformStep(); // Increment progress bar
				}
			}

			// STAGE 3: Reconstruct a detected object from its pixels.
			Detection[] output = new Detection[extract.Count];
			for (int i = 0; i < extract.Count; i++)
				output[i] = new Detection(extract[i].ToArray());

			progressBar.Value = progressBar.Minimum;
			return output.ToArray();
		}
	}

	class Detection
	{
		private Point[] point;

		public Detection(Point[] point)
		{
			this.point = point;
		}
	}
}
