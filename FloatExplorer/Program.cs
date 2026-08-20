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
            if (args is ["--screenshot", string outputPath])
            {
                using FormMain preview = new();
                preview.Show();
                Application.DoEvents();
                using Bitmap bitmap = new(preview.ClientSize.Width, preview.ClientSize.Height);
                preview.DrawToBitmap(bitmap, preview.ClientRectangle);
                bitmap.Save(outputPath, System.Drawing.Imaging.ImageFormat.Png);
                preview.Close();
                return;
            }

            Application.Run(new FormMain());
        }
    }
}
