﻿
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
using System.Linq;
using System.Drawing;
using Point = System.Windows.Point;
using System.Data.SqlTypes;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Navigation;
using Esri.ArcGISRuntime;
using Image = System.Windows.Controls.Image;
using System.IO;
using System.Windows.Documents;

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

            connectAndGetMaps();

            //at this point, we've tried to automatically sign in the user.
            //from here, the user will have to hit "Login" if some part of the process didn't work.
        }

        private async void connectAndGetMaps()
        {
            await AuthenticateWithPortal();

            GetWebMaps(portal);
        }

        private void OnMapLoadStatusChanged(object sender, LoadStatusEventArgs e)
        {
            

            Console.WriteLine("Load Status Change Detected");
            switch (e.Status)
            {
                case LoadStatus.Loaded:
                    this.Dispatcher.Invoke(() =>
                    {
                        StatusTxt.Text = "Map Loaded!";
                    });
                    this.Dispatcher.Invoke(() =>
                        {
                            InitializePathMet();
                        });
 
                    break;

                case LoadStatus.FailedToLoad:
                    this.Dispatcher.Invoke(() =>
                    {
                        StatusTxt.Text = "Map Failed to Load";
                    });
                    UserMapsBox.SelectedIndex = 0;
                    break;

                case LoadStatus.Loading:
                    this.Dispatcher.Invoke(() =>
                    {
                        StatusTxt.Text = "Map Loading...";
                    });
                    break;

                default:
                    this.Dispatcher.Invoke(() =>
                    {
                        StatusTxt.Text = "Choose a Map";
                    });
                    break;
            }
        }

        private void InitializePathMet()
        {
            if(MyMapView.Map.LoadStatus != LoadStatus.Loaded)
            {

                return;
            }
            
            sensors = new SerialSensors(Properties.Settings.Default.SensorsPort);
            sensors.UpdateEvent += OnUpdate;

            sensors.ExistsEvent += () =>
            {
                this.Dispatcher.Invoke((MethodInvoker)(() => { FileExists(); }));
            };

            sensors.SummaryEvent += (double laser, double encoder) =>
            {
                this.Dispatcher.Invoke((MethodInvoker)(() => { Summary(laser, encoder); }));
            };

            UpdateSensors();

            //we only call initialize pathMet if the map is loaded successfully, so if connection is established here, we can move into waitForStart
            //pathMetConnectionEstablished
            if (sensors.Connected)
            {
                WaitForStartPt();
                UpdateUI_PathMetConnected();
            }
        }

        private void Map_LoadStatusChanged(object sender, LoadStatusEventArgs e)
        {
            throw new NotImplementedException();
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

        private bool inPostRun = false;
        private bool runInProgress = false;

        #region pathMet_functions
        private void UpdateSensors()
        {
            if (sensors.Connected)
            {

                if (sensors.Sampling)
                {
                    if (!runInProgress)
                    {
                        UpdateUI_RunInProgress();
                        runInProgress = true;
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

            inPostRun = true;

            WaitForEndPt();
            sensors.Stop();
            lastRunDist = RetrieveLastRunDist();
        }

        private void incrementRunName()
        {
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
            runInProgress = true;

            //disable everything; the sensor will enable it when ready

            //for testing onStop functionality, should be uncommented
            txtFName.IsEnabled = false;
            btnStop.IsEnabled = false;
            pmStart.IsEnabled = false;
             //keep
            //OnStop(sender, e);//delete
            
            

            string name = txtFName.Text;
            if (name == "")
            {
                name = DateTime.Now.ToString("yyyy-MM-dd-HHmmss");
            }

            sensors.Start(name);//keep
            

        }

        private void btnRestart_Click(object sender, EventArgs e)
        {
            if (!sensors.Connected)
            {
                return;
            }

            sensors.Restart();
        }

        private void onSubmit(object sender, EventArgs e)
        {
            //convert current run to feature

            //upload newly created feature to featuretable

            incrementRunName();

            resetForNewRun();

        }

        private void resetForNewRun()
        {
            //say that we're not in post run anymore
            inPostRun = false;

            //clear currentRunGeom
            currentRunGeom = null;

            //clear the last run dist
            lastRunDist = 0.0;

            //clear the graphics layers which are only used for drawing the run
            runsLineOverlay.Graphics.Clear();
            runsPointOverlay.Graphics.Clear();
            startPointOverlay.Graphics.Clear();
            endPointOverlay.Graphics.Clear();

            //be ready to pick a new starting point
            WaitForStartPt();
        }

        private void onDiscard(object sender, EventArgs e)
        {
            //delete folder that had the run in it

            //dont upload to feature layer

            //make folder name not increment if possible

            resetForNewRun();
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
        private Polyline currentRunGeom = null;
        private MapPoint startPoint = null;
        private double lastRunDist;
        PolylineBuilder polyLineBuilder;
        GraphicsOverlay runsPointOverlay;
        GraphicsOverlay runsLineOverlay;
        GraphicsOverlay startPointOverlay;
        GraphicsOverlay endPointOverlay;
        double RunDistTolerance = 50.0;


        private async void MapChosen(object sender, SelectionChangedEventArgs e)
        {
            //if user has a map selected, load that map
            if (UserMapsBox.SelectedIndex == 0)
            {
                MyMapView.Map = new Map();
            }
            else
            {
                var mapId = availableMaps.ElementAt(UserMapsBox.SelectedIndex - 1).ItemId;
                InitializeMap(mapId);
            }
        }
        
        
        private async void InitializeMap(String mapId)
        {
            Console.WriteLine("Initializing Map");
            try
            {

                //get map by portal
                var portalItem = await PortalItem.CreateAsync(portal, mapId);
                Map currentMap = new Map(portalItem);
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

                currentMap.LoadStatusChanged += OnMapLoadStatusChanged;

                //map assignment
                MyMapView.Map = currentMap;


                //extent settings for navigation mode
                MyMapView.LocationDisplay.IsEnabled = true;

                MyMapView.LocationDisplay.AutoPanMode = LocationDisplayAutoPanMode.CompassNavigation;

            }
            catch
            {
                throw;
            }
                
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
            if(!sensors.Connected) 
            {
                System.Windows.MessageBox.Show("Lost pathMet connection");
                InitializePathMet();
                return;
            }

            if (MyMapView.Map.LoadStatus != LoadStatus.Loaded)
            {
                System.Windows.MessageBox.Show("Map not loaded");
                UpdateUI_waitForLogin();
                return;
            }

            startPtChosen = false;
            MyMapView.GeoViewTapped += StartPt_Tapped;
            UpdateUI_waitForStartPt();
        }

        private async void StartPt_Tapped(object sender, GeoViewInputEventArgs e)
        {
            DataContext = MyMapView.SketchEditor;

            MapPoint tappedPoint = e.Location;

            //try to snap the line
            MapPoint snappedPoint = await SnapToLine(tappedPoint);

            if (!Geometry.IsNullOrEmpty(snappedPoint)) { 
                // Project the snapped point to whatever spatial reference you want
                MapPoint projectedPoint = (MapPoint)GeometryEngine.Project(snappedPoint, SpatialReferences.WebMercator);

                //add tapped point through polyline builder and update graphics
                startPointOverlay.Graphics.Clear();

                polyLineBuilder = new PolylineBuilder(SpatialReferences.WebMercator);
                polyLineBuilder.AddPoint(projectedPoint);

                startPointOverlay.Graphics.Add(new Graphic(projectedPoint));
                //runsLineOverlay.Graphics.Add(new Graphic(polyLineBuilder.ToGeometry()));

                currentRunGeom = polyLineBuilder.ToGeometry() as Polyline;

                startPtChosen = true;
                startPoint = projectedPoint;
                UpdateUI_ReadyToRun();

                Console.WriteLine("StartPt chosen, start should be available.");

                //TODO: I think delete
                //UpdateSensors();
            }
            else
            {
                Console.WriteLine("Could not snap to any line.");
            }
        }

        //snap the inpu MapPoint to the first operational layer's line. If no line found, this method will return null.
        private async Task<MapPoint> SnapToLine(MapPoint inputPt)
        {

            FeatureLayer _featureLayer = MyMapView.Map.OperationalLayers[0] as FeatureLayer;

            FeatureQueryResult SelectionResult;
            MapPoint snappedPoint = inputPt;
            bool inputSnapped = false;

            try
            {
                // this is essentially the range of snapping to any path
                double tolerance = 15;

                //If the distance to nearest vertex is less than this value, snap to the nearest vertex. Higher values snap to the vertex from farther away.
                //It does not have a unit, since it is relative to the current MapScale
                //.05 will snap to a vertex if you click within 1/20th of the scale of the screen
                double vertexSnapDistance = .01;

                // Convert the tolerance to map units.
                double mapTolerance = tolerance * MyMapView.UnitsPerPixel;

                // Normalize the geometry if wrap-around is enabled.
                //    This is necessary because of how wrapped-around map coordinates are handled by Runtime.
                //    Without this step, querying may fail because wrapped-around coordinates are out of bounds.
                if (MyMapView.IsWrapAroundEnabled)
                {
                    inputPt = (MapPoint)GeometryEngine.NormalizeCentralMeridian(inputPt);
                }

                // Define the envelope around the tap location for selecting features.
                Envelope selectionEnvelope = new Envelope(inputPt.X - mapTolerance, inputPt.Y - mapTolerance, inputPt.X + mapTolerance,
                    inputPt.Y + mapTolerance, MyMapView.Map.SpatialReference);

                // Define the query parameters for selecting features.
                QueryParameters queryParams = new QueryParameters
                {
                    // Set the geometry to selection envelope for selection by geometry.
                    Geometry = selectionEnvelope
                };

                // Select the features based on query parameters defined above.
                //await _featureLayer.SelectFeaturesAsync(queryParams, Esri.ArcGISRuntime.Mapping.SelectionMode.New);
                //SelectionResult = _featureLayer.GetSelectedFeaturesAsync().Result;

                SelectionResult = await _featureLayer.SelectFeaturesAsync(queryParams, Esri.ArcGISRuntime.Mapping.SelectionMode.New);
                _featureLayer.ClearSelection();

                //get closest coordinate 
                ProximityResult nearestCoord = GeometryEngine.NearestCoordinate(SelectionResult.ElementAt(0).Geometry, inputPt);

                //get closest coordinate 
                ProximityResult nearestVert = GeometryEngine.NearestVertex(SelectionResult.ElementAt(0).Geometry, inputPt);

                //set the snapped point to the closest coordinate or the closest vertex if within the vertexSnapDistance

                if (nearestVert.Distance > MyMapView.MapScale * vertexSnapDistance)
                {
                    snappedPoint = nearestCoord.Coordinate;
                    inputSnapped = true;
                }
                else
                {
                    snappedPoint = nearestVert.Coordinate;
                    inputSnapped = true;
                }

            }
            catch (Exception ex)
            {

            }

            if (inputSnapped)
            {
                return snappedPoint;
            }
            else
            {
                return null;
            }

            //------------------------------------------
        }


        private void WaitForEndPt()
        {
            MyMapView.GeoViewTapped += EndPt_Tapped;


            //show length of past run
            PastRunDistContainer.Visibility = Visibility.Visible;
            pathDrawingControls.Visibility = Visibility.Visible;
            
            pastRunDistTxt.Text = lastRunDist.ToString() + "m";

            UpdateUI_waitForEndPt();
        }

        private void UndoLastVertex(object sender, RoutedEventArgs e) {
            if (polyLineBuilder.Parts[0].PointCount > 1)
            {
                polyLineBuilder.Parts[0].RemovePoint(polyLineBuilder.Parts[0].PointCount - 1);
                currentRunGeom = polyLineBuilder.ToGeometry() as Polyline;
                UpdateRunGraphicAndLength();
            }
        }

        private void UpdateRunGraphicAndLength()
        {
            //clear the end point overlay
            endPointOverlay.Graphics.Clear();

            //clear runslineoverlay
            runsLineOverlay.Graphics.Clear();

            //clear runspoint overlay
            runsPointOverlay.Graphics.Clear();

            //redraw the overlays

            var part = polyLineBuilder.Parts[0];

            int ptCount = part.PointCount;

            //put all points except for 0 and last in the runspoint overlay
            for (int i = 1; i < ptCount - 1 ; i++)
            {
                runsPointOverlay.Graphics.Add(new Graphic(part.GetPoint(i)));
            }

            //put the builder.geom as the runsline overlay
            runsLineOverlay.Graphics.Add(new Graphic(polyLineBuilder.ToGeometry()));

            //put the last point in the end point overlay
            endPointOverlay.Graphics.Add(new Graphic(part.GetPoint(ptCount-1)));


            //update line drawn length
            double lineLength = Math.Round(GeometryEngine.LengthGeodetic(polyLineBuilder.ToGeometry(), LinearUnits.Meters, GeodeticCurveType.Geodesic), 1);

            if (Math.Abs(lineLength - RetrieveLastRunDist()) <= RunDistTolerance)
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


        private async void EndPt_Tapped(object sender, GeoViewInputEventArgs e)
        {
            // Get the tapped point - this is in the map's spatial reference,
            // which in this case is WebMercator because that is the SR used by the included basemaps.
            MapPoint tappedPoint = e.Location;
            MapPoint snappedPoint = await SnapToLine(tappedPoint);

            if (!Geometry.IsNullOrEmpty(snappedPoint))
            {

                MapPoint projectedPoint = (MapPoint)GeometryEngine.Project(snappedPoint, SpatialReferences.WebMercator);

                //clear old endpoint and line
                endPointOverlay.Graphics.Clear();
                runsLineOverlay.Graphics.Clear();

                polyLineBuilder.AddPoint(projectedPoint);
                currentRunGeom = polyLineBuilder.ToGeometry() as Polyline;

                UpdateRunGraphicAndLength();

            }
            else
            {
                Console.WriteLine("Could not snap to any line.");
            }

        }

        private void doneDrawingLine_Click(object sender, RoutedEventArgs e)
        {
            endPointOverlay.Graphics.Clear();
            startPointOverlay.Graphics.Clear();
        }

        private double RetrieveLastRunDist()
        {
            Console.WriteLine("Getting last run dist: " + sensors.EncoderFinishDist);
            //return runDist;
            return sensors.EncoderFinishDist;
        }
        #endregion

        #region UI_functions
        private void InitializeUI()
        {
            StatusTxt.Background = System.Windows.Media.Brushes.Red;
            chkbxPm.Background = System.Windows.Media.Brushes.Red;
            chkbxPm.IsChecked = false;

            UserMapsBox.IsEnabled = false;

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

        private void UpdateUI_waitForLogin()
        {
            StatusTxt.Text = "Log In to Load a Map";
        }

        private void UpdateUI_waitForMapChosen()
        {
            StatusTxt.Text = "Choose a map from the dropdown";
            UserMapsBox.IsEnabled = true;
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
        private const string ServerUrl = "http://pathvu.maps.arcgis.com/sharing/rest";

        // - The Client ID for an app registered with the server (the ID below is for a public app created by the ArcGIS Runtime team).
        //private const string AppClientId = @"4PYwJ5MaXYfmMjss"; // our actual clientId
        private const string AppClientId = @"lgAdHkYZYlwwfAhC";

        // - An optional client secret for the app (only needed for the OAuthAuthorizationCode authorization type).
        //private const string AppClientSecret = "81bf742b3e2f4d249d16557cc8e41ac9"; //our actual secret
        private const string AppClientSecret = "";

        // - A URL for redirecting after a successful authorization (this must be a URL configured with the app).
        private const string OAuthRedirectUrl = @"my-ags-app://auth";
        // - The ID for a web map item hosted on the server (the ID below is for a traffic map of Paris).
        private const string WebMapId = "807b21d5a5f44d828a80c1c54ca43bea";

        ArcGISPortal portal;

        List<PortalItem> availableMaps;

        //also gets webMaps
        private async Task AuthenticateWithPortal()
        {
            UpdateUI_waitForLogin();
            try
            {
                // Set up the AuthenticationManager to use OAuth for secure ArcGIS Online requests.
                SetOAuthInfo();

                await SignIn();

                // Connect to the portal (ArcGIS Online, for example).
                portal = await ArcGISPortal.CreateAsync(new Uri(ServerUrl));
                
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void SetOAuthInfo()
        {
            Console.WriteLine("Setauth info called");
            // Register the server information with the AuthenticationManager, including the OAuth settings.
            ServerInfo serverInfo = new ServerInfo
            {
                ServerUri = new Uri("http://pathvu.maps.arcgis.com/sharing/rest"),
                TokenAuthenticationType = TokenAuthenticationType.OAuthAuthorizationCode,
                OAuthClientInfo = new OAuthClientInfo
                {
                    ClientId = AppClientId,
                    RedirectUri = new Uri(OAuthRedirectUrl)
                }
            };

            if (!String.IsNullOrEmpty(AppClientSecret))
            {
                // Use OAuthAuthorizationCode if you need a refresh token (and have specified a valid client secret).
                serverInfo.TokenAuthenticationType = TokenAuthenticationType.OAuthAuthorizationCode;
                serverInfo.OAuthClientInfo.ClientSecret = AppClientSecret;
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

        private async Task SignIn()
        {
            CredentialRequestInfo cri = new CredentialRequestInfo
            {
                // token authentication
                AuthenticationType = AuthenticationType.Token,
                // define the service URI
                ServiceUri = new Uri(ServerUrl),
                // OAuth (implicit flow) token type
                GenerateTokenOptions = new GenerateTokenOptions
                {
                    TokenAuthenticationType = TokenAuthenticationType.OAuthAuthorizationCode
                }
            };

            try
            {
                Console.WriteLine("getCredAsync Called");
                var crd = await AuthenticationManager.Current.GetCredentialAsync(cri, false);

                AuthenticationManager.Current.AddCredential(crd);
            }
            catch
            {
                throw;
            }
        }

        private async Task SignOut()
        {
            // clear the credential for the server from the IdenityManager (if it exists)
            await AuthenticationManager.Current.RemoveAndRevokeAllCredentialsAsync();

            // access the portal as anonymous
            this.portal = await ArcGISPortal.CreateAsync(new Uri(ServerUrl));
        }


        private async void GetWebMaps(ArcGISPortal portal)
        {
            if (portal == null)
            {
                UpdateUI_waitForLogin();
                return;
            }

            var groups = portal.User.Groups;

            //get all webmaps that belong to the user this portal was created with
            foreach (PortalGroup portalGroup in groups)
            {
                Console.WriteLine("grouptitle: " + portalGroup.Title);
                var parameters = PortalQueryParameters.CreateForItemsOfTypeInGroup(PortalItemType.WebMap, portalGroup.GroupId);
                parameters.Limit = 50;

                var portalItems = (await portal.FindItemsAsync(parameters)).Results;

                availableMaps = portalItems.ToList();
            }

            if (availableMaps.Count > 0)
            {
                //show the retrieved maps in the dropdown box
                foreach (PortalItem map in availableMaps)
                {
                    ComboBoxItem availableMap = new ComboBoxItem();
                    availableMap.Content = map.Title;
                    availableMap.Tag = map.ItemId;
                    UserMapsBox.Items.Add(availableMap);
                }
                UpdateUI_waitForMapChosen();
                
            }
            else
            {
                StatusTxt.Text = "No maps found for user. Log in to a different account or try again.";
            }
        }


        #endregion


        private void reviewBtn_Click(object sender, RoutedEventArgs e)
        {
            List<String> imgPaths = Directory.GetFiles(sensors.directory, "*.png").ToList();
            ReviewImagesWindow r = new ReviewImagesWindow(imgPaths);
            r.ShowDialog();
        }

        private async void loginBtn_Click(object sender, RoutedEventArgs e)
        {
            await SignOut();
            connectAndGetMaps();
        }
    }

}
