from math import e
import socket
import struct
import threading
import pandas as pd
from joblib import load
import numpy as np
import json
from tensorflow.keras.models import load_model
from collections import Counter

from sklearn.decomposition import PCA
# !pip3 install --upgrade joblib numpy


# A modell betöltése
model=load_model('LSTM_model')

port= 2222

def start_server():
    

    server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server_socket.bind(("127.0.0.1", port))
    server_socket.listen(5)
    print(f"Python server is listening on port {port}...")

    while True:

        try:
            conn, adr = server_socket.accept()
            threading.Thread(target=handle_client, args=(conn,)).start()
        except Exception as e:
             print(f"Kivétel a ClassifierModel osztályban: {e}")
            # conn.close()
    

def handle_client(conn):
    try:
        while True:
            # try:
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

            json_data = buffer.decode('utf-8')
                
            # df = pd.read_json(json_data)
            df= json.loads(json_data)
            df = np.array(df)

            df= df.reshape(-1, 25, 50)
        

            predictions = model.predict(df)
            predictions= np.argmax(predictions, axis=1)

            pred= np.bincount(predictions)
       
            most_common = Counter(predictions).most_common(1)[0][0]

            response_bytes = struct.pack('!I', most_common)  # Integer (4 byte)
            conn.sendall(response_bytes)  
                
            print(f"A tippelt edzes szama: {predictions}")
    except Exception as e:
        print(f"HIba a classifierModelben: {e} ")
    finally:
        conn.close()
  



