#import time
import tensorflow as tf
physical_devices = tf.config.experimental.list_physical_devices('GPU')
if len(physical_devices) > 0:
    tf.config.experimental.set_memory_growth(physical_devices[0], True)
from absl import app, flags, logging
from absl.flags import FLAGS
import core.utils as utils
from core.yolov4 import filter_boxes
from tensorflow.python.saved_model import tag_constants
from PIL import Image
import cv2
import numpy as np
from tensorflow.compat.v1 import ConfigProto
from tensorflow.compat.v1 import InteractiveSession


import socket
import sys
import cv2
import pickle
import struct
import redis





flags.DEFINE_string('framework', 'tf', '(tf, tflite, trt')
flags.DEFINE_string('weights', './checkpoints/yolov4-tiny-416',
                    'path to weights file')
flags.DEFINE_integer('size', 416, 'resize images to')
flags.DEFINE_boolean('tiny', False, 'yolo or yolo-tiny')
flags.DEFINE_string('model', 'yolov4', 'yolov3 or yolov4')
flags.DEFINE_string('video', './data/video/video.mp4', 'path to input video or set to 0 for webcam')
flags.DEFINE_string('output', None, 'path to output video')
flags.DEFINE_string('output_format', 'XVID', 'codec used in VideoWriter when saving video to file')
flags.DEFINE_float('iou', 0.45, 'iou threshold')
flags.DEFINE_float('score', 0.25, 'score threshold')
flags.DEFINE_boolean('dont_show', False, 'dont show video output')


HOST = '192.168.137.1'
PORT = 8000




def main(_argv):
    ring = redis.Redis(host='127.0.0.1', port=6379,db=1)
    ring.lpush('robot',1)
    break_flag = 0

    config = ConfigProto()
    config.gpu_options.allow_growth = True
    session = InteractiveSession(config=config)
    STRIDES, ANCHORS, NUM_CLASS, XYSCALE = utils.load_config(FLAGS)
    input_size = FLAGS.size
    video_path = FLAGS.video

    saved_model_loaded = tf.saved_model.load(FLAGS.weights, tags=[tag_constants.SERVING])
    infer = saved_model_loaded.signatures['serving_default']

    

    while True:
        
        s=socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        print('Socket created')
        s.bind((HOST, PORT))
        print('Socket bind complete')
        s.listen(5)
        print('Socket now listening')
        if break_flag == 1:
            print("ending")
            break
        conn, addr = s.accept()
        print("connected")

        

        while True:

            check = ring.lindex('robot',0)
            if check == b'1':
                pass
            
            if check == b'0':
                stop = ring.lpop('robot')
                # print(stop)
                break_flag = 1
                serverMessage = '10'
                conn.send(serverMessage.encode(encoding = "gb2312"))
                break

            print("in the main loop")
            data = b''
            payload_size = struct.calcsize("=L")


            while len(data) < payload_size:
                data += conn.recv(4096)
            packed_msg_size = data[:payload_size]
            data = data[payload_size:]
            msg_size = struct.unpack("L", packed_msg_size)[0]

            while len(data) < msg_size:
                data += conn.recv(4096)
            frame_data = data[:msg_size]
            data = data[msg_size:]

            frame_read=pickle.loads(frame_data)
       
            frame = cv2.cvtColor(frame_read, cv2.COLOR_BGR2RGB)
            image = Image.fromarray(frame)
    
            frame_size = frame.shape[:2]
            image_data = cv2.resize(frame, (input_size, input_size))
            image_data = image_data / 255.
            image_data = image_data[np.newaxis, ...].astype(np.float32)
            #start_time = time.time()
        
            batch_data = tf.constant(image_data)
            pred_bbox = infer(batch_data)
            for key, value in pred_bbox.items():
                boxes = value[:, :, 0:4]
                pred_conf = value[:, :, 4:]


            boxes, scores, classes, valid_detections = tf.image.combined_non_max_suppression(
                boxes=tf.reshape(boxes, (tf.shape(boxes)[0], -1, 1, 4)),
                scores=tf.reshape(
                    pred_conf, (tf.shape(pred_conf)[0], -1, tf.shape(pred_conf)[-1])),
                max_output_size_per_class=50,
                max_total_size=50,
                iou_threshold=FLAGS.iou,
                score_threshold=FLAGS.score
            )
            pred_bbox = [boxes.numpy(), scores.numpy(), classes.numpy(), valid_detections.numpy()]
            image,x_mid,area,class_name = utils.draw_bbox(frame, pred_bbox)
            result = np.asarray(image)
            cv2.namedWindow("result", cv2.WINDOW_AUTOSIZE)
            result = cv2.cvtColor(image, cv2.COLOR_RGB2BGR)
            cv2.imshow("result", result)
            cv2.waitKey(1)
        
            print("x_mid:",x_mid)
            print("area:",area)
            print("class_name:",class_name)

            if class_name:
                for i in range(5):
                    try:
                        if class_name[i]==0:
                            print("found person")
                            if area[i]<0.4:
                                if (x_mid[i]-0.5)>0.2:
                                    serverMessage = '4'
                                    conn.send(serverMessage.encode(encoding = "gb2312"))
                                    print("直走右轉")
                                    break
                                elif (x_mid[i]-0.5)<(-0.2):
                                    serverMessage = '5'
                                    conn.send(serverMessage.encode(encoding = "gb2312"))
                                    print("直走左轉")
                                    break
                                else:
                                    serverMessage = '6'
                                    conn.send(serverMessage.encode(encoding = "gb2312"))
                                    print("直走")
                                    break


                            elif area[i]>0.6:
                                if (x_mid[i]-0.5)>0.1:
                                    serverMessage = '4'
                                    conn.send(serverMessage.encode(encoding = "gb2312"))
                                    print("後退右轉")
                                    break
                                elif (x_mid[i]-0.5)<(-0.1):
                                    serverMessage = '5'
                                    conn.send(serverMessage.encode(encoding = "gb2312"))
                                    print("後退左轉")
                                    break
                                else:
                                    serverMessage = '11'
                                    conn.send(serverMessage.encode(encoding = "gb2312"))
                                    print("後退")
                                    break

                                
                            else:
                                if (x_mid[i]-0.5)>0.2:
                                    serverMessage = '4'
                                    conn.send(serverMessage.encode(encoding = "gb2312"))
                                    print("右轉")
                                    break
                                elif (x_mid[i]-0.5)<(-0.2):
                                    serverMessage = '5'
                                    conn.send(serverMessage.encode(encoding = "gb2312"))
                                    print("左轉")
                                    break
                    except:
                        if i==4:
                            serverMessage = '0'
                            conn.send(serverMessage.encode(encoding = "gb2312"))
                            print("停止")
                            print("except stop")
                            break
        



                            
                            
            else :
                    serverMessage = '0'
                    conn.send(serverMessage.encode(encoding = "gb2312"))
                    print("停止")
                    print("can't find people")
        
        print("out the loop")
        
                    

                
            
        
        #fps = 1.0 / (time.time() - start_time)
        #print("FPS: %.2f" % fps)
        
        #result = np.asarray(image)
        #cv2.namedWindow("result", cv2.WINDOW_AUTOSIZE)
        #result = cv2.cvtColor(image, cv2.COLOR_RGB2BGR)
        #cv2.imshow("result", result)
    conn.close()
    

        
        
       

        

if __name__ == '__main__':
    try:
        app.run(main)
    except SystemExit:
        pass





# conn.close()



