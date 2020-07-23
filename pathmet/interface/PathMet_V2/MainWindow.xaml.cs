
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
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Color = System.Drawing.Color;

namespace PathMet_V2
{
    public partial class MainWindow : Window
    {
        private ISensors sensors;

        public MainWindow()
        {

            InitializeComponent();
            
            InitializeUI();

            ConnectAndGetMaps();

            //at this point, we've tried to automatically sign in the user.
            //from here, the user will have to hit "Login" if some part of the process didn't work.

            StartSyncTimer();
            
        }
        
        private async void ConnectAndGetMaps()
        {

            if (!InternetConnected())
            {

                StatusTxt.Text = "No internet connection. Reconnect to get web maps.";
                StatusTxt.Background = System.Windows.Media.Brushes.Red;

                //LoadinofflineMaps will not complete if there is no keptUser, so the connectAndGetMaps() call in loginBtn_click will never successfully load in offline maps
                await LoadInOfflineMaps();

                return;
            }
            else
            {
                bool result = await AuthenticateWithPortal();

                if (result)
                {
                    //we are signed in

                    KeepUser();

                    SyncOfflineWaitingMaps();

                    GetWebMaps();
                }
                else
                {
                    //we could not sign in
                    new UserMessageBox("Could not authenticate or create portal connection for this user. Try signing in again.", "Could not Authenticate User.", "error").ShowDialog();
                }
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
                        if (dropDownMapsAreOnline)
                        {
                            StatusTxt.Text = "Map Loaded! Take Map Offline to start making runs.";
                            StatusTxt.Background = System.Windows.Media.Brushes.Transparent;
                            downloadMapBtn.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            StatusTxt.Text = "Offline map Loaded. ";
                            StatusTxt.Background = System.Windows.Media.Brushes.Transparent;
                            downloadMapBtn.Visibility = Visibility.Collapsed;

                            MapNotSynced();

                            if (sensors == null || sensors.Connected == false)
                            {
                                InitializePathMet();
                            }
                            else
                            {
                                WaitForStartPt();
                            }

                        }
                    });

                    break;

                case LoadStatus.FailedToLoad:
                    this.Dispatcher.Invoke(() =>
                    {
                        StatusTxt.Text = "Map Failed to Load";
                        StatusTxt.Background = System.Windows.Media.Brushes.Red;
                        Console.WriteLine("map failoed to load");
                    });
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
                        if (currentMapIsOffline)
                            WaitForStartPt();
                        else
                        {
                            StatusTxt.Text = "Map Loaded! Take Map Offline to start making runs.";
                            StatusTxt.Background = System.Windows.Media.Brushes.Transparent;
                            downloadMapBtn.Visibility = Visibility.Visible;
                        }

                    }
                    else
                    {
                        UpdateUI_waitForMapChosen();
                    }
                }
                else
                {
                    UpdateUI_SensorsNotConnected();
                    sensors.UpdateEvent -= OnUpdate;
                    sensors.Dispose();
                    new UserMessageBox("PathMet Connection could not be established. \n - Unplug PathMet \n -Turn PathMet off, then back on \n -wait for 20 seconds for laser to initialize \n -click 'Restart Service' again", "PathMet connection failed").ShowDialog();
                }
            }
            catch
            {
                sensors.UpdateEvent -= OnUpdate;
                sensors.Dispose();
                UpdateUI_SensorsNotConnected();
                new UserMessageBox("PathMet Connection could not be established. \n - Unplug PathMet \n -Turn PathMet off, then back on \n -wait for 20 seconds for laser to initialize \n -click 'Restart Service' again", "PathMet connection failed").ShowDialog();
            }
        }

        readonly string mapsToSyncFilePath = Path.Combine(Properties.Settings.Default.LogPath, "MapsToSync.txt");
        readonly string keptUserFilePath = Path.Combine(Properties.Settings.Default.LogPath, "KeptUser.txt");

        void Window_Closing(object sender, CancelEventArgs e)
        {
            if(offlineMapJobResults != null)
            {
                offlineMapJobResults.MobileMapPackage.Close();
            }

            // If map has not been synced yet
            if (needMapSync)
            {
                bool result = (bool) new UserMessageBox("Current map has not been synced. Closing now will not update the online ArcGIS map with the current map on this device. However, run data will still be available in the file system. Are you sure you want to close now?", "Close Without Syncing?", "yesno").ShowDialog();
                
                if (result)

                {
                    // If user doesn't want to close, cancel closure
                    AddToMapsToSync();
                }
                else
                {
                    //user is okay with closing without syncing. Save info about the unsynced map
                    e.Cancel = true;
                }
            }
            else if(userNameBox.Text != "")
            {
                bool result = (bool)new UserMessageBox("If you close now and lose connection to the internet, you will only have access to the maps you have taken offline when you restart the application. Are you sure you want to close now?", "Close?", "yesno").ShowDialog();

                if (!result)
                {
                    // If user doesn't want to log out, just return
                    return;
                }
            }


            if (sensors != null)
            {
                sensors.UpdateEvent -= OnUpdate;
                sensors.Dispose();
            }

            MyMapView.Map = null;

        }

        private void KeepUser()
        {

            List<string> lines = new List<String>
            {
                portal.User.UserName
            };

            File.WriteAllLines(keptUserFilePath, lines);
        }

        private void AddToMapsToSync()
        {
            if (!File.Exists(mapsToSyncFilePath))
            {
                StreamWriter sw = File.CreateText(mapsToSyncFilePath);
                sw.Close();
            }

            List<string> lines = File.ReadAllLines(mapsToSyncFilePath).ToList();



            lines.Add(mapIdToSync + "_" + userNameBox.Text);

            File.WriteAllLines(mapsToSyncFilePath, lines);

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
            ShowPastRunDist();

            WaitForEndPt();
        }

        private void IncrementRunName()
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
                //if run name does not end with a number, add a 2 to the end of it
                name += "2";
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
            sensors.Start(name);
            CurrentRunFolderPath = sensors.directory;
            lastStartTime = DateTime.UtcNow;
        }

        private async void RetryPmConnection_Click(object sender, EventArgs e)
        {
            btnRetryPmConnect_border.Background = darkenedWhiteButtonBrush;

            if (sensors != null)
            {
                sensors.UpdateEvent -= OnUpdate;
                sensors.Dispose();
            }

            InitializePathMet();

            await Task.Delay(250);

            btnRetryPmConnect_border.Background = normalWhiteButtonBrush;

        }

        private async void OnSubmit(object sender, EventArgs e)
        {
            try
            {
                //CURRENTLY IT IS HARDCODED THAT THE SECOND LAYER, LAYER[1], IS THE ONE THAT WE ARE WRITING THE RUN LINES TO
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

                MapNotSynced();

                //update the last submit time
                lastSubmitTime = DateTime.UtcNow;

                // Update the feature to get the updated objectid - a temporary ID is used before the feature is added.
                newRunFeature.Refresh();
            }
            catch (Exception ex)
            {
                new UserMessageBox(ex.Message, "Error adding feature","error").ShowDialog();
            }

            ResetForNewRun(true);
        }

        private void ResetForNewRun(bool increment)
        {

            if (increment)
            {
                IncrementRunName();
            }

            //clear currentRunGeom
            currentRunGeom = null;

            //clear the last run dist
            lastRunDist = 0.0;

            CommentBox.Text = "";
            ShowPastRunDist();

            //take all tap actions off the map
            MyMapView.GeoViewTapped -= StartPt_Tapped;
            MyMapView.GeoViewTapped -= EndPt_Tapped;

            //clear the graphics layers which are only used for drawing the run
            runsLineOverlay.Graphics.Clear();
            runsPointOverlay.Graphics.Clear();
            startPointOverlay.Graphics.Clear();
            endPointOverlay.Graphics.Clear();

            RunContentGrid.Visibility = Visibility.Visible;
            submitBtn.IsEnabled = false;
            UserContentGrid.IsEnabled = true;
            ObstacleGrid.IsEnabled = true;

            //be ready to pick a new starting point
            WaitForStartPt();
        }

        private void OnDiscard(object sender, EventArgs e)
        {
            //delete folder that had the run in it
            Directory.Delete(CurrentRunFolderPath, true);

            ResetForNewRun(false);
        }


        private async void BtnTrippingHazard_Click(object sender, EventArgs e)
        {
            if (!sensors.Connected)
            {
                return;
            }

            sensors.Flag("Tripping Hazard");

            btnTrippingHazard_Container.Background = darkenedBlueButtonBrush;
            await Task.Delay(250);
            btnTrippingHazard_Container.Background = normalBlueButtonBrush;
        }

        private async void BtnBrokenSidewalk_Click(object sender, EventArgs e)
        {
            if (!sensors.Connected)
            {
                return;
            }

            sensors.Flag("Broken Sidewalk");

            btnBrokenSidewalk_Container.Background = darkenedBlueButtonBrush;
            await Task.Delay(250);
            btnBrokenSidewalk_Container.Background = normalBlueButtonBrush;
        }

        private async void BtnVegetation_Click(object sender, EventArgs e)
        {
            if (!sensors.Connected)
            {
                return;
            }

            sensors.Flag("Vegetation");

            btnVegetation_Container.Background = darkenedBlueButtonBrush;
            await Task.Delay(250);
            btnVegetation_Container.Background = normalBlueButtonBrush;

        }

        private async void BtnOther_Click(object sender, EventArgs e)
        {
            if (!sensors.Connected)
            {
                return;
            }

            sensors.Flag("Other");


            btnOther_Container.Background = darkenedBlueButtonBrush;
            await Task.Delay(250);
            btnOther_Container.Background = normalBlueButtonBrush;
        }

        #endregion

        #region map_functions

        private bool startPtChosen = false;
        private Polyline currentRunGeom = null;
        private string CurrentRunFolderPath = "";
        private double lastRunDist;
        PolylineBuilder polyLineBuilder;
        GraphicsOverlay runsPointOverlay;
        GraphicsOverlay runsLineOverlay;
        GraphicsOverlay startPointOverlay;
        GraphicsOverlay endPointOverlay;
        readonly double RunDistTolerance = 50.0;

        int prevSelectedIndex = -1;
        private void MapChosen(object sender, SelectionChangedEventArgs e)
        {
            
            //if user selected the first item, which is not a map, load nothing
            if (UserMapsBox.SelectedIndex < 1)
            {
                //MyMapView.Map = new Map();
                MyMapView.Map = null;
                UpdateUI_waitForMapChosen();
            }
            else
            {
                if (!InternetConnected() && dropDownMapsAreOnline)
                {
                    new UserMessageBox("Reconnect to the internet to be able to load this map.", "Unable to Load Map Without Internet Connection", "error").ShowDialog();
                    return;
                }

                if (needMapSync)
                {
                    bool result = (bool)new UserMessageBox("Current map has not been synced. Switching map will not sync online ArcGIS map with the current map on this device. However, run data will still be available in the file system. Are you sure you want to switch map?", "Switch Map Without Syncing?", "yesno").ShowDialog();

                    if (!result)
                    {
                        UserMapsBox.SelectionChanged -= MapChosen;
                        UserMapsBox.SelectedIndex = prevSelectedIndex;
                        UserMapsBox.SelectionChanged += MapChosen;
                        return;
                    }

                    AddToMapsToSync();
                    MapSynced();
                    MapSyncStatus.Text = "";
                    MapSyncStatus.Background = System.Windows.Media.Brushes.Transparent;
                }

                //set the flag based on if the dropdown maps are online or not. 
                currentMapIsOffline = !dropDownMapsAreOnline;

                InitializeMap((Map)(((ComboBoxItem)UserMapsBox.SelectedItem).Tag));
                prevSelectedIndex = UserMapsBox.SelectedIndex;
            }
        }


        private void InitializeMap(Map inputMap)
        {
            Console.WriteLine("Initializing Map");
            try
            {
                //get map by portal
                Map currentMap = inputMap;

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
                SimpleMarkerSymbol startMarkerSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Diamond, Color.Goldenrod, 20);
                startPointOverlay.Renderer = new SimpleRenderer(startMarkerSymbol);
                MyMapView.GraphicsOverlays.Add(startPointOverlay);

                // Add a graphics overlay for showing the endPt.
                endPointOverlay = new GraphicsOverlay();
                SimpleMarkerSymbol endMarkerSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Diamond, Color.Red, 20);
                endPointOverlay.Renderer = new SimpleRenderer(endMarkerSymbol);
                MyMapView.GraphicsOverlays.Add(endPointOverlay);

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


        //THIS IS UNUSED CODE FOR BEING ABLE TO SELECT RUN LINES ON THE MAP AND DELETE/EDIT THEM---------------------
        /*
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
        */
        //----------------------------------------------------------


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

            if (!Esri.ArcGISRuntime.Geometry.Geometry.IsNullOrEmpty(snappedPoint)) {
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
                //startPoint = projectedPoint;
                UpdateUI_ReadyToRun();

                Console.WriteLine("StartPt chosen, start should be available.");

            }
            else
            {
                Console.WriteLine("Could not snap to any line.");
            }
        }

        //control if we want to let the user place points where there is not a sidewalk
        readonly bool freePointsAllowed = true;
        private async Task<MapPoint> SnapToLine(MapPoint inputPt)
        {
            //IT IS CURRENTLY HARDCODED THAT THE FIRST, 0TH, LAYER IS THE ONE USED FOR SNAPPING
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
                SelectionResult = await _featureLayer.SelectFeaturesAsync(queryParams, Esri.ArcGISRuntime.Mapping.SelectionMode.New);
                _featureLayer.ClearSelection();

                if (SelectionResult.Count() > 0)
                {
                    //get closest coordinate 
                    ProximityResult nearestCoord = GeometryEngine.NearestCoordinate(SelectionResult.ElementAt(0).Geometry, inputPt);

                    //get closest vertex 
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
                    if (freePointsAllowed) {
                        return inputPt;
                    } else { 
                        //line could not snap
                        return null;
                    }
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
        }


        private void WaitForEndPt()
        {
            MyMapView.GeoViewTapped += EndPt_Tapped;

            //show length of past run
            pathDrawingControls.Visibility = Visibility.Visible;

            UpdateUI_waitForEndPt();
        }

        private void ShowPastRunDist()
        {
            pastRunDistTxt.Text = lastRunDist.ToString() + "ft";
        }

        private async void UndoLastVertex(object sender, RoutedEventArgs e) {
            
            if (polyLineBuilder.Parts[0].PointCount > 1)
            {
                undoBtn_border.Background = darkenedWhiteButtonBrush;

                polyLineBuilder.Parts[0].RemovePoint(polyLineBuilder.Parts[0].PointCount - 1);
                currentRunGeom = polyLineBuilder.ToGeometry() as Polyline;
                UpdateRunGraphicAndLength();

                await Task.Delay(250);

                undoBtn_border.Background = normalWhiteButtonBrush;
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

            if (DrawnPathMatchesRunData(lineLength))
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

        private bool DrawnPathMatchesRunData(double lineLength)
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

            if (!Esri.ArcGISRuntime.Geometry.Geometry.IsNullOrEmpty(snappedPoint))
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
        private GenerateOfflineMapResult offlineMapJobResults = null;
        readonly string offlineMapsFolder = Path.Combine(Properties.Settings.Default.LogPath, "OfflineMapData");
        string currentOfflineMapdatafolder;
        bool currentMapIsOffline;

        private async void DownloadOfflineMap(object sender, RoutedEventArgs e)
        {
            if (MyMapView.Map.LoadStatus != LoadStatus.Loaded)
            {
                new UserMessageBox("No map loaded, so there's no map to take offline. MapLoadStatus: " + MyMapView.Map.LoadStatus.ToString(), "No Map Loaded").ShowDialog();

                UpdateUI_waitForMapChosen();
                return;
            }

            if (!InternetConnected())
            {
                StatusTxt.Text = "No internet connection. Reconnect to get maps.";
                StatusTxt.Background = System.Windows.Media.Brushes.Red;
                return;
            }

            try
            {
                progressIndicator.Visibility = Visibility.Visible;
                busyJobText.Text = "Downloading offline map... ";

                downloadMapBtn_border.Background = darkenedBlueButtonBrush;


                //area of interest set to the extent of the current map
                //Envelope areaOfInterest = MyMapView.Map.Item.Extent;
                Envelope areaOfInterest = MyMapView.VisibleArea.Extent; //set downloaded area to current view

                //set up our folder to hold offline map data
                currentOfflineMapdatafolder = Path.Combine(offlineMapsFolder, MyMapView.Map.Item.ItemId + "_" + portal.User.UserName);

                if (Directory.Exists(currentOfflineMapdatafolder))
                {
                    //unregister this map id before taking a new instance offline

                    Directory.Delete(currentOfflineMapdatafolder, true);
                }

                if (!Directory.Exists(currentOfflineMapdatafolder))
                {
                    Directory.CreateDirectory(currentOfflineMapdatafolder);
                }

                // Create an offline map task with the current (online) map.
                OfflineMapTask takeMapOfflineTask = await OfflineMapTask.CreateAsync(MyMapView.Map);

                // Create the default parameters for the task, pass in the area of interest.
                GenerateOfflineMapParameters parameters = await takeMapOfflineTask.CreateDefaultGenerateOfflineMapParametersAsync(areaOfInterest);

                // Create the job with the parameters and output location.
                _generateOfflineMapJob = takeMapOfflineTask.GenerateOfflineMap(parameters, currentOfflineMapdatafolder);

                // Handle the progress changed event for the job.
                _generateOfflineMapJob.ProgressChanged += OfflineMapJob_ProgressChanged;

                // Await the job to generate geodatabases, export tile packages, and create the mobile map package.
                offlineMapJobResults = await _generateOfflineMapJob.GetResultAsync();

                // Check for job failure (writing the output was denied, e.g.).
                if (_generateOfflineMapJob.Status != JobStatus.Succeeded)
                {
                    new UserMessageBox("Generate offline map package failed.", "Job Status", "error").ShowDialog();
                    progressIndicator.Visibility = Visibility.Collapsed;
                }

                // Check for errors with individual layers.
                if (offlineMapJobResults.LayerErrors.Any())
                {
                    // Build a string to show all layer errors.
                    System.Text.StringBuilder errorBuilder = new System.Text.StringBuilder();
                    foreach (KeyValuePair<Layer, Exception> layerError in offlineMapJobResults.LayerErrors)
                    {
                        errorBuilder.AppendLine(string.Format("{0} : {1}", layerError.Key.Id, layerError.Value.Message));
                    }

                    // Show layer errors.
                    string errorText = errorBuilder.ToString();
                    new UserMessageBox(errorText, "Error Loading One or More Layers", "error").ShowDialog();
                }

                // Display the offline map.
                MyMapView.Map = offlineMapJobResults.OfflineMap;

                // Apply the original viewpoint for the offline map.
                MyMapView.SetViewpoint(new Viewpoint(areaOfInterest));

                // Hide the "Take map offline" button.
                downloadMapBtn.Visibility = Visibility.Collapsed;


                // Show a message that the map is offline.

                StatusTxt.Text = "Map has been taken offline.";
                StatusTxt.Background = System.Windows.Media.Brushes.Transparent;

                currentMapIsOffline = true;

                UserMapsBox.IsEnabled = false;

                MyMapView.LocationDisplay.IsEnabled = false;

                MyMapView.LocationDisplay.AutoPanMode = LocationDisplayAutoPanMode.Off;

                MapSynced();



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
                new UserMessageBox("Map download was canceled", "").ShowDialog();
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

                downloadMapBtn_border.Background = normalBlueButtonBrush;
            }
        }

        OfflineMapSyncJob offlineMapSyncJob;
        private bool needMapSync;
        private string mapIdToSync;
        private async Task<bool> SyncOfflineMap(Map map, bool isTimed)
        {
            if (!needMapSync && isTimed)
            {
                new UserMessageBox("Map Does not need syncing.", "Sync Not Needed").ShowDialog();
                return false;
            }

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

                offlineMapSyncJob = task.SyncOfflineMap(parameters);
                offlineMapSyncJob.ProgressChanged += SyncOfflineMapJob_ProgressChanged;

                var results = await offlineMapSyncJob.GetResultAsync();

                var successful = false;

                // Check for job failure (writing the output was denied, e.g.).
                if (offlineMapSyncJob.Status == JobStatus.Succeeded)
                {
                    MapSynced();
                    lastSyncTime = DateTime.UtcNow;
                    successful = true;
                }
                else
                {
                    new UserMessageBox("Offline map sync failed. Job Status: " + offlineMapSyncJob.Status.ToString(), "Map Sync Failed", "error").ShowDialog();
                    progressIndicator.Visibility = Visibility.Collapsed;
                    successful = false;
                }

                if (results.HasErrors)
                {
                    new UserMessageBox("Layer errors with offline map sync", "Layer Errors", "error").ShowDialog();
                }

                return successful;

            }
            catch (TaskCanceledException)
            {
                //  task was canceled.
                new UserMessageBox("Syncing offline map was canceled", "Sync Cancelled").ShowDialog();
                return false;
            }
            catch (Exception ex)
            {
                // Exception while taking the map offline.
                new UserMessageBox(ex.Message, "Offline map sync error", "error").ShowDialog();
                return false;
            }
            finally
            {
                // Hide the activity indicator when the job is done.
                progressIndicator.Visibility = Visibility.Collapsed;
            }
        }

        private void MapSynced()
        {
            needMapSync = false;
            mapIdToSync = "";
            SyncBtn.IsEnabled = false;
            MapSyncStatus.Text = "Map Synced";
            MapSyncStatus.Background = System.Windows.Media.Brushes.PowderBlue;
        }
		

        private void MapNotSynced()
        {
            needMapSync = true;
            mapIdToSync = ((Esri.ArcGISRuntime.Portal.LocalItem)MyMapView.Map.Item).OriginalPortalItemId;
            SyncBtn.IsEnabled = true;
            MapSyncStatus.Text = "Map Not Synced";
            MapSyncStatus.Background = System.Windows.Media.Brushes.LightPink;
        }

        private async void SyncOfflineWaitingMaps()
        {
            if (!File.Exists(mapsToSyncFilePath)){
                return;
            }

            List<string> lines = File.ReadAllLines(mapsToSyncFilePath).ToList();

            for (int i = lines.Count-1; i >= 0; i--)
            {
                string line = lines.ElementAt(i);

                //line is structured as: originalmapId_username of user that owns these edits
                string[] parts = line.Split('_');

                if (parts.Length >= 2)
                {

                    //if part that corresponds to username matches the user that is logged in now, open that file as a map and sync it
                    if (parts[1].Equals(portal.User.UserName))
                    {
                        try
                        {
                            if (Directory.Exists(Path.Combine(offlineMapsFolder, line)))
                            {

                                MobileMapPackage offlineMapPackage = MobileMapPackage.OpenAsync(Path.Combine(offlineMapsFolder, line)).Result;
                                var successful = await SyncOfflineMap(offlineMapPackage.Maps.First(), false);
                                offlineMapPackage.Close();

                                if (successful)
                                {
                                    lines.RemoveAt(i);
                                }
                            }
                            else
                            {
                                string fileNotFound = lines.ElementAt(i) + "_fileNotFound";
                                lines.RemoveAt(i);
                                lines.Insert(i, fileNotFound);
                            }
                        }
                        catch
                        {
                            new UserMessageBox("MapId: " + line + " failed to sync from device. Check internet connection and try again. Alternatively, you can delete the folder at " + Path.Combine(Properties.Settings.Default.LogPath, offlineMapsFolder, parts[0]), "Syncing a waiting map failed", "error").ShowDialog();
                        }
                    }
                }
                else
                {
                    lines.RemoveAt(i);
                }
            }

            File.WriteAllLines(mapsToSyncFilePath, lines);
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
            if(_generateOfflineMapJob != null)
            {
                _generateOfflineMapJob.Cancel();
            }
            

            if(offlineMapSyncJob != null)
            {
                offlineMapSyncJob.Cancel();
            }
        }

        #endregion

        #region UI_functions

        BrushConverter converter;

        Brush darkenedWhiteButtonBrush;
        Brush darkenedBlueButtonBrush;
        Brush normalBlueButtonBrush;
        Brush normalWhiteButtonBrush;



        private void InitializeUI()
        {
            StatusTxt.Background = System.Windows.Media.Brushes.Red;
            chkbxPm.Background = System.Windows.Media.Brushes.Red;
            chkbxPm.IsChecked = false;

            converter = new BrushConverter();
            darkenedWhiteButtonBrush = (Brush)converter.ConvertFromString("#ffdddddd");
            darkenedBlueButtonBrush = (Brush)converter.ConvertFromString("#FF2f81b7");
            normalWhiteButtonBrush = (Brush)converter.ConvertFromString("#ffffffff");
            normalBlueButtonBrush = (Brush)converter.ConvertFromString("#ff5caade");

            UserMapsBox.IsEnabled = false;

            pmStart.IsEnabled = false;
            btnStop.IsEnabled = false;
            btnRetryPmConnect.IsEnabled = true;
            txtFName.IsEnabled = false;
            CommentBox.IsEnabled = false;
            btnVegetation.IsEnabled = false;
            btnTrippingHazard.IsEnabled = false;
            btnBrokenSidewalk.IsEnabled = false;
            btnOther.IsEnabled = false;
            pathDrawingControls.Visibility = Visibility.Collapsed;
            PostRunControlsPanel.Visibility = Visibility.Collapsed;
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
            pathDrawingControls.Visibility = Visibility.Collapsed;
            pmStart.IsEnabled = false;
            btnStop.IsEnabled = false;
            btnRetryPmConnect.IsEnabled = true;

            //fill the name of the run with a default name and allow user to change it
            txtFName.IsEnabled = true;
            CommentBox.IsEnabled = true;
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
            PostRunControlsPanel.Visibility = Visibility.Collapsed;
        }

        private void UpdateUI_waitForEndPt()
        {
            pmStart.IsEnabled = false;
            btnStop.IsEnabled = false;
            btnRetryPmConnect.IsEnabled = false;
            txtFName.IsEnabled = false;
            CommentBox.IsEnabled = true;
            btnVegetation.IsEnabled = false;
            btnTrippingHazard.IsEnabled = false;
            btnBrokenSidewalk.IsEnabled = false;
            btnOther.IsEnabled = false;
            StatusTxt.Text = "Choosing points for run path...";
            RunContentGrid.Visibility = Visibility.Collapsed;
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
            CommentBox.IsEnabled = true;
            btnVegetation.IsEnabled = false;
            btnTrippingHazard.IsEnabled = false;
            btnBrokenSidewalk.IsEnabled = false;
            btnOther.IsEnabled = false;
            RunContentGrid.Visibility = Visibility.Collapsed;
            PostRunControlsPanel.Visibility = Visibility.Visible;
            submitBtn.IsEnabled = true;
            StatusTxt.Background = System.Windows.Media.Brushes.Transparent;
            UserContentGrid.IsEnabled = false;
            ObstacleGrid.IsEnabled = false;
        }

        public void UpdateUI_RunInProgress()
        {
            pmStart.IsEnabled = false;
            btnStop.IsEnabled = true;
            txtFName.IsEnabled = false;
            CommentBox.IsEnabled = true;
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
            CommentBox.IsEnabled = true;
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
            CommentBox.IsEnabled = false;
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

        // - The Client ID for an app registered with the server
        private const string AppClientId = @"4PYwJ5MaXYfmMjss"; // our actual clientId

        private const string AppClientSecret = "81bf742b3e2f4d249d16557cc8e41ac9"; //our actual secret

        // - A URL for redirecting after a successful authorization (this must be a URL configured with the app).
        private const string OAuthRedirectUrl = @"my-ags-app://auth";

        ArcGISPortal portal;
        Uri profilePicUri;

        List<Map> availableMaps;

        
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
                        userNameBox.Text = portal.User.UserName;
                        userNameBox.Visibility = Visibility.Visible;
                        disconnectedFlagBox.Visibility = Visibility.Collapsed;

                        profilePicUri = portal.User.ThumbnailUri;
                        
                        profilePic.Source = profilePicUri != null ? new BitmapImage(profilePicUri) : null;
                        profilePic.Visibility = Visibility.Visible;

                        loginBtn.Content = "Log Out";
                        loginBtn.Click -= LoginBtn_Click;
                        loginBtn.Click += LogoutBtn_Click;

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
                    RedirectUri = new Uri(OAuthRedirectUrl),
                    
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
            Credential credential;

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
                },

            };

            try
            {
                var crd = await AuthenticationManager.Current.GetCredentialAsync(cri, false);

                AuthenticationManager.Current.AddCredential(crd);
                
                return true;
            }
            catch
            {
                
                UpdateUI_waitForLogin();
                return false;
            }
        }

        private void SignOut()
        {
            // clear the credential for the server from the IdenityManager (if it exists)
            Task removeCreds =  AuthenticationManager.Current.RemoveAndRevokeAllCredentialsAsync();
            removeCreds.Wait();

            //reset the portal and map
            portal = null;
            MyMapView.Map = null;

            if (offlineMapJobResults != null)
            {
                offlineMapJobResults.MobileMapPackage.Close();
            }

            //hide the take map offline btn
            downloadMapBtn.Visibility = Visibility.Collapsed;

            userNameBox.Text = "";
            userNameBox.Visibility = Visibility.Collapsed;
            disconnectedFlagBox.Visibility = Visibility.Collapsed;

            profilePic.Source = null;
            profilePic.Visibility = Visibility.Collapsed;

            //clear the maps dropdown
            ClearUserMapsBox();

            MapSynced();

            MapSyncStatus.Text = "";
            MapSyncStatus.Background = System.Windows.Media.Brushes.Transparent;

            DiscardUser();

            loginBtn.Content = "Log In";
            loginBtn.Click -= LogoutBtn_Click;
            loginBtn.Click += LoginBtn_Click;

            StatusTxt.Text = "Log in to load maps.";
            StatusTxt.Background = Brushes.Transparent;
        }

        private void ClearUserMapsBox()
        {
            for (int i = UserMapsBox.Items.Count - 1; i > 0 ; i--)
            {
                UserMapsBox.Items.RemoveAt(i);
            }
        }

        private void DiscardUser()
        {
            if (File.Exists(keptUserFilePath))
            {
                File.Delete(keptUserFilePath);
            }
        }


        private async void GetWebMaps()
        {

            if (!InternetConnected())
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

                List<PortalItem> foundItems = new List<PortalItem>();
                List<Map> foundMaps = new List<Map>();

                //get all webmaps that belong to the user this portal was created with
                foreach (PortalGroup portalGroup in groups)
                {
                    var parameters = PortalQueryParameters.CreateForItemsOfTypeInGroup(PortalItemType.WebMap, portalGroup.GroupId);
                    parameters.Limit = 50;

                    var portalItems = (await portal.FindItemsAsync(parameters)).Results;

                    foundItems.AddRange(portalItems);
                }

                if (foundItems.Count > 0)
                {
                    
                    foreach (PortalItem item in foundItems)
                    {
                        foundMaps.Add(new Map(item));
                    }

                    //show the retrieved maps in the dropdown box
                    PopulateMapDropdown(foundMaps, true);
                }
                else
                {
                    StatusTxt.Text = "No maps found for user. Log in to a different account or try again.";
                    StatusTxt.Background = System.Windows.Media.Brushes.Yellow;
                }
            }
            catch
            {
                throw;
            }
        }

        bool dropDownMapsAreOnline;

        private void PopulateMapDropdown(List<Map> maps, bool onlineMaps)
        {
            dropDownMapsAreOnline = onlineMaps;

            availableMaps = maps;

            ClearUserMapsBox();

            foreach (Map map in availableMaps)
            {
                ComboBoxItem availableMap = new ComboBoxItem
                {
                    Content = map.Item.Title,
                    Tag = map,
                    Padding = new Thickness(10)
                };

                UserMapsBox.Items.Add(availableMap);
            }

            UpdateUI_waitForMapChosen();
        }

        private bool SignInKeptUser()
        {
            if (!File.Exists(mapsToSyncFilePath) || !File.Exists(keptUserFilePath))
            {
                return false;
            }

            string userName = File.ReadAllLines(keptUserFilePath).First();

            if (String.IsNullOrWhiteSpace(userName) || String.IsNullOrEmpty(userName))
            {
                return false;
            }
            else
            {
                userNameBox.Text = userName;
                userNameBox.Visibility = Visibility.Visible;
                disconnectedFlagBox.Visibility = Visibility.Visible;

                loginBtn.Content = "Log Out";
                loginBtn.Click -= LoginBtn_Click;
                loginBtn.Click += LogoutBtn_Click;

                return true;
            }
        }

        private async Task<bool> LoadInOfflineMaps()
        {
            bool userFound = SignInKeptUser();

            if (!userFound)
            {
                return false;
            }

            string userName = this.userNameBox.Text;

            string[] offlineMaps = Directory.GetDirectories(offlineMapsFolder).Select(Path.GetFileName).ToArray();

            List<Map> foundMaps = new List<Map>();

            for (int i = 0; i < offlineMaps.Length; i++)
            {
                string mmpkName = offlineMaps[i];

                string[] parts = mmpkName.Split('_');

                //check to make sure it's a valid line first
                if (parts.Length >= 2)
                {
                    //if part that corresponds to username matches the user that was kept, open that file as a map
                    if (parts[1].Equals(userName))
                    {
                        try
                        {
                            string folderPath = Path.Combine(offlineMapsFolder, mmpkName);
                            //check that folder has an .info file to make sure it can be loaded without an error
                            var foundPackageInfoFiles = Directory.GetFiles(folderPath, "*.info");
                            if (foundPackageInfoFiles.Length > 0)
                            {
                                //turn the folder into map
                                MobileMapPackage offlineMapPackage = await MobileMapPackage.OpenAsync(Path.Combine(offlineMapsFolder, mmpkName));
                                await offlineMapPackage.LoadAsync();
                                foundMaps.Add(offlineMapPackage.Maps.First());
                                offlineMapPackage.Close();
                            }
                            else
                            {
                                Directory.Delete(folderPath, true);
                            }
                        }
                        catch(Exception e)
                        {
                            new UserMessageBox("MapId: " + parts[0] + " failed to load from device. Error: " + e.Message + " Alternatively, you can delete the folder at " + Path.Combine(offlineMapsFolder, mmpkName) , "Loading Offline Map Failed", "error").ShowDialog();
                        }
                    }
                }
            }

            //add maps to dropdown if any are found
            if(foundMaps.Count > 0)
            {
                PopulateMapDropdown(foundMaps, false);
                return true;
            }
            else
            {
              new UserMessageBox("No offline maps available", "No offline maps avialable for the logged in user", "error").ShowDialog();
                return false;
                
            }

        }


        public bool InternetConnected()
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
                lastInternetLostConnectionTime = DateTime.UtcNow;
                Console.WriteLine("no internet connection");
                return false;
            }
        }


        #endregion


        private async void ReviewBtn_Click(object sender, RoutedEventArgs e)
        {
            List<String> imgPaths = Directory.GetFiles(sensors.directory, "*.png").ToList();
            ReviewImagesWindow r = new ReviewImagesWindow(imgPaths);

            reviewBtn_border.Background = darkenedWhiteButtonBrush;

            r.ShowDialog();

            await Task.Delay(250);
            reviewBtn_border.Background = normalWhiteButtonBrush;
        }

        private async void LoginBtn_Click(object sender, RoutedEventArgs e)
        { 
            ConnectAndGetMaps();

            loginBtn_border.Background = darkenedBlueButtonBrush;
            await Task.Delay(200);
            loginBtn_border.Background = normalBlueButtonBrush;
        }

        private async void LogoutBtn_Click(object sender, RoutedEventArgs e)
        {
            loginBtn_border.Background = darkenedBlueButtonBrush;

            if (MyMapView.Map != null)
            {
                    string needSyncMsg = "Current map has not been synced. Logging out will not update the online ArcGIS map with the current map on this device. Run data will still be available in the file system.";
                    string dontNeedSyncMsg = "If you log out, the currently selected map will not be available until you log back in.";
                    string msg = (needMapSync ? needSyncMsg : dontNeedSyncMsg) + " Are you sure you want to log out now?";
                    bool result = (bool)new UserMessageBox(msg, "Log out?", "yesno").ShowDialog();

                    if (result)
                    {
                        //user is okay with logging out. Save info about the unsynced map
                        if(needMapSync)
                            AddToMapsToSync();
                    }
                    else
                    {
                        // If user doesn't want to log out, just return
                        return;
                    }
            }
            else
            {
                bool result = (bool)new UserMessageBox("Are you sure you want to log out?","Log out?", "yesno").ShowDialog();

                if (!result)
                {
                    // If user doesn't want to log out, just return
                    return;
                }
            }

            SignOut();

            await Task.Delay(200);
            loginBtn_border.Background = normalBlueButtonBrush;
        }

        private async void Sync_Clicked(object sender, RoutedEventArgs e)
        {
            SyncBtn_border.Background = darkenedBlueButtonBrush;

            if (dropDownMapsAreOnline)
            {
                SyncOfflineMap(MyMapView.Map, false);
            }
            else
            {
                new UserMessageBox("Reconnect to the internet and log in again to be able to sync.", "Unable to Sync Map").ShowDialog();
            }

            await Task.Delay(250);
            SyncBtn_border.Background = normalBlueButtonBrush;
        }

        #region timedMapSync

        readonly int syncWaitPeriod = 10; //minutes
        DateTime lastSubmitTime;
        DateTime lastStartTime;
        DateTime lastInternetLostConnectionTime;
        DateTime lastSyncTime;

        private void StartSyncTimer()
        {
            lastSubmitTime = DateTime.UtcNow;
            lastInternetLostConnectionTime = DateTime.UtcNow;
            lastSyncTime = DateTime.UtcNow;
            lastStartTime = DateTime.UtcNow;

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
            timer.Tick += TimedSync;
            timer.Start();
        }

        private async void TimedSync(object sender, EventArgs e)
        {
            DateTime now = DateTime.UtcNow;


            if (!InternetConnected() || !needMapSync)
            {
                return;
            }


            if (lastInternetLostConnectionTime.AddMinutes(syncWaitPeriod) > now || lastStartTime.AddMinutes(syncWaitPeriod) > now)
            {
                return;
            }

            if (lastSubmitTime.AddMinutes(syncWaitPeriod) > now || lastSyncTime.AddMinutes(syncWaitPeriod) > now)
            {
                return;
            }

            //if we've made it through all the checks
            await SyncOfflineMap(MyMapView.Map, true);
        }


        #endregion

    }

}
