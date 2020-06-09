
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
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
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using System;
using System.Linq;
using System.Drawing;
using Point = System.Windows.Point;
using System.Data.SqlTypes;
using System.Windows.Input;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Portal;
using Esri.ArcGISRuntime.Security;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Windows.Threading;

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

            InitializeAuthentication();

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
            
            
            if (!sensors.Connected)
            {
                return;
            }

            MyMapView.GeoViewTapped -= StartPt_Tapped;
            UpdateUI_RunInProgress();

            // disable everything; the sensor will enable it when ready

            //for testing onStop functionality, should be uncommented
            /*txtFName.IsEnabled = false;
            btnStop.IsEnabled = false;
            pmStart.IsEnabled = false;
            
            

            string name = txtFName.Text;
            if (name == "")
            {
                name = DateTime.Now.ToString("yyyy-MM-dd-HHmmss");
            }

            sensors.Start(name);
            */

            //for testing onStop functionality, shouldn't be here normally
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
        private Geometry currentRunGeom = null;
        private MapPoint startPoint = null;
        private double lastRunDist;
        PolylineBuilder polyLineBuilder;
        GraphicsOverlay runsPointOverlay;
        GraphicsOverlay runsLineOverlay;
        GraphicsOverlay startPointOverlay;
        GraphicsOverlay endPointOverlay;
        double RunDistTolerance = 20.0;


        private void InitializeMap()
        {
            //basemap
            //Map newMap = new Map(Basemap.CreateNavigationVector());

            //add pathvu specific map to mapView

            //var mapId = "8ff831ddf02344cda858a37c742804dc";
            //var webMapUrl = string.Format("https://www.arcgisonline.com/sharing/rest/content/items/{0}/data", mapId);
            // Map currentMap = new Map(new Uri(webMapUrl));
            Map currentMap = new Map();

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

            DataContext = MyMapView.SketchEditor;

            //extent settings for navigation mode
            MyMapView.LocationDisplay.IsEnabled = true;

            MyMapView.LocationDisplay.AutoPanMode = LocationDisplayAutoPanMode.CompassNavigation;
        }

        
        private async Task<Graphic> ChooseGraphicAsync()
        {
            // Wait for the user to click a location on the map
            Geometry mapPoint = await MyMapView.SketchEditor.StartAsync(SketchCreationMode.Point, false);

            // Convert the map point to a screen point
            Point screenCoordinate = MyMapView.LocationToScreen((MapPoint)mapPoint);

            // Identify graphics in the graphics overlay using the point
            IReadOnlyList<IdentifyGraphicsOverlayResult> results = await MyMapView.IdentifyGraphicsOverlaysAsync(screenCoordinate, 5, false);

            // If results were found, get the first graphic
            Graphic graphic = null;
            IdentifyGraphicsOverlayResult idResult = results.FirstOrDefault();
            if (idResult != null && idResult.Graphics.Count > 0)
            {
                graphic = idResult.Graphics.FirstOrDefault();
            }

            // Return the graphic (or null if none were found)
            return graphic;
        }


        private async void EditButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusTxt.Text = "Choose a line to edit";
                // Allow the user to select a graphic
                Graphic editGraphic = await ChooseGraphicAsync();
                if (editGraphic == null) { return; }

                // Let the user make changes to the graphic's geometry, await the result (updated geometry)
                Esri.ArcGISRuntime.Geometry.Geometry newGeometry = await MyMapView.SketchEditor.StartAsync(editGraphic.Geometry);

                // Display the updated geometry in the graphic
                editGraphic.Geometry = newGeometry;
            }
            catch (TaskCanceledException)
            {
                // Ignore ... let the user cancel editing
            }
            catch (Exception ex)
            {
                // Report exceptions
                System.Windows.MessageBox.Show("Error editing shape: " + ex.Message);
            }
        }


        //procedure sets up the UI for choosing a starting point
        private void WaitForStartPt()
        { 
        
            startPtChosen = false;
            MyMapView.GeoViewTapped += StartPt_Tapped;
            UpdateUI_waitForStartPt();
        }

        private void StartPt_Tapped(object sender, GeoViewInputEventArgs e)
        {
            // Get the tapped point - this is in the map's spatial reference,
            // which in this case is WebMercator because that is the SR used by the included basemaps.
            MapPoint tappedPoint = e.Location;

            // Project the point to whatever spatial reference you want
            MapPoint projectedPoint = (MapPoint)GeometryEngine.Project(tappedPoint, SpatialReferences.WebMercator);

            //TODO add logic to snap to path geometry

            //add tapped point through polyline builder and update graphics
            startPointOverlay.Graphics.Clear();

            polyLineBuilder = new PolylineBuilder(SpatialReferences.WebMercator);
            polyLineBuilder.AddPoint(projectedPoint);

            startPointOverlay.Graphics.Add(new Graphic(projectedPoint));
            //runsLineOverlay.Graphics.Add(new Graphic(polyLineBuilder.ToGeometry()));

            currentRunGeom = polyLineBuilder.ToGeometry();

            startPtChosen = true;
            startPoint = projectedPoint;

            Console.WriteLine("StartPt chosen, start should be available.");
            UpdateSensors();
        }

        private async void WaitForEndPt()
        { 
            //show length of past run
            PastRunDistContainer.Visibility = Visibility.Visible;
            pathDrawingControls.Visibility = Visibility.Visible;
            lastRunDist = GetLastRunDist();
            pastRunDistTxt.Text = lastRunDist.ToString() + "m";

            MyMapView.SketchEditor.GeometryChanged += showLineDataDuringPathDrawing;

            UpdateUI_waitForEndPt();

            try
            {
                // Let the user edit the current geometry 
                Geometry geometry = await MyMapView.SketchEditor.StartAsync(currentRunGeom, SketchCreationMode.Polyline);
                

                // Create and add a graphic from the geometry the user drew
                runsLineOverlay.Graphics.Add(new Graphic(geometry));
            }
            catch (TaskCanceledException)
            {
                // Ignore ... let the user cancel drawing
            }
            catch (Exception ex)
            {
                // Report exceptions
                System.Windows.MessageBox.Show("Error drawing graphic shape: " + ex.Message);
            }

            //get and show the past run's distance to compare our drawing with
        }

        private void showLineDataDuringPathDrawing(object sender, GeometryChangedEventArgs e)
        {
            
            double geomLength = GeometryEngine.LengthGeodetic(MyMapView.SketchEditor.Geometry, LinearUnits.Meters, GeodeticCurveType.Geodesic);
            double lineLength = Math.Round(geomLength, 1);

            System.Windows.Media.Color bkgdColor;

            if (Math.Abs(lineLength - GetLastRunDist()) <= RunDistTolerance)
            {
                StatusTxt.Text = "Drawn length: " + lineLength + " matches last run data.";
                StatusTxt.Background = System.Windows.Media.Brushes.Transparent;
                UpdateUI_PostRun();
            }
            else
            {
                UpdateUI_waitForEndPt();
                StatusTxt.Text = "Drawn length: " + lineLength + " does not match last run data.";
                StatusTxt.Background = System.Windows.Media.Brushes.Red;
            }
            
        }

        private void doneDrawingLine_Click(object sender, RoutedEventArgs e)
        {
            endPointOverlay.Graphics.Clear();
            startPointOverlay.Graphics.Clear();
        }

        private double GetLastRunDist()
        {
            return 20.0;
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

            MyMapView.DismissCallout();
            UpdateUI_waitForEndPt();
            //delete this comment

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

            StatusTxt.Text = "Initializing";

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
            pathDrawingControls.Visibility = Visibility.Hidden;
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
            //StatusTxt.Text = "Post-run: review or submit the completed run.";
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
        private const string ServerUrl = "https://www.arcgis.com/sharing/rest";
        // - The Client ID for an app registered with the server (the ID below is for a public app created by the ArcGIS Runtime team).
        private const string AppClientId = @"lgAdHkYZYlwwfAhC";
        // - An optional client secret for the app (only needed for the OAuthAuthorizationCode authorization type).
        private const string ClientSecret = "";
        // - A URL for redirecting after a successful authorization (this must be a URL configured with the app).
        private const string OAuthRedirectUrl = @"my-ags-app://auth";
        // - The ID for a web map item hosted on the server (the ID below is for a traffic map of Paris).
        private const string WebMapId = "807b21d5a5f44d828a80c1c54ca43bea";

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
        #endregion
    }

}
