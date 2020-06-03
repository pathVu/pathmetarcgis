
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Navigation;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Portal;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI.Controls;
using Color = System.Drawing.Color;

namespace PathMet_V2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ISensors sensors;

        public MainWindow()
        {
            InitializeComponent();

            InitializeUI();

            //InitializeAuthentication();

            InitializeMap();



            sensors = new SerialSensors(Properties.Settings.Default.SensorsPort);
            sensors.UpdateEvent += OnUpdate;
            UpdateSensors();

            sensors.ExistsEvent += () =>
            {
                this.Dispatcher.Invoke((MethodInvoker)(() => { FileExists(); }));
            };

            sensors.SummaryEvent += (double laser, double encoder) =>
            {
                this.Dispatcher.Invoke((MethodInvoker)(() => { Summary(laser, encoder); }));
            };

        }

        void Window_Closing(object sender, CancelEventArgs e)
        {
            sensors.UpdateEvent -= OnUpdate;
            sensors.Dispose();
        }


        delegate void UpdateSensorsDel();

        private void OnUpdate()
        {
            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new UpdateSensorsDel(UpdateSensors));
        }

        private void FileExists()
        {
            System.Windows.MessageBox.Show("That run exists. Please choose a different name.", "File Exists");
        }

        private void Summary(double laser, double encoder)
        {
            System.Windows.MessageBox.Show(String.Format("Laser: {0:0.000} in\nEncoder: {1:0.0} ft", laser, encoder / 12.0), "Summary");
        }

        #region pathMet_functions
        private void UpdateSensors()
        {
            if (sensors.Connected)
            {
                UpdateUI_PathMetConnected();

                if (sensors.Sampling)
                {
                    UpdateUI_RunInProgress();
                }
                else
                {
                    if (startPtChosen)
                    {
                        UpdateUI_ReadyToRun();
                    }
                    else
                    {
                        WaitForStartPt();
                    }
                }

                if (sensors.LaserStatus == SensorStatus.OK)
                {
                    chkbxL.Background = System.Windows.Media.Brushes.LightGreen;
                    chkbxL.IsChecked = true;
                }
                else
                {
                    chkbxL.Background = System.Windows.Media.Brushes.Red;
                    chkbxL.IsChecked = false;
                }

                if (sensors.CameraStatus == SensorStatus.OK)
                {
                    chkbxC.Background = System.Windows.Media.Brushes.LightGreen;
                    chkbxC.IsChecked = true;
                }
                else
                {
                    chkbxC.Background = System.Windows.Media.Brushes.Red;
                    chkbxC.IsChecked = false;
                }

                if (sensors.IMUStatus == SensorStatus.OK)
                {
                    chkbxI.Background = System.Windows.Media.Brushes.LightGreen;
                    chkbxI.IsChecked = true;
                }
                else
                {
                    chkbxI.Background = System.Windows.Media.Brushes.Red;
                    chkbxI.IsChecked = false;
                }

                if (sensors.EncoderStatus == SensorStatus.OK)
                {
                    chkbxE.Background = System.Windows.Media.Brushes.LightGreen;
                    chkbxE.IsChecked = true;
                }
                else
                {
                    chkbxE.Background = System.Windows.Media.Brushes.Red;
                    chkbxE.IsChecked = false;
                }

                btnRestart.IsEnabled = true;
            }
            else //sensors are not connected
            {
                UpdateUI_SensorsNotConnected();
            }
        }

        private void OnStop(object sender, EventArgs e)
        {
            if (!sensors.Connected)
            {
                return;
            }
            // disable everything; the sensor will enable it when ready
            txtFName.IsEnabled = false;
            btnStop.IsEnabled = false;
            pmStart.IsEnabled = false;
            WaitForEndPt();

            sensors.Stop();

            //TODO make this only happen after run has been submitted
            // if txtFName ends with a number, increment it
            string name = txtFName.Text;

            var match = Regex.Match(name, "\\d+$");
            if (match.Success)
            {
                int n = int.Parse(match.Value);
                name = name.Substring(0, name.Length - match.Value.Length) + String.Format("{0}", n + 1);
            }
            else if (name != "")
            {
                name = name + "2";
            }

            txtFName.Text = name;
        }

        private void OnStart(object sender, EventArgs e)
        {
            //for testing onStop functionality
            /*
            if (!sensors.Connected)
            {
                return;
            }
            // disable everything; the sensor will enable it when ready
            txtFName.IsEnabled = false;
            btnStop.IsEnabled = false;
            pmStart.IsEnabled = false;
            StopRegisteringMapTaps();

            string name = txtFName.Text;
            if (name == "")
            {
                name = DateTime.Now.ToString("yyyy-MM-dd-HHmmss");
            }

            sensors.Start(name);
            */
            StopRegisteringMapTaps();
            WaitForEndPt();
        }

        private void btnRestart_Click(object sender, EventArgs e)
        {
            if (!sensors.Connected)
            {
                return;
            }

            sensors.Restart();
        }

        private void btnTrippingHazard_Click(object sender, EventArgs e)
        {
            if (!sensors.Connected)
            {
                return;
            }

            sensors.Flag("Tripping Hazard");
        }

        private void btnBrokenSidewalk_Click(object sender, EventArgs e)
        {
            if (!sensors.Connected)
            {
                return;
            }

            sensors.Flag("Broken Sidewalk");
        }

        private void btnVegetation_Click(object sender, EventArgs e)
        {
            if (!sensors.Connected)
            {
                return;
            }

            sensors.Flag("Vegetation");
        }

        private void btnOther_Click(object sender, EventArgs e)
        {
            if (!sensors.Connected)
            {
                return;
            }

            sensors.Flag("Other");
        }

        #endregion

        #region map_functions

        private bool startPtChosen = false;
        private bool endPtChosen = false;
        private MapPoint startPoint = null;
        private MapPoint endPoint = null;
        private double lastRunDist;
        PolylineBuilder polyLineBuilder;
        GraphicsOverlay runsPointOverlay;
        GraphicsOverlay runsLineOverlay;
        GraphicsOverlay startPointOverlay;
        GraphicsOverlay endPointOverlay;
        double RunDistTolerance = 10.0;


        private void InitializeMap()
        {
            //basemap
            //Map newMap = new Map(Basemap.CreateNavigationVector());

            //add pathvu specific map to mapView
            
            var mapId = "8ff831ddf02344cda858a37c742804dc";
            var webMapUrl = string.Format("https://www.arcgisonline.com/sharing/rest/content/items/{0}/data", mapId);
            Map currentMap = new Map(new Uri(webMapUrl));

            //drawing initialization

            // Add a graphics overlay for showing the run line. 
            runsLineOverlay = new GraphicsOverlay();
            SimpleLineSymbol runLineSymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Aqua, 5);
            runsLineOverlay.Renderer = new SimpleRenderer(runLineSymbol);
            MyMapView.GraphicsOverlays.Add(runsLineOverlay);

            // Add a graphics overlay for showing the run points.
            runsPointOverlay = new GraphicsOverlay();
            SimpleMarkerSymbol runMarkerSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Cross, Color.Blue, 10);
            runsPointOverlay.Renderer = new SimpleRenderer(runMarkerSymbol);
            MyMapView.GraphicsOverlays.Add(runsPointOverlay);

            // Add a graphics overlay for showing the startPt.
            startPointOverlay = new GraphicsOverlay();
            SimpleMarkerSymbol startMarkerSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, Color.LightGreen, 10);
            startPointOverlay.Renderer = new SimpleRenderer(startMarkerSymbol);
            MyMapView.GraphicsOverlays.Add(startPointOverlay);

            // Add a graphics overlay for showing the endPt.
            endPointOverlay = new GraphicsOverlay();
            SimpleMarkerSymbol endMarkerSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, Color.DarkRed, 10);
            endPointOverlay.Renderer = new SimpleRenderer(endMarkerSymbol);
            MyMapView.GraphicsOverlays.Add(endPointOverlay);



            //create polyline builder
            //polyLineBuilder = new PolylineBuilder(SpatialReferences.Wgs84);

            //map assignment
            MyMapView.Map = currentMap;

            //extent settings for navigation mode
            MyMapView.LocationDisplay.IsEnabled = true;

            MyMapView.LocationDisplay.AutoPanMode = LocationDisplayAutoPanMode.CompassNavigation;
        }

        //procedure sets up the UI for choosing a starting point
        private void WaitForStartPt()
        {
            MyMapView.GeoViewTapped += StartPt_Tapped;
            startPtChosen = false;
            endPtChosen = false;
            UpdateUI_waitForStartPt();
        }

        private void WaitForEndPt()
        {
            MyMapView.GeoViewTapped += EndPt_Tapped;

            //show length of past run
            PastRunDistContainer.Visibility = Visibility.Visible;
            pathDrawingControls.Visibility = Visibility.Visible;
            lastRunDist = GetLastRunDist();
            pastRunDistTxt.Text = lastRunDist.ToString() + "m";

            UpdateUI_waitForEndPt();

            //get and show the past run's distance to compare our drawing with
        }

        private double GetLastRunDist()
        {
            return 20.0;
        }

        private void StartPt_Tapped(object sender, GeoViewInputEventArgs e)
        {
            // Get the tapped point - this is in the map's spatial reference,
            // which in this case is WebMercator because that is the SR used by the included basemaps.
            MapPoint tappedPoint = e.Location;

            // Project the point to WGS84
            MapPoint projectedPoint = (MapPoint)GeometryEngine.Project(tappedPoint, SpatialReferences.Wgs84);

            //TODO add logic to snap to path geometry

            //add tapped point through polyline builder and update graphics
            runsLineOverlay.Graphics.Clear();
            startPointOverlay.Graphics.Clear();

            polyLineBuilder = new PolylineBuilder(SpatialReferences.Wgs84);
            polyLineBuilder.AddPoint(projectedPoint);

            startPointOverlay.Graphics.Add(new Graphic(projectedPoint));
            runsLineOverlay.Graphics.Add(new Graphic(polyLineBuilder.ToGeometry()));

            startPtChosen = true;
            startPoint = projectedPoint;
            Console.WriteLine("StartPt chosen, start should be available.");
            UpdateSensors();
        }

        private void RestartDrawingRun(object sender, EventArgs e)
        {
            //clear the current polyline and points 
            polyLineBuilder = new PolylineBuilder(SpatialReferences.Wgs84);
            runsLineOverlay.Graphics.Clear();
            startPointOverlay.Graphics.Clear();
            runsPointOverlay.Graphics.Clear();
            endPointOverlay.Graphics.Clear();

            //add the starting point back to the line
            polyLineBuilder.AddPoint(startPoint);
            startPointOverlay.Graphics.Add(new Graphic(startPoint));
            runsLineOverlay.Graphics.Add(new Graphic(polyLineBuilder.ToGeometry()));

            //clear the end point bools
            endPoint = null;
            endPtChosen = false;

            MyMapView.DismissCallout();
            UpdateUI_waitForEndPt();
            //delete this comment

        }

        private void EndPt_Tapped(object sender, GeoViewInputEventArgs e)
        {
            // Get the tapped point - this is in the map's spatial reference,
            // which in this case is WebMercator because that is the SR used by the included basemaps.
            MapPoint tappedPoint = e.Location;

            // Project the point to WGS84
            MapPoint projectedPoint = (MapPoint)GeometryEngine.Project(tappedPoint, SpatialReferences.Wgs84);

            //clear old endpoint and line
            endPointOverlay.Graphics.Clear();
            runsLineOverlay.Graphics.Clear();

            polyLineBuilder.AddPoint(projectedPoint);

            //add graphics
            runsPointOverlay.Graphics.Add(new Graphic(projectedPoint));
            endPointOverlay.Graphics.Add(new Graphic(projectedPoint));
            runsLineOverlay.Graphics.Add(new Graphic(polyLineBuilder.ToGeometry()));


            //get the length of the line we've drawn so far
            double lineLength = Math.Round(GeometryEngine.LengthGeodetic(polyLineBuilder.ToGeometry(), LinearUnits.Meters, GeodeticCurveType.Geodesic), 1);

            String lengthStatus;

            System.Windows.Media.Color bkgdColor;

            if (Math.Abs(lineLength - GetLastRunDist()) <= RunDistTolerance)
            {
                endPtChosen = true;
                lengthStatus = "Drawn length matches last run data.";
                bkgdColor = System.Windows.Media.Colors.White;
                UpdateUI_PostRun();
                endPoint = projectedPoint;
            }
            else
            {
                endPtChosen = false;
                lengthStatus = "Drawn length does not match last run data.";
                bkgdColor = System.Windows.Media.Colors.Red;
                UpdateUI_waitForEndPt();
                endPoint = null;
            }


            MyMapView.ShowCalloutAt(projectedPoint, new Callout
            {
                Background = new System.Windows.Media.SolidColorBrush(bkgdColor),
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.PaleVioletRed),
                BorderThickness = new System.Windows.Thickness(5, 5, 5, 3),
                Content = lineLength.ToString() + "m \n" + lengthStatus
            });

        }

        private void StopRegisteringMapTaps()
        {
            MyMapView.GeoViewTapped -= StartPt_Tapped;
            MyMapView.GeoViewTapped -= EndPt_Tapped;
        }

        #endregion

        #region UI_functions
        private void InitializeUI()
        {
            StatusTxt.Background = System.Windows.Media.Brushes.Red;
            chkbxPm.Background = System.Windows.Media.Brushes.Red;
            chkbxPm.IsChecked = false;

            pmStart.IsEnabled = false;
            btnStop.IsEnabled = false;
            btnRestart.IsEnabled = false;
            txtFName.IsEnabled = false;
            btnVegetation.IsEnabled = false;
            btnTrippingHazard.IsEnabled = false;
            btnBrokenSidewalk.IsEnabled = false;
            btnOther.IsEnabled = false;
            PastRunDistContainer.Visibility = Visibility.Hidden;
            pathDrawingControls.Visibility = Visibility.Hidden;
            PostRunControlsPanel.Visibility = Visibility.Hidden;
            RunControlsPanel.Visibility = Visibility.Visible;

            chkbxL.Background = System.Windows.Media.Brushes.Transparent;
            chkbxL.IsChecked = false;
            chkbxC.Background = System.Windows.Media.Brushes.Transparent;
            chkbxC.IsChecked = false;
            chkbxI.Background = System.Windows.Media.Brushes.Transparent;
            chkbxI.IsChecked = false;
            chkbxE.Background = System.Windows.Media.Brushes.Transparent;
            chkbxE.IsChecked = false;
        }

        private void UpdateUI_PathMetConnected()
        {
            chkbxPm.Background = System.Windows.Media.Brushes.LightGreen;
            chkbxPm.IsChecked = true;
        }



        private void UpdateUI_waitForStartPt()
        {
            pmStart.IsEnabled = false;
            btnStop.IsEnabled = false;
            btnRestart.IsEnabled = false;
            txtFName.IsEnabled = false;
            btnVegetation.IsEnabled = false;
            btnTrippingHazard.IsEnabled = false;
            btnBrokenSidewalk.IsEnabled = false;
            btnOther.IsEnabled = false;
            StatusTxt.Text = "Choose starting point";
            StatusTxt.Background = System.Windows.Media.Brushes.Transparent;
        }

        private void UpdateUI_waitForEndPt()
        {
            pmStart.IsEnabled = false;
            btnStop.IsEnabled = false;
            btnRestart.IsEnabled = false;
            txtFName.IsEnabled = false;
            btnVegetation.IsEnabled = false;
            btnTrippingHazard.IsEnabled = false;
            btnBrokenSidewalk.IsEnabled = false;
            btnOther.IsEnabled = false;
            StatusTxt.Text = "Choosing points for run path...";
            RunControlsPanel.Visibility = Visibility.Hidden;
            PostRunControlsPanel.Visibility = Visibility.Visible;
            submitBtn.IsEnabled = false;
            StatusTxt.Background = System.Windows.Media.Brushes.Transparent;

        }

        private void UpdateUI_PostRun()
        {
            pmStart.IsEnabled = false;
            btnStop.IsEnabled = false;
            btnRestart.IsEnabled = false;
            txtFName.IsEnabled = false;
            btnVegetation.IsEnabled = false;
            btnTrippingHazard.IsEnabled = false;
            btnBrokenSidewalk.IsEnabled = false;
            btnOther.IsEnabled = false;
            RunControlsPanel.Visibility = Visibility.Hidden;
            PostRunControlsPanel.Visibility = Visibility.Visible;
            submitBtn.IsEnabled = true;
            StatusTxt.Text = "Post-run: review or submit the completed run.";
            StatusTxt.Background = System.Windows.Media.Brushes.Transparent;
        }

        public void UpdateUI_RunInProgress()
        {
            pmStart.IsEnabled = false;
            btnStop.IsEnabled = true;
            txtFName.IsEnabled = false;
            btnVegetation.IsEnabled = true;
            btnTrippingHazard.IsEnabled = true;
            btnBrokenSidewalk.IsEnabled = true;
            btnOther.IsEnabled = true;
            StatusTxt.Text = "Run in progress...";
            StatusTxt.Background = System.Windows.Media.Brushes.Transparent;
        }

        public void UpdateUI_ReadyToRun()
        {
            pmStart.IsEnabled = true;
            btnStop.IsEnabled = false;
            txtFName.IsEnabled = true;
            StatusTxt.Text = "Hit \"Start\" to begin run.";
            StatusTxt.Background = System.Windows.Media.Brushes.Transparent;
        }

        public void UpdateUI_SensorsNotConnected()
        {
            StatusTxt.Text = "Sensors are Not Connected";
            StatusTxt.Background = System.Windows.Media.Brushes.Red;
            chkbxPm.Background = System.Windows.Media.Brushes.Red;
            chkbxPm.IsChecked = false;

            pmStart.IsEnabled = false;
            btnStop.IsEnabled = false;
            txtFName.IsEnabled = false;
            btnVegetation.IsEnabled = false;
            btnTrippingHazard.IsEnabled = false;
            btnBrokenSidewalk.IsEnabled = false;
            btnOther.IsEnabled = false;


            chkbxL.Background = System.Windows.Media.Brushes.Transparent;
            chkbxL.IsChecked = false;
            chkbxC.Background = System.Windows.Media.Brushes.Transparent;
            chkbxC.IsChecked = false;
            chkbxI.Background = System.Windows.Media.Brushes.Transparent;
            chkbxI.IsChecked = false;
            chkbxE.Background = System.Windows.Media.Brushes.Transparent;
            chkbxE.IsChecked = false;

            btnRestart.IsEnabled = false;
        }

        #endregion

        #region User_Functions

        // Constants for OAuth-related values.
        // - The URL of the portal to authenticate with
        private const string ServerUrl = "https://chuckr.maps.arcgis.com/home/";
        // - The Client ID for an app registered with the server (the ID below is for a public app created by the ArcGIS Runtime team).
        private const string AppClientId = @"1EkwvPiVbGnxcwq1";
        // - An optional client secret for the app (only needed for the OAuthAuthorizationCode authorization type).
        private const string ClientSecret = "6e69276bfa7645e7a5718f0c2d1da805";
        // - A URL for redirecting after a successful authorization (this must be a URL configured with the app).
        private const string OAuthRedirectUrl = @"my-ags-app://auth";
        // - The ID for a web map item hosted on the server (the ID below is for a traffic map of Paris).
        private const string WebMapId = "10ebabd2fa134ebfb1e7664b4a744160";


        private async void InitializeAuthentication()
        {
            try
            {
                // Set up the AuthenticationManager to use OAuth for secure ArcGIS Online requests.
                SetOAuthInfo();

                // Connect to the portal (ArcGIS Online, for example).
                ArcGISPortal arcgisPortal = await ArcGISPortal.CreateAsync(new Uri(ServerUrl));


                // Get a web map portal item using its ID.
                // If the item contains layers not shared publicly, the user will be challenged for credentials at this point.
                PortalItem portalItem = await PortalItem.CreateAsync(arcgisPortal, WebMapId);

                // Create a new map with the portal item and display it in the map view.
                // If authentication fails, only the public layers are displayed.
                Map myMap = new Map(portalItem);
                MyMapView.Map = myMap;
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.ToString(), "Error starting sample");
            }
        }

        private void SetOAuthInfo()
        {
            // Register the server information with the AuthenticationManager, including the OAuth settings.
            ServerInfo serverInfo = new ServerInfo
            {
                ServerUri = new Uri(ServerUrl),
                TokenAuthenticationType = TokenAuthenticationType.OAuthImplicit,
                OAuthClientInfo = new OAuthClientInfo
                {
                    ClientId = AppClientId,
                    RedirectUri = new Uri(OAuthRedirectUrl)
                }
            };

            // If a client secret has been configured, set the authentication type to OAuthAuthorizationCode.
            if (!String.IsNullOrEmpty(ClientSecret))
            {
                // Use OAuthAuthorizationCode if you need a refresh token (and have specified a valid client secret).
                serverInfo.TokenAuthenticationType = TokenAuthenticationType.OAuthAuthorizationCode;
                serverInfo.OAuthClientInfo.ClientSecret = ClientSecret;
            }

            // Register this server with AuthenticationManager.
            AuthenticationManager.Current.RegisterServer(serverInfo);

            // Use the custom OAuthAuthorize class (defined in this module) to handle OAuth communication.
            AuthenticationManager.Current.OAuthAuthorizeHandler = new OAuthAuthorize();

            // Use a function in this class to challenge for credentials.
            AuthenticationManager.Current.ChallengeHandler = new ChallengeHandler(CreateCredentialAsync);
        }

        public async Task<Credential> CreateCredentialAsync(CredentialRequestInfo info)
        {
            // ChallengeHandler function for AuthenticationManager that will be called whenever a secured resource is accessed.
            Credential credential = null;

            try
            {
                // AuthenticationManager will handle challenging the user for credentials.
                credential = await AuthenticationManager.Current.GenerateCredentialAsync(info.ServiceUri);
            }
            catch (Exception)
            {
                // Exception will be reported in calling function.
                throw;
            }

            return credential;
        }
    }

    // In a desktop (WPF) app, an IOAuthAuthorizeHandler component is used to handle some of the OAuth details. Specifically, it
    //     implements AuthorizeAsync to show the login UI (generated by the server that hosts secure content) in a web control.
    //     When the user logs in successfully, cancels the login, or closes the window without continuing, the IOAuthAuthorizeHandler
    //     is responsible for obtaining the authorization from the server or raising an OperationCanceledException.
    // Note: a custom IOAuthAuthorizeHandler component is not necessary when using OAuth in an ArcGIS Runtime Universal Windows app.
    //     The UWP AuthenticationManager uses a built-in IOAuthAuthorizeHandler that is based on WebAuthenticationBroker.
    public class OAuthAuthorize : IOAuthAuthorizeHandler
    {
        // A window to contain the OAuth UI.
        private Window _authWindow;

        // A TaskCompletionSource to track the completion of the authorization.
        private TaskCompletionSource<IDictionary<string, string>> _taskCompletionSource;

        // URL for the authorization callback result (the redirect URI configured for the application).
        private string _callbackUrl;

        // URL that handles the OAuth request.
        private string _authorizeUrl;

        // A function to handle authorization requests. It takes the URIs for the secured service, the authorization endpoint, and the redirect URI.
        public Task<IDictionary<string, string>> AuthorizeAsync(Uri serviceUri, Uri authorizeUri, Uri callbackUri)
        {
            // If the TaskCompletionSource.Task has not completed, authorization is in progress.
            if (_taskCompletionSource != null || _authWindow != null)
            {
                // Allow only one authorization process at a time.
                throw new Exception("Authorization is in progress");
            }

            // Store the authorization and redirect URLs.
            _authorizeUrl = authorizeUri.AbsoluteUri;
            _callbackUrl = callbackUri.AbsoluteUri;

            // Create a task completion source to track completion.
            _taskCompletionSource = new TaskCompletionSource<IDictionary<string, string>>();

            // Call a function to show the login controls, make sure it runs on the UI thread.
            Dispatcher dispatcher = System.Windows.Application.Current.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
                AuthorizeOnUIThread(_authorizeUrl);
            else
            {
                Action authorizeOnUIAction = () => AuthorizeOnUIThread(_authorizeUrl);
                dispatcher.BeginInvoke(authorizeOnUIAction);
            }

            // Return the task associated with the TaskCompletionSource.
            return _taskCompletionSource.Task;
        }

        // A function to challenge for OAuth credentials on the UI thread.
        private void AuthorizeOnUIThread(string authorizeUri)
        {
            // Create a WebBrowser control to display the authorize page.
            System.Windows.Controls.WebBrowser authBrowser = new System.Windows.Controls.WebBrowser();

            // Handle the navigating event for the browser to check for a response sent to the redirect URL.
            authBrowser.Navigating += WebBrowserOnNavigating;

            // Display the web browser in a new window.
            _authWindow = new Window
            {
                Content = authBrowser,
                Height = 420,
                Width = 350,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            // Set the app's window as the owner of the browser window (if main window closes, so will the browser).
            if (System.Windows.Application.Current != null && System.Windows.Application.Current.MainWindow != null)
            {
                _authWindow.Owner = System.Windows.Application.Current.MainWindow;
            }

            // Handle the window closed event then navigate to the authorize url.
            _authWindow.Closed += OnWindowClosed;
            authBrowser.Navigate(authorizeUri);

            // Display the Window.
            if (_authWindow != null)
            {
                _authWindow.ShowDialog();
            }
        }

        private void OnWindowClosed(object sender, EventArgs e)
        {
            // If the browser window closes, return the focus to the main window.
            if (_authWindow != null && _authWindow.Owner != null)
            {
                _authWindow.Owner.Focus();
            }

            // If the task wasn't completed, the user must have closed the window without logging in.
            if (_taskCompletionSource != null && !_taskCompletionSource.Task.IsCompleted)
            {
                // Set the task completion to indicate a canceled operation.
                _taskCompletionSource.TrySetCanceled();
            }

            _taskCompletionSource = null;
            _authWindow = null;
        }

        // Handle browser navigation (page content changing).
        private void WebBrowserOnNavigating(object sender, NavigatingCancelEventArgs e)
        {
        // Check for a response to the callback url.
        System.Windows.Controls.WebBrowser webBrowser = sender as System.Windows.Controls.WebBrowser;
            Uri uri = e.Uri;

            // If no browser, uri, or an empty url return.
            if (webBrowser == null || uri == null || _taskCompletionSource == null || String.IsNullOrEmpty(uri.AbsoluteUri))
            {
                return;
            }

            // Check if the new content is from the callback url.
            bool isRedirected = uri.AbsoluteUri.StartsWith(_callbackUrl);

            if (isRedirected)
            {
                // Cancel the event to prevent it from being handled elsewhere.
                e.Cancel = true;

                // Get a local copy of the task completion source.
                TaskCompletionSource<IDictionary<string, string>> tcs = _taskCompletionSource;
                _taskCompletionSource = null;

                // Close the window.
                if (_authWindow != null)
                {
                    _authWindow.Close();
                }

                // Call a helper function to decode the response parameters (which includes the authorization key).
                IDictionary<string, string> authResponse = DecodeParameters(uri);

                // Set the result for the task completion source.
                tcs.SetResult(authResponse);
            }
        }

        // A helper function that decodes values from a querystring into a dictionary of keys and values.
        private static IDictionary<string, string> DecodeParameters(Uri uri)
        {
            // Create a dictionary of key value pairs returned in an OAuth authorization response URI query string.
            string answer = "";

            // Get the values from the URI fragment or query string.
            if (!String.IsNullOrEmpty(uri.Fragment))
            {
                answer = uri.Fragment.Substring(1);
            }
            else
            {
                if (!String.IsNullOrEmpty(uri.Query))
                {
                    answer = uri.Query.Substring(1);
                }
            }

            // Parse parameters into key / value pairs.
            Dictionary<string, string> keyValueDictionary = new Dictionary<string, string>();
            string[] keysAndValues = answer.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string kvString in keysAndValues)
            {
                string[] pair = kvString.Split('=');
                string key = pair[0];
                string value = string.Empty;
                if (key.Length > 1)
                {
                    value = Uri.UnescapeDataString(pair[1]);
                }

                keyValueDictionary.Add(key, value);
            }

            // Return the dictionary of string keys/values.
            return keyValueDictionary;


            #endregion


        }
    }
}
