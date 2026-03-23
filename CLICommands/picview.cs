using Commands;
using Core;
using System;
using System.Windows.Forms;
using System.Drawing;

namespace CLINETCommands
{
    public class PicViewer : ITerminalCommand
    {
        public string Name => "picview";
        private string _currentLocation;
        private string _helpMessage = @"Usage of picview command:
    picview <file> :  Displays in console the <file> data.
";

        public void Execute(string arg)
        {
            GlobalVariables.isErrorCommand = false;
            _currentLocation = File.ReadAllText(GlobalVariables.currentDirectory);
            if (arg == Name)
            {
                FileSystem.SuccessWriteLine($"Use -h param for {Name} command usage.");
                return;
            }
            // Display help message.
            if (arg == "picview -h")
            {
                Console.WriteLine(_helpMessage);
                return;
            }
            else //show the picture
            {
                int argLength = arg.Length - 8;

                string input = arg.Substring(8, argLength);

                Image theImage = Image.FromFile(_currentLocation + "\\" + input);

                if (!isImageCorrupted(theImage))
                {
                    PictureBox thePic = new PictureBox();
                    thePic.Image = theImage;
                    thePic.Size = theImage.Size;
                    Terminal.AddControl(thePic);
                }
                else
                {
                    Console.WriteLine("Unable to open image.");
                }
            }
        }

        /// <summary>
        /// Checks if an image is valid
        /// </summary>
        /// <param name="img">The Image Object</param>
        /// <returns>true is valid, false is corrupted</returns>
        public static bool isImageCorrupted(Image img)
        {
            bool itis = false;

            try
            {
                if (!ImageAnimator.CanAnimate(img))
                {
                    return itis;
                }
                int frames = img.GetFrameCount(System.Drawing.Imaging.FrameDimension.Time);
                if (frames <= 1)
                {
                    return itis;
                }
                byte[] times = img.GetPropertyItem(0x5100).Value;
                int frame = 0;
                for (; ; )
                {
                    int dur = BitConverter.ToInt32(times, 4 * frame);
                    if (++frame >= frames) break;
                    img.SelectActiveFrame(System.Drawing.Imaging.FrameDimension.Time, frame);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("[ERR] isImageCorrupted Method " + ex.Message);
            }

            return itis;
        }
    }
}
