# -*- coding: UTF-8 -*-

from flask import jsonify
from flask import Flask
from flask import render_template
from threading import Lock
from flask_socketio import SocketIO
from flask import request
import jinja2
import math
import numpy as np
import json
import redis
import pymysql
import time
from datetime import datetime
from datetime import timedelta
import sys
import os
import win32api


import heapq
import socket
import sys
import cv2
import pickle
import struct


async_mode = None
app = Flask(__name__)
app.config['SECRET_KEY'] = 'secret!'
socketio = SocketIO(app, async_mode=async_mode)
thread = None
thread_lock = Lock()
# count = 0
cardtimedict = {}
# # cardtime = dict_value_redis_data['time']
# cardtimelist = []
# if (len(cardtimedict) == 0):
# arr = "'"+ ID +"':'"+cardtime +"'"
# cardtimelist.append(arr)
# print(cardtimelist)
# cardtimedict.update(eval('{'+ (','.join(cardtimelist))+'}'))
# print((cardtimedict))

# 後端執行序產生之數據，即時推送至前端
def background_thread():
    print("---thread run---")

    while True:
        socketio.sleep(0.3)
        
        def convert(data):
            if isinstance(data, bytes):  return data.decode('utf-8')
            if isinstance(data, dict):   return dict(map(convert, data.items()))
            if isinstance(data, tuple):  return map(convert, data)
            return data        



        # 取得卡片id列表
        r = redis.Redis(host='127.0.0.1', port=6379,db=1)
        # CardID = "{'43A8','43DC'}"
        # r.lpush('CardID',CardID)
        # 取出目前存活卡片數量
        Cardcount = int(convert(r.lindex("card_count", 0)))
        

        # redis_keys = r.hkeys('setting_data')
        CardIDlist = []
        # print(len(redis_keys))
        # for i in range(len(redis_keys)):
        #     CardIDlist.append(convert(redis_keys[i]))
        # print ['3D5F', '44AF', '475B']
        
        # 連線redis
        # r = redis.Redis(host='127.0.0.1', port=6379,db=1)
        # cardtime = dict_value_redis_data['time']
        # cardtimelist = []
        # if (len(cardtimedict) == 0):
        # arr = "'"+ ID +"':'"+cardtime +"'"
        # cardtimelist.append(arr)
        # cardtimedict.update(eval('{'+ (','.join(cardtimelist))+'}'))
        
        # Coord_passingvalue_list = []
        # time_passingvalue_list = []
        
        passingvalue = {}
        addstrtail = '"}'
        Countpoint = 0
        Survival_card = []
        Missing_card = [] 
        Unique_card = []  
        

        while (len(Unique_card) <  Cardcount):
            tmp = {}
            Checkpoint = convert(r.lindex("card_location" , Countpoint))
            Cardcontent = Checkpoint
            dict_value_redis_data = eval(Cardcontent[0:-12] +addstrtail)
            ID = dict_value_redis_data['id']
            cardtime = dict_value_redis_data['time']
            cardcondition = Cardcontent[-2:-1]

            if (ID in Unique_card):
                Countpoint +=1
            else:
                if (ID not in cardtimedict):    
                # if (len(cardtimedict) == 0):
                    arr = "'"+ ID +"':' '"
                    # arr = "'"+ ID +"':'"+cardtime +"'"
                    # cardtimelist.append(arr)
                    # cardtimedict.update(eval('{'+ (','.join(cardtimelist))+'}'))
                    cardtimedict.update(eval('{'+ arr +'}'))
                    # print(cardtimedict)

                if (cardtimedict[ID] != cardtime):
                    arr = "'"+ ID +"':'"+cardtime +"'"
                    # cardtimelist.append(arr)
                    # print(arr)
                    # cardtimedict.update(eval('{'+ (','.join(cardtimelist))+'}'))
                    cardtimedict.update(eval('{'+ arr +'}'))
                    # print((','.join(cardtimelist)))
                    # print(cardtimedict)
                    # print(cardtimedict[ID])

                    # 座標點
                    # Coord_passingvalue_list = {'coord' : eval(dict_value_redis_data['coord'])}
                    Coord_passingvalue_list = eval(dict_value_redis_data['coord'])
                    # print(Coord_passingvalue_list)
                    # 時間點與轉換
                    # time_passingvalue_list = dict_value_redis_data['time']
                    timeArray = datetime.strptime(dict_value_redis_data['time'], "%Y-%m-%d %H:%M:%S")
                    otherStyleTime = datetime.strftime(timeArray,  "%Y/%m/%d %H:%M:%S")
                        
                    # time_passingvalue_dict = {'time' : otherStyleTime}
                    time_passingvalue_dict = otherStyleTime
                    # print(time_passingvalue_dict)
                    # 卡片ID

                    # 卡片狀態
                    # cardcondition_dict = {'exist':cardcondition}
                    cardcondition_dict = cardcondition

                    # print(cardcondition_dict)
                    # 傳送前整合(狀態, 座標, 時間)
                    tmp.setdefault(ID,[cardcondition_dict,Coord_passingvalue_list,time_passingvalue_dict])                
                    # print(passingvalue)
                    # Coord_passingvalue_list.append(time_passingvalue)
                    # passingvalue.append(Coord_passingvalue_list)
                    print('已獲取資料點之卡片', dict_value_redis_data['id'])
                    passingvalue.update(tmp)
                    Unique_card.append(ID)
                else :
                    print('資料庫時間重複', dict_value_redis_data['id'])
                    Unique_card.append(ID)
            # 已獲得資料的卡片數量增加   
            # Unique_card.append(ID)
            print(Unique_card)
            Countpoint +=1

            
            if (Countpoint > 10):
                    break

        if (len(passingvalue.keys()) < Cardcount):
            passingvalue = {}

        r.ltrim("card_count", 0, 100)
        r.ltrim("card_location", 0, 100)


        # while Whileloop_Countpoint < Cardcount :
        #     Checkpoint = convert(r.lindex("card_location" , Countpoint))
        #     Cardcontent = Checkpoint
        #     dict_value_redis_data = eval(Cardcontent[0:-12] +addstrtail)
        #     ID = dict_value_redis_data['id']
        #     cardtime = dict_value_redis_data['time']
        #     cardtimelist = []

        #     if (ID not in cardtimedict):    
        #     # if (len(cardtimedict) == 0):
        #         arr = "'"+ ID +"':' '"
        #         cardtimelist.append(arr)
        #         cardtimedict.update(eval('{'+ (','.join(cardtimelist))+'}'))

        #     elif (cardtimedict[ID] == cardtime):
        #         print('資料時間重複')
        #         # Countpoint += 1
        #         break
        #     else :
        #         arr = "'"+ ID +"':'"+cardtime +"'"
        #         cardtimelist.append(arr)
        #         cardtimedict.update(eval('{'+ (','.join(cardtimelist))+'}'))
                
        #         if (int(Checkpoint[-2:-1]) == 0 ):
        #             Cardcontent = Checkpoint
                    
        #             # 卡片ID

        #             # 卡片狀態
        #             # 座標點
        #             Coord_passingvalue_list = eval(dict_value_redis_data['coord'])
        #             # 時間點與轉換
        #             time_passingvalue_list = dict_value_redis_data['time']
        #             timeArray = datetime.strptime(time_passingvalue_list, "%Y-%m-%d %H:%M:%S")
        #             otherStyleTime = datetime.strftime(timeArray,  "%Y/%m/%d %H:%M:%S")
                    
        #             time_passingvalue = otherStyleTime
        #             # 傳送前整合
        #             Coord_passingvalue_list.append(time_passingvalue)
        #             passingvalue.append(Coord_passingvalue_list)
        #             print('已獲取資料點之卡片',dict_value_redis_data['id'])

        #             # while迴圈前進
        #             Countpoint += 1
        #             Whileloop_Countpoint += 1
        #         elif (int(Checkpoint[-2:-1]) != 0):
        #             print ('檢查到訊號消失的卡片',dict_value_redis_data['id'])

        #     for Cardlength in Cardcount :
        #         Checkpoint = convert(r.lindex("card_location" , 0))
        #         if (int(Checkpoint[-2:-1]) == 0 ):
        #             Cardcontent = Checkpoint
        #             dict_value_redis_data = eval(Cardcontent[0:-12] +addstrtail)
                

        # if (len(cardtimedict) < len(CardIDlist)):
        #     print('ps')
        #     for ID in CardIDlist:
        #         Cardcontent = convert(r.lindex(ID,1))
        #         print(Cardcontent)
        #         dict_value_redis_data = eval(Cardcontent[0:-7] +addstrtail)
        #         print(dict_value_redis_data)

        # else:
        #     for key in cardtimedict.keys():
        #         list_count = r.llen(ID)
        #         if (list_count >10):
        #             Cardcontent = convert(r.ltrim(key, 0, 9))
        #         else :
        #             Cardcontent = convert(r.lindex(key,0))
        #             print(cardtimedict)
        #             time1 = cardtimedict[key]
        #             dict_value_redis_data = eval(Cardcontent[0:-7] +addstrtail)
        #             cardtime = dict_value_redis_data['time']
        #             print(cardtime)
        #             print(time1)
        #             if (time1 == cardtime):
        #                 print(cardtime,time1)
        #                 print('資料時間重複')
        #                 passingvalue = []
                        
        #             else:
        #                 Coord_passingvalue_list = eval(dict_value_redis_data['coord']+']')
        #                 time_passingvalue_list = dict_value_redis_data['time']
        #                 cardtimedict[key] = time_passingvalue_list
        #                 timeArray = datetime.strptime(time_passingvalue_list, "%Y-%m-%d %H:%M:%S")
        #                 otherStyleTime = datetime.strftime(timeArray,  "%Y/%m/%d %H:%M:%S")
        #                 print(otherStyleTime)
        #                 time_passingvalue = otherStyleTime
        #                 Coord_passingvalue_list.append(time_passingvalue)
        #                 passingvalue.append(Coord_passingvalue_list)
        #                 print(passingvalue)
        #                 print('gggeww')

        print('後端傳輸之值',passingvalue)
        socketio.emit('server_response', passingvalue
            #   {'scales': t,
            #   'value': passingvalue['shotMax'],
            #   'value2': passingvalue[''] },
              ,     namespace='/test')
        # 注意：這裡不需要客户端連接的上下文，預設 broadcast = True

            # r.lpush(ID,att_str1)
            # r.lpush(ID,att_str2)
            # r.lpush(ID,att_str3)
        # 設定table, key, 內文
        # r.hset("distance", "3D5F", att_str1)
        # r.hset("distance", "44AF", att_str2)
        # r.hset("distance", "4SSF", att_str3)
        # # 取出table內全部key及value，做成dict
        # b_redis_data = r.hgetall('distance')

        # a       "port2": "4475",
        #         "distance2": "353 CM",
        #    a     "port3": "44AF",
        #         "distance3": "354 CM",
        #         "Gsensor": "X: -0.01g Y: -0.01g Z: 1.03g",
        #         "battery": "100 %",
        #         "sleeptime": "1000 ms",
        #         "without_move": "7 S",
        #         "last_receive": "2020/4/15 下午 11:09:53",
        #         "intervals": "7 s",
        #         "packet_loss": "0",
        #         "packet_receive": "11",
        #         "time": "2020-04-15 23:10:01",
        #         "coord": "[293.398809523810, 127.308035714286, 149.3947877691237]\r\n"
        #     }])


        # # 設定table, key, 內文
        # r.hset("distance", 1, att_str1)
        # 取出table內全部key及value，做成dict
        # b_redis_data = r.hgetall('distance')
        # redis_keys = r.hkeys('distance')

        
        #從二進轉dict
        # dict_redis_data = convert(b_redis_data)
        # print(dict_redis_data)
        # array_redis_keys = convert(redis_keys)
        # type(redis_keys)
        # print(redis_keys)
        
        # len(redis_keys)
        # array_redis_keys = []

        # addstrtail = ']"}'
        # addstrtail = "'}"

        # Coord_passingvalue_list = []
        # time_passingvalue_list = []
        # passingvalue = []
       
        # for i in range(len(redis_keys)):
            
            #從dict取出對應key (也是對應redis的key)之value, 為字串型 
            # array_redis_keys.append (convert(redis_keys[i]))     
            # value_redis_data = dict_redis_data[array_redis_keys[i]]

            #將字串格式value做不必要字串的去除(頭尾)，轉成dict形式方便取值
            # dict_value_redis_data = eval(value_redis_data[0:-7]+ addstrtail)
            # dict_value_redis_data = eval(value_redis_data[1:-7]+ addstrtail)

            #取出字串形式[x,y,z]的coord座標，透過eval轉成list，並儲存日期時間
            # Coord_passingvalue_list = eval(dict_value_redis_data['coord'])
            # time_passingvalue_list = dict_value_redis_data['time']
            # timeArray = datetime.strptime(time_passingvalue_list, "%Y-%m-%d %H:%M:%S")
            # otherStyleTime = datetime.strftime(timeArray,  "%Y/%m/%d %H:%M:%S")
            # time_passingvalue = otherStyleTime
            # Coord_passingvalue_list.append(time_passingvalue)

             #將座標轉作json格式傳至前端
            # passingvalue.append(Coord_passingvalue_list)
            # passingvalue.append(time_passingvalue)

        # print(passingvalue)
        # 取出table內其中一筆key及其value
        # hashvalue = r.hget('distance', 1)
        # hashkeyname = r.hkeys('distance')
        # print (hashkey)
        # print (hashvalue)
        # print (hashkeyname)
        # type (hashkeyname)
        
        # 連線MySQL 
        # db = pymysql.connect("localhost", "root", "esfortest", "test")
        # cursor = db.cursor()
        # # SQL特徵值
        # sql_select= "select *  from 'test' where 'id' =" + array_redis_keys + "ORDER BY time DESC LIMIT  "
        # cursor.execute(sql_select)
        # sql1sub=list(cursor.fetchall())



        # redis取值
        #從dict取出對應key (也是對應redis的key)之value, 為字串型 
        # value_redis_data = dict_redis_data[array_redis_keys]
        # # print(value_redis_data)
        # #填補下一行轉換dict前的字尾


        
        # # print(value_redis_data[0:-7]+ addstrtail)
        
        # Coord_passingvalue_list = eval(dict_value_redis_data['coord'])
        # time_passingvalue_list = dict_value_redis_data['time']

        # timeArray = datetime.strptime(time_passingvalue_list, "%Y-%m-%d %H:%M:%S")
        # otherStyleTime = datetime.strftime(timeArray,  "%Y/%m/%d %H:%M:%S")

        # time_passingvalue = otherStyleTime
        # type(time_passingvalue_list)


       

        

# --------------------------------web function--------------------------------------------------

@app.route('/')
def index():
    print("get index sucessfull")
    return render_template('wms_homepage.html', async_mode=socketio.async_mode)

@app.route("/wms_history")
def wms_history():
    return render_template("wms_history.html")

@app.route("/wms_input")
def wms_input():
    return render_template("wms_input2.html")

@app.route("/wms_setting")
def wms_setting():
    return render_template("wms_setting.html")

@socketio.on('connect', namespace='/test')
def test_connect():
    print("be connected")
    global thread
    with thread_lock:
        if thread is None:
            thread = socketio.start_background_task(target=background_thread)


# ------------------------------------button function-------------------------------------------

exe = 'C://智慧倉儲系統//Application software//App Source Code//PrecisePosition//bin//Debug//PrecisePosition.exe'

@app.route('/startmission', methods=['GET','POST'])
def execute_background_csharp():
    print(12)
    win32api.ShellExecute(0, 'open', exe, '','',1)
    return None
@app.route('/endmission', methods=['GET','POST'])
def shutdown_background_csharp():
    print(34)
    command = 'taskkill /F /IM PrecisePosition.exe'
    os.system(command)
    return None


@app.route('/moving', methods=['GET','POST'])
def moving():

    position_target = request.args.get("target1")
    # print(position_target)
    # "123,456,"
    str_list = position_target.split('"')
    string = str_list[1]
    ans = string.split(',')
    target_x = float(ans[0])
    target_y = float(ans[1])

    print(target_x,target_y)
    # os.system("python rf_bad.py")

    # os.system("python rf_socket.py %f %f" % (target_x,target_y))

    time.sleep(30)

    # ##############################################################################

    # # import packages

    # ##############################################################################




    # maze = [[2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2],
    #         [2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2],
    #         [2, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 2],
    #         [2, 2, 0, 2, 2, 2, 2, 2, 2, 2, 0, 2, 2, 2, 2, 2, 2, 0, 2, 2],
    #         [2, 2, 0, 2, 2, 2, 2, 2, 2, 2, 0, 2, 2, 2, 2, 2, 2, 0, 2, 2],
    #         [2, 2, 0, 2, 2, 2, 2, 2, 2, 2, 0, 2, 2, 2, 2, 2, 2, 0, 2, 2],
    #         [2, 2, 0, 2, 2, 2, 2, 2, 2, 2, 0, 2, 2, 2, 2, 2, 2, 0, 2, 2],
    #         [2, 2, 0, 2, 2, 2, 2, 2, 2, 2, 0, 2, 2, 2, 2, 2, 2, 0, 2, 2],
    #         [2, 2, 0, 2, 2, 2, 2, 2, 2, 2, 0, 2, 2, 2, 2, 2, 2, 0, 2, 2],
    #         [2, 2, 0, 2, 2, 2, 2, 2, 2, 2, 0, 2, 2, 2, 2, 2, 2, 0, 2, 2],
    #         [2, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 2],
    #         [2, 2, 0, 2, 2, 2, 2, 2, 2, 2, 0, 2, 2, 2, 2, 2, 2, 0, 2, 2],
    #         [2, 2, 0, 2, 2, 2, 2, 2, 2, 2, 0, 2, 2, 2, 2, 2, 2, 0, 2, 2],
    #         [2, 2, 0, 2, 2, 2, 2, 2, 2, 2, 0, 2, 2, 2, 2, 2, 2, 0, 2, 2],
    #         [2, 2, 0, 2, 2, 2, 2, 2, 2, 2, 0, 2, 2, 2, 2, 2, 2, 0, 2, 2],
    #         [2, 2, 0, 2, 2, 2, 2, 2, 2, 2, 0, 2, 2, 2, 2, 2, 2, 0, 2, 2],
    #         [2, 2, 0, 2, 2, 2, 2, 2, 2, 2, 0, 2, 2, 2, 2, 2, 2, 0, 2, 2],
    #         [2, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 2],
    #         [2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2],
    #         [2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2]]


    # db = pymysql.connect("localhost", "root", "esfortest", "test")
    # cursor = db.cursor()
    # SQL="SELECT X,Y,Z FROM data_table WHERE CardID = '43DC' ORDER BY mytime DESC;"
    # cursor.execute(SQL)   
    # ch = cursor.fetchone()
    # db.close()


    # raw_x = float(ch[0])
    # raw_y = float(ch[1])

    # if raw_y < 0 :
    #     raw_y = 0
    # start_y = round((626-raw_y)/31.3) -1
    # if raw_x > 0 :
    #     raw_x = 0
    # start_x = round((raw_x+613)/30.65) -1


    # #計算最近的線上之點
    # # 先直再橫
    # min_distance = 10000
    # min_y = 0 
    # min_x = 0
    # for i in range(9):
    #     for j in range(9):
    #         p1=np.array([start_y,start_x])
    #         if (start_y-4+i)>=0 and (start_x-4+j)>=0 and (start_y-4+i)<=19 and (start_x-4+j)<=19:
    #             if maze[start_y-4+i][start_x-4+j] == 0:
    #                 p2=np.array([start_y-4+i,start_x-4+j])
    #                 p3=p2-p1
    #                 p4=math.hypot(p3[0],p3[1])
    #                 if p4 < min_distance:
    #                     min_distance = p4
    #                     min_y = start_y-4+i
    #                     min_x = start_x-4+j


    # start = (min_y,min_x)
    # print(start)




    # # target_x = -300
    # # target_y = 300

    # if target_y < 0 :
    #     target_y = 0
    # target_y = round((626-target_y)/31.3) -1
    # if target_x > 0 :
    #     target_x = 0
    # target_x = round((target_x+613)/30.65) -1


    # #計算最近的線上之點
    # # 先直再橫
    # min_distance = 10000
    # min_y = 0 
    # min_x = 0
    # for i in range(9):
    #     for j in range(9):
    #         p1=np.array([target_y,target_x])
    #         if (target_y-4+i)>=0 and (target_x-4+j)>=0 and (target_y-4+i)<=19 and (target_x-4+j)<=19:
    #             if maze[target_y-4+i][target_x-4+j] == 0:
    #                 p2=np.array([target_y-4+i,target_x-4+j])
    #                 p3=p2-p1
    #                 p4=math.hypot(p3[0],p3[1])
    #                 if p4 < min_distance:
    #                     min_distance = p4
    #                     min_y = target_y-4+i
    #                     min_x = target_x-4+j

    # goal = (min_y,min_x)
    # print(goal)
    
    # grid = np.array(maze)
    # shape_length = np.shape(grid)
    # for i in range(shape_length[0]):
    #     for j in range(shape_length[1]):
    #         if grid[i][j] == 2:
    #             grid[i][j] = 1






    # ##############################################################################

    # # heuristic function for path scoring

    # ##############################################################################

    

    # def heuristic(a, b):

    #     return np.sqrt((b[0] - a[0]) ** 2 + (b[1] - a[1]) ** 2)

    

    # ##############################################################################

    # # path finding function

    # ##############################################################################

    

    # def astar(array, start, goal):

    #     # neighbors = [(0,1),(0,-1),(1,0),(-1,0),(1,1),(1,-1),(-1,1),(-1,-1)]
    #     neighbors = [(0,1),(0,-1),(1,0),(-1,0)]


    #     close_set = set()

    #     came_from = {}

    #     gscore = {start:0}

    #     fscore = {start:heuristic(start, goal)}

    #     oheap = []

    #     heapq.heappush(oheap, (fscore[start], start))
    

    #     while oheap:

    #         current = heapq.heappop(oheap)[1]

    #         if current == goal:

    #             data = []

    #             while current in came_from:

    #                 data.append(current)

    #                 current = came_from[current]

    #             return data

    #         close_set.add(current)

    #         for i, j in neighbors:

    #             neighbor = current[0] + i, current[1] + j

    #             tentative_g_score = gscore[current] + heuristic(current, neighbor)

    #             if 0 <= neighbor[0] < array.shape[0]:

    #                 if 0 <= neighbor[1] < array.shape[1]:                

    #                     if array[neighbor[0]][neighbor[1]] == 1:

    #                         continue

    #                 else:

    #                     # array bound y walls

    #                     continue

    #             else:

    #                 # array bound x walls

    #                 continue
                

    #             if neighbor in close_set and tentative_g_score >= gscore.get(neighbor, 0):

    #                 continue
                

    #             if  tentative_g_score < gscore.get(neighbor, 0) or neighbor not in [i[1]for i in oheap]:

    #                 came_from[neighbor] = current

    #                 gscore[neighbor] = tentative_g_score

    #                 fscore[neighbor] = tentative_g_score + heuristic(neighbor, goal)

    #                 heapq.heappush(oheap, (fscore[neighbor], neighbor))
    

    #     return False

    # route = astar(grid, start, goal)

    # route = route + [start]

    # route = route[::-1]

    # start_y = start[0]
    # start_x = start[1]
    # end_y = goal[0]
    # end_x = goal[1]
    # last_y = start_y
    # last_x = start_x
    # last_status = "起點"
    # signal = "無"


    # import redis
    # r = redis.Redis(host='127.0.0.1',port=6379,db=1)
    # r.delete('turn')
    # for pt in route:
    #     maze[pt[0]][pt[1]] = 1

    #     if (pt[0] > last_y and pt[1] == last_x):

    #         now_status = "向下"
    #         if (last_status == "起點"):
    #             last_status = now_status
    #         else:
    #             if( last_status == "向下"):
    #                 signal = "直走"
    #             if( last_status == "向上"):
    #                 signal = "錯誤"
    #             if( last_status == "向左"):
    #                 signal = "左轉"
    #             if( last_status == "向右"):
    #                 signal = "右轉"
    #             last_status = now_status
    #             if ((maze[last_y][last_x] != 2 and  maze[last_y][last_x+1] != 2 and maze[last_y+1][last_x] != 2) or (maze[last_y][last_x] != 2 and  maze[last_y][last_x-1] != 2 and maze[last_y+1][last_x] != 2) or (maze[last_y][last_x] != 2 and  maze[last_y-1][last_x] != 2 and maze[last_y][last_x-1]!= 2) or (maze[last_y][last_x] != 2 and  maze[last_y][last_x+1] != 2 and maze[last_y-1][last_x] != 2)):
    #                 if signal == '右轉':
    #                     r.rpush('turn', '-1')
    #                 if signal == '左轉':
    #                     r.rpush('turn', '-2')
    #                 if signal == '直走':
    #                     r.rpush('turn', '-3')

    #         last_y = pt[0]
    #         last_x = pt[1]
    #     if (pt[0] < last_y and pt[1] == last_x):

    #         now_status = "向上"
    #         if (last_status == "起點"):
    #             last_status = now_status
    #         else:
    #             if( last_status == "向下"):
    #                 signal = "錯誤"
    #             if( last_status == "向上"):
    #                 signal = "直走"
    #             if( last_status == "向左"):
    #                 signal = "右轉"
    #             if( last_status == "向右"):
    #                 signal = "左轉"
    #             last_status = now_status
    #             if ((maze[last_y][last_x] != 2 and  maze[last_y][last_x+1] != 2 and maze[last_y+1][last_x] != 2) or (maze[last_y][last_x] != 2 and  maze[last_y][last_x-1] != 2 and maze[last_y+1][last_x] != 2) or (maze[last_y][last_x] != 2 and  maze[last_y-1][last_x] != 2 and maze[last_y][last_x-1]!= 2) or (maze[last_y][last_x] != 2 and  maze[last_y][last_x+1] != 2 and maze[last_y-1][last_x] != 2)):
    #                 if signal == '右轉':
    #                     r.rpush('turn', '-1')
    #                 if signal == '左轉':
    #                     r.rpush('turn', '-2')
    #                 if signal == '直走':
    #                     r.rpush('turn', '-3')

    #         last_y = pt[0]
    #         last_x = pt[1]
    #     if (pt[0] == last_y and pt[1] > last_x):

    #         now_status = "向右"
    #         if (last_status == "起點"):
    #             last_status = now_status
    #         else:
    #             if( last_status == "向下"):
    #                 signal = "左轉"
    #             if( last_status == "向上"):
    #                 signal = "右轉"
    #             if( last_status == "向左"):
    #                 signal = "錯誤"
    #             if( last_status == "向右"):
    #                 signal = "直走"
    #             last_status = now_status
    #             if ((maze[last_y][last_x] != 2 and  maze[last_y][last_x+1] != 2 and maze[last_y+1][last_x] != 2) or (maze[last_y][last_x] != 2 and  maze[last_y][last_x-1] != 2 and maze[last_y+1][last_x] != 2) or (maze[last_y][last_x] != 2 and  maze[last_y-1][last_x] != 2 and maze[last_y][last_x-1]!= 2) or (maze[last_y][last_x] != 2 and  maze[last_y][last_x+1] != 2 and maze[last_y-1][last_x] != 2)):
    #                 if signal == '右轉':
    #                     r.rpush('turn', '-1')
    #                 if signal == '左轉':
    #                     r.rpush('turn', '-2')
    #                 if signal == '直走':
    #                     r.rpush('turn', '-3')

    #         last_y = pt[0]
    #         last_x = pt[1]
    #     if (pt[0] == last_y and pt[1] < last_x):

    #         now_status = "向左"
    #         if (last_status == "起點"):
    #             last_status = now_status
    #         else:
    #             if( last_status == "向下"):
    #                 signal = "右轉"
    #             if( last_status == "向上"):
    #                 signal = "左轉"
    #             if( last_status == "向左"):
    #                 signal = "直走"
    #             if( last_status == "向右"):
    #                 signal = "錯誤"
    #             last_status = now_status
    #             if ((maze[last_y][last_x] != 2 and  maze[last_y][last_x+1] != 2 and maze[last_y+1][last_x] != 2) or (maze[last_y][last_x] != 2 and  maze[last_y][last_x-1] != 2 and maze[last_y+1][last_x] != 2) or (maze[last_y][last_x] != 2 and  maze[last_y-1][last_x] != 2 and maze[last_y][last_x-1]!= 2) or (maze[last_y][last_x] != 2 and  maze[last_y][last_x+1] != 2 and maze[last_y-1][last_x] != 2)):
    #                 if signal == '右轉':
    #                     r.rpush('turn', '-1')
    #                 if signal == '左轉':
    #                     r.rpush('turn', '-2')
    #                 if signal == '直走':
    #                     r.rpush('turn', '-3')
    #         last_y = pt[0]
    #         last_x = pt[1]



    # command = r.lrange( "turn", 0, -1 )
    # for i in range(len(command)):
    #     command[i] = command[i].decode("gb2312")

    # print(command)
    # command_counter = 0

    # HOST = '192.168.137.1'
    # PORT = 8000

    # s=socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    # # print('Socket created')

    # s.bind((HOST, PORT))
    # # print('Socket bind complete')
    # s.listen(10)
    # # print('Socket now listening')



    # ring = redis.Redis(host='127.0.0.1', port=6379,db=1)
    # ring.lpush('robot',1)

    # client, addr = s.accept()
    # data = b''
    # payload_size = struct.calcsize("=L")


    # while True:

    #     while len(data) < payload_size:
    #         data += client.recv(4096)
    #     packed_msg_size = data[:payload_size]

    #     data = data[payload_size:]
    #     msg_size = struct.unpack("L", packed_msg_size)[0]

    #     while len(data) < msg_size:
    #         data += client.recv(4096)
    #     frame_data = data[:msg_size]
    #     data = data[msg_size:]

    #     frame=pickle.loads(frame_data)

    #     # db = pymysql.connect("localhost", "root", "esfortest", "test")
    #     # cursor = db.cursor()
    #     # SQL="SELECT X,Y,Z FROM data_table WHERE CardID = '43DC' ORDER BY mytime DESC;"
    #     # cursor.execute(SQL)   
    #     # ch = cursor.fetchone()
    #     # db.close()
    #     # end_x = float(ch[0])
    #     # end_y = float(ch[1])

    #     # if (end_x < target_x+30 and end_x > target_x-30 and end_y < target_y+30 and end_y > target_y-30):
    #     #     r.lset('robot',0,0)


    #     # content = client.recv(128)
    #     # print(content)

    #     check = ring.lindex('robot',0)
    #     if check == b'1':
    #         pass
        
    #     if check == b'0':
    #         stop = ring.lpop('robot')
    #         # print(stop)
    #         serverMessage = "10"
    #         client.send(serverMessage.encode(encoding = "gb2312"))
    #         break


        
        
    #     try:
    #         if command_counter < len(command) :
    #             serverMessage = command[command_counter]
    #             client.send(serverMessage.encode(encoding = "gb2312"))
    #             command_counter = command_counter + 1
    #         if command_counter == len(command) :
    #             serverMessage = "11"
    #             client.send(serverMessage.encode(encoding = "gb2312"))


    #     except:
    #         continue

    # client.close()
    return jsonify('OKKG')
    
@app.route('/start_following', methods=['GET','POST'])
def start_following():
    print('start_following')
    os.system("python server.py")
    ring = redis.Redis(host='127.0.0.1', port=6379,db=1)
    ring.lpush('robot',1)
    while True: 
        check = ring.lindex('robot',0)
        if check == b'1':
            pass
        
        if check == b'0':
            stop = ring.lpop('robot')
            break

    return jsonify('ok,start_following')

@app.route('/stop_following', methods=['GET','POST'])
def stop_following():
    print('stop_following')
    r = redis.Redis(host='127.0.0.1', port=6379,db=1)
    r.lset('robot',0,0)
    return jsonify('done,stop')

@app.route('/back', methods=['GET','POST'])
def back():
    print('back')
    os.system("python back_line.py")
    return jsonify('ok,back')




@app.route('/QueryHistoricalData',  methods=['GET','POST'])
def Query_Historical_Data():

    db = pymysql.connect("localhost", "root", "esfortest", "test")
    # db = pymysql.connect("localhost", "root", "rootroot", "test")
    cursor = db.cursor()

    # now_time = request.args.get("now_time")
    # ago_time = request.args.get("ago_time")
    # now_time = 
    # get CardID 
    # sql_select = "SELECT DISTINCT(`CardID`) FROM `data_table` WHERE `mytime` < '%s' and `mytime` > '%s'" %(now_time, ago_time)
    # print(sql_select)
    sql_select = "SELECT DISTINCT(`CardID`) FROM `data_table` WHERE `mytime` < '2020-07-15 00:40:00' and `mytime` > '2020-07-15 00:30:00'"
    cursor.execute(sql_select)
    CardIDlist = list(cursor.fetchall())
    print(CardIDlist)

    # 準備歷史資料
    Historical_Data_list = {}
    Historical_Data_set = {}
    transCardIDlist = []
    for CardID in CardIDlist :
         transCardIDlist.append(CardID[0])
    for CardID in CardIDlist :
       
        # sql_select = "SELECT DISTINCT `CardID`,`port1Distance`,`port2Distance`,`port3Distance` FROM `data_table`\
                        # WHERE `CardID` = '%s' And `mytime` < '%s' and `mytime` > '%s'\
                        # ORDER BY `data_table`.`mytime` DESC " %(CardID[0], now_time, ago_time)

        sql_select = "SELECT DISTINCT `CardID`,`x`,`y`,`z` FROM `data_table`\
                        WHERE `CardID` = '43A8' And `mytime` < '2020-07-15 00:40:00' and `mytime` > '2020-07-15 00:30:00'\
                        ORDER BY `data_table`.`mytime` DESC " 
        cursor.execute(sql_select)
        result_select = cursor.fetchall()
        
        print(len(result_select))
        
        # CardcoordX = []
        # CardcoordY = []
        CardcoordX =[]
        CardcoordY =[]
        CardcoordZ =[]
        # carddic = {}
        for i in range(len(result_select)) :
            # CardcoordX.append(row[1][0:-3]) 
            # CardcoordY.append(row[2][0:-3])
            
            CardcoordX.append(result_select[i][1])
            CardcoordY.append(result_select[i][2]) 
            CardcoordZ.append(result_select[i][3])
            
        # carddic = {CardID : CardID, x:CardcoordX , '%sy':CardcoordY}%(CardID,CardID,CardID)
        # print (carddic)
        dataset = str(CardID[0])+'dataset'
        Historical_Data_list = {CardID[0]:CardID[0],'x':CardcoordX, 'y':CardcoordY, 'z':CardcoordZ}
        Historical_Data_set.setdefault(CardID[0], [Historical_Data_list])
        print(Historical_Data_list)
        print(Historical_Data_set)
    Historical_Data_set.setdefault('CardIDlist', [transCardIDlist])
    try:
        return jsonify(Historical_Data_set)
    except Exception as e :
        print('error',str(e))



@app.route('/basesetting',  methods=['GET','POST'])
def basesetting():
    base1 = request.args.get("base1")
    base2 = request.args.get("base2")
    base3 = request.args.get("base3")
    base4 = request.args.get("base4")

    r = redis.Redis(host='127.0.0.1', port=6379,db=1)

    r.lpush("set_info",base1,base2,base3,base4)
    # length = r.llen("set_info")
    # for i in range(length):
    #     print(r.lpop("set_info"))
    
    # json.loads(basedata)
    print ('base1',base1)
    print ('base2',base2)
    print ('base3',base3)
    print ('base4',base4)
    return jsonify('ok')

if __name__ == '__main__':

    socketio.run(app, port=7788, debug=True)
