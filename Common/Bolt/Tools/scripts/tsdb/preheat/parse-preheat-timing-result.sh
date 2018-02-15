#!/bin/bash

file=$1

while read line 
do 
	slot=`echo $line|cut -d',' -f2`

	if [[ $slot -eq 95 ]] ; then 
		echo $line ;
	fi 

done < $file
