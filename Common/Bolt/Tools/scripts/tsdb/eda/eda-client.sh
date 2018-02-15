#!/bin/bash
server="lot-linux.cloudapp.net"
port="4242"
totalhomes=1
totalruns=1

startdate="2011/05/01-00:00:00"
enddate1mon="2011/05/31-00:00:00"
enddate6mon="2011/10/31-00:00:00"
enddate1yr="2012/04/30-00:00:00"

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
	totalrettime=0
	tsstring=$startdate

	if [[ $1 -eq 30 ]] ; then 
		testring=$enddate1mon		
	fi
	if [[ $1 -eq 180 ]] ; then 
		testring=$enddate6mon		
	fi
	if [[ $1 -eq 365 ]] ; then 
		testring=$enddate1yr		
	fi

	for (( home=1 ; home <=$totalhomes ; home++ )) ; 
	do 


		for ((temp=-30; temp <=40 ; temp++))
		do
			metric=home-$home-$temp
			START=$(date +%s.%N)
			qdata=`curl --silent "http://$server:$port/q?start=$tsstring&&end=$testring&m=min:$metric&ascii"`
			END=$(date +%s.%N)
								
			rettime=$(echo "$END - $START" | bc)
			totalrettime=`echo $totalrettime + $rettime |bc -l `
			echo $home $temp `echo $qdata | grep -o $metric |wc -l` `echo $qdata| wc --bytes` $totalrettime $rettime
		done
	done
}

c=1
for (( r=1 ; r <=$totalruns ; r++ ))
do
        retrieve 30
        val=$totalrettime
        data[$c]=$val
        c=$((c+1))
        echo $val
done
c=$((c-1))

getstd data $c
mean_val=$ret_mean
sd_val=$ret_std

echo Finished 1month mean is $mean_val sd is $sd_val
c=1
for (( r=1 ; r <=$totalruns ; r++ ))
do
        retrieve 180
        val=$totalrettime
        data[$c]=$val
        c=$((c+1))
        echo $val
done
c=$((c-1))

getstd data $c
mean_val=$ret_mean
sd_val=$ret_std

echo Finished 6 months mean is $mean_val sd is $sd_val
c=1
for (( r=1 ; r <=$totalruns ; r++ ))
do
        retrieve 365
        val=$totalrettime
        data[$c]=$val
        c=$((c+1))
        echo $val
done
c=$((c-1))

getstd data $c
mean_val=$ret_mean
sd_val=$ret_std

echo Finished 1 year mean is $mean_val sd is $sd_val
