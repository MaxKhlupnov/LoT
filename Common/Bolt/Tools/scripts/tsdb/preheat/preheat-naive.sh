#!/bin/bash
rm result.log
rm size-result.log
rm timing-result.log

datadirhbase=/tmp/hbase-root/hbase/
datadirzoo=/tmp/hbase-root/zookeeper
datadirtsdb=/tmp/tsdb/tmp
server="localhost"
port="4242"
base=1278633600
count=$base
slotIndex=0
for (( day=1 ; day <=1000 ; day++ )) ; 
do 
	for (( slot=1 ; slot <=96 ; slot++ )) ; 
	do
		#retrieve
		n=$(($slotIndex % 96))			
		d=$(($slotIndex / 96))			

		#currentday
		ts=$(($count - $n*900 ))
		te=$count
		tsstring=`date -u +%Y/%m/%d-%T -d @$ts`		
		testring=`date -u +%Y/%m/%d-%T -d @$te`		
		START=$(date +%s.%N)
		data[$d]=`curl --silent "http://$server:$port/q?start=$tsstring&&end=$testring&m=min:occupancy&ascii"`
		result=`echo $data|grep -orE "occupancy" |sort| uniq -c`	

		#past days
		for (( i=0 ; i < $d ; i++ )) 
		do
			ts=$(($base + 96*$i*900 ))	
			te=$(($ts + $n*900))

			tsstring=`date -u +%Y/%m/%d-%T -d @$ts`		
			testring=`date -u +%Y/%m/%d-%T -d @$te`		
			data[$i]=`curl --silent "http://$server:$port/q?start=$tsstring&&end=$testring&m=min:occupancy&ascii"`
		done
		END=$(date +%s.%N)	
		rettime=$(echo "$END - $START" | bc)


		#insert
		START=$(date +%s.%N)
		echo "put occupancy $count 0 a=b" |  nc -w 30 $server 4242
		END=$(date +%s.%N)
		inserttime=$(echo "$END - $START" | bc)

		#retrieved number of records
#		echo ${data[$d]}
		rn=`echo ${data[$d]} | grep -o occupancy |wc -l`
		for (( i=0 ; i < $d ; i++ )) 
		do
			rc=`echo ${data[$i]} | grep -o occupancy |wc -l`
			rn=`echo $rn + $rc|bc -l`
			echo ${data[$i]}
		done

		sbr=`echo "($n + 1) * ($d+1) "|bc -l` ;
		echo $d,$n,$slotIndex,$count,$inserttime,$rettime,$rn,$sbr >> timing-result.log
		 
#		size1=`du -s -b $datadirhbase`;
#		size2=`du -s -b $datadirtsdb`;
#		size3=`du -s -b $datadirzoo`;
#		size1=`echo $size1 | cut -d' ' -f1`
#		size2=`echo $size2 | cut -d' ' -f1`
#		size3=`echo $size3 | cut -d' ' -f1`
#		echo $slotIndex $size1 $size2 $size3 >> result.log
#		echo $slotIndex `du -b /tmp/hbase-root/hbase/` >> size-result.log
#		echo $slotIndex,$inserttime,$rettime  
#		echo $count $size1 $size2 $size3

		slotIndex=$((slotIndex+1))
		count=$((count+ 900)) #incrementing for 15 minute
	done
done
