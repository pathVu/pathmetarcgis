
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Management;
using System.IO;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Portal;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Symbology;
using System.Drawing;
using Esri.ArcGISRuntime.UI.Controls;
using Esri.ArcGISRuntime.Security;
using Color = System.Drawing.Color;
using Esri.ArcGISRuntime.Location;
using OpenCvSharp.Aruco;
using System.Data.SqlTypes;
using System.Threading;

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
            Esri.ArcGISRuntime.Mapping.Map currentMap = new Esri.ArcGISRuntime.Mapping.Map(new System.Uri(webMapUrl));

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
            restartDrawingBtn.Visibility = Visibility.Visible;
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
            double lineLength = Math.Round(GeometryEngine.LengthGeodetic(polyLineBuilder.ToGeometry(), LinearUnits.Meters, GeodeticCurveType.Geodesic),1);

            String lengthStatus;

            System.Windows.Media.Color bkgdColor;

            if(Math.Abs(lineLength - GetLastRunDist()) <= RunDistTolerance)
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

        void ShowCallout(MapPoint point)
        {
            CalloutDefinition calloutDef = new CalloutDefinition("Start");
            MyMapView.ShowCalloutAt(point, calloutDef);
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
            restartDrawingBtn.Visibility = Visibility.Hidden;
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

        ArcGISPortal portal;
        PortalUser user;

        private void loginBtn_Click(object sender, RoutedEventArgs e)
        {
            Login L = new Login();
            L.ShowDialog();
        }


        private async void LogIn(String userName, String password)
        {
            // generate an ArcGISTokenCredential using input from the user (username and password)
            var cred = await AuthenticationManager.Current.GenerateCredentialAsync(
                                                            new Uri("http://anorganization.maps.arcgis.com/sharing/rest"),
                                                            userName,
                                                            password) as ArcGISTokenCredential;

            // connect to the portal, pass in the token 
            portal = await ArcGISPortal.CreateAsync(
                                                            new Uri("http://anorganization.maps.arcgis.com/sharing/rest"),
                                                            cred,
                                                            CancellationToken.None);

            // get the current portal user and check privileges
            user = portal.User;
            IEnumerable<PortalPrivilege> privileges = user.Privileges;
        }

        #endregion

        
    }
}
