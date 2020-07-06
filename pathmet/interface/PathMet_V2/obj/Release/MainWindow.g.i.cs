﻿#pragma checksum "..\..\MainWindow.xaml" "{8829d00f-11b8-4213-878b-770e8597ac16}" "CF103209AE18FC790D3BF48681729956EE3F6C01950F23F6FBD15AF59911E34C"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using Esri.ArcGISRuntime;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Location;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using PathMet_V2;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;


namespace PathMet_V2 {
    
    
    /// <summary>
    /// MainWindow
    /// </summary>
    public partial class MainWindow : System.Windows.Window, System.Windows.Markup.IComponentConnector {
        
        
        #line 24 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal Esri.ArcGISRuntime.UI.Controls.MapView MyMapView;
        
        #line default
        #line hidden
        
        
        #line 25 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock StatusTxt;
        
        #line default
        #line hidden
        
        
        #line 30 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button downloadMapBtn;
        
        #line default
        #line hidden
        
        
        #line 34 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.StackPanel pathDrawingControls;
        
        #line default
        #line hidden
        
        
        #line 36 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Border drawnLineStatusIndicator;
        
        #line default
        #line hidden
        
        
        #line 56 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock pastRunDistTxt;
        
        #line default
        #line hidden
        
        
        #line 59 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock drawnRunDistTxt;
        
        #line default
        #line hidden
        
        
        #line 68 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button undoBtn;
        
        #line default
        #line hidden
        
        
        #line 71 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Grid progressIndicator;
        
        #line default
        #line hidden
        
        
        #line 80 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Documents.Run busyJobText;
        
        #line default
        #line hidden
        
        
        #line 81 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Documents.Run Percentage;
        
        #line default
        #line hidden
        
        
        #line 83 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ProgressBar progressBar;
        
        #line default
        #line hidden
        
        
        #line 92 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button CancelJobButton;
        
        #line default
        #line hidden
        
        
        #line 102 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Grid FullControlPanel;
        
        #line default
        #line hidden
        
        
        #line 109 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Grid UserContentGrid;
        
        #line default
        #line hidden
        
        
        #line 116 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock userName;
        
        #line default
        #line hidden
        
        
        #line 117 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Image profilePic;
        
        #line default
        #line hidden
        
        
        #line 122 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button loginBtn;
        
        #line default
        #line hidden
        
        
        #line 126 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock PathMetTitle;
        
        #line default
        #line hidden
        
        
        #line 131 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Grid MapSyncInfoContainer;
        
        #line default
        #line hidden
        
        
        #line 137 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ComboBox UserMapsBox;
        
        #line default
        #line hidden
        
        
        #line 140 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock MapSyncStatus;
        
        #line default
        #line hidden
        
        
        #line 145 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button SyncBtn;
        
        #line default
        #line hidden
        
        
        #line 154 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Grid RunContentGrid;
        
        #line default
        #line hidden
        
        
        #line 165 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Grid sensorsBox;
        
        #line default
        #line hidden
        
        
        #line 173 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.CheckBox chkbxPm;
        
        #line default
        #line hidden
        
        
        #line 174 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.CheckBox chkbxL;
        
        #line default
        #line hidden
        
        
        #line 175 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.CheckBox chkbxC;
        
        #line default
        #line hidden
        
        
        #line 176 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.CheckBox chkbxI;
        
        #line default
        #line hidden
        
        
        #line 177 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.CheckBox chkbxE;
        
        #line default
        #line hidden
        
        
        #line 190 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox txtFName;
        
        #line default
        #line hidden
        
        
        #line 199 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Grid RunControlsGrid;
        
        #line default
        #line hidden
        
        
        #line 205 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button pmStart;
        
        #line default
        #line hidden
        
        
        #line 216 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button btnStop;
        
        #line default
        #line hidden
        
        
        #line 232 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button btnRetryPmConnect;
        
        #line default
        #line hidden
        
        
        #line 237 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Grid RunEventsPanel;
        
        #line default
        #line hidden
        
        
        #line 244 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Grid ObstacleGrid;
        
        #line default
        #line hidden
        
        
        #line 252 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Border btnTrippingHazard_Container;
        
        #line default
        #line hidden
        
        
        #line 256 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button btnTrippingHazard;
        
        #line default
        #line hidden
        
        
        #line 267 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Border btnBrokenSidewalk_Container;
        
        #line default
        #line hidden
        
        
        #line 271 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button btnBrokenSidewalk;
        
        #line default
        #line hidden
        
        
        #line 282 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Border btnVegetation_Container;
        
        #line default
        #line hidden
        
        
        #line 286 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button btnVegetation;
        
        #line default
        #line hidden
        
        
        #line 297 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Border btnOther_Container;
        
        #line default
        #line hidden
        
        
        #line 301 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button btnOther;
        
        #line default
        #line hidden
        
        
        #line 323 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox CommentBox;
        
        #line default
        #line hidden
        
        
        #line 331 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.StackPanel PostRunControlsPanel;
        
        #line default
        #line hidden
        
        
        #line 338 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button reviewBtn;
        
        #line default
        #line hidden
        
        
        #line 339 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button discardBtn;
        
        #line default
        #line hidden
        
        
        #line 341 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button submitBtn;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/PathMet_V2;component/mainwindow.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\MainWindow.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            
            #line 13 "..\..\MainWindow.xaml"
            ((PathMet_V2.MainWindow)(target)).Closing += new System.ComponentModel.CancelEventHandler(this.Window_Closing);
            
            #line default
            #line hidden
            return;
            case 2:
            this.MyMapView = ((Esri.ArcGISRuntime.UI.Controls.MapView)(target));
            return;
            case 3:
            this.StatusTxt = ((System.Windows.Controls.TextBlock)(target));
            return;
            case 4:
            this.downloadMapBtn = ((System.Windows.Controls.Button)(target));
            
            #line 30 "..\..\MainWindow.xaml"
            this.downloadMapBtn.Click += new System.Windows.RoutedEventHandler(this.downloadOfflineMap);
            
            #line default
            #line hidden
            return;
            case 5:
            this.pathDrawingControls = ((System.Windows.Controls.StackPanel)(target));
            return;
            case 6:
            this.drawnLineStatusIndicator = ((System.Windows.Controls.Border)(target));
            return;
            case 7:
            this.pastRunDistTxt = ((System.Windows.Controls.TextBlock)(target));
            return;
            case 8:
            this.drawnRunDistTxt = ((System.Windows.Controls.TextBlock)(target));
            return;
            case 9:
            this.undoBtn = ((System.Windows.Controls.Button)(target));
            
            #line 68 "..\..\MainWindow.xaml"
            this.undoBtn.Click += new System.Windows.RoutedEventHandler(this.UndoLastVertex);
            
            #line default
            #line hidden
            return;
            case 10:
            this.progressIndicator = ((System.Windows.Controls.Grid)(target));
            return;
            case 11:
            this.busyJobText = ((System.Windows.Documents.Run)(target));
            return;
            case 12:
            this.Percentage = ((System.Windows.Documents.Run)(target));
            return;
            case 13:
            this.progressBar = ((System.Windows.Controls.ProgressBar)(target));
            return;
            case 14:
            this.CancelJobButton = ((System.Windows.Controls.Button)(target));
            
            #line 95 "..\..\MainWindow.xaml"
            this.CancelJobButton.Click += new System.Windows.RoutedEventHandler(this.CancelJobButton_Click);
            
            #line default
            #line hidden
            return;
            case 15:
            this.FullControlPanel = ((System.Windows.Controls.Grid)(target));
            return;
            case 16:
            this.UserContentGrid = ((System.Windows.Controls.Grid)(target));
            return;
            case 17:
            this.userName = ((System.Windows.Controls.TextBlock)(target));
            return;
            case 18:
            this.profilePic = ((System.Windows.Controls.Image)(target));
            return;
            case 19:
            this.loginBtn = ((System.Windows.Controls.Button)(target));
            
            #line 122 "..\..\MainWindow.xaml"
            this.loginBtn.Click += new System.Windows.RoutedEventHandler(this.loginBtn_Click);
            
            #line default
            #line hidden
            return;
            case 20:
            this.PathMetTitle = ((System.Windows.Controls.TextBlock)(target));
            return;
            case 21:
            this.MapSyncInfoContainer = ((System.Windows.Controls.Grid)(target));
            return;
            case 22:
            this.UserMapsBox = ((System.Windows.Controls.ComboBox)(target));
            
            #line 137 "..\..\MainWindow.xaml"
            this.UserMapsBox.SelectionChanged += new System.Windows.Controls.SelectionChangedEventHandler(this.MapChosen);
            
            #line default
            #line hidden
            return;
            case 23:
            this.MapSyncStatus = ((System.Windows.Controls.TextBlock)(target));
            return;
            case 24:
            this.SyncBtn = ((System.Windows.Controls.Button)(target));
            
            #line 145 "..\..\MainWindow.xaml"
            this.SyncBtn.Click += new System.Windows.RoutedEventHandler(this.Sync_Clicked);
            
            #line default
            #line hidden
            return;
            case 25:
            this.RunContentGrid = ((System.Windows.Controls.Grid)(target));
            return;
            case 26:
            this.sensorsBox = ((System.Windows.Controls.Grid)(target));
            return;
            case 27:
            this.chkbxPm = ((System.Windows.Controls.CheckBox)(target));
            return;
            case 28:
            this.chkbxL = ((System.Windows.Controls.CheckBox)(target));
            return;
            case 29:
            this.chkbxC = ((System.Windows.Controls.CheckBox)(target));
            return;
            case 30:
            this.chkbxI = ((System.Windows.Controls.CheckBox)(target));
            return;
            case 31:
            this.chkbxE = ((System.Windows.Controls.CheckBox)(target));
            return;
            case 32:
            this.txtFName = ((System.Windows.Controls.TextBox)(target));
            return;
            case 33:
            this.RunControlsGrid = ((System.Windows.Controls.Grid)(target));
            return;
            case 34:
            this.pmStart = ((System.Windows.Controls.Button)(target));
            
            #line 205 "..\..\MainWindow.xaml"
            this.pmStart.Click += new System.Windows.RoutedEventHandler(this.OnStart);
            
            #line default
            #line hidden
            return;
            case 35:
            this.btnStop = ((System.Windows.Controls.Button)(target));
            
            #line 216 "..\..\MainWindow.xaml"
            this.btnStop.Click += new System.Windows.RoutedEventHandler(this.OnStop);
            
            #line default
            #line hidden
            return;
            case 36:
            this.btnRetryPmConnect = ((System.Windows.Controls.Button)(target));
            
            #line 232 "..\..\MainWindow.xaml"
            this.btnRetryPmConnect.Click += new System.Windows.RoutedEventHandler(this.retryPmConnection_Click);
            
            #line default
            #line hidden
            return;
            case 37:
            this.RunEventsPanel = ((System.Windows.Controls.Grid)(target));
            return;
            case 38:
            this.ObstacleGrid = ((System.Windows.Controls.Grid)(target));
            return;
            case 39:
            this.btnTrippingHazard_Container = ((System.Windows.Controls.Border)(target));
            return;
            case 40:
            this.btnTrippingHazard = ((System.Windows.Controls.Button)(target));
            
            #line 256 "..\..\MainWindow.xaml"
            this.btnTrippingHazard.Click += new System.Windows.RoutedEventHandler(this.btnTrippingHazard_Click);
            
            #line default
            #line hidden
            return;
            case 41:
            this.btnBrokenSidewalk_Container = ((System.Windows.Controls.Border)(target));
            return;
            case 42:
            this.btnBrokenSidewalk = ((System.Windows.Controls.Button)(target));
            
            #line 271 "..\..\MainWindow.xaml"
            this.btnBrokenSidewalk.Click += new System.Windows.RoutedEventHandler(this.btnBrokenSidewalk_Click);
            
            #line default
            #line hidden
            return;
            case 43:
            this.btnVegetation_Container = ((System.Windows.Controls.Border)(target));
            return;
            case 44:
            this.btnVegetation = ((System.Windows.Controls.Button)(target));
            
            #line 286 "..\..\MainWindow.xaml"
            this.btnVegetation.Click += new System.Windows.RoutedEventHandler(this.btnVegetation_Click);
            
            #line default
            #line hidden
            return;
            case 45:
            this.btnOther_Container = ((System.Windows.Controls.Border)(target));
            return;
            case 46:
            this.btnOther = ((System.Windows.Controls.Button)(target));
            
            #line 301 "..\..\MainWindow.xaml"
            this.btnOther.Click += new System.Windows.RoutedEventHandler(this.btnOther_Click);
            
            #line default
            #line hidden
            return;
            case 47:
            this.CommentBox = ((System.Windows.Controls.TextBox)(target));
            return;
            case 48:
            this.PostRunControlsPanel = ((System.Windows.Controls.StackPanel)(target));
            return;
            case 49:
            this.reviewBtn = ((System.Windows.Controls.Button)(target));
            
            #line 338 "..\..\MainWindow.xaml"
            this.reviewBtn.Click += new System.Windows.RoutedEventHandler(this.reviewBtn_Click);
            
            #line default
            #line hidden
            return;
            case 50:
            this.discardBtn = ((System.Windows.Controls.Button)(target));
            
            #line 339 "..\..\MainWindow.xaml"
            this.discardBtn.Click += new System.Windows.RoutedEventHandler(this.onDiscard);
            
            #line default
            #line hidden
            return;
            case 51:
            this.submitBtn = ((System.Windows.Controls.Button)(target));
            
            #line 341 "..\..\MainWindow.xaml"
            this.submitBtn.Click += new System.Windows.RoutedEventHandler(this.onSubmit);
            
            #line default
            #line hidden
            return;
            }
            this._contentLoaded = true;
        }
    }
}

