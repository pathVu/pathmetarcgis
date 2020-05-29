using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace PathMet_V2
{
    class RunGeometryEditor
    {
        PolylineBuilder plbuilder;
        Graphic runGraphic;

        public RunGeometryEditor(GraphicsOverlay runsOverlay, Esri.ArcGISRuntime.Data.Feature runFeature)
        {
            // create a PolylineBuilder for working with road geometry
            // set initial state of the builder based on the Polyline passed in
            var roadPolyline = runFeature.Geometry as Polyline;
            this.plbuilder = new PolylineBuilder(roadPolyline);

            // create a graphic to show the run geometry
            var lineSymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, Color.FromArgb(0, 255, 0, 0), 1.0);
            this.runGraphic = new Graphic(roadPolyline, lineSymbol);

            // display the graphic in a graphics overlay in the map view
            runsOverlay.Graphics.Add(runGraphic);
        }

        public void AddPointToEnd(MapPoint point)
        {
            // add a point to the end of the last part in the polyline
            this.plbuilder.AddPoint(point);
        }

        // a read-only property to get the current Polyline stored in the builder
        public Polyline RunGeometry
        {
            get { return this.plbuilder.ToGeometry(); }
        }

        // update the line graphic with the geometry currently stored in the polyline builder
        public void UpdateRunGraphic()
        {
            this.runGraphic.Geometry = this.plbuilder.ToGeometry();
        }
    }
}
