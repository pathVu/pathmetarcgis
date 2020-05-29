using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Management;
using System.IO;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace PathMet
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            sensors = new SerialSensors(Properties.Settings.Default.SensorsPort);
            sensors.UpdateEvent += OnUpdate;
            UpdateSensors();

            sensors.ExistsEvent += () =>
            {
                this.Invoke((MethodInvoker)(() => { FileExists(); }));
            };

            sensors.SummaryEvent += (double laser, double encoder) =>
            {
                this.Invoke((MethodInvoker)(() => { Summary(laser, encoder); }));
            };

            this.FormClosing += delegate(object sender, FormClosingEventArgs e) {
                sensors.UpdateEvent -= OnUpdate;
                sensors.Dispose();
            };
        }

        private void OnUpdate()
        {
            this.BeginInvoke((MethodInvoker)(() => { UpdateSensors(); }));
        }

        private ISensors sensors;

        private void FileExists()
        {
            MessageBox.Show("That run exists. Please choose a different name.", "File Exists", MessageBoxButtons.OK);
        }

        private void Summary(double laser, double encoder)
        {
            MessageBox.Show(String.Format("Laser: {0:0.000} in\nEncoder: {1:0.0} ft", laser, encoder / 12.0), "Summary", MessageBoxButtons.OK);
        }

        private void UpdateSensors()
        {
            Console.WriteLine("UpdateSensors called.");
            if (sensors.Connected)
            {
                chkbxPm.BackColor = Color.LightGreen;
                chkbxPm.Checked = true;

                if (sensors.Sampling)
                {
                    pmStart.Enabled = false;
                    btnStop.Enabled = true;
                    txtFName.Enabled = false;
                }
                else
                {
                    pmStart.Enabled = true;
                    btnStop.Enabled = false;
                    txtFName.Enabled = true;
                }

                if (sensors.LaserStatus == SensorStatus.OK)
                {
                    chkbxL.BackColor = Color.LightGreen;
                    chkbxL.Checked = true;
                }
                else
                {
                    chkbxL.BackColor = Color.Red;
                    chkbxL.Checked = false;
                }

                if (sensors.CameraStatus == SensorStatus.OK)
                {
                    chkbxC.BackColor = Color.LightGreen;
                    chkbxC.Checked = true;
                }
                else
                {
                    chkbxC.BackColor = Color.Red;
                    chkbxC.Checked = false;
                }

                if (sensors.IMUStatus == SensorStatus.OK)
                {
                    chkbxI.BackColor = Color.LightGreen;
                    chkbxI.Checked = true;
                }
                else
                {
                    chkbxI.BackColor = Color.Red;
                    chkbxI.Checked = false;
                }

                if (sensors.EncoderStatus == SensorStatus.OK)
                {
                    chkbxE.BackColor = Color.LightGreen;
                    chkbxE.Checked = true;
                }
                else
                {
                    chkbxE.BackColor = Color.Red;
                    chkbxE.Checked = false;
                }

                btnRestart.Enabled = true;
            }
            else
            {
                chkbxPm.BackColor = Color.Red;
                chkbxPm.Checked = false;

                pmStart.Enabled = false;
                btnStop.Enabled = false;
                txtFName.Enabled = false;

                chkbxL.BackColor = Color.Transparent;
                chkbxL.Checked = false;
                chkbxC.BackColor = Color.Transparent;
                chkbxC.Checked = false;
                chkbxI.BackColor = Color.Transparent;
                chkbxI.Checked = false;
                chkbxE.BackColor = Color.Transparent;
                chkbxE.Checked = false;

                btnRestart.Enabled = false;
            }
        }

        private void OnStop(object sender, EventArgs e)
        {
            if (!sensors.Connected)
            {
                return;
            }
            // disable everything; the sensor will enable it when ready
            txtFName.Enabled = false;
            btnStop.Enabled = false;
            pmStart.Enabled = false;

            sensors.Stop();

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

        private void OnClick(object sender, EventArgs e)
        {
            if (!sensors.Connected)
            {
                return;
            }
            // disable everything; the sensor will enable it when ready
            txtFName.Enabled = false;
            btnStop.Enabled = false;
            pmStart.Enabled = false;

            string name = txtFName.Text;
            if (name == "")
            {
                name = DateTime.Now.ToString("yyyy-MM-dd-HHmmss");
            }

            sensors.Start(name);
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

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void chkbxPm_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void elementHost1_ChildChanged(object sender, System.Windows.Forms.Integration.ChildChangedEventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }

}
