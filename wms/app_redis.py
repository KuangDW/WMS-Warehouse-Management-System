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


async_mode = None
app = Flask(__name__)
app.config['SECRET_KEY'] = 'secret!'
socketio = SocketIO(app, async_mode=async_mode)
thread = None
thread_lock = Lock()
# count = 0
cardtimedict = {}
RFID_input_stock = ''
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
        socketio.sleep(0.5)
        
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
                else :
                    print('資料時間重複')
                # 已獲得資料的卡片數量增加   
                Unique_card.append(ID)
                print(Unique_card)
                Countpoint +=1

            
            if (Countpoint > 10):
                    break

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

@app.route("/wms_stock")
def wms_stock():
    return render_template("wms_stock.html")

@app.route("/wms_setting")
def wms_setting():
    return render_template("wms_setting.html")

@app.route("/wms_pickup")
def wms_pickup():
    return render_template("wms_pickup.html")

@app.route("/wms_testing")
def wms_testing():
    return render_template("wms_testing.html")

@socketio.on('connect', namespace='/test')
def test_connect():
    print("be connected")
    global thread
    with thread_lock:
        if thread is None:
            thread = socketio.start_background_task(target=background_thread)

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
# ------------------------------------------------RFID related----------------------------------------
@app.route('/RFID_input_start', methods=['GET','POST'])
def RFID_input_start():
     
    HOST = '192.168.137.1'
    PORT = 8000

    s=socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    print('Socket created')
    s.bind((HOST, PORT))
    print('Socket bind complete')
    s.listen(0)
    print('Socket now listening')
    r = redis.Redis(host='127.0.0.1', port=6379,db=1)
    r.lpush('socket',1)
    db = pymysql.connect("localhost", "root", "esfortest", "test")

    while True:
        check = r.lindex('socket',0)
        if check == b'1':
            pass
        
        if check == b'0':
            stop = r.lpop('socket')
            print(stop)
            break

        client, addr = s.accept()
        content = client.recv(128)
        ans_list = []
        if len(content) !=0:
            string = str(content)
            str_list = string.split(" ")
            for i in range(len(str_list)):
                processed = str_list[i].split("\\x")
                target = processed[len(processed)-1]
                target1 = target.split("'")
                target = target1[len(target1)-1]
                ans_list.append(target)
                ans_list = ans_list[0:12]
            if 'ff' in ans_list : 
                continue
            else:
                print(ans_list)
                r.hset('p_info', str(ans_list), str(datetime.now()))

                sql = "INSERT INTO tag_id (id, time) VALUES (%s, %s);"
                string = "".join(ans_list)
                # new_data = (str(ans_list),dt.now())
                new_data = (string,datetime.now())
                cursor = db.cursor()
                print(sql)
                try:
                    cursor.execute(sql, new_data)
                    db.commit()
                except Exception as e:
                    db.rollback()
                    print("Exception Occured : ", e)


    print("Closing connection")
    client.close()
    db.close()
    return jsonify('RFID_Socket_finish')

@app.route('/RFID_input_close', methods=['GET','POST'])
def RFID_input_close():
    r = redis.Redis(host='127.0.0.1', port=6379,db=1)
    r.lset('socket',0,0)
    return jsonify('RFID_Socket_closing')

@app.route('/RFID_InputHash_query', methods=['GET','POST'])
def RFID_InputHash_query():
    
    def convert(data):
        if isinstance(data, bytes):  return data.decode('utf-8')
        if isinstance(data, dict):   return dict(map(convert, data.items()))
        if isinstance(data, tuple):  return map(convert, data)
        return data       

    r = redis.Redis(host='127.0.0.1', port=6379,db=1)
    redis_keys = r.hkeys('p_info')
    RFIDlist = []
    print(convert(redis_keys))
    
    for i in range(len(redis_keys)):
        RFIDlist.append((convert(redis_keys[i])))
    # print('RFID count',len(redis_keys))
    # print('RFIDlist',RFIDlist)
    return jsonify(RFIDlist)

# -----------------------------------------------query data-----------------------------------------
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

@app.route('/inputformintoDB',  methods=['GET','POST'])
def input_form():
    # print(e)
    Purchase_date = request.args.get('Purchase_date', default="")
    RFID_num = request.args.get('RFID_num', default="")
    Supplier = request.args.get('Supplier', default="")
    Material_num = request.args.get('Material_num', default="")
    Material_name = request.args.get('Material_name', default="")
    Quantities = request.args.get('Quantities', default="")
    Location_ = request.args.get('Location_', default="")
    
    
    # insert into DB
    db = pymysql.connect("localhost", "root", "esfortest", "test")
    cursor = db.cursor()
    try:
        sql_select = "SELECT `RFID_num` from stock_table where `RFID_num` = '%s' limit 1;" %(RFID_num)
        cursor.execute(sql_select)
        result_select = cursor.fetchall()
        print(len(result_select))
    except:
        result_select =[]

    if (len(result_select) > 0 ):
        db.close()
        return jsonify('Duplicate RFID_num')
    else :
        sql = "INSERT INTO stock_table (`RFID_num`,`Supplier`,`Material_num`,`Material_name`,`Quantities`,`Location`,`Purchase_date`,`Inventory_date`)\
        VALUES ('%s','%s','%s','%s','%s','%s','%s','%s')"% (RFID_num, Supplier, Material_num, Material_name, Quantities, Location_, Purchase_date, Purchase_date)
        cursor.execute(sql)
        db.commit()
        db.close()

    # print(request_json_data['Purchase_date'])
    # print(request_json_data['RFID_num'])
    # print(request_json_data['Supplier'])
    # print(request_json_data['Material_num'])
    # print(request_json_data['Material_name'])
    # print(request_json_data['Quantities'])
    # print(request_json_data['Location_'])
    return jsonify('ok')



@app.route('/inventory_query',  methods=['GET','POST'])
def inventory_query():
    Section = request.args.get('categories')
    print(Section)
    db = pymysql.connect("localhost", "root", "esfortest", "test")
    # db = pymysql.connect("localhost", "root", "rootroot", "test")
    cursor = db.cursor()
    sql_select = "SELECT * FROM `stock_table`\
                WHERE `Location` = '%s' \
                ORDER BY `stock_table`.`Inventory_date` ASC , `stock_table`.`Material_num` ASC" %(Section)
    cursor.execute(sql_select)
    result_select = cursor.fetchall()
    print(result_select)
    Inventory_stock_list = []
    for i in range(len(result_select)) :
        Inventory_stock_list.append(result_select[i])
    return jsonify(Inventory_stock_list)


@app.route('/update_inventoryInfo',  methods=['GET','POST'])
def update_inventoryInfo():
    Section = request.args.get('Section')
    Check_RFIDlist = request.args.get('Check_RFIDlist')
    print(Section)
    print((Check_RFIDlist)[1:-1])
    Check_RFIDlist = '111'
    now_time = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    db = pymysql.connect("localhost", "root", "esfortest", "test")
    # db = pymysql.connect("localhost", "root", "rootroot", "test")
    cursor = db.cursor()
    sql_update = "UPDATE `stock_table`  SET `Inventory_date` = '%s'  WHERE (`RFID_num` IN (%s));" % (now_time, Check_RFIDlist[1:-1])
    # print(sql_update)
    cursor.execute(sql_update)
    # INSERT UPDATE delete需要commit
    db.commit()

    sql_select = "SELECT * FROM `stock_table`\
                WHERE `Location` = '%s' and (`RFID_num` IN ('%s')) \
                ORDER BY `stock_table`.`Inventory_date` ASC , `stock_table`.`Material_num` ASC" %(Section, Check_RFIDlist)
    print(sql_select)
    cursor.execute(sql_select)
    result_select = cursor.fetchall()
    print(result_select)
    Inventory_stock_list = []
    for i in range(len(result_select)) :
        Inventory_stock_list.append(result_select[i])

    print(jsonify(Inventory_stock_list))
    return jsonify(Inventory_stock_list)
    # return jsonify('ok')

@app.route('/pickup_query',  methods=['GET','POST'])
def pickup_query():
    Check_RFIDlist = request.args.get('Check_RFIDlist')
    print(Check_RFIDlist)
    # Check_RFIDlist = "['111','222','333','444']"
    db = pymysql.connect("localhost", "root", "esfortest", "test")
    cursor = db.cursor()
    sql_select = "SELECT * FROM `stock_table`\
                WHERE (`RFID_num` IN (%s)) \
                ORDER BY `stock_table`.`Material_num` ASC" %(Check_RFIDlist[1:-1])    
    cursor.execute(sql_select)
    result_select = cursor.fetchall()
    Inventory_stock_list = []
    for i in range(len(result_select)) :
        Inventory_stock_list.append(result_select[i])

    print(jsonify(Inventory_stock_list))
    return jsonify(Inventory_stock_list)





@app.route('/testing_rfid',  methods=['GET','POST'])
def testing_rfid():
    aa= 0
    return jsonify('ok')

if __name__ == '__main__':

    socketio.run(app, port=7788, debug=True)
