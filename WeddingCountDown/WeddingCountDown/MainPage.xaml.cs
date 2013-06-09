using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using WeddingCountDown.Resources;
using System.ComponentModel;
using SpyCamera.DataObjects;
using System.Collections.ObjectModel;
using Microsoft.Xna.Framework.Media;
using System.IO;
using System.Windows.Media.Imaging;
using Microsoft.Phone;
using Microsoft.Xna.Framework.Media.PhoneExtensions;
using System.Diagnostics;
using System.IO.IsolatedStorage;
using System.Reflection;
using Windows.Graphics;
using System.Windows.Shapes;
using System.Xml.Linq;
using Windows.UI;
using Microsoft.Phone.Scheduler;
using System.Threading.Tasks;
using LockScreenManager;
using System.Windows.Media;
using System.Runtime.InteropServices;

namespace WeddingCountDown
{
    public partial class MainPage : PhoneApplicationPage, INotifyPropertyChanged, IDisposable
    {
        MediaLibrary _mediaLibrary = new MediaLibrary();
        PictureCollection _pictures;
        private bool _initializing;
        PeriodicTask periodicTask;
        string periodicTaskName = "PeriodicAgent";
        private bool _keepUpdatingDate = true;
        static MainPage _mainPageInstance;

        // Constructor
        public MainPage()
        {
            _initializing = true;
            _mainPageInstance = this;
            FontColors = new ObservableCollection<string>();
            FontColors.Add("Black");
            FontColors.Add("White");

            DataContext = this;

            SetScreenHeightAndWidth();

            InitializeComponent();

            _pictures = _mediaLibrary.Pictures;
            ScreenShots = new ObservableCollection<ScreenShot>();

            LoadScreenShots();

            LoadDateAndPicture();

            Task.Factory.StartNew(StartUpdatingDate);

            StartPeriodicAgent();

            SetUpFontColors();

            if (_firstTimeOpeningApp)
            {
                _settingsScreenSelected = true;
                GoToScreen(Screen.Settings);
            }
            else
            {
                GoToScreen(Screen.Main);
            }

            _initializing = false;
        }

        private void SetScreenHeightAndWidth()
        {
            ScreenWidth = Convert.ToInt32(Application.Current.Host.Content.ActualWidth);
            ScreenHeight = Convert.ToInt32(Application.Current.Host.Content.ActualHeight);
            IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;

            if (!appSettings.Contains("screenwidth"))
            {
                if (SelectedScreenShot != null)
                {
                    appSettings.Add("screenwidth", ScreenWidth);
                }
            }
            else
            {
                if (SelectedScreenShot != null)
                {
                    appSettings["screenwidth"] = ScreenWidth;
                }
            }

            if (!appSettings.Contains("screenheight"))
            {
                if (SelectedScreenShot != null)
                {
                    appSettings.Add("screenheight", ScreenHeight);
                }
            }
            else
            {
                if (SelectedScreenShot != null)
                {
                    appSettings["screenheight"] = ScreenHeight;
                }
            }
        }

        public void StartUpdatingDate()
        {
            while (_keepUpdatingDate)
            {
                Dispatcher.BeginInvoke(() =>
                    {
                        DisplayDate = GetDisplayDateString();
                    });
                System.Threading.Thread.Sleep(System.TimeSpan.FromSeconds(5));
            }
        }

        private void StartPeriodicAgent()
        {
            periodicTask = ScheduledActionService.Find(periodicTaskName) as PeriodicTask;

            if (periodicTask != null)
            {
                try
                {
                    ScheduledActionService.Remove(periodicTaskName);
                }
                catch (Exception ex)
                {

                }
            }

            periodicTask = new PeriodicTask(periodicTaskName);
            periodicTask.Description = "This is a lockscreen image provider app.";
            periodicTask.ExpirationTime = DateTime.Now.AddDays(10);

            try
            {
                ScheduledActionService.Add(periodicTask);


#if DEBUG_AGENT
                ScheduledActionService.LaunchForTest(periodicTaskName, TimeSpan.FromSeconds(10)); 
#endif
                System.Diagnostics.Debug.WriteLine("Periodic task is started: " + periodicTaskName);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.Contains("BNS Error: The action is disabled"))
                {            // load error text from localized strings            
                    MessageBox.Show("Background agents for this application have been disabled by the user.");
                }
                if (ex.Message.Contains("BNS Error: The maximum number of ScheduledActions of this type have already been added."))
                {
                    // No user action required. The system prompts the user when the hard limit of periodic tasks has been reached.
                }
            }
        }

        private async void SetUpLockScreen()
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

        private void LoadScreenShots()
        {
            Debug.WriteLine("Starting to load ScreenShots");
            for (int i = 0; i <= _pictures.Count - 1; i++)
            {
                ScreenShots.Add(new ScreenShot(i, _pictures[i].Name));
            }
            Debug.WriteLine("Ending Loading");
        }

        private void SetDisplay(int num)
        {
            try
            {
                BitmapImage bmImage = new BitmapImage();
                bmImage.SetSource(_pictures[num].GetImage());

                Stream pic = _pictures[num].GetImage();
                Image img = new Image();
                img.Source = PictureDecoder.DecodeJpeg(pic);
            }
            catch (Exception ex)
            {

            }

            //MainImage.Source = img.Source;
        }

        public void GoToScreen(Screen screen)
        {
            switch (screen)
            {
                case Screen.Main:
                    {
                        SettingsScreenVisibility = System.Windows.Visibility.Collapsed;
                        MainScreenVisibility = System.Windows.Visibility.Visible;
                        AboutScreenVisibility = System.Windows.Visibility.Collapsed;
                        break;
                    }
                case Screen.Settings:
                    {
                        SettingsScreenVisibility = System.Windows.Visibility.Visible;
                        MainScreenVisibility = System.Windows.Visibility.Collapsed;
                        AboutScreenVisibility = System.Windows.Visibility.Collapsed;
                        break;
                    }
                case Screen.About:
                    {
                        SettingsScreenVisibility = System.Windows.Visibility.Collapsed;
                        MainScreenVisibility = System.Windows.Visibility.Collapsed;
                        AboutScreenVisibility = System.Windows.Visibility.Visible;
                        break;
                    }
            }
        }

        #region properties

        private bool _firstTimeOpeningApp;

        private ObservableCollection<string> _fontColors;
        public ObservableCollection<string> FontColors
        {
            get { return _fontColors; }
            set { _fontColors = value; RaisePropertyChanged("FontColors"); }
        }

        private string _selectedFontColor;
        public string SelectedFontColor
        {
            get { return _selectedFontColor; }
            set { _selectedFontColor = value; RaisePropertyChanged("SelectedFontColor"); }
        }

        private bool _settingsScreenSelected = false;
        private bool _aboutScreenSelected = false;

        private int _screenWidth;
        public int ScreenWidth
        {
            get { return _screenWidth; }
            set { _screenWidth = value; RaisePropertyChanged("ScreenWidth"); }
        }

        private int _screenHeight;
        public int ScreenHeight
        {
            get { return _screenHeight; }
            set { _screenHeight = value; RaisePropertyChanged("ScreenHeight"); }
        }

        private string _uiDisplayText;
        public string UiDisplayText
        {
            get { return _uiDisplayText; }
            set { _uiDisplayText = value; RaisePropertyChanged("UiDisplayText"); }
        }


        private string _displayText;
        public string DisplayText
        {
            get { return _displayText; }
            set { _displayText = value; RaisePropertyChanged("DisplayText"); }
        }


        private DateTime _selectedDate;
        public DateTime SelectedDate
        {
            get { return _selectedDate; }
            set { _selectedDate = value; RaisePropertyChanged("SelectedDate"); }
        }

        private string _displayDate;
        public string DisplayDate
        {
            get { return _displayDate; }
            set { _displayDate = value; RaisePropertyChanged("DisplayDate"); }
        }

        private Visibility _settingsScreenVisibility;
        public Visibility SettingsScreenVisibility
        {
            get { return _settingsScreenVisibility; }
            set { _settingsScreenVisibility = value; RaisePropertyChanged("SettingsScreenVisibility"); }
        }

        private Visibility _mainScreenVisibility;
        public Visibility MainScreenVisibility
        {
            get { return _mainScreenVisibility; }
            set { _mainScreenVisibility = value; RaisePropertyChanged("MainScreenVisibility"); }
        }

        private Visibility _aboutScreenVisibility;
        public Visibility AboutScreenVisibility
        {
            get { return _aboutScreenVisibility; }
            set { _aboutScreenVisibility = value; RaisePropertyChanged("AboutScreenVisibility"); }
        }

        private ObservableCollection<ScreenShot> _screenShots;
        public ObservableCollection<ScreenShot> ScreenShots
        {
            get { return _screenShots; }
            set { _screenShots = value; RaisePropertyChanged("ScreenShots"); }
        }

        private ScreenShot _selectedScreenShot;
        public ScreenShot SelectedScreenShot
        {
            get { return _selectedScreenShot; }
            set { _selectedScreenShot = value; RaisePropertyChanged("SelectedScreenShot"); }
        }

        #endregion properties

        #region ClickedHandlers

        private void SettingsClickedHandler(object sender, EventArgs e)
        {
            GoToScreen(Screen.Settings);
            _settingsScreenSelected = true;
        }

        private void BackButtonPressed(object sender, CancelEventArgs e)
        {
            if (_settingsScreenSelected || _aboutScreenSelected)
            {
                GoToScreen(Screen.Main);
                SaveDate();
                LoadPhotoIntoIsolatedStoreage();
                //SetUpLockScreen();
                _settingsScreenSelected = false;
                _aboutScreenSelected = false;
                UiDisplayText = GetDisplayText();
                SetUpFontColors();
                e.Cancel = true;
            }
        }

        private int _currentPicNumber = 0;

        private void AdvanceClick(object sender, RoutedEventArgs e)
        {
            SetDisplay(_currentPicNumber);
            _currentPicNumber = _currentPicNumber + 1;
        }

        #endregion ClickedHandlers

        #region propertychanged

        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string prop)
        {
            try
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(prop));
                }
            }
            catch (Exception ex)
            {

            }
        }

        #endregion propertychanged

        #region photo handling

        private ScreenShot _cachedScreenShot;
        private void LeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (SelectedScreenShot != null)
            {
                if (!_initializing)
                {
                    try
                    {
                        if (_cachedScreenShot != null)
                        {
                            if (SelectedScreenShot.Number != _cachedScreenShot.Number)
                            {
                                ScreenShots.RemoveAt(0);

                                _currentPicNumber = _currentPicNumber + 1;

                                Stream pic = _pictures[_currentPicNumber].GetImage();
                                Image img = new Image();
                                img.Source = PictureDecoder.DecodeJpeg(pic);

                                ScreenShots.Insert(ScreenShots.Count() + 1, new ScreenShot(_pictures[_currentPicNumber].Name, new Uri(_pictures[_currentPicNumber].GetPath()), img, _currentPicNumber));
                            }
                        }

                        _cachedScreenShot = new ScreenShot(SelectedScreenShot.Name, SelectedScreenShot.Uri, SelectedScreenShot.ImageToDisplay, SelectedScreenShot.Number);
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
        }

        private void PivotSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                ScreenShot sc = CardPivot.SelectedItem as ScreenShot;

                Image img = new Image();
                img.Source = PictureDecoder.DecodeJpeg(_pictures[sc.Number].GetImage());

                //foreach (var row in ScreenShots)
                //{
                //    if (row.Number != sc.Number)
                //    {
                //        row.ImageToDisplay = null;
                //    }
                //    else
                //    {
                //        row.ImageToDisplay = img;
                //    }
                //}

                img.Source = null;
                img = null;

                Debug.WriteLine(sc.Number);
            }
            catch (Exception ex)
            {

            }
        }

        private string _imageName;
        private void LoadPhotoIntoIsolatedStoreage()
        {
            ScreenShot sc = CardPivot.SelectedItem as ScreenShot;
            LockScreenChanger lockScreenManager = new LockScreenChanger();

            if (sc != null)
            {
                if (sc.Number >= 0)
                {
                    Stream pic = _pictures[sc.Number].GetImage();

                    //Step 1:  Load Main Photo into Isolated Storeage
                    lockScreenManager.LoadPhotoIntoIsolatedStoreage(pic);

                    //Step 2: ConverImageToHaveDate
                    lockScreenManager.ConvertSavedImageToHaveDate();

                    //Step 3: SetUpLockScreenWith New Image
                    lockScreenManager.SetUpLockScreen();
                }
            }
        }

        private string GetDisplayDateString()
        {
            string displayValue = string.Empty;

            var totalDays = SelectedDate - DateTime.Now;
            int days = (int)totalDays.TotalDays;
            int hours = (int)totalDays.Hours;
            int minutes = (int)totalDays.Minutes;

            displayValue = days + " Days " + hours + " Hours " + minutes + " Minutes ";

            return displayValue;
        }

        private string GetDisplayText()
        {
            string displaytext = string.Empty;

            displaytext = "Until " + DisplayText;

            return displaytext;
        }

        #endregion photo handling

        private void SetUpFontColors()
        {
            if (SelectedFontColor.ToUpper().Equals("WHITE"))
            {
                TitleTextBlock.Foreground = new SolidColorBrush(System.Windows.Media.Colors.White);
                DateTextBlock.Foreground = new SolidColorBrush(System.Windows.Media.Colors.White);
                UiTextTextBlock.Foreground = new SolidColorBrush(System.Windows.Media.Colors.White);
            }
            else
            {
                TitleTextBlock.Foreground = new SolidColorBrush(System.Windows.Media.Colors.Black);
                DateTextBlock.Foreground = new SolidColorBrush(System.Windows.Media.Colors.Black);
                UiTextTextBlock.Foreground = new SolidColorBrush(System.Windows.Media.Colors.Black);
            }

        }

        #region isolated storeage

        private void SaveDate()
        {
            IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;
            if (!appSettings.Contains("date"))
            {
                if (SelectedDate != null)
                {
                    appSettings.Add("date", SelectedDate);
                }
            }
            else
            {
                appSettings["date"] = SelectedDate;
            }

            if (!appSettings.Contains("picturename"))
            {
                if (SelectedScreenShot != null)
                {
                    appSettings.Add("picturename", SelectedScreenShot.Name);
                }
            }
            else
            {
                if (SelectedScreenShot != null)
                {
                    appSettings["picturename"] = SelectedScreenShot.Name;
                }
            }

            if (!appSettings.Contains("displaytext"))
            {
                if (SelectedScreenShot != null)
                {
                    appSettings.Add("displaytext", DisplayText);
                }
            }
            else
            {
                if (SelectedScreenShot != null)
                {
                    appSettings["displaytext"] = DisplayText;
                }
            }

            if (!appSettings.Contains("fontcolor"))
            {
                if (SelectedFontColor != null)
                {
                    appSettings.Add("fontcolor", SelectedFontColor);
                }
            }
            else
            {
                if (SelectedFontColor != null)
                {
                    appSettings["fontcolor"] = SelectedFontColor;
                }
            }

            if (!appSettings.Contains("firsttimeopening"))
            {
                appSettings.Add("firsttimeopening", true);
            }

            appSettings.Save();
        }

        private void LoadDateAndPicture()
        {
            IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;
            if (appSettings.Contains("date"))
            {
                SelectedDate = (DateTime)appSettings["date"];
            }
            else
            {
                SelectedDate = DateTime.Today;
            }

            if (appSettings.Contains("picturename"))
            {
                InitializeSelectedScreenShot((string)appSettings["picturename"]);
            }

            if (appSettings.Contains("displaytext"))
            {
                DisplayText = appSettings["displaytext"].ToString();
                UiDisplayText = GetDisplayText();
            }
            else
            {
                DisplayText = "Until You Marry The Love Of Your Life!";
                UiDisplayText = GetDisplayText();
            }

            if (appSettings.Contains("fontcolor"))
            {
                SelectedFontColor = appSettings["fontcolor"].ToString();
            }
            else
            {
                SelectedFontColor = "White";
            }

            if (appSettings.Contains("firsttimeopening"))
            {
                _firstTimeOpeningApp = false;
            }
            else
            {
                _firstTimeOpeningApp = true;
            }
        }

        private void InitializeSelectedScreenShot(string name)
        {
            try
            {
                foreach (var row in ScreenShots)
                {
                    if (row.Name != name)
                    {
                        row.ImageToDisplay = null;
                    }
                    else
                    {
                        Stream pic = _pictures[row.Number].GetImage();
                        Uri picUri = new Uri(_pictures[row.Number].GetPath());
                        Image img = new Image();
                        img.Source = PictureDecoder.DecodeJpeg(pic);

                        int screenWidth = Convert.ToInt32(Application.Current.Host.Content.ActualWidth);
                        int screenHeight = Convert.ToInt32(Application.Current.Host.Content.ActualHeight);

                        row.ImageToDisplay = img;
                        row.Uri = picUri;

                        SelectedScreenShot = row;
                    }
                }

                Debug.WriteLine(name);
            }
            catch (Exception ex)
            {

            }
        }

        #endregion isolated storeage

        public void Dispose()
        {
            _keepUpdatingDate = false;
            SaveDate();
        }

        private void AboutClicked(object sender, EventArgs e)
        {
            _aboutScreenSelected = true;
            GoToScreen(Screen.About);
        }
    }
}