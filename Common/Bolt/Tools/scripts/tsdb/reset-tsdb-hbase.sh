#!/bin/bash

echo diediedie | nc -w 30 localhost 4242
./hbase-0.94.10/bin/stop-hbase.sh
rm -rf /tmp/hbase-root
rm -rf /tmp/tsdb
rm -rf ./data-dir-tsdb/tsd/*

mkdir -p /tmp/tsdb/tmp
rm -rf  bak-hbase-0.94.10
mv hbase-0.94.10 bak-hbase-0.94.10
tar xfz hbase-0.94.10.tar.gz
cp bak-hbase-0.94.10/conf/hbase-env.sh ./hbase-0.94.10/conf/hbase-env.sh

echo Starting HBase
./hbase-0.94.10/bin/start-hbase.sh
