
using Esri.ArcGISRuntime;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Portal;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Tasks;
using Esri.ArcGISRuntime.Tasks.Offline;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Threading;
using Color = System.Drawing.Color;
using Point = System.Windows.Point;
using System.Windows.Media.Imaging;
using System.Data.SqlTypes;

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

            //TODO: If we can't get internet out in the field at this point, 
            //there needs to be some way to check if we have any local data that we can continue working on. 
            //maybe that means we need to remain signed in on app close
            //and if we come back and we're still logged in, then we will be able to get stuff
            //also means that we need to have an explicit logout button.

            //at this point, we've tried to automatically sign in the user.
            //from here, the user will have to hit "Login" if some part of the process didn't work.

            lastSubmitTime = DateTime.UtcNow;
            lastInternetLostConnectionTime = DateTime.UtcNow;
            lastSyncTime = DateTime.UtcNow;
            lastStartTime = DateTime.UtcNow;

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1)};
            timer.Tick += TimedSync;
            timer.Start();
        }

        int syncWaitPeriod = 10; //minutes
        DateTime lastSubmitTime;
        DateTime lastStartTime;
        DateTime lastInternetLostConnectionTime;
        DateTime lastSyncTime;

        private void TimedSync(object sender, EventArgs e)
        {
            DateTime now = DateTime.UtcNow;


            if (!internetConnected() || !needMapSync)
            {
                return;
            }


            if (lastInternetLostConnectionTime.AddMinutes(syncWaitPeriod) > now || lastStartTime.AddMinutes(syncWaitPeriod) > now)
            {
                return;
            }

            if(lastSubmitTime.AddMinutes(syncWaitPeriod) > now || lastSyncTime.AddMinutes(syncWaitPeriod) > now)
            {
                return;
            }

            //if we've made it through all the checks
            SyncOfflineMap(MyMapView.Map, true);
        }

        private async void connectAndGetMaps()
        {
            //TODO check if we have a map already downloaded/ let user browse downloaded maps

            if (!internetConnected()){

                StatusTxt.Text = "No internet connection. Reconnect to get maps.";
                StatusTxt.Background = System.Windows.Media.Brushes.Red;
                return;

            }

            bool result = await AuthenticateWithPortal();


            //code for detecting unsynced maps to be synced now
            //List<String> mapsToSync = Properties.Settings.Default.MapsToSync.Split(',').Where(x => !string.IsNullOrEmpty(x)).ToList();
            //if (mapsToSync.Count() > 0)
            //{
            //    foreach (string mapId in mapsToSync)
            //    {

            //        var portalItem = await PortalItem.CreateAsync(portal, mapId);
            //        Map map = new Map(portalItem);

            //        SyncOfflineMap(map);
            //    }
            //}

            if (result)
            {
                GetWebMaps(portal);
            }
            else
            {
                new UserMessageBox("Could not authenticate or create portal connection for this user. Try signing in again.", "Could not Authenticate User.", "error").ShowDialog();
            }
            
        }

        private void OnMapLoadStatusChanged(object sender, LoadStatusEventArgs e)
        {


            Console.WriteLine("Load Status Change Detected");
            switch (e.Status)
            {
                case LoadStatus.Loaded:
                    this.Dispatcher.Invoke(() =>
                    {
                        StatusTxt.Text = "Map Loaded! Take Map Offline to start making runs.";
                        StatusTxt.Background = System.Windows.Media.Brushes.Transparent;
                        downloadMapBtn.Visibility = Visibility.Visible;
                    });
                    /*this.Dispatcher.Invoke(() =>
                        {
                            InitializePathMet();
                        });*/

                    break;

                case LoadStatus.FailedToLoad:
                    this.Dispatcher.Invoke(() =>
                    {
                        StatusTxt.Text = "Map Failed to Load";
                        StatusTxt.Background = System.Windows.Media.Brushes.Red;
                    });
                    UserMapsBox.SelectedIndex = 0;
                    break;

                case LoadStatus.Loading:
                    this.Dispatcher.Invoke(() =>
                    {
                        StatusTxt.Text = "Map Loading...";
                        StatusTxt.Background = System.Windows.Media.Brushes.Transparent;
                    });
                    break;

                default:
                    this.Dispatcher.Invoke(() =>
                    {
                        StatusTxt.Text = "Choose a Map";
                        StatusTxt.Background = System.Windows.Media.Brushes.Transparent;
                    });
                    break;
            }
        }

        private void InitializePathMet()
        {
            Console.WriteLine("initializing pathmet...");
            try
            {
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

                if (sensors.Connected)
                {
                    UpdateUI_PathMetConnected();
                    if (MyMapView.Map.LoadStatus == LoadStatus.Loaded)
                    {
                        WaitForStartPt();
                    }
                    else
                    {
                        UpdateUI_waitForMapChosen();
                    }
                }
                else
                {
                    UpdateUI_SensorsNotConnected();
                    new UserMessageBox("PathMet Connection could not be established. \n - Unplug PathMet \n -Turn PathMet off, then back on \n -wait for 20 seconds for laser to initialize \n -click 'Restart Service' again", "PathMet connection failed").ShowDialog();
                }
            }
            catch
            {
                UpdateUI_SensorsNotConnected();
                new UserMessageBox("PathMet Connection could not be established. \n - Unplug PathMet \n -Turn PathMet off, then back on \n -wait for 20 seconds for laser to initialize \n -click 'Restart Service' again", "PathMet connection failed").ShowDialog();
            }
        }

        

        private void Map_LoadStatusChanged(object sender, LoadStatusEventArgs e)
        {
            throw new NotImplementedException();
        }

        void Window_Closing(object sender, CancelEventArgs e)
        {

            // If map has not been synced yet
            if (needMapSync)
            {

                bool result = (bool) new UserMessageBox("Current map has not been synced. Closing now will not update the online ArcGIS map with the current map on this device. However, run data will still be available in the file system.", "Close Without Syncing?", "yesno").ShowDialog();
                
                if (result)
                {
                    // If user doesn't want to close, cancel closure
                    e.Cancel = true;
                }
                else
                { 
                    //user is okay with closing without syncing. Save info about the unsynced map to User Properties
                    Properties.Settings.Default.MapsToSync += ("," + mapIdToSync);
                    Properties.Settings.Default.Save();
                }
            }

            if (sensors != null)
            {
                sensors.UpdateEvent -= OnUpdate;
                sensors.Dispose();
            }

        }

        delegate void UpdateSensorsDel();

        private void OnUpdate()
        {
            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new UpdateSensorsDel(UpdateSensors));
        }

        private void FileExists()
        {
            new UserMessageBox("That run exists. Please choose a different name.", "File Exists").ShowDialog();
            txtFName.IsEnabled = true;

        }

        private void Summary(double laser, double encoder)
        {
            new UserMessageBox(String.Format("Laser: {0:0.000} in\nEncoder: {1:0.0} ft", laser, encoder / 12.0), "Summary").ShowDialog();
        }

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
                else
                {
                    runInProgress = false;
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


            sensors.Stop();

            lastRunDist = RetrieveLastRunDist();
            showPastRunDist();

            WaitForEndPt();
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
                UpdateUI_SensorsNotConnected();
                return;
            }

            if (!startPtChosen)
            {
                return;
            }

            MyMapView.GeoViewTapped -= StartPt_Tapped;

            //disable everything; the sensor will enable it when ready

            txtFName.IsEnabled = false;
            btnStop.IsEnabled = false;
            pmStart.IsEnabled = false;
            btnRetryPmConnect.IsEnabled = false;


            string name = txtFName.Text;
            if (name == "")
            {
                name = DateTime.Now.ToString("yyyy-MM-dd-HHmmss");
            }

            //sensors will call updateUI
            sensors.Start(name);//keep
            CurrentRunFolderPath = sensors.directory;
            lastStartTime = DateTime.UtcNow;
        }

        private void retryPmConnection_Click(object sender, EventArgs e)
        {
            if (sensors != null)
            {
                sensors.UpdateEvent -= OnUpdate;
                sensors.Dispose();
            }

            InitializePathMet();
        }

        private async void onSubmit(object sender, EventArgs e)
        {
            try
            {
                //access layer we're writing the run to
                //just have 1 here for simplicity on these first maps
                FeatureLayer runsLayer = MyMapView.Map.OperationalLayers[1] as FeatureLayer;
                GeodatabaseFeatureTable runFeatureTable = (GeodatabaseFeatureTable)runsLayer.FeatureTable;

                //convert current run to feature
                ArcGISFeature newRunFeature = (ArcGISFeature)runFeatureTable.CreateFeature();
                newRunFeature.Geometry = currentRunGeom;

                //TODO here you could check for if we are trying to submit an incomplete run and handle it differently

                //set feature attributes
                String runName = txtFName.Text;
                newRunFeature.SetAttributeValue("Completion", "Complete");
                newRunFeature.SetAttributeValue("Run", runName);
                newRunFeature.SetAttributeValue("Street_Type", "Sidewalk");
                newRunFeature.SetAttributeValue("Comments", CommentBox.Text);

                //upload newly created feature to featuretable
                await runFeatureTable.AddFeatureAsync(newRunFeature);

                //apply edits
                //runfeaturetable.applyedits

                mapNotSynced();

                //update the last submit time
                lastSubmitTime = DateTime.UtcNow;

                // Update the feature to get the updated objectid - a temporary ID is used before the feature is added.
                newRunFeature.Refresh();
            }
            catch (Exception ex)
            {
                new UserMessageBox(ex.Message, "Error adding feature","error").ShowDialog();
            }

            resetForNewRun(true);
        }

        private void resetForNewRun(bool increment)
        {

            if (increment)
            {
                incrementRunName();
            }

            //clear currentRunGeom
            currentRunGeom = null;

            //clear the last run dist
            lastRunDist = 0.0;

            CommentBox.Text = "";
            showPastRunDist();

            //take all tap actions off the map
            MyMapView.GeoViewTapped -= StartPt_Tapped;
            MyMapView.GeoViewTapped -= EndPt_Tapped;

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
            Directory.Delete(CurrentRunFolderPath, true);

            //dont upload to feature layer
            //make folder name not increment if possible

            resetForNewRun(false);
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
        private String CurrentRunFolderPath = "";
        private MapPoint startPoint = null;
        private double lastRunDist;
        PolylineBuilder polyLineBuilder;
        GraphicsOverlay runsPointOverlay;
        GraphicsOverlay runsLineOverlay;
        GraphicsOverlay startPointOverlay;
        GraphicsOverlay endPointOverlay;
        double RunDistTolerance = 50.0;


        private void MapChosen(object sender, SelectionChangedEventArgs e)
        {
            //if user has a map selected, load that map
            if (UserMapsBox.SelectedIndex < 1)
            {
                MyMapView.Map = new Map();
            }
            else
            {
                if (internetConnected())
                {
                    var mapId = availableMaps.ElementAt(UserMapsBox.SelectedIndex - 1).ItemId;
                    InitializeMap(mapId);

                }
                else
                {
                    new UserMessageBox("Reconnect to the internet to be able to load this map.", "Unable to Load Map Without Internet Connection", "error").ShowDialog();
                }
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
                SimpleMarkerSymbol startMarkerSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Diamond, Color.DarkOrange, 20);
                startPointOverlay.Renderer = new SimpleRenderer(startMarkerSymbol);
                MyMapView.GraphicsOverlays.Add(startPointOverlay);

                // Add a graphics overlay for showing the endPt.
                endPointOverlay = new GraphicsOverlay();
                SimpleMarkerSymbol endMarkerSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Diamond, Color.DarkRed, 20);
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
                StatusTxt.Background = System.Windows.Media.Brushes.Transparent;
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
                new UserMessageBox("Error Editing Shape", ex.Message, "error").ShowDialog();

            }
        }


        //procedure sets up the UI for choosing a starting point
        private void WaitForStartPt()
        {
            if (!sensors.Connected)
            {
                new UserMessageBox("Lost pathMet connection", "PathMet Error", "error").ShowDialog();
                InitializePathMet();
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

            }
            else
            {
                Console.WriteLine("Could not snap to any line.");
            }
        }

        //snap the inpu MapPoint to the first operational layer's line. If no line found, this method will return null.
        private async Task<MapPoint> SnapToLine(MapPoint inputPt)
        {

            FeatureLayer _featureLayer = MyMapView.Map.OperationalLayers.First<Layer>() as FeatureLayer;

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

                if (SelectionResult.Count() > 0)
                {
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
                else
                {
                    return null;
                }

            }
            catch (Exception ex)
            {
                new UserMessageBox(ex.Message, "Exception in SnapToLine").ShowDialog();
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
            pathDrawingControls.Visibility = Visibility.Visible;
            

            UpdateUI_waitForEndPt();
        }

        private void showPastRunDist()
        {
            pastRunDistTxt.Text = lastRunDist.ToString() + "ft";
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
            for (int i = 1; i < ptCount - 1; i++)
            {
                runsPointOverlay.Graphics.Add(new Graphic(part.GetPoint(i)));
            }

            //put the builder.geom as the runsline overlay
            runsLineOverlay.Graphics.Add(new Graphic(polyLineBuilder.ToGeometry()));

            //put the last point in the end point overlay
            endPointOverlay.Graphics.Add(new Graphic(part.GetPoint(ptCount - 1)));


            //update line drawn length
            double lineLength = Math.Round(GeometryEngine.LengthGeodetic(polyLineBuilder.ToGeometry(), LinearUnits.Feet, GeodeticCurveType.Geodesic), 1);

            if (drawnPathMatchesRunData(lineLength))
            {
                StatusTxt.Text = "Drawn length matches last run data.";
                StatusTxt.Background = System.Windows.Media.Brushes.Transparent;
                drawnRunDistTxt.Text = lineLength + "ft";
                drawnLineStatusIndicator.BorderBrush = System.Windows.Media.Brushes.LightGreen;

                UpdateUI_PostRun();
            }
            else
            {
                UpdateUI_waitForEndPt();
                StatusTxt.Text = "Drawn length does not match last run data.";
                drawnRunDistTxt.Text = lineLength + "ft";
                StatusTxt.Background = System.Windows.Media.Brushes.Red;
                drawnLineStatusIndicator.BorderBrush = System.Windows.Media.Brushes.Red;

            }
        }

        private bool drawnPathMatchesRunData(double lineLength)
        {
            double difference = Math.Abs(lineLength - RetrieveLastRunDist());
            return (difference <= RunDistTolerance) && lineLength > 0;
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

        private double RetrieveLastRunDist()
        {
            //encoder is inches
            Console.WriteLine("Getting last run dist: " + sensors.EncoderFinishDist);
            //return runDist;
            return Math.Round((sensors.EncoderFinishDist) / 12, 2);
        }


        private GenerateOfflineMapJob _generateOfflineMapJob;
        private async void downloadOfflineMap(object sender, RoutedEventArgs e)
        {
            
            if (MyMapView.Map.LoadStatus != LoadStatus.Loaded)
            {
                new UserMessageBox("No map loaded, so no map to take offline. MapLoadStatus: " + MyMapView.Map.LoadStatus.ToString(), "No Map Loaded").ShowDialog();

                UpdateUI_waitForLogin();
                return;
            }

            if (!internetConnected())
            {
                StatusTxt.Text = "No internet connection. Reconnect to get maps.";
                StatusTxt.Background = System.Windows.Media.Brushes.Red;
                return;
            }

            try
            {
                progressIndicator.Visibility = Visibility.Visible;
                busyJobText.Text = "Downloading offline map... ";

                //area of interest set to the extent of the current map
                Envelope areaOfInterest = MyMapView.Map.Item.Extent;

                //set up our folder to hold offline map data
                var offlineMapdatafolder = Path.Combine(Properties.Settings.Default.LogPath, "OfflineMapData", MyMapView.Map.Item.ItemId);

                if (Directory.Exists(offlineMapdatafolder))
                {
                    Directory.Delete(offlineMapdatafolder, true);
                }

                if (!Directory.Exists(offlineMapdatafolder))
                {
                    Directory.CreateDirectory(offlineMapdatafolder);
                }

                // Create an offline map task with the current (online) map.
                OfflineMapTask takeMapOfflineTask = await OfflineMapTask.CreateAsync(MyMapView.Map);

                // Create the default parameters for the task, pass in the area of interest.
                GenerateOfflineMapParameters parameters = await takeMapOfflineTask.CreateDefaultGenerateOfflineMapParametersAsync(areaOfInterest);

                // Create the job with the parameters and output location.
                _generateOfflineMapJob = takeMapOfflineTask.GenerateOfflineMap(parameters, offlineMapdatafolder);

                // Handle the progress changed event for the job.
                _generateOfflineMapJob.ProgressChanged += OfflineMapJob_ProgressChanged;

                // Await the job to generate geodatabases, export tile packages, and create the mobile map package.
                GenerateOfflineMapResult results = await _generateOfflineMapJob.GetResultAsync();

                // Check for job failure (writing the output was denied, e.g.).
                if (_generateOfflineMapJob.Status != JobStatus.Succeeded)
                {
                    new UserMessageBox("Generate offline map package failed.", "Job Status", "error").ShowDialog();
                    progressIndicator.Visibility = Visibility.Collapsed;
                }

                // Check for errors with individual layers.
                if (results.LayerErrors.Any())
                {
                    // Build a string to show all layer errors.
                    System.Text.StringBuilder errorBuilder = new System.Text.StringBuilder();
                    foreach (KeyValuePair<Layer, Exception> layerError in results.LayerErrors)
                    {
                        errorBuilder.AppendLine(string.Format("{0} : {1}", layerError.Key.Id, layerError.Value.Message));
                    }

                    // Show layer errors.
                    string errorText = errorBuilder.ToString();
                    new UserMessageBox(errorText, "Error Loading One or More Layers", "error").ShowDialog();
                }

                // Display the offline map.
                MyMapView.Map = results.OfflineMap;

                // Apply the original viewpoint for the offline map.
                MyMapView.SetViewpoint(new Viewpoint(areaOfInterest));

                // Hide the "Take map offline" button.
                downloadMapBtn.Visibility = Visibility.Collapsed;


                // Show a message that the map is offline.
                StatusTxt.Text = "Map has been taken offline.";
                StatusTxt.Background = System.Windows.Media.Brushes.Transparent;

                MyMapView.LocationDisplay.IsEnabled = false;

                MyMapView.LocationDisplay.AutoPanMode = LocationDisplayAutoPanMode.Off;

                mapSynced();

                if (sensors == null || sensors.Connected == false)
                {
                    InitializePathMet();
                }
                else
                {
                    WaitForStartPt();
                }
            }
            catch (TaskCanceledException)
            {
                // Generate offline map task was canceled.
                new UserMessageBox("Taking map offline was canceled", "").ShowDialog();
            }
            catch (Exception ex)
            {
                // Exception while taking the map offline.
                new UserMessageBox(ex.Message, "Offline map error", "error").ShowDialog();
            }
            finally
            {
                // Hide the activity indicator when the job is done.
                progressIndicator.Visibility = Visibility.Collapsed;
            }

        }

        private bool needMapSync;
        private string mapIdToSync;
        private async void SyncOfflineMap(Map map, bool isTimed)
        {
            if (needMapSync)
            {
                try
                {
                    progressIndicator.Visibility = Visibility.Visible;
                    busyJobText.Text = "Syncing offline map... ";

                    var task = await OfflineMapSyncTask.CreateAsync(map);

                    var parameters = new OfflineMapSyncParameters()
                    {
                        SyncDirection = SyncDirection.Bidirectional,
                        RollbackOnFailure = true,
                    };

                    var job = task.SyncOfflineMap(parameters);
                    job.ProgressChanged += SyncOfflineMapJob_ProgressChanged;

                    var results = await job.GetResultAsync();

                    // Check for job failure (writing the output was denied, e.g.).
                    if (job.Status == JobStatus.Succeeded)
                    {
                        mapSynced();

                        lastSyncTime = DateTime.UtcNow;
                    }
                    else
                    {
                        new UserMessageBox("Offline map sync failed. Job Status: " + job.Status.ToString(), "Map Sync Failed", "error").ShowDialog();
                        progressIndicator.Visibility = Visibility.Collapsed;
                    }

                    if (results.HasErrors)
                    {
                        new UserMessageBox("Layer errors with offline map sync", "Layer Errors", "error").ShowDialog();
                    }

                    // Show a message that the map is offline.
                    StatusTxt.Text = "Offline Map has been synced.";
                    StatusTxt.Background = System.Windows.Media.Brushes.Transparent;
                }
                catch (TaskCanceledException)
                {
                    //  task was canceled.
                    new UserMessageBox("Syncing offline map was canceled", "Sync Cancelled").ShowDialog();
                }
                catch (Exception ex)
                {
                    // Exception while taking the map offline.
                    new UserMessageBox(ex.Message, "Offline map sync error", "error").ShowDialog();
                }
                finally
                {
                    // Hide the activity indicator when the job is done.
                    progressIndicator.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                if (!isTimed)
                {
                    new UserMessageBox("Map Does not need syncing.", "Sync Not Needed").ShowDialog();
                }
            }
        }

        private void mapSynced()
        {
            needMapSync = false;
            mapIdToSync = "";
            SyncBtn.IsEnabled = false;
            MapSyncStatus.Text = "Map is Synced";
            MapSyncStatus.Background = System.Windows.Media.Brushes.PowderBlue;
        }

        private void mapNotSynced()
        {
            needMapSync = true;
            mapIdToSync = MyMapView.Map.Item.ItemId;
            SyncBtn.IsEnabled = true;
            MapSyncStatus.Text = "Map has not been synced";
            MapSyncStatus.Background = System.Windows.Media.Brushes.LightPink;
        }

        private async void SyncOfflineWaitingMaps()
        {
            //here is where we want to go through the list of offline maps downloaded that need syncing and sync them 
        }

        private void OfflineMapJob_ProgressChanged(object sender, EventArgs e)
        {
            // Get the job.
            GenerateOfflineMapJob job = sender as GenerateOfflineMapJob;

            // Dispatch to the UI thread.
            Dispatcher.Invoke(() =>
            {
                // Show the percent complete and update the progress bar.
                Percentage.Text = job.Progress > 0 ? job.Progress.ToString() + " %" : string.Empty;
                progressBar.Value = job.Progress;
            });
        }

        private void SyncOfflineMapJob_ProgressChanged(object sender, EventArgs e)
        {
            // Get the job.
            OfflineMapSyncJob job = sender as OfflineMapSyncJob;

            // Dispatch to the UI thread.
            Dispatcher.Invoke(() =>
            {
                // Show the percent complete and update the progress bar.
                Percentage.Text = job.Progress > 0 ? job.Progress.ToString() + " %" : string.Empty;
                progressBar.Value = job.Progress;
            });
        }

        private void CancelJobButton_Click(object sender, RoutedEventArgs e)
        {
            // The user canceled the job.
            _generateOfflineMapJob.Cancel();
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
            btnRetryPmConnect.IsEnabled = true;
            txtFName.IsEnabled = false;
            btnVegetation.IsEnabled = false;
            btnTrippingHazard.IsEnabled = false;
            btnBrokenSidewalk.IsEnabled = false;
            btnOther.IsEnabled = false;
            pathDrawingControls.Visibility = Visibility.Hidden;
            PostRunControlsPanel.Visibility = Visibility.Hidden;
            FullControlPanel.Visibility = Visibility.Visible;

            StatusTxt.Text = "Initializing";
            StatusTxt.Background = System.Windows.Media.Brushes.Transparent;

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
            StatusTxt.Background = System.Windows.Media.Brushes.Transparent;
        }

        private void UpdateUI_waitForMapChosen()
        {
            StatusTxt.Text = "Choose a map from the dropdown";
            StatusTxt.Background = System.Windows.Media.Brushes.Transparent;
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
            btnRetryPmConnect.IsEnabled = true;

            //fill the name of the run with a default name and allow user to change it
            txtFName.IsEnabled = true;
            if (txtFName.Text == "")
            {
                txtFName.Text = DateTime.Now.ToString("yyyy-MM-dd-HHmmss");
            }
            btnVegetation.IsEnabled = false;
            btnTrippingHazard.IsEnabled = false;
            btnBrokenSidewalk.IsEnabled = false;
            btnOther.IsEnabled = false;
            StatusTxt.Text = "Choose starting point";
            StatusTxt.Background = System.Windows.Media.Brushes.Transparent;

            FullControlPanel.Visibility = Visibility.Visible;
            PostRunControlsPanel.Visibility = Visibility.Hidden;
        }

        private void UpdateUI_waitForEndPt()
        {
            pmStart.IsEnabled = false;
            btnStop.IsEnabled = false;
            btnRetryPmConnect.IsEnabled = false;
            txtFName.IsEnabled = false;
            btnVegetation.IsEnabled = false;
            btnTrippingHazard.IsEnabled = false;
            btnBrokenSidewalk.IsEnabled = false;
            btnOther.IsEnabled = false;
            StatusTxt.Text = "Choosing points for run path...";
            FullControlPanel.Visibility = Visibility.Hidden;
            PostRunControlsPanel.Visibility = Visibility.Visible;
            submitBtn.IsEnabled = false;
            StatusTxt.Background = System.Windows.Media.Brushes.Transparent;

        }

        private void UpdateUI_PostRun()
        {
            pmStart.IsEnabled = false;
            btnStop.IsEnabled = false;
            btnRetryPmConnect.IsEnabled = false;
            txtFName.IsEnabled = false;
            btnVegetation.IsEnabled = false;
            btnTrippingHazard.IsEnabled = false;
            btnBrokenSidewalk.IsEnabled = false;
            btnOther.IsEnabled = false;
            FullControlPanel.Visibility = Visibility.Hidden;
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
            StatusTxt.Text = "Now you may type a specific name for the run.\nHit \"Start\" to begin run.";
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

        ArcGISPortal portal;
        String signedInUserName;
        Uri profilePicUri;

        List<PortalItem> availableMaps;

        //also gets webMaps
        private async Task<bool> AuthenticateWithPortal()
        {
            UpdateUI_waitForLogin();
            try
            {
                // Set up the AuthenticationManager to use OAuth for secure ArcGIS Online requests.
                SetOAuthInfo();

                bool result = await SignIn();

                if (result) {
                    // Connect to the portal (ArcGIS Online, for example).
                    portal = await ArcGISPortal.CreateAsync(new Uri(ServerUrl));

                    if (portal != null)
                    {

                        signedInUserName = portal.User.UserName;
                        userName.Text = signedInUserName;
                        userName.Visibility = Visibility.Visible;

                        profilePicUri = portal.User.ThumbnailUri;
                        
                        profilePic.Source = profilePicUri != null ? new BitmapImage(profilePicUri) : null;
                        profilePic.Visibility = Visibility.Visible;

                        loginBtn.Content = "Log Out";
                        loginBtn.Click -= loginBtn_Click;
                        loginBtn.Click += logoutBtn_Click;

                        return true;
                    }
                }

                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return false;
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

        private async Task<bool> SignIn()
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
                return true;
            }
            catch
            {
                new UserMessageBox("Sign In Process Failed", "Unable to Sign in", "error").ShowDialog();
                UpdateUI_waitForLogin();
                return false;
            }
        }

        private async void SignOut()
        {
            // clear the credential for the server from the IdenityManager (if it exists)
            await AuthenticationManager.Current.RemoveAndRevokeAllCredentialsAsync();

            //reset the portal
            portal = null;

            loginBtn.Content = "Log In";
            loginBtn.Click -= logoutBtn_Click;

            userName.Text = "";
            userName.Visibility = Visibility.Collapsed;

            profilePic.Source = null;
            profilePic.Visibility = Visibility.Collapsed;

            //clear the maps dropdown
            UserMapsBox.Items.Clear();

            loginBtn.Click += loginBtn_Click;
        }


        private async void GetWebMaps(ArcGISPortal portal)
        {

            if (!internetConnected())
            {
                StatusTxt.Text = "No internet connection. Reconnect to get maps.";
                StatusTxt.Background = System.Windows.Media.Brushes.Yellow;
                return;
            }
            
            if (portal == null)
            {
                UpdateUI_waitForLogin();
                return;
            }

            

            try
            {
                var groups = portal.User.Groups;

                //get all webmaps that belong to the user this portal was created with
                foreach (PortalGroup portalGroup in groups)
                {
                    
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
                    StatusTxt.Background = System.Windows.Media.Brushes.Yellow;
                }
            }
            catch
            {

            }
        }


        public bool internetConnected()
        {
            System.Net.WebRequest req = System.Net.WebRequest.Create("https://www.google.co.in/");
            req.Timeout = 4000;
            System.Net.WebResponse resp;
            try
            {
                resp = req.GetResponse();
                resp.Close();
                req = null;
                return true;
            }
            catch
            {
                req = null;
                lastInternetLostConnectionTime = DateTime.UtcNow;
                Console.WriteLine("no internet connection");
                return false;
            }
        }


        #endregion


        private void reviewBtn_Click(object sender, RoutedEventArgs e)
        {
            List<String> imgPaths = Directory.GetFiles(sensors.directory, "*.png").ToList();
            ReviewImagesWindow r = new ReviewImagesWindow(imgPaths);
            r.ShowDialog();
        }

        private void loginBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!internetConnected()){
                StatusTxt.Text = "No internet connection. Reconnect to log in.";
                StatusTxt.Background = System.Windows.Media.Brushes.Red;
                return;
            }
            connectAndGetMaps();
        }

        private void logoutBtn_Click(object sender, RoutedEventArgs e)
        {
            if (needMapSync)
            {

                bool result = (bool)new UserMessageBox("Current map has not been synced. Logging out will not update the online ArcGIS map with the current map on this device. However, run data will still be available in the file system.", "Log out Without Syncing?", "yesno").ShowDialog();

                if (result)
                {
                    // If user doesn't want to log out, just return
                    return;
                }
                else
                {
                    //user is okay with logging out. Save info about the unsynced map to User Properties.
                    Properties.Settings.Default.MapsToSync += ("," + mapIdToSync);
                    Properties.Settings.Default.Save();
                }
            }

            SignOut();
            connectAndGetMaps();
        }

        private void Sync_Clicked(object sender, RoutedEventArgs e)
        {
            SyncOfflineMap(MyMapView.Map, false);
        }

    }

}
