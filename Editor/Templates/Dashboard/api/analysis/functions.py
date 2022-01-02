import json
import os
import re
import struct


def convertDatToJson(datFilePath, jsonFilePath):
    file = open(datFilePath, "rb")
    jsondict = {'episodes':[]}
    bytes = file.read(16)
    while bytes:
        # Read from .dat file
        (episode, duration, reward, num_positions) = struct.unpack("iffi", bytes)
        # print("Episode: %d, duration: %f, reward: %f, positions: %d" % (episode, duration, reward, num_positions))
        positions = []
        for i in range(num_positions):
            bytes = file.read(8)
            (x, y) = struct.unpack("ff", bytes)
            positions.append((x,y))
        bytes = file.read(16)

        # Write to json
        pos_x, pos_y = zip(*positions)
        episode_data = {
            'episode_number':episode, 
            'duration':duration, 
            'reward':reward, 
            'pos_x': pos_x,
            'pos_y': pos_y,
            'step_num':num_positions # not in spec, but could be useful
            }
        jsondict['episodes'].append(episode_data)

    with open(jsonFilePath, "w") as outfile:
        json.dump(jsondict, outfile)

def loadJSONIntoMemory(filePath):
    try: 
        with open(filePath) as json_file:
            data = json.load(json_file)

        return data
    except OSError as e:
        print("Error: %s : %s" % (filePath, e.strerror))
        return []

def removeFile(filePath):
    try:
        os.remove(filePath)
        print("File sucessfully deleted")
    except OSError as e:
        print("Error: %s : %s" % (filePath, e.strerror))

# Please specify the full directory for both parameters
def renameFile(existingFilePath, newFileName):
    try:
        os.rename(existingFilePath, newFileName)
        print("File sucessfully renamed to %s" % newFileName)
    except OSError as e:
        print("Error: %s : %s" % (existingFilePath, e.strerror))

def mergeFiles(filesToMerge, endFile):  
    with open(endFile, 'w') as outfile:
        for file in filesToMerge:
            with open(file) as infile:
                outfile.write(infile.read())
            outfile.write("\n")

def getAllFilesFromDirectory(directory):
    try:
        allFiles = []
        for file in os.listdir(directory):
            allFiles.append(os.path.join(directory, file))

        # Sort by file number
        allFiles.sort(key=lambda f: int(re.sub('\D', '', f)))

        return allFiles

    except OSError as e:
        print("Error: %s : %s" % (directory, e.strerror))
        return []

def getAllDatFilesFromDirectory(directory):
    try:
        allDatFiles = []
        for file in os.listdir(directory):
            if file.endswith(".dat"):
                allDatFiles.append(os.path.join(directory, file))

        # Sort by file number
        allDatFiles.sort(key=lambda f: int(re.sub('\D', '', f)))

        return allDatFiles

    except OSError as e:
        print("Error: %s : %s" % (directory, e.strerror))
        return []

def getAllJsonFilesFromDirectory(directory):
    try:
        allJsonFiles = []
        for file in os.listdir(directory):
            if file.endswith(".json"):
                allJsonFiles.append(os.path.join(directory, file))

        # Sort by file number
        allJsonFiles.sort(key=lambda f: int(re.sub('\D', '', f)))

        return allJsonFiles

    except OSError as e:
        print("Error: %s : %s" % (directory, e.strerror))
        return []
        