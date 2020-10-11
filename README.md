# 用於物流業之室內定位技術與倉儲管理系統 - 後端
## 日程規劃:
 - [x] 1. 距離資訊連線資料庫(Mysql & Redis)
 - [x] 2. 實作C#的座標定位算法
 - [x] 3. 資料庫配置與規劃
 - [x] 4. 未來工作
     - [x] 設定頁面
     - [x] RFID
     - [x] 基站擴充後的改變

## Mysql & Redis 安裝:
C#連結Mysql的方法
https://awei791129.pixnet.net/blog/post/24339980

如何安裝Redis
https://marcus116.blogspot.com/2019/02/how-to-install-redis-in-windows-os.html

管理Redis的應用程式安裝
https://marcus116.blogspot.com/2019/02/redis-redis-redis-desktop-manager.html

### 引入參考
#### *安裝資料庫後還需要引入相關參考才能使用下方程式碼:*
![](https://i.imgur.com/K5SxF5S.png)
```
using System.Text;
using System.Linq;
using System.Collections.Generic;
using MySql.Data.MySqlClient;   //引用MySql

using StackExchange.Redis; //引用redis
using Newtonsoft.Json;
```

<font color=red>先看程式概念!!! 程式啟動後，基站會不斷收到卡片資訊，傳給電腦。裡面有一個迴圈"MyTimer_Tick"會一直跑，讓每張卡片的資訊(卡片距離、電量等)更新下面附圖的監控列表，一秒更新一次，MyTimer_Tick運作方式如下。

EX:如果目前只有一張43A8卡片，迴圈就會，
更新43A8資料, 1秒間隔, 更新43A8資料, 1秒間隔, 更新43A8資料...這樣更新 。

EX:如果目前有兩張卡片(43A8,3D5D)，迴圈就會
更新43A8+3D5D資料, 1秒間隔, 更新43A8+3D5D資料, 1秒間隔, 更新43A8+3D5D資料, 1秒間隔, 更新43A8+3D5D資料...  這樣更新 。</font>

![](https://i.imgur.com/ZNLawjD.png)

我就是設定變數去接每個迴圈裡面的資料，並計算出其他資訊(座標、是否更新、目前卡片張數)一起丟進Mysql和Redis裡面。

<font color=blue>程式分成4個主要區塊 :0.獲得基站座標 1.定位計算 2.迴圈接資料 3.卡片狀況(張數&是否更新)</font>

## C#程式碼修改後的區塊摘要:
1. 引用Mysql,Redis的函式庫
2. 全域變數宣告
3. Form1_Load的函數中做的修改
    * 直接定位的部分
    * 設定頁面的部分
4. 三點與四點定位計算函數
5. 宣告可以記錄tag狀態的類別(card_counter)，並用List(myCardExistList)的方式來記錄全部卡片的狀態
6. 宣告可以用於傳入資料庫卡片資訊的類別(card_data_set)
7. 各基站所抓取的距離(d1~d5)
8. 各基站的名稱(port1_calc~port5_calc)
9. MyTimer_Tick，MyTimer_Tick就是用一個foreach的迴圈不斷更新卡片距離電量等等的資訊在下面的監控列表，我就是設定變數去接每個迴圈裡面的資料丟進Mysql和Redis裡面。

![](https://i.imgur.com/ZNLawjD.png)
* MyTimer_Tick任務
    * <font color=red>設定變數去接每個迴圈裡面的資料丟進Mysql和Redis裡面</font>
    * <font color=red>定位計算</font>
    * <font color=red>卡片狀態紀錄</font>

## 開始解說!!

### 引用Mysql,Redis的函式庫:
![](https://i.imgur.com/DMdjKkf.png)
### 全域變數宣告(後面都會有所解釋):
![](https://i.imgur.com/rfKuPQA.png)

* 目前四台基站的座標
![](https://i.imgur.com/EgP5WVq.png)

* 如果想由網頁設定也可以，可以把四台基站座標的陣列先宣告出來，再到redis裡面的set_info去抓。下面會有程式碼解釋
![](https://i.imgur.com/i4fe7Vo.png)


* 如果增加到超過四台基站以後會變成List的形式，myPortList裡面是port_counter的物件，而X1到X4變成計算定位點時，所需要的四個基站座標參數
* 一個port_counter的物件會有
    * id : 基站id
    * position : 基站的位置
    * distance : 基站到卡片的距離
![](https://i.imgur.com/O0PbhpI.png)



### Form1_Load的函數中做的修改(直接定位):
* Form1_Load是定位程式啟動之後一開始會跑的函數，從這邊更改一些初始參數，就可以讓程式一啟動就直接開始定位(232行~233行)
```
TagPageBox.SelectedIndex = 2;// 直接到監控列表頁面
Start_Listen_Btn_Click("Start monitoring", new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 2));

```

### Form1_Load的函數中做的修改(設定頁面):
* 因為前端所設定的基站座標會用List的方式存入Redis的set_info中，這邊的程式就是去set_info中將基站座標給pop出來
![](https://i.imgur.com/VWUb7bm.png)

* 基本跟Redis連線並取得List的長度(235行~237行)
```
ConnectionMultiplexer redis_outer = ConnectionMultiplexer.Connect("localhost");//大家使用redis的話應該都是本機端
IDatabase database = redis_outer.GetDatabase(1);  //連結到指定的資料庫，這樣代表就是db1
int length = (int)database.ListLength("set_info");
```
* 將redis裡面的List中座標字串pop出來，做字串切割的處理後，放入一開始全域變數有宣告的"各基站座標"的變數裡面
![](https://i.imgur.com/xkG6DVp.png)
```
for (int i = 0; i< length; i++)
{
    string RightPop = (String)database.ListRightPop("set_info");
    string[] coord1 = RightPop.Split('"');
    string[] coord = coord1[1].Split(',');
    Console.WriteLine(coord);
    double rst0 = 0.0;
    double rst1 = 0.0;
    double rst2 = 0.0;
    double.TryParse(coord[0], out rst0);
    double.TryParse(coord[1], out rst1);
    double.TryParse(coord[2], out rst2);
    Console.WriteLine(rst0);
    Console.WriteLine(rst1);
    Console.WriteLine(rst2);
    if (i==0)
    {
        double[] X1 = { rst0, rst1, rst2 };
    }
    if (i == 1)
    {
        double[] X2 = { rst0, rst1, rst2 };
    }
    if (i == 2)
    {
        double[] X3 = { rst0, rst1, rst2 };
    }
    if (i == 3)
    {
        double[] X4 = { rst0, rst1, rst2 };
    }
}

```
* 未來有更多基站的時候，就會變成List的形式在紀錄基站座標
```
for (int i = 0; i< length; i++)
{
    port_counter port_counter_data = new port_counter();
    port_counter_data.id = station[i];

    string RightPop = (String)database.ListRightPop("set_info");
    string[] coord1 = RightPop.Split('"');
    string[] coord = coord1[1].Split(',');
    Console.WriteLine(coord);
    double rst0 = 0.0;
    double rst1 = 0.0;
    double rst2 = 0.0;
    double.TryParse(coord[0], out rst0);
    double.TryParse(coord[1], out rst1);
    double.TryParse(coord[2], out rst2);
    Console.WriteLine(rst0);
    Console.WriteLine(rst1);
    Console.WriteLine(rst2);

    port_counter_data.position[0] = rst0;
    port_counter_data.position[1] = rst1;
    port_counter_data.position[2] = rst2;
    port_counter_data.distance = -1;
    myPortList.Add(port_counter_data);
}

```

### 三點與四點定位計算函數:
* 三點定位使用體積海龍公式，四點定位使用三平面求交點
* ans為四點定位的座標答案，ans1為三點定位的座標答案
* verification為四點定位存放有無實數解的變數，有解就是"have"，無解就是"none"。verification1為三點定位有無實數解的變數。

* 程式碼架構(3683行~4023行)

```
四點定位(各基站座標，測到的距離)
{
    答案存入ans；
    if ans 有實數解
    {
        verification = "have"；
        三點定位(各基站座標，測到的距離)；
    }
    else
    {
        verification = "none"；
        三點定位(各基站座標，測到的距離)；
    }
}

三點定位(各基站座標，測到的距離)
{
    答案存入ans1；
    if ans1 有實數解
    {
        verification1 = "have"；
    }
    else
    {
        verification1 = "none"；
    }
}
```
### 宣告可以記錄tag狀態的類別(card_counter)，並用List(myCardExistList)的方式來記錄全部卡片的狀態:
* 在MyTimer_Tick中會有詳細介紹如何使用這個List
* 一個card_counter的物件會有
    * id : 卡片id
    * without_move : 不動的時間
    * packet_receive : 封包接收數量
    * exist : 是否有在更新(有:0，無:1)
![](https://i.imgur.com/6Nj4NBl.png)

### 宣告可以用於傳入資料庫卡片資訊的類別(card_data_set):
![](https://i.imgur.com/DCwZG8n.png)

### 各基站所抓取的距離(d1~d5):
* 剛剛有解釋過，MyTimer_Tick裡面就是用一個foreach的迴圈不斷更新卡片距離電量等等的資訊，因此需要設定這些變數去接每個迴圈裡面的資料。
![](https://i.imgur.com/Hf7FQBW.png)

### 各基站的名稱(port1_calc~port5_calc):
* 同上
![](https://i.imgur.com/sVnr5IC.png)

## MyTimer_Tick(4085行~4641行):
* 重要概念:每一次迴圈都是跑一張卡片的更新而已。假如有張卡片為43A8，一個迴圈中，就只會得到各基站到這張43A8的卡片的距離，和43A8的電量等資料。

![](https://i.imgur.com/ZNLawjD.png)

* MyTimer_Tick我新增的部分:
    * 宣告一個類別為card_data_set的物件叫做redis_data，來接每次迴圈中所更新的資料，並在迴圈底部存入資料庫中
    * 宣告一個類別為card_counter的物件叫做card_counter_data，並利用myCardExistLists來記錄所有卡片是否在更新的資訊
    * 座標定位計算
        1. 如果 verification 跟 verification1 都是have，代表三點和四點定位都有答案，存入資料庫的座標為四點定位之解，並將兩者之解相減存入check，當作驗算
        2. 如果 verification 是 have，而verification1 是 none，存入資料庫的座標為四點定位之解，check為null3，表示三點定位無實數解
        3. 如果 verification 是 nine，而verification1 是 have，存入資料庫的座標為三點定位之解，check為null4，表示四點定位無實數解
        4. 如果 verification 跟 verification1 都是none，代表三點和四點定位都無實數解，存入資料庫的座標為上一次的答案，check為null_all，表示兩種算法都無實數解
        
    * myCardExistLists的操作
        1. 如果myCardExistLists中沒有這張卡片就會新增一個card_counter的物件存放這張卡片的資料，exist為0
        2. 如果有，就檢查這張卡片中的目前packet_receive有沒有跟上一次的packet_receive一樣
        3. 如果一樣，代表沒有更新，exist為1
        4. 如果不一樣，代表有更新，exist為0



    * 未來有更多基站時,myPortList的操作
        1. 將myPortList裡全部元素的distance設成-1
        2. 會依照基站名稱去找到myPortList中有一樣id的index
        3. 設定myPortList[index].distance為卡片到此基站的距離
        4. 把myPortList裡全部元素的distance不是-1的取出後計算定位
        * 由於我實作的定位函數的參數不是可以直接把四台基站座標和到卡片的距離隨便丟進去的，需要先擺把同高度平面的基站後，再擺不同平面的基站進去，所以需要寫判斷程式，下面有貼程式碼
        *  以夢想教室我們所實驗的為例:
            *  triposition1(三台地面的基站座標和到卡片距離，剩下的一台有高度的基站座標和到卡片距離);

* 程式碼部分
```
temp_last_without_move = tag.Value.No_Exe_Time;
temp_last_receive_packet = tag.Value.TotalPack;
card_counter_data.id = redis_data.id;

int index = myCardExistLists.FindIndex(x => x.id == card_counter_data.id);
if (index==-1)
{
    card_counter_data.packet_receive = temp_last_receive_packet;
    card_counter_data.without_move = temp_last_without_move;
    card_counter_data.exist = 0;
    myCardExistLists.Add(card_counter_data);
}
else
{
    if(temp_last_receive_packet == myCardExistLists[index].packet_receive)
    {
        myCardExistLists[index].packet_receive = temp_last_receive_packet;
        myCardExistLists[index].without_move = temp_last_without_move;
        myCardExistLists[index].exist = 1;
        redis_data.exist = 1;
    }
    else
    {
        myCardExistLists[index].packet_receive = temp_last_receive_packet;
        myCardExistLists[index].without_move = temp_last_without_move;
        myCardExistLists[index].exist = 0;
        redis_data.exist = 0;
    }
    
}


triposition1(X1[0], X1[1], X1[2], d1, port1_calc, X2[0], X2[1], X2[2], d2, port2_calc, X3[0], X3[1], X3[2], d3, port3_calc, X4[0], X4[1], X4[2], d4, port4_calc);

if(verification=="have"&& verification1 == "have")
{
    compare_distance = Math.Pow(ans[0] - ans1[0], 2) + Math.Pow(ans[1] - ans1[1], 2) + Math.Pow(ans[2] - ans1[2], 2);
    compare_distance = Math.Pow(compare_distance, 0.5);
    redis_data.check = compare_distance.ToString();
    final_ans[0] = ans[0];
    final_ans[1] = ans[1];
    final_ans[2] = ans[2];
    redis_data.coord = temp;
}
else if(verification == "have" && verification1 == "none")
{
    redis_data.check = "null3";
    final_ans[0] = ans[0];
    final_ans[1] = ans[1];
    final_ans[2] = ans[2];
    redis_data.coord = temp;
}
else if(verification == "none" && verification1 == "have")
{
    redis_data.check = "null4";
    final_ans[0] = ans1[0];
    final_ans[1] = ans1[1];
    final_ans[2] = ans1[2];
    redis_data.coord = temp1;
}
else
{
    redis_data.check = "null_all";
    int go = 0;
    string last_time_data = (String)db.ListGetByIndex("card_location",go);
    string[] last_time_word = last_time_data.Split('"');
    while(last_time_word[3] != redis_data.id)
    {
        go = go + 1;
        last_time_data = (String)db.ListGetByIndex("card_location",go);
        last_time_word = last_time_data.Split('"');
    }
    redis_data.coord = last_time_word[87];


}

DateTime myDate_redis = DateTime.Now;
redis_data.time = myDate_redis.ToString("yyyy-MM-dd HH:mm:ss");



string json = JsonConvert.SerializeObject(redis_data);//把物件壓成可以丟入redis的狀態(value)

db.ListLeftPush("card_count", myCardExistLists.Count);
db.ListLeftPush("card_location", json);


continue;

```

```
if(有超過3台基站以上的資料)
{
    找到同高度平面的是哪幾台；
    以及相對不同高度的是哪幾台；
    if(同高度平面的超過3台)
    {
        if(相對不同高度的>=1台)
        {
            四點定位(三台同高度平面，一台不同高度);
        }
        else
        {
            四點定位(三台同高度平面，-1);
        }
    }
    else
    {
        verification = "none";
        verification1 = "none";
    }
    
}
else
{
    verification = "none";
    verification1 = "none";
}
```






```
List<port_counter> list_need_calc = myPortList.FindAll(x => x.distance != -1);

if (list_need_calc.Count >= 3)
{
    int port_list_length = list_need_calc.Count;
    double[] port_z = new double[port_list_length];
    int k;
    for (k = 0; k < port_list_length; k++)
    {
        port_z[k] = list_need_calc[k].position[2];
    }

    int max_z_count = 0;
    double max_z = 0;

    foreach (var s in port_z.GroupBy(c => c))
    {
        if (s.Count() > max_z_count)
        {
            max_z_count = s.Count();
            max_z = s.Key;
        }
    }


    List<port_counter> listFind_plane_z = list_need_calc.FindAll(x => x.position[2] == max_z);
    List<port_counter> listFind_z = list_need_calc.FindAll(x => x.position[2] != max_z);

    if (listFind_plane_z.Count >= 3)
    {
        X1[0] = listFind_plane_z[0].position[0];
        X1[1] = listFind_plane_z[0].position[1];
        X1[2] = listFind_plane_z[0].position[2];
        X2[0] = listFind_plane_z[1].position[0];
        X2[1] = listFind_plane_z[1].position[1];
        X2[2] = listFind_plane_z[1].position[2];
        X3[0] = listFind_plane_z[2].position[0];
        X3[1] = listFind_plane_z[2].position[1];
        X3[2] = listFind_plane_z[2].position[2];

        if (listFind_z.Count >= 1)
        {
            X4[0] = listFind_z[0].position[0];
            X4[1] = listFind_z[0].position[1];
            X4[2] = listFind_z[0].position[2];

            Console.WriteLine(X1[0]);
            Console.WriteLine(X1[1]);
            Console.WriteLine(X1[2]);
            Console.WriteLine(X2[0]);
            Console.WriteLine(X2[1]);
            Console.WriteLine(X2[2]);

            Console.WriteLine(X3[0]);
            Console.WriteLine(X3[1]);
            Console.WriteLine(X3[2]);

            Console.WriteLine(X4[0]);
            Console.WriteLine(X4[1]);
            Console.WriteLine(X4[2]);

            triposition1(X1[0], X1[1], X1[2], listFind_plane_z[0].distance, listFind_plane_z[0].id, X2[0], X2[1], X2[2], listFind_plane_z[1].distance, listFind_plane_z[1].id, X3[0], X3[1], X3[2], listFind_plane_z[2].distance, listFind_plane_z[2].id, X4[0], X4[1], X4[2], listFind_z[0].distance, listFind_z[0].id);
        }
        else
        {
            X4[0] = -1;
            X4[1] = -1;
            X4[2] = -1;

            Console.WriteLine(X1[0]);
            Console.WriteLine(X1[1]);
            Console.WriteLine(X1[2]);
            Console.WriteLine(X2[0]);
            Console.WriteLine(X2[1]);
            Console.WriteLine(X2[2]);

            Console.WriteLine(X3[0]);
            Console.WriteLine(X3[1]);
            Console.WriteLine(X3[2]);

            Console.WriteLine(X4[0]);
            Console.WriteLine(X4[1]);
            Console.WriteLine(X4[2]);

            triposition1(X1[0], X1[1], X1[2], listFind_plane_z[0].distance, listFind_plane_z[0].id, X2[0], X2[1], X2[2], listFind_plane_z[1].distance, listFind_plane_z[1].id, X3[0], X3[1], X3[2], listFind_plane_z[2].distance, listFind_plane_z[2].id, X4[0], X4[1], X4[2], -1, "none");

        }
    }
    else
    {
        verification = "none";
        verification1 = "none";

    }
}
else
{
    verification = "none";
    verification1 = "none";
}


```





### <font color="red">系統操作流程</font>
1. 可以在網頁上面設定基站座標，設定好的基站位置會存入redis，然後去即時定位頁面上啟動定位程式。
2. C#的定位程式可以去Redis取用到基站座標，並開始定位，將tag的資訊存入Redis和Mysql裡面供前端取用
3. 如果需要關閉定位程式，網頁上也會有按鈕去關閉定位程式。


### 定位系統初始設定流程
![](https://i.imgur.com/azgySXb.png)
### 定位系統架構圖
![](https://i.imgur.com/LcugI3B.png)
### 定位系統操作流程 – 基站佈置
![](https://i.imgur.com/0Bh9eL1.png)







