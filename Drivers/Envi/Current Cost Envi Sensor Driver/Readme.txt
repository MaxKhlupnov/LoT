Steps to get a current cost envi sensor (http://www.amazon.com/Current-Cost-Envi-Energy-Monitor/dp/B002FVJA5I)
working with a hub. 
It comes with two components, the actual sensor (black box) and the display 
component.
 
1. Take sensor out of the box, and add batteries. The display 
component which receives readings from the sensor and talks to 
the hub via USB is powered through an adapter. 
 
(Skip this is if you're not using a brand new sensor)
2. Pair the sensor and the display. Using a pen press the little red
button on the sensor and keep it pressed for 9-10 seconds. On releasing, 
the red LED on the sensor will flash rapidly for a minute. (If not, please 
try again.) While the LED is flashing press the down button (V) on the display 
until the LED on the display flashes. When you release the display will 
tune itself and then pair with the sensor.
[This is a one-time per lifetime setup]
 
3. On your Win7 or Win8 machine install Hub\Tools\PL2303_Prolific_GPS_AllInOne_1013.exe. Ensure 
that the USB cable is plugged out while doing this.
 
4. Now plug in the USB and connect the display to power. Now run the 
HomeOS.Hub.Drivers.Envi module. It will connect to the display and 
poll power readings in Watts from the sensor. The readings are displayed 
on the console (logger), and any modules subscribed to the driver's port 
("envi") will be notified.