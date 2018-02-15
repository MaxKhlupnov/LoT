
1. For running it for 10 homes firs create metrics:
	./create-metrics.sh

2. Transfer dnw.sh to client machine (for remote reads), or run locally:
	./dnw.sh
	This script first appends object summaries to the 10 metrics (in function insertdata)
	then retrieves data for 60 and 600 min windows (in function retrieve), 10 times each, 
	then computes the mean and variation of 60 and 600 window retrieval times and prints them

3. To re-run reset the database by cd.. ; ./reset-tsdb/-hbase.sh ; and start at step 1
