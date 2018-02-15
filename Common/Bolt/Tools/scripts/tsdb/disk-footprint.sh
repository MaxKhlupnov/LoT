#!/bin/bash
datadirhbase=/tmp/hbase-root/hbase/
datadirzoo=/tmp/hbase-root/zookeeper
datadirtsdb=/tmp/tsdb/tmp
cachedirtsdb=./data-dir-tsdb

size1=`du -sk -b $datadirhbase`;
size2=`du -sk -b $datadirtsdb`;
size3=`du -sk -b $datadirzoo`;
size4=`du -sk -b $cachedirtsdb`;

size1=`echo $size1 | cut -d' ' -f1`
size2=`echo $size2 | cut -d' ' -f1`
size3=`echo $size3 | cut -d' ' -f1`
size4=`echo $size4 | cut -d' ' -f1`

echo $size1 $size2 $size3 $size4
