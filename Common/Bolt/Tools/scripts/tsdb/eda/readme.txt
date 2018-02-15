
1. Insert metrics into opentsdb (in paralell, one thread per home) 
	./create-metrics.sh 
Check if all metrics are inserted: ../opentsdb/build/tsdb uid grep home


[Data is already converted and checked in into data.gz; please unzip gzip -d data.gz and skip 2]
2. Convert data from raw to tsdb format (hourly data, with temperature at 1 C granularity)
	./insertdata.sh >> ./data

3. Import data to tsdb 
	../opentsdb/build/tsdb import ./data
../opentsdb/build/tsdb scan <Start-date> sum <metric>


4. On client machine, run ./eda-client.sh
