#!/bin/bash 

numberofhomes=100
temp_start=-30
temp_end=40
mkdir -p ./logs 

function addmetric
{
	i=$1
	for ((j=$temp_start ; j <= $temp_end ; j++ )) 
	do
			metric=home-$i-$j
			../opentsdb/build/tsdb mkmetric $metric  >> ./logs/mkmetric-$i.log
	done
 
	../opentsdb/build/tsdb mkmetric home-$i--0 >> ./logs/mkmetric-$i.log
}

for ((i=1; i <=$numberofhomes ; i++ )) 
do	
	addmetric $i &
done
