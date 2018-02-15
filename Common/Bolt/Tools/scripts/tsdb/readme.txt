

1. Pre-requisites to install: 
	install build-essentials 
	install  openjdk-7-jdk
	install git 
	install dh-autoreconf
	install gnuplot
2. Get Hbase (v0.94.10)
	http://www.apache.org/dyn/closer.cgi/hbase/
  
3. Get OpenTSDB: 
	git clone git://github.com/OpenTSDB/opentsdb.git
	If required, change MAX_NUM_TAGS max number of tags per value from 8 to 32767 (max)
	cd opentsdb
	./build.sh

	make directory for opentsdb's cachedir
	mkdir -p ./data-dir/tsdb/tsd	

	Then add current dir path to startTSDB.sh and run 
	./startTSDB.sh
4. Record diskfootprint by running ./disk-footprint.sh
5. Proceed to run preheat, eda, or dnw.
