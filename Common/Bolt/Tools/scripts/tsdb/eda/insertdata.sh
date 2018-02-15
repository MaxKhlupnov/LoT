#!/bin/bash 

server="localhost"
port="4242"
datadir="./meter-data/*"
count=1
numberofhomes=100

echo "building temperature lookup table"
while read line 
do
	temp=`echo $line|cut -d' ' -f1`
	day=`echo $line|cut -d' ' -f5`
	yr=`echo $day|cut -d'-' -f1`
	hr=`echo $line|cut -d' ' -f6|tr -d '\r'`
	dtime=`echo $day $hr:00:00`
	ts=`date --date="$dtime"   "+%s"`
	data[$ts]=`echo $temp|cut -f1 -d"."` ;
done < ./weather.txt

for f in $datadir 
do 	
	while read line 
	do
		day=`echo $line | cut -d' ' -f1 `
		yr=`echo $day|cut -d'-' -f1`
		mn=`echo $day|cut -d'-' -f2`
		dt=`echo $day|cut -d'-' -f3`


		hr=`echo $line | cut -d' ' -f2 `
		hr=`echo $hr / 100 |bc`		
		eg=`echo $line | cut -d' ' -f3 `
		dtime=`echo $day $hr:00:00` 
		ts=`date --date="$dtime"   "+%s"`
		temp=${data[$ts]}

		metric=home-$count-$temp
		echo $metric $ts $eg a=b

	done < $f

	if [[ $count -eq $numberofhomes ]] ; then 
		exit 2
	fi
	count=$((count+1)) ;

done
