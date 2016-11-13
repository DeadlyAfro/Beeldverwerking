using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

using Vector = System.Windows.Vector;

namespace INFOIBV
{
	public partial class INFOIBV : Form
	{
		private Bitmap InputImage;
		private Bitmap OutputImage;

		private int WIDTH, HEIGHT;

		public INFOIBV()
		{
			InitializeComponent();
		}

		private void LoadImageButton_Click(object sender, EventArgs e)
		{
			if (openImageDialog.ShowDialog() == DialogResult.OK) // Open File Dialog
			{
				string file = openImageDialog.FileName;       // Get the file name
				imageFileName.Text = file;                    // Show file name
				if (InputImage != null) InputImage.Dispose(); // Reset image
				InputImage = new Bitmap(file);                // Create new Bitmap from file
				if (InputImage.Size.Height <= 0 || InputImage.Size.Width <= 0 ||
					InputImage.Size.Height > 512 || InputImage.Size.Width > 512) // Dimension check
					MessageBox.Show("Error in image dimensions (have to be > 0 and <= 512)");
				else
					pictureBox1.Image = (Image)InputImage;   // Display input image
			}
		}

		private void applyButton_Click(object sender, EventArgs e)
		{
			if (InputImage == null) return;                   // Get out if no input image
			if (OutputImage != null) OutputImage.Dispose();   // Reset output image
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
					Image[x, y] = InputImage.GetPixel(x, y);  // Set pixel color in array at (x,y)
				}
			}

			// ==========================================================================================
			WIDTH = InputImage.Size.Width;
			HEIGHT = InputImage.Size.Height;

			const int THRESHOLD = 10;
			const int MINIMUM_DETECTION_PIXELS = 128;
			const int MAXIMUM_BOUNDARY_ERROR = 25;

			Color[,] grayscaleImage = ConvertToGrayscale(Image);

			float[,] floatImage = ConvertToFloat(grayscaleImage);

			float[,] edgeImage = DetectEdges(floatImage);

			float[,] thresholdImage = ApplyThreshold(edgeImage, THRESHOLD);

			// float[,] morphedImage = MorphologicalTransform(thresholdImage);

			Detection[] detectedObjects = FloodFillExtraction(thresholdImage);

			Detection[] filteredObjects = FilterBySize(detectedObjects, MINIMUM_DETECTION_PIXELS);

			float[,] referenceImage = ImportReferenceImage();
			float[,] referenceEdges = DetectEdges(referenceImage);
			float[,] referenceThreshold = ApplyThreshold(referenceEdges, THRESHOLD);
			Detection referenceObject = FloodFillExtraction(referenceThreshold)[1];

			Detection[] targetObjects = FindTargetObjects(filteredObjects, referenceObject.BoundaryCurve, MAXIMUM_BOUNDARY_ERROR);

			Image = DisplayFoundObjects(targetObjects);

			//float[,] normalizedImage = NormalizeFloatArray(thresholdImage);
			//Image = ConvertToImage(normalizedImage);

			// ==========================================================================================

			// Copy array to output Bitmap
			for (int x = 0; x < WIDTH; x++)
			{
				for (int y = 0; y < HEIGHT; y++)
				{
					OutputImage.SetPixel(x, y, Image[x, y]); // Set the pixel color at coordinate (x,y)
				}
			}

			pictureBox2.Image = (Image)OutputImage;           // Display output image
			progressBar.Visible = false;                      // Hide progress bar
		}

		private void saveButton_Click(object sender, EventArgs e)
		{
			if (OutputImage == null) return;                  // Get out if no output image
			if (saveImageDialog.ShowDialog() == DialogResult.OK)
				OutputImage.Save(saveImageDialog.FileName);   // Save the output image
		}

		// ===== DEBUG FUNCTIONS =====

		private float[,] NormalizeFloatArray(float[,] input) // Output: [0,1] - ONLY USED FOR DEBUG DRAWINGS
		{
			float[,] output = new float[WIDTH, HEIGHT];
			progressBar.Value = progressBar.Minimum;

			float MIN = float.MaxValue, MAX = float.MinValue; // Initialize 

			for (int x = 0; x < WIDTH; x++) // Iterate once to collect all necessary variables for normalizing
			{
				for (int y = 0; y < HEIGHT; y++)
				{
					float value = input[x, y];

					MIN = Math.Min(MIN, value);
					MAX = Math.Max(MAX, value);

					progressBar.PerformStep();
				}
			}

			progressBar.Value = progressBar.Minimum;

			for (int x = 0; x < WIDTH; x++) // Loop again to remap all values
			{
				for (int y = 0; y < HEIGHT; y++)
				{
					float value = input[x, y];

					if (MAX - MIN == 0) // Catch devide-by-zero
						throw new Exception("SHIT");

					value = (value - MIN) / (MAX - MIN); // Remap values to [0,1]

					if (value < 0 || value > 1 || float.IsNaN(value)) // Just in case
						throw new Exception("ALSO SHIT");

					output[x, y] = value;

					progressBar.PerformStep();
				}
			}

			progressBar.Value = progressBar.Minimum;
			return output;
		}

		private Color[,] ConvertToImage(float[,] input) // Expects [0,1] - ONLY USED FOR DEBUG DRAWINGS
		{
			Color[,] output = new Color[WIDTH, HEIGHT];
			progressBar.Value = progressBar.Minimum;

			for (int x = 0; x < WIDTH; x++)
			{
				for (int y = 0; y < HEIGHT; y++)
				{
					float value = input[x, y];
					int grayValue = (int)Math.Round(value * 255); // Stretch to byte

					output[x, y] = Color.FromArgb(grayValue, grayValue, grayValue); // Set as gray

					progressBar.PerformStep();
				}
			}

			progressBar.Value = progressBar.Minimum;
			return output;
		}

		// ===== PROCESSING FUNCTIONS =====

		private Color[,] ConvertToGrayscale(Color[,] input)
		{
			Color[,] output = new Color[WIDTH, HEIGHT];
			progressBar.Value = progressBar.Minimum;

			for (int x = 0; x < WIDTH; x++)
			{
				for (int y = 0; y < HEIGHT; y++)
				{
					Color inputColor = input[x, y];

					int grayscale = (int)(inputColor.R * 0.3 + inputColor.G * 0.59 + inputColor.B * 0.11); // Calculate grayscale value

					output[x, y] = Color.FromArgb(grayscale, grayscale, grayscale); // Set as output

					progressBar.PerformStep();
				}
			}

			progressBar.Value = progressBar.Minimum;
			return output;
		}

		private float[,] ConvertToFloat(Color[,] input)
		{
			float[,] output = new float[WIDTH, HEIGHT];
			progressBar.Value = progressBar.Minimum;

			for (int x = 0; x < WIDTH; x++)
				for (int y = 0; y < HEIGHT; y++)
				{
					output[x, y] = input[x, y].R; // Assuming grayscaling has been applied, all channels should contain the same gray value

					progressBar.PerformStep();
				}

			progressBar.Value = progressBar.Minimum;
			return output;
		}

		public float[,] DetectEdges(float[,] input)
		{
			float[,] output = new float[WIDTH, HEIGHT];
			progressBar.Value = progressBar.Minimum;

			for (int x = 0; x < WIDTH; x++)
			{
				for (int y = 0; y < HEIGHT; y++)
				{
					float value = 0;

					if (0 < x && x < WIDTH - 1)
						value += Math.Abs((-input[x - 1, y] + input[x + 1, y]) / 3f); // Add horizontal (-1, 0, 1) kernel as absolute value
					if (0 < y && y < HEIGHT - 1)
						value += Math.Abs((-input[x, y - 1] + input[x, y + 1]) / 3f); // Add vertical (-1, 0, 1) kernel as absolute value

					output[x, y] = value;

					progressBar.PerformStep();
				}
			}

			progressBar.Value = progressBar.Minimum;
			return output;
		}

		private float[,] ApplyThreshold(float[,] input, float threshold)
		{
			float[,] output = new float[WIDTH, HEIGHT];
			progressBar.Value = progressBar.Minimum;

			for (int x = 0; x < WIDTH; x++)
			{
				for (int y = 0; y < HEIGHT; y++)
				{
					if (input[x, y] > threshold)
						output[x, y] = 1;
					else
						output[x, y] = 0;

					progressBar.PerformStep();
				}
			}

			progressBar.Value = progressBar.Minimum;
			return output;
		}

		private Detection[] FloodFillExtraction(float[,] input)
		{
			progressBar.Value = progressBar.Minimum;

			// STAGE 0: Copy the input to an array that can be manipulated
			int[,] flood = new int[WIDTH, HEIGHT];
			for (int x = 0; x < WIDTH; x++)
				for (int y = 0; y < HEIGHT; y++)
				{
					flood[x, y] = (int)input[x, y]; // Copy the input to an array we can process

					progressBar.PerformStep();
				}

			progressBar.Value = progressBar.Minimum;

			// STAGE 1: Use flood fill to find all objects and assign an identifier to all their pixels
			int ObjectIdentifier = 2; // At this stage, 0 should represent an object and 1 should represent an edge.
			for (int x = 0; x < WIDTH; x++)
			{
				for (int y = 0; y < HEIGHT; y++)
				{
					if (flood[x, y] == 0) // Discovered new object
					{
						Queue<Point> work = new Queue<Point>(); // Keep track of BFS frontier
						work.Enqueue(new Point(x, y)); // Start with the point we just found

						while (work.Count > 0) // Continue until every pixel of the object has been processed
						{
							Point p = work.Dequeue();

							flood[p.X, p.Y] = ObjectIdentifier; // Make sure current point is set as object

							for (int i = -1; i <= 1; i++) // In a 3x3 square around the current pixel
							{
								for (int j = -1; j <= 1; j++)
								{
									if (p.X + i < 0 || p.X + i == WIDTH || p.Y + j < 0 || p.Y + j == HEIGHT) // Check if we are within boundaries
										continue;

									if (flood[p.X + i, p.Y + j] == 0) // Check if the pixel belongs to the object
									{
										work.Enqueue(new Point(p.X + i, p.Y + j)); // Add pixel to queue
										flood[p.X + i, p.Y + j] = ObjectIdentifier; // Set pixel as found
									}
								}
							}
						}

						ObjectIdentifier++; // Increase the counter for the next detection
					}

					progressBar.PerformStep();
				}
			}

			progressBar.Value = progressBar.Minimum;

			// STAGE 2: Group all pixels of each object
			List<List<Point>> extract = new List<List<Point>>(); // Create list to store objects
			for (int x = 0; x < WIDTH; x++)
			{
				for (int y = 0; y < HEIGHT; y++)
				{
					if (flood[x, y] > 1) // Part of object found
					{
						int Identifier = flood[x, y] - 2; // Subtract the offset from stage 1

						if (extract.Count < Identifier) // Should never happen. Zero-based Count should also give leading index for new objects
							throw new Exception("SHIT");

						if (extract.Count == Identifier) // Object not yet stored
							extract.Add(new List<Point>()); // Create new object

						extract[Identifier].Add(new Point(x, y)); // Add point to the correct object
					}

					progressBar.PerformStep();
				}
			}

			progressBar.Value = progressBar.Minimum;

			// STAGE 3: Reconstruct a detected object from its pixels.
			int temp = progressBar.Maximum;
			progressBar.Maximum = extract.Count;

			Detection[] output = new Detection[extract.Count];
			for (int i = 0; i < extract.Count; i++)
			{
				output[i] = new Detection(extract[i].ToArray()); // Create Detection objects with list of pixels

				progressBar.PerformStep();
			}

			progressBar.Maximum = temp;
			progressBar.Value = progressBar.Minimum;
			return output.ToArray();
		}

		private Detection[] FilterBySize(Detection[] input, int minimumPixelCount)
		{
			List<Detection> output = new List<Detection>();

			foreach (Detection obj in input)
				if (obj.Size > minimumPixelCount) // Filter out all objects with a surface smaller than the minimal pixelsize squared
					output.Add(obj);

			return output.ToArray();
		}

		private float[,] ImportReferenceImage()
		{
			float[,] output = new float[WIDTH, HEIGHT];
			progressBar.Value = progressBar.Minimum;

			Bitmap referenceImage = new Bitmap(Application.StartupPath + "\\ReferenceImage.png");

			for (int x = 0; x < WIDTH; x++)
				for (int y = 0; y < HEIGHT; y++)
				{
					if (referenceImage.GetPixel(x, y).R > 128) // Just to be sure
						output[x, y] = 255;
					else
						output[x, y] = 0;

					progressBar.PerformStep();
				}

			progressBar.Value = progressBar.Minimum;
			return output;
		}

		private Detection[] FindTargetObjects(Detection[] input, float[] referenceCurve, float threshold)
		{
			List<Detection> outputList = new List<Detection>();
			progressBar.Value = progressBar.Minimum;

			foreach (Detection d in input) // Loop over all detected objects
			{
				float minSquaredDifferenceSum = float.MaxValue; // Float to store the smallest difference, set to highest value so any value smaller will replace this

				for (int offset = 0; offset < Detection.ANGLE_RESOLUTION; offset++) // Loop over all rotation offsets
				{
					float squaredDifferenceSum = 0.0f;

					for (int i = offset; i < Detection.ANGLE_RESOLUTION + offset; i++) // Use the angle of rotation as an offset when looping through the arrays
					{
						int originalIndex = i - offset;
						int offsetIndex = i % Detection.ANGLE_RESOLUTION;

						float difference = d.BoundaryCurve[offsetIndex] - referenceCurve[originalIndex];
						squaredDifferenceSum += difference * difference; // Add the squared difference to the sum
					}

					minSquaredDifferenceSum = Math.Min(minSquaredDifferenceSum, squaredDifferenceSum); // Only remember the lowest error
				}

				if (minSquaredDifferenceSum <= threshold) // Check if the lowest error is within threshold
					outputList.Add(d);

				progressBar.PerformStep();
			}

			progressBar.Value = progressBar.Minimum;
			return outputList.ToArray();
		}

		private Color[,] DisplayFoundObjects(Detection[] foundObjects)
		{
			const int INCREMENT = 30; // Variable that determines the increase in color for each new object

			Color[,] output = new Color[WIDTH, HEIGHT];
			progressBar.Value = progressBar.Minimum;

			for (int x = 0; x < WIDTH; x++) // Setup a new black image
				for (int y = 0; y < HEIGHT; y++)
				{
					output[x, y] = Color.Black;

					progressBar.PerformStep();
				}

			progressBar.Value = progressBar.Minimum;
			int temp = progressBar.Maximum;
			progressBar.Maximum = foundObjects.Length;

			int grayValue = INCREMENT; // Initalize a color
			foreach (Detection d in foundObjects)
			{
				foreach (Point p in d.Points) // Loop over all pixels of the detected object
					output[p.X, p.Y] = Color.FromArgb(grayValue, grayValue, grayValue);

				grayValue = (grayValue + INCREMENT) % 256; // Wrap the gray value to keep within 256 range

				progressBar.PerformStep();
			}

			progressBar.Value = progressBar.Minimum;
			progressBar.Maximum = temp;
			return output;
		}
	}

	class Detection
	{
		public const int ANGLE_RESOLUTION = 720;
		public const double SCAN_INTERVAL = 360.0 / ANGLE_RESOLUTION;
		public const int STEPS_PER_PIXEL = 8;

		// Only produce GET functions to outside objects
		public int Left
		{
			get; private set;
		}
		public int Right
		{
			get; private set;
		}
		public int Top
		{
			get; private set;
		}
		public int Bottom
		{
			get; private set;
		}
		public int Size
		{
			get { return Points.Length; }
		}

		public Point[] Points
		{
			get; private set;
		}
		public PointF Center
		{
			get; private set;
		}
		public float[] BoundaryCurve
		{
			get; private set;
		}

		public Detection(Point[] points)
		{
			Points = points;
			CalculateBoundingBox();
			CalculateCenter();

			CalculateBoundary();
			NormalizeBoundary();
		}

		private void CalculateBoundingBox()
		{
			Left = int.MaxValue;
			Right = int.MinValue;
			Top = int.MaxValue;
			Bottom = int.MinValue;

			foreach (Point p in Points) // Calculate the correct values for each property
			{
				Left = Math.Min(Left, p.X);
				Right = Math.Max(Right, p.X);
				Top = Math.Min(Top, p.Y);
				Bottom = Math.Max(Bottom, p.Y);
			}
		}

		private void CalculateCenter()
		{
			float totalX = 0.0f;
			float totalY = 0.0f;

			for (int i = 0; i < Size; i++) // Give each pixel the same weight and calculate the average
			{
				totalX += Points[i].X;
				totalY += Points[i].Y;
			}

			Center = new PointF(totalX / Size, totalY / Size);
		}

		private void CalculateBoundary()
		{
			// Create a temporary bool[,] representation of the object
			int Width = Right - Left + 1, Height = Bottom - Top + 1;
			bool[,] workspace = new bool[Width, Height];

			foreach (Point p in Points) // Set all pixels of the object to True
				workspace[p.X - Left, p.Y - Top] = true;

			BoundaryCurve = new float[ANGLE_RESOLUTION]; // Initialize an array to store the results
			Vector CenterVec = new Vector(Center.X - Left, Center.Y - Top); // Calculate the center of the workspace

			for (int i = 0; i < ANGLE_RESOLUTION; i++) // Loop over the requested number of directions
			{
				// Construct a vector to walk over the workspace with
				double degrees = i * SCAN_INTERVAL;
				double radians = degrees * Math.PI / 180;
				Vector vec = new Vector(Math.Cos(radians), Math.Sin(radians)) / STEPS_PER_PIXEL;
				Vector pos = CenterVec; // Set the starting position of the walker

				while (pos.X > 0 && pos.X < Width - 1 && pos.Y > 0 && pos.Y < Height - 1) // Keep walking until we run outside the workspace
				{
					int x = (int)Math.Round(pos.X);
					int y = (int)Math.Round(pos.Y);

					if (workspace[x, y]) // If our current position is part of the object
										 // Overwrite any previous value, which means that we detect the outside edges
										 // The default value is 0, which means that no detection defaults to 0
						BoundaryCurve[i] = (float)(new Vector(pos.X - CenterVec.X, pos.Y - CenterVec.Y)).Length;

					pos += vec; // Move the walker
				}
			}
		}

		private void NormalizeBoundary()
		{
			float value = 0;

			for (int i = 0; i < ANGLE_RESOLUTION; i++) // Find highest distance to boundary
				value = Math.Max(value, BoundaryCurve[i]); // All distances are 0 or higher

			if (value > 0) // Prevent divide-by-zero
				for (int i = 0; i < ANGLE_RESOLUTION; i++)
					BoundaryCurve[i] /= value; // Normalize
		}
	}
}