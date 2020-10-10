import socket
from datetime import datetime as dt
import redis
import pymysql


HOST = '192.168.137.1'
PORT = 8000

s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
print('Socket created')
s.bind((HOST, PORT))
print('Socket bind complete')
s.listen(10)
print('Socket now listening')
r = redis.Redis(host='127.0.0.1', port=6379, db=1)
r.delete('p_info')
r.lpush('socket', 1)
db = pymysql.connect("localhost", "root", "esfortest", "test")

# sol = ['01b808099f48881c293e00f1a','01b808099f48881a1938014b0','01b808099f488828593e00fa2','01c3e8e9eeefcccfd6c5a10fba','01b1b2b3b4b5a181d793e00f02','01d4c1c7dfc9c4e1ee93e00fb9','01c3c8c9cecfa18006938014f7','01b808099f48880e893801416','01b808099f48880e693801415','01c3c8c9cecfa180d29380146']
sol = ['b0b9ddz02vdbffffffffff','b0b9ddz02vdc8080811b92','b808099f48881c293e00f1a', 'b808099f48881a1938014b0', 'b808099f488828593e00fa2', 'c3e8e9eeefcccfd6c5a10fba', 'b1b2b3b4b5a181d793e00f02',
       'd4c1c7dfc9c4e1ee93e00fb9', 'c3c8c9cecfa18006938014f7', 'b808099f48880e893801416', 'b808099f48880e693801415', 'c3c8c9cecfa180d29380146']




# b0b9ddz02vdc8080811b92
# b0b9ddz02vdbffffffffff
# 資料庫
# db = pymysql.connect("localhost", "root", "esfortest", "etf")
# cursor = db.cursor()
# sql = "INSERT INTO tag (`id_1`,`id_2`,`id_3`,`id_4`,`id_5`,`id_6`,`id_7`,`id_8`,`id_9`,`id_10`,`id_11`,`id_12`) VALUES"
# sql = "INSERT INTO tag_id (`id`,`time`) VALUES"
# values = "('%s','%s','%s','%s','%s','%s','%s','%s','%s','%s','%s','%s')"
# values = "('%s','%s')"
# sql += values % (ans_list[0],ans_list[1],ans_list[2],ans_list[3],ans_list[4],ans_list[5],ans_list[6],ans_list[7],ans_list[8],ans_list[9],ans_list[10],ans_list[11],)
# sql += values % (str(ans_list),str(dt.now()))


# try:
#     cursor.execute(sql)
#     db.commit()
# except Exception as e:
#     db.rollback()
# db.close()


while True:
    are_you_real = 0
    check = r.lindex('socket', 0)
    if check == b'1':
        pass

    if check == b'0':
        stop = r.lpop('socket')
        print(stop)
        break

    client, addr = s.accept()
    content = client.recv(128)
    ans_list = []
    if len(content) != 0:
        string = str(content)
        str_list = string.split(" ")
        for i in range(len(str_list)):
            processed = str_list[i].split("\\x")
            target = processed[len(processed)-1]
            target1 = target.split("'")
            target = target1[len(target1)-1]
            ans_list.append(target)
            ans_list = ans_list[0:12]

        string = "".join(ans_list)
        print(string)

        if 'ff' in ans_list:
            continue
        else:
            # print(ans_list)

            # r.hset('p_info', str(ans_list), str(dt.now()))

            sql = "INSERT INTO tag_id (id, time) VALUES (%s, %s);"

            if '"' in string:
                adjust = string.split('"')
                string = adjust[1]
            if 'module' in string:
                continue

            for i in range(len(sol)):
                if string == sol[i]:
                    are_you_real = 1

            if are_you_real == 0:
                continue

            if r.hexists('p_info', string):
                # count = r.hget('p_info',string).decode()
                # counter = int(count) + 1
                # r.hset('p_info', string,counter)
                r.hincrby('p_info', string, 1)
            else:
                r.hset('p_info', string, 1)

            # byte_list = r.hvals('p_info')
            # byte_key_list = r.hkeys('p_info')
            # length = r.hlen('p_info')
            # string_list=[x.decode() for x in byte_list]
            # string_key_list=[x.decode() for x in byte_key_list]
            # results = list(map(int, string_list))
            # total = sum(results)
            # average = total/length

            # for i in range(length):
            #     if (average/results[i]) > 10:
            #         r.hdel('p_info', string_key_list[i])

            # new_data = (str(ans_list),dt.now())
            new_data = (string, dt.now())
            cursor = db.cursor()
            # print(sql)
            try:
                cursor.execute(sql, new_data)
                db.commit()
            except Exception as e:
                db.rollback()
                print("Exception Occured : ", e)
    else:
        print("hahaha")


print("Closing connection")
client.close()
db.close()

# ['d4', 'c1', 'c7', 'df', 'c9', 'c4', 'e1', 'ee', '93', 'e0', '0f', 'b9']
# ['b', '80', '80', '99', 'f4', '88', '82', '85', '93', 'e0', '0f', 'a2']
# ['b', '80', '80', '99', 'f4', '88', '81', 'c2', '93', 'e0', '0f', '1a']
# ['c3', 'e8', 'e9', 'ee', 'ef', 'cc', 'cf', 'd6', 'c5', 'a1', '0f', 'ba']
# ['b1', 'b2', 'b3', 'b4', 'b5', 'a1', '81', 'd7', '93', 'e0', '0f', '02']


# db = pymysql.connect("localhost", "root", "esfortest", "test")
# cursor = db.cursor()
# SQL="SELECT * FROM tag_id"
# cursor.execute(SQL)
# ch = cursor.fetchone()
# print(ch)
# db.close()

# print(type(ch))
# print(ch[0])
# print(ch[1])


# 'b808099f48881c293e00f1a':1
# 'b808099f48881a1938014b0':2
# 'b808099f488828593e00fa2':3
# 'c3e8e9eeefcccfd6c5a10fba' :4
# 'b1b2b3b4b5a181d793e00f02' :5
# 'd4c1c7dfc9c4e1ee93e00fb9' :6
# 'c3c8c9cecfa18006938014f7' :7
# 'b808099f48880e893801416':8
# 'b808099f48880e693801415':9
# 'c3c8c9cecfa180d29380146':10





# d4c1c7dfc9c4e1ee93e00fb9
# b1b2b3b4b5a181d793e00f02
# c3c8c9cecfa18006938014f7

# b808099f48881c293e00f1a
# b808099f48880e693801415

# c3e8e9eeefcccfd6c5a10fba
# b808099f48880e893801416