#!/bin/bash
cd ..

for ((i=1 ; i <=10 ; i++)) 
do
	./opentsdb/build/tsdb mkmetric object$i
done
