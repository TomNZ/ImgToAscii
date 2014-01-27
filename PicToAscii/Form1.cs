using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PicToAscii
{
    public partial class Form1 : Form
    {
        // Constants for determining which letters to use
        private const string _FONT_FACE = "Courier New";
        private const int _FONT_SIZE = 12;
        private int[] _AVAILABLE_CHAR_IDS = new int[] {
            32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47,
            48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63,
            64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 
            80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93, 94, 95,
            96, 97, 98, 99, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109,
            110, 111, 112, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122,
            123, 124, 125, 126, 176, 177, 178, 219, 254
        };

        private const double _LETTER_ASPECT_RATIO = 0.5;

        // Cache the letter "density" values
        private bool _brightnessesComputed = false;
        private Dictionary<double, int> _brightnesses;
        private double _minBrightness = 1;

        public Form1()
        {
            InitializeComponent();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            // Just show the open file dialog, and place the result
            // in the filename textbox if valid
            var result = fileImage.ShowDialog();
            if (result == DialogResult.OK)
            {
                txtFile.Text = fileImage.FileName;
            }
        }

        private void btnGo_Click(object sender, EventArgs e)
        {
            // Parse the width/height
            int maxWidth;
            if (!int.TryParse(txtWidth.Text, out maxWidth))
            {
                MessageBox.Show("Please enter a valid number for maximum width");
                return;
            }

            // Attempt to open the file
            Image image = null;
            try
            {
                image = Image.FromFile(txtFile.Text);
            }
            catch (Exception)
            {
                MessageBox.Show("Please specify a valid filename for the input image");
                return;
            }

            // Compute densities if needed
            ComputeLetterDensities();

            Bitmap bitmap = null;

            // Do we need to resize?
            int newWidth = Math.Min(image.Width, maxWidth);
            double aspectRatio = (double)image.Width / (double)image.Height;
            aspectRatio /= _LETTER_ASPECT_RATIO;

            bitmap = new Bitmap(newWidth, (int)((double)newWidth / aspectRatio));

            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.DrawImage(image, 0, 0, bitmap.Width, bitmap.Height);
            }

            var brightnessKeys = _brightnesses.Keys.ToList();
            brightnessKeys.Sort();
            brightnessKeys.Reverse();

            // Produce the string
            // NOTE: This is currently SUPER inefficient - can probably be optimized
            var output = new StringBuilder();
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    double brightness = (double)bitmap.GetPixel(x, y).GetBrightness();
                    bool foundChar = false;
                    foreach (var brightnessKey in brightnessKeys)
                    {
                        // Some finnicky math to convert brightness based
                        // on the "most dense" character's actual brightness
                        var brightnessKeyActual = (brightnessKey - _minBrightness) / (1 - _minBrightness);
                        if (brightness > brightnessKeyActual)
                        {
                            output.Append((char)_brightnesses[brightnessKey]);
                            foundChar = true;
                            break;
                        }
                    }
                    if (!foundChar)
                    {
                        // Just take the darkest one we know about
                        output.Append((char)_brightnesses[brightnessKeys[brightnessKeys.Count - 1]]);
                    }
                }
                output.Append(Environment.NewLine);
            }

            txtImage.Text = output.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        private void ComputeLetterDensities()
        {
            if (_brightnessesComputed)
            {
                return;
            }

            _brightnesses = new Dictionary<double, int>();

            // Build the font we want to use as a reference
            var font = new Font(_FONT_FACE, _FONT_SIZE);
            Bitmap bitmap = new Bitmap(1, 1);
            Graphics graphics = Graphics.FromImage(bitmap);
            if (graphics.MeasureString("iii", font).Width != graphics.MeasureString("WWW", font).Width)
                throw new Exception("Must use a fixed pitch font.");
            
            // Work out how big each letter is - assuming a fixed pitch font
            StringFormat sf = new StringFormat(StringFormat.GenericTypographic);
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            SizeF charSize = graphics.MeasureString("█", font, new PointF(0, 0), sf);

            int letterHeight = (int)charSize.Height + 4;
            int letterWidth = (int)charSize.Width + 2;

            // Compute densities for each letter
            bitmap = new Bitmap(letterWidth, letterHeight, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            graphics = Graphics.FromImage(bitmap);

            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            _minBrightness = 1;

            foreach (int id in _AVAILABLE_CHAR_IDS)
            {
                // Clear the bitmap
                graphics.FillRectangle(new SolidBrush(Color.White), new Rectangle(0, 0, bitmap.Width, bitmap.Height));

                // Draw the letter
                graphics.DrawString(((char)id).ToString(), font, new SolidBrush(Color.Black), 0, 0);

                // Determine average pixel brightness as "density"
                double totalBrightness = 0;
                for (int x = 0; x < bitmap.Width; x++)
                {
                    for (int y = 0; y < bitmap.Height; y++)
                    {
                        totalBrightness += (double)bitmap.GetPixel(x, y).GetBrightness();
                    }
                }
                double brightness = totalBrightness / (double)(bitmap.Width * bitmap.Height);
                if (!_brightnesses.ContainsKey(brightness))
                {
                    _brightnesses.Add(brightness, id);
                    if (brightness < _minBrightness)
                    {
                        _minBrightness = brightness;
                    }
                }
            }
        }
    }
}
