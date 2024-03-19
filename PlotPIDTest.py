import numpy as np
import matplotlib.pyplot as plt
import sys
from datetime import datetime
from matplotlib.offsetbox import (TextArea, DrawingArea, OffsetImage,
                                  AnnotationBbox)
import json


def myfloat(s):
    result = None
    for i in s.split():
        try:
            result = float(i)
            break
        except:
            result = 0
            continue
    if result != None:
        if result < 0:
            return 0
    return result



args = sys.argv
# load diag log into memory
file_path = 'diags.log'
with open(file_path, 'r') as file:
    file_contents = file.readlines()
# scan file for last thermo test occurrence
startlines=[]
lineno = -1
startline = -1
for line in file_contents:
    lineno += 1
    if line.find("Starting RC monitoring") > 0:
        startline = lineno
        startlines.append(startline)
if startline < 0:
    print("PID test not found")
    exit()
if len(startlines) < 6:
    print("Incomplete PID test")
    exit()
# isolate date/time
eofdate = file_contents[startline].find("[")
dateTime = file_contents[startline][0:eofdate]
dateTime = dateTime.replace(",", ".")
startdt_obj = datetime.strptime(dateTime[:-5], '%Y-%m-%d %H:%M:%S')
milliseconds = int(dateTime[-4:-1])
startdt_obj = startdt_obj.replace(microsecond=milliseconds*1000)

# build lists of time, temps
tgt1 = "Velocity = "

sampleno = 0
x = []
y = [[],[],[],[]]
maxdelta=[-9999,-9999]

accum = 0
nsamples = 0
v1targets=[[7500,3750,700,-700,-3750,-7500],
           [-7500,-3750,-700,700,3750,7500]]
startlines.append(len(file_contents))
startlines=startlines[-7:]
maxvarx=0
maxvary=0
for j in range(6):
    for i in range(startlines[j]+1, startlines[j+1]):
        if i >= len(file_contents):
            continue
        index = file_contents[i].find(tgt1)
        if index < 0:
            continue
        sampeofdate = file_contents[i].find("[")
        sampdateTime = file_contents[i][0:eofdate]
        sampdateTime = sampdateTime.replace(",", ".")
        adt_obj = datetime.strptime(sampdateTime[:-5], '%Y-%m-%d %H:%M:%S')
        milliseconds = int(sampdateTime[-4:-1])
        adt_obj = adt_obj.replace(microsecond=milliseconds*1000)
        sampTime = (adt_obj-startdt_obj).total_seconds()
        index1 = file_contents[i].find(tgt1)
        if index1 < 0:
            continue
        temp = file_contents[i][index1+len(tgt1):]
        ordinates = temp.split(' ', 10)
        x.append(sampTime)
        for i in range(len(ordinates)):
            y[i].append(int(ordinates[i]))
            y[i+2].append(v1targets[i][j])
            dv=abs(v1targets[i][j]-int(ordinates[i]))
            if(dv>maxdelta[i]):
                maxdelta[i]=dv
                maxvarx=sampTime
                maxvary=int(ordinates[i])

        #accum = accum+myfloat(ordinates[0])+myfloat(ordinates[3])
        nsamples = nsamples+1
# convert lists into numpy arrays
x = np.array(x)
y = np.array(y)
# generate plot
labels = ["M1", "M2", ]

deltaT = -999
if nsamples > 0:
    deltaT = accum/nsamples

fig = plt.figure()
ax = fig.add_subplot(111)

for i in range(2):
    ax.plot(x, y[i, :], label=labels[i])
    ax.plot(x, y[i+2,:],linewidth=6,
        color="green", alpha=0.3)

ax.set_title("PIDTest " + " " + dateTime)
ax.set(xlabel='Time [s]', ylabel='Velocity [AU]')
plt.subplots_adjust(left=0.15)
plt.subplots_adjust(bottom=0.25)
plt.legend(loc='best')
fname = dateTime+'.png'
fname = fname.replace(" ", "_")
fname = fname.replace(":", "")
fname = fname.replace(",", "_")
if maxdelta[0]<350 and maxdelta[1]<350:
    result="PASS"
else:
    result="FAIL"
plt.text(x[0]+10,y[1][0],
     r'max$\Delta$V='+str(maxdelta[0])+" : "+str(maxdelta[1])+"; "+result)
plt.scatter(maxvarx, maxvary, s=500, c='red', marker='o')

# save plot to file
plt.savefig(fname, format="png", bbox_inches='tight')

plt.show()


'''
# open
with open('original.json', 'r') as file:

    # read
    data = json.load(file)
    
    # add
    data["abc"]["mno"] = 3

    # remove
    data.pop("jkl")

    newData = json.dumps(data, indent=4)

# open
with open('modified.json', 'w') as file:

    # write
    file.write(newData)
'''