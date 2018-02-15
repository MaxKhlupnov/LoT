results_combined = open('writes_Directory.tex', 'w')

results = {}

input_FDS = ['writes_label_Directory.dat', 'writes_latency_Directory.dat', 'writes_construct_and_secure_db_Directory.dat', 'writes_update_vds_Directory.dat', 'writes_fds_todisk_Directory.dat', 'writes_upload_fds_Directory.dat'] 

num = 0
for x in input_FDS:
    results_combined.write(str(num) + '-' + x)
    results_combined.write('\n')
    num += 1

results_combined.write('\n')


def combine_results():
    ret = {}
    no_lines = 0
    for file in input_FDS: 
        f = open(file, 'r') 
        lines = f.readlines()   # read in the data
        ret[file] = lines 
        #print(ret[file])
        no_lines = len(lines)
        f.close()

    for i in range (0, no_lines):
        for j in range(0, len(input_FDS)):
            f = input_FDS[j]
            try:
                num = float(ret[f][i].rstrip('\r\n'))
                total = float(ret[input_FDS[1]][i].rstrip('\r\n'))
                percent = (num * 100.0)/total
                percent = '%.1f' % percent
            except:
                percent = 0.0
            results_combined.write(ret[f][i].rstrip('\r\n'))
            if (j >= 2):
                results_combined.write(' (' + str(percent) + ')') 
            if (j != (len(input_FDS) - 1)):
                results_combined.write(' &')
            else:
                results_combined.write(' \\\\')
        results_combined.write('\n')
        results_combined.write('\n')

    results_combined.close()

if __name__ == "__main__":
    combine_results()
