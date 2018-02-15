#!/bin/bash
datadirhbase=/tmp/hbase-root/hbase/
datadirzoo=/tmp/hbase-root/zookeeper
datadirtsdb=/tmp/tsdb/tmp
server="lot-linux.cloudapp.net"
port="4242"
totalhomes=10
totalruns=10
base=1262304000
count=$base

function insertdata() 
{
	foo=1
	for (( slot=1 ; slot <=1000 ; slot++ )) ; 

	do
		for (( home=1 ; home <=$totalhomes ; home++ )) ; 
		do
			#insert
			START=$(date +%s.%N)
			echo "put object$home $count $foo ID=$foo Type=animal EnterTimestamp=99999 ExitTimestamp=1000000 EntryArea=1 ExitArea=2 SMPC=$foo.112.112.112.112.112.112.112.112.112.112.112.112.112.112.112.112.112.112.112.112.112.112.112.112.112.112.112.112.112.112.112.112.112.112.112.112.112.112.112.112.112.112.112.112.112.112.112.112.112.112.112.112.112.112.112.112.112.112.112.123.123.123.123.123.112.123.123.123.123.123.123.112" |  nc -w 30 $server 4242
			END=$(date +%s.%N)
			inserttime=$(echo "$END - $START" | bc)
			echo $home $slot $inserttime
		done
		count=$((count+60)) #one object every 60 seconds
		foo=$((foo+1))
	done
	#count=$((count+1000))
}

function getstd()
{
        data=$1
        l=$2
        mean=0
        sd=0

        for (( i=1 ; i<=$l ; i++ )) ;
        do
                mean=`echo $mean + ${data[$i]} | bc -l `
        done

        mean=`echo $mean / $l |bc -l`

        for (( i=1 ; i<=$l ; i++ )) ;
        do
                a=`echo $mean - ${data[$i]} | bc -l `
                t=`echo  $a '*' $a  |bc -l`
                sd=`echo $sd + $t |bc -l`
        done
        sd=`echo sqrt '(' $sd / $l ')' |bc -l `
	ret_mean=$mean
	ret_std=$sd
#        echo $ret_mean $ret_sd
}


function retrieve() 
{
	slotIndex=$1 #window 
	totalrettime=0
	for (( home=1 ; home <=$totalhomes ; home++ )) ; 
	do 
			#retrieve
			n=$(($slotIndex))			
#			echo $n $home $slotIndex

			#currentday
			ts=$(($count - $n*60 ))
			te=$count
			tsstring=`date -u +%Y/%m/%d-%T -d @$ts`		
			testring=`date -u +%Y/%m/%d-%T -d @$te`		
#			echo $tsstring to $testring
			START=$(date +%s.%N)
			data=`curl --silent "http://$server:$port/q?start=$tsstring&&end=$testring&m=min:object$home&ascii"`
			END=$(date +%s.%N)
								
			rettime=$(echo "$END - $START" | bc)
			totalrettime=`echo $totalrettime + $rettime |bc -l `

			echo $slotIndex $ts $te $rettime  $totalrettime  `echo $data | grep -o object |wc -l` 
#			echo $data
#			echo "http://$server:$port/q?start=$tsstring&&end=$testring&m=min:object$home&ascii"
#			echo $data
#			echo `echo $data|grep -orE "object1" |sort| uniq -c`		

#			slotIndex=$((slotIndex+0))
		#	count=$((count+1))
	done
}

insertdata

c=1
for (( r=1 ; r <=$totalruns ; r++ )) 
do
	retrieve 600
	val=$totalrettime
	data[$c]=$val
	c=$((c+1))
#	echo $val
done
c=$((c-1))

getstd data $c
mean_val_600=$ret_mean
sd_val_600=$ret_std

c=1
for (( r=1 ; r <=$totalruns ; r++ )) 
do
	retrieve 60
	val=$totalrettime
	data[$c]=$val
	c=$((c+1))
	echo $val
done
c=$((c-1))

getstd data $c
mean_val_60=$ret_mean
sd_val_60=$ret_std

echo mean-60  $mean_val_60 std-60 $sd_val_60 mean-600 $mean_val_600 std-600 $sd_val_600
