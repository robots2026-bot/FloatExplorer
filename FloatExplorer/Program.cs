namespace FloatExplorer
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            if (args.Length >= 2 && args[0] == "--screenshot")
            {
                using FormMain preview = new();
                preview.Show();
                Application.DoEvents();
                string formatId = args.Length >= 3 ? args[2] : "fp32";
                string input = args.Length >= 4 ? args[3] : "0.1";
                preview.LoadPreview(formatId, input);
                Application.DoEvents();
                using Bitmap bitmap = new(preview.ClientSize.Width, preview.ClientSize.Height);
                preview.DrawToBitmap(bitmap, preview.ClientRectangle);
                bitmap.Save(args[1], System.Drawing.Imaging.ImageFormat.Png);
                preview.Close();
                return;
            }

            Application.Run(new FormMain());
        }
    }
}
