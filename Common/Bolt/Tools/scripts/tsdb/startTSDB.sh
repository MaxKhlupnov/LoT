#!/bin/bash
currentdir="/home/azureuser/bolt-tsdb/"
cd opentsdb
env COMPRESSION=NONE HBASE_HOME=$currentdir/hbase-0.94.10 ./src/create_table.sh
mv outlog outlog.bak
nohup ./build/tsdb tsd --port=4242 --staticroot=build/staticroot --cachedir=$currentdir/data-dir-tsdb/tsd >> ./outlog  &
