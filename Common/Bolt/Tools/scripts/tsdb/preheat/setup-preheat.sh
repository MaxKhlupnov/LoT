vim #!/bin/bash -x

cd ..
./reset-tsdb-hbase.sh

echo "Waiting ..."
sleep 10 
./startTSDB.sh
sleep 5
./opentsdb/build/tsdb mkmetric occupancy
#./preheat-naive.sh
