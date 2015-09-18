using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Great_American_Broadcast.Resources;
using System.ServiceModel.Syndication;
using System.Xml;
using System.Xml.Linq;
using System.IO;
using Microsoft.Phone.BackgroundAudio;
using System.Windows.Media;
using System.Windows.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Reflection;
using Microsoft.Phone.Tasks;
using System.Windows.Threading;
using System.Windows.Shapes;

namespace Great_American_Broadcast
{
    public class GAB : IComparable
    {
        public string URL = "";
        public string Title = "";
        public string Description = "";
        public DateTime BroadcastDate;

        public int CompareTo(object obj)
        {
            GAB objCompare = (GAB)obj;
            return this.BroadcastDate.CompareTo(objCompare.BroadcastDate);
        }
    }

    public partial class MainPage : PhoneApplicationPage
    {
        private List<GAB> GAB_List = new List<GAB>();
        private DispatcherTimer PlaybackTimer;

        public MainPage()
        {
            InitializeComponent();
            Loaded += new RoutedEventHandler(MainPage_Loaded);
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                PlaybackTimer = new DispatcherTimer();
                PlaybackTimer.Interval = TimeSpan.FromMilliseconds(1000);
                PlaybackTimer.Tick += PlaybackTimer_Tick;
                LoadFeeds();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }            
        }

        private void ProgressInfo()
        {
            try
            {
                if (BackgroundAudioPlayer.Instance.PlayerState == PlayState.Playing)
                {
                    double dIndicatoreSize = 40;
                    double dWidth = this.ActualWidth;
                    double dCurrent = BackgroundAudioPlayer.Instance.Position.TotalSeconds;
                    double dTotal = BackgroundAudioPlayer.Instance.Track.Duration.TotalSeconds;
                    double dPosition = ((dCurrent / dTotal) * (dWidth - dIndicatoreSize));


                    Ellipse PositionBubble = new Ellipse();
                    PositionBubble.Fill = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    PositionBubble.Stroke = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                    PositionBubble.StrokeThickness = 4;
                    PositionBubble.Width = dIndicatoreSize;
                    PositionBubble.Height = dIndicatoreSize;
                    PositionBubble.Name = "PositionBubbleGAB";

                    Rectangle ProgressBar = new Rectangle();
                    ProgressBar.Fill = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                    ProgressBar.Width = dWidth;
                    ProgressBar.Height = 24;
                    ProgressBar.Name = "ProgressBarGAB";

                    TextBlock ProgressStatus = new TextBlock();
                    ProgressStatus.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 0, 139));
                    ProgressStatus.FontSize = 24;
                    ProgressStatus.FontWeight = FontWeights.Bold;
                    ProgressStatus.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                    ProgressStatus.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                    ProgressStatus.TextAlignment = TextAlignment.Center;
                    ProgressStatus.Width = dWidth;
                    ProgressStatus.Name = "ProgressStatusGAB";
                    ProgressStatus.Text =
                        String.Format(@"{0:hh\:mm\:ss}", BackgroundAudioPlayer.Instance.Position) + "    -    " +
                        String.Format(@"{0:hh\:mm\:ss}", BackgroundAudioPlayer.Instance.Track.Duration);


                    this.ProgressCanvas.Children.Clear();

                    this.ProgressCanvas.Children.Add(ProgressBar);
                    Canvas.SetLeft(ProgressBar, 0);
                    Canvas.SetTop(ProgressBar, 8);

                    this.ProgressCanvas.Children.Add(PositionBubble);
                    Canvas.SetLeft(PositionBubble, dPosition);
                    Canvas.SetTop(PositionBubble, 0);

                    this.ProgressCanvas.Children.Add(ProgressStatus);
                    Canvas.SetLeft(ProgressStatus, 0);
                    Canvas.SetTop(ProgressStatus, 2);

                    ProgressRow.Height = new GridLength(40, GridUnitType.Pixel);
                }
                else
                {
                    ProgressRow.Height = new GridLength(0, GridUnitType.Pixel);
                    this.ProgressCanvas.Children.Clear();
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        public void PlaybackTimer_Tick(object sender, EventArgs e)
        {
            ProgressInfo();
        }

        private void Progress_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                if (BackgroundAudioPlayer.Instance.PlayerState == PlayState.Playing)
                {
                    double dSelected = e.GetPosition(this).X;
                    double dWidth = this.ActualWidth;
                    double dPercent = (dSelected / dWidth);
                    double dDuration = BackgroundAudioPlayer.Instance.Track.Duration.TotalMilliseconds;

                    int iMilliseconds = Convert.ToInt32(dDuration * dPercent);

                    TimeSpan tsPosition = new TimeSpan(0, 0, 0, 0, iMilliseconds);

                    BackgroundAudioPlayer.Instance.Position = tsPosition;
                    
                    ProgressInfo();
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
            
        }

        private void Instance_PlayStateChanged(object sender, EventArgs e)
        {
            try
            {
                ApplicationBarIconButton AppButton = (ApplicationBarIconButton)ApplicationBar.Buttons[0];

                switch (BackgroundAudioPlayer.Instance.PlayerState)
                {
                    case PlayState.Playing:
                        

                        AppButton.IconUri = new Uri("/Assets/AppBar/transport.pause.png", UriKind.RelativeOrAbsolute);
                        AppButton.Text = "pause";
                        PlaybackTimer.Start();
                        break;

                    case PlayState.Paused:
                        PlaybackTimer.Stop();
                        AppButton.IconUri = new Uri("/Assets/AppBar/transport.play.png", UriKind.RelativeOrAbsolute);
                        AppButton.Text = "play";
                        break;

                    case PlayState.Stopped:
                    case PlayState.TrackEnded:
                        MediaStop();
                        break;
                }
                AppButton = null;
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void Refresh(object sender, EventArgs e)
        {
            LoadFeeds();
        }

        private void ShowAbout(object sender, EventArgs e)
        {
            try
            {
                var AssemblyInfo = new AssemblyName(Assembly.GetExecutingAssembly().FullName);
                string Title = AssemblyInfo.Name;

                string Message =
                    "Site: Blissgig.com" + Environment.NewLine +
                    "Contact: Blissgig@gmail.com" + Environment.NewLine +
                    "Content: AlexBennett.com" + Environment.NewLine +
                    "Copyright 2013 - " + DateTime.Now.Year.ToString() + " James Rose" + Environment.NewLine +
                    "Version: " + AssemblyInfo.Version.Major.ToString() + "." +
                        AssemblyInfo.Version.Minor.ToString() + "." +
                        AssemblyInfo.Version.Build.ToString() + "." +
                        AssemblyInfo.Version.MinorRevision.ToString();

                MessageBox.Show(Message, Title, MessageBoxButton.OK);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void Stop(object sender, EventArgs e)
        {
            MediaStop();
        }

        private void MediaPlayPause(object sender, EventArgs e)
        {
            try
            {
                switch (BackgroundAudioPlayer.Instance.PlayerState)
                {
                    case PlayState.Playing:
                        BackgroundAudioPlayer.Instance.Pause();
                        break;

                    case PlayState.Paused:
                        BackgroundAudioPlayer.Instance.Play();
                        break;
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void MediaStop()
        {
            try
            {
                BackgroundAudioPlayer.Instance.Stop();
                ApplicationBarIconButton AppButton = (ApplicationBarIconButton)ApplicationBar.Buttons[0];

                PlaybackTimer.Stop();

                ProgressRow.Height = new GridLength(0, GridUnitType.Pixel);

                AppButton.IsEnabled = false;
                AppButton = (ApplicationBarIconButton)ApplicationBar.Buttons[1];
                AppButton.IsEnabled = false;
                AppButton = null;
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void LoadList()
        {
            try
            {
                GAB GAB_Item = new GAB();

                GABList.Items.Clear(); //Clear the UI list

                //Sort desending (notice the negative before the "x")
                GAB_List.Sort((x, y) => -x.BroadcastDate.CompareTo(y.BroadcastDate));

                //Load the items into the list
                for (int iGAB = 0; iGAB < GAB_List.Count(); iGAB++)
                {
                    GAB_Item = GAB_List[iGAB];
                    this.GABList.Items.Add(GAB_Item.Title + Environment.NewLine + GAB_Item.Description);
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
            finally
            {
                ApplicationBarIconButton RefreshButton = (ApplicationBarIconButton)ApplicationBar.Buttons[2];
                RefreshButton.IsEnabled = true;
                RefreshButton = null;
            }
        }

        private void LoadFeeds()
        {
            try
            {
                //Get the list of xml feeds from Alex's site
                ApplicationBarIconButton AppButton = (ApplicationBarIconButton)ApplicationBar.Buttons[0];
                AppButton.IsEnabled = false;
                AppButton = null;

                MediaStop();

                GAB_List.Clear();
                GABList.Items.Clear(); //UI List

                GAB GAB_Item = new GAB();

                //The live feed
                GAB_Item.Title = "Great American Broadcast Network Live";
                GAB_Item.Description = "Entertainment 24/7";
                GAB_Item.BroadcastDate = DateTime.Now;
                GAB_Item.URL = "http://http.yourmuze.com/gab-1/aacplus-64-s.aac";
                GAB_List.Add(GAB_Item);


                WebClient webClient = new WebClient();
                webClient.DownloadStringCompleted += new DownloadStringCompletedEventHandler(webClient_DownloadListCompleted);
                webClient.DownloadStringAsync(new System.Uri("http://alexbennett.com/podcast/gabfeeds.txt"));
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void webClient_DownloadListCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                if (e.Error == null)
                {
                    string sReturn = e.Result.ToString().Replace("\n","");
                    string[] sLines = sReturn.Split('\r');
                    
                    for (byte bLines = 0; bLines < sLines.Count(); bLines++)
                    {
                        if (sLines[bLines].Trim().Length > 0)
                        {
                            LoadFeed(sLines[bLines]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void LoadFeed(string URI)
        {
            try
            {
                WebClient webClient = new WebClient();
                webClient.DownloadStringCompleted += new DownloadStringCompletedEventHandler(Feed_DownloadCompleted);
                webClient.DownloadStringAsync(new System.Uri(URI));
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void Feed_DownloadCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                if (e.Error == null)
                {
                    GAB GAB_Item;
                    XElement xmlitems = XElement.Parse(e.Result);
                    List<XElement> elements = xmlitems.Descendants("item").ToList();

                    foreach (XElement rssItem in elements)
                    {
                        GAB_Item = new GAB();
                        GAB_Item.Title = rssItem.Element("title").Value;
                        GAB_Item.Description = rssItem.Element("description").Value;
                        GAB_Item.URL = rssItem.Element("guid").Value;
                        GAB_Item.BroadcastDate = Convert.ToDateTime(rssItem.Element("pubDate").Value);
                        
                        GAB_List.Add(GAB_Item);
                    }

                    LoadList();                   
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void feedListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                ListBox listBox = sender as ListBox;

                if (listBox != null && listBox.SelectedItem != null)
                {
                    GAB GAB_Item = GAB_List[Convert.ToInt16(listBox.SelectedIndex)];

                    //continue playing when phone is locked:  http://stackoverflow.com/questions/10108145/windows-phone-7-unable-to-play-audio-under-lock-screen
                    AudioTrack GABAudio = new AudioTrack(new Uri(GAB_Item.URL, UriKind.Absolute), GAB_Item.Title, GAB_Item.Description, "Great American Broadcast Network", null);
                        
                    BackgroundAudioPlayer.Instance.Volume = 1.0;
                    BackgroundAudioPlayer.Instance.Track = GABAudio;  
                    BackgroundAudioPlayer.Instance.PlayStateChanged += Instance_PlayStateChanged;
                    BackgroundAudioPlayer.Instance.Play();
    
                    //Set Play/Pause, Stop buttons as enabled
                    ApplicationBarIconButton AppButton = (ApplicationBarIconButton)ApplicationBar.Buttons[0];
                    AppButton.IsEnabled = true;
                    AppButton.IconUri = new Uri("/Assets/AppBar/transport.pause.png", UriKind.RelativeOrAbsolute);
                    AppButton.Text = "pause";

                    AppButton = (ApplicationBarIconButton)ApplicationBar.Buttons[1];
                    AppButton.IsEnabled = true;
                    AppButton = null;
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }
   
        private void LogException(Exception ex)
        {
            //This should log to the event viewer.  NBD
            Console.WriteLine("Error: " + ex.Message);
        }
    }
}