using Microsoft.Phone.Controls;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace LockScreenManager
{
    public class LockScreenChanger
    {
        public LockScreenChanger()
        {
            LoadScreenDimensions();
            LoadDisplayText();
            LoadDisplayDateString();
            LoadFontColor();
        }

        private async void SetUpLockScreen(string imageName)
        {
            string filePath = string.Empty;
            try
            {
                var isProvider = Windows.Phone.System.UserProfile.LockScreenManager.IsProvidedByCurrentApplication;
                if (!isProvider)
                {
                    // If you're not the provider, this call will prompt the user for permission.
                    // Calling RequestAccessAsync from a background agent is not allowed.
                    var op = await Windows.Phone.System.UserProfile.LockScreenManager.RequestAccessAsync();

                    // Only do further work if the access was granted.
                    isProvider = op == Windows.Phone.System.UserProfile.LockScreenRequestResult.Granted;
                }

                if (isProvider)
                {
                    // At this stage, the app is the active lock screen background provider.

                    // The following code example shows the new URI schema.
                    // ms-appdata points to the root of the local app data folder.
                    // ms-appx points to the Local app install folder, to reference resources bundled in the XAP package.
                    var schema = "ms-appdata:///Local/";

                    // Get the physical path using reflection
                    var uri = new Uri(schema + imageName);


                    try
                    {
                        // Set the lock screen background image.
                        Windows.Phone.System.UserProfile.LockScreen.SetImageUri(uri);
                    }
                    catch (Exception ex)
                    {

                    }

                    // Get the URI of the lock screen background image.
                    var currentImage = Windows.Phone.System.UserProfile.LockScreen.GetImageUri();
                    System.Diagnostics.Debug.WriteLine("The new lock screen background image is set to {0}", currentImage.ToString());
                }
                else
                {
                    MessageBox.Show("You said no, so I can't update your background.");
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }

        //Step 1:  Save the image to WeddingImage
        //Step 2:  Convert the image to have the date
        //Step 3:  Save the image as a new image name and set lock screen to image

        public void LoadPhotoIntoIsolatedStoreage(Stream pic)
        {
            var fileDelete = IsolatedStorageFile.GetUserStoreForApplication();
            if (fileDelete.FileExists("weddingimage.jpg"))
            {
                try
                {
                    fileDelete.DeleteFile("weddingimage.jpg");
                }
                catch (IsolatedStorageException ex)
                {

                }
            }

            try
            {
                using (var isoStore = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (var fileStream = isoStore.CreateFile("weddingimage.jpg"))
                    {
                        BitmapImage image = new BitmapImage();
                        image.SetSource(pic);

                        Image imagePic = new Image();
                        imagePic.Source = image;

                        int screenWidth = Convert.ToInt32(Application.Current.Host.Content.ActualWidth);
                        int screenHeight = Convert.ToInt32(Application.Current.Host.Content.ActualHeight);

                        Canvas canvas = new Canvas();
                        canvas.Children.Add(imagePic);

                        canvas.Height = screenHeight;
                        canvas.Width = screenWidth;

                        WriteableBitmap wb = new WriteableBitmap(screenWidth, screenHeight);
                        wb.Render(canvas, null);
                        wb.Invalidate();

                        System.Windows.Media.Imaging.Extensions.SaveJpeg(wb, fileStream, wb.PixelWidth, wb.PixelHeight, 0, 85);
                        fileStream.Close();
                    }
                    isoStore.Dispose();
                }
            }
            catch (Exception ex)
            {

            }

        }

        string _imageName;
        int _screenWidth;
        int _screenHeight;
        string _displayDate;
        string _displayText;
        System.Windows.Media.Color _fontColor;

        private void LoadScreenDimensions()
        {
            IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;

            if (appSettings.Contains("screenwidth"))
            {
                _screenWidth = (int)appSettings["screenwidth"];
            }
            else
            {
                _screenWidth = 480;
            }

            if (appSettings.Contains("screenheight"))
            {
                _screenHeight = (int)appSettings["screenheight"];
            }
            else
            {
                _screenHeight = 720;
            }
        }

        private void LoadDisplayDateString()
        {
            IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;
            DateTime selectedDate;
            if (appSettings.Contains("date"))
            {
                selectedDate = (DateTime)appSettings["date"];
            }
            else
            {
                selectedDate = DateTime.Today;
            }
            string displayValue = string.Empty;

            var totalDays = selectedDate - DateTime.Now;
            int days = (int)totalDays.TotalDays;
            int hours = (int)totalDays.Hours;
            int minutes = (int)totalDays.Minutes;

            _displayDate = days + " Days " + hours + " Hours " + minutes + " Minutes ";
        }

        public void LoadDisplayText()
        {
            IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;

            if (appSettings.Contains("displaytext"))
            {
                _displayText = "Until " + appSettings["displaytext"].ToString();
            }
            else
            {
                _displayText = "Until You Marry The Love Of Your Life!";
            }
        }

        public void LoadFontColor()
        {
            IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;

            if (appSettings.Contains("fontcolor"))
            {
                string color = appSettings["fontcolor"].ToString().ToUpper();

                if (color.Equals("WHITE"))
                {
                    _fontColor = System.Windows.Media.Colors.White;
                }
                else
                {
                    _fontColor = System.Windows.Media.Colors.Black;
                }
            }
            else
            {
                _fontColor = System.Windows.Media.Colors.White;
            }
        }

        public void ConvertSavedImageToHaveDate()
        {
            var storedFile = IsolatedStorageFile.GetUserStoreForApplication();
            if (storedFile.FileExists("weddingimage.jpg"))
            {
                using (var isoStore = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    Stream pic = isoStore.OpenFile("weddingimage.jpg", FileMode.Open);

                    if (isoStore.FileExists("wedding1.jpg"))
                    {
                        _imageName = "wedding.jpg";
                        isoStore.DeleteFile("wedding1.jpg");
                    }
                    else if (isoStore.FileExists("wedding.jpg"))
                    {
                        _imageName = "wedding1.jpg";
                        isoStore.DeleteFile("wedding.jpg");
                    }
                    else
                    {
                        _imageName = "wedding.jpg";
                    }

                    using (var fileStream = isoStore.CreateFile(_imageName))
                    {
                        BitmapImage image = new BitmapImage();
                        image.SetSource(pic);

                        Image iCanvas = new Image();
                        iCanvas.Source = image;
                        iCanvas.Height = _screenHeight;
                        iCanvas.Width = _screenWidth;

                        var canvas = new Canvas();

                        WrapPanel wp = new WrapPanel();
                        wp.Orientation = System.Windows.Controls.Orientation.Vertical;

                        var textBlock = new TextBlock
                        {
                            Text = _displayText,
                            FontSize = 25,
                            Foreground = new System.Windows.Media.SolidColorBrush(_fontColor),
                            Margin = new Thickness(0, 25, 0, 0)
                        };

                        textBlock.Width = _screenWidth - 25;
                        textBlock.TextWrapping = TextWrapping.Wrap;

                        var dateTextBlock = new TextBlock
                        {
                            Text = _displayDate,
                            FontSize = 25,
                            Foreground = new System.Windows.Media.SolidColorBrush(_fontColor)
                        };

                        wp.Children.Add(dateTextBlock);
                        wp.Children.Add(textBlock);

                        canvas.Children.Add(wp);
                        canvas.Children.Add(iCanvas);

                        Canvas.SetLeft(wp, 25);
                        Canvas.SetTop(wp, 50);
                        Canvas.SetZIndex(wp, 1);

                        Canvas.SetLeft(iCanvas, 0);
                        Canvas.SetTop(iCanvas, 0);
                        Canvas.SetZIndex(iCanvas, 0);

                        wp.Width = _screenWidth - 25;
                        canvas.Height = _screenHeight;
                        canvas.Width = _screenWidth;

                        WriteableBitmap wb = new WriteableBitmap(_screenWidth, _screenHeight);
                        wb.Render(canvas, null);
                        wb.Invalidate();

                        System.Windows.Media.Imaging.Extensions.SaveJpeg(wb, fileStream, wb.PixelWidth, wb.PixelHeight, 0, 85);
                        pic.Close();
                    }
                }
            }
        }

        public async void SetUpLockScreen()
        {
            string filePath = string.Empty;
            if (_imageName != null)
            {
                if (_imageName != string.Empty)
                {
                    try
                    {
                        var isProvider = Windows.Phone.System.UserProfile.LockScreenManager.IsProvidedByCurrentApplication;
                        if (!isProvider)
                        {
                            // If you're not the provider, this call will prompt the user for permission.
                            // Calling RequestAccessAsync from a background agent is not allowed.
                            var op = await Windows.Phone.System.UserProfile.LockScreenManager.RequestAccessAsync();

                            // Only do further work if the access was granted.
                            isProvider = op == Windows.Phone.System.UserProfile.LockScreenRequestResult.Granted;
                        }

                        if (isProvider)
                        {
                            // At this stage, the app is the active lock screen background provider.

                            // The following code example shows the new URI schema.
                            // ms-appdata points to the root of the local app data folder.
                            // ms-appx points to the Local app install folder, to reference resources bundled in the XAP package.
                            var schema = "ms-appdata:///Local/";

                            // Get the physical path using reflection

                            var uri = new Uri(schema + _imageName);


                            try
                            {
                                // Set the lock screen background image.
                                Windows.Phone.System.UserProfile.LockScreen.SetImageUri(uri);
                            }
                            catch (Exception ex)
                            {

                            }

                            // Get the URI of the lock screen background image.
                            var currentImage = Windows.Phone.System.UserProfile.LockScreen.GetImageUri();
                            System.Diagnostics.Debug.WriteLine("The new lock screen background image is set to {0}", currentImage.ToString());
                        }
                        else
                        {
                            MessageBox.Show("You said no, so I can't update your background.");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.ToString());
                    }
                }
            }

        }
    }
}
