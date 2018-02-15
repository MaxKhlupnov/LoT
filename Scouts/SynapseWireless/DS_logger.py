"""
SNAP Connect Multi-cast Counter Listener Example
"""
import logging
from snapconnect import snap

import binascii
import sys
import socket

import time
import datetime

from ConfigParser import SafeConfigParser

#Resolution of ADC in SNAP
MAX_ADC_VAL = 1024
        
#Input voltage given to the SNAP Rf engine. ADC is calculated relative to this voltage
INPUT_VOLTAGE = 1.6 

#Time between each read on the SNAP node
DELTA = datetime.timedelta(milliseconds = 20) 

class DoorwaySensorClient(object):

    global BRIDGE_NODE
    global channel
    global log_name
    global snapconnect_addr

    
    def __init__(self,address,channel,log_name):
        # Create a SNAP instance
        self.channel = channel
        self.log_name = log_name
        self.snap = snap.Snap(funcs={'bridge_node_name':self.bridge_node_name,'reportIR': self.reportIR,'reportUS': self.reportUS,'reportPIR': self.reportPIR,'heartbeat':self.heartbeat},addr=address)
        self.snap.set_hook(snap.hooks.HOOK_SERIAL_CLOSE,self.connection_closed)
        self.snapconnect_addr = binascii.hexlify(self.snap.load_nv_param(2))
        #print self.log_name + ": snapconnect %02X.%02X.%02X" % (ord(addr[-3]), ord(addr[-2]), ord(addr[-1]))

        #sendingSocket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        #sendingSocket.sendto("Dongle\n",('localhost',8400))
        #print sendingSocket.recv(100)
        #sendingSocket.close()
        
    def connect(self,port,re):    
        # Open USB0 connected to a USB SNAP bridge (0)
        self.snap.open_serial(2, port,reconnect=re)
        # Create a logger
        self.log = logging.getLogger("Test")
        self.snap.mcast_rpc(1,2,'callback','bridge_node_name','localAddr')

    def bridge_node_name(self,name):
        self.BRIDGE_NODE = binascii.hexlify(name)
        print self.log_name + ": "+ self.snapconnect_addr + " at bridge ",self.BRIDGE_NODE
        #sendingSocket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        #sendingSocket.sendto("Bridge or Node\n",('localhost',8400))
        #print sendingSocket.recv(100)
        #sendingSocket.close()
        self.snap.rpc(name,'saveNvParam',4,self.channel)
        self.snap.rpc(name,'reboot')
    
    def connection_closed(self,serial_type,port):
        print "connection closed!!"

    ##RECEIVE HEARTBEAT####################################
    def heartbeat(self,SNAP_ID,readTime,seqnum):
        sysReadTime = datetime.datetime.now()
        filename = "heartbeat_"+sysReadTime.strftime("%Y-%m-%d")+"_"+str(SNAP_ID)
        #self.logData(filename,str(sysReadTime),readTime,str(SNAP_ID),seqnum)

        sendingSocket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        sendingSocket.sendto(str(SNAP_ID),('localhost',8402))
        sendingSocket.close()

    ##RECEIVE IR INFORMATION###############################
    def reportIR(self,SNAP_ID,irData,seqnum):
        if (SNAP_ID > 1):    
            filename = "3IR_"+datetime.datetime.now().strftime("%Y-%m-%d")+"_"+str(SNAP_ID)
            self.log3IR(SNAP_ID,irData,filename,seqnum)
        else:
            filename = "5IR_"+datetime.datetime.now().strftime("%Y-%m-%d")+"_"+str(SNAP_ID)
            self.log5IR(SNAP_ID,irData,filename,seqnum)
            
    def log3IR(self,SNAP_ID,irData,filename,seqnum):
        reads = len(irData)/8
        chars = 8   
        
        systime = datetime.datetime.now()
        currentRead = reads-1
        while(chars <= 8*reads):
            sysReadTime = str(systime - currentRead*DELTA)
            readTime = self.parseInt(irData[chars-8],irData[chars-7])
            ir1 = self.getHeightInches(self.parseInt(irData[chars-6],irData[chars-5]))
            ir2 = self.getHeightInches(self.parseInt(irData[chars-4],irData[chars-3]))
            ir3 = self.getHeightInches(self.parseInt(irData[chars-2],irData[chars-1]))
            data = str(SNAP_ID)+" "+str(ir1)+" "+str(ir2)+" "+str(ir3)
            self.logData("IR",filename, sysReadTime,readTime,data,seqnum)
            chars = chars + 8
            currentRead = currentRead - 1
            
        
    def log5IR(self,SNAP_ID,irData,filename,seqnum):
        reads = len(irData)/12
        chars = 12 
        
        systime = datetime.datetime.now()
        currentRead = reads-1
        while(chars <= 12*reads):
            sysReadTime = str(systime - currentRead*DELTA)
            readTime = self.parseInt(irData[chars-12],irData[chars-11])
            ir1 = self.getHeightInches(self.parseInt(irData[chars-10],irData[chars-9]))
            ir2 = self.getHeightInches(self.parseInt(irData[chars-8],irData[chars-7]))
            ir3 = self.getHeightInches(self.parseInt(irData[chars-6],irData[chars-5]))
            ir4 = self.getHeightInches(self.parseInt(irData[chars-4],irData[chars-3]))
            ir5 = self.getHeightInches(self.parseInt(irData[chars-2],irData[chars-1]))
            data = str(SNAP_ID)+" "+str(ir1)+" "+str(ir2)+" "+str(ir3)+" "+str(ir4)+" "+str(ir5)
            self.logData(filename, sysReadTime,readTime,data,seqnum)
            chars = chars + 12
            currentRead = currentRead - 1
            
    def getHeightInches(self,ir):
        voltageAdc = round((ir*INPUT_VOLTAGE/MAX_ADC_VAL),2)*2
        if voltageAdc <= 0.40:
            resultInches=60
        elif voltageAdc <= 0.45:
            resultInches = (4 * voltageAdc - 0.45 * 4 + ((0.45 - 0.4) * 56))/(0.45 - 0.4)
        elif voltageAdc <= 0.5:
            resultInches = (4 * voltageAdc - 0.5 * 4 + ((0.5 - 0.45) * 52))/(0.5 - 0.45)
        elif voltageAdc <= 0.55:
            resultInches = (4 * voltageAdc - 0.55 * 4 + ((0.55 - 0.5) * 48))/(0.55 - 0.5)
        elif voltageAdc <= 0.6:
            resultInches = (4 * voltageAdc - 0.6 * 4 + ((0.6 - 0.55) * 44))/(0.6 - 0.55)
        elif voltageAdc <= 0.7:
            resultInches = (4 * voltageAdc - 0.7 * 4 + ((0.7 - 0.6) * 40))/(0.7 - 0.6)
        elif voltageAdc <= 0.75:
            resultInches = (4 * voltageAdc - 0.75 * 4 + ((0.75 - 0.7) * 36))/(0.75 - 0.7)
        elif voltageAdc <= 0.8:
            resultInches = (4 * voltageAdc - 0.8 * 4 + ((0.8 - 0.75) * 32))/(0.8 - 0.75)
        elif voltageAdc <= 0.9:
            resultInches = (4 * voltageAdc - 0.9 * 4 + ((0.9 - 0.8) * 28))/(0.9 - 0.8)
        elif voltageAdc <= 1.0:
            resultInches = (4 * voltageAdc - 1.0 * 4 + ((1.0 - 0.9) * 24))/(1.0 - 0.9)
        elif voltageAdc <= 1.25:
            resultInches = (4 * voltageAdc - 1.25 * 4 + ((1.25 - 1.0) * 20))/(1.25 - 1)
        elif voltageAdc <= 1.5:
            resultInches = (4 * voltageAdc - 1.5 * 4 + ((1.5 - 1.25) * 16))/(1.5 - 1.25)
        elif voltageAdc <= 2.0:
            resultInches =  (4 * voltageAdc - 2.0 * 4 + ((2.0 - 1.5) * 12))/(2 - 1.5)
        elif voltageAdc <= 2.5:
            resultInches =  (4 * voltageAdc - 2.5 * 4 + ((2.5 - 2.0) * 8))/(2.5 - 2.0)
        elif voltageAdc <= 2.75:
            resultInches =  (4 * voltageAdc - 2.75 * 4 + ((2.75 - 2.5) * 6))/(2.75 - 2.5)
        else :
           resultInches = 0
            
        return resultInches
            
    ##RECEIVE US INFORMATION#############################
    def reportUS(self,SNAP_ID,usData,pktnum):
        if (SNAP_ID > 1):    
            filename = "2US_"+datetime.datetime.now().strftime("%Y-%m-%d")+"_"+str(SNAP_ID)
            self.log2US(SNAP_ID,usData,filename,pktnum)
        else:
            filename = "3US_"+datetime.datetime.now().strftime("%Y-%m-%d")+"_"+str(SNAP_ID)
            self.log3US(SNAP_ID,usData,filename,pktnum)
            
    def log2US(self,SNAP_ID,usData,filename,pktnum):
        reads = len(usData)/6
        chars = 6
        
        systime = datetime.datetime.now()
        
        currentRead = reads-1
        while(chars <= 6*reads):
            sysReadTime = str(systime - currentRead*DELTA)
            readTime = self.parseInt(usData[chars-6],usData[chars-5])
            us1 = self.parseInt(usData[chars-4],usData[chars-3])/2*.01356*2.54
            us2 = self.parseInt(usData[chars-2],usData[chars-1])/2*.01356*2.54
            data = str(SNAP_ID)+" "+str(us1)+" "+str(us2)
            self.logData("US",filename, sysReadTime,readTime,data,pktnum)
            chars = chars + 6
            currentRead = currentRead - 1
        
    def log3US(self,SNAP_ID,usData,filename,pktnum):
        reads = len(usData)/8
        chars = 8
            
        systime = datetime.datetime.now()
        currentRead = reads-1
        while(chars <= 8*reads):
            sysReadTime = str(systime - currentRead*DELTA)
            readTime = self.parseInt(usData[chars-8],usData[chars-7])
            us1 = self.parseInt(usData[chars-6],usData[chars-5])/2*.01356*2.54
            us2 = self.parseInt(usData[chars-4],usData[chars-3])/2*.01356*2.54
            us3 = self.parseInt(usData[chars-2],usData[chars-1])/2*.01356*2.54
            data = str(SNAP_ID)+" "+str(us1)+" "+str(us2)+" "+str(us3)
            self.logData(filename, sysReadTime,readTime,data,pktnum)
            chars = chars + 8
            currentRead = currentRead - 1
            
    ##RECEIVE PIR INFORMATION####################
    def reportPIR(self,SNAP_ID,readtime,pin,isset,pktseqnum):
        systime = str(datetime.datetime.now())
        filename = "2PIR_"+datetime.datetime.now().strftime("%Y-%m-%d")
        self.logData(filename,systime,readtime,str(SNAP_ID)+" "+str(pin)+" "+str(isset),pktseqnum)
       
    ##HELPER FUNCTIONS#########################
    def parseInt(self,hi,low):
        return int(ord(hi)<<8 | ord(low))

    def logData(self,dataType,file_name,timestamp,nodetime,data,pktseqnum):
        timeList = timestamp.split(".")
        if len(timeList) == 1:
            timestamp = timestamp+".000000"
        #Write output to log file
        outputFileLog = open (".\\data\\"+file_name+".log", "a+")
        datagram = str(timestamp)+" "+str(nodetime)+" "+str(data)+" "+str(pktseqnum)+"\n"
        outputFileLog.write(datagram)
        outputFileLog.write(str(timestamp)+" "+str(nodetime)+" ")
        outputFileLog.write(str(data)+" "+str(pktseqnum)+"\n")
        outputFileLog.close()

        datagram = dataType+" "+datagram
        #need to send per ID (keep mapping node to socket)
        sendingSocket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        sendingSocket.sendto('3::'+datagram,('localhost',8404))
        #print sendingSocket.recv(100)
        sendingSocket.close()


if __name__ == '__main__':
    #config_section = sys.argv[1]
    #config_parser = SafeConfigParser()
    #config_parser.read('config.ini')
    #license = '\x00\x00\x20' #binascii.unhexlify(config_parser.get(config_section,'license'))
    channel = 4#config_parser.getint(config_section,'channel')
    loc = 0#config_parser.getint('Loc_Mapping',config_parser.get(config_section,'bridge'))

    logging.basicConfig(level=logging.INFO) # print logging messages to STDOUT
    client = DoorwaySensorClient(address=None,channel=channel,log_name='testing bridge') # Instantiate a client instance
    client.connect(loc,False)
    client.snap.loop() # Loops waiting for SNAP messages
