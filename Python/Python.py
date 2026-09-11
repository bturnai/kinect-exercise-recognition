    
from math import e
import cv2
import os
from sys import platform
import argparse
import sys
import os
import socket
import numpy as np
import multiprocessing
import struct

import matplotlib.pyplot as plt 
import pandas as pd
import json
from io import StringIO
import threading

from threading import Thread
from ClassifierModell import start_server as start_classifier


from sklearn.metrics import accuracy_score, classification_report, confusion_matrix
from sklearn.model_selection import train_test_split
from sklearn.preprocessing import LabelEncoder, MinMaxScaler
from sklearn.svm import SVC


portOP= 1111
# import Trainer.py

OpenposeDir = r'C:\Users\Legion\Documents\openpose\build\python\openpose\Release'
dir_path = OpenposeDir

if platform == "win32":
    # dir_path = os.path.dirname(os.path.realpath(__file__))
    sys.path.append(dir_path + '/../../python/openpose/Release');
    
    sys.path.append(dir_path );
    import pyopenpose as op

else:
    sys.path.append('../../python');
    
    from openpose import pyopenpose as op


def imageDecode(buffer) ->np.ndarray:
    image_array = np.frombuffer(buffer, np.uint8)
    image_array = np.frombuffer(buffer, dtype=np.uint8).reshape((480, 640, 4)) 
    image_bgr = cv2.cvtColor(image_array, cv2.COLOR_BGRA2BGR)
    return image_bgr

def start_openpose_server():
    params = {
        "model_folder": "C:/Users/Legion/Documents/openpose/models/",
        "net_resolution": "320x176",
        "output_resolution": "640x480",  
        "keypoint_scale": "0",  
        "face": False,
        "hand": False,
        "number_people_max": 1
    }
    opWrapper = op.WrapperPython()
    opWrapper.configure(params)
    opWrapper.start()

    server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server_socket.bind(("127.0.0.1", portOP))
    server_socket.listen(5)
    print(f"Python server is listening on port {portOP}...")

    while True:
        conn, adr = server_socket.accept()
        threading.Thread(target=handle_client, args=(conn, opWrapper)).start()

def handle_client(conn, opWrapper):
    try:
        while True:
            length_prefix = conn.recv(4)
            if not length_prefix:
                break
            image_length = struct.unpack('!I', length_prefix)[0]

            buffer = bytearray()
            while len(buffer) < image_length:
                data = conn.recv(min(4096, image_length - len(buffer)))
                if not data:
                    break
                buffer.extend(data)

            if len(buffer) == image_length:
                datum = op.Datum()
                datum.cvInputData = imageDecode(buffer)
                opWrapper.emplaceAndPop(op.VectorDatum([datum]))

                
                        
                if datum.poseKeypoints is None:
                    response_data = {"poseKeypoints": None}
                    print("Nem talalt csontvazat")
                else:
                    
                    response_data = {"poseKeypoints": datum.poseKeypoints.tolist()}
                    print(response_data)
                json_response = json.dumps(response_data, ensure_ascii=False)


                response_bytes = json_response.encode('utf-8')
                response_length = struct.pack('!I', len(response_bytes))
                conn.sendall(response_length + response_bytes)
                
                print(f"Keypoints elkuldve a kliens reszere {len(response_bytes)}")
    except Exception as e:
        print(f"Error handling client: {e}")
    finally:
        conn.close()
        print("Python.py conn lezarva")




# start_openpose_server()

def main():
    openpose_thread = Thread(target=start_openpose_server)
    classifier_thread = Thread(target=start_classifier)

    openpose_thread.start()
    classifier_thread.start()

    openpose_thread.join()
    classifier_thread.join()

if __name__ == "__main__":
    main()