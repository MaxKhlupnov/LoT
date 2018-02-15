results_combined = open('writes_File.tex', 'w')

results = {}

input_VDS = ['writes_label_File.dat', 'writes_latency_File.dat', 'writes_index_lookup_File.dat', 'writes_construct_db_File.dat', 'writes_serial_File.dat', 'writes_todisk_File.dat', 'writes_add_dbi_File.dat', 'writes_flushindex_File.dat', 'writes_upload_indi_File.dat', 'writes_upload_chunked_File.dat', 'writes_compress_File.dat', 'writes_encrypt_File.dat']

num = 0
for x in input_VDS:
    results_combined.write(str(num) + '-' + x)
    results_combined.write('\n')
    num += 1

results_combined.write('\n')


def combine_results():
    ret = {}
    no_lines = 0
    for file in input_VDS: 
        f = open(file, 'r') 
        lines = f.readlines()   # read in the data
        ret[file] = lines 
        #print(ret[file])
        no_lines = len(lines)
        f.close()

    for i in range (0, no_lines):
        for j in range(0, len(input_VDS)):
            f = input_VDS[j]
            try:
                num = float(ret[f][i].rstrip('\r\n'))
                total = float(ret[input_VDS[1]][i].rstrip('\r\n'))
                percent = (num * 100.0)/total
                percent = '%.1f' % percent
            except:
                percent = 0.0
            results_combined.write(ret[f][i].rstrip('\r\n'))
            if (j >= 2):
                results_combined.write(' (' + str(percent) + ')') 
            
            if (j != (len(input_VDS) - 1)):
                results_combined.write(' &')
            else:
                results_combined.write(' \\\\')
        results_combined.write('\n')
        results_combined.write('\n')

    results_combined.close()

if __name__ == "__main__":
    combine_results()
