

import numpy as np
import heapq
import pymysql
import socket
import sys
import cv2
import pickle
import struct
import redis
import math


HOST = '192.168.137.1'
PORT = 8000

s=socket.socket(socket.AF_INET, socket.SOCK_STREAM)
s.bind((HOST, PORT))
s.listen(10)

client, addr = s.accept()
data = b''
payload_size = struct.calcsize("=L")
while True:

    while len(data) < payload_size:
        data += client.recv(4096)
    packed_msg_size = data[:payload_size]

    data = data[payload_size:]
    msg_size = struct.unpack("L", packed_msg_size)[0]

    while len(data) < msg_size:
        data += client.recv(4096)
    frame_data = data[:msg_size]
    data = data[msg_size:]

    frame=pickle.loads(frame_data)


    
    
    try:
        serverMessage = "15"
        client.send(serverMessage.encode(encoding = "gb2312"))
        break
        
    except:
        continue
client.close()