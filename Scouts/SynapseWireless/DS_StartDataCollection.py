"""
Start data collection for the doorway sensors.
"""
import subprocess
import socket
import select

loggers = []
devices = {}
openPorts = range(8410,8431)

addr = 'localhost'
scoutResponseAddress = (addr,8401)
deviceHeartBeatAddress = (addr,8402)
scoutRequestsAddress = (addr,8400)
driverRequestsAddress = (addr,8403)
dataMultiplexingAddress = (addr,8404)
driverResponseAddress = (addr,8405)

dataSendingSock = socket.socket(socket.AF_INET,socket.SOCK_DGRAM)
responseSock = socket.socket(socket.AF_INET,socket.SOCK_DGRAM)

debugAddress = (addr,8499)
debugSock = socket.socket(socket.AF_INET,socket.SOCK_DGRAM)

def startLoggers(numberOfLoggers):
    for i in range(1,numberOfLoggers+1):
        loggers.append(subprocess.Popen([r'C:\python27\python.exe','DS_logger.py','DS_logger'+str(i)]))

def killLoggers():
    for logger in loggers:
        logger.kill()

def respondToScout():
    #filter out old devices in this step
    for device in devices.keys():
        responseSock.sendto(device,scoutResponseAddress)

def confirmDevice(device):
    device = "Doorjamb "+str(device)
    if device not in devices.keys():
        devices[device] = openPorts[0] #get unique ID info or port
        openPorts.remove(openPorts[0])
        print device

def forwardDeviceData(deviceID,data):
    deviceID = "Doorjamb "+str(deviceID)
    #debugSock.sendto("data forward"+str(deviceID)+str(devices),debugAddress)
    if deviceID in devices.keys():
        #if data.split(' ')[0] == "IR":
        #    debugSock.sendto("data forward"+str(deviceID)+" "+data,debugAddress)
        dataSendingSock.sendto(data,(addr,devices[deviceID]))

def respondToDriver(deviceID):
    #debugSock.sendto("got driver request for: "+str(deviceID),debugAddress)
    #debugSock.sendto("sending driver response", debugAddress)
    #multiple drivers??
    #debugSock.sendto(str(deviceID)+":"+str(devices[deviceID]),debugAddress)
    responseSock.sendto(str(deviceID)+':'+str(devices[deviceID]),driverResponseAddress)

if __name__ == '__main__':

    print "STARTER: starting processes"
    startLoggers(1) #maybe determine # of loggers to start in windows?
    print "STARTER: processes started"

    deviceHeartBeatSock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    deviceHeartBeatSock.bind(deviceHeartBeatAddress)
    scoutRequestsSock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    scoutRequestsSock.bind(scoutRequestsAddress)
    driverRequestsSock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    driverRequestsSock.bind(driverRequestsAddress)
    dataMultiplexingSock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    dataMultiplexingSock.bind(dataMultiplexingAddress)
    

    inputs = [deviceHeartBeatSock,scoutRequestsSock,driverRequestsSock,dataMultiplexingSock]
    for sock in inputs:
        sock.setblocking(0)
    outputs = []#[scoutResponseSock, driverResponseSock]
    while 1:
        #periodically see if there are new bridges? or check only scout asks

        readable, writeable, exceptional = select.select(inputs,outputs,inputs)
        for sock in readable:
            if sock is deviceHeartBeatSock:
                device, clientAddr = sock.recvfrom(100)
                confirmDevice(device)
            if sock is scoutRequestsSock:
                data, clientAddr = sock.recvfrom(100)
                print data #possibly check specific request
                if data == 'kill':
                    killLoggers()
                    exit()
                else:
                    respondToScout()
            if sock is driverRequestsSock:
                data, clientAddr = sock.recvfrom(100)
                respondToDriver("Doorjamb 3")#int(data))
            if sock is dataMultiplexingSock:
                data, clientAddr = sock.recvfrom(100)
                data = data.split('::')
                deviceID = int(data[0])
                confirmDevice(deviceID)
                forwardDeviceData(deviceID,data[1])
                
                    
                    
                
                

