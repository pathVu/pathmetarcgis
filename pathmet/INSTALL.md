# Installing pathmet.exe on the computer

## Sensors

Connect the IMU and QSB, and, using Device Manager, take note of the COM ports they get assigned. You might need to "Update Driver" on the QSB to get it to show up as a COM port.

Connect the laser to the COM1 port on the back of the computer.

## Camera

Install OpenCV and set up the OPENCV_DIR variable as shown in the instructions.

https://docs.opencv.org/2.4/doc/tutorials/introduction/windows_install/windows_install.html

I used C:\opencv as the install directory, which makes the setx command:

```
setx -m OPENCV_DIR C:opencv\Build\x64\vc14
```

pathmet.exe uses dynamic-link libraries, so to run it you will need to add the %OPENCV_DIR%\bin directory to the system path. I did this at the user level, which added another step later on when configuring the service.

OpenCV will detect the camera automatically, you just have to plug it in and let Windows install drivers.

## Install the Visual Studio 2015 Runtime

https://www.microsoft.com/en-us/download/details.aspx?id=48145

## pathmet.exe and pathmet.cfg

Create a C:\pathmet directory, and copy pathmet.exe to it.

Create a pathmet.cfg file. This configures the COM ports to use for the sensors. It should look like this, but using the COM ports you noted when connecting sensors:

```
imu=COM3
encoder=COM4
laser=COM1
```

At this stage, you should be able to run pathmet.exe from the command line. It will display the sensor configuration.

## Configure the network

The tablet executable tries to connect to 10.1.1.1 on port 10101 in order to communicate with the sensors server. So, configure one of the Ethernet ports to use a hard-coded address of 10.1.1.1.

Run pathmet.exe with the tablet connected to that port (you'll have to configure the tablet to use 10.1.1.2 on its USB->ethernet), and the tablet should eventually connect and enable the start button.

## Configure pathmet.exe to start up automatically as a service

Download nssm from https://nssm.cc/

Run `nssm install pathmet` to open the GUI. Choose the pathmet executable, and C:\pathmet as the startup directory. Under I/O you can redirect the stderr output to a log file (I used C:\pathmet.err). Since I didn't configure the PATH variable to work system-wide (I only configured it for my user) I needed to add a PATH=%OPENCV_DIR%\bin line to the Environment tab.

Install the service. Now it should appear in the Windows services. Start the service, and set it to start up automatically.

## Configure the Power Button

I went into the Windows settings and changed the power button from "Sleep" to "Power Off". Once you do that, power it off with the button, and power it back on, to make sure the service runs automatically and the tablet connects.

## Runs

Runs are stored in the C:\pathmet directory, under a folder with the run name.



