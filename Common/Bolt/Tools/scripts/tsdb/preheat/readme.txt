To run preheat naive with opentsdb: 

1. First run create-metrics.sh 
2. Then run preheat-naive.sh  on the client machine
3. To parse retrieval time logs, run ./parse-preheat-timing-result.sh timing-result.log
4. To re-run preheat, i.e. reset the hbase database and re-create the metric run
	./setup-preheat.sh
