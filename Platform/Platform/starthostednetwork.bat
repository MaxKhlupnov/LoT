:: start the hosted network
netsh wlan set hostednetwork mode=allow ssid=setup key=helloworld 
netsh wlan start hostednetwork
